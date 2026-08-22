using System.Diagnostics;
using System.Runtime.InteropServices;
using ClaudePermissionAssistant.Core.Interfaces;
using ClaudePermissionAssistant.Core.Models;
using Microsoft.Extensions.Logging;

namespace ClaudePermissionAssistant.Automation.Services;

/// <summary>
/// Hardened executor with comprehensive safety checks and state machine
/// </summary>
public class ClaudePermissionPromptExecutorHardened : IClaudePermissionPromptExecutor
{
    private readonly IClaudePromptDetector _detector;
    private readonly ILogger<ClaudePermissionPromptExecutorHardened> _logger;
    private readonly ExecutorConfiguration _config;
    private readonly HashSet<string> _handledPrompts = new();
    private readonly Dictionary<string, DateTime> _promptFirstSeen = new();
    private readonly object _lock = new();

    public ClaudePermissionPromptExecutorHardened(
        IClaudePromptDetector detector,
        ILogger<ClaudePermissionPromptExecutorHardened> logger,
        ExecutorConfiguration? config = null)
    {
        _detector = detector;
        _logger = logger;
        _config = config ?? new ExecutorConfiguration();
    }

    public ExecutionResult Execute(DetectedPrompt prompt)
    {
        var startTime = DateTime.UtcNow;
        var state = ExecutionState.Detected;

        try
        {
            _logger.LogInformation("Starting execution for prompt in session {ProcessId}",
                prompt.Session.TerminalProcessId);

            // Check if already handled
            if (IsPromptAlreadyHandled(prompt))
            {
                return CreateFailureResult(prompt, startTime, 0,
                    "Prompt already handled", ExecutionState.Failed);
            }

            // Step 1: Re-detect the prompt
            _logger.LogDebug("Re-detecting prompt");
            var redetected = _detector.DetectPrompt(prompt.Session);
            if (redetected == null)
            {
                return CreateFailureResult(prompt, startTime, 0,
                    "Prompt no longer present", ExecutionState.Failed);
            }

            state = ExecutionState.Verified;

            // Step 2: Verify the "allow from project" option is available
            if (!redetected.Request.HasAllowFromProjectOption ||
                !redetected.Request.AllowFromProjectOptionNumber.HasValue)
            {
                return CreateFailureResult(prompt, startTime, 0,
                    "Allow from project option not found", ExecutionState.Failed);
            }

            var optionNumber = redetected.Request.AllowFromProjectOptionNumber.Value;
            _logger.LogInformation("Target option number: {OptionNumber}", optionNumber);

            // Step 3: Attempt execution with retries
            for (int attempt = 0; attempt <= _config.MaxRetryAttempts; attempt++)
            {
                if (attempt > 0)
                {
                    _logger.LogInformation("Retry attempt {Attempt} of {Max}",
                        attempt, _config.MaxRetryAttempts);
                    Thread.Sleep(_config.RetryDelayMs);
                }

                var attemptResult = ExecuteAttempt(prompt, redetected, optionNumber, ref state);

                if (attemptResult.Success)
                {
                    MarkPromptAsHandled(prompt);
                    return attemptResult;
                }

                // If failure was due to focus issues, retry
                if (!attemptResult.ForegroundVerified && attempt < _config.MaxRetryAttempts)
                {
                    _logger.LogWarning("Focus verification failed, will retry");
                    continue;
                }

                // Other failures should not retry
                if (attemptResult.ErrorMessage?.Contains("no longer present") == true ||
                    attemptResult.ErrorMessage?.Contains("option not found") == true)
                {
                    return attemptResult;
                }
            }

            return CreateFailureResult(prompt, startTime, optionNumber,
                $"Execution failed after {_config.MaxRetryAttempts + 1} attempts",
                ExecutionState.Failed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during execution");
            return CreateFailureResult(prompt, startTime, 0,
                $"Exception: {ex.Message}", ExecutionState.Failed);
        }
    }

    private ExecutionResult ExecuteAttempt(
        DetectedPrompt originalPrompt,
        DetectedPrompt redetected,
        int optionNumber,
        ref ExecutionState state)
    {
        var startTime = DateTime.UtcNow;

        // Step 3.1: Bring terminal to foreground
        _logger.LogDebug("Bringing terminal to foreground");
        var targetHwnd = redetected.Session.TerminalWindowHandle;

        if (!SetForegroundWindow(targetHwnd))
        {
            _logger.LogWarning("SetForegroundWindow returned false");
        }

        // Step 3.2: Wait for focus transition
        Thread.Sleep(_config.FocusDelayMs);

        // Step 3.3: CRITICAL - Verify foreground window
        var foregroundHwnd = GetForegroundWindow();
        bool foregroundVerified = foregroundHwnd == targetHwnd;

        if (_config.RequireForegroundVerification && !foregroundVerified)
        {
            _logger.LogError("Foreground verification FAILED. Target: 0x{Target:X}, Actual: 0x{Actual:X}",
                targetHwnd.ToInt64(), foregroundHwnd.ToInt64());

            return new ExecutionResult
            {
                Success = false,
                ExecutedAt = startTime,
                Prompt = originalPrompt,
                SelectedOptionNumber = optionNumber,
                ErrorMessage = "Foreground verification failed - ABORTED to prevent wrong window input",
                ExecutionDuration = DateTime.UtcNow - startTime,
                FinalState = ExecutionState.Failed,
                ForegroundVerified = false,
                RetryCount = 0
            };
        }

        state = ExecutionState.Focused;
        _logger.LogInformation("Foreground verified: 0x{Hwnd:X}", foregroundHwnd.ToInt64());

        // Step 3.4: Send keyboard input
        _logger.LogInformation("Sending option {Number}", optionNumber);
        SendKeyPress(optionNumber.ToString()[0]);

        Thread.Sleep(_config.KeyPressDelayMs);

        _logger.LogDebug("Sending Enter");
        SendKeyPress('\r');

        state = ExecutionState.InputSent;

        // Step 3.5: Wait for processing
        Thread.Sleep(_config.VerificationDelayMs);

        state = ExecutionState.Verifying;

        // Step 3.6: Verify prompt disappeared
        _logger.LogDebug("Verifying prompt disappeared");
        var stillPresent = _detector.DetectPrompt(redetected.Session);
        var promptDisappeared = stillPresent == null;

        if (promptDisappeared)
        {
            _logger.LogInformation("Prompt disappeared - execution successful");
            state = ExecutionState.Success;

            return new ExecutionResult
            {
                Success = true,
                ExecutedAt = startTime,
                Prompt = originalPrompt,
                SelectedOptionNumber = optionNumber,
                PromptDisappeared = true,
                ExecutionDuration = DateTime.UtcNow - startTime,
                FinalState = ExecutionState.Success,
                ForegroundVerified = foregroundVerified,
                RetryCount = 0
            };
        }
        else
        {
            _logger.LogWarning("Prompt still present after execution");
            return new ExecutionResult
            {
                Success = false,
                ExecutedAt = startTime,
                Prompt = originalPrompt,
                SelectedOptionNumber = optionNumber,
                ErrorMessage = "Prompt still present after input",
                PromptDisappeared = false,
                ExecutionDuration = DateTime.UtcNow - startTime,
                FinalState = ExecutionState.Failed,
                ForegroundVerified = foregroundVerified,
                RetryCount = 0
            };
        }
    }

    public bool IsPromptAlreadyHandled(DetectedPrompt prompt)
    {
        lock (_lock)
        {
            var key = GetPromptKey(prompt);
            return _handledPrompts.Contains(key);
        }
    }

    public void MarkPromptAsHandled(DetectedPrompt prompt)
    {
        lock (_lock)
        {
            var key = GetPromptKey(prompt);
            _handledPrompts.Add(key);
            _promptFirstSeen[key] = prompt.DetectedAt;

            // Cleanup old entries
            if (_handledPrompts.Count > 1000)
            {
                var cutoff = DateTime.UtcNow.AddHours(-1);
                var oldKeys = _promptFirstSeen
                    .Where(kvp => kvp.Value < cutoff)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var oldKey in oldKeys)
                {
                    _handledPrompts.Remove(oldKey);
                    _promptFirstSeen.Remove(oldKey);
                }

                _logger.LogDebug("Cleaned up {Count} old prompt entries", oldKeys.Count);
            }
        }
    }

