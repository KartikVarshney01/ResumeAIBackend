using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResumeAI.Template.API.DTOs;
using ResumeAI.Template.API.Models;
using ResumeAI.Template.API.Services;

namespace ResumeAI.Template.API.Controllers;

[ApiController]
[Route("api/templates")]
public class TemplateController : ControllerBase
{
    private readonly ITemplateService _templateService;

    public TemplateController(ITemplateService templateService) =>
        _templateService = templateService;

    // GET /api/templates
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var templates = await _templateService.GetAllTemplates();
        var isPremium = IsPremiumUser();
        return Ok(templates.Select(t => MapToDto(t, isPremium)));
    }

    // GET /api/templates/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var template = await _templateService.GetTemplateById(id);
        if (template is null) return NotFound();

        var isPremium = IsPremiumUser();

        // Gate HtmlLayout and CssStyles for free users
        if (template.IsPremium && !isPremium)
        {
            return Ok(new TemplateResponseDto
            {
                TemplateId   = template.TemplateId,
                Name         = template.Name,
                Description  = template.Description,
                ThumbnailUrl = template.ThumbnailUrl,
                Category     = template.Category,
                IsPremium    = template.IsPremium,
                IsActive     = template.IsActive,
                UsageCount   = template.UsageCount,
                HtmlLayout   = null,
                CssStyles    = null,
                Locked       = true
            });
        }

        return Ok(MapToDto(template, isPremium));
    }

    // POST /api/templates
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreateTemplateDto dto)
    {
        var template = new ResumeTemplate
        {
            Name         = dto.Name,
            Description  = dto.Description,
            ThumbnailUrl = dto.ThumbnailUrl,
            HtmlLayout   = dto.HtmlLayout,
            CssStyles    = dto.CssStyles,
            Category     = dto.Category,
            IsPremium    = dto.IsPremium
        };

        var created = await _templateService.CreateTemplate(template);
        return StatusCode(201, MapToDto(created, isPremiumUser: true));
    }

    // PUT /api/templates/{id}
    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTemplateDto dto)
    {
        var existing = await _templateService.GetTemplateById(id);
        if (existing is null) return NotFound();

        if (dto.Name         is not null) existing.Name         = dto.Name;
        if (dto.Description  is not null) existing.Description  = dto.Description;
        if (dto.ThumbnailUrl is not null) existing.ThumbnailUrl = dto.ThumbnailUrl;
        if (dto.HtmlLayout   is not null) existing.HtmlLayout   = dto.HtmlLayout;
        if (dto.CssStyles    is not null) existing.CssStyles    = dto.CssStyles;
        if (dto.Category     is not null) existing.Category     = dto.Category;
        if (dto.IsPremium    is not null) existing.IsPremium    = dto.IsPremium.Value;

        var updated = await _templateService.UpdateTemplate(id, existing);
        return Ok(MapToDto(updated, isPremiumUser: true));
    }

    // GET /api/templates/free
    [HttpGet("free")]
    public async Task<IActionResult> GetFree()
    {
        var templates = await _templateService.GetFreeTemplates();
        return Ok(templates.Select(t => MapToDto(t, isPremiumUser: false)));
    }

    // GET /api/templates/premium
    [HttpGet("premium")]
    [Authorize]
    public async Task<IActionResult> GetPremium()
    {
        var templates = await _templateService.GetPremiumTemplates();
        return Ok(templates.Select(t => MapToDto(t, IsPremiumUser())));
    }

    // GET /api/templates/by-category?category=MODERN
    [HttpGet("by-category")]
    public async Task<IActionResult> GetByCategory([FromQuery] string category)
    {
        var templates = await _templateService.GetByCategory(category);
        return Ok(templates.Select(t => MapToDto(t, isPremiumUser: false)));
    }

    // GET /api/templates/popular
    [HttpGet("popular")]
    public async Task<IActionResult> GetPopular()
    {
        var templates = await _templateService.GetPopularTemplates();
        return Ok(templates.Select(t => MapToDto(t, isPremiumUser: false)));
    }

    // PUT /api/templates/{id}/deactivate
    [HttpPut("{id}/deactivate")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Deactivate(int id)
    {
        await _templateService.DeactivateTemplate(id);
        return NoContent();
    }

    // POST /api/templates/{id}/use
    [HttpPost("{id}/use")]
    [Authorize]
    public async Task<IActionResult> Use(int id)
    {
        await _templateService.IncrementUsage(id);
        return NoContent();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private bool IsPremiumUser()
    {
        var plan = User.FindFirstValue("plan");
        return plan == "PREMIUM";
    }

    private static TemplateResponseDto MapToDto(ResumeTemplate t, bool isPremiumUser) =>
        new()
        {
            TemplateId   = t.TemplateId,
            Name         = t.Name,
            Description  = t.Description,
            ThumbnailUrl = t.ThumbnailUrl,
            Category     = t.Category,
            IsPremium    = t.IsPremium,
            IsActive     = t.IsActive,
            UsageCount   = t.UsageCount,
            HtmlLayout   = isPremiumUser || !t.IsPremium ? t.HtmlLayout : null,
            CssStyles    = isPremiumUser || !t.IsPremium ? t.CssStyles  : null,
            Locked       = t.IsPremium && !isPremiumUser
        };
}