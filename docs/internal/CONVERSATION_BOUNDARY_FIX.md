# Conversation Boundary Detection Fix

## Problem Statement

When Claude Prompter monitors a terminal, permissions from conversation p1 work fine, but permissions from a new conversation p2 in the same terminal **fail to detect or are incorrectly rejected as duplicates**.

## Root Cause Analysis

### Issue #1: Aggressive Deduplication Across Conversations

**Current Behavior:**
```csharp
private string GetPromptKey(DetectedPrompt prompt)
{
    var textToHash = prompt.Request.PromptRegion ?? prompt.RawText;
    var textHash = SHA256.ComputeHash(textToHash).Substring(0, 16);
    return $"{TerminalProcessId}_{ClaudeProcessId}_{textHash}";
}
```

**The Problem:**
- Key includes PIDs (which don't change across conversations)
- Key includes text hash (common permissions repeat: "Do you want to run bash?")
- **Cooldown:** 5 seconds

**Scenario:**
1. Conversation p1: User asks Claude to run bash script
2. Permission prompt: "Do you want to run bash?" → Approved
3. Key stored: `12345_67890_abc123def456` with timestamp
4. Conversation p1 completes
5. **User waits 3 seconds**
6. Conversation p2: User asks Claude to run another bash script
7. Permission prompt: "Do you want to run bash?" (SAME TEXT!)
8. Key generated: `12345_67890_abc123def456` (IDENTICAL!)
9. Check: `DateTime.UtcNow - handledAt = 3 seconds < 5 seconds`
10. **Result: REJECTED AS DUPLICATE** ❌

### Issue #2: UI Automation Cache Staleness

**Current Behavior:**
- AutomationElement cached for 30 seconds
- Cache refreshes only on:
  - Age > 30 seconds
  - 3+ consecutive failures
  - Manual clear

**The Problem:**
- Between conversations, terminal content changes completely
- Cache might serve old content from conversation p1
- Detector sees old terminal state → no new prompt found

### Issue #3: No Conversation Boundary Detection

The app has no concept of:
- When a conversation ends
- When a new conversation starts
- That prompts in different conversations are independent

## Solution

### Fix #1: Reduce Cooldown to 1 Second

**Rationale:**
- 5 seconds prevents legitimate approvals in new conversations
- 1 second is enough to prevent rapid re-detection of the same prompt
- Most conversation gaps are > 1 second

**Change:**
```csharp
// Before:
private static readonly TimeSpan DuplicateCooldown = TimeSpan.FromSeconds(5);

// After:
private static readonly TimeSpan DuplicateCooldown = TimeSpan.FromSeconds(1);
```

### Fix #2: Add Terminal Content Change Detection

Detect when terminal content has changed significantly (indicates new conversation or context).

**Implementation:**
```csharp
private string? _lastTerminalTextHash;
private DateTime _lastTerminalTextChange = DateTime.MinValue;

private bool HasTerminalContentChangedSignificantly(string currentText)
{
    using var sha256 = SHA256.Create();
    var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(currentText));
    var currentHash = Convert.ToHexString(hashBytes).Substring(0, 16).ToLowerInvariant();
    
    if (_lastTerminalTextHash == null || _lastTerminalTextHash != currentHash)
    {
        var changed = _lastTerminalTextHash != null; // First time = no change
        _lastTerminalTextHash = currentHash;
        _lastTerminalTextChange = DateTime.UtcNow;
        return changed;
    }
    
    return false;
}
```

**When significant change detected:**
1. Clear `_handledPrompts` cache
2. Force UI Automation cache refresh
3. Log the event

### Fix #3: Add Sequence Number to Deduplication Key

Add a "context sequence number" that increments when terminal content changes significantly.

**Implementation:**
```csharp
private int _terminalContextSequence = 0;

private string GetPromptKey(DetectedPrompt prompt)
{
    var textToHash = prompt.Request.PromptRegion ?? prompt.RawText;
    using var sha256 = SHA256.Create();
    var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(textToHash));
    var textHash = Convert.ToHexString(hashBytes).Substring(0, 16).ToLowerInvariant();
    
    // Include context sequence to differentiate between conversations
    return $"{prompt.Session.TerminalProcessId}_{prompt.Session.ClaudeProcessId}_{_terminalContextSequence}_{textHash}";
}
```

**When to increment:**
- When terminal content hash changes significantly
- When terminal text grows by > 50% (indicates new output)
- When terminal text shrinks (indicates screen clear)

### Fix #4: Force Cache Refresh on Conversation Boundary

When a conversation boundary is detected:
1. Clear UI Automation cache for the window
2. Force fresh AutomationElement acquisition
3. Clear handled prompts cache

## Implementation Plan

### Phase 1: Quick Fix (Reduce Cooldown)
- Change cooldown from 5s to 1s
- Test with rapid successive prompts
- Verify no duplicate approvals

### Phase 2: Content Change Detection
- Add terminal text hashing
- Detect significant content changes
- Clear caches on change
- Add logging

### Phase 3: Context Sequence Numbers
- Add sequence number to keys
- Increment on content change
- Update tests

### Phase 4: Enhanced Diagnostics
- Log conversation boundaries
- Log cache clears
- Log deduplication decisions with reasons

## Testing Strategy

### Test Case 1: Same Permission, Different Conversations
1. Start monitoring
2. Conversation p1: Request bash permission → Approve
3. Wait 2 seconds (less than old 5s cooldown)
4. Conversation p2: Request bash permission again
5. **Expected:** Approved (not rejected as duplicate)

### Test Case 2: Multiple Permissions in Single Conversation
1. Conversation p1: Request bash permission → Approve
2. Immediately (< 1s): Same terminal shows same prompt (re-detection)
3. **Expected:** Rejected as duplicate (within 1s cooldown)

### Test Case 3: Rapid Conversation Switching
1. Conversation p1: Request permission A → Approve
2. Immediately start p2: Request permission B → Approve
3. Immediately start p3: Request permission A again → Approve
4. **Expected:** All approved (different contexts)

### Test Case 4: Terminal Content Change Detection
1. Run command that clears screen
2. Start new conversation
3. Request permission
4. **Expected:** Approved (content change detected)

## Rollback Plan

If issues occur:
1. Revert cooldown to 5 seconds
2. Remove context sequence logic
3. Keep diagnostic logging for future analysis

## Metrics to Monitor

- **False Positives**: Prompts incorrectly rejected as duplicates
- **False Negatives**: Prompts incorrectly approved multiple times
- **Detection Rate**: % of prompts successfully detected
- **Approval Rate**: % of detected prompts successfully approved

## Related Issues

- Foreground window mismatch (fixed in previous commit)
- UI Automation cache staleness
- 24/7 stability (memory cleanup)

## Author

Claude Opus 4.6 (1M context)

## Date

2026-09-03
