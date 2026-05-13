using System.ComponentModel.DataAnnotations;

namespace ResumeAI.Template.API.Models;

public class ResumeTemplate
{
    public int TemplateId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public string? ThumbnailUrl { get; set; }

    public string HtmlLayout { get; set; } = string.Empty;

    public string CssStyles { get; set; } = string.Empty;

    public string Category { get; set; } = "PROFESSIONAL";
    // PROFESSIONAL | CREATIVE | MODERN | MINIMALIST | ATS-OPTIMISED

    public bool IsPremium { get; set; } = false;

    public bool IsActive { get; set; } = true;

    public int UsageCount { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}