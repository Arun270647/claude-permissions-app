using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using ClaudePermissionAssistant.Automation.Services;
using ClaudePermissionAssistant.Core.Models;
using Microsoft.Win32;

namespace ClaudePermissionAssistant.App;

public partial class MainWindow : Window
{
    private readonly WindowInspectorService _inspectorService;
    private InspectionResult? _currentInspection;

    public MainWindow()
    {
        InitializeComponent();
        _inspectorService = new WindowInspectorService();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        RefreshWindowList();
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshWindowList();
    }

    private void RefreshWindowList()
    {
        try
        {
            StatusTextBlock.Text = "Loading windows...";
            var windows = _inspectorService.GetAllWindows();

            var windowViewModels = windows.Select(w => new WindowViewModel
            {
                Window = w,
                DisplayText = $"{w.ProcessName} - {w.WindowTitle} (PID: {w.ProcessId})"
            }).ToList();

            WindowComboBox.ItemsSource = windowViewModels;

            if (windowViewModels.Count > 0)
            {
                WindowComboBox.SelectedIndex = 0;
                StatusTextBlock.Text = $"Found {windowViewModels.Count} window(s). Select one and click Inspect.";
            }
            else
            {
                StatusTextBlock.Text = "No windows found.";
            }
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Error loading windows: {ex.Message}";
            MessageBox.Show($"Error loading windows: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void InspectButton_Click(object sender, RoutedEventArgs e)
    {
        if (WindowComboBox.SelectedItem is not WindowViewModel selected)
        {
            MessageBox.Show("Please select a window to inspect.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            StatusTextBlock.Text = $"Inspecting window: {selected.Window.WindowTitle}...";
            AutomationTreeView.Items.Clear();
            PropertiesTextBlock.Text = string.Empty;
            ExportButton.IsEnabled = false;

            _currentInspection = _inspectorService.InspectWindow(selected.Window.WindowHandle);

            if (_currentInspection.Success && _currentInspection.RootElement != null)
            {
                AutomationTreeView.Items.Add(_currentInspection.RootElement);
                StatusTextBlock.Text = $"Inspection complete. Total elements: {_currentInspection.TotalElements}";
                ExportButton.IsEnabled = true;
            }
            else
            {
                StatusTextBlock.Text = $"Inspection failed: {_currentInspection.ErrorMessage}";
                MessageBox.Show($"Inspection failed: {_currentInspection.ErrorMessage}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Error during inspection: {ex.Message}";
            MessageBox.Show($"Error during inspection: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AutomationTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is AutomationElementInfo element)
        {
            DisplayElementProperties(element);
        }
    }

    private void DisplayElementProperties(AutomationElementInfo element)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Control Type: {element.ControlType}");
        sb.AppendLine($"Name: {element.Name}");
        sb.AppendLine($"AutomationId: {element.AutomationId}");
        sb.AppendLine($"ClassName: {element.ClassName}");
        sb.AppendLine($"RuntimeId: {element.RuntimeId}");
        sb.AppendLine($"ProcessId: {element.ProcessId}");
        sb.AppendLine();
        sb.AppendLine($"IsEnabled: {element.IsEnabled}");
        sb.AppendLine($"IsOffscreen: {element.IsOffscreen}");
        sb.AppendLine();
        sb.AppendLine($"BoundingRectangle: {element.BoundingRectangle}");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(element.AcceleratorKey))
            sb.AppendLine($"AcceleratorKey: {element.AcceleratorKey}");
        if (!string.IsNullOrEmpty(element.AccessKey))
            sb.AppendLine($"AccessKey: {element.AccessKey}");
        if (!string.IsNullOrEmpty(element.HelpText))
            sb.AppendLine($"HelpText: {element.HelpText}");
        if (!string.IsNullOrEmpty(element.ItemStatus))
            sb.AppendLine($"ItemStatus: {element.ItemStatus}");
        if (!string.IsNullOrEmpty(element.ItemType))
            sb.AppendLine($"ItemType: {element.ItemType}");

        if (element.SupportedPatterns.Length > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Supported Patterns:");
            foreach (var pattern in element.SupportedPatterns)
            {
                sb.AppendLine($"  - {pattern}");
            }
        }

        // Add TextPattern information if applicable
        if (element.TextPatternSupported)
        {
            sb.AppendLine();
            sb.AppendLine("TextPattern:");
            sb.AppendLine($"  Supported: true");

            if (element.ExtractedTextLength.HasValue)
            {
                sb.AppendLine($"  TextLength: {element.ExtractedTextLength.Value}");
            }

            if (!string.IsNullOrEmpty(element.TextExtractionError))
            {
                sb.AppendLine($"  ExtractionError: {element.TextExtractionError}");
            }

            if (!string.IsNullOrEmpty(element.ExtractedText))
            {
                const int MaxDisplayLength = 2000; // Truncate for UI display
                sb.AppendLine($"  ExtractedText:");

                if (element.ExtractedText.Length <= MaxDisplayLength)
                {
                    sb.AppendLine($"  --- BEGIN TEXT ---");
                    sb.AppendLine(element.ExtractedText);
                    sb.AppendLine($"  --- END TEXT ---");
                }
                else
                {
                    sb.AppendLine($"  --- BEGIN TEXT (truncated, showing first {MaxDisplayLength} chars) ---");
                    sb.AppendLine(element.ExtractedText.Substring(0, MaxDisplayLength));
                    sb.AppendLine($"  ... (truncated, full text in export)");
                    sb.AppendLine($"  --- END TEXT ---");
                }
            }
        }

        sb.AppendLine();
        sb.AppendLine($"Depth: {element.Depth}");
        sb.AppendLine($"Children: {element.Children.Count}");

        PropertiesTextBlock.Text = sb.ToString();
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentInspection == null || !_currentInspection.Success)
        {
            MessageBox.Show("No inspection data to export.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                DefaultExt = "txt",
                FileName = $"UIAutomation_{_currentInspection.Window.ProcessName}_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };

            if (saveDialog.ShowDialog() == true)
            {
                var exportText = _inspectorService.ExportTreeToText(_currentInspection);
                File.WriteAllText(saveDialog.FileName, exportText);
                StatusTextBlock.Text = $"Exported to: {saveDialog.FileName}";
                MessageBox.Show($"Successfully exported to:\n{saveDialog.FileName}", "Export Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Export failed: {ex.Message}";
            MessageBox.Show($"Export failed: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private class WindowViewModel
    {
        public required WindowInfo Window { get; init; }
        public required string DisplayText { get; init; }
    }
}