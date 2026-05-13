using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using ResumeAI.Resume.API.Data;
using ResumeAI.Resume.API.Models;
using ResumeAI.Resume.API.Repositories;
using ResumeAI.Resume.API.Services;

namespace ResumeAI.Resume.Tests;

[TestFixture]
public class ResumeServiceTests
{
    private Mock<IResumeRepository> _resumeRepoMock;
    private ResumeDbContext         _dbContext;
    private ResumeService           _resumeService;

    [SetUp]
    public void SetUp()
    {
        _resumeRepoMock = new Mock<IResumeRepository>();

        var options = new DbContextOptionsBuilder<ResumeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext     = new ResumeDbContext(options);
        _resumeService = new ResumeService(_resumeRepoMock.Object, _dbContext);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    // ── CreateResume Tests ────────────────────────────────────────────────────

    [Test]
    public async Task CreateResume_FreePlan_UnderLimit_ShouldCreate()
    {
        // Arrange
        var resume = new ResumeEntity { UserId = 1, Title = "My Resume" };

        _resumeRepoMock.Setup(r => r.CountByUserId(1)).ReturnsAsync(2); // under 3
        _resumeRepoMock.Setup(r => r.Save(It.IsAny<ResumeEntity>()))
            .ReturnsAsync((ResumeEntity r) => r);

        // Act
        var result = await _resumeService.CreateResume(resume, "FREE");

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("My Resume");
        _resumeRepoMock.Verify(r => r.Save(It.IsAny<ResumeEntity>()), Times.Once);
    }

    [Test]
    public async Task CreateResume_FreePlan_AtLimit_ShouldThrowInvalidOperation()
    {
        // Arrange
        var resume = new ResumeEntity { UserId = 1, Title = "New Resume" };

        _resumeRepoMock.Setup(r => r.CountByUserId(1)).ReturnsAsync(3); // at limit

        // Act
        var act = async () => await _resumeService.CreateResume(resume, "FREE");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Free plan allows maximum 3 resumes");
    }

    [Test]
    public async Task CreateResume_PremiumPlan_OverFreeLimit_ShouldCreate()
    {
        // Arrange
        var resume = new ResumeEntity { UserId = 1, Title = "My Resume" };

        _resumeRepoMock.Setup(r => r.Save(It.IsAny<ResumeEntity>()))
            .ReturnsAsync((ResumeEntity r) => r);

        // Act — premium has no limit
        var result = await _resumeService.CreateResume(resume, "PREMIUM");

        // Assert
        result.Should().NotBeNull();
        _resumeRepoMock.Verify(r => r.CountByUserId(It.IsAny<int>()), Times.Never);
        _resumeRepoMock.Verify(r => r.Save(It.IsAny<ResumeEntity>()), Times.Once);
    }

    // ── DeleteResume Tests ────────────────────────────────────────────────────

    [Test]
    public async Task DeleteResume_WithValidId_ShouldDelete()
    {
        // Arrange
        var resume = new ResumeEntity { ResumeId = 1, UserId = 1, Title = "My Resume" };

        _resumeRepoMock.Setup(r => r.GetById(1)).ReturnsAsync(resume);
        _resumeRepoMock.Setup(r => r.Delete(It.IsAny<ResumeEntity>())).Returns(Task.CompletedTask);

        // Act
        await _resumeService.DeleteResume(1);

        // Assert
        _resumeRepoMock.Verify(r => r.Delete(It.Is<ResumeEntity>(r =>
            r.ResumeId == 1)), Times.Once);
    }

    [Test]
    public async Task DeleteResume_WithInvalidId_ShouldThrowKeyNotFound()
    {
        // Arrange
        _resumeRepoMock.Setup(r => r.GetById(99)).ReturnsAsync((ResumeEntity?)null);

        // Act
        var act = async () => await _resumeService.DeleteResume(99);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Resume not found");
    }

    // ── UpdateAtsScore Tests ──────────────────────────────────────────────────

    [Test]
    public async Task UpdateAtsScore_WithValidId_ShouldUpdateScore()
    {
        // Arrange
        var resume = new ResumeEntity { ResumeId = 1, AtsScore = 0 };

        _resumeRepoMock.Setup(r => r.GetById(1)).ReturnsAsync(resume);
        _resumeRepoMock.Setup(r => r.Update(It.IsAny<ResumeEntity>()))
            .ReturnsAsync((ResumeEntity r) => r);

        // Act
        await _resumeService.UpdateAtsScore(1, 85);

        // Assert
        _resumeRepoMock.Verify(r => r.Update(It.Is<ResumeEntity>(r =>
            r.AtsScore == 85)), Times.Once);
    }

    [Test]
    public async Task UpdateAtsScore_WithInvalidId_ShouldThrowKeyNotFound()
    {
        // Arrange
        _resumeRepoMock.Setup(r => r.GetById(99)).ReturnsAsync((ResumeEntity?)null);

        // Act
        var act = async () => await _resumeService.UpdateAtsScore(99, 85);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Resume not found");
    }

    // ── PublishResume Tests ───────────────────────────────────────────────────

    [Test]
    public async Task PublishResume_WithValidId_ShouldSetIsPublicTrue()
    {
        // Arrange
        var resume = new ResumeEntity { ResumeId = 1, IsPublic = false };

        _resumeRepoMock.Setup(r => r.GetById(1)).ReturnsAsync(resume);
        _resumeRepoMock.Setup(r => r.Update(It.IsAny<ResumeEntity>()))
            .ReturnsAsync((ResumeEntity r) => r);

        // Act
        await _resumeService.PublishResume(1);

        // Assert
        _resumeRepoMock.Verify(r => r.Update(It.Is<ResumeEntity>(r =>
            r.IsPublic == true)), Times.Once);
    }

    [Test]
    public async Task PublishResume_WithInvalidId_ShouldThrowKeyNotFound()
    {
        // Arrange
        _resumeRepoMock.Setup(r => r.GetById(99)).ReturnsAsync((ResumeEntity?)null);

        // Act
        var act = async () => await _resumeService.PublishResume(99);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Resume not found");
    }

    // ── DuplicateResume Tests ─────────────────────────────────────────────────

    [Test]
    public async Task DuplicateResume_FreePlan_AtLimit_ShouldThrowInvalidOperation()
    {
        // Arrange
        var resume = new ResumeEntity
        {
            ResumeId = 1,
            UserId   = 1,
            Title    = "Original"
        };

        _resumeRepoMock.Setup(r => r.GetById(1)).ReturnsAsync(resume);
        _resumeRepoMock.Setup(r => r.CountByUserId(1)).ReturnsAsync(3);

        // Act
        var act = async () => await _resumeService.DuplicateResume(1, "FREE");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Free plan allows maximum 3 resumes");
    }

    [Test]
    public async Task DuplicateResume_WithInvalidId_ShouldThrowKeyNotFound()
    {
        // Arrange
        _resumeRepoMock.Setup(r => r.GetById(99)).ReturnsAsync((ResumeEntity?)null);

        // Act
        var act = async () => await _resumeService.DuplicateResume(99, "FREE");

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Resume not found");
    }

    [Test]
    public async Task DuplicateResume_PremiumPlan_ShouldCreateCopyWithSuffix()
    {
        // Arrange
        var original = new ResumeEntity
        {
            ResumeId = 1,
            UserId   = 1,
            Title    = "My Resume",
            Status   = "DRAFT",
            Language = "en",
            Sections = new List<ResumeSection>
            {
                new ResumeSection
                {
                    SectionId   = 1,
                    SectionType = "SUMMARY",
                    Title       = "Summary",
                    Content     = "Test content",
                    DisplayOrder = 0
                }
            }
        };

        // Add to InMemory DB
        _dbContext.Resumes.Add(original);
        await _dbContext.SaveChangesAsync();

        _resumeRepoMock.Setup(r => r.GetById(1)).ReturnsAsync(original);

        // Act
        var result = await _resumeService.DuplicateResume(1, "PREMIUM");

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("My Resume (Copy)");
        result.ResumeId.Should().NotBe(1);
        result.Title.Should().Contain("(Copy)");
    }

    // ── GetAllByUserId Tests ──────────────────────────────────────────────────

    [Test]
    public async Task GetAllByUserId_ShouldReturnUserResumes()
    {
        // Arrange
        var resumes = new List<ResumeEntity>
        {
            new ResumeEntity { ResumeId = 1, UserId = 1, Title = "Resume 1" },
            new ResumeEntity { ResumeId = 2, UserId = 1, Title = "Resume 2" }
        };

        _resumeRepoMock.Setup(r => r.GetAllByUserId(1)).ReturnsAsync(resumes);

        // Act
        var result = await _resumeService.GetAllByUserId(1);

        // Assert
        result.Should().HaveCount(2);
        result.All(r => r.UserId == 1).Should().BeTrue();
    }

    [Test]
    public async Task GetPublicResumes_ShouldReturnOnlyPublicResumesOrderedByViews()
    {
        _dbContext.Resumes.AddRange(
            new ResumeEntity { ResumeId = 10, IsPublic = true,  ViewCount = 100, Title = "Popular" },
            new ResumeEntity { ResumeId = 11, IsPublic = false, ViewCount = 500, Title = "Private" },
            new ResumeEntity { ResumeId = 12, IsPublic = true,  ViewCount = 200, Title = "Most Popular" }
        );
        await _dbContext.SaveChangesAsync();

        var result = await _resumeService.GetPublicResumes();

        result.Should().HaveCount(2);
        result[0].ResumeId.Should().Be(12);
        result[1].ResumeId.Should().Be(10);
        result.All(r => r.IsPublic).Should().BeTrue();
    }
}
