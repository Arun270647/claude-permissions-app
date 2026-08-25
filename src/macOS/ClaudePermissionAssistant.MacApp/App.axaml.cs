using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ClaudePermissionAssistant.Core.Services;

namespace ClaudePermissionAssistant.MacApp;

public partial class App : Application
{
    private AutoUpdateService? _autoUpdateService;
    private const string CURRENT_VERSION = "1.0.1"; // Update this for each release

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // Initialize auto-update service (silent updates)
        _autoUpdateService = new AutoUpdateService(CURRENT_VERSION, "macos");
        _autoUpdateService.UpdateAvailable += OnUpdateAvailable;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
            desktop.Exit += (_, _) => _autoUpdateService?.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async void OnUpdateAvailable(object? sender, UpdateAvailableEventArgs e)
    {
        // Silent update - no prompts, just download and install
        await _autoUpdateService!.DownloadAndApplyUpdateAsync(e.UpdateInfo);
        // App will automatically exit and restart after update
    }
}
