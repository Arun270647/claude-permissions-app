namespace ClaudePermissionAssistant.Core.Models;

public class DetectedPrompt
{
    public required ClaudeSession Session { get; init; }
    public required string RawText { get; init; }
    public required PermissionRequest Request { get; init; }
    public DateTime DetectedAt { get; init; } = DateTime.UtcNow;
    public int? TextLineNumber { get; init; }
    public int? TextColumnNumber { get; init; }

    public bool IsValid => Request.IsValid && Session.IsVerified;
}
