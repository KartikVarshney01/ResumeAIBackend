using Microsoft.EntityFrameworkCore;
using ResumeAI.Export.API.Data;
using ResumeAI.Export.API.Models;

namespace ResumeAI.Export.API.Repositories;

public interface IExportJobRepository
{
    Task<ExportJob> Save(ExportJob job);
    Task<ExportJob> Update(ExportJob job);
    Task<ExportJob?> GetById(int jobId);
    Task<IList<ExportJob>> GetByUserId(int userId);
    Task<IList<ExportJob>> GetByResumeId(int resumeId);
    Task<IList<ExportJob>> GetPendingJobs();
}

public class ExportJobRepository : IExportJobRepository
{
    private readonly ExportDbContext _ctx;

    public ExportJobRepository(ExportDbContext ctx) => _ctx = ctx;

    public async Task<ExportJob> Save(ExportJob job)
    {
        _ctx.ExportJobs.Add(job);
        await _ctx.SaveChangesAsync();
        return job;
    }

    public async Task<ExportJob> Update(ExportJob job)
    {
        _ctx.ExportJobs.Update(job);
        await _ctx.SaveChangesAsync();
        return job;
    }

    public async Task<ExportJob?> GetById(int jobId) =>
        await _ctx.ExportJobs.FindAsync(jobId);

    public async Task<IList<ExportJob>> GetByUserId(int userId) =>
        await _ctx.ExportJobs
            .Where(j => j.UserId == userId)
            .OrderByDescending(j => j.RequestedAt)
            .ToListAsync();

    public async Task<IList<ExportJob>> GetByResumeId(int resumeId) =>
        await _ctx.ExportJobs
            .Where(j => j.ResumeId == resumeId)
            .OrderByDescending(j => j.RequestedAt)
            .ToListAsync();

    public async Task<IList<ExportJob>> GetPendingJobs() =>
        await _ctx.ExportJobs
            .Where(j => j.Status == "PENDING")
            .OrderBy(j => j.RequestedAt)
            .ToListAsync();
}
