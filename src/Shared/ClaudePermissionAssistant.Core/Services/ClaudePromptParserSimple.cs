using System.Text.RegularExpressions;
using ClaudePermissionAssistant.Core.Interfaces;
using ClaudePermissionAssistant.Core.Models;

namespace ClaudePermissionAssistant.Core.Services;

/// <summary>
/// Parser for Claude Code permission prompts supporting multiple prompt variants
/// </summary>
public class ClaudePromptParserSimple : IClaudePromptParser
{
    // Generic Claude question pattern - matches "Do you want to [action]?" structure
    // Added "make" for "Do you want to make this edit"
    private static readonly Regex ClaudeQuestionPattern = new(
        @"Do you want to (proceed|create|read|write|execute|run|modify|delete|access|make)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    // Numbered option marker pattern - finds option numbers anywhere in text
    // Handles "> 1. Yes" and options on same line like "2. Yes ... 3. No"
    private static readonly Regex OptionMarkerPattern = new(
        @"(?:^|[\s>])(\d+)[\.\)]\s+",
        RegexOptions.Multiline | RegexOptions.Compiled
    );

    // Legacy pattern kept for ContainsPromptMarkers simple check
    private static readonly Regex OptionPattern = new(
        @"^[\s>]*(\d+)[\.\)]\s*(.+?)$",
        RegexOptions.Multiline | RegexOptions.Compiled
    );

    // Persistent approval indicators
    private static readonly string[] PersistentApprovalPhrases = new[]
    {
        "from this project",
        "for this project",
        "for this session",
        "auto-approve",
        "accept edits",
        "auto-approve file edits",
        "common file commands",
        "switch to accept",
        "don't ask again"  // PHASE 5: Real Claude Code command approval pattern
    };

    public PermissionRequest? ParsePermissionRequest(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        if (!ContainsPromptMarkers(text))
            return null;

        // Find ALL question matches in the buffer, not just the first
        // This handles terminal scrollback containing multiple historical prompts
        var allQuestionMatches = ClaudeQuestionPattern.Matches(text);
        if (allQuestionMatches.Count == 0)
            return null;

        // Try to parse each candidate prompt region, starting from the LAST (most recent)
        // This ensures we find the current active prompt even if old prompts exist in scrollback
        for (int i = allQuestionMatches.Count - 1; i >= 0; i--)
        {
            var questionMatch = allQuestionMatches[i];
            var candidateRegion = ExtractPromptRegionFromMatch(text, questionMatch);

            if (string.IsNullOrEmpty(candidateRegion))
                continue;

            var request = TryParsePromptRegion(candidateRegion, text);
            if (request != null)
            {
                return request;
            }
        }

        // No valid prompts found in any candidate region
        return null;
    }

    private PermissionRequest? TryParsePromptRegion(string promptRegion, string fullText)
    {
        // Parse options from this specific prompt region
        var options = ExtractOptionsFromRegion(promptRegion);
        if (options.Length == 0)
            return null;

        // Must have at least Allow and Deny options
        if (!options.Any(o => o.Action == PermissionAction.Allow || o.Action == PermissionAction.AlwaysAllow))
            return null;
        if (!options.Any(o => o.Action == PermissionAction.Deny))
            return null;

        var persistentApprovalOption = FindPersistentApprovalOption(options);
        var promptType = ClassifyPromptType(promptRegion, options);
        var (toolName, description) = DeriveToolInfo(promptType, promptRegion);

        var request = new PermissionRequest
        {
            ToolName = toolName,
            Description = description,
            Options = options,
            PromptType = promptType,
            PersistentApprovalOptionNumber = persistentApprovalOption?.Number,
            // Backward compatibility - map to legacy property
            AllowFromProjectOptionNumber = persistentApprovalOption?.Number,
            // PHASE 5 FIX: Store the extracted prompt region for stable identity hashing
            PromptRegion = promptRegion
        };

        return request;
    }

    public bool IsValidPromptFormat(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        // Must contain Claude question pattern
        if (!ClaudeQuestionPattern.IsMatch(text))
            return false;

        // Must contain at least one numbered option
        if (!OptionPattern.IsMatch(text))
            return false;

        return true;
    }

    public bool ContainsPromptMarkers(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        // Must contain "Do you want to" question
        bool hasQuestion = text.Contains("Do you want to", StringComparison.OrdinalIgnoreCase);
        if (!hasQuestion)
            return false;

        // Must contain at least one numbered option (simple check)
        var matches = OptionPattern.Matches(text);
        if (matches.Count == 0)
            return false;

        // Must contain at least one "Yes" and one "No" option
        bool hasYes = text.Contains("Yes", StringComparison.OrdinalIgnoreCase);
        bool hasNo = text.Contains("No", StringComparison.OrdinalIgnoreCase);

        return hasYes && hasNo;
    }

    private PermissionOption[] ExtractOptionsFromRegion(string promptRegion)
    {
        // Find all numbered option markers in the prompt region
        var matches = OptionMarkerPattern.Matches(promptRegion);
        if (matches.Count == 0)
            return Array.Empty<PermissionOption>();

        var options = new List<PermissionOption>();

        // Extract text between consecutive option markers
        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            if (!int.TryParse(match.Groups[1].Value, out int number))
                continue;

            // Find where this option's text starts (after the number and dot/paren)
            int textStart = match.Index + match.Length;

            // Find where this option's text ends (at next option marker or end of region)
            int textEnd;
            if (i < matches.Count - 1)
            {
                // Text ends at the start of next option marker
                textEnd = matches[i + 1].Index;
            }
            else
            {
                // Last option: text goes to end of prompt region
                textEnd = promptRegion.Length;
            }

            // Extract option text
            if (textEnd > textStart)
            {
                var optionText = promptRegion.Substring(textStart, textEnd - textStart);
                optionText = CleanOptionText(optionText);

                if (!string.IsNullOrWhiteSpace(optionText))
                {
                    var action = DetermineAction(optionText);

                    options.Add(new PermissionOption
                    {
                        Number = number,
                        Text = optionText,
                        Action = action
                    });
                }
            }
        }

        return options.ToArray();
    }

