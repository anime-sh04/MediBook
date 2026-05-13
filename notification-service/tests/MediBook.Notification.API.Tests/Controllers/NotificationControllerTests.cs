using MediBook.Notification.API.Controllers;
using MediBook.Notification.API.DTOs;
using MediBook.Notification.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using NUnit.Framework;
using FluentValidation;
using FluentValidation.Results;

namespace MediBook.Notification.API.Tests.Controllers;

[TestFixture]
public class NotificationControllerTests
{
    private Mock<INotificationService> _notifServiceMock;
    private Mock<IValidator<SendNotificationRequest>> _sendValidatorMock;
    private Mock<IValidator<SendBulkRequest>> _bulkValidatorMock;
    private Mock<IValidator<SendEmailRequest>> _emailValidatorMock;
    private Mock<ILogger<NotificationController>> _loggerMock;
    private NotificationController _controller;

    [SetUp]
    public void Setup()
    {
        _notifServiceMock = new Mock<INotificationService>();
        _sendValidatorMock = new Mock<IValidator<SendNotificationRequest>>();
        _bulkValidatorMock = new Mock<IValidator<SendBulkRequest>>();
        _emailValidatorMock = new Mock<IValidator<SendEmailRequest>>();
        _loggerMock = new Mock<ILogger<NotificationController>>();

        _sendValidatorMock.Setup(x => x.ValidateAsync(It.IsAny<SendNotificationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _bulkValidatorMock.Setup(x => x.ValidateAsync(It.IsAny<SendBulkRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _emailValidatorMock.Setup(x => x.ValidateAsync(It.IsAny<SendEmailRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _controller = new NotificationController(
            _notifServiceMock.Object,
            _sendValidatorMock.Object,
            _bulkValidatorMock.Object,
            _emailValidatorMock.Object,
            _loggerMock.Object);
    }

    [Test]
    public async Task Send_ShouldReturnCreated_WhenSuccessful()
    {
        // Arrange
        var request = new SendNotificationRequest(Guid.NewGuid(), "test@example.com", "User", "Type", "Title", "Msg", "APP");
        var response = new NotificationResponse(Guid.NewGuid(), request.RecipientId, request.Type, request.Title, request.Message, request.Channel, null, null, false, DateTime.UtcNow, DateTime.UtcNow);
        
        _notifServiceMock.Setup(x => x.SendAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Send(request, default);

        // Assert
        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status201Created);
        ((ObjectResult)result).Value.Should().BeEquivalentTo(response);
    }

    [Test]
    public async Task GetUnreadCount_ShouldReturnOk()
    {
        // Arrange
        var recipientId = Guid.NewGuid();
        var response = new UnreadCountResponse(5);
        _notifServiceMock.Setup(x => x.GetUnreadCountAsync(recipientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetUnreadCount(recipientId, default);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(response);
    }

    [Test]
    public async Task MarkAsRead_ShouldReturnNotFound_WhenNotificationMissing()
    {
        // Arrange
        var id = Guid.NewGuid();
        _notifServiceMock.Setup(x => x.MarkAsReadAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.MarkAsRead(id, default);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
