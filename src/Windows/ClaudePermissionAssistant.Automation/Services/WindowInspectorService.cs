using System.Diagnostics;
using System.Text;
using System.Windows.Automation;
using ClaudePermissionAssistant.Core.Models;

namespace ClaudePermissionAssistant.Automation.Services;

public class WindowInspectorService
{
    public InspectionResult InspectWindow(IntPtr windowHandle)
    {
        try
        {
            var windowInfo = GetWindowInfo(windowHandle);
            var rootElement = AutomationElement.FromHandle(windowHandle);

            if (rootElement == null)
            {
                return new InspectionResult
                {
                    Window = windowInfo,
                    Success = false,
                    ErrorMessage = "Failed to get AutomationElement from window handle"
                };
            }

            var elementInfo = BuildElementTree(rootElement, 0);
            var totalElements = CountElements(elementInfo);

            return new InspectionResult
            {
                Window = windowInfo,
                RootElement = elementInfo,
                Success = true,
                TotalElements = totalElements
            };
        }
        catch (Exception ex)
        {
            return new InspectionResult
            {
                Window = GetWindowInfo(windowHandle),
                Success = false,
                ErrorMessage = $"{ex.GetType().Name}: {ex.Message}"
            };
        }
    }

    public List<WindowInfo> GetAllWindows()
    {
        var windows = new List<WindowInfo>();
        var condition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window);
        var rootElement = AutomationElement.RootElement;
        var windowElements = rootElement.FindAll(TreeScope.Children, condition);

        foreach (AutomationElement element in windowElements)
        {
            try
            {
                var processId = element.Current.ProcessId;
                var process = Process.GetProcessById(processId);
                var handle = new IntPtr(element.Current.NativeWindowHandle);

                windows.Add(new WindowInfo
                {
                    ProcessId = processId,
                    ProcessName = process.ProcessName,
                    WindowTitle = element.Current.Name,
                    WindowHandle = handle
                });
            }
            catch
            {
                // Skip windows we can't access
            }
        }

