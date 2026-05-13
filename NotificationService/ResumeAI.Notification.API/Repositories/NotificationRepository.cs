using Microsoft.EntityFrameworkCore;
using ResumeAI.Notification.API.Data;
using ResumeAI.Notification.API.Models;

namespace ResumeAI.Notification.API.Repositories;

public interface INotificationRepository
{
    Task<IList<NotificationEntity>> GetByUserId(int userId);
    Task<IList<NotificationEntity>> GetUnreadByUserId(int userId);
    Task<NotificationEntity> Save(NotificationEntity notification);
    Task MarkAsRead(int notificationId);
    Task MarkAllAsRead(int userId);
    Task<int> GetUnreadCount(int userId);
}

public class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _ctx;

    public NotificationRepository(NotificationDbContext ctx) => _ctx = ctx;

    public async Task<IList<NotificationEntity>> GetByUserId(int userId) =>
        await _ctx.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

    public async Task<IList<NotificationEntity>> GetUnreadByUserId(int userId) =>
        await _ctx.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

    public async Task<NotificationEntity> Save(NotificationEntity notification)
    {
        _ctx.Notifications.Add(notification);
        await _ctx.SaveChangesAsync();
        return notification;
    }

    public async Task MarkAsRead(int notificationId)
    {
        await _ctx.Notifications
            .Where(n => n.NotificationId == notificationId)
            .ExecuteUpdateAsync(n =>
                n.SetProperty(x => x.IsRead, true));
    }

    public async Task MarkAllAsRead(int userId)
    {
        await _ctx.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(n =>
                n.SetProperty(x => x.IsRead, true));
    }

    public async Task<int> GetUnreadCount(int userId) =>
        await _ctx.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
}