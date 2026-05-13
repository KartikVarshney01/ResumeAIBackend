using Microsoft.EntityFrameworkCore;
using ResumeAI.Resume.API.Data;
using ResumeAI.Resume.API.Models;

namespace ResumeAI.Resume.API.Repositories;

public interface ISectionRepository
{
    Task<IList<ResumeSection>> GetByResumeId(int resumeId);
    Task<ResumeSection?> GetById(int sectionId);
    Task<ResumeSection> Save(ResumeSection section);
    Task<ResumeSection> Update(ResumeSection section);
    Task Delete(ResumeSection section);
}

public class SectionRepository : ISectionRepository
{
    private readonly ResumeDbContext _ctx;

    public SectionRepository(ResumeDbContext ctx) => _ctx = ctx;

    public async Task<IList<ResumeSection>> GetByResumeId(int resumeId) =>
        await _ctx.ResumeSections
            .Where(s => s.ResumeId == resumeId)
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync();

    public async Task<ResumeSection?> GetById(int sectionId) =>
        await _ctx.ResumeSections.FindAsync(sectionId);

    public async Task<ResumeSection> Save(ResumeSection section)
    {
        _ctx.ResumeSections.Add(section);
        await _ctx.SaveChangesAsync();
        return section;
    }

    public async Task<ResumeSection> Update(ResumeSection section)
    {
        _ctx.ResumeSections.Update(section);
        await _ctx.SaveChangesAsync();
        return section;
    }

    public async Task Delete(ResumeSection section)
    {
        _ctx.ResumeSections.Remove(section);
        await _ctx.SaveChangesAsync();
    }
}