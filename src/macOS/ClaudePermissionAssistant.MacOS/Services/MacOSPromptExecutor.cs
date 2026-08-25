using ClaudePermissionAssistant.Core.Interfaces;
using ClaudePermissionAssistant.Core.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace ClaudePermissionAssistant.MacOS.Services;

/// <summary>
/// macOS-specific prompt executor using AppleScript for keystroke injection
/// </summary>
public class MacOSPromptExecutor : IClaudePermissionPromptExecutor
{
    private static readonly object _executionGate = new();
    private readonly IClaudePromptDetector _detector;
    private readonly ILogger<MacOSPromptExecutor> _logger;
    private readonly ExecutorConfiguration _config;
    private readonly Dictionary<string, DateTime> _handledPrompts = new();
    private readonly object _lock = new();
    private static readonly TimeSpan DuplicateCooldown = TimeSpan.FromSeconds(10);

    public MacOSPromptExecutor(
        IClaudePromptDetector detector,
        ILogger<MacOSPromptExecutor> logger,
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

            // Mark as handled IMMEDIATELY to prevent duplicate execution attempts
            MarkPromptAsHandled(prompt);

            // Step 1: Re-detect the prompt
            _logger.LogDebug("Re-detecting prompt");
            var redetected = _detector.DetectPrompt(prompt.Session);
            if (redetected == null)
            {
                return CreateFailureResult(prompt, startTime, 0,
                    "Prompt no longer present", ExecutionState.Failed);
            }

            state = ExecutionState.Verified;

            // Step 2: Verify an approval option is available
            if (!redetected.Request.BestApprovalOptionNumber.HasValue)
            {
                return CreateFailureResult(prompt, startTime, 0,
                    "No approval option found", ExecutionState.Failed);
            }

            var optionNumber = redetected.Request.BestApprovalOptionNumber.Value;
            _logger.LogInformation("Target option number: {OptionNumber}", optionNumber);

            // Step 3: Attempt execution with global lock
            lock (_executionGate)
            {
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
                        return attemptResult;
                    }

                    if (attemptResult.ErrorMessage?.Contains("no longer present") == true)
                    {
                        return attemptResult;
                    }
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

        try
        {
            // Step 1: Activate Terminal.app
            _logger.LogDebug("Activating Terminal.app");
            if (!ActivateTerminal())
            {
                return CreateFailureResult(originalPrompt, startTime, optionNumber,
                    "Failed to activate terminal", ExecutionState.Failed);
            }

            Thread.Sleep(_config.FocusDelayMs);
            state = ExecutionState.Focused;

            // Step 2: Send keystroke using AppleScript
            _logger.LogInformation("Sending option {Number}", optionNumber);
            if (!SendKeystroke(optionNumber.ToString()))
            {
                return CreateFailureResult(originalPrompt, startTime, optionNumber,
                    "Failed to send option number", ExecutionState.Failed);
            }

            Thread.Sleep(_config.KeyPressDelayMs);

            // Send Enter
            _logger.LogDebug("Sending Enter");
            if (!SendKeystroke("return"))
            {
                return CreateFailureResult(originalPrompt, startTime, optionNumber,
                    "Failed to send Enter", ExecutionState.Failed);
            }

            state = ExecutionState.InputSent;

            // Step 3: Wait and verify prompt disappeared
            Thread.Sleep(_config.VerificationDelayMs);
            state = ExecutionState.Verifying;

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
                    ForegroundVerified = true,
                    RetryCount = 0
                };
            }
            else
            {
                _logger.LogWarning("Prompt still present after execution");
                return CreateFailureResult(originalPrompt, startTime, optionNumber,
                    "Prompt still present after input", ExecutionState.Failed);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during execution attempt");
            return CreateFailureResult(originalPrompt, startTime, optionNumber,
                $"Exception: {ex.Message}", ExecutionState.Failed);
        }
    }

    private bool ActivateTerminal()
    {
        var script = @"tell application ""Terminal"" to activate";
        return ExecuteAppleScript(script);
    }

    private bool SendKeystroke(string key)
    {
        // SECURITY FIX: Validate and sanitize input to prevent AppleScript injection
        var sanitizedKey = ValidateAndSanitizeKeystroke(key);

        if (sanitizedKey == null)
        {
            _logger.LogError("Invalid keystroke attempted: {Key}", key);
            return false;
        }

        // AppleScript to send keystroke to Terminal.app
        var script = $@"
tell application ""System Events""
    tell process ""Terminal""
        keystroke ""{sanitizedKey}""
    end tell
end tell
";
        return ExecuteAppleScript(script);
    }

    /// <summary>
    /// SECURITY FIX: Whitelist-based validation for keystrokes
    /// </summary>
    private string? ValidateAndSanitizeKeystroke(string key)
    {
        // Whitelist: only allow single digits, letters, or special keywords
        if (key == "return" || key == "tab" || key == "escape")
        {
            return key;
        }

        // Allow single alphanumeric characters
        if (key.Length == 1 && char.IsLetterOrDigit(key[0]))
        {
            // Escape double quotes for AppleScript
            return key.Replace("\"", "\\\"");
        }

        // Reject anything else
        return null;
    }

    private bool ExecuteAppleScript(string script)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "osascript",
                Arguments = "-e \"" + script.Replace("\"", "\\\"") + "\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
                return false;

            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
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

            if (_handledPrompts.Count > 1000)
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

                _logger.LogDebug("Cleaned up {Count} old prompt entries", oldKeys.Count);
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
    /// SECURITY FIX: Use cryptographic hashing instead of GetHashCode() for stable, collision-resistant identity
    /// </summary>
    private string GetPromptKey(DetectedPrompt prompt)
    {
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
}
