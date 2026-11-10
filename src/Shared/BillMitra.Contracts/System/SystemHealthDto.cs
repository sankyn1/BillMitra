namespace BillMitra.Contracts.System;

public class SystemHealthDto
{
    public string Status { get; set; } = "Healthy";
    public string AppName { get; set; } = "BillMitra";
    public string Version { get; set; } = "0.1.0-alpha";
    public string Environment { get; set; } = "Development";
    public bool DatabaseConnected { get; set; }
    public string DatabaseProvider { get; set; } = "PostgreSQL";
    public DateTime ServerTimeUtc { get; set; } = DateTime.UtcNow;
    public TimeSpan Uptime { get; set; }
}
