using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using ResumeAI.JobMatch.API.Models;
using ResumeAI.JobMatch.API.Repositories;
using ResumeAI.JobMatch.API.Services;

namespace ResumeAI.JobMatch.Tests;

[TestFixture]
public class JobMatchServiceTests
{
    private Mock<IJobMatchRepository> _repoMock;
    private JobMatchService           _service;

    [SetUp]
    public void SetUp()
    {
        _repoMock = new Mock<IJobMatchRepository>();
        _service  = new JobMatchService(_repoMock.Object);
    }

    [Test]
    public async Task SaveJob_ShouldSanitizeDescription()
    {
        // Arrange
        var job = new JobMatchEntity 
        { 
            JobDescription = "<script>alert('xss')</script><p>Safe content</p>" 
        };
        _repoMock.Setup(r => r.Save(It.IsAny<JobMatchEntity>())).ReturnsAsync((JobMatchEntity j) => j);

        // Act
        var result = await _service.SaveJob(job);

        // Assert
        result.JobDescription.Should().NotContain("<script>");
        result.JobDescription.Should().Contain("<p>Safe content</p>");
    }

    [Test]
    public async Task UpdateStatus_WithValidStatus_ShouldUpdate()
    {
        // Arrange
        var job = new JobMatchEntity { JobMatchId = 1, Status = "SAVED" };
        _repoMock.Setup(r => r.GetById(1)).ReturnsAsync(job);
        _repoMock.Setup(r => r.Update(It.IsAny<JobMatchEntity>())).ReturnsAsync((JobMatchEntity j) => j);

        // Act
        var result = await _service.UpdateStatus(1, "APPLIED");

        // Assert
        result.Status.Should().Be("APPLIED");
        _repoMock.Verify(r => r.Update(It.IsAny<JobMatchEntity>()), Times.Once);
    }

    [Test]
    public async Task UpdateStatus_WithInvalidStatus_ShouldThrowInvalidOperation()
    {
        // Act
        var act = async () => await _service.UpdateStatus(1, "BOGUS");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Invalid status*");
    }

    [Test]
    public async Task CalculateMatchScore_ShouldReturnCorrectScore()
    {
        // Arrange
        var resumeContent = "Software Engineer C# .NET SQL Developer";
        var jobDescription = "Looking for a C# .NET Developer with SQL experience";

        // Act
        var score = await _service.CalculateMatchScore(resumeContent, jobDescription);

        // Assert
        score.Should().BeGreaterThan(0);
        score.Should().BeLessThanOrEqualTo(100);
    }

    [Test]
    public async Task CalculateMatchScore_NoOverlap_ShouldReturnZero()
    {
        // Arrange
        var resumeContent = "Chef Cooking Baking";
        var jobDescription = "Software Engineer C# .NET";

        // Act
        var score = await _service.CalculateMatchScore(resumeContent, jobDescription);

        // Assert
        score.Should().Be(0);
    }
}
