using BillMitra.Api.Middleware;
using BillMitra.Application;
using BillMitra.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog structured logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "BillMitra Local API", Version = "v1", Description = "GST Billing & Business Management System Engine" });
});

// Configure CORS for desktop shell & local WebView2
builder.Services.AddCors(options =>
{
    options.AddPolicy("DesktopLocalPolicy", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
            {
                var uri = new Uri(origin);
                return uri.Host == "localhost" || uri.Host == "127.0.0.1";
            })
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Register Application & Infrastructure
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure Middleware Pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("EnableSwagger", true))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BillMitra API v1");
    });
}

app.UseCors("DesktopLocalPolicy");

// Enable serving Angular SPA if static files exist in wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();

app.MapControllers();

// Fallback to Angular index.html for SPA client-side routing
app.MapFallbackToFile("index.html");

Log.Information("Starting BillMitra Local API on {Urls}...", builder.Configuration["Urls"] ?? "http://127.0.0.1:5000");

app.Run();

// Required for WebApplicationFactory in integration tests
public partial class Program { }
