using Microsoft.EntityFrameworkCore;
using ResumeAI.Resume.API.Data;
using ResumeAI.Resume.API.Models;
using ResumeAI.Resume.API.Repositories;

namespace ResumeAI.Resume.API.Services;

public interface ISectionService
{
    Task<IList<ResumeSection>> GetByResumeId(int resumeId);
    Task<ResumeSection?> GetById(int sectionId);
    Task<ResumeSection> AddSection(ResumeSection section);
    Task<ResumeSection> UpdateSection(ResumeSection section);
    Task DeleteSection(int sectionId);
    Task ReorderSections(int resumeId, IList<int> orderedIds);
    Task ToggleVisibility(int sectionId);
    Task<IList<ResumeSection>> BulkUpdate(IList<ResumeSection> sections);
}

public class SectionService : ISectionService
{
    private readonly ISectionRepository _repo;
    private readonly ResumeDbContext _ctx;

    public SectionService(ISectionRepository repo, ResumeDbContext ctx)
    {
        _repo = repo;
        _ctx = ctx;
    }

    public async Task<IList<ResumeSection>> GetByResumeId(int resumeId) =>
        await _repo.GetByResumeId(resumeId);

    public async Task<ResumeSection?> GetById(int sectionId) =>
        await _repo.GetById(sectionId);

    public async Task<ResumeSection> AddSection(ResumeSection section) =>
        await _repo.Save(section);

    public async Task<ResumeSection> UpdateSection(ResumeSection section) =>
        await _repo.Update(section);

    public async Task DeleteSection(int sectionId)
    {
        var section = await _repo.GetById(sectionId)
            ?? throw new KeyNotFoundException("Section not found");

        await _repo.Delete(section);
    }

    public async Task ReorderSections(int resumeId, IList<int> orderedIds)
    {
        // ExecuteUpdateAsync — no entity loading, direct DB update
        for (int i = 0; i < orderedIds.Count; i++)
        {
            await _ctx.ResumeSections
                .Where(s => s.SectionId == orderedIds[i] && s.ResumeId == resumeId)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(x => x.DisplayOrder, i));
        }
    }

    public async Task ToggleVisibility(int sectionId)
    {
        var section = await _repo.GetById(sectionId)
            ?? throw new KeyNotFoundException("Section not found");

        section.IsVisible = !section.IsVisible;
        await _repo.Update(section);
    }

    public async Task<IList<ResumeSection>> BulkUpdate(IList<ResumeSection> sections)
    {
        foreach (var section in sections)
            _ctx.ResumeSections.Update(section);

        // Single SaveChangesAsync for all updates
        await _ctx.SaveChangesAsync();
        return sections;
    }
}
