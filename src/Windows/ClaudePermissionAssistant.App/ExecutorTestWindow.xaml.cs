using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using ClaudePermissionAssistant.Automation.Services;
using ClaudePermissionAssistant.Core.Models;
using ClaudePermissionAssistant.Core.Services;
using Microsoft.Extensions.Logging;

namespace ClaudePermissionAssistant.App;

public partial class ExecutorTestWindow : Window
{
    private readonly WindowInspectorService _inspectorService;
    private readonly ClaudePromptDetector _detector;
    private readonly ClaudePromptParserSimple _parser;
    private readonly ClaudePermissionPromptExecutorHardened _executor;
    private readonly ILogger<ClaudePermissionPromptExecutorHardened> _logger;

    private List<WindowViewModel> _windows = new();
    private DetectedPrompt? _detectedPrompt;
    private ClaudeSession? _currentSession;
    private string? _rawExtractedText;

    public ExecutorTestWindow()
    {
        InitializeComponent();

        _inspectorService = new WindowInspectorService();
        _parser = new ClaudePromptParserSimple();
        _detector = new ClaudePromptDetector(_parser);

        // Create simple logger
        _logger = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug))
            .CreateLogger<ClaudePermissionPromptExecutorHardened>();

        // Create executor with safe configuration
        var config = new ExecutorConfiguration
        {
            FocusDelayMs = 150,
            KeyPressDelayMs = 100,
            VerificationDelayMs = 500,
            MaxRetryAttempts = 1,
            RetryDelayMs = 500,
            RequireForegroundVerification = true
        };

        _executor = new ClaudePermissionPromptExecutorHardened(_detector, _logger, config);

        Loaded += ExecutorTestWindow_Loaded;
    }

    private void ExecutorTestWindow_Loaded(object sender, RoutedEventArgs e)
    {
        RefreshWindowList();

        // Show warning on first load
        var result = MessageBox.Show(
            "This tool will send REAL keyboard input to the selected terminal.\n\n" +
            "Before using:\n" +
            "1. Trigger a real Claude Code permission prompt\n" +
            "2. Leave the prompt visible\n" +
            "3. Select the terminal window\n" +
            "4. Click Detect Prompt\n" +
            "5. Only then click Execute Approval\n\n" +
            "The application will verify foreground focus before sending input.\n\n" +
            "Do you understand and want to proceed?",
            "Executor Test - Safety Warning",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning
        );

        if (result == MessageBoxResult.No)
        {
            Close();
        }
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
            DetectButton.IsEnabled = false;
            ExecuteButton.IsEnabled = false;
            ExportRawTextButton.IsEnabled = false;
            ExportDiagnosticsButton.IsEnabled = false;
            PromptInfoTextBlock.Text = string.Empty;
            ResultTextBlock.Visibility = Visibility.Collapsed;
            _detectedPrompt = null;
            _currentSession = null;
            _rawExtractedText = null;
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
            DetectButton.IsEnabled = true;

            // Create session for this window
            _currentSession = new ClaudeSession
            {
                ClaudeProcessId = selected.Window.ProcessId,
                TerminalProcessId = selected.Window.ProcessId,
                TerminalWindowHandle = selected.Window.WindowHandle,
                TerminalType = TerminalType.CMD,  // Assume CMD for now
                TerminalProcessName = selected.Window.ProcessName,
                WindowTitle = selected.Window.WindowTitle,
                DetectedAt = DateTime.UtcNow
            };

            // Reset subsequent steps
            ExecuteButton.IsEnabled = false;
            ExportRawTextButton.IsEnabled = false;
            ExportDiagnosticsButton.IsEnabled = false;
            PromptInfoTextBlock.Text = string.Empty;
            ResultTextBlock.Visibility = Visibility.Collapsed;
            _detectedPrompt = null;
            _rawExtractedText = null;
            DetectionStatus.Text = "Terminal selected. Click Detect Prompt to scan for Claude permission prompt.";
            DetectionStatus.Foreground = new SolidColorBrush(Colors.Blue);
        }
        else
        {
            SelectedWindowInfo.Text = "No window selected";
            DetectButton.IsEnabled = false;
            DetectionStatus.Text = "Select a terminal first";
            DetectionStatus.Foreground = new SolidColorBrush(Colors.Gray);
        }
    }

    private void DetectButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentSession == null)
        {
            MessageBox.Show("No terminal selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            StatusTextBlock.Text = "Detecting prompt...";
            DetectionStatus.Text = "Scanning terminal text via TextPattern...";
            DetectionStatus.Foreground = new SolidColorBrush(Colors.Orange);

            // Step 1: Extract raw text first
            _rawExtractedText = _detector.GetTerminalText(_currentSession.TerminalWindowHandle);

            // Step 2: Detect prompt
            _detectedPrompt = _detector.DetectPrompt(_currentSession);

            if (_detectedPrompt == null)
            {
                // Prompt not detected - show comprehensive diagnostics
                DetectionStatus.Text = "❌ NO PROMPT DETECTED";
                DetectionStatus.Foreground = new SolidColorBrush(Colors.Red);

                DisplayFailureDiagnostics(_rawExtractedText);

                ExecuteButton.IsEnabled = false;
                ExportRawTextButton.IsEnabled = !string.IsNullOrEmpty(_rawExtractedText);
                ExportDiagnosticsButton.IsEnabled = !string.IsNullOrEmpty(_rawExtractedText);
                ExecutionInfo.Text = "No prompt detected - cannot execute";
                ExecutionInfo.Foreground = new SolidColorBrush(Colors.Red);
                StatusTextBlock.Text = "No prompt detected. See diagnostics below.";
                return;
            }

            // Prompt detected successfully
            DetectionStatus.Text = "✅ PROMPT DETECTED";
            DetectionStatus.Foreground = new SolidColorBrush(Colors.Green);

            // Display prompt information
            DisplayPromptInfo(_detectedPrompt);

            // Enable export buttons
            ExportRawTextButton.IsEnabled = true;
            ExportDiagnosticsButton.IsEnabled = true;

            // Enable execute button if persistent approval option exists
            if (_detectedPrompt.Request.HasPersistentApprovalOption &&
                _detectedPrompt.Request.PersistentApprovalOptionNumber.HasValue)
            {
                ExecuteButton.IsEnabled = true;
                ExecutionInfo.Text = $"Ready to send option {_detectedPrompt.Request.PersistentApprovalOptionNumber.Value} + Enter";
                ExecutionInfo.Foreground = new SolidColorBrush(Colors.Green);
            }
            else
            {
                ExecuteButton.IsEnabled = false;
                ExecutionInfo.Text = "No persistent approval option found - cannot execute";
                ExecutionInfo.Foreground = new SolidColorBrush(Colors.Red);
            }

            StatusTextBlock.Text = "Prompt detected. Review information and click Execute Approval if ready.";
        }
        catch (Exception ex)
        {
            DetectionStatus.Text = $"❌ ERROR: {ex.Message}";
            DetectionStatus.Foreground = new SolidColorBrush(Colors.Red);
            MessageBox.Show($"Error detecting prompt: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DisplayFailureDiagnostics(string? rawText)
    {
        var sb = new StringBuilder();

        sb.AppendLine("═══════════════════════════════════════");
        sb.AppendLine("❌ NO CLAUDE PROMPT DETECTED");
        sb.AppendLine("═══════════════════════════════════════");
        sb.AppendLine();

        // Terminal information
        sb.AppendLine("TARGET TERMINAL:");
        if (_currentSession != null)
        {
            sb.AppendLine($"  HWND: 0x{_currentSession.TerminalWindowHandle:X}");
            sb.AppendLine($"  Process ID: {_currentSession.TerminalProcessId}");
            sb.AppendLine($"  Process Name: {_currentSession.TerminalProcessName}");
            sb.AppendLine($"  Window Title: {_currentSession.WindowTitle}");
        }
        sb.AppendLine();

        // Raw text extraction
        sb.AppendLine("RAW TEXTPATTERN EXTRACTION:");
        if (string.IsNullOrEmpty(rawText))
        {
            sb.AppendLine("  ❌ NO TEXT EXTRACTED");
            sb.AppendLine("  TextPattern may not be available for this window.");
        }
        else
        {
            sb.AppendLine($"  ✓ Text extracted: {rawText.Length} characters");
            sb.AppendLine();

            // Parser diagnostics
            sb.AppendLine("PARSER DIAGNOSTICS:");
            sb.AppendLine();

            // Check individual conditions
            bool containsPromptMarkers = _parser.ContainsPromptMarkers(rawText);
            bool isValidFormat = _parser.IsValidPromptFormat(rawText);

            sb.AppendLine($"  ContainsPromptMarkers: {containsPromptMarkers}");
            sb.AppendLine($"  IsValidPromptFormat: {isValidFormat}");
            sb.AppendLine();

            // Detailed marker checks
            sb.AppendLine("MARKER CHECKS:");
            bool hasDoYouWant = rawText.Contains("Do you want to", StringComparison.OrdinalIgnoreCase);
            bool hasYes = rawText.Contains("Yes", StringComparison.OrdinalIgnoreCase);
            bool hasNo = rawText.Contains("No", StringComparison.OrdinalIgnoreCase);

            sb.AppendLine($"  'Do you want to': {(hasDoYouWant ? "✓ FOUND" : "✗ MISSING")}");
            sb.AppendLine($"  'Yes' option: {(hasYes ? "✓ FOUND" : "✗ MISSING")}");
            sb.AppendLine($"  'No' option: {(hasNo ? "✓ FOUND" : "✗ MISSING")}");
            sb.AppendLine();

            // Check for numbered options
            var numberedOptionPattern = new System.Text.RegularExpressions.Regex(@"^\s*\d+[\.\)]", System.Text.RegularExpressions.RegexOptions.Multiline);
            var numberedMatches = numberedOptionPattern.Matches(rawText);
            sb.AppendLine($"  Numbered options found: {numberedMatches.Count}");
            sb.AppendLine();

            // Try to parse anyway to see what we get
            var request = _parser.ParsePermissionRequest(rawText);
            if (request != null)
            {
                sb.AppendLine("PARSER OUTPUT (partial):");
                sb.AppendLine($"  Tool Name: {request.ToolName}");
                sb.AppendLine($"  Prompt Type: {request.PromptType}");
                sb.AppendLine($"  Options Detected: {request.Options.Length}");
                sb.AppendLine($"  Has Persistent Approval: {request.HasPersistentApprovalOption}");
                sb.AppendLine($"  Persistent Approval Option: {request.PersistentApprovalOptionNumber?.ToString() ?? "NULL"}");
                sb.AppendLine();

                foreach (var option in request.Options)
                {
                    sb.AppendLine($"    {option.Number}. {option.Text} ({option.Action})");
                }
                sb.AppendLine();
                sb.AppendLine("  Note: Parser returned a request but it may not have passed validation.");
            }
            else
            {
                sb.AppendLine("PARSER OUTPUT:");
                sb.AppendLine("  NULL (no request returned)");
            }
            sb.AppendLine();

            // Show excerpt of raw text
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("RAW TEXT EXCERPT (first 2000 chars):");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine();

            int maxDisplay = Math.Min(2000, rawText.Length);
            sb.AppendLine(rawText.Substring(0, maxDisplay));

            if (rawText.Length > 2000)
            {
                sb.AppendLine();
                sb.AppendLine($"... (truncated, full text is {rawText.Length} chars)");
                sb.AppendLine("Use 'Export Raw Text' to see complete content.");
            }

            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("Use 'Export Diagnostics' for full report");
            sb.AppendLine("═══════════════════════════════════════");
        }

        PromptInfoTextBlock.Text = sb.ToString();
    }

    private void DisplayPromptInfo(DetectedPrompt prompt)
    {
        var sb = new StringBuilder();

        sb.AppendLine("═══════════════════════════════════════");
        sb.AppendLine("✅ CLAUDE PERMISSION PROMPT DETECTED");
        sb.AppendLine("═══════════════════════════════════════");
        sb.AppendLine();

        sb.AppendLine("TARGET TERMINAL:");
        sb.AppendLine($"  Process: {prompt.Session.TerminalProcessId}");
        sb.AppendLine($"  HWND: 0x{prompt.Session.TerminalWindowHandle:X}");
        sb.AppendLine();

        sb.AppendLine("PROMPT DETAILS:");
        sb.AppendLine($"  Tool Name: {prompt.Request.ToolName}");
        sb.AppendLine($"  Prompt Type: {prompt.Request.PromptType}");
        sb.AppendLine($"  Description: {prompt.Request.Description}");
        sb.AppendLine();

        sb.AppendLine("PERSISTENT APPROVAL:");
        sb.AppendLine($"  Available: {prompt.Request.HasPersistentApprovalOption}");
        if (prompt.Request.PersistentApprovalOptionNumber.HasValue)
        {
            sb.AppendLine($"  Option Number: {prompt.Request.PersistentApprovalOptionNumber.Value}");
        }
        else
        {
            sb.AppendLine($"  Option Number: NOT DETECTED");
        }
        sb.AppendLine();

        sb.AppendLine($"DETECTED OPTIONS ({prompt.Request.Options.Length}):");
        foreach (var option in prompt.Request.Options)
        {
            var marker = option.Number == prompt.Request.PersistentApprovalOptionNumber ? "👉" : "  ";
            sb.AppendLine($"{marker} {option.Number}. {option.Text}");
            sb.AppendLine($"     Action: {option.Action}");
        }
        sb.AppendLine();

        if (prompt.Request.HasPersistentApprovalOption && prompt.Request.PersistentApprovalOptionNumber.HasValue)
        {
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine($"✅ READY TO EXECUTE");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"Will send: '{prompt.Request.PersistentApprovalOptionNumber.Value}' + Enter");
            sb.AppendLine();
            sb.AppendLine("Safety checks that will be performed:");
            sb.AppendLine("  ✓ Re-detect prompt before execution");
            sb.AppendLine("  ✓ Verify persistent approval option still exists");
            sb.AppendLine("  ✓ Bring terminal to foreground");
            sb.AppendLine("  ✓ Verify foreground window matches target");
            sb.AppendLine("  ✓ Send keyboard input");
            sb.AppendLine("  ✓ Verify prompt disappeared after submission");
        }
        else
        {
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("⚠️ CANNOT EXECUTE");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine("No persistent approval option detected.");
            sb.AppendLine("Automation requires a persistent approval option.");
        }

        PromptInfoTextBlock.Text = sb.ToString();
    }

    private void ExecuteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_detectedPrompt == null)
        {
            MessageBox.Show("No prompt detected. Click Detect Prompt first.", "Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Final confirmation
        var result = MessageBox.Show(
            $"About to send REAL keyboard input:\n\n" +
            $"Option: {_detectedPrompt.Request.PersistentApprovalOptionNumber}\n" +
            $"Target: HWND 0x{_detectedPrompt.Session.TerminalWindowHandle:X}\n\n" +
            "This will:\n" +
            "1. Bring the terminal to foreground\n" +
            "2. Send the option number\n" +
            "3. Send Enter key\n\n" +
            "Continue?",
            "Execute Approval - Final Confirmation",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning
        );

        if (result == MessageBoxResult.No)
        {
            return;
        }

        try
        {
            StatusTextBlock.Text = "Executing automation...";
            ExecuteButton.IsEnabled = false;
            DetectButton.IsEnabled = false;

            // Execute
            var executionResult = _executor.Execute(_detectedPrompt);

            // Display results
            DisplayExecutionResult(executionResult);

            // Re-enable detection
            DetectButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Execution error: {ex.Message}";
            MessageBox.Show($"Execution error: {ex.Message}\n\n{ex.StackTrace}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);

            DetectButton.IsEnabled = true;
        }
    }

    private void DisplayExecutionResult(ExecutionResult result)
    {
        var sb = new StringBuilder();

        sb.AppendLine("═══════════════════════════════════════");
        if (result.Success)
        {
            sb.AppendLine("✅ EXECUTION SUCCESSFUL");
        }
        else
        {
            sb.AppendLine("❌ EXECUTION FAILED");
        }
        sb.AppendLine("═══════════════════════════════════════");
        sb.AppendLine();

        sb.AppendLine("EXECUTION DETAILS:");
        sb.AppendLine($"  Success: {result.Success}");
        sb.AppendLine($"  Final State: {result.FinalState}");
        sb.AppendLine($"  Option Sent: {result.SelectedOptionNumber}");
        sb.AppendLine($"  Duration: {result.ExecutionDuration.TotalMilliseconds:F0}ms");
        sb.AppendLine($"  Retry Count: {result.RetryCount}");
        sb.AppendLine();

        sb.AppendLine("VERIFICATION CHECKS:");
        sb.AppendLine($"  Foreground Verified: {result.ForegroundVerified}");
        sb.AppendLine($"  Prompt Disappeared: {result.PromptDisappeared}");
        sb.AppendLine();

        if (!result.Success)
        {
            sb.AppendLine("ERROR:");
            sb.AppendLine($"  {result.ErrorMessage}");
            sb.AppendLine();
        }

        if (result.Success)
        {
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("✅ Claude prompt successfully approved!");
            sb.AppendLine("═══════════════════════════════════════");
            StatusTextBlock.Text = "Execution successful! Claude prompt approved.";
            StatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);
        }
        else
        {
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("❌ Execution failed - see error above");
            sb.AppendLine("═══════════════════════════════════════");
            StatusTextBlock.Text = $"Execution failed: {result.ErrorMessage}";
            StatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
        }

        ResultTextBlock.Text = sb.ToString();
        ResultTextBlock.Visibility = Visibility.Visible;

        // Clear detected prompt so user must re-detect
        _detectedPrompt = null;
        ExecuteButton.IsEnabled = false;
        ExecutionInfo.Text = "Execution complete. Re-detect prompt to try again.";
        ExecutionInfo.Foreground = new SolidColorBrush(Colors.Gray);
    }

    private void ExportRawTextButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_rawExtractedText))
        {
            MessageBox.Show("No text to export.", "No Text", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                DefaultExt = "txt",
                FileName = $"executor_raw_text_{timestamp}.txt"
            };

            if (saveDialog.ShowDialog() == true)
            {
                var sb = new StringBuilder();
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine("Executor Test - Raw TextPattern Extraction");
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine($"Captured: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                if (_currentSession != null)
                {
                    sb.AppendLine($"HWND: 0x{_currentSession.TerminalWindowHandle:X}");
                    sb.AppendLine($"Process ID: {_currentSession.TerminalProcessId}");
                    sb.AppendLine($"Process Name: {_currentSession.TerminalProcessName}");
                    sb.AppendLine($"Window Title: {_currentSession.WindowTitle}");
                }

                sb.AppendLine($"Text Length: {_rawExtractedText.Length} characters");
                sb.AppendLine();
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine("RAW TEXTPATTERN OUTPUT (UNMODIFIED)");
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine();
                sb.AppendLine(_rawExtractedText);

                File.WriteAllText(saveDialog.FileName, sb.ToString());

                StatusTextBlock.Text = $"Raw text exported to: {saveDialog.FileName}";
                MessageBox.Show($"Raw text exported successfully:\n{saveDialog.FileName}", "Export Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportDiagnosticsButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_rawExtractedText))
        {
            MessageBox.Show("No diagnostics to export.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                DefaultExt = "txt",
                FileName = $"executor_diagnostics_{timestamp}.txt"
            };

            if (saveDialog.ShowDialog() == true)
            {
                var sb = new StringBuilder();
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine("Executor Test - Detection Diagnostics");
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine($"Captured: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine();

                // Terminal information
                sb.AppendLine("TARGET TERMINAL:");
                if (_currentSession != null)
                {
                    sb.AppendLine($"  HWND: 0x{_currentSession.TerminalWindowHandle:X}");
                    sb.AppendLine($"  Process ID: {_currentSession.TerminalProcessId}");
                    sb.AppendLine($"  Process Name: {_currentSession.TerminalProcessName}");
                    sb.AppendLine($"  Window Title: {_currentSession.WindowTitle}");
                    sb.AppendLine($"  Terminal Type: {_currentSession.TerminalType}");
                }
                sb.AppendLine();

                // Detection status
                sb.AppendLine("DETECTION RESULT:");
                if (_detectedPrompt != null)
                {
                    sb.AppendLine("  ✓ PROMPT DETECTED");
                    sb.AppendLine($"  Tool Name: {_detectedPrompt.Request.ToolName}");
                    sb.AppendLine($"  Prompt Type: {_detectedPrompt.Request.PromptType}");
                    sb.AppendLine($"  Persistent Approval: {_detectedPrompt.Request.HasPersistentApprovalOption}");
                    sb.AppendLine($"  Persistent Approval Option: {_detectedPrompt.Request.PersistentApprovalOptionNumber?.ToString() ?? "NULL"}");
                }
                else
                {
                    sb.AppendLine("  ✗ PROMPT NOT DETECTED");
                }
                sb.AppendLine();

                // Raw text extraction
                sb.AppendLine("RAW TEXT EXTRACTION:");
                sb.AppendLine($"  Length: {_rawExtractedText.Length} characters");
                sb.AppendLine();

                // Parser diagnostics
                sb.AppendLine("PARSER DIAGNOSTICS:");
                bool containsPromptMarkers = _parser.ContainsPromptMarkers(_rawExtractedText);
                bool isValidFormat = _parser.IsValidPromptFormat(_rawExtractedText);

                sb.AppendLine($"  ContainsPromptMarkers: {containsPromptMarkers}");
                sb.AppendLine($"  IsValidPromptFormat: {isValidFormat}");
                sb.AppendLine();

                // Marker checks
                sb.AppendLine("MARKER CHECKS:");
                bool hasDoYouWant = _rawExtractedText.Contains("Do you want to", StringComparison.OrdinalIgnoreCase);
                bool hasYes = _rawExtractedText.Contains("Yes", StringComparison.OrdinalIgnoreCase);
                bool hasNo = _rawExtractedText.Contains("No", StringComparison.OrdinalIgnoreCase);

                sb.AppendLine($"  'Do you want to': {hasDoYouWant}");
                sb.AppendLine($"  'Yes': {hasYes}");
                sb.AppendLine($"  'No': {hasNo}");
                sb.AppendLine();

                // Numbered options
                var numberedOptionPattern = new System.Text.RegularExpressions.Regex(@"^\s*\d+[\.\)]", System.Text.RegularExpressions.RegexOptions.Multiline);
                var numberedMatches = numberedOptionPattern.Matches(_rawExtractedText);
                sb.AppendLine($"  Numbered options pattern matches: {numberedMatches.Count}");
                foreach (System.Text.RegularExpressions.Match match in numberedMatches)
                {
                    sb.AppendLine($"    - '{match.Value.Trim()}' at index {match.Index}");
                }
                sb.AppendLine();

                // Parser attempt
                sb.AppendLine("PARSER ATTEMPT:");
                var request = _parser.ParsePermissionRequest(_rawExtractedText);
                if (request != null)
                {
                    sb.AppendLine("  Parser returned a PermissionRequest:");
                    sb.AppendLine($"    Tool Name: {request.ToolName}");
                    sb.AppendLine($"    Prompt Type: {request.PromptType}");
                    sb.AppendLine($"    Description: {request.Description}");
                    sb.AppendLine($"    Options: {request.Options.Length}");
                    sb.AppendLine($"    Has Allow: {request.HasAllowOption}");
                    sb.AppendLine($"    Has Deny: {request.HasDenyOption}");
                    sb.AppendLine($"    Is Valid: {request.IsValid}");
                    sb.AppendLine($"    Has Persistent Approval: {request.HasPersistentApprovalOption}");
                    sb.AppendLine($"    Persistent Approval Option: {request.PersistentApprovalOptionNumber?.ToString() ?? "NULL"}");
                    sb.AppendLine();

                    sb.AppendLine("  Parsed Options:");
                    foreach (var option in request.Options)
                    {
                        sb.AppendLine($"    {option.Number}. {option.Text}");
                        sb.AppendLine($"       Action: {option.Action}");
                    }
                }
                else
                {
                    sb.AppendLine("  Parser returned NULL");
                }
                sb.AppendLine();

                // Full raw text
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine("RAW TEXTPATTERN OUTPUT (COMPLETE)");
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine();
                sb.AppendLine(_rawExtractedText);

                File.WriteAllText(saveDialog.FileName, sb.ToString());

                StatusTextBlock.Text = $"Diagnostics exported to: {saveDialog.FileName}";
                MessageBox.Show($"Diagnostics exported successfully:\n{saveDialog.FileName}", "Export Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
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
