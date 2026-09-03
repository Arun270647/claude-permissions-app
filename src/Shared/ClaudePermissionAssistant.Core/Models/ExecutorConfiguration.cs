namespace ClaudePermissionAssistant.Core.Models;

public class ExecutorConfiguration
{
    public int FocusDelayMs { get; set; } = 150;
    public int KeyPressDelayMs { get; set; } = 80;
    public int VerificationDelayMs { get; set; } = 300;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 300;
    public bool RequireForegroundVerification { get; set; } = true;
    public int ForegroundRetryAttempts { get; set; } = 3;
    public int ForegroundRetryDelayMs { get; set; } = 100;
}
