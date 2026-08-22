# Real-World Validation Guide

## Critical Limitation

⚠️ **This validation CANNOT be performed by Claude Code itself**

Since the application is designed to automate Claude Code permission prompts, and I am Claude Code, I cannot simultaneously:
- Run as the automation target
- Test the automation against myself
- Capture my own UI Automation properties

**This validation must be performed manually by the developer.**

---

## Validation Objectives

Verify that:
1. Claude Code permission prompts are accessible via UI Automation
2. Text can be extracted from terminal windows
3. The prompt pattern is correctly detected
4. The correct option number is identified
5. Keyboard input can be safely delivered
6. The automation works reliably across terminals

---

## STEP 1: Real Text Extraction

### Prerequisites
- Claude Code installed and functional
- Phase 1 Inspector built and ready
- Terminal environment (Windows Terminal, CMD, or PowerShell)

### Procedure

1. **Build the Inspector**
   ```bash
   dotnet build
   ```

2. **Start the Phase 1 Inspector**
   ```bash
   dotnet run --project src/ClaudePermissionAssistant.App/ClaudePermissionAssistant.App.csproj
   ```

3. **In a separate terminal, trigger a Claude Code permission prompt**
   
   Example ways to trigger prompts:
   - Ask Claude Code to read a file
   - Ask Claude Code to execute a bash command
   - Ask Claude Code to write to a file
   
   Wait for the permission prompt to appear:
   ```
   Do you want to proceed?
   
   > 1. Yes
     2. Yes, allow reading from /c/C: from this project
     3. No
   ```

4. **While the prompt is visible, use the Inspector**
   - Click "Refresh Windows"
   - Find your terminal in the list (look for process name: WindowsTerminal, cmd, powershell, etc.)
   - Select the terminal window
   - Click "Inspect Selected"

5. **Record Findings**

   **Document in this section:**

   #### Terminal Environment
   - [ ] Terminal: _________________ (Windows Terminal / CMD / PowerShell)
   - [ ] Process Name: _________________
   - [ ] Window Title: _________________
   - [ ] Process ID: _________________

   #### UI Automation Accessibility

   **Root Element**:
   - ControlType: _________________
   - Name: _________________
   - AutomationId: _________________
   - ClassName: _________________

   **Text-Bearing Element** (if found):
   - Path in tree: _________________
   - ControlType: _________________
   - Name: _________________
   - AutomationId: _________________

   **Supported Patterns**:
   - [ ] TextPattern
   - [ ] ValuePattern
   - [ ] Other: _________________

   #### Text Extraction

   **Can text be extracted?**
   - [ ] Yes, via TextPattern
   - [ ] Yes, via ValuePattern
   - [ ] Yes, via other method: _________________
   - [ ] No, text is not accessible

   **If text is accessible, record the EXACT format:**

   ```
   [Paste exact text here, including line breaks, spacing, special characters]
   
   
   
   
   ```

   **Text Format Observations**:
   - Contains "Do you want to proceed?": [ ] Yes / [ ] No
   - Contains "Yes, allow reading from": [ ] Yes / [ ] No
   - Contains "from this project": [ ] Yes / [ ] No
   - Numbered options visible: [ ] Yes / [ ] No
   - Line breaks preserved: [ ] Yes / [ ] No
   - Special characters (>, arrows): [ ] Yes / [ ] No
   - ANSI color codes present: [ ] Yes / [ ] No

6. **Export the Tree**
   - Click "Export to File"
   - Save as `terminal_[name]_prompt.txt`
   - Keep this file for reference

---

## STEP 2: Real Prompt Parsing

### Procedure

1. **Take the extracted text from Step 1**

2. **Create a test file**: `test_real_prompt.txt`

3. **Run through the parser**

   Create a quick test program or use existing unit tests:

   ```csharp
   var parser = new ClaudePromptParserSimple();
   var text = File.ReadAllText("test_real_prompt.txt");
   var request = parser.ParsePermissionRequest(text);
   
   if (request != null)
   {
       Console.WriteLine($"Tool: {request.ToolName}");
       Console.WriteLine($"Type: {request.PromptType}");
       Console.WriteLine($"Has allow-from-project: {request.HasAllowFromProjectOption}");
       Console.WriteLine($"Option number: {request.AllowFromProjectOptionNumber}");
       
       Console.WriteLine("\nAll options:");
       foreach (var opt in request.Options)
       {
           Console.WriteLine($"  {opt.Number}. {opt.Text}");
       }
   }
   else
   {
       Console.WriteLine("FAILED TO PARSE");
   }
   ```

### Record Findings

**Parser Results**:
- [ ] Text was recognized as Claude prompt
- [ ] "Do you want to proceed?" detected
- [ ] "Yes, allow reading from ... from this project" found
- [ ] Correct option number extracted: _____
- [ ] All options parsed correctly

**If parsing failed**:
- What pattern was missing? _________________
- What needs adjustment? _________________

---

## STEP 3: Foreground Safety Test

### Procedure

**⚠️ MANUAL TESTING - DO NOT RUN AUTOMATION YET**

