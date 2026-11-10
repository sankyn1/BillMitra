using BillMitra.Application.Common.Interfaces;
using BillMitra.Infrastructure.Data;
using BillMitra.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BillMitra.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Hybrid Connection Factory supporting PostgreSQL, SQL Server (Local Express), and SQLite
        services.AddSingleton<HybridDbConnectionFactory>();
        services.AddSingleton<IDbConnectionFactory>(sp => sp.GetRequiredService<HybridDbConnectionFactory>());
        services.AddSingleton<DatabaseInitializer>();

        // EF Core Registration for migrations
        var postgresConnection = configuration.GetConnectionString("DefaultConnection") 
            ?? "Host=localhost;Port=5432;Database=billmitra_dev;Username=postgres;Password=postgres;";

        services.AddDbContext<BillMitraDbContext>(options =>
        {
            options.UseNpgsql(postgresConnection, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(BillMitraDbContext).Assembly.FullName);
            });
        });

        // Repositories
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IFinancialYearRepository, FinancialYearRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IPartyRepository, PartyRepository>();
        services.AddScoped<ISalesDocumentRepository, SalesDocumentRepository>();
        services.AddScoped<IDocumentSequenceRepository, DocumentSequenceRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IGstReturnRepository, GstReturnRepository>();
        services.AddScoped<IEWayBillRepository, EWayBillRepository>();
        services.AddScoped<IEInvoiceRepository, EInvoiceRepository>();
        services.AddScoped<IPurchaseRepository, PurchaseRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IGstReturnProvider, Services.SandboxGstReturnProvider>();
        services.AddScoped<IEWayBillProvider, Services.SandboxEWayBillProvider>();
        services.AddScoped<IEInvoiceProvider, Services.SandboxEInvoiceProvider>();
        services.AddScoped<IPdfGeneratorService, Services.QuestPdfGeneratorService>();

        return services;
    }
}
