using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResumeAI.Notification.API.Services;

namespace ResumeAI.Notification.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService) =>
        _notificationService = notificationService;

    // GET /api/notifications
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var notifications = await _notificationService.GetByUserId(GetCurrentUserId());
        return Ok(notifications);
    }

    // GET /api/notifications/unread
    [HttpGet("unread")]
    public async Task<IActionResult> GetUnread()
    {
        var notifications = await _notificationService.GetUnread(GetCurrentUserId());
        return Ok(notifications);
    }

    // GET /api/notifications/unread/count
    [HttpGet("unread/count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var count = await _notificationService.GetUnreadCount(GetCurrentUserId());
        return Ok(new { count });
    }

    // PUT /api/notifications/{id}/read
    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        await _notificationService.MarkAsRead(id);
        return NoContent();
    }

    // PUT /api/notifications/read-all
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await _notificationService.MarkAllAsRead(GetCurrentUserId());
        return NoContent();
    }

    // POST /api/notifications/send (admin only — for system notifications)
    [HttpPost("send")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Send([FromBody] SendNotificationDto dto)
    {
        var notification = await _notificationService.CreateAndSend(
            dto.UserId, dto.Type, dto.Title, dto.Message, dto.ActionUrl);
        return StatusCode(201, notification);
    }

    private int GetCurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

public class SendNotificationDto
{
    public int UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
}