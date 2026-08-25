using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ClaudePermissionAssistant.Core.Services;

namespace ClaudePermissionAssistant.MacApp;

public partial class UpdateWindow : Window
{
    private readonly UpdateInfo _updateInfo;
    private readonly AutoUpdateService _updateService;

    public bool UpdateApplied { get; private set; }

    public UpdateWindow(UpdateInfo updateInfo, AutoUpdateService updateService, string patchNotes)
    {
        InitializeComponent();

        _updateInfo = updateInfo;
        _updateService = updateService;

        VersionTextBlock.Text = $"Version {updateInfo.Version} is available";
        PatchNotesTextBlock.Text = patchNotes;

        _updateService.UpdateProgress += OnUpdateProgress;

        // Prevent closing without updating
        Closing += (_, e) =>
        {
            if (!UpdateApplied)
            {
                e.Cancel = true;
            }
        };
    }

    private void OnUpdateProgress(object? sender, UpdateProgressEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            StatusTextBlock.Text = e.Message;
            UpdateProgressBar.Value = e.ProgressPercent;
        });
    }

    private async void UpdateButton_Click(object? sender, RoutedEventArgs e)
    {
        UpdateButton.IsEnabled = false;
        ExitButton.IsEnabled = false;
        UpdateButton.Content = "Downloading...";
        StatusTextBlock.Text = "Starting download...";

        var success = await _updateService.DownloadAndApplyUpdateAsync(_updateInfo);

        if (success)
        {
            UpdateApplied = true;
        }
        else
        {
            StatusTextBlock.Text = "Update failed. Please try again or download manually.";
            UpdateButton.IsEnabled = true;
            ExitButton.IsEnabled = true;
            UpdateButton.Content = "Retry Update";
        }
    }

    private void ExitButton_Click(object? sender, RoutedEventArgs e)
    {
        UpdateApplied = false;
        Closing -= null!;
        Close();
    }
}
