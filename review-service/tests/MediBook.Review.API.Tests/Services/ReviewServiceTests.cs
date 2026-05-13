using MediBook.Review.API.DTOs;
using MediBook.Review.API.Entities;
using MediBook.Review.API.Repositories;
using MediBook.Review.API.Services;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using NUnit.Framework;

namespace MediBook.Review.API.Tests.Services;

[TestFixture]
public class ReviewServiceTests
{
    private Mock<IReviewRepository> _repoMock;
    private Mock<IProviderClient> _providerClientMock;
    private Mock<ILogger<ReviewService>> _loggerMock;
    private ReviewService _reviewService;

    [SetUp]
    public void Setup()
    {
        _repoMock = new Mock<IReviewRepository>();
        _providerClientMock = new Mock<IProviderClient>();
        _loggerMock = new Mock<ILogger<ReviewService>>();
        
        _reviewService = new ReviewService(
            _repoMock.Object,
            _providerClientMock.Object,
            _loggerMock.Object);
    }

    [Test]
    public async Task AddReviewAsync_ShouldAddReview_WhenSuccessful()
    {
        // Arrange
        var request = new AddReviewRequest(1, Guid.NewGuid(), Guid.NewGuid(), 5, "Great!", false);
        _repoMock.Setup(x => x.ExistsByAppointmentIdAsync(request.AppointmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        
        var review = Review.API.Entities.Review.Create(1, request.PatientId, request.ProviderId, 5, "Great!", false);
        _repoMock.Setup(x => x.AddAsync(It.IsAny<Review.API.Entities.Review>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);
        _repoMock.Setup(x => x.AvgRatingByProviderIdAsync(request.ProviderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5.0);

        // Act
        var result = await _reviewService.AddReviewAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Rating.Should().Be(5);
        _repoMock.Verify(x => x.AddAsync(It.IsAny<Review.API.Entities.Review>(), It.IsAny<CancellationToken>()), Times.Once);
        _providerClientMock.Verify(x => x.UpdateProviderRatingAsync(request.ProviderId, 5.0, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AddReviewAsync_ShouldThrow_WhenReviewAlreadyExists()
    {
        // Arrange
        var request = new AddReviewRequest(1, Guid.NewGuid(), Guid.NewGuid(), 5, "Great!", false);
        _repoMock.Setup(x => x.ExistsByAppointmentIdAsync(request.AppointmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = async () => await _reviewService.AddReviewAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Test]
    public async Task GetAvgRatingAsync_ShouldReturnCorrectData()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        _repoMock.Setup(x => x.AvgRatingByProviderIdAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(4.5);
        _repoMock.Setup(x => x.CountByProviderIdAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);

        // Act
        var result = await _reviewService.GetAvgRatingAsync(providerId);

        // Assert
        result.AvgRating.Should().Be(4.5);
        result.ReviewCount.Should().Be(10);
    }
}
