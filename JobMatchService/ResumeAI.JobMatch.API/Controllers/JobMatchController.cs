using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResumeAI.JobMatch.API.DTOs;
using ResumeAI.JobMatch.API.Models;
using ResumeAI.JobMatch.API.Services;

namespace ResumeAI.JobMatch.API.Controllers;

[ApiController]
[Route("api/jobmatches")]
[Authorize]
public class JobMatchController : ControllerBase
{
    private readonly IJobMatchService _jobMatchService;

    public JobMatchController(IJobMatchService jobMatchService) =>
        _jobMatchService = jobMatchService;

    // GET /api/jobmatches
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var jobs = await _jobMatchService.GetByUserId(GetCurrentUserId());
        return Ok(jobs.Select(MapToDto));
    }

    // GET /api/jobmatches/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var job = await _jobMatchService.GetById(id);
        if (job is null) return NotFound();
        return Ok(MapToDto(job));
    }

    // GET /api/jobmatches/resume/{resumeId}
    [HttpGet("resume/{resumeId}")]
    public async Task<IActionResult> GetByResume(int resumeId)
    {
        var jobs = await _jobMatchService.GetByResumeId(resumeId);
        return Ok(jobs.Select(MapToDto));
    }

    // POST /api/jobmatches
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] SaveJobDto dto)
    {
        var job = new JobMatchEntity
        {
            UserId         = GetCurrentUserId(),
            ResumeId       = dto.ResumeId,
            JobTitle       = dto.JobTitle,
            Company        = dto.Company,
            JobDescription = dto.JobDescription,
            JobUrl         = dto.JobUrl,
            Source         = dto.Source,
            Location       = dto.Location,
            SalaryRange    = dto.SalaryRange,
            IsRemote       = dto.IsRemote
        };

        // Auto calculate match score if resume content provided
        if (dto.ResumeContent is not null && dto.JobDescription is not null)
        {
            job.MatchScore = await _jobMatchService.CalculateMatchScore(
                dto.ResumeContent, dto.JobDescription);
        }

        var created = await _jobMatchService.SaveJob(job);
        return StatusCode(201, MapToDto(created));
    }

    // PUT /api/jobmatches/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateJobDto dto)
    {
        var job = await _jobMatchService.GetById(id);
        if (job is null) return NotFound();

        if (dto.JobTitle       is not null) job.JobTitle       = dto.JobTitle;
        if (dto.Company        is not null) job.Company        = dto.Company;
        if (dto.JobDescription is not null) job.JobDescription = dto.JobDescription;
        if (dto.JobUrl         is not null) job.JobUrl         = dto.JobUrl;
        if (dto.Location       is not null) job.Location       = dto.Location;
        if (dto.SalaryRange    is not null) job.SalaryRange    = dto.SalaryRange;
        if (dto.IsRemote       is not null) job.IsRemote       = dto.IsRemote.Value;

        var updated = await _jobMatchService.UpdateJob(job);
        return Ok(MapToDto(updated));
    }

    // DELETE /api/jobmatches/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _jobMatchService.DeleteJob(id);
        return NoContent();
    }

    // PUT /api/jobmatches/{id}/status
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
    {
        var updated = await _jobMatchService.UpdateStatus(id, dto.Status);
        return Ok(MapToDto(updated));
    }

    // POST /api/jobmatches/calculate-score
    [HttpPost("calculate-score")]
    public async Task<IActionResult> CalculateScore([FromBody] CalculateScoreDto dto)
    {
        var score = await _jobMatchService.CalculateMatchScore(
            dto.ResumeContent, dto.JobDescription);
        return Ok(new { score });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private int GetCurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static JobMatchResponseDto MapToDto(JobMatchEntity j) => new()
    {
        JobMatchId  = j.JobMatchId,
        ResumeId    = j.ResumeId,
        JobTitle    = j.JobTitle,
        Company     = j.Company,
        JobUrl      = j.JobUrl,
        Source      = j.Source,
        MatchScore  = j.MatchScore,
        Status      = j.Status,
        Location    = j.Location,
        SalaryRange = j.SalaryRange,
        IsRemote    = j.IsRemote,
        CreatedAt   = j.CreatedAt,
        UpdatedAt   = j.UpdatedAt
    };
}