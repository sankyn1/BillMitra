using BillMitra.Application.Common.Interfaces;
using BillMitra.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BillMitra.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ISystemHealthService, SystemHealthService>();
        return services;
    }
}
