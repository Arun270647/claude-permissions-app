namespace ClaudePermissionAssistant.Core.Models;

// Version: 1.0.0
public class ExecutorConfiguration
{
    public int FocusDelayMs { get; set; } = 100;
    public int KeyPressDelayMs { get; set; } = 50;
    public int VerificationDelayMs { get; set; } = 300;
    public int MaxRetryAttempts { get; set; } = 2;
    public int RetryDelayMs { get; set; } = 500;
    public bool RequireForegroundVerification { get; set; } = true;
}
