using MediBook.Payment.API.Controllers;
using MediBook.Payment.API.DTOs;
using MediBook.Payment.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using NUnit.Framework;

namespace MediBook.Payment.API.Tests.Controllers;

[TestFixture]
public class PaymentControllerTests
{
    private Mock<IPaymentService> _payServiceMock;
    private Mock<ILogger<PaymentController>> _loggerMock;
    private PaymentController _controller;

    [SetUp]
    public void Setup()
    {
        _payServiceMock = new Mock<IPaymentService>();
        _loggerMock = new Mock<ILogger<PaymentController>>();
        _controller = new PaymentController(_payServiceMock.Object, _loggerMock.Object);
    }

    [Test]
    public async Task Process_ShouldReturnCreated_WhenSuccessful()
    {
        // Arrange
        var request = new ProcessPaymentRequest(10, Guid.NewGuid(), Guid.NewGuid(), 150, "CASH", Guid.NewGuid(), 101);
        var paymentDto = new PaymentDto(1, request.AppointmentId, request.PatientId, request.ProviderId, request.Amount, "PENDING", "CASH", "txn_1", "INR", "order_1", "pay_1", null, "Notes", request.CorrelationId, request.SlotId);
        
        _payServiceMock.Setup(x => x.ProcessPaymentAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((paymentDto, null));

        // Act
        var result = await _controller.Process(request, default);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        ((CreatedAtActionResult)result).Value.Should().BeEquivalentTo(paymentDto);
    }

    [Test]
    public async Task Confirm_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var request = new ConfirmPaymentRequest(1, "order_1", "pay_1", "sig_1", "txn_1");
        var response = new ConfirmPaymentResponse(1, "Paid", "order_1", "pay_1", "txn_1", Guid.NewGuid(), 101, "Message");

        _payServiceMock.Setup(x => x.ConfirmPaymentAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Confirm(request, default);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(response);
    }

    [Test]
    public async Task Fail_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var request = new FailPaymentRequest(1, "Reason");
        var response = new FailPaymentResponse(1, "Failed", Guid.NewGuid(), 101, "Message");

        _payServiceMock.Setup(x => x.FailPaymentAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Fail(request, default);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(response);
    }
}
