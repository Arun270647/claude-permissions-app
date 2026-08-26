# Testing Guide: Long-Running Session Fix

## Problem Fixed
After 30+ minutes of running, Claude Prompter would stop detecting prompts. This was caused by stale UI Automation elements and lack of automatic recovery.

## What Was Changed

### 1. ClaudePromptDetector.cs - Smart Caching
- **Added cache management**: AutomationElement references are now cached with timestamps
- **Auto-refresh**: Cache entries older than 30 seconds are automatically refreshed
- **Failure tracking**: Tracks consecutive failures per window and forces refresh after 3 failures
- **Better error logging**: All exceptions now logged with type and message for debugging

### 2. BackgroundMonitorService.cs - Recovery Mechanism
- **Failure monitoring**: Tracks consecutive text extraction failures
- **Automatic recovery**: Triggers after 10 consecutive failures
- **Recovery actions**:
  - Clears UI Automation cache to force fresh element acquisition
  - Clears handled prompts to allow re-detection
  - Comprehensive logging of recovery attempts
- **Periodic maintenance**: Cleans up stale cache entries every 5 minutes

## How to Test

### Test 1: Immediate Functionality (5 minutes)
**Purpose:** Verify the fix doesn't break existing functionality

1. **Build the application:**
   ```bash
   dotnet build src/Windows/ClaudePermissionAssistant.App/ClaudePermissionAssistant.App.csproj --configuration Release
   ```

2. **Run the built executable:**
   ```bash
   ./publish/win-x64/ClaudePrompter.exe
   ```
   (Or use the full rebuild script: `rebuild.bat`)

3. **Add a terminal to monitor:**
   - Open CMD or PowerShell
   - Click "Add Terminal" in Claude Prompter
   - Select your terminal window

4. **Start monitoring:**
   - Click "Start Monitoring"

5. **Trigger Claude prompts:**
   - In the terminal, run Claude Code commands that trigger permissions
   - Example: `claude "read a file"` (if not already allowed)

6. **Verify immediate detection:**
   - ✅ Prompts should be detected within 500ms
   - ✅ Statistics should increment (Prompts Detected, Prompts Approved)
   - ✅ No errors in the UI

### Test 2: Short-Term Stability (15 minutes)
**Purpose:** Verify recovery mechanism works correctly

1. **Keep the app running** with a terminal being monitored

2. **Trigger prompts periodically:**
   - Every 2-3 minutes, trigger a Claude prompt
   - Mix different prompt types (Bash, Read, Edit, etc.)

3. **Monitor the log file:**
   ```bash
   tail -f "C:\Users\USER\AppData\Local\ClaudePermissionAssistant\Logs\[latest-log-file].log"
   ```

4. **Look for these log entries:**
   - `MONITOR_HEARTBEAT` - Should appear every second
   - `MONITOR_TEXT_EXTRACTION` - Should show successful extraction
   - `PROMPT_DETECTED` - Should appear when prompts are shown
   - `APPROVAL_SUCCESS` - Should appear after approval

5. **Expected behavior:**
   - ✅ No recovery triggers (no `MONITOR_RECOVERY` messages)
   - ✅ All prompts detected and approved
   - ✅ Text extraction consistently succeeds

### Test 3: Long-Running Session (30-60 minutes) ⭐ PRIMARY TEST
**Purpose:** Verify the fix solves the original 30+ minute issue

1. **Set up long-running test:**
   ```bash
   # Build the app
   rebuild.bat
   
   # Run the app
   ./publish/win-x64/ClaudePrompter.exe
   ```

2. **Add and start monitoring** a terminal with Claude Code running

3. **Create a test script** to periodically trigger prompts:
   
   Save this as `test-long-session.bat`:
   ```batch
   @echo off
   :loop
   echo [%TIME%] Triggering test prompt...
   timeout /t 300 /nobreak >nul
   echo Test iteration at %TIME%
   goto loop
   ```

4. **In the monitored terminal**, manually trigger Claude prompts:
   - Every 5-10 minutes, run a Claude command that needs permissions
   - Example: `claude "what files are in this directory?"`
   - Or use any command that triggers a prompt

5. **Monitor for 30-60 minutes:**
   - Continue working normally
   - Let the app run in the background
   - Periodically check the statistics

6. **At 30, 45, and 60 minute marks:**
   - Trigger a Claude prompt
   - **Verify detection still works:**
     - ✅ Prompt appears in Claude terminal
     - ✅ Prompter detects it (statistics increment)
     - ✅ Approval happens automatically
     - ✅ Claude continues without manual intervention

7. **Check the log file** for recovery events:
   ```bash
   # Search for recovery events
   grep "MONITOR_RECOVERY" "C:\Users\USER\AppData\Local\ClaudePermissionAssistant\Logs\*.log"
   ```
   
   **If recovery triggered:**
   - Should see `MONITOR_RECOVERY_START`
   - Followed by `Cache cleared successfully`
   - Followed by `MONITOR_RECOVERY_COMPLETE`
   - Detection should resume immediately after recovery

