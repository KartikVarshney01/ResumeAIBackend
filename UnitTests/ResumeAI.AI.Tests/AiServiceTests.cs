using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ResumeAI.AI.API.Data;
using ResumeAI.AI.API.Models;
using ResumeAI.AI.API.Repositories;
using ResumeAI.AI.API.Services;
using StackExchange.Redis;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace ResumeAI.AI.Tests;

[TestFixture]
public class AiServiceTests
{
    private Mock<IAiRequestRepository>   _repoMock;
    private Mock<IConfiguration>         _configMock;
    private Mock<IConnectionMultiplexer> _redisMock;
    private Mock<IDatabase>              _redisDatabaseMock;
    private Mock<ILogger<AiService>>     _loggerMock;
    private Mock<IHttpClientFactory>     _httpClientFactoryMock;
    private AiDbContext                  _dbContext;

    [SetUp]
    public void SetUp()
    {
        _repoMock          = new Mock<IAiRequestRepository>();
        _configMock        = new Mock<IConfiguration>();
        _redisMock         = new Mock<IConnectionMultiplexer>();
        _redisDatabaseMock = new Mock<IDatabase>();
        _loggerMock         = new Mock<ILogger<AiService>>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();

        // Setup config
        _configMock.Setup(c => c["OpenAI:ApiKey"]).Returns("test-openai-key");
        _configMock.Setup(c => c["Anthropic:ApiKey"]).Returns("test-anthropic-key");

        // Setup Redis
        _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_redisDatabaseMock.Object);

        var options = new DbContextOptionsBuilder<AiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AiDbContext(options);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    // ── Quota Tests ───────────────────────────────────────────────────────────

    [Test]
    public async Task GetRemainingQuota_WhenNoUsage_ShouldReturnFullLimit()
    {
        // Arrange
        _redisDatabaseMock.Setup(r => r.StringGetAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        var service = CreateService();

        // Act
        var remaining = await service.GetRemainingQuota(1, "PREMIUM");

        // Assert
        remaining.Should().Be(100); // PREMIUM_MONTHLY_LIMIT
    }

    [Test]
    public async Task GetRemainingQuota_WhenSomeUsed_ShouldReturnCorrectRemaining()
    {
        // Arrange
        _redisDatabaseMock.Setup(r => r.StringGetAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue("3")); // 3 used

        var service = CreateService();

        // Act
        var remaining = await service.GetRemainingQuota(1, "PREMIUM");

        // Assert
        remaining.Should().Be(97); // 100 - 3
    }

    // ── Repository Tests ──────────────────────────────────────────────────────

    [Test]
    public async Task GetMonthlyUsageCount_ShouldCountOnlyCompletedRequests()
    {
        // Arrange
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        _dbContext.AiRequests.AddRange(
            new AiRequest
            {
                UserId      = 1,
                RequestType = "GENERATE_SUMMARY",
                Status      = "COMPLETED",
                RequestedAt = DateTime.UtcNow,
                Prompt      = "test"
            },
            new AiRequest
            {
                UserId      = 1,
                RequestType = "ATS_CHECK",
                Status      = "FAILED", // should not count
                RequestedAt = DateTime.UtcNow,
                Prompt      = "test"
            },
            new AiRequest
            {
                UserId      = 2, // different user
                RequestType = "GENERATE_SUMMARY",
                Status      = "COMPLETED",
                RequestedAt = DateTime.UtcNow,
                Prompt      = "test"
            }
        );
        await _dbContext.SaveChangesAsync();

        var repo = new AiRequestRepository(_dbContext);

        // Act
        var count = await repo.GetMonthlyUsageCount(1);

        // Assert
        count.Should().Be(1); // only 1 completed for user 1
    }

    [Test]
    public async Task GetByUserId_ShouldReturnOnlyUserRequests()
    {
        // Arrange
        _dbContext.AiRequests.AddRange(
            new AiRequest { UserId = 1, RequestType = "GENERATE_SUMMARY", Status = "COMPLETED", RequestedAt = DateTime.UtcNow, Prompt = "test1" },
            new AiRequest { UserId = 1, RequestType = "ATS_CHECK", Status = "COMPLETED", RequestedAt = DateTime.UtcNow, Prompt = "test2" },
            new AiRequest { UserId = 2, RequestType = "IMPROVE_BULLET", Status = "COMPLETED", RequestedAt = DateTime.UtcNow, Prompt = "test3" }
        );
        await _dbContext.SaveChangesAsync();

        var repo = new AiRequestRepository(_dbContext);

        // Act
        var result = await repo.GetByUserId(1);

        // Assert
        result.Should().HaveCount(2);
        result.All(r => r.UserId == 1).Should().BeTrue();
    }

    [Test]
    public async Task GetByResumeId_ShouldReturnOnlyResumeRequests()
    {
        // Arrange
        _dbContext.AiRequests.AddRange(
            new AiRequest { UserId = 1, ResumeId = 1, RequestType = "GENERATE_SUMMARY", Status = "COMPLETED", RequestedAt = DateTime.UtcNow, Prompt = "test1" },
            new AiRequest { UserId = 1, ResumeId = 2, RequestType = "ATS_CHECK", Status = "COMPLETED", RequestedAt = DateTime.UtcNow, Prompt = "test2" }
        );
        await _dbContext.SaveChangesAsync();

        var repo = new AiRequestRepository(_dbContext);

        // Act
        var result = await repo.GetByResumeId(1);

        // Assert
        result.Should().HaveCount(1);
        result.First().ResumeId.Should().Be(1);
    }

    [Test]
    public async Task Save_ShouldPersistRequest()
    {
        // Arrange
        var repo = new AiRequestRepository(_dbContext);
        var request = new AiRequest
        {
            UserId      = 1,
            ResumeId    = 1,
            RequestType = "GENERATE_SUMMARY",
            Status      = "PENDING",
            Prompt      = "Test prompt",
            RequestedAt = DateTime.UtcNow
        };

        // Act
        var result = await repo.Save(request);

        // Assert
        result.RequestId.Should().BeGreaterThan(0);
        _dbContext.AiRequests.Count().Should().Be(1);
    }

    [Test]
    public async Task Update_ShouldUpdateRequest()
    {
        // Arrange
        var repo = new AiRequestRepository(_dbContext);
        var request = new AiRequest
        {
            UserId      = 1,
            RequestType = "GENERATE_SUMMARY",
            Status      = "PENDING",
            Prompt      = "Test",
            RequestedAt = DateTime.UtcNow
        };

        _dbContext.AiRequests.Add(request);
        await _dbContext.SaveChangesAsync();

        // Act
        request.Status      = "COMPLETED";
        request.Response    = "AI response here";
        request.CompletedAt = DateTime.UtcNow;
        var updated = await repo.Update(request);

        // Assert
        updated.Status.Should().Be("COMPLETED");
        updated.Response.Should().Be("AI response here");
        updated.CompletedAt.Should().NotBeNull();
    }

    // ── Request Type Tests ────────────────────────────────────────────────────

    [Test]
    public void AiRequest_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var request = new AiRequest();

        // Assert
        request.Status.Should().Be("PENDING");
        request.AiProvider.Should().Be("OPENAI");
        request.TokensUsed.Should().Be(0);
    }

