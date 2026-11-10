using System.Data;
using BillMitra.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace BillMitra.Infrastructure.Data;

public class PostgreSqlDbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;
    private readonly ILogger<PostgreSqlDbConnectionFactory> _logger;

    public string ProviderName => "PostgreSQL";

    public PostgreSqlDbConnectionFactory(IConfiguration configuration, ILogger<PostgreSqlDbConnectionFactory> logger)
    {
        _logger = logger;
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Host=localhost;Port=5432;Database=billmitra_dev;Username=postgres;Password=postgres;";
    }

    public IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    public async Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(2)); // Rapid timeout for health checks
            await connection.OpenAsync(cts.Token);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Database connection probe failed: {ErrorMessage}", ex.Message);
            return false;
        }
    }
}

