using System.ComponentModel.DataAnnotations;

namespace ResumeAI.AI.API.Models;

public class AiRequest
{
    public int RequestId { get; set; }

    public int UserId { get; set; }

    public int ResumeId { get; set; }

    [Required]
    public string RequestType { get; set; } = string.Empty;
    // GENERATE_SUMMARY | IMPROVE_BULLET | ATS_CHECK | TAILOR_JOB | SKILL_SUGGEST

    public string Prompt { get; set; } = string.Empty;

    public string? Response { get; set; }

    public string Status { get; set; } = "PENDING"; // PENDING | PROCESSING | COMPLETED | FAILED

    public string AiProvider { get; set; } = "OPENAI"; // OPENAI | ANTHROPIC

    public int TokensUsed { get; set; } = 0;

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }
}