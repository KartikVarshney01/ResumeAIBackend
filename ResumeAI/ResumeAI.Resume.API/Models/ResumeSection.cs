using System.ComponentModel.DataAnnotations;

namespace ResumeAI.Resume.API.Models;

public class ResumeSection
{
    public int SectionId { get; set; }

    public int ResumeId { get; set; }

    [Required]
    public string SectionType { get; set; } = string.Empty;

    [Required]
    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public int DisplayOrder { get; set; } = 0;

    public bool IsVisible { get; set; } = true;

    public bool AiGenerated { get; set; } = false;

    // Navigation property
    public ResumeEntity? Resume { get; set; }
}