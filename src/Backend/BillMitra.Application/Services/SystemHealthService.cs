using System.Diagnostics;
using BillMitra.Application.Common.Interfaces;
using BillMitra.Contracts.System;

namespace BillMitra.Application.Services;

public class SystemHealthService : ISystemHealthService
{
    private static readonly DateTime ProcessStartTime = DateTime.UtcNow;
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public SystemHealthService(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<SystemHealthDto> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        var dbConnected = await _dbConnectionFactory.CanConnectAsync(cancellationToken);

        return new SystemHealthDto
        {
            Status = dbConnected ? "Healthy" : "Degraded",
            AppName = "BillMitra",
            Version = "0.1.0-alpha",
            Environment = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            DatabaseConnected = dbConnected,
            DatabaseProvider = _dbConnectionFactory.ProviderName,
            ServerTimeUtc = DateTime.UtcNow,
            Uptime = DateTime.UtcNow - ProcessStartTime
        };
    }
}

