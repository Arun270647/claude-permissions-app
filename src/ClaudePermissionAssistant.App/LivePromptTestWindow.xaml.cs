using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using ClaudePermissionAssistant.Automation.Services;
using ClaudePermissionAssistant.Core.Models;
using Microsoft.Win32;

namespace ClaudePermissionAssistant.App;

public partial class LivePromptTestWindow : Window
{
    private readonly WindowInspectorService _inspectorService;
    private readonly ClaudePromptParserSimple _parser;
    private List<WindowViewModel> _windows = new();
    private string? _extractedText;
    private PermissionRequest? _parsedRequest;

    public LivePromptTestWindow()
    {
        InitializeComponent();
        _inspectorService = new WindowInspectorService();
        _parser = new ClaudePromptParserSimple();
        Loaded += LivePromptTestWindow_Loaded;
    }

    private void LivePromptTestWindow_Loaded(object sender, RoutedEventArgs e)
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
            _windows = windows.Select(w => new WindowViewModel
            {
                Window = w,
                DisplayText = $"{w.ProcessName} (PID: {w.ProcessId}) - {w.WindowTitle}"
            }).ToList();

            WindowComboBox.ItemsSource = _windows;

            if (_windows.Count > 0)
            {
                StatusTextBlock.Text = $"Found {_windows.Count} window(s). Select terminal running Claude Code.";
            }
            else
            {
                StatusTextBlock.Text = "No windows found.";
            }

            // Reset state
            ReadTextButton.IsEnabled = false;
            ParseButton.IsEnabled = false;
            ExportButton.IsEnabled = false;
            RawTextBlock.Text = string.Empty;
            ResultsTextBlock.Text = string.Empty;
            _extractedText = null;
            _parsedRequest = null;
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Error: {ex.Message}";
            MessageBox.Show($"Error loading windows: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void WindowComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (WindowComboBox.SelectedItem is WindowViewModel selected)
        {
            SelectedWindowInfo.Text = $"Selected: {selected.Window.ProcessName} (PID: {selected.Window.ProcessId}, HWND: 0x{selected.Window.WindowHandle:X})";
            ReadTextButton.IsEnabled = true;

            // Reset subsequent steps
            ParseButton.IsEnabled = false;
            ExportButton.IsEnabled = false;
            RawTextBlock.Text = string.Empty;
            ResultsTextBlock.Text = string.Empty;
            _extractedText = null;
            _parsedRequest = null;
        }
        else
        {
            SelectedWindowInfo.Text = "No window selected";
            ReadTextButton.IsEnabled = false;
        }
    }

