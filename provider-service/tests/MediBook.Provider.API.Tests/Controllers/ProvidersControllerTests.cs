using MediBook.Provider.API.Controllers;
using MediBook.Provider.API.DTOs;
using MediBook.Provider.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using NUnit.Framework;
using FluentValidation;
using FluentValidation.Results;
using System.Security.Claims;

namespace MediBook.Provider.API.Tests.Controllers;

[TestFixture]
public class ProvidersControllerTests
{
    private Mock<IProviderService> _providerServiceMock;
    private Mock<IValidator<RegisterProviderRequest>> _validatorMock;
    private Mock<ILogger<ProvidersController>> _loggerMock;
    private ProvidersController _controller;

    [SetUp]
    public void Setup()
    {
        _providerServiceMock = new Mock<IProviderService>();
        _validatorMock = new Mock<IValidator<RegisterProviderRequest>>();
        _loggerMock = new Mock<ILogger<ProvidersController>>();

        _validatorMock.Setup(x => x.ValidateAsync(It.IsAny<RegisterProviderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _controller = new ProvidersController(
            _providerServiceMock.Object,
            _validatorMock.Object,
            _loggerMock.Object);

        // Mock User for Authorize checks and TryGetUserId
        var userId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "Provider")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Test]
    public async Task RegisterProvider_ShouldReturnCreated_WhenSuccessful()
    {
        // Arrange
        var request = new RegisterProviderRequest("Cardio", "MD", 10, "Bio", "Clinic", "Addr", "City", "State", 100);
        var response = new ProviderProfileDto(Guid.NewGuid(), Guid.NewGuid(), "Cardio", "MD", 10, "Bio", "Clinic", "Addr", "City", "State", 100, true, true, 0, DateTime.UtcNow);
        
        _providerServiceMock.Setup(x => x.RegisterProviderAsync(It.IsAny<Guid>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.RegisterProvider(request, default);

        // Assert
        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status201Created);
        ((ObjectResult)result).Value.Should().BeEquivalentTo(response);
    }

    [Test]
    public async Task GetById_ShouldReturnOk_WhenFound()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var response = new ProviderProfileDto(providerId, Guid.NewGuid(), "Cardio", "MD", 10, "Bio", "Clinic", "Addr", "City", "State", 100, true, true, 0, DateTime.UtcNow);
        
        _providerServiceMock.Setup(x => x.GetProviderByIdAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetById(providerId, default);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(response);
    }

    [Test]
    public async Task GetById_ShouldReturnNotFound_WhenServiceThrowsKeyNotFound()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        _providerServiceMock.Setup(x => x.GetProviderByIdAsync(providerId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Not found"));

        // Act
        var result = await _controller.GetById(providerId, default);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
