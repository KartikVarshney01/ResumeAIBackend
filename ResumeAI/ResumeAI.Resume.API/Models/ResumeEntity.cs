using System.ComponentModel.DataAnnotations;

namespace ResumeAI.Resume.API.Models;

public class ResumeEntity
{
    public int ResumeId { get; set; }

    [Required, MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    public string? TargetJobTitle { get; set; }

    public int UserId { get; set; }

    public int TemplateId { get; set; }

    public int AtsScore { get; set; } = 0;

    public string Status { get; set; } = "DRAFT";

    public string Language { get; set; } = "en";

    public bool IsPublic { get; set; } = false;

    public int ViewCount { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ResumeSection> Sections { get; set; } = new List<ResumeSection>();
}