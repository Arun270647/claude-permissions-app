namespace ClaudePermissionAssistant.Core.Models;

public class PermissionRequest
{
    public required string ToolName { get; init; }
    public required string Description { get; init; }
    public required PermissionOption[] Options { get; init; }
    public string? Context { get; init; }
    public string? CommandLine { get; init; }
    public string? WorkingDirectory { get; init; }

    public bool HasAllowOption => Options.Any(o => o.Action == PermissionAction.Allow);
    public bool HasDenyOption => Options.Any(o => o.Action == PermissionAction.Deny);
    public bool IsValid => !string.IsNullOrWhiteSpace(ToolName) &&
                           Options.Length > 0 &&
                           HasAllowOption &&
                           HasDenyOption;
}
