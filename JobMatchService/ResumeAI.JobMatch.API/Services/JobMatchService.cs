using Ganss.Xss;
using ResumeAI.JobMatch.API.Models;
using ResumeAI.JobMatch.API.Repositories;

namespace ResumeAI.JobMatch.API.Services;

public interface IJobMatchService
{
    Task<IList<JobMatchEntity>> GetByUserId(int userId);
    Task<IList<JobMatchEntity>> GetByResumeId(int resumeId);
    Task<JobMatchEntity?> GetById(int jobMatchId);
    Task<JobMatchEntity> SaveJob(JobMatchEntity jobMatch);
    Task<JobMatchEntity> UpdateJob(JobMatchEntity jobMatch);
    Task DeleteJob(int jobMatchId);
    Task<JobMatchEntity> UpdateStatus(int jobMatchId, string status);
    Task<int> CalculateMatchScore(string resumeContent, string jobDescription);
}

public class JobMatchService : IJobMatchService
{
    private readonly IJobMatchRepository _repo;
    private readonly HtmlSanitizer _sanitizer;

    public JobMatchService(IJobMatchRepository repo)
    {
        _repo      = repo;
        _sanitizer = new HtmlSanitizer();
    }

    public async Task<IList<JobMatchEntity>> GetByUserId(int userId) =>
        await _repo.GetByUserId(userId);

    public async Task<IList<JobMatchEntity>> GetByResumeId(int resumeId) =>
        await _repo.GetByResumeId(resumeId);

    public async Task<JobMatchEntity?> GetById(int jobMatchId) =>
        await _repo.GetById(jobMatchId);

    public async Task<JobMatchEntity> SaveJob(JobMatchEntity jobMatch)
    {
        if (jobMatch.JobDescription is not null)
            jobMatch.JobDescription = _sanitizer.Sanitize(jobMatch.JobDescription);

        return await _repo.Save(jobMatch);
    }

    public async Task<JobMatchEntity> UpdateJob(JobMatchEntity jobMatch)
    {
        if (jobMatch.JobDescription is not null)
            jobMatch.JobDescription = _sanitizer.Sanitize(jobMatch.JobDescription);

        return await _repo.Update(jobMatch);
    }

    public async Task DeleteJob(int jobMatchId)
    {
        var job = await _repo.GetById(jobMatchId)
            ?? throw new KeyNotFoundException("Job match not found");
        await _repo.Delete(job);
    }

    public async Task<JobMatchEntity> UpdateStatus(int jobMatchId, string status)
    {
        var validStatuses = new[] { "SAVED", "APPLIED", "INTERVIEWING", "OFFERED", "REJECTED" };
        if (!validStatuses.Contains(status))
            throw new InvalidOperationException(
                $"Invalid status. Must be one of: {string.Join(", ", validStatuses)}");

        var job = await _repo.GetById(jobMatchId)
            ?? throw new KeyNotFoundException("Job match not found");

        job.Status = status;
        return await _repo.Update(job);
    }

    public Task<int> CalculateMatchScore(string resumeContent, string jobDescription)
    {
        var resumeWords = resumeContent.ToLower()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet();

        var jobWords = jobDescription.ToLower()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3)
            .Distinct()
            .ToList();

        if (jobWords.Count == 0) return Task.FromResult(0);

        var matchCount = jobWords.Count(w => resumeWords.Contains(w));
        var score      = (int)Math.Min(100, (matchCount * 100.0 / jobWords.Count) * 2);

        return Task.FromResult(score);
    }
}