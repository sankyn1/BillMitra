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
        services.AddScoped<IMasterService, MasterService>();
        services.AddScoped<ISalesService, SalesService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IGstReturnService, GstReturnService>();
        services.AddScoped<IEWayBillService, EWayBillService>();
        services.AddScoped<IEInvoiceService, EInvoiceService>();
        services.AddScoped<IPurchaseService, PurchaseService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<IWarehouseService, WarehouseService>();
        services.AddScoped<IInventoryService, InventoryService>();

        return services;
    }
}
