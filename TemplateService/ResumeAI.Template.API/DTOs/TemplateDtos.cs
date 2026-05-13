using System.ComponentModel.DataAnnotations;

namespace ResumeAI.Template.API.DTOs;

public class CreateTemplateDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public string? ThumbnailUrl { get; set; }

    [Required]
    public string HtmlLayout { get; set; } = string.Empty;

    [Required]
    public string CssStyles { get; set; } = string.Empty;

    [Required]
    public string Category { get; set; } = "PROFESSIONAL";

    public bool IsPremium { get; set; } = false;
}

public class UpdateTemplateDto
{
    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public string? ThumbnailUrl { get; set; }

    public string? HtmlLayout { get; set; }

    public string? CssStyles { get; set; }

    public string? Category { get; set; }

    public bool? IsPremium { get; set; }
}

public class TemplateResponseDto
{
    public int TemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool IsPremium { get; set; }
    public bool IsActive { get; set; }
    public int UsageCount { get; set; }

    // These are null for Free users accessing Premium templates
    public string? HtmlLayout { get; set; }
    public string? CssStyles { get; set; }

    // Tells frontend to show upgrade prompt
    public bool Locked { get; set; } = false;
}