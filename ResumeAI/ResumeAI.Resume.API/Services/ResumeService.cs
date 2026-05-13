using Microsoft.EntityFrameworkCore;
using ResumeAI.Resume.API.Data;
using ResumeAI.Resume.API.Models;
using ResumeAI.Resume.API.Repositories;

namespace ResumeAI.Resume.API.Services;

public interface IResumeService
{
    Task<IList<ResumeEntity>> GetAllByUserId(int userId);
    Task<ResumeEntity?> GetById(int resumeId);
    Task<ResumeEntity> CreateResume(ResumeEntity resume, string plan);
    Task<ResumeEntity> UpdateResume(ResumeEntity resume);
    Task DeleteResume(int resumeId);
    Task<ResumeEntity> DuplicateResume(int resumeId, string plan);
    Task PublishResume(int resumeId);
    Task UpdateAtsScore(int resumeId, int score);
    Task<IList<ResumeEntity>> GetPublicResumes();
}

public class ResumeService : IResumeService
{
    private readonly IResumeRepository _repo;
    private readonly ResumeDbContext _ctx;

    public ResumeService(IResumeRepository repo, ResumeDbContext ctx)
    {
        _repo = repo;
        _ctx  = ctx;
    }

    public async Task<IList<ResumeEntity>> GetAllByUserId(int userId) =>
        await _repo.GetAllByUserId(userId);

    public async Task<ResumeEntity?> GetById(int resumeId) =>
        await _repo.GetById(resumeId);

    public async Task<ResumeEntity> CreateResume(ResumeEntity resume, string plan)
    {
        if (plan == "FREE")
        {
            var count = await _repo.CountByUserId(resume.UserId);
            if (count >= 3)
                throw new InvalidOperationException("Free plan allows maximum 3 resumes");
        }
        return await _repo.Save(resume);
    }

    public async Task<ResumeEntity> UpdateResume(ResumeEntity resume) =>
        await _repo.Update(resume);

    public async Task DeleteResume(int resumeId)
    {
        var resume = await _repo.GetById(resumeId)
            ?? throw new KeyNotFoundException("Resume not found");
        await _repo.Delete(resume);
    }

    public async Task<ResumeEntity> DuplicateResume(int resumeId, string plan)
    {
        var existing = await _repo.GetById(resumeId)
            ?? throw new KeyNotFoundException("Resume not found");

        if (plan == "FREE")
        {
            var count = await _repo.CountByUserId(existing.UserId);
            if (count >= 3)
                throw new InvalidOperationException("Free plan allows maximum 3 resumes");
        }

        var src = await _ctx.Resumes
            .AsNoTracking()
            .Include(r => r.Sections)
            .FirstOrDefaultAsync(r => r.ResumeId == resumeId)
            ?? throw new KeyNotFoundException("Resume not found");

        src.ResumeId  = 0;
        src.Title     = $"{src.Title} (Copy)";
        src.CreatedAt = DateTime.UtcNow;
        src.UpdatedAt = DateTime.UtcNow;

        foreach (var section in src.Sections)
            section.SectionId = 0;

        _ctx.Resumes.Add(src);
        await _ctx.SaveChangesAsync();
        return src;
    }

    public async Task PublishResume(int resumeId)
    {
        var resume = await _repo.GetById(resumeId)
            ?? throw new KeyNotFoundException("Resume not found");
        resume.IsPublic = true;
        await _repo.Update(resume);
    }

    public async Task UpdateAtsScore(int resumeId, int score)
    {
        var resume = await _repo.GetById(resumeId)
            ?? throw new KeyNotFoundException("Resume not found");
        resume.AtsScore = score;
        await _repo.Update(resume);
    }

    public async Task<IList<ResumeEntity>> GetPublicResumes() =>
        await _ctx.Resumes
            .Where(r => r.IsPublic)
            .OrderByDescending(r => r.ViewCount)
            .ToListAsync();
}