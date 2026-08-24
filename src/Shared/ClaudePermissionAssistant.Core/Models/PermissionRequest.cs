namespace ClaudePermissionAssistant.Core.Models;

public class PermissionRequest
{
    public required string ToolName { get; init; }
    public required string Description { get; init; }
    public required PermissionOption[] Options { get; init; }
    public string? Context { get; init; }
    public string? CommandLine { get; init; }
    public string? WorkingDirectory { get; init; }
    public ClaudePermissionPromptType PromptType { get; init; }

    // PHASE 5 FIX: Store the extracted prompt region for stable identity hashing
    // This contains just the prompt question + options, not the entire terminal buffer
    public string? PromptRegion { get; init; }

    // Legacy property - kept for backward compatibility
    public int? AllowFromProjectOptionNumber { get; init; }

    // New generalized properties
    public int? PersistentApprovalOptionNumber { get; init; }

    public bool HasAllowOption => Options.Any(o => o.Action == PermissionAction.Allow);
    public bool HasDenyOption => Options.Any(o => o.Action == PermissionAction.Deny);

    // Legacy property - kept for backward compatibility
    public bool HasAllowFromProjectOption => AllowFromProjectOptionNumber.HasValue;

    // New generalized property
    public bool HasPersistentApprovalOption => PersistentApprovalOptionNumber.HasValue;

    // The option number to select: prefer persistent approval, fall back to simple "Yes"
    public int? BestApprovalOptionNumber =>
        PersistentApprovalOptionNumber ??
        Options.FirstOrDefault(o => o.Action == PermissionAction.Allow)?.Number;

    public bool IsValid => !string.IsNullOrWhiteSpace(ToolName) &&
                           Options.Length > 0 &&
                           HasAllowOption &&
                           HasDenyOption;
}