    private void ReadTextButton_Click(object sender, RoutedEventArgs e)
    {
        if (WindowComboBox.SelectedItem is not WindowViewModel selected)
        {
            MessageBox.Show("Please select a window first.", "No Selection",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            StatusTextBlock.Text = "Extracting text via TextPattern...";

            // Find the Text Area Document element and extract text
            var (success, text, error) = ExtractTerminalText(selected.Window.WindowHandle);

            if (!success || string.IsNullOrEmpty(text))
            {
                var errorMsg = error ?? "No text extracted (TextPattern may not be available)";
                RawTextBlock.Text = $"[EXTRACTION FAILED]\n\n{errorMsg}";
                StatusTextBlock.Text = "Text extraction failed.";

                MessageBox.Show($"Failed to extract text:\n{errorMsg}", "Extraction Failed",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _extractedText = text;

            // Display raw text
            RawTextBlock.Text = $"[EXTRACTED {text.Length} CHARACTERS]\n\n{text}";

            StatusTextBlock.Text = $"Extracted {text.Length} characters. Click Parse Claude Prompt to analyze.";
            ParseButton.IsEnabled = true;
            ExportButton.IsEnabled = true;

            // Clear parser results
            ResultsTextBlock.Text = string.Empty;
            _parsedRequest = null;
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Error: {ex.Message}";
            MessageBox.Show($"Error extracting text: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ParseButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_extractedText))
        {
            MessageBox.Show("No text to parse. Click Read Terminal Text first.", "No Text",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            StatusTextBlock.Text = "Parsing with ClaudePromptParserSimple...";

            // Parse the text
            _parsedRequest = _parser.ParsePermissionRequest(_extractedText);

            // Build results display
            var sb = new StringBuilder();

            if (_parsedRequest == null)
            {
                sb.AppendLine("═══════════════════════════════════════");
                sb.AppendLine("❌ FAIL: Claude Prompt NOT Detected");
                sb.AppendLine("═══════════════════════════════════════");
                sb.AppendLine();
                sb.AppendLine("The parser did not recognize this as a Claude Code permission prompt.");
                sb.AppendLine();
                sb.AppendLine("Diagnostic:");

                // Check which markers are present
                var hasContainsMarkers = _parser.ContainsPromptMarkers(_extractedText);
                var hasValidFormat = _parser.IsValidPromptFormat(_extractedText);

                sb.AppendLine($"  ContainsPromptMarkers: {hasContainsMarkers}");
                sb.AppendLine($"  IsValidPromptFormat: {hasValidFormat}");

                sb.AppendLine();
                sb.AppendLine("Missing markers check:");
                sb.AppendLine($"  'Do you want to proceed?': {(_extractedText.Contains("Do you want to proceed", StringComparison.OrdinalIgnoreCase) ? "✓ FOUND" : "✗ MISSING")}");
                sb.AppendLine($"  'allow reading from': {(_extractedText.Contains("allow reading from", StringComparison.OrdinalIgnoreCase) ? "✓ FOUND" : "✗ MISSING")}");
                sb.AppendLine($"  'from this project': {(_extractedText.Contains("from this project", StringComparison.OrdinalIgnoreCase) ? "✓ FOUND" : "✗ MISSING")}");

                StatusTextBlock.Text = "FAIL: Claude prompt not detected.";
            }
            else
            {
                if (_parsedRequest.HasAllowFromProjectOption && _parsedRequest.AllowFromProjectOptionNumber.HasValue)
                {
                    sb.AppendLine("═══════════════════════════════════════");
                    sb.AppendLine("✅ PASS: Claude Prompt Detected!");
                    sb.AppendLine("═══════════════════════════════════════");
                }
                else
                {
                    sb.AppendLine("═══════════════════════════════════════");
                    sb.AppendLine("⚠️ PARTIAL: Prompt detected but incomplete");
                    sb.AppendLine("═══════════════════════════════════════");
                }

                sb.AppendLine();
                sb.AppendLine($"Tool Name: {_parsedRequest.ToolName}");
                sb.AppendLine($"Prompt Type: {_parsedRequest.PromptType}");
                sb.AppendLine($"Description: {_parsedRequest.Description}");
                sb.AppendLine();
                sb.AppendLine($"Has Allow From Project: {_parsedRequest.HasAllowFromProjectOption}");

                if (_parsedRequest.AllowFromProjectOptionNumber.HasValue)
                {
                    sb.AppendLine($"Allow From Project Option Number: {_parsedRequest.AllowFromProjectOptionNumber.Value}");
                }
                else
                {
                    sb.AppendLine($"Allow From Project Option Number: NOT DETECTED");
                }

                sb.AppendLine();
                sb.AppendLine($"Total Options Detected: {_parsedRequest.Options.Length}");
                sb.AppendLine();
                sb.AppendLine("All Detected Options:");

                foreach (var option in _parsedRequest.Options)
                {
                    var marker = option.Number == _parsedRequest.AllowFromProjectOptionNumber ? "👉" : "  ";
                    sb.AppendLine($"{marker} {option.Number}. {option.Text}");
                    sb.AppendLine($"     Action: {option.Action}");
                }

                if (_parsedRequest.HasAllowFromProjectOption && _parsedRequest.AllowFromProjectOptionNumber.HasValue)
                {
                    sb.AppendLine();
                    sb.AppendLine("═══════════════════════════════════════");
                    sb.AppendLine($"✅ Ready for automation: Would send '{_parsedRequest.AllowFromProjectOptionNumber.Value}' + Enter");
                    sb.AppendLine("═══════════════════════════════════════");

                    StatusTextBlock.Text = $"PASS: Detected option {_parsedRequest.AllowFromProjectOptionNumber.Value} for automation.";
                }
                else
                {
                    sb.AppendLine();
                    sb.AppendLine("⚠️ Allow-from-project option not found - automation would not execute.");

                    StatusTextBlock.Text = "Prompt detected but allow-from-project option missing.";
                }
            }

            ResultsTextBlock.Text = sb.ToString();
        }
        catch (Exception ex)
        {
            ResultsTextBlock.Text = $"[PARSER ERROR]\n\n{ex.GetType().Name}: {ex.Message}\n\n{ex.StackTrace}";
            StatusTextBlock.Text = $"Parser error: {ex.Message}";
            MessageBox.Show($"Parser error: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_extractedText))
        {
            MessageBox.Show("No text to export.", "No Text",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var saveDialog = new SaveFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                DefaultExt = "txt",
                FileName = $"claude_prompt_capture_{timestamp}.txt"
            };

            if (saveDialog.ShowDialog() == true)
            {
                var sb = new StringBuilder();
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine("Claude Permission Prompt Capture");
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine($"Captured: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                if (WindowComboBox.SelectedItem is WindowViewModel selected)
                {
                    sb.AppendLine($"Process: {selected.Window.ProcessName}");
                    sb.AppendLine($"PID: {selected.Window.ProcessId}");
                    sb.AppendLine($"Window: {selected.Window.WindowTitle}");
                    sb.AppendLine($"HWND: 0x{selected.Window.WindowHandle:X}");
                }

                sb.AppendLine($"Text Length: {_extractedText.Length}");
                sb.AppendLine();
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine("RAW TEXTPATTERN OUTPUT");
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine();
                sb.AppendLine(_extractedText);
                sb.AppendLine();
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine("PARSER DIAGNOSTICS");
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine();

                if (_parsedRequest != null)
                {
                    sb.AppendLine($"Parser Result: SUCCESS");
                    sb.AppendLine($"Tool Name: {_parsedRequest.ToolName}");
                    sb.AppendLine($"Prompt Type: {_parsedRequest.PromptType}");
                    sb.AppendLine($"Has Allow From Project: {_parsedRequest.HasAllowFromProjectOption}");
                    sb.AppendLine($"Allow From Project Option Number: {_parsedRequest.AllowFromProjectOptionNumber?.ToString() ?? "NULL"}");
                    sb.AppendLine();
                    sb.AppendLine("Detected Options:");
                    foreach (var option in _parsedRequest.Options)
                    {
                        sb.AppendLine($"  {option.Number}. {option.Text} ({option.Action})");
                    }
                }
                else
                {
                    sb.AppendLine($"Parser Result: FAILED");
                    sb.AppendLine($"ContainsPromptMarkers: {_parser.ContainsPromptMarkers(_extractedText)}");
                    sb.AppendLine($"IsValidPromptFormat: {_parser.IsValidPromptFormat(_extractedText)}");
                }

                File.WriteAllText(saveDialog.FileName, sb.ToString());

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

    private (bool success, string? text, string? error) ExtractTerminalText(IntPtr windowHandle)
    {
        try
        {
            var element = AutomationElement.FromHandle(windowHandle);
            if (element == null)
            {
                return (false, null, "Failed to get AutomationElement from window handle");
            }

            // Try to find the "Text Area" Document element
            var textAreaCondition = new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Document),
                new PropertyCondition(AutomationElement.AutomationIdProperty, "Text Area")
            );

            var textAreaElement = element.FindFirst(TreeScope.Descendants, textAreaCondition);

            if (textAreaElement == null)
            {
                // Fallback: try any Document element
                var docCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Document);
                textAreaElement = element.FindFirst(TreeScope.Descendants, docCondition);
            }

            if (textAreaElement == null)
            {
                return (false, null, "No Document/Text Area element found in window");
            }

            // Check if TextPattern is supported
            var supportedPatterns = textAreaElement.GetSupportedPatterns();
            if (!supportedPatterns.Contains(TextPattern.Pattern))
            {
                return (false, null, "TextPattern not supported by this element");
            }

            // Get TextPattern
            var textPattern = textAreaElement.GetCurrentPattern(TextPattern.Pattern) as TextPattern;
            if (textPattern == null)
            {
                return (false, null, "GetCurrentPattern(TextPattern) returned null");
            }

            // Get document range
            var documentRange = textPattern.DocumentRange;
            if (documentRange == null)
            {
                return (false, null, "DocumentRange is null");
            }

            // Extract text
            var text = documentRange.GetText(-1);
            if (text == null)
            {
                return (false, null, "GetText returned null");
            }

            return (true, text, null);
        }
        catch (Exception ex)
        {
            return (false, null, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private class WindowViewModel
    {
        public required WindowInfo Window { get; init; }
        public required string DisplayText { get; init; }
    }
}
