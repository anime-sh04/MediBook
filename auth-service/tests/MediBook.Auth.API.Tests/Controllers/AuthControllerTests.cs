using MediBook.Auth.API.Controllers;
using MediBook.Auth.API.DTOs;
using MediBook.Auth.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using NUnit.Framework;
using FluentValidation;
using FluentValidation.Results;

namespace MediBook.Auth.API.Tests.Controllers;

[TestFixture]
public class AuthControllerTests
{
    private Mock<IAuthService> _authServiceMock;
    private Mock<ILogger<AuthController>> _loggerMock;
    private Mock<IValidator<RegisterRequest>> _registerValidatorMock;
    private Mock<IValidator<LoginRequest>> _loginValidatorMock;
    private AuthController _controller;

    [SetUp]
    public void Setup()
    {
        _authServiceMock = new Mock<IAuthService>();
        _loggerMock = new Mock<ILogger<AuthController>>();
        _registerValidatorMock = new Mock<IValidator<RegisterRequest>>();
        _loginValidatorMock = new Mock<IValidator<LoginRequest>>();

        // Default validation success
        _registerValidatorMock.Setup(x => x.ValidateAsync(It.IsAny<RegisterRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _loginValidatorMock.Setup(x => x.ValidateAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _controller = new AuthController(
            _authServiceMock.Object,
            _registerValidatorMock.Object,
            _loginValidatorMock.Object,
            _loggerMock.Object);
        
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Test]
    public async Task Register_ShouldReturnCreated_WhenRegistrationSuccessful()
    {
        // Arrange
        var request = new RegisterRequest("Full Name", "test@example.com", "Pass123!", "12345");
        var response = new RegisterResponse(Guid.NewGuid(), "Full Name", "test@example.com", "12345", "Patient", true, DateTime.UtcNow);
        
        _authServiceMock.Setup(x => x.RegisterAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Register(request, default);

        // Assert
        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status201Created);
        ((ObjectResult)result).Value.Should().BeEquivalentTo(response);
    }

    [Test]
    public async Task Login_ShouldReturnOk_WhenLoginSuccessful()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "Pass123!");
        var userDto = new UserDto(Guid.NewGuid(), "Name", "test@example.com", "123", "Patient", true, DateTime.UtcNow, null, true);
        var response = new LoginResponse("access-token", "refresh-token", DateTime.UtcNow.AddMinutes(60), userDto);
        
        _authServiceMock.Setup(x => x.LoginAsync(request, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Login(request, default);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(response);
    }

    [Test]
    public async Task Login_ShouldReturnUnauthorized_WhenAuthServiceThrowsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "wrong");
        _authServiceMock.Setup(x => x.LoginAsync(request, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid email or password."));

        // Act
        var result = await _controller.Login(request, default);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }
}
