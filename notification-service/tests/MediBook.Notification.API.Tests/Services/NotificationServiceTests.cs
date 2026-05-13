using MediBook.Notification.API.DTOs;
using MediBook.Notification.API.Entities;
using MediBook.Notification.API.Hubs;
using MediBook.Notification.API.Repositories;
using MediBook.Notification.API.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using NUnit.Framework;

namespace MediBook.Notification.API.Tests.Services;

[TestFixture]
public class NotificationServiceTests
{
    private Mock<INotificationRepository> _repoMock;
    private Mock<IEmailService> _emailServiceMock;
    private Mock<IHubContext<NotificationHub>> _hubMock;
    private Mock<IHubClients> _clientsMock;
    private Mock<IClientProxy> _clientProxyMock;
    private Mock<IGroupManager> _groupsMock;
    private Mock<ILogger<NotificationService>> _loggerMock;
    private NotificationService _notificationService;

    [SetUp]
    public void Setup()
    {
        _repoMock = new Mock<INotificationRepository>();
        _emailServiceMock = new Mock<IEmailService>();
        _hubMock = new Mock<IHubContext<NotificationHub>>();
        _clientsMock = new Mock<IHubClients>();
        _clientProxyMock = new Mock<IClientProxy>();
        _groupsMock = new Mock<IGroupManager>();
        _loggerMock = new Mock<ILogger<NotificationService>>();

        _hubMock.Setup(x => x.Clients).Returns(_clientsMock.Object);
        _clientsMock.Setup(x => x.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);

        _notificationService = new NotificationService(
            _repoMock.Object,
            _emailServiceMock.Object,
            _hubMock.Object,
            _loggerMock.Object);
    }

    [Test]
    public async Task SendAsync_ShouldSaveNotification()
    {
        // Arrange
        var request = new SendNotificationRequest(
            RecipientId: Guid.NewGuid(),
            RecipientEmail: "test@example.com",
            RecipientName: "Test User",
            Type: "TestType",
            Title: "Test Title",
            Message: "Test Message",
            Channel: "APP"
        );

        // Act
        var result = await _notificationService.SendAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Test Title");
        _repoMock.Verify(x => x.AddAsync(It.IsAny<Notification.API.Entities.Notification>(), It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SendAsync_ShouldTriggerEmail_WhenChannelIsEmail()
    {
        // Arrange
        var request = new SendNotificationRequest(
            RecipientId: Guid.NewGuid(),
            RecipientEmail: "test@example.com",
            RecipientName: "Test User",
            Type: "EmailTest",
            Title: "Email Title",
            Message: "Email Message",
            Channel: "EMAIL"
        );

        // Act
        await _notificationService.SendAsync(request);

        // Assert
        // The email is sent in a fire-and-forget task in the service, but we can verify it's called if we wait or if it's awaited.
        // Wait, in NotificationService.cs:
        // _ = SendEmailSafeAsync(...)
        // Since it's fire-and-forget, we might need a small delay or change the code to await it for testing.
        // Actually, the request has a fire-and-forget call. To test this properly, we'd need to await it or mock the task.
        // For now, I'll assume it's called.
        
        // Wait, I can verify it if I wait a bit.
        await Task.Delay(100);
        _emailServiceMock.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Test]
    public async Task MarkAsReadAsync_ShouldReturnTrue_WhenSuccessful()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repoMock.Setup(x => x.MarkAsReadAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _notificationService.MarkAsReadAsync(id);

        // Assert
        result.Should().BeTrue();
        _repoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
