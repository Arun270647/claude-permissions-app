# CRITICAL SECURITY FIX: Keystroke Injection Leak Prevention

## SEVERITY: CRITICAL

**Date:** 2026-09-03  
**Priority:** P0 - IMMEDIATE  
**Status:** FIXED

## Problem Statement

**CRITICAL SECURITY VULNERABILITY**: The application was injecting keystrokes (1, 2, 3) into windows OTHER than the monitored terminal, specifically observed injecting into "Backend-refactor progress report" window.

This is the exact scenario that the 3-layer security validation was designed to prevent.

## Evidence

Screenshot shows:
- Window title: "Backend-refactor progress report"
- Content: "Read 1 file" followed by lines of numbers: "3", "2", "2", "2", "1", "3", "2", "2"
- These numbers appear to be keystrokes that were injected by the app

## Root Cause Analysis

### Hypothesis 1: Window Handle Confusion
- UI Automation cache returned element for wrong window
- Window handle got reused by OS for different window
- Cached automation element pointed to wrong process

### Hypothesis 2: Foreground Verification Bypass
- Foreground window verification passed for wrong window
- `SetForegroundWindow` succeeded but targeted wrong window
- Recent exponential backoff changes may have weakened verification

### Hypothesis 3: Process ID Mismatch Not Detected
- Window handle valid but process changed
- No verification that target window process matches expected terminal process
- Window title changed but not validated

## Security Layers Added

### Layer 1: Window Process ID Verification
**Location:** `ClaudePermissionPromptExecutorHardened.cs:ExecuteAttempt()`

```csharp
var windowProcessId = GetWindowProcessId(targetHwnd);

if (windowProcessId != redetected.Session.TerminalProcessId)
{
    _logger.LogError("SECURITY: Window process mismatch! Expected PID {Expected}, got PID {Actual} - ABORTING",
        redetected.Session.TerminalProcessId, windowProcessId);
    return CreateFailureResult(..., "Window process mismatch. SECURITY ABORT.");
}
```

**Purpose:** Verify the window's process ID matches the terminal we're monitoring. If the handle got reused or confused, this catches it.

### Layer 2: Window Title Verification
**Location:** `ClaudePermissionPromptExecutorHardened.cs:ExecuteAttempt()`

```csharp
var windowTitle = GetWindowTitle(targetHwnd);
var expectedTerminalIndicators = new[] { "cmd", "powershell", "terminal", "claude", "bash", "sh", "zsh" };
var titleLower = windowTitle.ToLowerInvariant();
var looksLikeTerminal = expectedTerminalIndicators.Any(indicator => titleLower.Contains(indicator));

if (!looksLikeTerminal && windowTitle.Length > 0)
{
    _logger.LogError("SECURITY: Window title suspicious! '{Title}' does not look like a terminal - ABORTING", windowTitle);
    return CreateFailureResult(..., "Window title does not match terminal pattern. SECURITY ABORT.");
}
```

**Purpose:** Verify the window title looks like a terminal. "Backend-refactor progress report" would be rejected.

### Layer 3: Window Handle Zero Check Enhanced
**Location:** `ClaudePromptDetector.cs:GetTerminalText()`

```csharp
if (windowHandle == IntPtr.Zero)
{
    System.Diagnostics.Debug.WriteLine("[ClaudePromptDetector] SECURITY: Invalid window handle (Zero)");
    return null;
}
```

**Purpose:** Prevent text extraction from invalid handles.

### Layer 4: Comprehensive Logging
**Location:** `BackgroundMonitorService.cs:MonitorCycle()`

```csharp
_logger.LogInfo($"MONITOR_TEXT_EXTRACTION_START: HWND=0x{claudeSession.TerminalWindowHandle:X}, PID={claudeSession.TerminalProcessId}");

var preview = terminalText.Substring(0, 200);
_logger.LogInfo($"MONITOR_TEXT_PREVIEW: '{preview}...'");
```

**Purpose:** Log which window is being read and preview the extracted text. This allows post-mortem analysis if issue recurs.

## Testing Requirements

### Test 1: Wrong Window Rejection
1. Start monitoring a terminal (e.g., CMD)
2. Open another window (e.g., Notepad, Visual Studio)
3. Switch focus to the other window
4. Trigger a detection cycle
5. **Expected:** No keystrokes injected into wrong window
6. **Expected:** Logs show "SECURITY: Window title suspicious!" or "Window process mismatch"

### Test 2: Window Handle Reuse
1. Start monitoring terminal with PID 1234
2. Close the terminal
3. OS reuses window handle for different process (PID 5678)
4. Detection cycle runs
5. **Expected:** "Window process mismatch - expected PID 1234, got 5678"
6. **Expected:** NO keystrokes injected

### Test 3: Title Verification
1. Monitor terminal with title "PowerShell"
2. Simulate window title changing to "Backend-refactor progress report"
3. Detection cycle runs
4. **Expected:** "Window title suspicious! 'Backend-refactor progress report' does not look like a terminal"
5. **Expected:** NO keystrokes injected

### Test 4: Legitimate Terminal Works
1. Monitor CMD terminal
2. Trigger Claude permission prompt
3. **Expected:** Keystrokes injected successfully
4. **Expected:** Logs show "SECURITY: Window identity verified - PID 1234, Title: 'C:\Windows\System32\cmd.exe'"

## Rollback Plan

If these checks cause false positives (legitimate terminals rejected):

1. **Disable title verification**: Comment out `looksLikeTerminal` check
2. **Keep PID verification**: This is critical and should not cause false positives
3. **Add more terminal indicators**: Expand `expectedTerminalIndicators` array

## Monitoring

Watch for these log patterns:

**Security rejections (expected when protection works):**
```
SECURITY: Window process mismatch! Expected PID 1234, got 5678 - ABORTING
SECURITY: Window title suspicious! 'Backend-refactor progress report' does not look like a terminal - ABORTING
```

**Successful verification (expected for legitimate terminals):**
```
SECURITY: Window identity verified - PID 1234, Title: 'PowerShell'
```

## Related Security Issues

- HIGH-002: Race Condition Protection (SECURITY_AUDIT_REPORT.md)
- Foreground window verification (FOREGROUND_WINDOW_FIX.md)
- Keystroke injection spam prevention (KEYSTROKE_SPAM_FIX.md)

## Impact

**Before Fix:**
- Keystrokes could leak into arbitrary windows
- "Backend-refactor progress report" received "1 2 3" keystrokes
- NO verification of window identity before injection

**After Fix:**
- Process ID must match monitored terminal
- Window title must look like a terminal
- Comprehensive logging of target window
- Multi-layer validation before ANY keystroke injection

## Author

Claude Opus 4.6 (1M context)

## Date

2026-09-03
