namespace ClaudePermissionAssistant.Core.Models;

public class InspectionResult
{
    public required WindowInfo Window { get; init; }
    public AutomationElementInfo? RootElement { get; init; }
    public DateTime InspectedAt { get; init; } = DateTime.UtcNow;
    public int TotalElements { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}
