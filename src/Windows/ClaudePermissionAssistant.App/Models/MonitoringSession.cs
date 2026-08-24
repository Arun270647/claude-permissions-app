using ClaudePermissionAssistant.App.Services;
using ClaudePermissionAssistant.Core.Models;

namespace ClaudePermissionAssistant.App.Models;

/// <summary>
/// Represents an active monitoring session for a terminal
/// </summary>
public class MonitoringSession
{
    public required TerminalCandidate Terminal { get; init; }
    public DateTime StartedAt { get; init; } = DateTime.UtcNow;
    public bool IsRunning { get; set; }
    public string? LastError { get; set; }
    public DateTime? LastActivity { get; set; }
}
