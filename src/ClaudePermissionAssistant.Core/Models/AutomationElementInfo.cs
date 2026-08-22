namespace ClaudePermissionAssistant.Core.Models;

public class AutomationElementInfo
{
    public required string Name { get; init; }
    public required string AutomationId { get; init; }
    public required string ControlType { get; init; }
    public required string ClassName { get; init; }
    public required bool IsEnabled { get; init; }
    public required bool IsOffscreen { get; init; }
    public required string BoundingRectangle { get; init; }
    public required string[] SupportedPatterns { get; init; }
    public int Depth { get; init; }
    public List<AutomationElementInfo> Children { get; init; } = new();

    public string RuntimeId { get; init; } = string.Empty;
    public string ProcessId { get; init; } = string.Empty;
    public string AcceleratorKey { get; init; } = string.Empty;
    public string AccessKey { get; init; } = string.Empty;
    public string HelpText { get; init; } = string.Empty;
    public string ItemStatus { get; init; } = string.Empty;
    public string ItemType { get; init; } = string.Empty;

    // TextPattern extraction
    public bool TextPatternSupported { get; init; }
    public string? ExtractedText { get; init; }
    public int? ExtractedTextLength { get; init; }
    public string? TextExtractionError { get; init; }
}
