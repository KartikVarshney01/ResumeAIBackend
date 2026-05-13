using System.ComponentModel.DataAnnotations;

namespace ResumeAI.Export.API.DTOs;

public class RequestExportDto
{
    [Required]
    public int ResumeId { get; set; }

    [Required]
    public string Format { get; set; } = string.Empty; // PDF | DOCX

    // Resume data for rendering
    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public string Email { get; set; } = string.Empty;

    public string? TargetJobTitle { get; set; }

    public IList<SectionDto> Sections { get; set; } = new List<SectionDto>();
}

public class SectionDto
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class ExportJobResponseDto
{
    public int ExportJobId { get; set; }
    public int ResumeId { get; set; }
    public string Format { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? FileUrl { get; set; }
    public string? ErrorMessage { get; set; }
    public long FileSizeBytes { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}