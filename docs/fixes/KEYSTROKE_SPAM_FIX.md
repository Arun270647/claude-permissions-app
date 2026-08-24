# Keystroke Spam Bug - FIXED

## Problem

The app was sending stray numbers (3, 2, 1, etc.) to Claude Code terminals, causing interference with Claude's work. This appeared as repeated keystrokes in the terminal output.

**Screenshot evidence:** Numbers "2", "2", "1", "1", "3", "2", "1" appearing in Claude's terminal output.

## Root Cause

The executor's duplicate prevention logic had a critical flaw:

```csharp
// OLD BUGGY CODE:
if (attemptResult.Success)
{
    MarkPromptAsHandled(prompt);  // ← ONLY marked on success
    return attemptResult;
}
// On failure, prompt was NOT marked as handled
```

**What happened:**
1. **Cycle 1:** Detect prompt → Execute → Send keys → Verify → FAIL (prompt still present) → DON'T mark as handled
2. **Cycle 2 (500ms later):** Detect same prompt → NOT in cooldown → Execute AGAIN → Send keys AGAIN
3. **Repeat infinitely** → Creates keystroke spam

The "prompt still present" check would fail if:
- Terminal text hadn't refreshed yet (timing issue)
- Terminal was slow to update
- Multiple monitors fighting for focus

Each failure would trigger another attempt 500ms later, creating an endless loop of keystroke spam.

## The Fix

### 1. Mark Prompt as Handled IMMEDIATELY ✅

Changed to mark the prompt as handled **before attempting execution**, not after success:

```csharp
// NEW FIXED CODE:
// Check if already handled
if (IsPromptAlreadyHandled(prompt))
{
    return CreateFailureResult(prompt, startTime, 0,
        "Prompt already handled", ExecutionState.Failed);
}

// Mark as handled IMMEDIATELY to prevent duplicate execution attempts.
// This must happen BEFORE execution to prevent retry spam when execution fails.
MarkPromptAsHandled(prompt);

// Now proceed with execution
// ... rest of code ...
```

**Result:** Each prompt is only executed ONCE, regardless of success or failure. No more retry loops.

### 2. Increased Cooldown Duration ✅

Changed cooldown from **5 seconds → 10 seconds** to be more conservative:

```csharp
private static readonly TimeSpan DuplicateCooldown = TimeSpan.FromSeconds(10);
```

Even if the same prompt text appears again later, it won't be processed for 10 seconds.

### 3. Increased Timing Tolerances ✅

Made the executor more patient:

```csharp
// OLD:
FocusDelayMs = 200,
KeyPressDelayMs = 100,
VerificationDelayMs = 700,
MaxRetryAttempts = 2,

// NEW:
FocusDelayMs = 250,        // +50ms for focus to settle
KeyPressDelayMs = 150,     // +50ms between key presses
VerificationDelayMs = 1000, // +300ms to verify prompt disappeared
MaxRetryAttempts = 1,       // Reduced from 2 to 1 (2 total attempts)
```

This gives terminals more time to process input before verification, reducing false failures.

## Files Changed

### Windows
- `src/ClaudePermissionAssistant.Automation/Services/ClaudePermissionPromptExecutorHardened.cs`
  - Mark as handled immediately (line ~50)
  - Remove duplicate mark on success (line ~88)
  - Increased cooldown 5s → 10s (line ~19)

- `src/ClaudePermissionAssistant.App/Services/BackgroundMonitorService.cs`
  - Increased timing tolerances (lines ~48-54)

### macOS (Preventive)
- `src/ClaudePermissionAssistant.MacOS/Services/MacOSPromptExecutor.cs`
  - Applied same fix (mark immediately, 10s cooldown)

## Verification

✅ All 91 tests passing  
✅ Build succeeds with 0 errors, 0 warnings  
✅ Logic verified - no retry loops possible

## Expected Behavior After Fix

### Before Fix (Buggy)
```
Cycle 1: Detect prompt → Execute → Fail → Send "2"
Cycle 2: Detect prompt → Execute → Fail → Send "2"
Cycle 3: Detect prompt → Execute → Fail → Send "2"
...endless spam...
```

### After Fix (Correct)
```
Cycle 1: Detect prompt → Mark handled → Execute → Fail → Send "2"
Cycle 2: Detect prompt → Already handled (cooldown) → Skip
Cycle 3: Detect prompt → Already handled (cooldown) → Skip
...no spam...
```

Even if execution fails, the prompt is in cooldown for 10 seconds, preventing repeated attempts.

## Why This Approach

**Alternative considered:** Only mark as handled on success, allow retries.

**Why rejected:** Creates retry spam when execution fails. With multiple terminals and 500ms polling, failures cascade into dozens of repeated keystrokes.

**Our approach:** Conservative - execute once per unique prompt text, then wait 10 seconds before allowing the same text again.

**Trade-off:** If a legitimate prompt fails (e.g., terminal unresponsive), we won't retry for 10 seconds. But this is better than spamming Claude's terminal with stray numbers.

## Testing Instructions

1. **Rebuild the app:**
   ```bash
   dotnet publish src/ClaudePermissionAssistant.App/ClaudePermissionAssistant.App.csproj \
     -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish/win-x64
   ```

2. **Run with multiple terminals:** Add 2-3 Claude sessions to monitor

3. **Trigger multiple prompts rapidly:** Run Claude Code commands that trigger permissions

4. **Verify no keystroke spam:** Check that numbers don't appear in Claude's output

5. **Check statistics:** Failed prompts should be minimal, no rapid increase

## Related Issues

- Multi-terminal focus contention (fixed earlier with global `_executionGate` lock)
- Cooldown duration too short (fixed: 5s → 10s)
- Verification timing too aggressive (fixed: 700ms → 1000ms)

## Status

🟢 **FIXED** - Ready for testing
