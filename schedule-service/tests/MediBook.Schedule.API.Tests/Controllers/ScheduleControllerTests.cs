using MediBook.Schedule.API.Controllers;
using MediBook.Schedule.API.DTOs;
using MediBook.Schedule.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using NUnit.Framework;

namespace MediBook.Schedule.API.Tests.Controllers;

[TestFixture]
public class ScheduleControllerTests
{
    private Mock<IScheduleService> _scheduleServiceMock;
    private Mock<ILogger<ScheduleController>> _loggerMock;
    private ScheduleController _controller;

    [SetUp]
    public void Setup()
    {
        _scheduleServiceMock = new Mock<IScheduleService>();
        _loggerMock = new Mock<ILogger<ScheduleController>>();
        _controller = new ScheduleController(_scheduleServiceMock.Object, _loggerMock.Object);
    }

    [Test]
    public async Task AddSlot_ShouldReturnCreated_WhenSuccessful()
    {
        // Arrange
        var request = new AddSlotRequest(Guid.NewGuid(), "2026-06-01", "09:00", "10:00", 150, "none");
        var response = new AvailabilitySlotDto(1, request.ProviderId, request.Date, request.StartTime, request.EndTime, 60, false, false, "none", DateTime.UtcNow);
        
        _scheduleServiceMock.Setup(x => x.AddSlotAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.AddSlot(request, default);

        // Assert
        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status201Created);
        ((ObjectResult)result).Value.Should().BeEquivalentTo(response);
    }

    [Test]
    public async Task GetById_ShouldReturnOk_WhenFound()
    {
        // Arrange
        int slotId = 1;
        var response = new AvailabilitySlotDto(slotId, Guid.NewGuid(), "2026-06-01", "09:00", "10:00", 60, false, false, "none", DateTime.UtcNow);
        
        _scheduleServiceMock.Setup(x => x.GetSlotByIdAsync(slotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetById(slotId, default);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(response);
    }

    [Test]
    public async Task GetById_ShouldReturnNotFound_WhenServiceThrowsKeyNotFound()
    {
        // Arrange
        int slotId = 99;
        _scheduleServiceMock.Setup(x => x.GetSlotByIdAsync(slotId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Not found"));

        // Act
        var result = await _controller.GetById(slotId, default);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
