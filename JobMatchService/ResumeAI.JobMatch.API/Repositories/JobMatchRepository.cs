using Microsoft.EntityFrameworkCore;
using ResumeAI.JobMatch.API.Data;
using ResumeAI.JobMatch.API.Models;

namespace ResumeAI.JobMatch.API.Repositories;

public interface IJobMatchRepository
{
    Task<IList<JobMatchEntity>> GetByUserId(int userId);
    Task<IList<JobMatchEntity>> GetByResumeId(int resumeId);
    Task<JobMatchEntity?> GetById(int jobMatchId);
    Task<JobMatchEntity> Save(JobMatchEntity jobMatch);
    Task<JobMatchEntity> Update(JobMatchEntity jobMatch);
    Task Delete(JobMatchEntity jobMatch);
}

public class JobMatchRepository : IJobMatchRepository
{
    private readonly JobMatchDbContext _ctx;

    public JobMatchRepository(JobMatchDbContext ctx) => _ctx = ctx;

    public async Task<IList<JobMatchEntity>> GetByUserId(int userId) =>
        await _ctx.JobMatches
            .Where(j => j.UserId == userId)
            .OrderByDescending(j => j.MatchScore)
            .ToListAsync();

    public async Task<IList<JobMatchEntity>> GetByResumeId(int resumeId) =>
        await _ctx.JobMatches
            .Where(j => j.ResumeId == resumeId)
            .OrderByDescending(j => j.MatchScore)
            .ToListAsync();

    public async Task<JobMatchEntity?> GetById(int jobMatchId) =>
        await _ctx.JobMatches.FindAsync(jobMatchId);

    public async Task<JobMatchEntity> Save(JobMatchEntity jobMatch)
    {
        _ctx.JobMatches.Add(jobMatch);
        await _ctx.SaveChangesAsync();
        return jobMatch;
    }

    public async Task<JobMatchEntity> Update(JobMatchEntity jobMatch)
    {
        jobMatch.UpdatedAt = DateTime.UtcNow;
        _ctx.JobMatches.Update(jobMatch);
        await _ctx.SaveChangesAsync();
        return jobMatch;
    }

    public async Task Delete(JobMatchEntity jobMatch)
    {
        _ctx.JobMatches.Remove(jobMatch);
        await _ctx.SaveChangesAsync();
    }
}