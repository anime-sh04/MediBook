using MediBook.Appointment.API.DTOs;
using MediBook.Appointment.API.Entities;
using MediBook.Appointment.API.Repositories;
using MediBook.Appointment.API.Services;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using NUnit.Framework;

namespace MediBook.Appointment.API.Tests.Services;

[TestFixture]
public class AppointmentServiceTests
{
    private Mock<IAppointmentRepository> _repoMock;
    private Mock<IScheduleClient> _schedSvcMock;
    private Mock<IPaymentClient> _paySvcMock;
    private Mock<ILogger<AppointmentService>> _loggerMock;
    private AppointmentService _appointmentService;

    [SetUp]
    public void Setup()
    {
        _repoMock = new Mock<IAppointmentRepository>();
        _schedSvcMock = new Mock<IScheduleClient>();
        _paySvcMock = new Mock<IPaymentClient>();
        _loggerMock = new Mock<ILogger<AppointmentService>>();

        _appointmentService = new AppointmentService(
            _repoMock.Object,
            _schedSvcMock.Object,
            _paySvcMock.Object,
            _loggerMock.Object);
    }

    [Test]
    public async Task CreateFromSagaAsync_ShouldCreateAppointment_WhenNotExists()
    {
        // Arrange
        var command = new CreateAppointmentFromSagaCommand(
            PatientId: Guid.NewGuid(),
            ProviderId: Guid.NewGuid(),
            SlotId: 1,
            ServiceType: "Consultation",
            AppointmentDate: "2026-06-01",
            StartTime: "09:00",
            EndTime: "10:00",
            ModeOfConsultation: "Online",
            CorrelationId: Guid.NewGuid(),
            Notes: "Test Notes"
        );

        _repoMock.Setup(x => x.FindBySlotIdAsync(command.SlotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Appointment.API.Entities.Appointment?)null);

        var appointment = Appointment.API.Entities.Appointment.Create(
            command.PatientId, command.ProviderId, command.SlotId, command.ServiceType,
            DateOnly.Parse(command.AppointmentDate), TimeOnly.Parse(command.StartTime),
            TimeOnly.Parse(command.EndTime), command.ModeOfConsultation, command.Notes);

        _repoMock.Setup(x => x.AddAsync(It.IsAny<Appointment.API.Entities.Appointment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(appointment);

        // Act
        var result = await _appointmentService.CreateFromSagaAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.SlotId.Should().Be(command.SlotId);
        _repoMock.Verify(x => x.AddAsync(It.IsAny<Appointment.API.Entities.Appointment>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnDto_WhenFound()
    {
        // Arrange
        int apptId = 1;
        var appt = Appointment.API.Entities.Appointment.Create(
            Guid.NewGuid(), Guid.NewGuid(), 1, "Consultation",
            DateOnly.FromDateTime(DateTime.UtcNow), new TimeOnly(10, 0),
            new TimeOnly(11, 0), "Online", "Notes");

        _repoMock.Setup(x => x.GetByIdAsync(apptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(appt);

        // Act
        var result = await _appointmentService.GetByIdAsync(apptId);

        // Assert
        result.Should().NotBeNull();
        result.ServiceType.Should().Be("Consultation");
    }

    [Test]
    public async Task CancelAppointmentAsync_ShouldCallSchedAndPayClients()
    {
        // Arrange
        int apptId = 1;
        var appt = Appointment.API.Entities.Appointment.Create(
            Guid.NewGuid(), Guid.NewGuid(), 101, "Consultation",
            DateOnly.FromDateTime(DateTime.UtcNow), new TimeOnly(10, 0),
            new TimeOnly(11, 0), "Online", "Notes");

        _repoMock.Setup(x => x.GetByIdAsync(apptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(appt);

        // Act
        await _appointmentService.CancelAppointmentAsync(apptId);

        // Assert
        appt.Status.Should().Be("Cancelled");
        _schedSvcMock.Verify(x => x.UnbookSlotAsync(101, It.IsAny<CancellationToken>()), Times.Once);
        _paySvcMock.Verify(x => x.RefundAsync(apptId, It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