    private string ExtractPromptRegionFromMatch(string text, Match questionMatch)
    {
        // Extract prompt region starting from this specific question match
        if (!questionMatch.Success)
            return string.Empty;

        // PHASE 5 FIX: Include context BEFORE the question for better classification
        // Real Claude prompts often have important context above the question:
        //   "Bash command"
        //   "curl --version"
        //   "This command requires approval"
        //   "Do you want to proceed?"
        int regionStart = questionMatch.Index;

        // Look backwards up to 500 characters for context
        // Stop at double newline (paragraph break) or another "Do you want to" question
        int contextStart = Math.Max(0, regionStart - 500);
        var precedingText = text.Substring(contextStart, regionStart - contextStart);

        // Find last double newline (paragraph separator) before the question
        int lastParagraphBreak = precedingText.LastIndexOf("\n\n", StringComparison.Ordinal);
        if (lastParagraphBreak >= 0)
        {
            // Start from after the paragraph break
            regionStart = contextStart + lastParagraphBreak + 2;
        }
        else if (contextStart > 0)
        {
            // No paragraph break found - look for last single newline to avoid including massive content
            // This prevents including thousands of lines when buffer has no paragraph breaks
            int lastNewline = precedingText.LastIndexOf('\n');
            if (lastNewline >= 0)
            {
                // Start from after the last newline (beginning of line before question)
                regionStart = contextStart + lastNewline + 1;
            }
            else
            {
                // No newlines at all - rare case, start from context beginning
                regionStart = contextStart;
            }
        }

        // Find the end boundary (common prompt terminators)
        string[] terminators = new[] { "Esc to cancel", "Tab to amend", "Enter your choice" };
        int regionEnd = text.Length;

        foreach (var terminator in terminators)
        {
            int terminatorIndex = text.IndexOf(terminator, questionMatch.Index, StringComparison.OrdinalIgnoreCase);
            if (terminatorIndex > questionMatch.Index && terminatorIndex < regionEnd)
            {
                regionEnd = terminatorIndex;
            }
        }

        // Extract region from context through options (before terminator)
        if (regionEnd > regionStart)
        {
            return text.Substring(regionStart, regionEnd - regionStart);
        }

        return string.Empty;
    }

