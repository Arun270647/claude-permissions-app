namespace ClaudePermissionAssistant.App.Models;

/// <summary>
/// PHASE 2 & 3: Comprehensive health metrics for per-terminal monitoring
/// Tracks detection success rate, resource usage, and auto-recovery triggers
/// </summary>
public class TerminalHealthMetrics
{
    // Basic Statistics (Phase 1)
    public int PromptsDetected { get; set; }
    public int PromptsApproved { get; set; }
    public int PromptsFailed { get; set; }
    public DateTime? LastApproval { get; set; }
    public string? LastApprovalDetails { get; set; }

    // PHASE 2: Health Monitoring
    public int ConsecutiveFailures { get; set; }
    public int TotalTextExtractionAttempts { get; set; }
    public int SuccessfulTextExtractions { get; set; }
    public int FailedTextExtractions { get; set; }
    public DateTime? LastSuccessfulTextExtraction { get; set; }
    public DateTime? LastRecoveryTrigger { get; set; }
    public int RecoveryTriggersTotal { get; set; }
    public DateTime MonitoringStarted { get; set; } = DateTime.UtcNow;

    // PHASE 3: Per-Terminal Diagnostics
    public int CacheHits { get; set; }
    public int CacheMisses { get; set; }
    public int CacheRefreshes { get; set; }
    public int CacheCleanups { get; set; }
    public int CurrentCacheSize { get; set; }
    public int WindowValidationFailures { get; set; }
    public int ComObjectsDisposed { get; set; }
    public DateTime? LastCacheCleanup { get; set; }
    public TimeSpan TotalMonitoringTime => DateTime.UtcNow - MonitoringStarted;

    // Health Status Calculations
    public double DetectionSuccessRate
    {
        get
        {
            if (TotalTextExtractionAttempts == 0) return 100.0;
            return (SuccessfulTextExtractions * 100.0) / TotalTextExtractionAttempts;
        }
    }

    public double ApprovalSuccessRate
    {
        get
        {
            if (PromptsDetected == 0) return 100.0;
            return (PromptsApproved * 100.0) / PromptsDetected;
        }
    }

    public double CacheHitRate
    {
        get
        {
            var total = CacheHits + CacheMisses;
            if (total == 0) return 0.0;
            return (CacheHits * 100.0) / total;
        }
    }

    public TerminalHealthStatus HealthStatus
    {
        get
        {
            // Critical: Multiple consecutive failures or very low success rate
            if (ConsecutiveFailures >= 10 || DetectionSuccessRate < 50.0)
                return TerminalHealthStatus.Critical;

            // Degraded: Some failures or moderate success rate
            if (ConsecutiveFailures >= 5 || DetectionSuccessRate < 80.0)
                return TerminalHealthStatus.Degraded;

            // Warning: Recent recovery or slightly low success rate
            if (RecoveryTriggersTotal > 0 && LastRecoveryTrigger.HasValue &&
                (DateTime.UtcNow - LastRecoveryTrigger.Value).TotalMinutes < 5)
                return TerminalHealthStatus.Warning;

            if (DetectionSuccessRate < 95.0)
                return TerminalHealthStatus.Warning;

            // Healthy: Everything working well
            return TerminalHealthStatus.Healthy;
        }
    }

    public void Reset()
    {
        PromptsDetected = 0;
        PromptsApproved = 0;
        PromptsFailed = 0;
        LastApproval = null;
        LastApprovalDetails = null;
        ConsecutiveFailures = 0;
        TotalTextExtractionAttempts = 0;
        SuccessfulTextExtractions = 0;
        FailedTextExtractions = 0;
        LastSuccessfulTextExtraction = null;
        LastRecoveryTrigger = null;
        RecoveryTriggersTotal = 0;
        CacheHits = 0;
        CacheMisses = 0;
        CacheRefreshes = 0;
        CacheCleanups = 0;
        CurrentCacheSize = 0;
        WindowValidationFailures = 0;
        ComObjectsDisposed = 0;
        LastCacheCleanup = null;
        MonitoringStarted = DateTime.UtcNow;
    }
}

/// <summary>
/// PHASE 2: Health status enum for terminal monitoring
/// </summary>
public enum TerminalHealthStatus
{
    /// <summary>Healthy - Detection rate 95%+, no recent failures</summary>
    Healthy,

    /// <summary>Warning - Detection rate 80-95%, or recent recovery</summary>
    Warning,

    /// <summary>Degraded - Detection rate 50-80%, or 5+ consecutive failures</summary>
    Degraded,

    /// <summary>Critical - Detection rate below 50%, or 10+ consecutive failures</summary>
    Critical
}
