using System.ComponentModel.DataAnnotations;

namespace ResumeAI.AI.API.DTOs;

public class GenerateSummaryDto
{
    [Required]
    public int ResumeId { get; set; }

    [Required]
    public string ResumeContent { get; set; } = string.Empty;
}

public class ImproveBulletDto
{
    [Required]
    public int ResumeId { get; set; }

    [Required]
    public string BulletText { get; set; } = string.Empty;
}

public class AtsCheckDto
{
    [Required]
    public int ResumeId { get; set; }

    [Required]
    public string ResumeContent { get; set; } = string.Empty;

    [Required]
    public string JobDescription { get; set; } = string.Empty;
}

public class TailorJobDto
{
    [Required]
    public int ResumeId { get; set; }

    [Required]
    public string ResumeContent { get; set; } = string.Empty;

    [Required]
    public string JobDescription { get; set; } = string.Empty;
}

public class SuggestSkillsDto
{
    [Required]
    public int ResumeId { get; set; }

    [Required]
    public string CurrentSkills { get; set; } = string.Empty;

    [Required]
    public string JobDescription { get; set; } = string.Empty;
}

public class AiResponseDto
{
    public int RequestId { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public string? Response { get; set; }
    public string Status { get; set; } = string.Empty;
    public string AiProvider { get; set; } = string.Empty;
    public int TokensUsed { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class QuotaResponseDto
{
    public int RemainingRequests { get; set; }
    public int MonthlyLimit { get; set; }
    public int UsedRequests { get; set; }
    public string Plan { get; set; } = string.Empty;
}