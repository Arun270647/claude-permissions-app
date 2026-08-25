using System.Windows;
using ClaudePermissionAssistant.App.Services;
using ClaudePermissionAssistant.Core.Services;

namespace ClaudePermissionAssistant.App;

/// <summary>
/// System tray application entry point
/// </summary>
public partial class App : System.Windows.Application
{
    private SingleInstanceManager? _singleInstanceManager;
    private TrayApplicationContext? _trayContext;
    private AutoUpdateService? _autoUpdateService;

    private const string CURRENT_VERSION = "1.0.0"; // Update this for each release

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Single instance check
        _singleInstanceManager = new SingleInstanceManager();

        if (!_singleInstanceManager.IsFirstInstance)
        {
            MessageBox.Show(
                "Claude Permission Assistant is already running.\n\n" +
                "Check the system tray for the application icon.",
                "Already Running",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );

            Shutdown();
            return;
        }

        // Initialize auto-update service (silent updates)
        _autoUpdateService = new AutoUpdateService(CURRENT_VERSION, "windows");
        _autoUpdateService.UpdateAvailable += OnUpdateAvailable;

        // Initialize tray application
        _trayContext = new TrayApplicationContext();

        // Show dashboard on first launch
        _trayContext.ShowDashboard();
    }

    private async void OnUpdateAvailable(object? sender, UpdateAvailableEventArgs e)
    {
        // Silent update - no prompts, just download and install
        await _autoUpdateService!.DownloadAndApplyUpdateAsync(e.UpdateInfo);
        // App will automatically exit and restart after update
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _autoUpdateService?.Dispose();
        _trayContext?.Dispose();
        _singleInstanceManager?.Dispose();
        base.OnExit(e);
    }
}

