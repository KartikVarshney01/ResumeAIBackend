using System.ComponentModel.DataAnnotations;

namespace ResumeAI.JobMatch.API.Models;

public class JobMatchEntity
{
    public int JobMatchId { get; set; }

    [Required, MaxLength(200)]
    public string JobTitle { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Company { get; set; } = string.Empty;

    public string? JobDescription { get; set; }

    public string? JobUrl { get; set; }

    public int UserId { get; set; }

    public int ResumeId { get; set; }

    public string Source { get; set; } = "MANUAL";

    public int MatchScore { get; set; } = 0;

    public string Status { get; set; } = "SAVED";

    public string? Location { get; set; }

    public string? SalaryRange { get; set; }

    public bool IsRemote { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}