using MediBook.Appointment.API.Controllers;
using MediBook.Appointment.API.DTOs;
using MediBook.Appointment.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using NUnit.Framework;

namespace MediBook.Appointment.API.Tests.Controllers;

[TestFixture]
public class AppointmentControllerTests
{
    private Mock<IAppointmentService> _apptServiceMock;
    private Mock<ILogger<AppointmentController>> _loggerMock;
    private AppointmentController _controller;

    [SetUp]
    public void Setup()
    {
        _apptServiceMock = new Mock<IAppointmentService>();
        _loggerMock = new Mock<ILogger<AppointmentController>>();
        _controller = new AppointmentController(_apptServiceMock.Object, _loggerMock.Object);
    }

    [Test]
    public async Task GetById_ShouldReturnOk_WhenFound()
    {
        // Arrange
        int apptId = 1;
        var response = new AppointmentDto(apptId, Guid.NewGuid(), Guid.NewGuid(), 101, "Consultation", "2026-06-01", "09:00", "10:00", "Scheduled", "Notes", "Online", DateTime.UtcNow, DateTime.UtcNow);
        
        _apptServiceMock.Setup(x => x.GetByIdAsync(apptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetById(apptId, default);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(response);
    }

    [Test]
    public async Task GetById_ShouldReturnNotFound_WhenServiceThrowsKeyNotFound()
    {
        // Arrange
        int apptId = 99;
        _apptServiceMock.Setup(x => x.GetByIdAsync(apptId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Not found"));

        // Act
        var result = await _controller.GetById(apptId, default);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task Cancel_ShouldReturnNoContent_WhenSuccessful()
    {
        // Arrange
        int apptId = 1;
        _apptServiceMock.Setup(x => x.CancelAppointmentAsync(apptId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Cancel(apptId, default);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }
}
