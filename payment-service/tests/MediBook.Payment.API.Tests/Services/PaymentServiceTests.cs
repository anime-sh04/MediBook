using MediBook.Payment.API.DTOs;
using MediBook.Payment.API.Entities;
using MediBook.Payment.API.Helpers;
using MediBook.Payment.API.Messaging.Infrastructure;
using MediBook.Payment.API.Repositories;
using MediBook.Payment.API.Services;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using NUnit.Framework;

namespace MediBook.Payment.API.Tests.Services;

[TestFixture]
public class PaymentServiceTests
{
    private Mock<IPaymentRepository> _repoMock;
    private Mock<ISagaEventPublisher> _sagaPublisherMock;
    private Mock<ILogger<PaymentService>> _loggerMock;
    private Mock<IHttpClientFactory> _httpClientFactoryMock;
    private RazorpaySettings _razorpaySettings;
    private PaymentService _paymentService;

    [SetUp]
    public void Setup()
    {
        _repoMock = new Mock<IPaymentRepository>();
        _sagaPublisherMock = new Mock<ISagaEventPublisher>();
        _loggerMock = new Mock<ILogger<PaymentService>>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _razorpaySettings = new RazorpaySettings { KeyId = "test_key", KeySecret = "test_secret" };

        _paymentService = new PaymentService(
            _repoMock.Object,
            _razorpaySettings,
            _sagaPublisherMock.Object,
            _loggerMock.Object,
            _httpClientFactoryMock.Object);
    }

    [Test]
    public async Task ProcessPaymentAsync_ShouldCreateCashPayment_WhenModeIsCash()
    {
        // Arrange
        var request = new ProcessPaymentRequest(
            AppointmentId: 10,
            PatientId: Guid.NewGuid(),
            ProviderId: Guid.NewGuid(),
            Amount: 150,
            Mode: "Cash",
            CorrelationId: Guid.NewGuid(),
            SlotId: 101
        );

        _repoMock.Setup(x => x.FindByAppointmentIdAsync(request.AppointmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment.API.Entities.Payment?)null);

        // Act
        var result = await _paymentService.ProcessPaymentAsync(request);

        // Assert
        result.Payment.Should().NotBeNull();
        result.Payment.Mode.Should().Be("Cash");
        result.RazorpayOrder.Should().BeNull();
        _repoMock.Verify(x => x.AddAsync(It.IsAny<Payment.API.Entities.Payment>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ConfirmPaymentAsync_ShouldMarkPaidAndPublishSucceeded()
    {
        // Arrange
        int paymentId = 1;
        var payment = Payment.API.Entities.Payment.Create(
            10, Guid.NewGuid(), Guid.NewGuid(), 150, "Card", Guid.NewGuid(), 101, "INR", "Notes");
        
        _repoMock.Setup(x => x.GetTrackedByIdAsync(paymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var request = new ConfirmPaymentRequest(paymentId, "order_1", "pay_1", "sig_1", "txn_1");

        // Act
        var result = await _paymentService.ConfirmPaymentAsync(request);

        // Assert
        result.Status.Should().Be("Paid");
        _repoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _sagaPublisherMock.Verify(x => x.PublishPaymentSucceeded(It.IsAny<Payment.API.Messaging.Contracts.PaymentSucceeded>()), Times.Once);
    }

    [Test]
    public async Task FailPaymentAsync_ShouldMarkFailedAndPublishFailed()
    {
        // Arrange
        int paymentId = 1;
        var payment = Payment.API.Entities.Payment.Create(
            10, Guid.NewGuid(), Guid.NewGuid(), 150, "Card", Guid.NewGuid(), 101, "INR", "Notes");
        
        _repoMock.Setup(x => x.GetTrackedByIdAsync(paymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var request = new FailPaymentRequest(paymentId, "User cancelled");

        // Act
        var result = await _paymentService.FailPaymentAsync(request);

        // Assert
        result.Status.Should().Be("Failed");
        _repoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _sagaPublisherMock.Verify(x => x.PublishPaymentFailed(It.IsAny<Payment.API.Messaging.Contracts.PaymentFailed>()), Times.Once);
    }
}
