# Live Prompt Test - Manual Validation Guide

**Purpose**: Developer tool for manually validating real Claude Code permission prompt detection.

**Status**: Observation only - NO automation execution.

---

## What This Tool Does

The Live Prompt Test window allows you to:

1. **Select** a terminal window running Claude Code
2. **Extract** text from the terminal via UI Automation TextPattern
3. **Parse** the extracted text using `ClaudePromptParserSimple`
4. **View** detailed results showing whether the prompt was detected
5. **Export** captured prompts for documentation/regression testing

**This tool does NOT**:
- Send keyboard input
- Press any keys
- Execute automation
- Modify the terminal
- Auto-approve anything

---

## How to Launch

### Method 1: From Phase 1 Inspector

1. Run the Inspector:
   ```bash
   dotnet run --project src/ClaudePermissionAssistant.App/ClaudePermissionAssistant.App.csproj
   ```

2. Click menu: **Developer → Live Prompt Test...**

### Method 2: Direct Launch

The `LivePromptTestWindow` is accessible programmatically if needed for testing.

---

## Usage Instructions

### Step 1: Prepare Claude Code

1. Open a **conhost-based terminal**:
   - CMD (Command Prompt)
   - PowerShell (standalone, not ISE)

2. Start Claude Code in that terminal

3. **Trigger a permission prompt** by asking Claude to:
   - Read a file
   - Execute a bash command
   - Write to a file

4. **Leave the prompt visible** - do NOT respond to it yet

---

### Step 2: Select Terminal in Live Test

1. In Live Prompt Test window, click **Refresh Windows**

2. Find your terminal in the list:
   - Look for `conhost (PID: ...)` or
   - Process name matching your terminal
   - Window title showing your session

3. **Select the terminal window**

4. Verify the selection info shows correct:
   - Process name
   - PID
   - HWND (window handle in hex)

---

### Step 3: Extract Terminal Text

1. Click **Step 2: Read Terminal Text**

2. Watch the **Raw TextPattern Output** panel

**What to Look For**:

✅ **Success**:
```
[EXTRACTED 1234 CHARACTERS]

Do you want to proceed?

> 1. Yes
  2. Yes, allow reading from /c/C: from this project
  3. No

Enter your choice:
```

❌ **Failure**:
```
[EXTRACTION FAILED]

No Document/Text Area element found in window
```

**Common Issues**:
- **No text**: Terminal may not expose TextPattern
- **Different element**: May need alternate detection strategy
- **Empty text**: Terminal may be in alternate buffer or empty state

---

### Step 4: Parse Claude Prompt

1. Click **Step 3: Parse Claude Prompt**

2. Watch the **Parser Results** panel

**Success Example**:
```
═══════════════════════════════════════
✅ PASS: Claude Prompt Detected!
═══════════════════════════════════════

Tool Name: Read
Prompt Type: AllowReading
Description: Allow reading from directory

Has Allow From Project: True
Allow From Project Option Number: 2

Total Options Detected: 3

All Detected Options:
   1. Yes
     Action: Allow
👉 2. Yes, allow reading from /c/C: from this project
     Action: AlwaysAllow
   3. No
     Action: Deny

═══════════════════════════════════════
✅ Ready for automation: Would send '2' + Enter
═══════════════════════════════════════
```

**Failure Example**:
```
═══════════════════════════════════════
❌ FAIL: Claude Prompt NOT Detected
═══════════════════════════════════════

The parser did not recognize this as a Claude Code permission prompt.

Diagnostic:
  ContainsPromptMarkers: False
  IsValidPromptFormat: False

Missing markers check:
  'Do you want to proceed?': ✗ MISSING
  'allow reading from': ✗ MISSING
  'from this project': ✗ MISSING
```

---

### Step 5: Export Captured Prompt

1. Click **Export Captured Prompt**

2. Save the file (default name: `claude_prompt_capture_YYYYMMDD_HHMMSS.txt`)

**Export Contents**:
- Capture metadata (timestamp, process, PID, HWND)
- Full raw TextPattern output (no truncation)
- Parser diagnostics
- Detected options
- Success/failure status

---

## Validation Checklist

Use this checklist for manual validation:

### Terminal Compatibility

- [ ] **CMD**: Extract text, parse prompt, detect option 2
- [ ] **PowerShell**: Extract text, parse prompt, detect option 2
- [ ] **Windows Terminal + CMD**: Test separately (may differ from standalone)
- [ ] **Windows Terminal + PowerShell**: Test separately

### Prompt Variations

Test with different paths:

- [ ] `/c/C:` (root drive)
- [ ] `/c/Users/USER/project` (user directory)
- [ ] `/c/Users/USER/Documents/my-project` (spaces or special characters if applicable)

Verify for each:
- [ ] Prompt detected: ✅ PASS
- [ ] Allow From Project Option Number: `2` (or actual number)
- [ ] Correct option text identified

### Edge Cases

