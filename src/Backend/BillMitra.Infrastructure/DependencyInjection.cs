using BillMitra.Application.Common.Interfaces;
using BillMitra.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BillMitra.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Host=localhost;Port=5432;Database=billmitra_dev;Username=postgres;Password=postgres;";

        // Dapper connection factory for high-performance runtime access
        services.AddSingleton<IDbConnectionFactory, PostgreSqlDbConnectionFactory>();

        // EF Core strictly for migrations / design-time tooling
        services.AddDbContext<BillMitraDbContext>(options =>
        {
            options.UseNpgsql(connectionString, b => b.MigrationsAssembly(typeof(BillMitraDbContext).Assembly.FullName));
        });

        return services;
    }
}