        return windows.OrderBy(w => w.ProcessName).ThenBy(w => w.WindowTitle).ToList();
    }

    private WindowInfo GetWindowInfo(IntPtr windowHandle)
    {
        try
        {
            var element = AutomationElement.FromHandle(windowHandle);
            var processId = element.Current.ProcessId;
            var process = Process.GetProcessById(processId);

            return new WindowInfo
            {
                ProcessId = processId,
                ProcessName = process.ProcessName,
                WindowTitle = element.Current.Name,
                WindowHandle = windowHandle
            };
        }
        catch (Exception ex)
        {
            return new WindowInfo
            {
                ProcessId = 0,
                ProcessName = "Unknown",
                WindowTitle = $"Error: {ex.Message}",
                WindowHandle = windowHandle
            };
        }
    }

    private AutomationElementInfo BuildElementTree(AutomationElement element, int depth)
    {
        if (depth > 50) // Prevent infinite recursion
        {
            return CreateElementInfo(element, depth, new List<AutomationElementInfo>());
        }

        var children = new List<AutomationElementInfo>();

        try
        {
            var childElements = element.FindAll(TreeScope.Children, Condition.TrueCondition);

            foreach (AutomationElement child in childElements)
            {
                try
                {
                    children.Add(BuildElementTree(child, depth + 1));
                }
                catch
                {
                    // Skip elements we can't process
                }
            }
        }
        catch
        {
            // If we can't get children, just return the element info without them
        }

        return CreateElementInfo(element, depth, children);
    }

    private AutomationElementInfo CreateElementInfo(AutomationElement element, int depth, List<AutomationElementInfo> children)
    {
        var patterns = GetSupportedPatterns(element);
        var current = element.Current;
        var runtimeId = GetRuntimeIdString(element);

        // Attempt to extract text if TextPattern is supported
        var (textPatternSupported, extractedText, textLength, extractionError) = ExtractTextIfSupported(element);

        return new AutomationElementInfo
        {
            Name = current.Name ?? string.Empty,
            AutomationId = current.AutomationId ?? string.Empty,
            ControlType = current.ControlType?.ProgrammaticName ?? "Unknown",
            ClassName = current.ClassName ?? string.Empty,
            IsEnabled = current.IsEnabled,
            IsOffscreen = current.IsOffscreen,
            BoundingRectangle = FormatBoundingRectangle(current.BoundingRectangle),
            SupportedPatterns = patterns,
            Depth = depth,
            Children = children,
            RuntimeId = runtimeId,
            ProcessId = current.ProcessId.ToString(),
            AcceleratorKey = current.AcceleratorKey ?? string.Empty,
            AccessKey = current.AccessKey ?? string.Empty,
            HelpText = current.HelpText ?? string.Empty,
            ItemStatus = current.ItemStatus ?? string.Empty,
            ItemType = current.ItemType ?? string.Empty,
            TextPatternSupported = textPatternSupported,
            ExtractedText = extractedText,
            ExtractedTextLength = textLength,
            TextExtractionError = extractionError
        };
    }

    private string[] GetSupportedPatterns(AutomationElement element)
    {
        var patterns = new List<string>();
        var allPatterns = element.GetSupportedPatterns();

        foreach (var pattern in allPatterns)
        {
            patterns.Add(pattern.ProgrammaticName);
        }

        return patterns.ToArray();
    }

    private string GetRuntimeIdString(AutomationElement element)
    {
        try
        {
            var runtimeId = element.GetRuntimeId();
            if (runtimeId == null || runtimeId.Length == 0)
                return string.Empty;

            return string.Join(".", runtimeId.Select(i => i.ToString()));
        }
        catch
        {
            return string.Empty;
        }
    }

    private string FormatBoundingRectangle(System.Windows.Rect rect)
    {
        return $"{rect.X},{rect.Y},{rect.Width},{rect.Height}";
    }

    private (bool supported, string? text, int? length, string? error) ExtractTextIfSupported(AutomationElement element)
    {
        try
        {
            // Check if TextPattern is supported
            var supportedPatterns = element.GetSupportedPatterns();
            var supportsTextPattern = supportedPatterns.Contains(TextPattern.Pattern);

            if (!supportsTextPattern)
            {
                return (false, null, null, null);
            }

            // Attempt to get the TextPattern
            try
            {
                var textPattern = element.GetCurrentPattern(TextPattern.Pattern) as TextPattern;
                if (textPattern == null)
                {
                    return (true, null, null, "TextPattern supported but GetCurrentPattern returned null");
                }

                // Get the document range
                var documentRange = textPattern.DocumentRange;
                if (documentRange == null)
                {
                    return (true, null, null, "DocumentRange is null");
                }

                // Extract the text
                // Using -1 to get all available text
                var text = documentRange.GetText(-1);

                if (text == null)
                {
                    return (true, null, null, "GetText returned null");
                }

                return (true, text, text.Length, null);
            }
            catch (Exception ex)
            {
                // Extraction failed but pattern is supported
                return (true, null, null, $"Extraction failed: {ex.GetType().Name}: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            // Failed to check pattern support
            return (false, null, null, $"Pattern check failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private int CountElements(AutomationElementInfo? element)
    {
        if (element == null)
            return 0;

        int count = 1;
        foreach (var child in element.Children)
        {
            count += CountElements(child);
        }

        return count;
    }

    public string ExportTreeToText(InspectionResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== UI Automation Inspection Report ===");
        sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine("Window Information:");
        sb.AppendLine($"  Process ID: {result.Window.ProcessId}");
        sb.AppendLine($"  Process Name: {result.Window.ProcessName}");
        sb.AppendLine($"  Window Title: {result.Window.WindowTitle}");
        sb.AppendLine($"  Window Handle: 0x{result.Window.WindowHandle:X}");
        sb.AppendLine();
        sb.AppendLine($"Total Elements: {result.TotalElements}");
        sb.AppendLine();
        sb.AppendLine("=== Automation Tree ===");

        if (result.RootElement != null)
        {
            AppendElementToText(sb, result.RootElement);
        }

        return sb.ToString();
    }

    private void AppendElementToText(StringBuilder sb, AutomationElementInfo element)
    {
        var indent = new string(' ', element.Depth * 2);

        sb.AppendLine($"{indent}[{element.ControlType}]");
        sb.AppendLine($"{indent}  Name: {element.Name}");
        sb.AppendLine($"{indent}  AutomationId: {element.AutomationId}");
        sb.AppendLine($"{indent}  ClassName: {element.ClassName}");
        sb.AppendLine($"{indent}  IsEnabled: {element.IsEnabled}");
        sb.AppendLine($"{indent}  IsOffscreen: {element.IsOffscreen}");
        sb.AppendLine($"{indent}  BoundingRectangle: {element.BoundingRectangle}");
        sb.AppendLine($"{indent}  RuntimeId: {element.RuntimeId}");

        if (element.SupportedPatterns.Length > 0)
        {
            sb.AppendLine($"{indent}  Patterns: {string.Join(", ", element.SupportedPatterns)}");
        }

        if (!string.IsNullOrEmpty(element.HelpText))
        {
            sb.AppendLine($"{indent}  HelpText: {element.HelpText}");
        }

        // Add TextPattern information if applicable
        if (element.TextPatternSupported)
        {
            sb.AppendLine();
            sb.AppendLine($"{indent}  TextPattern:");
            sb.AppendLine($"{indent}    Supported: true");

            if (element.ExtractedTextLength.HasValue)
            {
                sb.AppendLine($"{indent}    TextLength: {element.ExtractedTextLength.Value}");
            }

            if (!string.IsNullOrEmpty(element.TextExtractionError))
            {
                sb.AppendLine($"{indent}    ExtractionError: {element.TextExtractionError}");
            }

            if (!string.IsNullOrEmpty(element.ExtractedText))
            {
                sb.AppendLine($"{indent}    ExtractedText:");
                sb.AppendLine($"{indent}    --- BEGIN TEXT ---");

                // Include the full text in export (preserve ANSI codes and all characters)
                var lines = element.ExtractedText.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
                foreach (var line in lines)
                {
                    sb.AppendLine($"{indent}    {line}");
                }

                sb.AppendLine($"{indent}    --- END TEXT ---");
            }
        }

        sb.AppendLine();

        foreach (var child in element.Children)
        {
            AppendElementToText(sb, child);
        }
    }
}