    private string CleanOptionText(string text)
    {
        // Trim whitespace
        text = text.Trim();

        // Collapse multiple spaces (from terminal line wrapping/formatting)
        text = Regex.Replace(text, @"\s{2,}", " ");

        // Remove newlines within option text (terminal wrapping)
        text = text.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");

        // Final trim and collapse again
        text = Regex.Replace(text, @"\s+", " ").Trim();

        return text;
    }

    private PermissionOption? FindPersistentApprovalOption(PermissionOption[] options)
    {
        foreach (var option in options)
        {
            if (IsPersistentApprovalOption(option.Text))
            {
                return option;
            }
        }

        return null;
    }

    private bool IsPersistentApprovalOption(string optionText)
    {
        var lower = optionText.ToLowerInvariant();

        // Count how many persistent approval phrases are present
        int matchCount = 0;
        foreach (var phrase in PersistentApprovalPhrases)
        {
            if (lower.Contains(phrase.ToLowerInvariant()))
            {
                matchCount++;
            }
        }

        // Require at least one persistent approval phrase
        // AND the option must start with "Yes" (not just any text containing these phrases)
        bool startsWithYes = optionText.StartsWith("Yes", StringComparison.OrdinalIgnoreCase);

        return matchCount >= 1 && startsWithYes;
    }

    private ClaudePermissionPromptType ClassifyPromptType(string text, PermissionOption[] options)
    {
        var lower = text.ToLowerInvariant();

        // Check persistent approval options for additional context
        var persistentOption = FindPersistentApprovalOption(options);
        if (persistentOption != null)
        {
            var optionLower = persistentOption.Text.ToLowerInvariant();

            if (optionLower.Contains("accept edits") || optionLower.Contains("auto-approve file edits"))
                return ClaudePermissionPromptType.AllowEditing;
        }

        // Check question text for action keywords
        if (lower.Contains("create") || lower.Contains("write"))
            return ClaudePermissionPromptType.AllowWriting;

        if (lower.Contains("read") || lower.Contains("reading"))
            return ClaudePermissionPromptType.AllowReading;

        if (lower.Contains("execute") || lower.Contains("run") || lower.Contains("command"))
            return ClaudePermissionPromptType.AllowExecuting;

        if (lower.Contains("modify") || lower.Contains("edit"))
            return ClaudePermissionPromptType.AllowEditing;

        return ClaudePermissionPromptType.Unknown;
    }

    private (string toolName, string description) DeriveToolInfo(ClaudePermissionPromptType promptType, string text)
    {
        return promptType switch
        {
            ClaudePermissionPromptType.AllowReading => ("Read", "Allow reading from directory"),
            ClaudePermissionPromptType.AllowWriting => ("Write", "Allow writing file"),
            ClaudePermissionPromptType.AllowEditing => ("Edit", "Allow editing files"),
            ClaudePermissionPromptType.AllowExecuting => ("Execute", "Allow executing command"),
            ClaudePermissionPromptType.AllowCommand => ("Command", "Allow command execution"),
            _ => ("Unknown", "Permission request")
        };
    }

    private PermissionAction DetermineAction(string optionText)
    {
        var lower = optionText.ToLowerInvariant();

        // Check for persistent approval first
        if (IsPersistentApprovalOption(optionText))
            return PermissionAction.AlwaysAllow;

        // Simple "Yes" without persistent approval indicators
        if (lower.StartsWith("yes") && !IsPersistentApprovalOption(optionText))
            return PermissionAction.Allow;

        // "No" option
        if (lower.StartsWith("no"))
            return PermissionAction.Deny;

        return PermissionAction.Unknown;
    }
}
