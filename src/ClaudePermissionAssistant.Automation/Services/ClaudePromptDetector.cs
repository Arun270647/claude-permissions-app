using System.Windows.Automation;
using ClaudePermissionAssistant.Core.Interfaces;
using ClaudePermissionAssistant.Core.Models;

namespace ClaudePermissionAssistant.Automation.Services;

public class ClaudePromptDetector : IClaudePromptDetector
{
    private readonly IClaudePromptParser _parser;

    public ClaudePromptDetector(IClaudePromptParser parser)
    {
        _parser = parser;
    }

    public DetectedPrompt? DetectPrompt(ClaudeSession session)
    {
        if (!session.IsVerified)
            return null;

        var text = GetTerminalText(session.TerminalWindowHandle);
        if (string.IsNullOrWhiteSpace(text))
            return null;

        if (!_parser.ContainsPromptMarkers(text))
            return null;

        var request = _parser.ParsePermissionRequest(text);
        if (request == null || !request.IsValid)
            return null;

        return new DetectedPrompt
        {
            Session = session,
            RawText = text,
            Request = request
        };
    }

    public string? GetTerminalText(IntPtr windowHandle)
    {
        try
        {
            var element = AutomationElement.FromHandle(windowHandle);
            if (element == null)
                return null;

            var text = TryGetTextViaTextPattern(element);
            if (!string.IsNullOrWhiteSpace(text))
                return text;

            text = TryGetTextViaValuePattern(element);
            if (!string.IsNullOrWhiteSpace(text))
                return text;

            text = TryGetTextFromChildren(element);
            return text;
        }
        catch
        {
            return null;
        }
    }

    public bool CanAccessTerminalText(IntPtr windowHandle)
    {
        try
        {
            var element = AutomationElement.FromHandle(windowHandle);
            if (element == null)
                return false;

            if (SupportsPattern(element, TextPattern.Pattern))
                return true;

            if (SupportsPattern(element, ValuePattern.Pattern))
                return true;

            var editElement = FindEditControl(element);
            if (editElement != null)
            {
                if (SupportsPattern(editElement, TextPattern.Pattern))
                    return true;
                if (SupportsPattern(editElement, ValuePattern.Pattern))
                    return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private string? TryGetTextViaTextPattern(AutomationElement element)
    {
        try
        {
            if (!SupportsPattern(element, TextPattern.Pattern))
                return null;

            var textPattern = element.GetCurrentPattern(TextPattern.Pattern) as TextPattern;
            if (textPattern == null)
                return null;

            var documentRange = textPattern.DocumentRange;
            return documentRange?.GetText(-1);
        }
        catch
        {
            return null;
        }
    }

    private string? TryGetTextViaValuePattern(AutomationElement element)
    {
        try
        {
            if (!SupportsPattern(element, ValuePattern.Pattern))
                return null;

            var valuePattern = element.GetCurrentPattern(ValuePattern.Pattern) as ValuePattern;
            return valuePattern?.Current.Value;
        }
        catch
        {
            return null;
        }
    }

    private string? TryGetTextFromChildren(AutomationElement element)
    {
        try
        {
            var editElement = FindEditControl(element);
            if (editElement == null)
                return null;

            var text = TryGetTextViaTextPattern(editElement);
            if (!string.IsNullOrWhiteSpace(text))
                return text;

            text = TryGetTextViaValuePattern(editElement);
            return text;
        }
        catch
        {
            return null;
        }
    }

    private AutomationElement? FindEditControl(AutomationElement parent)
    {
        try
        {
            var editCondition = new OrCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit),
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Document)
            );

            var editElement = parent.FindFirst(TreeScope.Descendants, editCondition);
            return editElement;
        }
        catch
        {
            return null;
        }
    }

    private bool SupportsPattern(AutomationElement element, AutomationPattern pattern)
    {
        try
        {
            var supportedPatterns = element.GetSupportedPatterns();
            return supportedPatterns.Contains(pattern);
        }
        catch
        {
            return false;
        }
    }
}
