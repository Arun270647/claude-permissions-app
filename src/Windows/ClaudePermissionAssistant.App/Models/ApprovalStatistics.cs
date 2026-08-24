namespace ClaudePermissionAssistant.App.Models;

/// <summary>
/// In-memory statistics for the monitoring session
/// </summary>
public class ApprovalStatistics
{
    public int PromptsDetected { get; set; }
    public int PromptsApproved { get; set; }
    public int PromptsFailed { get; set; }
    public DateTime? LastApproval { get; set; }
    public string? LastError { get; set; }
    public string? LastApprovalDetails { get; set; }

    public void Reset()
    {
        PromptsDetected = 0;
        PromptsApproved = 0;
        PromptsFailed = 0;
        LastApproval = null;
        LastError = null;
        LastApprovalDetails = null;
    }
}
