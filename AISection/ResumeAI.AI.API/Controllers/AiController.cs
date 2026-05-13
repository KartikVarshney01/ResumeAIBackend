using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResumeAI.AI.API.DTOs;
using ResumeAI.AI.API.Models;
using ResumeAI.AI.API.Repositories;
using ResumeAI.AI.API.Services;

namespace ResumeAI.AI.API.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IAiService _aiService;
    private readonly IAiRequestRepository _repo;

    public AiController(IAiService aiService, IAiRequestRepository repo)
    {
        _aiService = aiService;
        _repo      = repo;
    }

    // POST /api/ai/generate-summary
    [HttpPost("generate-summary")]
    public async Task<IActionResult> GenerateSummary([FromBody] GenerateSummaryDto dto)
    {
        var result = await _aiService.GenerateSummary(
            GetCurrentUserId(), dto.ResumeId, dto.ResumeContent);
        return Ok(MapToDto(result));
    }

    // POST /api/ai/improve-bullet
    [HttpPost("improve-bullet")]
    public async Task<IActionResult> ImproveBullet([FromBody] ImproveBulletDto dto)
    {
        var result = await _aiService.ImproveBullet(
            GetCurrentUserId(), dto.ResumeId, dto.BulletText);
        return Ok(MapToDto(result));
    }

    // POST /api/ai/ats-check
    [HttpPost("ats-check")]
    public async Task<IActionResult> AtsCheck([FromBody] AtsCheckDto dto)
    {
        var result = await _aiService.CheckAtsCompatibility(
            GetCurrentUserId(), dto.ResumeId, dto.ResumeContent, dto.JobDescription);
        return Ok(MapToDto(result));
    }

    // POST /api/ai/tailor-job
    [HttpPost("tailor-job")]
    public async Task<IActionResult> TailorJob([FromBody] TailorJobDto dto)
    {
        var result = await _aiService.TailorForJob(
            GetCurrentUserId(), dto.ResumeId, dto.ResumeContent, dto.JobDescription);
        return Ok(MapToDto(result));
    }

    // POST /api/ai/suggest-skills
    [HttpPost("suggest-skills")]
    public async Task<IActionResult> SuggestSkills([FromBody] SuggestSkillsDto dto)
    {
        var result = await _aiService.SuggestSkills(
            GetCurrentUserId(), dto.ResumeId, dto.CurrentSkills, dto.JobDescription);
        return Ok(MapToDto(result));
    }

    // GET /api/ai/quota
    [HttpGet("quota")]
    public async Task<IActionResult> GetQuota()
    {
        var userId    = GetCurrentUserId();
        var plan      = GetCurrentPlan();
        var remaining = await _aiService.GetRemainingQuota(userId, plan);
        var used      = await _repo.GetMonthlyUsageCount(userId);
        var limit     = plan == "PREMIUM" ? 100 : 50;

        return Ok(new QuotaResponseDto
        {
            RemainingRequests = remaining,
            MonthlyLimit      = limit,
            UsedRequests      = used,
            Plan              = plan
        });
    }

    // GET /api/ai/history/user
    [HttpGet("history/user")]
    public async Task<IActionResult> GetUserHistory()
    {
        var requests = await _repo.GetByUserId(GetCurrentUserId());
        return Ok(requests.Select(MapToDto));
    }

    // GET /api/ai/history/resume/{resumeId}
    [HttpGet("history/resume/{resumeId}")]
    public async Task<IActionResult> GetResumeHistory(int resumeId)
    {
        var requests = await _repo.GetByResumeId(resumeId);
        return Ok(requests.Select(MapToDto));
    }

    // GET /api/ai/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var request = await _repo.GetById(id);
        if (request is null) return NotFound();
        return Ok(MapToDto(request));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private int GetCurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private string GetCurrentPlan() =>
        User.FindFirstValue("plan") ?? "FREE";

    private static AiResponseDto MapToDto(AiRequest r) => new()
    {
        RequestId   = r.RequestId,
        RequestType = r.RequestType,
        Response    = r.Response,
        Status      = r.Status,
        AiProvider  = r.AiProvider,
        TokensUsed  = r.TokensUsed,
        RequestedAt = r.RequestedAt,
        CompletedAt = r.CompletedAt
    };
}