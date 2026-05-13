using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Moq;
using NUnit.Framework;
using ResumeAI.Notification.API.Hubs;
using ResumeAI.Notification.API.Models;
using ResumeAI.Notification.API.Repositories;
using ResumeAI.Notification.API.Services;

namespace ResumeAI.Notification.Tests;

[TestFixture]
public class NotificationServiceTests
{
    private Mock<INotificationRepository> _repoMock;
    private Mock<IHubContext<NotificationHub>> _hubMock;
    private Mock<IHubClients> _clientsMock;
    private Mock<IClientProxy> _clientProxyMock;
    private NotificationService _service;

    [SetUp]
    public void SetUp()
    {
        _repoMock = new Mock<INotificationRepository>();
        _hubMock = new Mock<IHubContext<NotificationHub>>();
        _clientsMock = new Mock<IHubClients>();
        _clientProxyMock = new Mock<IClientProxy>();

        _hubMock.Setup(h => h.Clients).Returns(_clientsMock.Object);
        _clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);

        _service = new NotificationService(_repoMock.Object, _hubMock.Object);
    }

    [Test]
    public async Task CreateAndSend_ShouldSaveToDbAndPushToHub()
    {
        // Arrange
        _repoMock.Setup(r => r.Save(It.IsAny<NotificationEntity>())).ReturnsAsync((NotificationEntity n) => n);

        // Act
        var result = await _service.CreateAndSend(1, "INFO", "Title", "Message");

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(1);
        result.Title.Should().Be("Title");
        
        _repoMock.Verify(r => r.Save(It.IsAny<NotificationEntity>()), Times.Once);
        _clientsMock.Verify(c => c.Group("user_1"), Times.Once);
        _clientProxyMock.Verify(c => c.SendCoreAsync(
            "ReceiveNotification",
            It.Is<object[]>(o => o.Length == 1),
            default), Times.Once);
    }

    [Test]
    public async Task GetUnreadCount_ShouldReturnCountFromRepo()
    {
        // Arrange
        _repoMock.Setup(r => r.GetUnreadCount(1)).ReturnsAsync(5);

        // Act
        var result = await _service.GetUnreadCount(1);

        // Assert
        result.Should().Be(5);
    }

    [Test]
    public async Task MarkAsRead_ShouldCallRepo()
    {
        // Arrange
        _repoMock.Setup(r => r.MarkAsRead(100)).Returns(Task.CompletedTask);

        // Act
        await _service.MarkAsRead(100);

        // Assert
        _repoMock.Verify(r => r.MarkAsRead(100), Times.Once);
    }
}
