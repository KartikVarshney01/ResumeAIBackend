using Microsoft.EntityFrameworkCore;
using ResumeAI.Resume.API.Data;
using ResumeAI.Resume.API.Models;

namespace ResumeAI.Resume.API.Repositories;

public interface IResumeRepository
{
    Task<IList<ResumeEntity>> GetAllByUserId(int userId);
    Task<ResumeEntity?> GetById(int resumeId);
    Task<ResumeEntity> Save(ResumeEntity resume);
    Task<ResumeEntity> Update(ResumeEntity resume);
    Task Delete(ResumeEntity resume);
    Task<int> CountByUserId(int userId);
}

public class ResumeRepository : IResumeRepository
{
    private readonly ResumeDbContext _ctx;

    public ResumeRepository(ResumeDbContext ctx) => _ctx = ctx;

    public async Task<IList<ResumeEntity>> GetAllByUserId(int userId) =>
        await _ctx.Resumes
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.UpdatedAt)
            .ToListAsync();

    public async Task<ResumeEntity?> GetById(int resumeId) =>
        await _ctx.Resumes
            .Include(r => r.Sections.OrderBy(s => s.DisplayOrder))
            .FirstOrDefaultAsync(r => r.ResumeId == resumeId);

    public async Task<ResumeEntity> Save(ResumeEntity resume)
    {
        _ctx.Resumes.Add(resume);
        await _ctx.SaveChangesAsync();
        return resume;
    }

    public async Task<ResumeEntity> Update(ResumeEntity resume)
    {
        resume.UpdatedAt = DateTime.UtcNow;
        _ctx.Resumes.Update(resume);
        await _ctx.SaveChangesAsync();
        return resume;
    }

    public async Task Delete(ResumeEntity resume)
    {
        _ctx.Resumes.Remove(resume);
        await _ctx.SaveChangesAsync();
    }

    public async Task<int> CountByUserId(int userId) =>
        await _ctx.Resumes.CountAsync(r => r.UserId == userId);
}