using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using NUnit.Framework;
using ResumeAI.Template.API.Data;
using ResumeAI.Template.API.Models;
using ResumeAI.Template.API.Repositories;
using ResumeAI.Template.API.Services;

namespace ResumeAI.Template.Tests;

[TestFixture]
public class TemplateServiceTests
{
    private Mock<ITemplateRepository> _repoMock;
    private IMemoryCache              _cache;
    private TemplateService           _service;

    [SetUp]
    public void SetUp()
    {
        _repoMock = new Mock<ITemplateRepository>();
        _cache    = new MemoryCache(new MemoryCacheOptions());
        _service  = new TemplateService(_repoMock.Object, _cache);
    }

    [TearDown]
    public void TearDown()
    {
        _cache.Dispose();
    }

    // ── CreateTemplate Tests ──────────────────────────────────────────────────

    [Test]
    public async Task CreateTemplate_ShouldSaveAndReturn()
    {
        // Arrange
        var template = new ResumeTemplate
        {
            Name      = "Modern Pro",
            Category  = "MODERN",
            IsPremium = false
        };

        _repoMock.Setup(r => r.Save(It.IsAny<ResumeTemplate>()))
            .ReturnsAsync((ResumeTemplate t) => t);

        // Act
        var result = await _service.CreateTemplate(template);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Modern Pro");
        _repoMock.Verify(r => r.Save(It.IsAny<ResumeTemplate>()), Times.Once);
    }

    // ── GetFreeTemplates Cache Tests ──────────────────────────────────────────

    [Test]
    public async Task GetFreeTemplates_FirstCall_ShouldHitRepository()
    {
        // Arrange
        var templates = new List<ResumeTemplate>
        {
            new ResumeTemplate { TemplateId = 1, Name = "Basic", IsPremium = false },
            new ResumeTemplate { TemplateId = 2, Name = "Clean", IsPremium = false }
        };

        _repoMock.Setup(r => r.GetFree()).ReturnsAsync(templates);

        // Act
        var result = await _service.GetFreeTemplates();

        // Assert
        result.Should().HaveCount(2);
        _repoMock.Verify(r => r.GetFree(), Times.Once);
    }

    [Test]
    public async Task GetFreeTemplates_SecondCall_ShouldUseCacheNotRepository()
    {
        // Arrange
        var templates = new List<ResumeTemplate>
        {
            new ResumeTemplate { TemplateId = 1, Name = "Basic", IsPremium = false }
        };

        _repoMock.Setup(r => r.GetFree()).ReturnsAsync(templates);

        // Act — call twice
        await _service.GetFreeTemplates();
        await _service.GetFreeTemplates();

        // Assert — repo called only once, second was from cache
        _repoMock.Verify(r => r.GetFree(), Times.Once);
    }

    [Test]
    public async Task GetPopularTemplates_SecondCall_ShouldUseCacheNotRepository()
    {
        // Arrange
        var templates = new List<ResumeTemplate>
        {
            new ResumeTemplate { TemplateId = 1, Name = "Popular", UsageCount = 100 }
        };

        _repoMock.Setup(r => r.GetPopular()).ReturnsAsync(templates);

        // Act — call twice
        await _service.GetPopularTemplates();
        await _service.GetPopularTemplates();

        // Assert
        _repoMock.Verify(r => r.GetPopular(), Times.Once);
    }

    // ── UpdateTemplate Tests ──────────────────────────────────────────────────

    [Test]
    public async Task UpdateTemplate_WithValidId_ShouldUpdateAndInvalidateCache()
    {
        // Arrange
        var existing = new ResumeTemplate
        {
            TemplateId = 1,
            Name       = "Old Name",
            Category   = "PROFESSIONAL",
            IsPremium  = false,
            HtmlLayout = "<div></div>",
            CssStyles  = "body{}"
        };

        var updated = new ResumeTemplate
        {
            Name      = "New Name",
            Category  = "MODERN",
            IsPremium = true,
            HtmlLayout = "<div>new</div>",
            CssStyles  = "body{color:red}"
        };

        _repoMock.Setup(r => r.GetById(1)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.Update(It.IsAny<ResumeTemplate>()))
            .ReturnsAsync((ResumeTemplate t) => t);

        // Pre-populate cache
        _cache.Set("templates_free", new List<ResumeTemplate> { existing });

        // Act
        var result = await _service.UpdateTemplate(1, updated);

        // Assert
        result.Name.Should().Be("New Name");
        result.Category.Should().Be("MODERN");
        result.IsPremium.Should().BeTrue();
        _cache.TryGetValue("templates_free", out _).Should().BeFalse(); // cache invalidated
    }

