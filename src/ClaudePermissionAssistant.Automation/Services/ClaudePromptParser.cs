using System.Text.RegularExpressions;
using ClaudePermissionAssistant.Core.Interfaces;
using ClaudePermissionAssistant.Core.Models;

namespace ClaudePermissionAssistant.Automation.Services;

public class ClaudePromptParser : IClaudePromptParser
{
    private static readonly string[] PromptMarkers =
    {
        "Claude Code wants to",
        "Claude wants to",
        "wants to use",
        "permission",
        "Allow",
        "Deny"
    };

    private static readonly Regex ToolNamePattern = new(
        @"(?:Claude Code|Claude)\s+wants to\s+(?:use\s+)?(.+?)(?:\n|$)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly Regex DescriptionPattern = new(
        @"Description:\s*(.+?)(?:\n|$)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly Regex OptionPattern = new(
        @"^\s*(\d+)[\.\)]\s*(.+?)$",
        RegexOptions.Multiline | RegexOptions.Compiled
    );

    public PermissionRequest? ParsePermissionRequest(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        if (!IsValidPromptFormat(text))
            return null;

        var toolName = ExtractToolName(text);
        if (string.IsNullOrWhiteSpace(toolName))
            return null;

        var description = ExtractDescription(text);
        var options = ExtractOptions(text);

        if (options.Length == 0)
            return null;

        var request = new PermissionRequest
        {
            ToolName = toolName.Trim(),
            Description = description?.Trim() ?? string.Empty,
            Options = options,
            Context = ExtractContext(text)
        };

        return request.IsValid ? request : null;
    }

    public bool IsValidPromptFormat(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        if (!ContainsPromptMarkers(text))
            return false;

        var hasToolName = ToolNamePattern.IsMatch(text);
        var hasOptions = OptionPattern.IsMatch(text);
        var hasAllowOption = text.Contains("Allow", StringComparison.OrdinalIgnoreCase);
        var hasDenyOption = text.Contains("Deny", StringComparison.OrdinalIgnoreCase);

        return hasToolName && hasOptions && hasAllowOption && hasDenyOption;
    }

    public bool ContainsPromptMarkers(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        int markerCount = PromptMarkers.Count(marker =>
            text.Contains(marker, StringComparison.OrdinalIgnoreCase));

        return markerCount >= 2;
    }

    private string? ExtractToolName(string text)
    {
        var match = ToolNamePattern.Match(text);
        return match.Success ? match.Groups[1].Value : null;
    }

    private string? ExtractDescription(string text)
    {
        var match = DescriptionPattern.Match(text);
        return match.Success ? match.Groups[1].Value : null;
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

    private PermissionAction DetermineAction(string optionText)
    {
        var lower = optionText.ToLowerInvariant();

        if (lower.Contains("always") && lower.Contains("allow"))
            return PermissionAction.AlwaysAllow;
        if (lower.Contains("never") && lower.Contains("allow"))
            return PermissionAction.NeverAllow;
        if (lower.Contains("allow"))
            return PermissionAction.Allow;
        if (lower.Contains("deny"))
            return PermissionAction.Deny;
        if (lower.Contains("ask"))
            return PermissionAction.Ask;

        return PermissionAction.Unknown;
    }

    private string? ExtractContext(string text)
    {
        var lines = text.Split('\n');
        var contextLines = new List<string>();
        bool inContext = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("Description:", StringComparison.OrdinalIgnoreCase))
            {
                inContext = true;
                continue;
            }

            if (inContext && (trimmed.StartsWith("Options:", StringComparison.OrdinalIgnoreCase) ||
                              Regex.IsMatch(trimmed, @"^\d+[\.\)]")))
            {
                break;
            }

            if (inContext && !string.IsNullOrWhiteSpace(trimmed))
            {
                contextLines.Add(trimmed);
            }
        }

        return contextLines.Count > 0 ? string.Join(" ", contextLines) : null;
    }
}
