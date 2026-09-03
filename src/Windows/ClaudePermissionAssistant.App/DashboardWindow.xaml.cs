using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using ClaudePermissionAssistant.App.Models;
using ClaudePermissionAssistant.App.Services;

namespace ClaudePermissionAssistant.App;

public partial class DashboardWindow : Window
{
    private readonly FileLoggingService _loggingService;
    private readonly TerminalFilterService _terminalFilter;
    private readonly ObservableCollection<MonitoredTerminalEntry> _monitoredTerminals = new();
    private readonly ApprovalStatistics _aggregateStatistics = new();

    public DashboardWindow(
        FileLoggingService loggingService)
    {
        InitializeComponent();

        _loggingService = loggingService;
        _terminalFilter = new TerminalFilterService();

        TerminalListBox.ItemsSource = _monitoredTerminals;
        UpdateEmptyState();

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionTextBlock.Text = $"v{version?.Major}.{version?.Minor}.{version?.Build}";

    }

    private void AddTerminalButton_Click(object sender, RoutedEventArgs e)
    {
        var selectWindow = new TerminalSelectWindow(_terminalFilter);
        selectWindow.Owner = this;

        if (selectWindow.ShowDialog() == true && selectWindow.SelectedTerminal != null)
        {
            var terminal = selectWindow.SelectedTerminal;

            if (_monitoredTerminals.Any(m => m.Terminal.WindowInfo.ProcessId == terminal.WindowInfo.ProcessId))
            {
                MessageBox.Show(
                    "This terminal is already being monitored.",
                    "Already Monitoring",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var statistics = new ApprovalStatistics();
            var monitorService = new BackgroundMonitorService(_loggingService, statistics);

            monitorService.StatisticsUpdated += (s, args) => Dispatcher.Invoke(UpdateAggregateStatistics);
            monitorService.ErrorOccurred += (s, error) => Dispatcher.Invoke(() =>
            {
                LastErrorTextBlock.Text = error;
                if (error.Contains("disconnected", StringComparison.OrdinalIgnoreCase))
                {
                    RemoveDisconnectedTerminal(terminal.WindowInfo.ProcessId);
                }
            });

            try
            {
                monitorService.Start(terminal);
            }
            catch (Exception ex)
            {
                monitorService.Dispose();
                MessageBox.Show(
                    $"Failed to start monitoring: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            var entry = new MonitoredTerminalEntry
            {
                Terminal = terminal,
                MonitorService = monitorService,
                Statistics = statistics
            };

            _monitoredTerminals.Add(entry);
            UpdateEmptyState();
            UpdateAggregateStatistics();

            // Give focus back to the terminal so existing prompts can be approved immediately
            SetForegroundWindow(terminal.WindowInfo.WindowHandle);
            this.WindowState = WindowState.Minimized;
        }
    }

    private void RemoveTerminalButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.Tag is MonitoredTerminalEntry entry)
        {
            entry.MonitorService.Stop();
            entry.MonitorService.Dispose();
            _monitoredTerminals.Remove(entry);
            UpdateEmptyState();
            UpdateAggregateStatistics();
        }
    }

    private void StopAllButton_Click(object sender, RoutedEventArgs e)
    {
        foreach (var entry in _monitoredTerminals.ToList())
        {
            entry.MonitorService.Stop();
            entry.MonitorService.Dispose();
        }
        _monitoredTerminals.Clear();
        UpdateEmptyState();
        UpdateAggregateStatistics();
    }

    private void RemoveDisconnectedTerminal(int processId)
    {
        var entry = _monitoredTerminals.FirstOrDefault(m => m.Terminal.WindowInfo.ProcessId == processId);
        if (entry != null)
        {
            entry.MonitorService.Dispose();
            _monitoredTerminals.Remove(entry);
            UpdateEmptyState();
            UpdateAggregateStatistics();
        }
    }

    private void UpdateEmptyState()
    {
        NoTerminalsTextBlock.Visibility = _monitoredTerminals.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        StopAllButton.IsEnabled = _monitoredTerminals.Count > 0;
    }

    private void UpdateAggregateStatistics()
    {
        int detected = 0, approved = 0, failed = 0;
        DateTime? lastApproval = null;
        string? lastApprovalDetails = null;

        foreach (var entry in _monitoredTerminals)
        {
            detected += entry.Statistics.PromptsDetected;
            approved += entry.Statistics.PromptsApproved;
            failed += entry.Statistics.PromptsFailed;

            if (entry.Statistics.LastApproval.HasValue &&
                (!lastApproval.HasValue || entry.Statistics.LastApproval > lastApproval))
            {
                lastApproval = entry.Statistics.LastApproval;
                lastApprovalDetails = entry.Statistics.LastApprovalDetails;
            }
        }

        PromptsDetectedTextBlock.Text = detected.ToString();
        PromptsApprovedTextBlock.Text = approved.ToString();
        PromptsFailedTextBlock.Text = failed.ToString();

        LastApprovalTextBlock.Text = lastApproval.HasValue
            ? $"{lastApproval.Value:HH:mm:ss} - {lastApprovalDetails ?? "Unknown"}"
            : "Never";

        // Refresh list to update status colors
        TerminalListBox.Items.Refresh();
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        e.Cancel = false;

        foreach (var entry in _monitoredTerminals)
        {
            entry.MonitorService.Stop();
            entry.MonitorService.Dispose();
        }
        _monitoredTerminals.Clear();
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
}

public class MonitoredTerminalEntry
{
    public required TerminalCandidate Terminal { get; init; }
    public required BackgroundMonitorService MonitorService { get; init; }
    public required ApprovalStatistics Statistics { get; init; }

    public string DisplayName => Terminal.DisplayName;

    public string Details =>
        $"PID: {Terminal.WindowInfo.ProcessId} | {Terminal.WindowInfo.WindowTitle}";

    // PHASE 2/3: Health status display
    public Brush StatusColor
    {
        get
        {
            if (!MonitorService.IsRunning)
                return Brushes.Gray;

            var health = MonitorService.HealthMetrics.HealthStatus;
            return health switch
            {
                TerminalHealthStatus.Healthy => Brushes.LimeGreen,
                TerminalHealthStatus.Warning => Brushes.Orange,
                TerminalHealthStatus.Degraded => Brushes.OrangeRed,
                TerminalHealthStatus.Critical => Brushes.Red,
                _ => Brushes.Gray
            };
        }
    }

    // PHASE 2/3: Detailed health info
    public string HealthInfo
    {
        get
        {
            if (!MonitorService.IsRunning)
                return "Stopped";

            var metrics = MonitorService.HealthMetrics;
            return $"{metrics.HealthStatus} | Detection: {metrics.DetectionSuccessRate:F1}% | " +
                   $"Approval: {metrics.ApprovalSuccessRate:F1}% | Cache: {metrics.CacheHitRate:F1}%";
        }
    }

    // PHASE 3: Detailed diagnostics
    public string DiagnosticsInfo
    {
        get
        {
            if (!MonitorService.IsRunning)
                return "";

            var metrics = MonitorService.HealthMetrics;
            return $"Extractions: {metrics.SuccessfulTextExtractions}/{metrics.TotalTextExtractionAttempts} | " +
                   $"Cache Size: {metrics.CurrentCacheSize} | " +
                   $"Recoveries: {metrics.RecoveryTriggersTotal} | " +
                   $"Uptime: {metrics.TotalMonitoringTime:hh\\:mm\\:ss}";
        }
    }
}
