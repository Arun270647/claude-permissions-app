using ClaudePermissionAssistant.Core.Interfaces;
using ClaudePermissionAssistant.Core.Models;
using Microsoft.Extensions.Logging;

namespace ClaudePermissionAssistant.Automation.Services;

public class BackgroundMonitorService : IBackgroundMonitorService, IDisposable
{
    private readonly IClaudeSessionDetector _sessionDetector;
    private readonly IClaudePromptDetector _promptDetector;
    private readonly IClaudePermissionPromptExecutor _executor;
    private readonly ILogger<BackgroundMonitorService> _logger;
    private readonly MonitorStatistics _statistics = new();

    private Task? _monitorTask;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly object _lock = new();

    private const int PollingIntervalMs = 500; // Check every 500ms
    private const int SessionRefreshIntervalMs = 5000; // Refresh sessions every 5s

    public event EventHandler<DetectedPrompt>? PromptDetected;
    public event EventHandler<ExecutionResult>? PromptExecuted;
    public event EventHandler<Exception>? ErrorOccurred;

    public bool IsMonitoring { get; private set; }
    public bool IsEnabled { get; set; } = true;
    public MonitorStatistics Statistics => _statistics;

    public BackgroundMonitorService(
        IClaudeSessionDetector sessionDetector,
        IClaudePromptDetector promptDetector,
        IClaudePermissionPromptExecutor executor,
        ILogger<BackgroundMonitorService> logger)
    {
        _sessionDetector = sessionDetector;
        _promptDetector = promptDetector;
        _executor = executor;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (IsMonitoring)
            {
                _logger.LogWarning("Monitor already running");
                return Task.CompletedTask;
            }

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _statistics.IsMonitoring = true;
            _statistics.MonitorStartedAt = DateTime.UtcNow;
            IsMonitoring = true;

            _monitorTask = Task.Run(() => MonitorLoop(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

            _logger.LogInformation("Background monitor started");
            return Task.CompletedTask;
        }
    }

    public async Task StopAsync()
    {
        lock (_lock)
        {
            if (!IsMonitoring)
            {
                return;
            }

            _cancellationTokenSource?.Cancel();
            _statistics.IsMonitoring = false;
            IsMonitoring = false;
        }

        if (_monitorTask != null)
        {
            try
            {
                await _monitorTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _monitorTask = null;

        _logger.LogInformation("Background monitor stopped");
    }

    private async Task MonitorLoop(CancellationToken cancellationToken)
    {
        ClaudeSession[] sessions = Array.Empty<ClaudeSession>();
        var lastSessionRefresh = DateTime.UtcNow;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Refresh sessions periodically
                if (DateTime.UtcNow - lastSessionRefresh > TimeSpan.FromMilliseconds(SessionRefreshIntervalMs))
                {
                    sessions = _sessionDetector.DetectActiveSessions();
                    _statistics.ClaudeSessionsDetected = sessions.Length;
                    lastSessionRefresh = DateTime.UtcNow;

                    _logger.LogDebug("Detected {Count} Claude sessions", sessions.Length);
                }

                // Only check for prompts if monitoring is enabled
                if (!IsEnabled)
                {
                    await Task.Delay(PollingIntervalMs, cancellationToken);
                    continue;
                }

                // Check each session for prompts
                foreach (var session in sessions)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        var prompt = _promptDetector.DetectPrompt(session);

                        if (prompt != null && prompt.IsValid)
                        {
                            _logger.LogInformation("Claude permission prompt detected in session {ProcessId}",
                                session.TerminalProcessId);

                            _statistics.PromptsDetected++;
                            PromptDetected?.Invoke(this, prompt);

                            // Only execute if enabled and prompt has the "allow from project" option
                            if (IsEnabled && prompt.Request.HasAllowFromProjectOption)
                            {
                                var result = _executor.Execute(prompt);

                                _logger.LogInformation("Execution result: Success={Success}, Option={Option}",
                                    result.Success, result.SelectedOptionNumber);

                                if (result.Success)
                                {
                                    _statistics.PromptsAutomaticallyApproved++;
                                    _statistics.LastApprovalTime = result.ExecutedAt;
                                }
                                else
                                {
                                    _statistics.PromptsFailed++;
                                    _statistics.LastErrorTime = result.ExecutedAt;
                                    _statistics.LastError = result.ErrorMessage;
                                }

                                PromptExecuted?.Invoke(this, result);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error checking session {ProcessId}", session.TerminalProcessId);
                        _statistics.LastErrorTime = DateTime.UtcNow;
                        _statistics.LastError = ex.Message;
                        ErrorOccurred?.Invoke(this, ex);
                    }
                }

                await Task.Delay(PollingIntervalMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in monitor loop");
                _statistics.LastErrorTime = DateTime.UtcNow;
                _statistics.LastError = ex.Message;
                ErrorOccurred?.Invoke(this, ex);

                // Continue monitoring despite error
                await Task.Delay(PollingIntervalMs, cancellationToken);
            }
        }
    }

    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
        _cancellationTokenSource?.Dispose();
    }
}
