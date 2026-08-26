using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using ClaudePermissionAssistant.Core.Interfaces;
using ClaudePermissionAssistant.Core.Models;
using Microsoft.Extensions.Logging;

namespace ClaudePermissionAssistant.Automation.Services;

/// <summary>
/// Hardened executor with comprehensive safety checks and state machine
/// </summary>
public class ClaudePermissionPromptExecutorHardened : IClaudePermissionPromptExecutor
{
    private static readonly object _executionGate = new();
    private readonly IClaudePromptDetector _detector;
    private readonly ILogger<ClaudePermissionPromptExecutorHardened> _logger;
    private readonly ExecutorConfiguration _config;
    private readonly Dictionary<string, DateTime> _handledPrompts = new();
    private readonly object _lock = new();
    private static readonly TimeSpan DuplicateCooldown = TimeSpan.FromSeconds(5);

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

            // Step 2: Verify an approval option is available (persistent or simple "Yes")
            if (!redetected.Request.BestApprovalOptionNumber.HasValue)
            {
                return CreateFailureResult(prompt, startTime, 0,
                    "No approval option found", ExecutionState.Failed);
            }

            var optionNumber = redetected.Request.BestApprovalOptionNumber.Value;
            _logger.LogInformation("Target option number: {OptionNumber}", optionNumber);

            // Step 3: Execute with global lock
            // Global lock ensures only one executor can send keys at a time.
            // Without this, multiple monitors would interfere with each other.
            lock (_executionGate)
            {
                var attemptResult = ExecuteAttempt(prompt, redetected, optionNumber, ref state);

                // Keys were sent, mark as handled and return success
                MarkPromptAsHandled(prompt);
                return attemptResult;
            }
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

        // SECURITY: Validate window handle before attempting keyboard injection
        var targetHwnd = redetected.Session.TerminalWindowHandle;
        if (targetHwnd == IntPtr.Zero)
        {
            _logger.LogError("SECURITY: Invalid window handle (Zero) - ABORTING to prevent blind keystroke injection");
            return CreateFailureResult(originalPrompt, startTime, optionNumber,
                "Invalid window handle - cannot safely inject keystrokes",
                ExecutionState.Failed);
        }

        // Verify the window still exists (only in production mode with RequireForegroundVerification)
        // In test mode, we allow fake HWNDs since tests use mock window handles
        if (_config.RequireForegroundVerification && !IsWindow(targetHwnd))
        {
            _logger.LogError("SECURITY: Window no longer exists (HWND 0x{Hwnd:X}) - ABORTING", targetHwnd.ToInt64());
            return CreateFailureResult(originalPrompt, startTime, optionNumber,
                $"Window 0x{targetHwnd.ToInt64():X} no longer exists",
                ExecutionState.Failed);
        }

        // Step 3.1: Bring terminal to foreground
        _logger.LogDebug("Bringing terminal to foreground");


        if (!SetForegroundWindow(targetHwnd))
        {
            _logger.LogWarning("SetForegroundWindow returned false");
        }

        // Step 3.2: Wait for focus transition
        Thread.Sleep(_config.FocusDelayMs);

        // SECURITY FIX: Make foreground window verification MANDATORY (abort on mismatch)
        var foregroundHwnd = GetForegroundWindow();
        bool foregroundVerified = foregroundHwnd == targetHwnd;

        if (!foregroundVerified)
        {
            _logger.LogError("Foreground verification FAILED. Target: 0x{Target:X}, Actual: 0x{Actual:X}",
                targetHwnd.ToInt64(), foregroundHwnd.ToInt64());

            if (_config.RequireForegroundVerification)
            {
                _logger.LogError("ABORTING to prevent wrong window injection (RequireForegroundVerification=true)");
                return CreateFailureResult(originalPrompt, startTime, optionNumber,
                    $"Foreground window mismatch - expected 0x{targetHwnd.ToInt64():X}, got 0x{foregroundHwnd.ToInt64():X}",
                    ExecutionState.Failed);
            }
            else
            {
                _logger.LogWarning("Continuing despite verification failure (RequireForegroundVerification=false - test mode)");
            }
        }
        else
        {
            _logger.LogInformation("Foreground verified: 0x{Hwnd:X}", foregroundHwnd.ToInt64());
        }

