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

    private const string CURRENT_VERSION = "1.0.1";

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Single instance check
        _singleInstanceManager = new SingleInstanceManager();

        if (!_singleInstanceManager.IsFirstInstance)
        {
            MessageBox.Show(
                "Claude Prompter is already running.\n\n" +
                "Check the system tray for the application icon.",
                "Already Running",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );

            Shutdown();
            return;
        }

        // Initialize auto-update service
        _autoUpdateService = new AutoUpdateService(CURRENT_VERSION, "windows");

        // Check for mandatory updates before allowing app usage
        await CheckMandatoryUpdate();

        // Initialize tray application
        _trayContext = new TrayApplicationContext();

        // Show dashboard on first launch
        _trayContext.ShowDashboard();

        // Subscribe to future update checks (background)
        _autoUpdateService.UpdateAvailable += OnUpdateAvailable;
    }

    private async Task CheckMandatoryUpdate()
    {
        try
        {
            var updateInfo = await _autoUpdateService!.CheckForUpdatesAsync();

            if (updateInfo != null && updateInfo.Mandatory)
            {
                // Show mandatory update dialog - user cannot skip
                var updateWindow = new UpdateWindow(updateInfo, _autoUpdateService);
                updateWindow.ShowDialog();

                // If the window was closed without updating (shouldn't happen with mandatory), exit
                if (!updateWindow.UpdateApplied)
                {
                    Shutdown();
                    return;
                }
            }
        }
        catch
        {
            // If update check fails (no internet), allow the app to run
        }
    }

    private void OnUpdateAvailable(object? sender, UpdateAvailableEventArgs e)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var updateWindow = new UpdateWindow(e.UpdateInfo, _autoUpdateService!);
            updateWindow.ShowDialog();

            if (!updateWindow.UpdateApplied && e.UpdateInfo.Mandatory)
            {
                Shutdown();
            }
        });
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _autoUpdateService?.Dispose();
        _trayContext?.Dispose();
        _singleInstanceManager?.Dispose();
        base.OnExit(e);
    }
}
