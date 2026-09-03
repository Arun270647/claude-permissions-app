using System.Diagnostics;
using System.Runtime.InteropServices;
using ClaudePermissionAssistant.Core.Interfaces;
using ClaudePermissionAssistant.Core.Models;

namespace ClaudePermissionAssistant.Automation.Services;

public class ClaudePermissionPromptExecutor : IClaudePermissionPromptExecutor
{
    private readonly IClaudePromptDetector _detector;
    private readonly HashSet<string> _handledPrompts = new();
    private int _contextSequence = 0;
    private readonly object _lock = new();

    public ClaudePermissionPromptExecutor(IClaudePromptDetector detector)
    {
        _detector = detector;
    }

    public ExecutionResult Execute(DetectedPrompt prompt)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            // Check if already handled
            if (IsPromptAlreadyHandled(prompt))
            {
                return new ExecutionResult
                {
                    Success = false,
                    ExecutedAt = startTime,
                    Prompt = prompt,
                    SelectedOptionNumber = 0,
                    ErrorMessage = "Prompt already handled",
                    ExecutionDuration = TimeSpan.Zero
                };
            }

            // Verify prompt is still present
            var redetected = _detector.DetectPrompt(prompt.Session);
            if (redetected == null)
            {
                return new ExecutionResult
                {
                    Success = false,
                    ExecutedAt = startTime,
                    Prompt = prompt,
                    SelectedOptionNumber = 0,
                    ErrorMessage = "Prompt no longer present",
                    ExecutionDuration = DateTime.UtcNow - startTime
                };
            }

            // Verify the "allow from project" option is available
            if (!prompt.Request.HasAllowFromProjectOption || !prompt.Request.AllowFromProjectOptionNumber.HasValue)
            {
                return new ExecutionResult
                {
                    Success = false,
                    ExecutedAt = startTime,
                    Prompt = prompt,
                    SelectedOptionNumber = 0,
                    ErrorMessage = "Allow from project option not found",
                    ExecutionDuration = DateTime.UtcNow - startTime
                };
            }

            var optionNumber = prompt.Request.AllowFromProjectOptionNumber.Value;

            // Bring terminal window to foreground
            if (!SetForegroundWindow(prompt.Session.TerminalWindowHandle))
            {
                return new ExecutionResult
                {
                    Success = false,
                    ExecutedAt = startTime,
                    Prompt = prompt,
                    SelectedOptionNumber = optionNumber,
                    ErrorMessage = "Failed to bring terminal window to foreground",
                    ExecutionDuration = DateTime.UtcNow - startTime
                };
            }

            // Small delay to ensure window is focused
            Thread.Sleep(100);

            // Send the option number
            SendKeyPress(optionNumber.ToString()[0]);

            // Small delay between key presses
            Thread.Sleep(50);

            // Send Enter
            SendKeyPress('\r');

            // Wait a moment for the prompt to process
            Thread.Sleep(200);

            // Verify prompt disappeared
            var stillPresent = _detector.DetectPrompt(prompt.Session);
            var promptDisappeared = stillPresent == null;

            // Mark as handled
            MarkPromptAsHandled(prompt);

            return new ExecutionResult
            {
                Success = true,
                ExecutedAt = startTime,
                Prompt = prompt,
                SelectedOptionNumber = optionNumber,
                PromptDisappeared = promptDisappeared,
                ExecutionDuration = DateTime.UtcNow - startTime
            };
        }
        catch (Exception ex)
        {
            return new ExecutionResult
            {
                Success = false,
                ExecutedAt = startTime,
                Prompt = prompt,
                SelectedOptionNumber = 0,
                ErrorMessage = $"Exception: {ex.Message}",
                ExecutionDuration = DateTime.UtcNow - startTime
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

            // Limit size to prevent memory leak
            if (_handledPrompts.Count > 1000)
            {
                _handledPrompts.Clear();
            }
        }
    }

    public void ClearHandledPrompts()
    {
        lock (_lock)
        {
            _handledPrompts.Clear();
        }
    }

    public void CleanupOldHandledPrompts()
    {
        // Not implemented in this executor (used for compatibility only)
    }

    public void IncrementContextSequence()
    {
        lock (_lock)
        {
            _contextSequence++;
        }
    }

    public int GetContextSequence()
    {
        lock (_lock)
        {
            return _contextSequence;
        }
    }

    private string GetPromptKey(DetectedPrompt prompt)
    {
        // Create a unique key based on session and prompt content
        return $"{prompt.Session.TerminalProcessId}_{prompt.Session.ClaudeProcessId}_{_contextSequence}_{prompt.DetectedAt.Ticks}";
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

        SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    #region Windows API

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

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
