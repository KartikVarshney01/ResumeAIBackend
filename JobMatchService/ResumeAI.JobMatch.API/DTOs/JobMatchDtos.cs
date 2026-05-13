using System.ComponentModel.DataAnnotations;

namespace ResumeAI.JobMatch.API.DTOs;

public class SaveJobDto
{
    [Required]
    public int ResumeId { get; set; }

    [Required, MaxLength(200)]
    public string JobTitle { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Company { get; set; } = string.Empty;

    public string? JobDescription { get; set; }

    public string? JobUrl { get; set; }

    public string Source { get; set; } = "MANUAL";

    public string? Location { get; set; }

    public string? SalaryRange { get; set; }

    public bool IsRemote { get; set; } = false;

    // Optional — resume content for auto score calculation
    public string? ResumeContent { get; set; }
}

public class UpdateJobDto
{
    [MaxLength(200)]
    public string? JobTitle { get; set; }

    [MaxLength(200)]
    public string? Company { get; set; }

    public string? JobDescription { get; set; }

    public string? JobUrl { get; set; }

    public string? Location { get; set; }

    public string? SalaryRange { get; set; }

    public bool? IsRemote { get; set; }
}

public class UpdateStatusDto
{
    [Required]
    public string Status { get; set; } = string.Empty;
}

public class CalculateScoreDto
{
    [Required]
    public string ResumeContent { get; set; } = string.Empty;

    [Required]
    public string JobDescription { get; set; } = string.Empty;
}

public class JobMatchResponseDto
{
    public int JobMatchId { get; set; }
    public int ResumeId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string? JobUrl { get; set; }
    public string Source { get; set; } = string.Empty;
    public int MatchScore { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? SalaryRange { get; set; }
    public bool IsRemote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}