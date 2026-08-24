using System.Timers;
using ClaudePermissionAssistant.App.Models;
using ClaudePermissionAssistant.Automation.Services;
using ClaudePermissionAssistant.Core.Interfaces;
using ClaudePermissionAssistant.Core.Models;
using ClaudePermissionAssistant.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClaudePermissionAssistant.App.Services;

/// <summary>
/// Continuous monitoring service that watches a selected terminal for Claude permission prompts
/// and automatically approves them using the proven automation engine
/// </summary>
public class BackgroundMonitorService : IDisposable
{
    private readonly IClaudePromptDetector _detector;
    private readonly ClaudePermissionPromptExecutorHardened _executor;
    private readonly FileLoggingService _logger;
    private readonly ApprovalStatistics _statistics;
    private readonly System.Timers.Timer _monitorTimer;
    private readonly object _lock = new();

    private MonitoringSession? _currentSession;
    private bool _isRunning;
    private bool _isProcessing;
    private int _cycleCount = 0;
    private DateTime _lastDiagnosticLog = DateTime.MinValue;

    public event EventHandler<StatisticsUpdatedEventArgs>? StatisticsUpdated;
    public event EventHandler<string>? StatusChanged;
    public event EventHandler<string>? ErrorOccurred;

    public BackgroundMonitorService(
        FileLoggingService logger,
        ApprovalStatistics statistics)
    {
        _logger = logger;
        _statistics = statistics;

        // Use proven automation components
        var parser = new ClaudePromptParserSimple();
        _detector = new ClaudePromptDetector(parser);

        var executorLogger = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information))
            .CreateLogger<ClaudePermissionPromptExecutorHardened>();

        var executorConfig = new ExecutorConfiguration
        {
            FocusDelayMs = 200,
            KeyPressDelayMs = 100,
            VerificationDelayMs = 500,
            MaxRetryAttempts = 0,
            RetryDelayMs = 0,
            RequireForegroundVerification = false
        };

        _executor = new ClaudePermissionPromptExecutorHardened(_detector, executorLogger, executorConfig);

        // Polling interval: 500ms
        _monitorTimer = new System.Timers.Timer(500);
        _monitorTimer.Elapsed += MonitorTimer_Elapsed;
        _monitorTimer.AutoReset = true;
    }

    public bool IsRunning
    {
        get
        {
            lock (_lock)
            {
                return _isRunning;
            }
        }
    }

    public MonitoringSession? CurrentSession
    {
        get
        {
            lock (_lock)
            {
                return _currentSession;
            }
        }
    }

    /// <summary>
    /// Start continuous monitoring of the specified terminal
    /// </summary>
    public void Start(TerminalCandidate terminal)
    {
        lock (_lock)
        {
            if (_isRunning)
            {
                throw new InvalidOperationException("Monitoring is already running");
            }

            if (!terminal.TextPatternAvailable)
            {
                throw new InvalidOperationException("TextPattern is not available for this terminal");
            }

            _currentSession = new MonitoringSession
            {
                Terminal = terminal,
                IsRunning = true
            };

            _isRunning = true;
            _statistics.Reset();
            _executor.ClearHandledPrompts(); // Allow re-handling of prompts for new session
            _cycleCount = 0;
            _lastDiagnosticLog = DateTime.MinValue;

            _logger.LogInfo($"═══════════════════════════════════════");
            _logger.LogInfo($"MONITOR_STARTED");
            _logger.LogInfo($"  Terminal: {terminal.DisplayName}");
            _logger.LogInfo($"  HWND: 0x{terminal.WindowInfo.WindowHandle:X}");
            _logger.LogInfo($"  PID: {terminal.WindowInfo.ProcessId}");
            _logger.LogInfo($"  Process: {terminal.WindowInfo.ProcessName}");
            _logger.LogInfo($"  TextPattern Available: {terminal.TextPatternAvailable}");
            _logger.LogInfo($"  Is Claude Terminal: {terminal.IsClaudeTerminal}");
            _logger.LogInfo($"═══════════════════════════════════════");

            StatusChanged?.Invoke(this, "Running");

            _monitorTimer.Start();
        }
    }

    /// <summary>
    /// Stop continuous monitoring
    /// </summary>
    public void Stop()
    {
        lock (_lock)
        {
            if (!_isRunning)
            {
                return;
            }

            _monitorTimer.Stop();
            _isRunning = false;

            if (_currentSession != null)
            {
                _currentSession.IsRunning = false;
                _logger.LogInfo($"═══════════════════════════════════════");
                _logger.LogInfo($"MONITOR_STOPPED");
                _logger.LogInfo($"  Terminal: {_currentSession.Terminal.DisplayName}");
                _logger.LogInfo($"  Total Cycles: {_cycleCount}");
                _logger.LogInfo($"  Prompts Detected: {_statistics.PromptsDetected}");
                _logger.LogInfo($"  Prompts Approved: {_statistics.PromptsApproved}");
                _logger.LogInfo($"  Prompts Failed: {_statistics.PromptsFailed}");
                _logger.LogInfo($"═══════════════════════════════════════");
            }

            StatusChanged?.Invoke(this, "Stopped");
        }
    }

    private void MonitorTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        // Prevent re-entrance
        if (_isProcessing)
            return;

        try
        {
            _isProcessing = true;
            MonitorCycle();
        }
        catch (Exception ex)
        {
            HandleError("Monitor cycle error", ex);
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private void MonitorCycle()
    {
        MonitoringSession? session;

        lock (_lock)
        {
            if (!_isRunning || _currentSession == null)
                return;

            session = _currentSession;
            _cycleCount++;
        }

        try
        {
            // PHASE 5 DIAGNOSTIC: Log heartbeat once per second (not every 500ms)
            var shouldLogDiagnostics = (DateTime.UtcNow - _lastDiagnosticLog).TotalSeconds >= 1.0;

            // Check if terminal still exists
            if (!IsTerminalAlive(session.Terminal))
            {
                HandleTerminalDisconnect();
                return;
            }

            // Create Claude session for detection
            var claudeSession = CreateClaudeSessionFromTerminal(session.Terminal);

            if (shouldLogDiagnostics)
            {
                _lastDiagnosticLog = DateTime.UtcNow;
                _logger.LogInfo($"MONITOR_HEARTBEAT: Cycle={_cycleCount}, HWND=0x{claudeSession.TerminalWindowHandle:X}, PID={claudeSession.TerminalProcessId}, ClaudeProcessId={claudeSession.ClaudeProcessId?.ToString() ?? "NULL"}, IsVerified={claudeSession.IsVerified}");
            }

            // Extract terminal text for diagnostics
            var terminalText = _detector.GetTerminalText(claudeSession.TerminalWindowHandle);
            var textLength = terminalText?.Length ?? 0;

            if (shouldLogDiagnostics)
            {
                _logger.LogInfo($"MONITOR_TEXT_EXTRACTION: Length={textLength}, HasText={textLength > 0}");
            }

            // Use proven detector to find Claude permission prompts
            var detectedPrompt = _detector.DetectPrompt(claudeSession);

            if (shouldLogDiagnostics)
            {
                if (detectedPrompt == null)
                {
                    // Log why detection failed
                    if (!claudeSession.IsVerified)
                    {
                        _logger.LogInfo($"MONITOR_DETECTION: FAILED - Session not verified (ClaudeProcessId is null)");
                    }
                    else if (textLength == 0)
                    {
                        _logger.LogInfo($"MONITOR_DETECTION: FAILED - No terminal text extracted");
                    }
                    else
                    {
                        _logger.LogInfo($"MONITOR_DETECTION: FAILED - Parser returned null (no prompt markers or invalid format)");
                    }
                }
                else
                {
                    _logger.LogInfo($"MONITOR_DETECTION: SUCCESS - PromptType={detectedPrompt.Request.PromptType}, HasPersistentApproval={detectedPrompt.Request.HasPersistentApprovalOption}, OptionNumber={detectedPrompt.Request.PersistentApprovalOptionNumber?.ToString() ?? "NULL"}");
                }
            }

            if (detectedPrompt == null)
            {
                // No prompt detected - continue monitoring
                if (shouldLogDiagnostics)
                {
                    _logger.LogInfo($"MONITOR_CYCLE_RESULT: No prompt detected, continuing monitoring");
                }
                return;
            }

            // Check if prompt has any approval option (persistent or simple "Yes")
            if (!detectedPrompt.Request.BestApprovalOptionNumber.HasValue)
            {
                _logger.LogInfo($"MONITOR_REJECTION: Prompt detected but NO approval option found (PromptType={detectedPrompt.Request.PromptType}, Options={detectedPrompt.Request.Options.Length})");
                return;
            }

            // Prompt detected with persistent approval option
            _statistics.PromptsDetected++;
            session.LastActivity = DateTime.UtcNow;

            _logger.Log(new LogEntry
            {
                Event = "PROMPT_DETECTED",
                PromptType = detectedPrompt.Request.PromptType,
                OptionNumber = detectedPrompt.Request.PersistentApprovalOptionNumber,
                TerminalPid = session.Terminal.WindowInfo.ProcessId
            });

            NotifyStatisticsUpdated();

            // Check if already handled (duplicate protection)
            if (_executor.IsPromptAlreadyHandled(detectedPrompt))
            {
                // Already handled - skip
                if (shouldLogDiagnostics)
                {
                    _logger.LogInfo($"MONITOR_REJECTION: Prompt already handled (duplicate protection)");
                }
                return;
            }

            // Prompt is ready for execution
            _logger.LogInfo($"MONITOR_EXECUTION_START: PromptType={detectedPrompt.Request.PromptType}, Option={detectedPrompt.Request.PersistentApprovalOptionNumber}");

            // Execute approval using proven executor
            var startTime = DateTime.UtcNow;
            var result = _executor.Execute(detectedPrompt);
            var duration = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

            if (result.Success)
            {
                _statistics.PromptsApproved++;
                _statistics.LastApproval = DateTime.Now;
                _statistics.LastApprovalDetails = $"{result.Prompt.Request.PromptType} → Option {result.SelectedOptionNumber}";

                _logger.Log(new LogEntry
                {
                    Event = "APPROVAL_SUCCESS",
                    PromptType = detectedPrompt.Request.PromptType,
                    OptionNumber = result.SelectedOptionNumber,
                    Success = true,
                    DurationMs = duration,
                    TerminalPid = session.Terminal.WindowInfo.ProcessId
                });
            }
            else
            {
                _statistics.PromptsFailed++;
                _statistics.LastError = result.ErrorMessage;

                _logger.Log(new LogEntry
                {
                    Event = "APPROVAL_FAILED",
                    PromptType = detectedPrompt.Request.PromptType,
                    OptionNumber = result.SelectedOptionNumber,
                    Success = false,
                    Error = result.ErrorMessage,
                    DurationMs = duration,
                    TerminalPid = session.Terminal.WindowInfo.ProcessId
                });

                ErrorOccurred?.Invoke(this, result.ErrorMessage ?? "Unknown error");
            }

            NotifyStatisticsUpdated();
        }
        catch (Exception ex)
        {
            HandleError("Monitor cycle exception", ex);
        }
    }

    private bool IsTerminalAlive(TerminalCandidate terminal)
    {
        try
        {
            // Check if process still exists
            using var process = System.Diagnostics.Process.GetProcessById(terminal.WindowInfo.ProcessId);
            return !process.HasExited;
        }
        catch
        {
            return false;
        }
    }

    private void HandleTerminalDisconnect()
    {
        _statistics.LastError = "Terminal disconnected";
        _logger.LogError("TERMINAL_DISCONNECTED");

        Stop();

        ErrorOccurred?.Invoke(this, "Terminal disconnected");
    }

    private void HandleError(string message, Exception ex)
    {
        _statistics.LastError = ex.Message;
        _logger.LogError(message, ex);

        ErrorOccurred?.Invoke(this, $"{message}: {ex.Message}");

        // Don't stop monitoring on errors - continue trying
        // The application should remain alive
    }

    private ClaudeSession CreateClaudeSessionFromTerminal(TerminalCandidate terminal)
    {
        // PHASE 5 FIX: ALWAYS set ClaudeProcessId to ensure IsVerified = true
        // ExecutorTest sets this explicitly, BackgroundMonitor must do the same
        // ClaudePromptDetector.DetectPrompt() requires session.IsVerified (which checks ClaudeProcessId.HasValue)

        // Explicit fallback: use detected Claude PID if available, otherwise use terminal PID
        int claudeProcessId;
        if (terminal.ClaudeSession != null && terminal.ClaudeSession.ClaudeProcessId.HasValue)
        {
            claudeProcessId = terminal.ClaudeSession.ClaudeProcessId.Value;
            _logger.LogInfo($"MONITOR_SESSION_CREATE: Using detected Claude PID={claudeProcessId}");
        }
        else
        {
            claudeProcessId = terminal.WindowInfo.ProcessId;
            _logger.LogInfo($"MONITOR_SESSION_CREATE: No Claude session detected, using terminal PID={claudeProcessId} as fallback");
        }

        var session = new ClaudeSession
        {
            TerminalWindowHandle = terminal.WindowInfo.WindowHandle,
            TerminalProcessId = terminal.WindowInfo.ProcessId,
            ClaudeProcessId = claudeProcessId,  // CRITICAL: Must be set for IsVerified to return true
            TerminalType = terminal.TerminalType,
            TerminalProcessName = terminal.WindowInfo.ProcessName,
            WindowTitle = terminal.WindowInfo.WindowTitle,
            DetectedAt = DateTime.UtcNow
        };

        _logger.LogInfo($"MONITOR_SESSION_VERIFY: Created session with IsVerified={session.IsVerified}, ClaudeProcessId={session.ClaudeProcessId}");

        return session;
    }

    private void NotifyStatisticsUpdated()
    {
        StatisticsUpdated?.Invoke(this, new StatisticsUpdatedEventArgs
        {
            Statistics = _statistics
        });
    }

    public void Dispose()
    {
        Stop();
        _monitorTimer?.Dispose();
    }
}

public class StatisticsUpdatedEventArgs : EventArgs
{
    public required ApprovalStatistics Statistics { get; init; }
}
