using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace BillMitra.Desktop;

public partial class MainWindow : Window
{
    private const string ApiBaseUrl = "http://127.0.0.1:5000";
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(2) };
    private Process? _backendProcess;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            StatusText.Text = "Verifying backend service...";
            await EnsureBackendRunningAsync();

            StatusText.Text = "Initializing desktop environment...";
            await InitializeWebViewAsync();

            StatusText.Text = "Loading BillMitra workspace...";
            AppWebView.Source = new Uri(ApiBaseUrl);
            AppWebView.NavigationCompleted += (s, args) =>
            {
                SplashOverlay.Visibility = Visibility.Collapsed;
                AppWebView.Visibility = Visibility.Visible;
            };
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Initialization error: {ex.Message}";
            MessageBox.Show(
                $"Failed to initialize BillMitra desktop application:\n\n{ex.Message}\n\nPlease verify WebView2 runtime and local prerequisites.",
                "BillMitra Initialization Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task EnsureBackendRunningAsync()
    {
        // Check if API is already running (e.g. as Windows service or dev process)
        if (await IsApiRespondingAsync())
        {
            return;
        }

        // Locate backend executable relative to desktop bin
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var backendExe = Path.Combine(baseDir, "Backend", "BillMitra.Api.exe");

        // Fallback for development debugging
        if (!File.Exists(backendExe))
        {
            backendExe = Path.Combine(baseDir, "..", "..", "..", "..", "Backend", "BillMitra.Api", "bin", "Debug", "net10.0", "BillMitra.Api.exe");
        }

        if (File.Exists(backendExe))
        {
            StatusText.Text = "Starting local BillMitra API engine...";
            var startInfo = new ProcessStartInfo
            {
                FileName = backendExe,
                WorkingDirectory = Path.GetDirectoryName(backendExe),
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _backendProcess = Process.Start(startInfo);

            // Poll for API readiness up to 15 seconds
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed < TimeSpan.FromSeconds(15))
            {
                if (await IsApiRespondingAsync())
                {
                    return;
                }
                await Task.Delay(500);
            }
        }
        else
        {
            // If backend exe not found on disk, continue anyway and let WebView display instructions or offline status
            Debug.WriteLine($"Backend binary not found at: {backendExe}");
        }
    }

    private async Task<bool> IsApiRespondingAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{ApiBaseUrl}/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task InitializeWebViewAsync()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var userDataFolder = Path.Combine(localAppData, "BillMitra", "WebView2Data");
        Directory.CreateDirectory(userDataFolder);

        var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
        await AppWebView.EnsureCoreWebView2Async(env);

        // Security settings
        AppWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
        AppWebView.CoreWebView2.Settings.AreDevToolsEnabled = true;
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Gracefully terminate child API process if spawned by Desktop Shell
        if (_backendProcess != null && !_backendProcess.HasExited)
        {
            try
            {
                _backendProcess.Kill(entireProcessTree: true);
                _backendProcess.Dispose();
            }
            catch
            {
                // Process already terminated
            }
        }
    }
}
