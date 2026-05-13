using Microsoft.Extensions.Caching.Memory;
using ResumeAI.Template.API.Models;
using ResumeAI.Template.API.Repositories;

namespace ResumeAI.Template.API.Services;

public interface ITemplateService
{
    Task<ResumeTemplate> CreateTemplate(ResumeTemplate template);
    Task<ResumeTemplate?> GetTemplateById(int id);
    Task<IList<ResumeTemplate>> GetAllTemplates();
    Task<IList<ResumeTemplate>> GetFreeTemplates();
    Task<IList<ResumeTemplate>> GetPremiumTemplates();
    Task<IList<ResumeTemplate>> GetByCategory(string category);
    Task<ResumeTemplate> UpdateTemplate(int id, ResumeTemplate template);
    Task DeactivateTemplate(int id);
    Task IncrementUsage(int id);
    Task<IList<ResumeTemplate>> GetPopularTemplates();
}

public class TemplateService : ITemplateService
{
    private readonly ITemplateRepository _repo;
    private readonly IMemoryCache _cache;

    private const string FREE_CACHE_KEY    = "templates_free";
    private const string POPULAR_CACHE_KEY = "templates_popular";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(2);

    public TemplateService(ITemplateRepository repo, IMemoryCache cache)
    {
        _repo  = repo;
        _cache = cache;
    }

    public async Task<ResumeTemplate> CreateTemplate(ResumeTemplate template) =>
        await _repo.Save(template);

    public async Task<ResumeTemplate?> GetTemplateById(int id) =>
        await _repo.GetById(id);

    public async Task<IList<ResumeTemplate>> GetAllTemplates() =>
        await _repo.GetAll();

    public async Task<IList<ResumeTemplate>> GetFreeTemplates()
    {
        // Check cache first
        if (_cache.TryGetValue(FREE_CACHE_KEY, out IList<ResumeTemplate>? cached))
            return cached!;

        var templates = await _repo.GetFree();

        _cache.Set(FREE_CACHE_KEY, templates, CacheTtl);
        return templates;
    }

    public async Task<IList<ResumeTemplate>> GetPremiumTemplates() =>
        await _repo.GetPremium();

    public async Task<IList<ResumeTemplate>> GetByCategory(string category) =>
        await _repo.GetByCategory(category);

    public async Task<ResumeTemplate> UpdateTemplate(int id, ResumeTemplate template)
    {
        var existing = await _repo.GetById(id)
            ?? throw new KeyNotFoundException("Template not found");

        existing.Name        = template.Name;
        existing.Description = template.Description;
        existing.HtmlLayout  = template.HtmlLayout;
        existing.CssStyles   = template.CssStyles;
        existing.Category    = template.Category;
        existing.IsPremium   = template.IsPremium;
        existing.ThumbnailUrl = template.ThumbnailUrl;

        // Invalidate cache
        _cache.Remove(FREE_CACHE_KEY);
        _cache.Remove(POPULAR_CACHE_KEY);

        return await _repo.Update(existing);
    }

    public async Task DeactivateTemplate(int id)
    {
        var existing = await _repo.GetById(id)
            ?? throw new KeyNotFoundException("Template not found");

        // Soft delete — never hard delete
        await _repo.Deactivate(id);

        // Invalidate cache
        _cache.Remove(FREE_CACHE_KEY);
        _cache.Remove(POPULAR_CACHE_KEY);
    }

    public async Task IncrementUsage(int id)
    {
        var existing = await _repo.GetById(id)
            ?? throw new KeyNotFoundException("Template not found");

        // Atomic increment via ExecuteUpdateAsync
        await _repo.IncrementUsage(id);

        // Invalidate popular cache
        _cache.Remove(POPULAR_CACHE_KEY);
    }

    public async Task<IList<ResumeTemplate>> GetPopularTemplates()
    {
        // Check cache first
        if (_cache.TryGetValue(POPULAR_CACHE_KEY, out IList<ResumeTemplate>? cached))
            return cached!;

        var templates = await _repo.GetPopular();

        _cache.Set(POPULAR_CACHE_KEY, templates, CacheTtl);
        return templates;
    }
}