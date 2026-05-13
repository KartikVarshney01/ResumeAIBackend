using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResumeAI.Resume.API.DTOs;
using ResumeAI.Resume.API.Models;
using ResumeAI.Resume.API.Services;

namespace ResumeAI.Resume.API.Controllers;

[ApiController]
[Route("api/sections")]
[Authorize]
public class SectionController : ControllerBase
{
    private readonly ISectionService _sectionService;

    public SectionController(ISectionService sectionService) =>
        _sectionService = sectionService;

    // GET /api/sections/resume/{resumeId}
    [HttpGet("resume/{resumeId}")]
    public async Task<IActionResult> GetByResume(int resumeId)
    {
        var sections = await _sectionService.GetByResumeId(resumeId);
        return Ok(sections);
    }

    // POST /api/sections
    [HttpPost]
    public async Task<IActionResult> Add([FromBody] CreateSectionDto dto)
    {
        var section = new ResumeSection
        {
            ResumeId     = dto.ResumeId,
            SectionType  = dto.SectionType,
            Title        = dto.Title,
            Content      = dto.Content,
            DisplayOrder = dto.DisplayOrder
        };

        var created = await _sectionService.AddSection(section);
        return StatusCode(201, created);
    }

    // PUT /api/sections/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSectionDto dto)
    {
        var section = await _sectionService.GetById(id);
        if (section is null) return NotFound();

        if (dto.Title       is not null) section.Title       = dto.Title;
        if (dto.Content     is not null) section.Content     = dto.Content;
        if (dto.SectionType is not null) section.SectionType = dto.SectionType;

        await _sectionService.UpdateSection(section);
        return Ok(section);
    }

    // DELETE /api/sections/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _sectionService.DeleteSection(id);
        return NoContent();
    }

    // PUT /api/sections/reorder
    [HttpPut("reorder")]
    public async Task<IActionResult> Reorder([FromBody] ReorderSectionsDto dto)
    {
        await _sectionService.ReorderSections(dto.ResumeId, dto.OrderedIds);
        return NoContent();
    }

    // PUT /api/sections/{id}/visibility
    [HttpPut("{id}/visibility")]
    public async Task<IActionResult> ToggleVisibility(int id)
    {
        await _sectionService.ToggleVisibility(id);
        return NoContent();
    }

    // PUT /api/sections/bulk-update
    [HttpPut("bulk-update")]
    public async Task<IActionResult> BulkUpdate([FromBody] BulkUpdateSectionsDto dto)
    {
        var sections = await _sectionService.GetByResumeId(dto.ResumeId);

        foreach (var item in dto.Sections)
        {
            var section = sections.FirstOrDefault(s => s.SectionId == item.SectionId);
            if (section is null) continue;

            if (item.Title        is not null) section.Title        = item.Title;
            if (item.Content      is not null) section.Content      = item.Content;
            if (item.IsVisible    is not null) section.IsVisible    = item.IsVisible.Value;
            if (item.DisplayOrder is not null) section.DisplayOrder = item.DisplayOrder.Value;
        }

        var updated = await _sectionService.BulkUpdate(sections);
        return Ok(updated);
    }
}