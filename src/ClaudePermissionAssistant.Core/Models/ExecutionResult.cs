namespace ClaudePermissionAssistant.Core.Models;

public class ExecutionResult
{
    public required bool Success { get; init; }
    public required DateTime ExecutedAt { get; init; }
    public required DetectedPrompt Prompt { get; init; }
    public required int SelectedOptionNumber { get; init; }
    public string? ErrorMessage { get; init; }
    public bool PromptDisappeared { get; init; }
    public TimeSpan ExecutionDuration { get; init; }
    public ExecutionState FinalState { get; init; }
    public bool ForegroundVerified { get; init; }
    public int RetryCount { get; init; }
}
