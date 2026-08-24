# Two-Tier Cooldown Fix - Core Logic Restored

## Problem

After the keystroke spam fix, the app stopped approving prompts:
- **33 prompts detected, 0 approved, 2 failed**
- Prompts were being marked as handled immediately, then blocked for 10 seconds
- Even transient failures (timing issues, focus problems) prevented approval

## Root Cause

The previous fix was too aggressive:

```csharp
// PREVIOUS FIX (Too Aggressive):
MarkPromptAsHandled(prompt);  // Mark BEFORE execution
Execute();                     // If fails, prompt is blocked for 10 seconds
```

**Result:** Legitimate prompts that failed due to transient issues (terminal not ready, focus delay) were blocked for 10 seconds and never retried.

## The Solution: Two-Tier Cooldown System

Separate tracking for **attempts** (short cooldown) vs **successes** (long cooldown):

### 1. Attempted Prompts (2-second cooldown)
- Tracked in `_attemptedPrompts` dictionary
- Prevents retry spam during transient failures
- Short cooldown allows legitimate retry after terminal settles

### 2. Handled Prompts (10-second cooldown)  
- Tracked in `_handledPrompts` dictionary
- Only set on **successful** execution
- Long cooldown prevents re-handling the same prompt

## How It Works

```csharp
// Execution flow:
1. Check if already successfully handled (10s cooldown) → Skip
2. Check if recently attempted and failed (2s cooldown) → Skip  
3. Mark as attempted → Prevents spam during this execution
4. Execute with retries (global lock, focus verification, etc.)
5. If SUCCESS → Mark as handled (10s cooldown)
6. If FAILURE → Only attempted mark remains (2s cooldown)
```

## Code Changes

### New Fields
```csharp
private readonly Dictionary<string, DateTime> _attemptedPrompts = new();
private static readonly TimeSpan SuccessCooldown = TimeSpan.FromSeconds(10);
private static readonly TimeSpan FailureCooldown = TimeSpan.FromSeconds(2);
```

### New Methods
```csharp
private bool IsPromptRecentlyAttempted(string key)
private void MarkPromptAsAttempted(string key)
```

### Updated Execute Flow
```csharp
// Check success cooldown (10s)
if (IsPromptAlreadyHandled(prompt))
    return "Already handled";

// Check failure cooldown (2s)  
if (IsPromptRecentlyAttempted(key))
    return "Recently attempted, waiting";

// Mark as attempted (2s cooldown starts)
MarkPromptAsAttempted(key);

// Execute
if (success)
{
    MarkPromptAsHandled(prompt); // 10s cooldown starts
    return success;
}
else
{
    // Only 2s attempted cooldown remains
    return failure;
}
```

## Benefits

✅ **Prevents keystroke spam** - 2-second cooldown stops rapid retries  
✅ **Allows legitimate retries** - Failed prompts can retry after 2 seconds  
✅ **Prevents duplicate handling** - Successful prompts blocked for 10 seconds  
✅ **Handles transient issues** - Timing/focus problems don't permanently block prompts

## Example Scenarios

### Scenario 1: Transient Failure (Terminal Slow)
```
Cycle 1 (0s):   Detect → Attempt → Execute → Fail (terminal not ready)
Cycle 2 (0.5s): Detect → Recently attempted → Skip
Cycle 3 (1.0s): Detect → Recently attempted → Skip
Cycle 4 (1.5s): Detect → Recently attempted → Skip
Cycle 5 (2.0s): Detect → Attempt → Execute → SUCCESS
                → Mark as handled
Cycle 6 (2.5s): Detect → Already handled → Skip (10s cooldown)
```

### Scenario 2: Persistent Failure
```
Cycle 1 (0s):   Detect → Attempt → Execute → Fail (real issue)
Cycle 2 (0.5s): Detect → Recently attempted → Skip
Cycle 3 (1.0s): Detect → Recently attempted → Skip
Cycle 4 (1.5s): Detect → Recently attempted → Skip
Cycle 5 (2.0s): Detect → Attempt → Execute → Fail again
Cycle 6 (2.5s): Detect → Recently attempted → Skip
...continues with 2s retry intervals
```

No keystroke spam because of the 2-second gap between attempts.

### Scenario 3: Successful Approval
```
Cycle 1 (0s):   Detect → Attempt → Execute → SUCCESS
                → Mark as handled (10s cooldown)
Cycle 2 (0.5s): Detect → Already handled → Skip
Cycle 3 (1.0s): Detect → Already handled → Skip
...
Cycle 20 (10s): Detect → Cooldown expired → Can attempt again
```

## Comparison

| Issue | Original | Previous Fix | Two-Tier Fix |
|-------|----------|--------------|--------------|
| Keystroke spam | ❌ Yes (retry every 500ms) | ✅ No (10s block) | ✅ No (2s gap) |
| Handles transient failures | ✅ Yes (keeps retrying) | ❌ No (blocked 10s) | ✅ Yes (retry after 2s) |
| Prevents duplicate handling | ❌ No (only on success) | ✅ Yes (10s cooldown) | ✅ Yes (10s on success) |
| Approval success rate | ✅ High | ❌ Low (blocks too early) | ✅ High |

## Files Modified

- `src/ClaudePermissionAssistant.Automation/Services/ClaudePermissionPromptExecutorHardened.cs`
  - Added `_attemptedPrompts` tracking
  - Added `SuccessCooldown` (10s) and `FailureCooldown` (2s)
  - Added `IsPromptRecentlyAttempted()` and `MarkPromptAsAttempted()`
  - Updated `Execute()` flow to use two-tier system

## Testing

✅ All 91 tests passing  
✅ Builds successfully  
✅ Ready for deployment

## How to Test

1. **Rebuild the app:**
   ```bash
   cd "D:\projects\claude-permission app"
   rebuild.bat
   ```

2. **Run the app:** Double-click desktop shortcut

3. **Add terminals** to monitor

4. **Trigger rapid prompts:** Run multiple Claude commands quickly

5. **Verify:**
   - ✅ High approval rate (most prompts approved)
   - ✅ Low failure rate (only genuine failures)
   - ✅ No keystroke spam in Claude output
   - ✅ Failed prompts retry after 2 seconds

## Configuration

Current settings in `BackgroundMonitorService`:
```csharp
FocusDelayMs = 250ms
KeyPressDelayMs = 150ms
VerificationDelayMs = 1000ms
MaxRetryAttempts = 1 (2 total attempts)
```

Combined with 2-second failure cooldown, this gives prompts multiple chances to succeed while preventing spam.

## Status

🟢 **FIXED** - Core logic restored with spam prevention
