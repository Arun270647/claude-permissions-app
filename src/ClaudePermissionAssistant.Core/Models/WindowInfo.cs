namespace ClaudePermissionAssistant.Core.Models;

public class WindowInfo
{
    public required int ProcessId { get; init; }
    public required string ProcessName { get; init; }
    public required string WindowTitle { get; init; }
    public required IntPtr WindowHandle { get; init; }
    public DateTime CapturedAt { get; init; } = DateTime.UtcNow;
}