    [Test]
    public async Task UpdateTemplate_WithInvalidId_ShouldThrowKeyNotFound()
    {
        // Arrange
        _repoMock.Setup(r => r.GetById(99)).ReturnsAsync((ResumeTemplate?)null);

        // Act
        var act = async () => await _service.UpdateTemplate(99, new ResumeTemplate());

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Template not found");
    }

    // ── DeactivateTemplate Tests ──────────────────────────────────────────────

    [Test]
    public async Task DeactivateTemplate_WithValidId_ShouldDeactivateAndInvalidateCache()
    {
        // Arrange
        var template = new ResumeTemplate { TemplateId = 1, IsActive = true };

        _repoMock.Setup(r => r.GetById(1)).ReturnsAsync(template);
        _repoMock.Setup(r => r.Deactivate(1)).Returns(Task.CompletedTask);

        // Pre-populate cache
        _cache.Set("templates_free",    new List<ResumeTemplate> { template });
        _cache.Set("templates_popular", new List<ResumeTemplate> { template });

        // Act
        await _service.DeactivateTemplate(1);

        // Assert
        _repoMock.Verify(r => r.Deactivate(1), Times.Once);
        _cache.TryGetValue("templates_free",    out _).Should().BeFalse();
        _cache.TryGetValue("templates_popular", out _).Should().BeFalse();
    }

    [Test]
    public async Task DeactivateTemplate_WithInvalidId_ShouldThrowKeyNotFound()
    {
        // Arrange
        _repoMock.Setup(r => r.GetById(99)).ReturnsAsync((ResumeTemplate?)null);

        // Act
        var act = async () => await _service.DeactivateTemplate(99);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Template not found");
    }

    // ── IncrementUsage Tests ──────────────────────────────────────────────────

    [Test]
    public async Task IncrementUsage_WithValidId_ShouldIncrementAndInvalidatePopularCache()
    {
        // Arrange
        var template = new ResumeTemplate { TemplateId = 1, UsageCount = 5 };

        _repoMock.Setup(r => r.GetById(1)).ReturnsAsync(template);
        _repoMock.Setup(r => r.IncrementUsage(1)).Returns(Task.CompletedTask);

        // Pre-populate cache
        _cache.Set("templates_popular", new List<ResumeTemplate> { template });

        // Act
        await _service.IncrementUsage(1);

        // Assert
        _repoMock.Verify(r => r.IncrementUsage(1), Times.Once);
        _cache.TryGetValue("templates_popular", out _).Should().BeFalse();
    }

    [Test]
    public async Task IncrementUsage_WithInvalidId_ShouldThrowKeyNotFound()
    {
        // Arrange
        _repoMock.Setup(r => r.GetById(99)).ReturnsAsync((ResumeTemplate?)null);

        // Act
        var act = async () => await _service.IncrementUsage(99);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Template not found");
    }

    // ── GetTemplateById Tests ─────────────────────────────────────────────────

    [Test]
    public async Task GetTemplateById_WithValidId_ShouldReturnTemplate()
    {
        // Arrange
        var template = new ResumeTemplate { TemplateId = 1, Name = "Modern" };
        _repoMock.Setup(r => r.GetById(1)).ReturnsAsync(template);

        // Act
        var result = await _service.GetTemplateById(1);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Modern");
    }

    [Test]
    public async Task GetTemplateById_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        _repoMock.Setup(r => r.GetById(99)).ReturnsAsync((ResumeTemplate?)null);

        // Act
        var result = await _service.GetTemplateById(99);

        // Assert
        result.Should().BeNull();
    }

    // ── GetByCategory Tests ───────────────────────────────────────────────────

    [Test]
    public async Task GetByCategory_ShouldReturnOnlyCategoryTemplates()
    {
        // Arrange
        var templates = new List<ResumeTemplate>
        {
            new ResumeTemplate { TemplateId = 1, Name = "Modern1", Category = "MODERN" },
            new ResumeTemplate { TemplateId = 2, Name = "Modern2", Category = "MODERN" }
        };

        _repoMock.Setup(r => r.GetByCategory("MODERN")).ReturnsAsync(templates);

        // Act
        var result = await _service.GetByCategory("MODERN");

        // Assert
        result.Should().HaveCount(2);
        result.All(t => t.Category == "MODERN").Should().BeTrue();
    }
}