    [Test]
    public async Task GetMonthlyUsageCount_PreviousMonth_ShouldNotCount()
    {
        // Arrange
        _dbContext.AiRequests.Add(new AiRequest
        {
            UserId      = 1,
            RequestType = "GENERATE_SUMMARY",
            Status      = "COMPLETED",
            RequestedAt = DateTime.UtcNow.AddMonths(-1), // last month
            Prompt      = "test"
        });
        await _dbContext.SaveChangesAsync();

        var repo = new AiRequestRepository(_dbContext);

        // Act
        var count = await repo.GetMonthlyUsageCount(1);

        // Assert
        count.Should().Be(0); // last month's requests don't count
    }

    [Test]
    public async Task ProcessRequest_WhenQuotaExceeded_ShouldThrowInvalidOperation()
    {
        // Arrange — simulate 100 used calls (PREMIUM_MONTHLY_LIMIT)
        _redisDatabaseMock.Setup(r => r.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue("100")); // PREMIUM_MONTHLY_LIMIT

        var service = CreateService();

        // Act
        var act = async () => await service.GenerateSummary(1, 1, "content");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Monthly AI quota exceeded. Upgrade to Premium.");
    }

    [Test]
    public async Task ProcessRequest_AtFreeLimit_ShouldNotBeBlocked()
    {
        // Arrange — 10 calls (old free limit) must NOT block; threshold is now 100
        _redisDatabaseMock.Setup(r => r.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue("10")); // FREE_MONTHLY_LIMIT — no longer a hard cap

        // Repo mock — needs Save/Update to not throw
        _repoMock.Setup(r => r.Save(It.IsAny<AiRequest>()))
                 .ReturnsAsync((AiRequest r) => r);
        _repoMock.Setup(r => r.Update(It.IsAny<AiRequest>()))
                 .ReturnsAsync((AiRequest r) => r);

        // Redis increment / TTL stubs
        _redisDatabaseMock.Setup(r => r.StringIncrementAsync(
            It.IsAny<RedisKey>(), It.IsAny<long>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(11);
        _redisDatabaseMock.Setup(r => r.KeyTimeToLiveAsync(
            It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(TimeSpan.FromDays(20));

        var service = CreateService();

        // Act — should not throw (both AI calls will fail with placeholder keys,
        //        but the quota gate itself must pass)
        var act = async () => await service.GenerateSummary(1, 1, "content");

        // The AI call will fail, but the exception must NOT be InvalidOperationException
        // (i.e., the quota gate did not block the request)
        var result = await service.GenerateSummary(1, 1, "content");
        result.Status.Should().BeOneOf("COMPLETED", "FAILED"); // quota gate passed
    }

    // ── Private helper ────────────────────────────────────────────────────────
    private AiService CreateService() =>
        new AiService(
            _repoMock.Object,
            _dbContext,
            _redisMock.Object,
            _configMock.Object,
            _loggerMock.Object,
            _httpClientFactoryMock.Object);
}
