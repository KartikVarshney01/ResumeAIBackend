using System.ComponentModel.DataAnnotations;

namespace ResumeAI.Notification.API.Models;

public class NotificationEntity
{
    public int NotificationId { get; set; }

    public int UserId { get; set; }

    [Required]
    public string Type { get; set; } = string.Empty;
    // EXPORT_COMPLETE | AI_COMPLETE | JOB_MATCH | SYSTEM

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; } = false;

    public string? ActionUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}