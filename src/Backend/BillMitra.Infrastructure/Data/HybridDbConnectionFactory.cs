using System.Data;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Infrastructure.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace BillMitra.Infrastructure.Data;

public class HybridDbConnectionFactory : IDbConnectionFactory
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<HybridDbConnectionFactory> _logger;
    private string _activeProvider = "SqlServer";
    private string _activeConnectionString = string.Empty;
    private readonly object _lock = new();
    private bool _initialized = false;

    public string ProviderName => _activeProvider;

    public HybridDbConnectionFactory(IConfiguration configuration, ILogger<HybridDbConnectionFactory> logger)
    {
        _configuration = configuration;
        _logger = logger;
        DataDirectoryManager.EnsureDirectoriesExist();
        DetermineInitialConfiguration();
    }

    private void DetermineInitialConfiguration()
    {
        var configuredProvider = _configuration["Database:Provider"];
        if (string.Equals(configuredProvider, "SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            _activeProvider = "SqlServer";
            _activeConnectionString = _configuration.GetConnectionString("SqlServer") 
                ?? "Server=.\\SQLEXPRESS;Database=billmitra_dev;Trusted_Connection=True;TrustServerCertificate=True;";
            return;
        }

        if (string.Equals(configuredProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            _activeProvider = "Sqlite";
            _activeConnectionString = $"Data Source={DataDirectoryManager.LocalSqliteDbFile}";
            return;
        }

        if (string.Equals(configuredProvider, "PostgreSQL", StringComparison.OrdinalIgnoreCase))
        {
            _activeProvider = "PostgreSQL";
            _activeConnectionString = _configuration.GetConnectionString("DefaultConnection") 
                ?? "Host=localhost;Port=5432;Database=billmitra_dev;Username=postgres;Password=postgres;";
            return;
        }

        // By default on Windows developer workstation: test SQL Server (SQLEXPRESS) first, then SQLite
        var sqlServerConn = _configuration.GetConnectionString("SqlServer")
            ?? "Server=.\\SQLEXPRESS;Database=billmitra_dev;Trusted_Connection=True;TrustServerCertificate=True;";
        _activeProvider = "SqlServer";
        _activeConnectionString = sqlServerConn;
    }

    public IDbConnection CreateConnection()
    {
        lock (_lock)
        {
            return _activeProvider switch
            {
                "SqlServer" => new SqlConnection(_activeConnectionString),
                "Sqlite" => new SqliteConnection(_activeConnectionString),
                _ => new NpgsqlConnection(_activeConnectionString)
            };
        }
    }

    public async Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        // Try active provider
        try
        {
            var connection = CreateConnection();
            if (connection is System.Data.Common.DbConnection dbConn)
            {
                await dbConn.OpenAsync(cancellationToken);
            }
            else
            {
                connection.Open();
            }
            return connection;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Connection failed with provider {Provider}: {ErrorMessage}. Attempting fallback.", _activeProvider, ex.Message);

            lock (_lock)
            {
                if (_activeProvider != "Sqlite")
                {
                    _activeProvider = "Sqlite";
                    _activeConnectionString = $"Data Source={DataDirectoryManager.LocalSqliteDbFile}";
                }
            }

            var fallbackConnection = CreateConnection();
            if (fallbackConnection is System.Data.Common.DbConnection dbFallback)
            {
                await dbFallback.OpenAsync(cancellationToken);
            }
            else
            {
                fallbackConnection.Open();
            }
            return fallbackConnection;
        }
    }

    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await CreateConnectionAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Database connection probe failed: {ErrorMessage}", ex.Message);
            return false;
        }
    }
}
