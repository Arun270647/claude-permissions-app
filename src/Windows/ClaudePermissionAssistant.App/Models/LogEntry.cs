using ClaudePermissionAssistant.Core.Models;

namespace ClaudePermissionAssistant.App.Models;

/// <summary>
/// Log entry for file logging
/// </summary>
public class LogEntry
{
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public required string Event { get; init; }
    public ClaudePermissionPromptType? PromptType { get; init; }
    public int? OptionNumber { get; init; }
    public bool? Success { get; init; }
    public string? Error { get; init; }
    public int? TerminalPid { get; init; }
    public long? DurationMs { get; init; }
}
