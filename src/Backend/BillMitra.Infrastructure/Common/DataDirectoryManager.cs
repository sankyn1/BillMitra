namespace BillMitra.Infrastructure.Common;

public static class DataDirectoryManager
{
    private static readonly string RootFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "BillMitra");

    public static string DatabasePath => Path.Combine(RootFolder, "Database");
    public static string LogsPath => Path.Combine(RootFolder, "Logs");
    public static string BackupsPath => Path.Combine(RootFolder, "Backups");
    public static string LocalSqliteDbFile => Path.Combine(DatabasePath, "billmitra_local.db");

    public static void EnsureDirectoriesExist()
    {
        try
        {
            Directory.CreateDirectory(RootFolder);
            Directory.CreateDirectory(DatabasePath);
            Directory.CreateDirectory(LogsPath);
            Directory.CreateDirectory(BackupsPath);
        }
        catch
        {
            // Fallback to local app data if ProgramData is restricted during development
            var localFallback = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BillMitra");
            Directory.CreateDirectory(localFallback);
            Directory.CreateDirectory(Path.Combine(localFallback, "Database"));
            Directory.CreateDirectory(Path.Combine(localFallback, "Logs"));
            Directory.CreateDirectory(Path.Combine(localFallback, "Backups"));
        }
    }
}
