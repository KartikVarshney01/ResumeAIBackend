using Microsoft.AspNetCore.SignalR;
using ResumeAI.Notification.API.Hubs;
using ResumeAI.Notification.API.Models;
using ResumeAI.Notification.API.Repositories;

namespace ResumeAI.Notification.API.Services;

public interface INotificationService
{
    Task<NotificationEntity> CreateAndSend(int userId, string type, string title, string message, string? actionUrl = null);
    Task<IList<NotificationEntity>> GetByUserId(int userId);
    Task<IList<NotificationEntity>> GetUnread(int userId);
    Task<int> GetUnreadCount(int userId);
    Task MarkAsRead(int notificationId);
    Task MarkAllAsRead(int userId);
}

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repo;
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationService(
        INotificationRepository repo,
        IHubContext<NotificationHub> hubContext)
    {
        _repo       = repo;
        _hubContext = hubContext;
    }

    public async Task<NotificationEntity> CreateAndSend(
        int userId, string type, string title, string message, string? actionUrl = null)
    {
        // Save to DB
        var notification = new NotificationEntity
        {
            UserId    = userId,
            Type      = type,
            Title     = title,
            Message   = message,
            ActionUrl = actionUrl,
            IsRead    = false,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.Save(notification);

        // Push to user via SignalR in real time
        await _hubContext.Clients
            .Group($"user_{userId}")
            .SendAsync("ReceiveNotification", new
            {
                notification.NotificationId,
                notification.Type,
                notification.Title,
                notification.Message,
                notification.ActionUrl,
                notification.IsRead,
                notification.CreatedAt
            });

        return notification;
    }

    public async Task<IList<NotificationEntity>> GetByUserId(int userId) =>
        await _repo.GetByUserId(userId);

    public async Task<IList<NotificationEntity>> GetUnread(int userId) =>
        await _repo.GetUnreadByUserId(userId);

    public async Task<int> GetUnreadCount(int userId) =>
        await _repo.GetUnreadCount(userId);

    public async Task MarkAsRead(int notificationId) =>
        await _repo.MarkAsRead(notificationId);

    public async Task MarkAllAsRead(int userId) =>
        await _repo.MarkAllAsRead(userId);
}