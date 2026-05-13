using Microsoft.EntityFrameworkCore;
using ResumeAI.Template.API.Data;
using ResumeAI.Template.API.Models;

namespace ResumeAI.Template.API.Repositories;

public interface ITemplateRepository
{
    Task<IList<ResumeTemplate>> GetAll();
    Task<ResumeTemplate?> GetById(int templateId);
    Task<IList<ResumeTemplate>> GetFree();
    Task<IList<ResumeTemplate>> GetPremium();
    Task<IList<ResumeTemplate>> GetByCategory(string category);
    Task<IList<ResumeTemplate>> GetPopular();
    Task<ResumeTemplate> Save(ResumeTemplate template);
    Task<ResumeTemplate> Update(ResumeTemplate template);
    Task IncrementUsage(int templateId);
    Task Deactivate(int templateId);
}

public class TemplateRepository : ITemplateRepository
{
    private readonly TemplateDbContext _ctx;

    public TemplateRepository(TemplateDbContext ctx) => _ctx = ctx;

    public async Task<IList<ResumeTemplate>> GetAll() =>
        await _ctx.ResumeTemplates
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync();

    public async Task<ResumeTemplate?> GetById(int templateId) =>
        await _ctx.ResumeTemplates.FindAsync(templateId);

    public async Task<IList<ResumeTemplate>> GetFree() =>
        await _ctx.ResumeTemplates
            .Where(t => t.IsActive && !t.IsPremium)
            .OrderBy(t => t.Name)
            .ToListAsync();

    public async Task<IList<ResumeTemplate>> GetPremium() =>
        await _ctx.ResumeTemplates
            .Where(t => t.IsActive && t.IsPremium)
            .OrderBy(t => t.Name)
            .ToListAsync();

    public async Task<IList<ResumeTemplate>> GetByCategory(string category) =>
        await _ctx.ResumeTemplates
            .Where(t => t.IsActive && t.Category == category)
            .OrderBy(t => t.Name)
            .ToListAsync();

    public async Task<IList<ResumeTemplate>> GetPopular() =>
        await _ctx.ResumeTemplates
            .Where(t => t.IsActive)
            .OrderByDescending(t => t.UsageCount)
            .Take(10)
            .ToListAsync();

    public async Task<ResumeTemplate> Save(ResumeTemplate template)
    {
        _ctx.ResumeTemplates.Add(template);
        await _ctx.SaveChangesAsync();
        return template;
    }

    public async Task<ResumeTemplate> Update(ResumeTemplate template)
    {
        _ctx.ResumeTemplates.Update(template);
        await _ctx.SaveChangesAsync();
        return template;
    }

    public async Task IncrementUsage(int templateId)
    {
        // Atomic increment — no read-modify-write race condition
        await _ctx.ResumeTemplates
            .Where(t => t.TemplateId == templateId)
            .ExecuteUpdateAsync(t =>
                t.SetProperty(x => x.UsageCount, x => x.UsageCount + 1));
    }

    public async Task Deactivate(int templateId)
    {
        await _ctx.ResumeTemplates
            .Where(t => t.TemplateId == templateId)
            .ExecuteUpdateAsync(t =>
                t.SetProperty(x => x.IsActive, false));
    }
}