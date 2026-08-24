using System.Windows;
using ClaudePermissionAssistant.App.Services;

namespace ClaudePermissionAssistant.App;

/// <summary>
/// System tray application entry point
/// </summary>
public partial class App : System.Windows.Application
{
    private SingleInstanceManager? _singleInstanceManager;
    private TrayApplicationContext? _trayContext;

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

        // Initialize tray application
        _trayContext = new TrayApplicationContext();

        // Show dashboard on first launch
        _trayContext.ShowDashboard();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayContext?.Dispose();
        _singleInstanceManager?.Dispose();
        base.OnExit(e);
    }
}

