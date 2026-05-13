using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using ResumeAI.Export.API.Data;
using ResumeAI.Export.API.Models;
using ResumeAI.Export.API.Repositories;
using ResumeAI.Export.API.Rendering;
using ResumeAI.Export.API.Services;

namespace ResumeAI.Export.Tests;

[TestFixture]
public class ExportServiceTests
{
    private Mock<IExportJobRepository> _repoMock;
    private Mock<IPdfRenderer>         _pdfRendererMock;
    private Mock<IDocxRenderer>        _docxRendererMock;
    private Mock<IConfiguration>       _configMock;
    private Mock<MassTransit.IPublishEndpoint> _publishMock;
    private ExportDbContext            _dbContext;
    private ExportService              _service;

    [SetUp]
    public void SetUp()
    {
        _repoMock         = new Mock<IExportJobRepository>();
        _pdfRendererMock  = new Mock<IPdfRenderer>();
        _docxRendererMock = new Mock<IDocxRenderer>();
        _configMock       = new Mock<IConfiguration>();
        _publishMock      = new Mock<MassTransit.IPublishEndpoint>();

        // Setup Azure storage to empty so it falls back to local
        _configMock.Setup(c => c["AzureStorage:ConnectionString"])
            .Returns("YOUR_AZURE_STORAGE_CONNECTION_STRING");
        _configMock.Setup(c => c["AzureStorage:ContainerName"])
            .Returns("exports");

        var options = new DbContextOptionsBuilder<ExportDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ExportDbContext(options);

        _service = new ExportService(
            _repoMock.Object,
            _pdfRendererMock.Object,
            _docxRendererMock.Object,
            _configMock.Object,
            _publishMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    // ── RequestExport Tests ───────────────────────────────────────────────────

    [Test]
    public async Task RequestExport_WithPdfFormat_ShouldCreatePendingJob()
    {
        // Arrange
        var job = new ExportJob
        {
            ExportJobId = 1,
            UserId      = 1,
            ResumeId    = 1,
            Format      = "PDF",
            Status      = "PENDING"
        };

        _repoMock.Setup(r => r.Save(It.IsAny<ExportJob>()))
            .ReturnsAsync((ExportJob j) => j);

        // Act
        var result = await _service.RequestExport(1, 1, "PDF");

        // Assert
        result.Should().NotBeNull();
        result.Format.Should().Be("PDF");
        result.Status.Should().Be("PENDING");
        _repoMock.Verify(r => r.Save(It.Is<ExportJob>(j =>
            j.Format == "PDF" && j.Status == "PENDING")), Times.Once);
    }

    [Test]
    public async Task RequestExport_WithDocxFormat_ShouldCreatePendingJob()
    {
        // Arrange
        _repoMock.Setup(r => r.Save(It.IsAny<ExportJob>()))
            .ReturnsAsync((ExportJob j) => j);

        // Act
        var result = await _service.RequestExport(1, 1, "DOCX");

        // Assert
        result.Format.Should().Be("DOCX");
        result.Status.Should().Be("PENDING");
    }

    [Test]
    public async Task RequestExport_WithInvalidFormat_ShouldThrowInvalidOperation()
    {
        // Act
        var act = async () => await _service.RequestExport(1, 1, "INVALID");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Format must be PDF or DOCX");
    }

    [Test]
    public async Task RequestExport_ShouldReturnCorrectFormat()
    {
        // Arrange
        _repoMock.Setup(r => r.Save(It.IsAny<ExportJob>()))
            .ReturnsAsync((ExportJob j) => j);

        // Act
        var result = await _service.RequestExport(1, 1, "PDF");

        // Assert
        result.Format.Should().Be("PDF");
    }

    // ── ProcessExport Tests ───────────────────────────────────────────────────

    [Test]
    public async Task ProcessExport_WithPdfFormat_ShouldUsePdfRenderer()
    {
        // Arrange
        var job = new ExportJob
        {
            ExportJobId = 1,
            UserId      = 1,
            ResumeId    = 1,
            Format      = "PDF",
            Status      = "PENDING"
        };

        var resumeData = new ResumeData
        {
            FullName = "Test User",
            Email    = "test@example.com",
            Sections = new List<SectionData>()
        };

        _repoMock.Setup(r => r.GetById(1)).ReturnsAsync(job);
        _repoMock.Setup(r => r.Update(It.IsAny<ExportJob>()))
            .ReturnsAsync((ExportJob j) => j);
        _pdfRendererMock.Setup(r => r.Render(It.IsAny<ResumeData>()))
            .Returns(new byte[] { 1, 2, 3, 4, 5 });

        // Act
        await _service.ProcessExport(1, resumeData);

        // Assert
        _pdfRendererMock.Verify(r => r.Render(It.IsAny<ResumeData>()), Times.Once);
        _docxRendererMock.Verify(r => r.Render(It.IsAny<ResumeData>()), Times.Never);
    }

    [Test]
    public async Task ProcessExport_WithDocxFormat_ShouldUseDocxRenderer()
    {
        // Arrange
        var job = new ExportJob
        {
            ExportJobId = 1,
            UserId      = 1,
            ResumeId    = 1,
            Format      = "DOCX",
            Status      = "PENDING"
        };

        var resumeData = new ResumeData
        {
            FullName = "Test User",
            Email    = "test@example.com",
            Sections = new List<SectionData>()
        };

        _repoMock.Setup(r => r.GetById(1)).ReturnsAsync(job);
        _repoMock.Setup(r => r.Update(It.IsAny<ExportJob>()))
            .ReturnsAsync((ExportJob j) => j);
        _docxRendererMock.Setup(r => r.Render(It.IsAny<ResumeData>()))
            .Returns(new byte[] { 1, 2, 3 });

        // Act
        await _service.ProcessExport(1, resumeData);

        // Assert
        _docxRendererMock.Verify(r => r.Render(It.IsAny<ResumeData>()), Times.Once);
        _pdfRendererMock.Verify(r => r.Render(It.IsAny<ResumeData>()), Times.Never);
    }

    [Test]
    public async Task ProcessExport_WhenRendererSucceeds_ShouldMarkJobCompleted()
    {
        // Arrange
        var job = new ExportJob
        {
            ExportJobId = 1,
            UserId      = 1,
            ResumeId    = 1,
            Format      = "PDF",
            Status      = "PENDING"
        };

        var resumeData = new ResumeData
        {
            FullName = "Test User",
            Email    = "test@example.com",
            Sections = new List<SectionData>()
        };

        _repoMock.Setup(r => r.GetById(1)).ReturnsAsync(job);
        _repoMock.Setup(r => r.Update(It.IsAny<ExportJob>()))
            .ReturnsAsync((ExportJob j) => j);
        _pdfRendererMock.Setup(r => r.Render(It.IsAny<ResumeData>()))
            .Returns(new byte[] { 1, 2, 3, 4, 5 });

        // Act
        await _service.ProcessExport(1, resumeData);

        // Assert
        _repoMock.Verify(r => r.Update(It.Is<ExportJob>(j =>
            j.Status == "COMPLETED" &&
            j.CompletedAt != null &&
            j.FileSizeBytes == 5)), Times.AtLeastOnce);
    }

    [Test]
    public async Task ProcessExport_WhenRendererFails_ShouldMarkJobFailed()
    {
        // Arrange
        var job = new ExportJob
        {
            ExportJobId = 1,
            UserId      = 1,
            ResumeId    = 1,
            Format      = "PDF",
            Status      = "PENDING"
        };

        var resumeData = new ResumeData
        {
            FullName = "Test User",
            Email    = "test@example.com",
            Sections = new List<SectionData>()
        };

        _repoMock.Setup(r => r.GetById(1)).ReturnsAsync(job);
        _repoMock.Setup(r => r.Update(It.IsAny<ExportJob>()))
            .ReturnsAsync((ExportJob j) => j);
        _pdfRendererMock.Setup(r => r.Render(It.IsAny<ResumeData>()))
            .Throws(new Exception("Render failed"));

        // Act
        await _service.ProcessExport(1, resumeData);

        // Assert
        _repoMock.Verify(r => r.Update(It.Is<ExportJob>(j =>
            j.Status == "FAILED" &&
            j.ErrorMessage == "Render failed")), Times.AtLeastOnce);
    }

    [Test]
    public async Task ProcessExport_WithInvalidJobId_ShouldThrowKeyNotFound()
    {
        // Arrange
        _repoMock.Setup(r => r.GetById(99)).ReturnsAsync((ExportJob?)null);

        // Act
        var act = async () => await _service.ProcessExport(99, new ResumeData());

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Export job not found");
    }

    // ── GetJobById Tests ──────────────────────────────────────────────────────

    [Test]
    public async Task GetJobById_WithValidId_ShouldReturnJob()
    {
        // Arrange
        var job = new ExportJob { ExportJobId = 1, Format = "PDF", Status = "COMPLETED" };
        _repoMock.Setup(r => r.GetById(1)).ReturnsAsync(job);

        // Act
        var result = await _service.GetJobById(1);

        // Assert
        result.Should().NotBeNull();
        result!.Format.Should().Be("PDF");
        result.Status.Should().Be("COMPLETED");
    }

    [Test]
    public async Task GetJobById_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        _repoMock.Setup(r => r.GetById(99)).ReturnsAsync((ExportJob?)null);

        // Act
        var result = await _service.GetJobById(99);

        // Assert
        result.Should().BeNull();
    }

    // ── GetUserExports Tests ──────────────────────────────────────────────────

    [Test]
    public async Task GetUserExports_ShouldReturnOnlyUserJobs()
    {
        // Arrange
        var jobs = new List<ExportJob>
        {
            new ExportJob { ExportJobId = 1, UserId = 1, Format = "PDF" },
            new ExportJob { ExportJobId = 2, UserId = 1, Format = "DOCX" }
        };

        _repoMock.Setup(r => r.GetByUserId(1)).ReturnsAsync(jobs);

        // Act
        var result = await _service.GetUserExports(1);

        // Assert
        result.Should().HaveCount(2);
        result.All(j => j.UserId == 1).Should().BeTrue();
    }

    [Test]
    public async Task GetResumeExports_ShouldReturnOnlyResumeJobs()
    {
        var jobs = new List<ExportJob>
        {
            new ExportJob { ExportJobId = 1, ResumeId = 10, Format = "PDF" },
            new ExportJob { ExportJobId = 2, ResumeId = 10, Format = "DOCX" }
        };
        _repoMock.Setup(r => r.GetByUserId(1)).ReturnsAsync(new List<ExportJob>()); // Dummy setup for unused method
        _repoMock.Setup(r => r.GetByResumeId(10)).ReturnsAsync(jobs);
        var result = await _service.GetResumeExports(10);
        result.Should().HaveCount(2);
        result.All(j => j.ResumeId == 10).Should().BeTrue();
    }
}
