using MediBook.Provider.API.Data;
using MediBook.Provider.API.DTOs;
using MediBook.Provider.API.Entities;
using MediBook.Provider.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using NUnit.Framework;
using StackExchange.Redis;

namespace MediBook.Provider.API.Tests.Services;

[TestFixture]
public class ProviderServiceTests
{
    private ProviderDbContext _db;
    private Mock<ILogger<ProviderService>> _loggerMock;
    private Mock<IConnectionMultiplexer> _redisMuxMock;
    private Mock<IDatabase> _redisDbMock;
    private RedisCacheService _cacheService;
    private ProviderService _providerService;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ProviderDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new ProviderDbContext(options);

        _loggerMock = new Mock<ILogger<ProviderService>>();
        
        _redisMuxMock = new Mock<IConnectionMultiplexer>();
        _redisDbMock = new Mock<IDatabase>();
        _redisMuxMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_redisDbMock.Object);
        
        _cacheService = new RedisCacheService(_redisMuxMock.Object);

        _providerService = new ProviderService(_db, _loggerMock.Object, _cacheService);
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
    }

    [Test]
    public async Task RegisterProviderAsync_ShouldCreateProfile_WhenNotExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new RegisterProviderRequest(
            Specialization: "Cardiology",
            Qualification: "MD",
            ExperienceYears: 10,
            Bio: "Test Bio",
            ClinicName: "Test Clinic",
            ClinicAddress: "123 Street",
            City: "Test City",
            State: "Test State",
            ConsultationFee: 100
        );

        // Act
        var result = await _providerService.RegisterProviderAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.Specialization.Should().Be("Cardiology");

        var profileInDb = await _db.ProviderProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        profileInDb.Should().NotBeNull();
    }

    [Test]
    public async Task RegisterProviderAsync_ShouldThrowException_WhenProfileExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingProfile = ProviderProfile.Create(userId, "Spec", "Qual", 5, "Bio", "Clinic", "Addr", "City", "State", 50);
        _db.ProviderProfiles.Add(existingProfile);
        await _db.SaveChangesAsync();

        var request = new RegisterProviderRequest("Cardio", "MD", 10, "Bio", "Clinic", "Addr", "City", "State", 100);

        // Act
        Func<Task> act = async () => await _providerService.RegisterProviderAsync(userId, request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("User already has a provider profile.");
    }

    [Test]
    public async Task GetProviderByIdAsync_ShouldReturnFromCache_WhenCached()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var dto = new ProviderProfileDto(providerId, Guid.NewGuid(), "Spec", "Qual", 5, "Bio", "Clinic", "Addr", "City", "State", 50, true, true, 4.5, DateTime.UtcNow);
        
        var json = System.Text.Json.JsonSerializer.Serialize(dto);
        _redisDbMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(json);

        // Act
        var result = await _providerService.GetProviderByIdAsync(providerId);

        // Assert
        result.Should().NotBeNull();
        result.ProviderId.Should().Be(providerId);
        _redisDbMock.Verify(x => x.StringGetAsync(It.Is<RedisKey>(k => k.ToString().Contains(providerId.ToString())), It.IsAny<CommandFlags>()), Times.Once);
    }
}
