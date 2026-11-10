using BillMitra.Application.Common.Interfaces;
using BillMitra.Application.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace BillMitra.UnitTests;

public class SystemHealthServiceTests
{
    private readonly Mock<IDbConnectionFactory> _mockDbFactory = new();

    [Fact]
    public async Task GetHealthStatusAsync_WhenDatabaseConnects_ReturnsHealthy()
    {
        _mockDbFactory.Setup(f => f.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockDbFactory.Setup(f => f.ProviderName).Returns("PostgreSQL");

        var service = new SystemHealthService(_mockDbFactory.Object);

        var health = await service.GetHealthStatusAsync();

        health.Status.Should().Be("Healthy");
        health.DatabaseConnected.Should().BeTrue();
        health.DatabaseProvider.Should().Be("PostgreSQL");
    }

    [Fact]
    public async Task GetHealthStatusAsync_WhenDatabaseFails_ReturnsDegraded()
    {
        _mockDbFactory.Setup(f => f.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockDbFactory.Setup(f => f.ProviderName).Returns("PostgreSQL");

        var service = new SystemHealthService(_mockDbFactory.Object);

        var health = await service.GetHealthStatusAsync();

        health.Status.Should().Be("Degraded");
        health.DatabaseConnected.Should().BeFalse();
    }
}

