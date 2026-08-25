using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ClaudePermissionAssistant.Core.Services;

namespace ClaudePermissionAssistant.MacApp;

public partial class App : Application
{
    private AutoUpdateService? _autoUpdateService;
    private const string CURRENT_VERSION = "1.0.1";

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        _autoUpdateService = new AutoUpdateService(CURRENT_VERSION, "macos");
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
            desktop.Exit += (_, _) => _autoUpdateService?.Dispose();

            // Check for mandatory updates after window is shown
            Dispatcher.UIThread.Post(async () => await CheckMandatoryUpdate(desktop));
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task CheckMandatoryUpdate(IClassicDesktopStyleApplicationLifetime desktop)
    {
        try
        {
            var updateInfo = await _autoUpdateService!.CheckForUpdatesAsync();

            if (updateInfo != null && updateInfo.Mandatory)
            {
                var patchNotes = !string.IsNullOrWhiteSpace(updateInfo.PatchNotes)
                    ? updateInfo.PatchNotes
                    : $"Claude Prompter v{updateInfo.Version}\n\n" +
                      "• Performance improvements and bug fixes\n" +
                      "• Enhanced stability across all platforms\n" +
                      "• Updated auto-update system";

                var updateWindow = new UpdateWindow(updateInfo, _autoUpdateService, patchNotes);
                await updateWindow.ShowDialog(desktop.MainWindow!);

                if (!updateWindow.UpdateApplied)
                {
                    desktop.Shutdown();
                }
            }
            else
            {
                // Subscribe to background checks for non-blocking updates
                _autoUpdateService!.UpdateAvailable += OnUpdateAvailable;
            }
        }
        catch
        {
            // If update check fails (no internet), allow usage
            _autoUpdateService!.UpdateAvailable += OnUpdateAvailable;
        }
    }

    private async void OnUpdateAvailable(object? sender, UpdateAvailableEventArgs e)
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                var patchNotes = !string.IsNullOrWhiteSpace(e.UpdateInfo.PatchNotes)
                    ? e.UpdateInfo.PatchNotes
                    : $"Claude Prompter v{e.UpdateInfo.Version}\n\n• Performance improvements and bug fixes";

                var updateWindow = new UpdateWindow(e.UpdateInfo, _autoUpdateService!, patchNotes);
                await updateWindow.ShowDialog(desktop.MainWindow);

                if (!updateWindow.UpdateApplied && e.UpdateInfo.Mandatory)
                {
                    desktop.Shutdown();
                }
            }
        });
    }
}
