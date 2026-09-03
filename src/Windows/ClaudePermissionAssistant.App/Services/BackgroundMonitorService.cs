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
    private readonly ClaudePromptDetector _detector;
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
    private DateTime _lastCacheCleanup = DateTime.MinValue;
    private int _consecutiveTextExtractionFailures = 0;
    private DateTime _lastHandledPromptsCleanup = DateTime.MinValue;
    private const int MaxConsecutiveFailuresBeforeRecovery = 10;
    private const int CacheCleanupIntervalMinutes = 1;    // PHASE 1 FIX: Reduced from 5 minutes
    private const int HandledPromptsCleanupIntervalMinutes = 2;  // PHASE 1 FIX: Reduced from 10 minutes

    // Conversation boundary detection
    private string? _lastTerminalTextHash;
    private int _lastTerminalTextLength = 0;
    private DateTime _lastConversationBoundary = DateTime.MinValue;

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
            FocusDelayMs = 250,              // Increased from 150ms for better stability
            KeyPressDelayMs = 80,
            VerificationDelayMs = 300,
            MaxRetryAttempts = 3,
            RetryDelayMs = 300,
            RequireForegroundVerification = true,
            ForegroundRetryAttempts = 5,     // Increased from 3 for active terminals
            ForegroundRetryDelayMs = 200     // Increased from 100ms for exponential backoff
        };

        _executor = new ClaudePermissionPromptExecutorHardened(_detector, executorLogger, executorConfig);

        // Polling interval: 300ms for faster detection
        _monitorTimer = new System.Timers.Timer(300);
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
            _lastCacheCleanup = DateTime.UtcNow; // Initialize cache cleanup timer
            _lastHandledPromptsCleanup = DateTime.UtcNow; // Initialize handled prompts cleanup timer
            _consecutiveTextExtractionFailures = 0;
            _lastTerminalTextHash = null;
            _lastTerminalTextLength = 0;
            _lastConversationBoundary = DateTime.UtcNow;

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

            // Periodic cache cleanup to prevent memory bloat (every 5 minutes)
            var shouldCleanupCache = (DateTime.UtcNow - _lastCacheCleanup).TotalMinutes >= CacheCleanupIntervalMinutes;
            if (shouldCleanupCache)
            {
                _detector.CleanupStaleCache();
                _lastCacheCleanup = DateTime.UtcNow;

                if (shouldLogDiagnostics)
                {
                    _logger.LogInfo("MONITOR_CACHE_CLEANUP: Periodic cleanup completed");
                }
            }

            // Periodic cleanup of old handled prompts (every 10 minutes) for 24/7 stability
            var shouldCleanupHandledPrompts = (DateTime.UtcNow - _lastHandledPromptsCleanup).TotalMinutes >= HandledPromptsCleanupIntervalMinutes;
            if (shouldCleanupHandledPrompts)
            {
                _executor.CleanupOldHandledPrompts();
                _lastHandledPromptsCleanup = DateTime.UtcNow;

                if (shouldLogDiagnostics)
                {
                    _logger.LogInfo("MONITOR_HANDLED_PROMPTS_CLEANUP: Periodic cleanup completed (24/7 stability)");
                }
            }

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
            // SECURITY: Log window handle being monitored
            if (shouldLogDiagnostics)
            {
                _logger.LogInfo($"MONITOR_TEXT_EXTRACTION_START: HWND=0x{claudeSession.TerminalWindowHandle:X}, PID={claudeSession.TerminalProcessId}");
            }

            var terminalText = _detector.GetTerminalText(claudeSession.TerminalWindowHandle);
            var textLength = terminalText?.Length ?? 0;

            // SECURITY: Log first 200 chars of extracted text to verify it's from the right window
            if (shouldLogDiagnostics && textLength > 0 && terminalText != null)
            {
                var preview = terminalText.Length > 200 ? terminalText.Substring(0, 200) : terminalText;
                preview = preview.Replace("\r", "").Replace("\n", " ");
                _logger.LogInfo($"MONITOR_TEXT_PREVIEW: '{preview}...'");
            }

            // CONVERSATION BOUNDARY DETECTION
            // Detect when terminal content changes significantly (indicates new conversation)
            if (textLength > 0 && terminalText != null)
            {
                var conversationBoundaryDetected = DetectConversationBoundary(terminalText, textLength);
                if (conversationBoundaryDetected)
                {
                    _logger.LogInfo("═══════════════════════════════════════");
                    _logger.LogInfo("CONVERSATION_BOUNDARY_DETECTED");
                    _logger.LogInfo($"  Reason: Significant terminal content change");
                    _logger.LogInfo($"  Previous text length: {_lastTerminalTextLength}");
                    _logger.LogInfo($"  Current text length: {textLength}");
                    _logger.LogInfo($"  Time since last boundary: {(DateTime.UtcNow - _lastConversationBoundary).TotalSeconds:F1}s");
                    _logger.LogInfo("  Action: Incrementing context sequence, clearing caches");

                    // Increment context sequence (invalidates old deduplication keys)
                    _executor.IncrementContextSequence();

                    // Clear UI Automation cache to force fresh element acquisition
                    _detector.ClearCache(claudeSession.TerminalWindowHandle);

                    // Clear handled prompts to allow re-detection
                    _executor.ClearHandledPrompts();

                    _lastConversationBoundary = DateTime.UtcNow;

                    _logger.LogInfo($"  New context sequence: {_executor.GetContextSequence()}");
                    _logger.LogInfo("═══════════════════════════════════════");
                }
            }

            // Track text extraction failures
            if (textLength == 0)
            {
                _consecutiveTextExtractionFailures++;

                if (shouldLogDiagnostics)
                {
                    _logger.LogInfo($"MONITOR_TEXT_EXTRACTION: FAILED - Length=0, ConsecutiveFailures={_consecutiveTextExtractionFailures}");
                }

                // Trigger recovery if failures exceed threshold
                if (_consecutiveTextExtractionFailures >= MaxConsecutiveFailuresBeforeRecovery)
                {
                    _logger.LogInfo($"MONITOR_RECOVERY: Triggering recovery after {_consecutiveTextExtractionFailures} consecutive text extraction failures");
                    TriggerRecovery(claudeSession.TerminalWindowHandle);
                    _consecutiveTextExtractionFailures = 0; // Reset counter after recovery attempt
                }
            }
            else
            {
                // Text extraction succeeded - reset counter
                if (_consecutiveTextExtractionFailures > 0)
                {
                    _logger.LogInfo($"MONITOR_TEXT_EXTRACTION: RECOVERED - Previous failures: {_consecutiveTextExtractionFailures}");
                    _consecutiveTextExtractionFailures = 0;
                }

                if (shouldLogDiagnostics)
                {
                    _logger.LogInfo($"MONITOR_TEXT_EXTRACTION: Length={textLength}, HasText=true");
                }
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

            // Check if already handled (duplicate protection) - do this BEFORE incrementing statistics
            if (_executor.IsPromptAlreadyHandled(detectedPrompt))
            {
                // Already handled - skip (don't count as a new detection)
                if (shouldLogDiagnostics)
                {
                    _logger.LogInfo($"MONITOR_REJECTION: Prompt already handled (duplicate protection)");
                }
                return;
            }

            // Prompt detected with persistent approval option (and not a duplicate)
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

    private void TriggerRecovery(IntPtr windowHandle)
    {
        _logger.LogInfo("═══════════════════════════════════════");
        _logger.LogInfo("MONITOR_RECOVERY_START");
        _logger.LogInfo($"  Reason: {_consecutiveTextExtractionFailures} consecutive text extraction failures");
        _logger.LogInfo($"  Action: Aggressive cache cleanup and COM object release");

        try
        {
            // PHASE 1 FIX: Aggressive recovery with COM cleanup

            // Step 1: Clear the detector's automation element cache
            _detector.ClearCache(windowHandle);
            _logger.LogInfo("  Detector cache cleared");

            // Step 2: Run detector's cleanup to release COM objects
            _detector.CleanupStaleCache();
            _logger.LogInfo("  Stale COM objects released");

            // Step 3: Clear handled prompts to allow re-detection
            _executor.ClearHandledPrompts();
            _logger.LogInfo("  Handled prompts cleared");

            // Step 4: Force conversation boundary detection reset
            _lastTerminalTextHash = null;
            _lastTerminalTextLength = 0;
            _lastConversationBoundary = DateTime.UtcNow;
            _logger.LogInfo("  Conversation boundary reset");

            // Step 5: Force garbage collection to release COM objects
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            _logger.LogInfo("  Forced garbage collection complete");

            // Step 6: Reset cleanup timers to force immediate cleanup on next cycle
            _lastCacheCleanup = DateTime.MinValue;
            _lastHandledPromptsCleanup = DateTime.MinValue;

            _logger.LogInfo("MONITOR_RECOVERY_COMPLETE");
        }
        catch (Exception ex)
        {
            _logger.LogError("MONITOR_RECOVERY_FAILED", ex);
        }
        finally
        {
            _logger.LogInfo("═══════════════════════════════════════");
        }
    }

    /// <summary>
    /// Detect if terminal content has changed significantly, indicating a conversation boundary
    /// Returns true if a new conversation has likely started
    /// </summary>
    private bool DetectConversationBoundary(string currentText, int currentLength)
    {
        // First run - establish baseline
        if (_lastTerminalTextHash == null)
        {
            _lastTerminalTextHash = ComputeTextHash(currentText);
            _lastTerminalTextLength = currentLength;
            return false;
        }

        // Check for significant length changes
        var lengthChangeRatio = Math.Abs(currentLength - _lastTerminalTextLength) / (double)Math.Max(_lastTerminalTextLength, 1);

        // Significant shrinkage (>30% smaller) - terminal was cleared or scrolled significantly
        if (currentLength < _lastTerminalTextLength * 0.7)
        {
            _lastTerminalTextHash = ComputeTextHash(currentText);
            _lastTerminalTextLength = currentLength;
            return true;
        }

        // Significant growth (>80% larger) - lots of new output (likely new conversation)
        // But only if at least 5 seconds have passed since last boundary to avoid false positives during active output
        if (currentLength > _lastTerminalTextLength * 1.8 &&
            (DateTime.UtcNow - _lastConversationBoundary).TotalSeconds > 5)
        {
            _lastTerminalTextHash = ComputeTextHash(currentText);
            _lastTerminalTextLength = currentLength;
            return true;
        }

        // Content hash changed completely (screen was cleared and refilled)
        var currentHash = ComputeTextHash(currentText);
        if (currentHash != _lastTerminalTextHash)
        {
            // Hash changed - but only treat as boundary if content is substantially different
            // (to avoid false positives from minor scrolling)
            if (lengthChangeRatio > 0.3)
            {
                _lastTerminalTextHash = currentHash;
                _lastTerminalTextLength = currentLength;
                return true;
            }

            // Update hash but don't treat as boundary (minor change)
            _lastTerminalTextHash = currentHash;
            _lastTerminalTextLength = currentLength;
        }

        return false;
    }

    /// <summary>
    /// Compute a hash of terminal text for change detection
    /// Uses first 32 chars for performance (full hash not needed for change detection)
    /// </summary>
    private string ComputeTextHash(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(hashBytes).Substring(0, 32).ToLowerInvariant();
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

        // PHASE 1 FIX: Dispose detector to release COM objects
        (_detector as IDisposable)?.Dispose();

        // PHASE 1 FIX: Force garbage collection to release unmanaged resources
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}

public class StatisticsUpdatedEventArgs : EventArgs
{
    public required ApprovalStatistics Statistics { get; init; }
}
