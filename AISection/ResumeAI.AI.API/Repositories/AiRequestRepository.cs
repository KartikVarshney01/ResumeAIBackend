using Microsoft.EntityFrameworkCore;
using ResumeAI.AI.API.Data;
using ResumeAI.AI.API.Models;

namespace ResumeAI.AI.API.Repositories;

public interface IAiRequestRepository
{
    Task<AiRequest> Save(AiRequest request);
    Task<AiRequest> Update(AiRequest request);
    Task<AiRequest?> GetById(int requestId);
    Task<IList<AiRequest>> GetByUserId(int userId);
    Task<IList<AiRequest>> GetByResumeId(int resumeId);
    Task<int> GetMonthlyUsageCount(int userId);
}

public class AiRequestRepository : IAiRequestRepository
{
    private readonly AiDbContext _ctx;

    public AiRequestRepository(AiDbContext ctx) => _ctx = ctx;

    public async Task<AiRequest> Save(AiRequest request)
    {
        _ctx.AiRequests.Add(request);
        await _ctx.SaveChangesAsync();
        return request;
    }

    public async Task<AiRequest> Update(AiRequest request)
    {
        _ctx.AiRequests.Update(request);
        await _ctx.SaveChangesAsync();
        return request;
    }

    public async Task<AiRequest?> GetById(int requestId) =>
        await _ctx.AiRequests.FindAsync(requestId);

    public async Task<IList<AiRequest>> GetByUserId(int userId) =>
        await _ctx.AiRequests
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();

    public async Task<IList<AiRequest>> GetByResumeId(int resumeId) =>
        await _ctx.AiRequests
            .Where(r => r.ResumeId == resumeId)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();

    public async Task<int> GetMonthlyUsageCount(int userId)
    {
        var startOfMonth = new DateTime(
            DateTime.UtcNow.Year,
            DateTime.UtcNow.Month, 1);

        return await _ctx.AiRequests
            .CountAsync(r =>
                r.UserId == userId &&
                r.RequestedAt >= startOfMonth &&
                r.Status == "COMPLETED");
    }
}