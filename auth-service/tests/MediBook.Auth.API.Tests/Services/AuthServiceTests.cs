using MediBook.Auth.API.Data;
using MediBook.Auth.API.DTOs;
using MediBook.Auth.API.Entities;
using MediBook.Auth.API.Helpers;
using MediBook.Auth.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using FluentAssertions;
using NUnit.Framework;

namespace MediBook.Auth.API.Tests.Services;

[TestFixture]
public class AuthServiceTests
{
    private AuthDbContext _db;
    private JwtTokenGenerator _jwtGenerator;
    private Mock<ILogger<AuthService>> _loggerMock;
    private IOptions<JwtSettings> _jwtSettings;
    private AuthService _authService;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new AuthDbContext(options);

        // JwtSettings setup
        var settings = new JwtSettings
        {
            SecretKey = "a-very-long-secret-key-that-is-at-least-32-characters",
            Issuer = "MediBook.Auth",
            Audience = "MediBook.Clients",
            AccessTokenExpiryMinutes = 60,
            RefreshTokenExpiryDays = 7
        };
        _jwtSettings = Options.Create(settings);

        // JwtTokenGenerator - using real instance as it is sealed
        _jwtGenerator = new JwtTokenGenerator(_jwtSettings);

        _loggerMock = new Mock<ILogger<AuthService>>();

        _authService = new AuthService(_db, _jwtGenerator, _jwtSettings, _loggerMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
    }

    [Test]
    public async Task RegisterAsync_ShouldCreateUser_WhenEmailIsUnique()
    {
        // Arrange
        var request = new RegisterRequest(
            FullName: "Test User",
            Email: "test@example.com",
            Password: "Password123!",
            Phone: "1234567890"
        );

        // Act
        var response = await _authService.RegisterAsync(request, default);

        // Assert
        response.Should().NotBeNull();
        response.Email.Should().Be(request.Email);
        response.FullName.Should().Be(request.FullName);
        
        var userInDb = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        userInDb.Should().NotBeNull();
        userInDb!.FullName.Should().Be(request.FullName);
    }

    [Test]
    public async Task RegisterAsync_ShouldThrowException_WhenEmailExists()
    {
        // Arrange
        var existingUser = User.Create("Existing", "test@example.com", "hash", "123", "Patient");
        _db.Users.Add(existingUser);
        await _db.SaveChangesAsync();

        var request = new RegisterRequest(
            FullName: "Test User",
            Email: "test@example.com",
            Password: "Password123!",
            Phone: "1234567890"
        );

        // Act
        Func<Task> act = async () => await _authService.RegisterAsync(request, default);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Test]
    public async Task LoginAsync_ShouldReturnTokens_WhenCredentialsAreValid()
    {
        // Arrange
        string password = "Password123!";
        string hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        var user = User.Create("Test User", "login@example.com", hash, "123", "Patient");
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var request = new LoginRequest("login@example.com", password);

        // Act
        var response = await _authService.LoginAsync(request, "127.0.0.1", default);

        // Assert
        response.Should().NotBeNull();
        response.AccessToken.Should().NotBeNullOrEmpty();
        response.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task LoginAsync_ShouldThrowException_WhenPasswordIsInvalid()
    {
        // Arrange
        string password = "CorrectPassword";
        string hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        var user = User.Create("Test User", "login@example.com", hash, "123", "Patient");
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var request = new LoginRequest("login@example.com", "WrongPassword");

        // Act
        Func<Task> act = async () => await _authService.LoginAsync(request, "127.0.0.1", default);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid email or password.");
    }
}
