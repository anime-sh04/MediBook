using MediBook.Review.API.Controllers;
using MediBook.Review.API.DTOs;
using MediBook.Review.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using NUnit.Framework;

namespace MediBook.Review.API.Tests.Controllers;

[TestFixture]
public class ReviewControllerTests
{
    private Mock<IReviewService> _reviewServiceMock;
    private Mock<ILogger<ReviewController>> _loggerMock;
    private ReviewController _controller;

    [SetUp]
    public void Setup()
    {
        _reviewServiceMock = new Mock<IReviewService>();
        _loggerMock = new Mock<ILogger<ReviewController>>();
        _controller = new ReviewController(_reviewServiceMock.Object, _loggerMock.Object);
    }

    [Test]
    public async Task AddReview_ShouldReturnCreated_WhenSuccessful()
    {
        // Arrange
        var request = new AddReviewRequest(1, Guid.NewGuid(), Guid.NewGuid(), 5, "Great!", false);
        var responseDto = new ReviewDto(1, request.AppointmentId, request.PatientId, request.ProviderId, 5, "Great!", "2026-05-13", true, false);
        
        _reviewServiceMock.Setup(x => x.AddReviewAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        // Act
        var result = await _controller.AddReview(request, default);

        // Assert
        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status201Created);
        ((ObjectResult)result).Value.Should().BeEquivalentTo(responseDto);
    }

    [Test]
    public async Task GetAvgRating_ShouldReturnOk()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var response = new AvgRatingDto(providerId, 4.5, 10);
        _reviewServiceMock.Setup(x => x.GetAvgRatingAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetAvgRating(providerId, default);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(response);
    }

    [Test]
    public async Task GetByAppointment_ShouldReturnNotFound_WhenReviewMissing()
    {
        // Arrange
        int appointmentId = 1;
        _reviewServiceMock.Setup(x => x.GetByAppointmentAsync(appointmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReviewDto?)null);

        // Act
        var result = await _controller.GetByAppointment(appointmentId, default);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
