namespace ClaudePermissionAssistant.Core.Models;

public class ExecutorConfiguration
{
    public int FocusDelayMs { get; set; } = 200;
    public int KeyPressDelayMs { get; set; } = 100;
    public int VerificationDelayMs { get; set; } = 500;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 500;
    public bool RequireForegroundVerification { get; set; } = true;
    public int ForegroundRetryAttempts { get; set; } = 3;
    public int ForegroundRetryDelayMs { get; set; } = 150;
}
