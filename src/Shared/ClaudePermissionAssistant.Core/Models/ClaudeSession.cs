namespace ClaudePermissionAssistant.Core.Models;

// v1.0.0 - First Production Release
public class ClaudeSession
{
    public required IntPtr TerminalWindowHandle { get; init; }
    public required int TerminalProcessId { get; init; }
    public int? ClaudeProcessId { get; init; }
    public required TerminalType TerminalType { get; init; }
    public required string TerminalProcessName { get; init; }
    public required string WindowTitle { get; init; }
    public DateTime DetectedAt { get; init; } = DateTime.UtcNow;
    public bool IsVerified => ClaudeProcessId.HasValue;
}
