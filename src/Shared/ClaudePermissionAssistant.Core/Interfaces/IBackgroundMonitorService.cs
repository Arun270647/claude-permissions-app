using ClaudePermissionAssistant.Core.Models;

namespace ClaudePermissionAssistant.Core.Interfaces;

public interface IBackgroundMonitorService
{
    bool IsMonitoring { get; }
    bool IsEnabled { get; set; }
    MonitorStatistics Statistics { get; }

    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync();

    event EventHandler<DetectedPrompt>? PromptDetected;
    event EventHandler<ExecutionResult>? PromptExecuted;
    event EventHandler<Exception>? ErrorOccurred;
}