- [ ] **Multiple windows**: Select correct terminal from list
- [ ] **Prompt disappeared**: Extract when no prompt visible (should get normal terminal text)
- [ ] **Long scrollback**: Extract works with large terminal buffers
- [ ] **Different prompt types**: Test other Claude permission types if possible

---

## Expected Results

### Successful Detection

**Criteria**:
- Parser returns non-null `PermissionRequest`
- `HasAllowFromProjectOption` = `true`
- `AllowFromProjectOptionNumber` = `2` (or appropriate number)
- All options parsed correctly
- Status shows ✅ PASS

### What This Proves

✅ **Text extraction works** via TextPattern  
✅ **Parser correctly identifies** Claude Code prompts  
✅ **Dynamic paths handled** (not hardcoded)  
✅ **Option numbering detected** correctly  
✅ **Automation would target correct option**  

---

## Troubleshooting

### "No windows found"

- Click **Refresh Windows** again
- Ensure terminal is actually open
- Check you have permissions to enumerate windows

### "No Document/Text Area element found"

- Terminal type may not be conhost
- Try different terminal (CMD or PowerShell)
- Window may not expose TextPattern

### "TextPattern not supported"

- This terminal doesn't expose text via UI Automation
- Try conhost-based terminal (CMD/PowerShell standalone)
- Windows Terminal support needs separate validation

### "Claude prompt not detected"

Check diagnostics:
- Which markers are missing?
- Is the prompt format different than expected?
- Are ANSI codes interfering? (check raw text)

### Parser sees text but fails to match

**Possible causes**:
1. **ANSI escape sequences** in terminal output
2. **Different prompt format** than expected
3. **Whitespace differences** (tabs vs spaces)
4. **Line ending differences** (CRLF vs LF)

**Action**: Export the captured prompt and examine the raw text.

---

## After Successful Validation

Once you confirm:
- ✅ Text extraction works on target terminal
- ✅ Parser detects real Claude prompts
- ✅ Correct option number identified
- ✅ Dynamic paths handled

You have validated:
- **Detection pipeline works end-to-end**
- **Ready for executor safety testing** (next phase)

**DO NOT** proceed to automatic execution until:
- Executor foreground verification tested
- Keyboard input delivery tested safely
- Post-action verification confirmed

---

## Captured Prompt Usage

### For Documentation

Add successful captures to documentation:
- `docs/REAL_WORLD_VALIDATION.md`
- Include exact prompt format
- Document terminal type
- Record detection results

### For Regression Tests

Create fixture files:
- `tests/fixtures/claude-prompt-conhost-1.txt`
- `tests/fixtures/claude-prompt-conhost-2.txt`
- `tests/fixtures/claude-prompt-different-path.txt`

Add test cases that parse these fixtures and verify detection.

### For Parser Improvements

If prompts fail to parse:
- Compare raw text to expected format
- Identify formatting differences
- Implement normalization if needed (ANSI stripping, etc.)
- Add regression tests with real captures

---

## Safety Reminders

⚠️ **This tool is observation only**

- Does NOT send keyboard input
- Does NOT press 2 or Enter
- Does NOT execute automation
- Does NOT approve anything

The purpose is to **validate that detection works** before implementing automated execution.

Manual validation confirms:
1. Real prompts can be extracted
2. Parser recognizes them correctly
3. Correct option is identified
4. Automation WOULD work (if enabled)

---

## Next Phase

After successful validation with this tool, the next phase is:

**Executor Safety Testing**:
1. Test `SetForegroundWindow` behavior
2. Test keyboard input delivery (controlled environment)
3. Verify prompt dismissal
4. Test post-action verification
5. Validate full end-to-end flow

**NOT** implementing System Tray UI yet.

Focus remains on **validating the automation pipeline works correctly**.

---

## Technical Details

### Text Extraction Method

```csharp
AutomationElement.FromHandle(windowHandle)
  → Find Document element (AutomationId: "Text Area")
  → GetCurrentPattern(TextPattern.Pattern)
  → textPattern.DocumentRange
  → documentRange.GetText(-1)  // -1 = all text
```

### Parser Used

`ClaudePromptParserSimple` - detects:
- "Do you want to proceed?"
- "Yes, allow reading from"
- "from this project"
- Numbered options (1., 2., 3.)

### Export Format

```
═══════════════════════════════════════
Claude Permission Prompt Capture
═══════════════════════════════════════
Captured: [timestamp]
Process: [name]
PID: [number]
HWND: [hex]
Text Length: [chars]

═══════════════════════════════════════
RAW TEXTPATTERN OUTPUT
═══════════════════════════════════════

[exact unmodified text]

═══════════════════════════════════════
PARSER DIAGNOSTICS
═══════════════════════════════════════

Parser Result: SUCCESS/FAILED
[detailed parser output]
```

---

**Document Version**: 1.0  
**Last Updated**: 2026-08-22  
**Status**: Ready for manual validation
