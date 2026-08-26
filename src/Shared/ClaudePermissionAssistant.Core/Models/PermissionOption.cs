namespace ClaudePermissionAssistant.Core.Models;

// v1.0.0
public class PermissionOption
{
    public required int Number { get; init; }
    public required string Text { get; init; }
    public PermissionAction Action { get; init; }
}

public enum PermissionAction
{
    Unknown,
    Allow,
    Deny,
    AlwaysAllow,
    NeverAllow,
    Ask
}
