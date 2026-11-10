using BillMitra.Application.Common.Interfaces;
using BillMitra.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BillMitra.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ISystemHealthService, SystemHealthService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ISetupService, SetupService>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<IFinancialYearService, FinancialYearService>();

        return services;
    }
}
