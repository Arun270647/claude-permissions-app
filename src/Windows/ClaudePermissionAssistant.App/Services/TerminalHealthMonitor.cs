using ClaudePermissionAssistant.App.Models;
using ClaudePermissionAssistant.Automation.Services;

namespace ClaudePermissionAssistant.App.Services;

/// <summary>
/// PHASE 2: Health monitoring and auto-recovery for terminal monitoring sessions
/// Watches metrics and triggers recovery when detection rate degrades
/// </summary>
public class TerminalHealthMonitor
{
    private readonly FileLoggingService _logger;
    private readonly TerminalHealthMetrics _metrics;
    private DateTime _lastHealthCheck = DateTime.UtcNow;
    private const int HealthCheckIntervalSeconds = 30;

    // Auto-recovery thresholds
    private const int ConsecutiveFailuresThreshold = 5;
    private const double DetectionRateCriticalThreshold = 50.0; // 50%
    private const double DetectionRateDegradedThreshold = 80.0; // 80%

    public TerminalHealthMonitor(FileLoggingService logger, TerminalHealthMetrics metrics)
    {
        _logger = logger;
        _metrics = metrics;
    }

    /// <summary>
    /// Check health and return true if auto-recovery should trigger
    /// </summary>
    public bool CheckHealthAndShouldRecover(IntPtr windowHandle)
    {
        var now = DateTime.UtcNow;
        var timeSinceLastCheck = (now - _lastHealthCheck).TotalSeconds;

        // Only check health every 30 seconds to avoid spam
        if (timeSinceLastCheck < HealthCheckIntervalSeconds)
            return false;

        _lastHealthCheck = now;

        var status = _metrics.HealthStatus;
        var detectionRate = _metrics.DetectionSuccessRate;
        var approvalRate = _metrics.ApprovalSuccessRate;

        _logger.LogInfo("═══════════════════════════════════════");
        _logger.LogInfo("TERMINAL_HEALTH_CHECK");
        _logger.LogInfo($"  Status: {status}");
        _logger.LogInfo($"  Detection Rate: {detectionRate:F1}%");
        _logger.LogInfo($"  Approval Rate: {approvalRate:F1}%");
        _logger.LogInfo($"  Consecutive Failures: {_metrics.ConsecutiveFailures}");
        _logger.LogInfo($"  Total Extractions: {_metrics.TotalTextExtractionAttempts}");
        _logger.LogInfo($"  Successful: {_metrics.SuccessfulTextExtractions}");
        _logger.LogInfo($"  Failed: {_metrics.FailedTextExtractions}");
        _logger.LogInfo($"  Cache Hit Rate: {_metrics.CacheHitRate:F1}%");
        _logger.LogInfo($"  Cache Size: {_metrics.CurrentCacheSize}");
        _logger.LogInfo($"  Recovery Triggers: {_metrics.RecoveryTriggersTotal}");
        _logger.LogInfo($"  Monitoring Time: {_metrics.TotalMonitoringTime:hh\\:mm\\:ss}");

        // Auto-recovery triggers
        bool shouldRecover = false;
        string? recoveryReason = null;

        if (_metrics.ConsecutiveFailures >= ConsecutiveFailuresThreshold)
        {
            shouldRecover = true;
            recoveryReason = $"{_metrics.ConsecutiveFailures} consecutive failures (threshold: {ConsecutiveFailuresThreshold})";
        }
        else if (detectionRate < DetectionRateCriticalThreshold && _metrics.TotalTextExtractionAttempts >= 20)
        {
            shouldRecover = true;
            recoveryReason = $"Critical detection rate {detectionRate:F1}% (threshold: {DetectionRateCriticalThreshold}%)";
        }
        else if (detectionRate < DetectionRateDegradedThreshold && _metrics.TotalTextExtractionAttempts >= 50)
        {
            // Only trigger for degraded if we haven't recovered recently
            var minutesSinceLastRecovery = _metrics.LastRecoveryTrigger.HasValue
                ? (now - _metrics.LastRecoveryTrigger.Value).TotalMinutes
                : double.MaxValue;

            if (minutesSinceLastRecovery >= 10)
            {
                shouldRecover = true;
                recoveryReason = $"Degraded detection rate {detectionRate:F1}% (threshold: {DetectionRateDegradedThreshold}%)";
            }
        }

        if (shouldRecover)
        {
            _logger.LogInfo($"  AUTO_RECOVERY_TRIGGERED: {recoveryReason}");
        }

        _logger.LogInfo("═══════════════════════════════════════");

        return shouldRecover;
    }

    /// <summary>
    /// Record that recovery was triggered
    /// </summary>
    public void RecordRecovery()
    {
        _metrics.RecoveryTriggersTotal++;
        _metrics.LastRecoveryTrigger = DateTime.UtcNow;
        _metrics.ConsecutiveFailures = 0; // Reset on recovery
    }

    /// <summary>
    /// Record successful text extraction
    /// </summary>
    public void RecordSuccessfulExtraction()
    {
        _metrics.TotalTextExtractionAttempts++;
        _metrics.SuccessfulTextExtractions++;
        _metrics.LastSuccessfulTextExtraction = DateTime.UtcNow;
        _metrics.ConsecutiveFailures = 0; // Reset on success
    }

    /// <summary>
    /// Record failed text extraction
    /// </summary>
    public void RecordFailedExtraction()
    {
        _metrics.TotalTextExtractionAttempts++;
        _metrics.FailedTextExtractions++;
        _metrics.ConsecutiveFailures++;
    }

    /// <summary>
    /// Record cache hit (element found in cache)
    /// </summary>
    public void RecordCacheHit()
    {
        _metrics.CacheHits++;
    }

    /// <summary>
    /// Record cache miss (element not in cache, had to create new)
    /// </summary>
    public void RecordCacheMiss()
    {
        _metrics.CacheMisses++;
        _metrics.CacheRefreshes++;
    }

    /// <summary>
    /// Record cache cleanup operation
    /// </summary>
    public void RecordCacheCleanup(int elementsRemoved, int currentSize)
    {
        _metrics.CacheCleanups++;
        _metrics.LastCacheCleanup = DateTime.UtcNow;
        _metrics.CurrentCacheSize = currentSize;
        _metrics.ComObjectsDisposed += elementsRemoved;
    }

    /// <summary>
    /// Update current cache size
    /// </summary>
    public void UpdateCacheSize(int size)
    {
        _metrics.CurrentCacheSize = size;
    }

    /// <summary>
    /// Record window validation failure
    /// </summary>
    public void RecordWindowValidationFailure()
    {
        _metrics.WindowValidationFailures++;
    }
}