### Test 4: Cache Cleanup (Check at 5, 10, 15 minutes)
**Purpose:** Verify periodic maintenance works

1. **Look for cache cleanup logs:**
   ```bash
   grep "MONITOR_CACHE_CLEANUP" "C:\Users\USER\AppData\Local\ClaudePermissionAssistant\Logs\*.log"
   ```

2. **Expected behavior:**
   - Should see cleanup message every 5 minutes
   - Format: `MONITOR_CACHE_CLEANUP: Periodic cleanup completed`

### Test 5: Stress Test (Optional, 2+ hours)
**Purpose:** Verify extreme long-running stability

1. **Run the app overnight or for several hours**

2. **Periodically trigger prompts:**
   - Set up a script to trigger prompts every 10-15 minutes
   - Or manually trigger when you remember

3. **Check after 2+ hours:**
   - ✅ App still running
   - ✅ Prompts still being detected
   - ✅ Statistics show continued activity
   - ✅ No crashes or freezes

## Expected Log Patterns

### Normal Operation (No Issues)
```
MONITOR_HEARTBEAT: Cycle=1234, HWND=0xABC123, ...
MONITOR_TEXT_EXTRACTION: Length=2450, HasText=true
PROMPT_DETECTED
APPROVAL_SUCCESS
```

### Recovery Triggered (After Failures)
```
MONITOR_TEXT_EXTRACTION: FAILED - Length=0, ConsecutiveFailures=10
═══════════════════════════════════════
MONITOR_RECOVERY_START
  Reason: 10 consecutive text extraction failures
  Action: Clearing UI Automation cache
  Cache cleared successfully
  Handled prompts cleared
MONITOR_RECOVERY_COMPLETE
═══════════════════════════════════════
MONITOR_TEXT_EXTRACTION: RECOVERED - Previous failures: 10
PROMPT_DETECTED
APPROVAL_SUCCESS
```

### Periodic Maintenance
```
MONITOR_CACHE_CLEANUP: Periodic cleanup completed
```

## Success Criteria

✅ **Test 1 (5 min):** Immediate detection works perfectly
✅ **Test 2 (15 min):** No recovery needed, all prompts detected
✅ **Test 3 (30-60 min):** Prompts still detected after 30+ minutes ⭐ **CRITICAL**
✅ **Test 4:** Cache cleanup occurs every 5 minutes
✅ **Test 5 (Optional):** Multi-hour stability

## Troubleshooting

### If prompts stop being detected:

1. **Check the log file:**
   ```bash
   tail -n 100 "C:\Users\USER\AppData\Local\ClaudePermissionAssistant\Logs\[latest-log-file].log"
   ```

2. **Look for:**
   - Consecutive `MONITOR_TEXT_EXTRACTION: FAILED` messages
   - `MONITOR_RECOVERY_START` messages
   - Any exception messages

3. **If recovery is triggered:**
   - This is **expected behavior** - the fix is working!
   - Verify that detection resumes after recovery
   - Check for `MONITOR_TEXT_EXTRACTION: RECOVERED` message

4. **If recovery keeps triggering repeatedly:**
   - This indicates UI Automation is fundamentally failing
   - Check if the terminal window is still open
   - Try stopping and restarting monitoring
   - Report the issue with log excerpts

### If the app crashes:

1. **Collect crash information:**
   - Check Windows Event Viewer (Application logs)
   - Check the last log file entries
   - Note what action triggered the crash

2. **Report with:**
   - How long the app was running
   - What you were doing when it crashed
   - Relevant log excerpts
   - Steps to reproduce

## Performance Impact

- **Memory:** Minimal increase (~few KB for cache dictionary)
- **CPU:** No noticeable impact (cleanup happens every 5 minutes)
- **Disk I/O:** Slightly more logging for diagnostics

## Reverting (If Needed)

If this fix causes issues:

```bash
git checkout HEAD~1
dotnet build --configuration Release
```

Then report the issue with detailed logs.

## Technical Notes

### Why 30 seconds for cache age?
- Balance between freshness and performance
- UI Automation element acquisition is expensive (~50-100ms)
- 30 seconds ensures elements are recent enough

### Why 10 failures for recovery trigger?
- Allows for transient failures (terminal minimized, etc.)
- 10 failures × 500ms = 5 seconds of consecutive failures
- Aggressive enough to recover quickly
- Conservative enough to avoid false triggers

### Why 5 minutes for cache cleanup?
- Prevents memory bloat over very long sessions
- Infrequent enough to not impact performance
- Matches the _handledPrompts cleanup interval in executor

## Next Steps

After successful testing:

1. ✅ Verify all tests pass (especially Test 3)
2. ✅ Confirm no new issues introduced
3. ✅ Update CHANGELOG.md with fix details
4. ✅ Consider merge to main (after user approval)

## Questions?

If you encounter any issues during testing:
1. Collect the log file
2. Note the exact test step where issue occurred
3. Document expected vs actual behavior
4. Share the information for analysis

---

**Created:** 2026-08-25  
**Fix Version:** Will be v1.0.2  
**Related Issue:** Long-running session prompt detection failure