    public void ClearHandledPrompts()
    {
        lock (_lock)
        {
            _handledPrompts.Clear();
            _promptFirstSeen.Clear();
            _logger.LogInformation("Cleared all handled prompts");
        }
    }

    private string GetPromptKey(DetectedPrompt prompt)
    {
        // Use session and raw text hash for uniqueness
        // This distinguishes same prompt in different sessions
        // and different prompts in same session
        var textHash = prompt.RawText.GetHashCode();
        return $"{prompt.Session.TerminalProcessId}_{prompt.Session.ClaudeProcessId}_{textHash}";
    }

    private ExecutionResult CreateFailureResult(
        DetectedPrompt prompt,
        DateTime startTime,
        int optionNumber,
        string errorMessage,
        ExecutionState state)
    {
        _logger.LogWarning("Execution failed: {Error}", errorMessage);

        return new ExecutionResult
        {
            Success = false,
            ExecutedAt = startTime,
            Prompt = prompt,
            SelectedOptionNumber = optionNumber,
            ErrorMessage = errorMessage,
            ExecutionDuration = DateTime.UtcNow - startTime,
            FinalState = state,
            ForegroundVerified = false,
            RetryCount = 0
        };
    }

    private void SendKeyPress(char key)
    {
        var inputs = new INPUT[2];

        // Key down
        inputs[0] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0,
                    wScan = key,
                    dwFlags = KEYEVENTF_UNICODE,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        // Key up
        inputs[1] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0,
                    wScan = key,
                    dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        var sent = SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
        _logger.LogDebug("SendInput sent {Count} inputs for key '{Key}'", sent, key);
    }

    #region Windows API

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    private const int INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_UNICODE = 0x0004;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public int type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;
        [FieldOffset(0)]
        public KEYBDINPUT ki;
        [FieldOffset(0)]
        public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    #endregion
}
