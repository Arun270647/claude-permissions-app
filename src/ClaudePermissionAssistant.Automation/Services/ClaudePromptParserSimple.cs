using System.Text.RegularExpressions;
using ClaudePermissionAssistant.Core.Interfaces;
using ClaudePermissionAssistant.Core.Models;

namespace ClaudePermissionAssistant.Automation.Services;

/// <summary>
/// Simplified parser specifically for Claude Code "allow reading from ... from this project" prompts
/// </summary>
public class ClaudePromptParserSimple : IClaudePromptParser
{
    private static readonly Regex ProceedQuestionPattern = new(
        @"Do you want to proceed\?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly Regex AllowReadingFromProjectPattern = new(
        @"^[\s>]*(\d+)[\.\)]\s*(Yes,?\s+allow\s+reading\s+from\s+.+?\s+from\s+this\s+project)",
        RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly Regex OptionPattern = new(
        @"^[\s>]*(\d+)[\.\)]\s*(.+?)$",
        RegexOptions.Multiline | RegexOptions.Compiled
    );

    public PermissionRequest? ParsePermissionRequest(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        if (!ContainsPromptMarkers(text))
            return null;

        if (!IsValidPromptFormat(text))
            return null;

        var options = ExtractOptions(text);
        if (options.Length == 0)
            return null;

        var allowFromProjectOption = FindAllowReadingFromProjectOption(options);

        var request = new PermissionRequest
        {
            ToolName = "Read", // Specific to reading prompts
            Description = "Allow reading from directory",
            Options = options,
            PromptType = ClaudePermissionPromptType.AllowReading,
            AllowFromProjectOptionNumber = allowFromProjectOption?.Number
        };

        return request;
    }

    public bool IsValidPromptFormat(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        // Must contain "Do you want to proceed?"
        if (!ProceedQuestionPattern.IsMatch(text))
            return false;

        // Must contain at least one numbered option
        if (!OptionPattern.IsMatch(text))
            return false;

        // Must contain the specific "allow reading from ... from this project" pattern
        if (!AllowReadingFromProjectPattern.IsMatch(text))
            return false;

        return true;
    }

    public bool ContainsPromptMarkers(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        // Check for key markers
        bool hasProceed = text.Contains("Do you want to proceed", StringComparison.OrdinalIgnoreCase);
        bool hasAllowReading = text.Contains("allow reading from", StringComparison.OrdinalIgnoreCase);
        bool hasFromProject = text.Contains("from this project", StringComparison.OrdinalIgnoreCase);

        return hasProceed && hasAllowReading && hasFromProject;
    }

    private PermissionOption[] ExtractOptions(string text)
    {
        var options = new List<PermissionOption>();
        var matches = OptionPattern.Matches(text);

        foreach (Match match in matches)
        {
            if (int.TryParse(match.Groups[1].Value, out int number))
            {
                var optionText = match.Groups[2].Value.Trim();
                var action = DetermineAction(optionText);

                options.Add(new PermissionOption
                {
                    Number = number,
                    Text = optionText,
                    Action = action
                });
            }
        }

        return options.ToArray();
    }

    private PermissionOption? FindAllowReadingFromProjectOption(PermissionOption[] options)
    {
        foreach (var option in options)
        {
            // Check if this option matches the "allow reading from ... from this project" pattern
            var match = AllowReadingFromProjectPattern.Match($"{option.Number}. {option.Text}");
            if (match.Success)
            {
                return option;
            }
        }

        return null;
    }

    private PermissionAction DetermineAction(string optionText)
    {
        var lower = optionText.ToLowerInvariant();

        // Check for the specific "allow from project" pattern first
        if (lower.Contains("allow") && lower.Contains("from this project"))
            return PermissionAction.AlwaysAllow;

        if (lower.Contains("yes") && !lower.Contains("from this project"))
            return PermissionAction.Allow;

        if (lower.Contains("no"))
            return PermissionAction.Deny;

        return PermissionAction.Unknown;
    }
}