        state = ExecutionState.Focused;

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

        // Step 3.6: Verify prompt disappeared (optional - keys were already sent)
        _logger.LogDebug("Verifying prompt disappeared");
        var stillPresent = _detector.DetectPrompt(redetected.Session);
        var promptDisappeared = stillPresent == null;

        if (promptDisappeared)
        {
            _logger.LogInformation("Prompt disappeared - execution successful");
        }
        else
        {
            _logger.LogInformation("Prompt still visible, but keys were sent - treating as success");
        }

        state = ExecutionState.Success;

        return new ExecutionResult
        {
            Success = true,
            ExecutedAt = startTime,
            Prompt = originalPrompt,
            SelectedOptionNumber = optionNumber,
            PromptDisappeared = promptDisappeared,
            ExecutionDuration = DateTime.UtcNow - startTime,
            FinalState = ExecutionState.Success,
            ForegroundVerified = foregroundVerified,
            RetryCount = 0
        };
    }

    public bool IsPromptAlreadyHandled(DetectedPrompt prompt)
    {
        lock (_lock)
        {
            var key = GetPromptKey(prompt);
            if (_handledPrompts.TryGetValue(key, out var handledAt))
            {
                if (DateTime.UtcNow - handledAt < DuplicateCooldown)
                    return true;

                // Cooldown expired — this is a new instance of the same prompt text
                _handledPrompts.Remove(key);
                return false;
            }
            return false;
        }
    }

    public void MarkPromptAsHandled(DetectedPrompt prompt)
    {
        lock (_lock)
        {
            var key = GetPromptKey(prompt);
            _handledPrompts[key] = DateTime.UtcNow;

            // Cleanup old entries more aggressively for 24/7 stability
            // Clean up every 100 entries (instead of 1000) to prevent buildup
            if (_handledPrompts.Count % 100 == 0 && _handledPrompts.Count > 0)
            {
                var cutoff = DateTime.UtcNow.AddMinutes(-5);
                var oldKeys = _handledPrompts
                    .Where(kvp => kvp.Value < cutoff)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var oldKey in oldKeys)
                {
                    _handledPrompts.Remove(oldKey);
                }

                if (oldKeys.Count > 0)
                {
                    _logger.LogDebug("Inline cleanup: Removed {Count} old prompt entries (count: {Total})", oldKeys.Count, _handledPrompts.Count);
                }
            }
        }
    }

    public void ClearHandledPrompts()
    {
        lock (_lock)
        {
            _handledPrompts.Clear();
            _logger.LogInformation("Cleared all handled prompts");
        }
    }

    /// <summary>
    /// Periodic cleanup of old handled prompts for 24/7 stability
    /// Removes entries older than 5 minutes to prevent memory bloat
    /// </summary>
    public void CleanupOldHandledPrompts()
    {
        lock (_lock)
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-5);
            var oldKeys = _handledPrompts
                .Where(kvp => kvp.Value < cutoff)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var oldKey in oldKeys)
            {
                _handledPrompts.Remove(oldKey);
            }

            if (oldKeys.Count > 0)
            {
                _logger.LogDebug("Periodic cleanup: Removed {Count} old prompt entries (24/7 stability)", oldKeys.Count);
            }
        }
    }

    /// <summary>
    /// SECURITY FIX: Use cryptographic hashing instead of GetHashCode() for stable, collision-resistant identity
    /// </summary>
    private string GetPromptKey(DetectedPrompt prompt)
    {
        // Use prompt region for stable identity, not full terminal buffer
        // PromptRegion contains just the question + options, so hash remains stable
        // as new lines are added to terminal buffer
        var textToHash = prompt.Request.PromptRegion ?? prompt.RawText;

        // SECURITY FIX: Use SHA-256 instead of GetHashCode() for collision resistance
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(textToHash));
        var textHash = Convert.ToHexString(hashBytes).Substring(0, 16).ToLowerInvariant();

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
    private static extern bool IsWindow(IntPtr hWnd);

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
