namespace ClaudePermissionAssistant.Core.Models;

public class MonitorStatistics
{
    public int PromptsDetected { get; set; }
    public int PromptsAutomaticallyApproved { get; set; }
    public int PromptsFailed { get; set; }
    public DateTime? LastApprovalTime { get; set; }
    public DateTime? LastErrorTime { get; set; }
    public string? LastError { get; set; }
    public int ClaudeSessionsDetected { get; set; }
    public bool IsMonitoring { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime MonitorStartedAt { get; set; }

    public void Reset()
    {
        PromptsDetected = 0;
        PromptsAutomaticallyApproved = 0;
        PromptsFailed = 0;
        LastApprovalTime = null;
        LastErrorTime = null;
        LastError = null;
    }
}
