using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResumeAI.Resume.API.DTOs;
using ResumeAI.Resume.API.Models;
using ResumeAI.Resume.API.Services;

namespace ResumeAI.Resume.API.Controllers;

[ApiController]
[Route("api/resumes")]
[Authorize]
public class ResumeController : ControllerBase
{
    private readonly IResumeService _resumeService;

    public ResumeController(IResumeService resumeService) =>
        _resumeService = resumeService;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _resumeService.GetAllByUserId(GetCurrentUserId()));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var resume = await _resumeService.GetById(id);
        if (resume is null) return NotFound();
        return Ok(resume);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateResumeDto dto)
    {
        var resume = new ResumeEntity
        {
            UserId         = GetCurrentUserId(),
            Title          = dto.Title,
            TargetJobTitle = dto.TargetJobTitle,
            TemplateId     = dto.TemplateId,
            Language       = dto.Language
        };
        var created = await _resumeService.CreateResume(resume, GetCurrentPlan());
        return StatusCode(201, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateResumeDto dto)
    {
        var resume = await _resumeService.GetById(id);
        if (resume is null) return NotFound();

        if (dto.Title          is not null) resume.Title          = dto.Title;
        if (dto.TargetJobTitle is not null) resume.TargetJobTitle = dto.TargetJobTitle;
        if (dto.TemplateId     is not null) resume.TemplateId     = dto.TemplateId.Value;
        if (dto.Status         is not null) resume.Status         = dto.Status;
        if (dto.Language       is not null) resume.Language       = dto.Language;

        await _resumeService.UpdateResume(resume);
        return Ok(resume);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _resumeService.DeleteResume(id);
        return NoContent();
    }

    [HttpPost("{id}/duplicate")]
    public async Task<IActionResult> Duplicate(int id)
    {
        var duplicate = await _resumeService.DuplicateResume(id, GetCurrentPlan());
        return StatusCode(201, duplicate);
    }

    [HttpPut("{id}/publish")]
    public async Task<IActionResult> Publish(int id)
    {
        await _resumeService.PublishResume(id);
        return NoContent();
    }

    [HttpPut("{id}/ats-score")]
    public async Task<IActionResult> UpdateAtsScore(int id, [FromBody] UpdateAtsScoreDto dto)
    {
        await _resumeService.UpdateAtsScore(id, dto.Score);
        return NoContent();
    }

    [HttpGet("public")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublic() =>
        Ok(await _resumeService.GetPublicResumes());

    private int GetCurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private string GetCurrentPlan() =>
        User.FindFirstValue("plan") ?? "FREE";
}