using System.Windows;
using ClaudePermissionAssistant.Core.Services;

namespace ClaudePermissionAssistant.App;

public partial class UpdateWindow : Window
{
    private readonly UpdateInfo _updateInfo;
    private readonly AutoUpdateService _updateService;

    public bool UpdateApplied { get; private set; }

    public UpdateWindow(UpdateInfo updateInfo, AutoUpdateService updateService)
    {
        InitializeComponent();

        _updateInfo = updateInfo;
        _updateService = updateService;

        VersionTextBlock.Text = $"Version {updateInfo.Version} is available (you have v{GetCurrentVersion()})";

        // Display patch notes
        var patchNotes = !string.IsNullOrWhiteSpace(updateInfo.PatchNotes)
            ? updateInfo.PatchNotes
            : GetDefaultPatchNotes(updateInfo.Version);

        PatchNotesTextBlock.Text = patchNotes;

        // Subscribe to progress updates
        _updateService.UpdateProgress += OnUpdateProgress;

        // Prevent closing without updating (mandatory)
        if (updateInfo.Mandatory)
        {
            Closing += (_, e) =>
            {
                if (!UpdateApplied)
                {
                    e.Cancel = true;
                    MessageBox.Show(
                        "This update is mandatory. Please click 'Update Now' or 'Exit App' to close.",
                        "Update Required",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            };
        }
    }

    private string GetCurrentVersion()
    {
        return "1.0.1";
    }

    private string GetDefaultPatchNotes(string version)
    {
        return $"Claude Prompter v{version}\n\n" +
               "• Performance improvements and bug fixes\n" +
               "• Enhanced stability across all platforms\n" +
               "• Updated auto-update system\n\n" +
               "See full release notes on GitHub for details.";
    }

    private void OnUpdateProgress(object? sender, UpdateProgressEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            StatusTextBlock.Text = e.Message;
            UpdateProgressBar.Value = e.ProgressPercent;
        });
    }

    private async void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateButton.IsEnabled = false;
        ExitButton.IsEnabled = false;
        UpdateButton.Content = "Downloading...";
        StatusTextBlock.Text = "Starting download...";

        var success = await _updateService.DownloadAndApplyUpdateAsync(_updateInfo);

        if (success)
        {
            UpdateApplied = true;
            // App will exit and restart automatically
        }
        else
        {
            StatusTextBlock.Text = "Update failed. Please try again or download manually.";
            UpdateButton.IsEnabled = true;
            ExitButton.IsEnabled = true;
            UpdateButton.Content = "⬇️  Retry Update";
        }
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateApplied = false;
        // Allow close when exit button is clicked
        Closing -= null!;
        System.Windows.Application.Current.Shutdown();
    }
}
