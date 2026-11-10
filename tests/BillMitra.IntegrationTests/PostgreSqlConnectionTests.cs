using BillMitra.Infrastructure.Data;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BillMitra.IntegrationTests;

public class PostgreSqlConnectionTests
{
    [Fact]
    public void Factory_ShouldInitializeWithDefaultConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=billmitra_dev;Username=postgres;Password=postgres;"
            })
            .Build();

        var factory = new PostgreSqlDbConnectionFactory(config, NullLogger<PostgreSqlDbConnectionFactory>.Instance);

        factory.ProviderName.Should().Be("PostgreSQL");
        var connection = factory.CreateConnection();
        connection.Should().NotBeNull();
        connection.ConnectionString.Should().Contain("billmitra_dev");
    }

    [Fact]
    public async Task CanConnectAsync_WhenServerNotRunning_ReturnsFalseWithoutThrowingUnhandled()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Unreachable port
                ["ConnectionStrings:DefaultConnection"] = "Host=127.0.0.1;Port=59999;Database=billmitra_dev;Username=postgres;Password=postgres;Timeout=1;"
            })
            .Build();

        var factory = new PostgreSqlDbConnectionFactory(config, NullLogger<PostgreSqlDbConnectionFactory>.Instance);

        var canConnect = await factory.CanConnectAsync();

        canConnect.Should().BeFalse("Unreachable database should report false gracefully.");
    }
}

