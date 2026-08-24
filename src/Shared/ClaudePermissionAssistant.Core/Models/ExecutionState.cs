namespace ClaudePermissionAssistant.Core.Models;

public enum ExecutionState
{
    Idle,
    Detected,
    Verified,
    Focused,
    InputSent,
    Verifying,
    Success,
    Failed
}
