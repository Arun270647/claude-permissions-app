# Foreground Window Mismatch Fix

## Problem Description

Users experiencing "Foreground window mismatch" errors when Claude Prompter attempts to auto-approve prompts, especially when Claude Code is running background agents that produce rapid terminal output.

**Error Message:**
```
Foreground window mismatch - expected 0x3C066A, got 0xB064A
```

## Root Cause Analysis

The issue occurs during the keystroke injection phase when:

1. **Rapid Terminal Activity**: Claude Code with background agents produces frequent terminal updates
2. **Focus Stealing**: Windows or other applications change focus during the retry window
3. **Insufficient Retry Logic**: Original implementation:
   - Only 3 retry attempts
   - Fixed 150ms focus delay
   - Fixed 100ms retry delay
   - Total retry window: ~300ms (too short for active terminals)

## Solution Implemented

### 1. Increased Retry Attempts
- **Before**: 3 attempts
- **After**: 5 attempts
- **Impact**: More opportunities to capture focus during active terminal updates

### 2. Longer Initial Delays
- **FocusDelayMs**: 150ms → 250ms
- **ForegroundRetryDelayMs**: 100ms → 200ms
- **Impact**: Allows more time for focus to stabilize

### 3. Exponential Backoff
Implemented progressive delays that increase with each retry:

**Focus delays**: 250ms, 350ms, 450ms, 550ms, 650ms
**Retry delays**: 200ms, 300ms, 400ms, 500ms

**Impact**: Early retries are quick for simple cases, later retries give more time for complex scenarios

### 4. Window Visibility Check
Added `ShowWindow(SW_RESTORE)` call before focus attempts:
- Ensures window is not minimized
- Brings window to visible state
- Brief 50ms pause for window to restore

### 5. Improved Focus Sequence
Each retry now:
1. Calls `BringWindowToTop()` first
2. Pauses 50ms
3. Calls `SetForegroundWindow()`
4. Waits with exponential backoff
5. Verifies focus

## Code Changes

### BackgroundMonitorService.cs
```csharp
// Configuration changes
FocusDelayMs = 250              // Was 150ms
ForegroundRetryAttempts = 5     // Was 3
ForegroundRetryDelayMs = 200    // Was 100ms
```

### ClaudePermissionPromptExecutorHardened.cs
```csharp
// Added ShowWindow call
ShowWindow(targetHwnd, SW_RESTORE);
Thread.Sleep(50);

// Exponential backoff for focus delay
var delayMs = _config.FocusDelayMs + (attempt * 100);
Thread.Sleep(delayMs);

// Exponential backoff for retry delay
var retryDelay = _config.ForegroundRetryDelayMs + (attempt * 100);
Thread.Sleep(retryDelay);
```

## Testing Instructions

### Prerequisites
- Claude Code running with background agents (e.g., workflow, parallel agents)
- Claude Prompter monitoring the terminal
- Active terminal output (rapid scrolling)

### Test Scenarios

#### 1. Active Background Agents
```bash
# Start Claude Code with background agents
claude "run a workflow with 3 parallel agents"

# Expected: Prompts should be auto-approved despite rapid output
# Verify: Check logs show successful foreground verification
```

#### 2. Rapid Window Switching
```bash
# Start Claude Code
# Manually switch focus between terminal and other windows rapidly
# Trigger a permission prompt

# Expected: Prompter should successfully capture focus within 5 attempts
# Verify: Statistics show successful approval, not failures
```

#### 3. Minimized Window
```bash
# Minimize the terminal window
# Trigger a permission prompt

# Expected: Window should restore and focus should be captured
# Verify: Logs show "Foreground verified" message
```

### Verification Steps

1. **Check Statistics**:
   - Prompts Approved count increases
   - Prompts Failed count does NOT increase
   - Last Error shows no foreground mismatch

2. **Check Logs** (`logs/ClaudePrompter_*.log`):
   - Look for: `Foreground verified: 0x{Hwnd:X} (attempt N, delay Xms)`
   - Should see successful verification within 1-5 attempts
   - No "ABORTING after N attempts" errors

3. **Monitor Performance**:
   - Approval should complete within 2-3 seconds
   - Terminal should briefly gain focus
   - Keystrokes should be injected correctly

## Performance Impact

- **Best Case** (no focus issues): +100ms (one extra attempt with longer delay)
- **Typical Case** (minor focus changes): +200-400ms (2-3 attempts)
- **Worst Case** (very active terminal): +1-2 seconds (4-5 attempts)

Trade-off: Slightly longer processing time in exchange for significantly higher success rate.

## Failure Scenarios

Even with these improvements, failures may still occur if:

1. **User manually steals focus** during all 5 attempts
2. **Terminal crashes** or becomes unresponsive
3. **System blocks SetForegroundWindow** (Windows security policy)
4. **Terminal window handle becomes invalid** between detection and execution

In these cases, the error is legitimate and should be reported to the user.

## Monitoring

Key log messages to watch:

**Success**:
```
Foreground verified: 0x3C066A (attempt 2, delay 350ms)
```

**Retry in progress**:
```
Foreground verification attempt 2 failed. Target: 0x3C066A, Actual: 0xB064A
Waiting 300ms before retry 3
```

**Failure** (after 5 attempts):
```
ABORTING after 5 attempts to set foreground window
```

## Rollback Plan

If this fix causes issues:

1. Revert changes to `BackgroundMonitorService.cs` and `ClaudePermissionPromptExecutorHardened.cs`
2. Original values:
   - `FocusDelayMs = 150`
   - `ForegroundRetryAttempts = 3`
   - `ForegroundRetryDelayMs = 100`
3. Remove `ShowWindow()` call
4. Remove exponential backoff logic

## Related Issues

- HIGH-002: Race Condition Protection (SECURITY_AUDIT_REPORT.md)
- Foreground window verification is a critical security feature
- This fix maintains security while improving reliability

## Author

Claude Code (Opus 4.6)

## Date

2026-09-03
