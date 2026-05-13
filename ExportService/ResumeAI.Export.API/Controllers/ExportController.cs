using System.Security.Claims;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResumeAI.Export.API.Consumers;
using ResumeAI.Export.API.DTOs;
using ResumeAI.Export.API.Models;
using ResumeAI.Export.API.Services;
using ResumeAI.Shared.Events;

namespace ResumeAI.Export.API.Controllers;

[ApiController]
[Route("api/exports")]
[Authorize]
public class ExportController : ControllerBase
{
    private readonly IExportService _exportService;
    private readonly IPublishEndpoint _publishEndpoint;

    public ExportController(
        IExportService exportService,
        IPublishEndpoint publishEndpoint)
    {
        _exportService   = exportService;
        _publishEndpoint = publishEndpoint;
    }

    // POST /api/exports/request
    [HttpPost("request")]
    public async Task<IActionResult> RequestExport([FromBody] RequestExportDto dto)
    {
        var userId = GetCurrentUserId();

        // Create export job in DB
        var job = await _exportService.RequestExport(
            userId, dto.ResumeId, dto.Format);

        // Publish message to RabbitMQ for async processing
        await _publishEndpoint.Publish(new ExportRequestMessage
        {
            JobId          = job.ExportJobId,
            UserId         = userId,
            ResumeId       = dto.ResumeId,
            Format         = dto.Format,
            FullName       = dto.FullName,
            Email          = dto.Email,
            TargetJobTitle = dto.TargetJobTitle,
            Sections       = dto.Sections.Select(s => new SectionMessage
            {
                Title   = s.Title,
                Content = s.Content
            }).ToList()
        });

        return StatusCode(202, new
        {
            job.ExportJobId,
            job.Status,
            message = "Export job queued — check status with GET /api/exports/{id}"
        });
    }

    // GET /api/exports/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var job = await _exportService.GetJobById(id);
        if (job is null) return NotFound();
        return Ok(MapToDto(job));
    }

    // GET /api/exports/user
    [HttpGet("user")]
    public async Task<IActionResult> GetUserExports()
    {
        var exports = await _exportService.GetUserExports(GetCurrentUserId());
        return Ok(exports.Select(MapToDto));
    }

    // GET /api/exports/resume/{resumeId}
    [HttpGet("resume/{resumeId}")]
    public async Task<IActionResult> GetResumeExports(int resumeId)
    {
        var exports = await _exportService.GetResumeExports(resumeId);
        return Ok(exports.Select(MapToDto));
    }

    // GET /api/exports/{id}/download
    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(int id)
    {
        var userId = GetCurrentUserId();
        var job    = await _exportService.GetJobById(id);
        
        if (job is null) return NotFound();
        
        // Security check: Ensure user owns this export
        if (job.UserId != userId)
            return Forbid();

        if (job.Status != "COMPLETED")
            return BadRequest(new { error = "Export not ready yet" });

        // If local file
        if (job.FileUrl!.StartsWith("/exports/"))
        {
            var filePath = job.FileUrl.TrimStart('/');
            if (!System.IO.File.Exists(filePath))
                return NotFound(new { error = "File not found on server" });

            var bytes       = await System.IO.File.ReadAllBytesAsync(filePath);
            var contentType = job.Format == "PDF"
                ? "application/pdf"
                : "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            // Return as attachment to force download
            return File(bytes, contentType, $"resume_{job.ResumeId}_{DateTime.Now:yyyyMMdd}.{job.Format.ToLower()}");
        }

        // If Azure Blob — redirect to URL
        return Redirect(job.FileUrl);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private int GetCurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static ExportJobResponseDto MapToDto(ExportJob j) => new()
    {
        ExportJobId   = j.ExportJobId,
        ResumeId      = j.ResumeId,
        Format        = j.Format,
        Status        = j.Status,
        FileUrl       = j.FileUrl,
        ErrorMessage  = j.ErrorMessage,
        FileSizeBytes = j.FileSizeBytes,
        RequestedAt   = j.RequestedAt,
        CompletedAt   = j.CompletedAt
    };
}
