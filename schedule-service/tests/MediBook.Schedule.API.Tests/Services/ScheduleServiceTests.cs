using MediBook.Schedule.API.DTOs;
using MediBook.Schedule.API.Entities;
using MediBook.Schedule.API.Messaging.Infrastructure;
using MediBook.Schedule.API.Repositories;
using MediBook.Schedule.API.Services;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using NUnit.Framework;

namespace MediBook.Schedule.API.Tests.Services;

[TestFixture]
public class ScheduleServiceTests
{
    private Mock<ISlotRepository> _repoMock;
    private Mock<ISagaEventPublisher> _publisherMock;
    private Mock<ILogger<ScheduleService>> _loggerMock;
    private ScheduleService _scheduleService;

    [SetUp]
    public void Setup()
    {
        _repoMock = new Mock<ISlotRepository>();
        _publisherMock = new Mock<ISagaEventPublisher>();
        _loggerMock = new Mock<ILogger<ScheduleService>>();

        _scheduleService = new ScheduleService(_repoMock.Object, _publisherMock.Object, _loggerMock.Object);
    }

    [Test]
    public async Task AddSlotAsync_ShouldCallRepoAndReturnDto()
    {
        // Arrange
        var request = new AddSlotRequest(
            ProviderId: Guid.NewGuid(),
            Date: "2026-06-01",
            StartTime: "09:00",
            EndTime: "10:00",
            Price: 150,
            Recurrence: "none"
        );

        var slot = AvailabilitySlot.Create(
            request.ProviderId,
            DateOnly.Parse(request.Date),
            TimeOnly.Parse(request.StartTime),
            TimeOnly.Parse(request.EndTime),
            request.Recurrence,
            request.Price);

        _repoMock.Setup(x => x.AddAsync(It.IsAny<AvailabilitySlot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(slot);

        // Act
        var result = await _scheduleService.AddSlotAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.ProviderId.Should().Be(request.ProviderId);
        _repoMock.Verify(x => x.AddAsync(It.IsAny<AvailabilitySlot>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task BookSlotAsync_ShouldMarkAsBooked()
    {
        // Arrange
        int slotId = 1;
        var slot = AvailabilitySlot.Create(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), new TimeOnly(10, 0), new TimeOnly(11, 0), "none", 100);
        
        _repoMock.Setup(x => x.GetByIdAsync(slotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slot);

        // Act
        await _scheduleService.BookSlotAsync(slotId);

        // Assert
        slot.IsBooked.Should().BeTrue();
        _repoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task BookSlotAsync_ShouldThrowException_WhenSlotNotFound()
    {
        // Arrange
        int slotId = 99;
        _repoMock.Setup(x => x.GetByIdAsync(slotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AvailabilitySlot?)null);

        // Act
        Func<Task> act = async () => await _scheduleService.BookSlotAsync(slotId);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
