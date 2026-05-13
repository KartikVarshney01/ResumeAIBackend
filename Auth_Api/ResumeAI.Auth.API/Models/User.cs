using System.ComponentModel.DataAnnotations;

namespace ResumeAI.Auth.API.Models;
public class User
{
    public int UserId {get; set;}

    [Required, MaxLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    public string Role { get; set; } = "USER"; // USER | ADMIN

    public string Provider { get; set; } = "LOCAL";  // LOCAL | GOOGLE | LINKEDIN

    public bool IsActive { get; set; } = true;

    public string SubscriptionPlan { get; set; } = "FREE"; // FREE | PREMIUM

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}