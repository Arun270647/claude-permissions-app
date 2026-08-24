using System.Windows;
using System.Windows.Controls;
using ClaudePermissionAssistant.App.Models;
using ClaudePermissionAssistant.App.Services;

namespace ClaudePermissionAssistant.App;

/// <summary>
/// System tray application context - manages tray icon and application lifecycle
/// Uses Hardcodet.NotifyIcon.Wpf for system tray integration
/// </summary>
public class TrayApplicationContext : IDisposable
{
    private readonly dynamic? _trayIcon; // TaskbarIcon from Hardcodet.NotifyIcon.Wpf
    private readonly FileLoggingService _loggingService;

    private DashboardWindow? _dashboardWindow;
    private bool _isRunning;

    public TrayApplicationContext()
    {
        _loggingService = new FileLoggingService();

        // Create tray icon using reflection to avoid namespace issues
        var assembly = System.Reflection.Assembly.Load("Hardcodet.NotifyIcon.Wpf");
        var taskbarIconType = assembly.GetTypes().FirstOrDefault(t => t.Name == "TaskbarIcon");

        if (taskbarIconType == null)
            throw new InvalidOperationException("TaskbarIcon type not found in Hardcodet.NotifyIcon.Wpf");

        _trayIcon = Activator.CreateInstance(taskbarIconType) ?? throw new InvalidOperationException("Failed to create TaskbarIcon instance");

        // Set properties
        taskbarIconType.GetProperty("ToolTipText")?.SetValue(_trayIcon, "Claude Permission Assistant\nStatus: Stopped");
        taskbarIconType.GetProperty("ContextMenu")?.SetValue(_trayIcon, CreateContextMenu());

        // Subscribe to double-click event
        var eventInfo = taskbarIconType.GetEvent("TrayMouseDoubleClick");
        if (eventInfo != null)
        {
            var handler = new Action<object?, EventArgs>((s, e) => ShowDashboard());
            var delegateType = eventInfo.EventHandlerType;
            var delegateMethod = Delegate.CreateDelegate(delegateType!, handler.Target, handler.Method);
            eventInfo.AddEventHandler(_trayIcon, delegateMethod);
        }

        _loggingService.LogInfo("APPLICATION_STARTED");
    }

    private ContextMenu CreateContextMenu()
    {
        var contextMenu = new ContextMenu();

        var openItem = new MenuItem { Header = "Open" };
        openItem.Click += (s, e) => ShowDashboard();

        var startItem = new MenuItem { Header = "Start", Name = "StartItem" };
        startItem.Click += (s, e) => ShowDashboard(); // User selects terminal in dashboard

        var stopItem = new MenuItem { Header = "Stop", Name = "StopItem", IsEnabled = false };
        stopItem.Click += (s, e) => StopMonitoring();

        var exitItem = new MenuItem { Header = "Exit" };
        exitItem.Click += (s, e) => ExitApplication();

        contextMenu.Items.Add(openItem);
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(startItem);
        contextMenu.Items.Add(stopItem);
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(exitItem);

        return contextMenu;
    }

    private void ExitApplication()
    {
        if (_isRunning)
        {
            var result = MessageBox.Show(
                "Monitoring is currently running. Are you sure you want to exit?",
                "Confirm Exit",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result != MessageBoxResult.Yes)
                return;
        }

        _loggingService.LogInfo("APPLICATION_EXIT");

        StopMonitoring();

        // Dispose tray icon
        if (_trayIcon is IDisposable disposable)
            disposable.Dispose();

        System.Windows.Application.Current.Shutdown();
    }

    public void ShowDashboard()
    {
        if (_dashboardWindow == null || !_dashboardWindow.IsLoaded)
        {
            _dashboardWindow = new DashboardWindow(_loggingService);
            _dashboardWindow.Closed += DashboardWindow_Closed;
            _dashboardWindow.Show();
        }
        else
        {
            _dashboardWindow.Activate();
            _dashboardWindow.WindowState = WindowState.Normal;
        }
    }

    private void DashboardWindow_Closed(object? sender, EventArgs e)
    {
        // Dashboard closing does NOT exit the application
        // Application continues running in tray
        _dashboardWindow = null;
    }

    private void StopMonitoring()
    {
        _isRunning = false;
        UpdateTrayStatus("Stopped");
        UpdateTrayMenuItems();
    }

    private void UpdateTrayStatus(string status)
    {
        var toolTipText = $"Claude Permission Assistant\nStatus: {status}";
        _trayIcon?.GetType().GetProperty("ToolTipText")?.SetValue(_trayIcon, toolTipText);
    }

    private void UpdateTrayMenuItems()
    {
        var contextMenu = _trayIcon?.GetType().GetProperty("ContextMenu")?.GetValue(_trayIcon) as ContextMenu;

        if (contextMenu == null)
            return;

        MenuItem? startItem = null;
        MenuItem? stopItem = null;

        foreach (var item in contextMenu.Items)
        {
            if (item is MenuItem menuItem)
            {
                if (menuItem.Name == "StartItem")
                    startItem = menuItem;
                else if (menuItem.Name == "StopItem")
                    stopItem = menuItem;
            }
        }

        if (startItem != null)
            startItem.IsEnabled = !_isRunning;

        if (stopItem != null)
            stopItem.IsEnabled = _isRunning;
    }

    public void Dispose()
    {
        if (_trayIcon is IDisposable disposable)
            disposable.Dispose();
    }
}
