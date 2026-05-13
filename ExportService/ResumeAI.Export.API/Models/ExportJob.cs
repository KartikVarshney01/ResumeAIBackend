using System.ComponentModel.DataAnnotations;

namespace ResumeAI.Export.API.Models;

public class ExportJob
{
    public int ExportJobId { get; set; }

    public int UserId { get; set; }

    public int ResumeId { get; set; }

    [Required]
    public string Format { get; set; } = string.Empty;
    // PDF | DOCX

    public string Status { get; set; } = "PENDING";
    // PENDING | PROCESSING | COMPLETED | FAILED

    public string? FileUrl { get; set; }

    public string? ErrorMessage { get; set; }

    public long FileSizeBytes { get; set; } = 0;

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }
}