1. **Get terminal HWND**
   - Use the Inspector to get the window handle (displayed as hex)
   - Record: Terminal HWND = 0x________________

2. **Test SetForegroundWindow manually**

   Create a small test program:
   ```csharp
   IntPtr hwnd = new IntPtr(0x...); // from inspector
   
   Console.WriteLine("Press Enter to bring terminal to foreground...");
   Console.ReadLine();
   
   bool result = SetForegroundWindow(hwnd);
   Console.WriteLine($"SetForegroundWindow returned: {result}");
   
   Thread.Sleep(100);
   
   IntPtr foreground = GetForegroundWindow();
   Console.WriteLine($"Foreground window: 0x{foreground:X}");
   Console.WriteLine($"Match: {foreground == hwnd}");
   ```

3. **Record Results**

   **SetForegroundWindow Test**:
   - [ ] Call returned true
   - [ ] Terminal came to foreground visually
   - [ ] GetForegroundWindow matched target HWND
   - [ ] Focus remained stable (didn't flicker away)

   **If failed**:
   - What happened? _________________
   - Did another window steal focus? _________________

---

## STEP 4: Execution Sequence Test

### Procedure

**⚠️ CAUTION: This will send actual keyboard input**

**Safety checklist before proceeding**:
- [ ] Terminal with Claude prompt is open
- [ ] Terminal is on a separate virtual desktop or behind other windows
- [ ] No sensitive applications are open
- [ ] You are ready to manually dismiss the prompt if automation fails
- [ ] You understand this will send "2\n" to the terminal

1. **Start with a safe test**

   **Instead of full automation, manually test the sequence:**
   - Bring terminal to foreground manually
   - Press "2"
   - Press Enter
   - Observe that Claude accepts option 2

2. **If manual test works, try controlled automation**

   Modify the executor to use test mode:
   - Set breakpoints after each step
   - Log every action
   - Add confirmation dialogs

3. **Record Results**

   **Keyboard Input Delivery**:
   - [ ] Terminal received "2" character
   - [ ] Terminal received Enter key
   - [ ] Prompt was dismissed
   - [ ] Claude Code accepted the selection
   - [ ] No input went to wrong window

   **Timing**:
   - Focus delay adequate: [ ] Yes / [ ] No (need: ____ ms)
   - Key press delay adequate: [ ] Yes / [ ] No (need: ____ ms)
   - Verification delay adequate: [ ] Yes / [ ] No (need: ____ ms)

---

## STEP 5: Post-Action Verification

### Procedure

1. **After sending input, re-inspect the terminal**

2. **Check if prompt disappeared**
   - Use Inspector to capture text again
   - Compare before/after text

3. **Record Results**

   **Verification**:
   - [ ] Prompt text no longer present
   - [ ] Claude Code continued execution
   - [ ] No errors displayed
   - [ ] Terminal returned to normal state

---

## STEP 6: Duplicate Protection Test

### Procedure

1. **Trigger a Claude Code prompt**

2. **Let automation detect and handle it**

3. **Immediately after, check if automation tries again**
   - Watch logs
   - Verify no second execution

4. **Record Results**

   **Duplicate Detection**:
   - [ ] Same prompt was not executed twice
   - [ ] Prompt was marked as handled
   - [ ] Log shows "Prompt already handled" on second detection

---

## STEP 7: Terminal Testing Matrix

Test across all supported terminals.

### Windows Terminal + PowerShell

| Test | Result | Notes |
|------|--------|-------|
| Text extraction | ⬜ PASS / ⬜ FAIL | |
| Prompt detection | ⬜ PASS / ⬜ FAIL | |
| Foreground verification | ⬜ PASS / ⬜ FAIL | |
| Keyboard execution | ⬜ PASS / ⬜ FAIL | |
| Post-action verification | ⬜ PASS / ⬜ FAIL | |
| Duplicate protection | ⬜ PASS / ⬜ FAIL | |

### Windows Terminal + CMD

| Test | Result | Notes |
|------|--------|-------|
| Text extraction | ⬜ PASS / ⬜ FAIL | |
| Prompt detection | ⬜ PASS / ⬜ FAIL | |
| Foreground verification | ⬜ PASS / ⬜ FAIL | |
| Keyboard execution | ⬜ PASS / ⬜ FAIL | |
| Post-action verification | ⬜ PASS / ⬜ FAIL | |
| Duplicate protection | ⬜ PASS / ⬜ FAIL | |

### Standalone CMD (conhost)

| Test | Result | Notes |
|------|--------|-------|
| Text extraction | ⬜ PASS / ⬜ FAIL | |
| Prompt detection | ⬜ PASS / ⬜ FAIL | |
| Foreground verification | ⬜ PASS / ⬜ FAIL | |
| Keyboard execution | ⬜ PASS / ⬜ FAIL | |
| Post-action verification | ⬜ PASS / ⬜ FAIL | |
| Duplicate protection | ⬜ PASS / ⬜ FAIL | |

### Standalone PowerShell (conhost)

| Test | Result | Notes |
|------|--------|-------|
| Text extraction | ⬜ PASS / ⬜ FAIL | |
| Prompt detection | ⬜ PASS / ⬜ FAIL | |
| Foreground verification | ⬜ PASS / ⬜ FAIL | |
| Keyboard execution | ⬜ PASS / ⬜ FAIL | |
| Post-action verification | ⬜ PASS / ⬜ FAIL | |
| Duplicate protection | ⬜ PASS / ⬜ FAIL | |

---

## STEP 8: Failure Behavior Testing

### Test Scenarios

#### Scenario 1: Text extraction fails
- **Setup**: Terminal where TextPattern/ValuePattern don't work
- **Expected**: Automation does nothing, logs failure
- **Actual**: ⬜ PASS / ⬜ FAIL
- **Notes**: _________________

#### Scenario 2: Prompt cannot be verified
- **Setup**: Trigger prompt, close terminal before automation runs
- **Expected**: "Prompt no longer present" error
- **Actual**: ⬜ PASS / ⬜ FAIL
- **Notes**: _________________

#### Scenario 3: Target HWND cannot be determined
- **Setup**: Invalid session data
- **Expected**: Automation aborts
- **Actual**: ⬜ PASS / ⬜ FAIL
- **Notes**: _________________

#### Scenario 4: Foreground verification fails
- **Setup**: Another window steals focus
- **Expected**: ABORT, no keyboard input sent
- **Actual**: ⬜ PASS / ⬜ FAIL
- **Notes**: _________________

#### Scenario 5: Option number cannot be determined
- **Setup**: Prompt without "allow from project" option
- **Expected**: "Allow from project option not found"
- **Actual**: ⬜ PASS / ⬜ FAIL
- **Notes**: _________________

#### Scenario 6: Prompt disappears before execution
- **Setup**: User manually dismisses prompt quickly
- **Expected**: Re-detection fails, automation aborts
- **Actual**: ⬜ PASS / ⬜ FAIL
- **Notes**: _________________

#### Scenario 7: Prompt remains after execution
- **Setup**: Input doesn't register
- **Expected**: Bounded retry (max 2), then failure
- **Actual**: ⬜ PASS / ⬜ FAIL
- **Notes**: _________________

#### Scenario 8: Terminal closes
- **Setup**: Close terminal during automation
- **Expected**: Graceful failure, no crash
- **Actual**: ⬜ PASS / ⬜ FAIL
- **Notes**: _________________

#### Scenario 9: Foreground focus changes during execution
- **Setup**: Switch window between SetForegroundWindow and SendInput
- **Expected**: Verification fails, input blocked OR input sent safely
- **Actual**: ⬜ PASS / ⬜ FAIL
- **Notes**: _________________

---

## Summary of Findings

### Text Extraction Method

**What works**:
- [ ] TextPattern on all terminals
- [ ] ValuePattern on all terminals
- [ ] TextPattern on some terminals: _________________
- [ ] ValuePattern on some terminals: _________________
- [ ] Other method: _________________

**What doesn't work**:
- _________________

### Prompt Detection

**Success rate across terminals**:
- Windows Terminal: ____ %
- CMD: ____ %
- PowerShell: ____ %

**Common issues**:
- _________________

### Keyboard Execution

**Reliability**:
- Foreground verification: ____ % success
- Key delivery: ____ % success
- Prompt dismissal: ____ % success

**Timing requirements**:
- Focus delay: ____ ms
- Key press delay: ____ ms
- Verification delay: ____ ms

### Overall Assessment

**Supported Terminals**:
- [ ] Windows Terminal + PowerShell
- [ ] Windows Terminal + CMD
- [ ] Standalone CMD
- [ ] Standalone PowerShell
- [ ] Other: _________________

**Unsupported Terminals**:
- [ ] Git Bash
- [ ] VSCode Integrated Terminal
- [ ] Other: _________________

**Known Limitations**:
1. _________________
2. _________________
3. _________________

**Blocking Issues** (must fix before production):
1. _________________
2. _________________

**Non-Blocking Issues** (can work around):
1. _________________
2. _________________

---

## Ready for System Tray UI?

**Checklist**:

- [ ] Text can be extracted from at least one terminal type
- [ ] Prompt pattern is correctly detected
- [ ] Correct option number is identified
- [ ] Foreground verification works
- [ ] Keyboard input is delivered safely
- [ ] Prompt disappears after execution
- [ ] Duplicate execution is prevented
- [ ] At least one terminal environment fully supported
- [ ] Failure modes behave safely
- [ ] No crashes or hangs observed

**Decision**:
- [ ] ✅ Ready to proceed to UI implementation
- [ ] ⚠️ Needs fixes first (list below)
- [ ] ❌ Fundamental issues prevent automation

**Required fixes before UI**:
1. _________________
2. _________________
3. _________________

---

## Appendix: Example Captured Text

### Windows Terminal + PowerShell

```
[Paste captured text from real Claude prompt here]




```

### CMD

```
[Paste captured text from real Claude prompt here]




```

### PowerShell

```
[Paste captured text from real Claude prompt here]




```

---

**Validation Date**: _________________  
**Performed By**: _________________  
**Claude Code Version**: _________________  
**Windows Version**: _________________
