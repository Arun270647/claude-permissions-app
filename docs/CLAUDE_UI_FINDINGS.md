# Claude Code UI Automation Findings

**Document Status**: Research & Analysis Phase  
**Last Updated**: 2026-08-22  
**Phase**: 2 - Detection Architecture

## Executive Summary

Claude Code permission prompts are **terminal text content**, not semantic UI controls. They appear as formatted text in the terminal buffer, and users respond via keyboard input rather than clicking buttons. This fundamentally shapes detection strategy.

## Critical Discovery

**Permission prompts are NOT exposed as clickable UI controls.**

Evidence:
- Claude Code is a CLI tool that runs inside terminal emulators
- Permission prompts are rendered as text output to the terminal
- User interaction is via keyboard (number selection: 1=Allow, 2=Deny, etc.)
- No WPF/WinForms UI elements are created for the prompt itself

**Implication**: Detection must rely on terminal text content analysis via UI Automation TextPattern or ValuePattern, not semantic button controls.

## Terminal Environment Analysis

### Process Architecture

#### Windows Terminal
- **Process Name**: `WindowsTerminal.exe`
- **Architecture**: Host process spawns child shell processes
- **Process Tree Example**:
  ```
  WindowsTerminal.exe (PID: xxxxx)
    └─ powershell.exe / cmd.exe / bash.exe (PID: yyyyy)
        └─ claude.exe (PID: zzzzz)
  ```
- **UI Automation Root**: WindowsTerminal.exe window
- **Expected ControlTypes**: Window → Pane → Edit/Document controls

#### CMD (Console Host)
- **Process Name**: `cmd.exe`
- **Architecture**: Console window owned by conhost.exe
- **Process Tree Example**:
  ```
  conhost.exe (Console Window Host)
    └─ cmd.exe (PID: xxxxx)
        └─ claude.exe (PID: yyyyy)
  ```
- **UI Automation Root**: conhost.exe window
- **Expected ControlTypes**: Window → Edit/Document

#### PowerShell (Console)
- **Process Name**: `powershell.exe`
- **Architecture**: Similar to CMD, uses conhost.exe
- **Process Tree Example**:
  ```
  conhost.exe
    └─ powershell.exe (PID: xxxxx)
        └─ claude.exe (PID: yyyyy)
  ```
- **UI Automation Root**: conhost.exe window
- **Expected ControlTypes**: Window → Edit/Document

#### PowerShell 7+ (pwsh)
- **Process Name**: `pwsh.exe`
- **Architecture**: May use Windows Terminal or conhost
- **Behavior**: Similar to PowerShell but newer implementation

#### Git Bash
- **Process Name**: `bash.exe` or `mintty.exe`
- **Architecture**: MinTTY window or console host
- **Process Tree Example**:
  ```
  mintty.exe (PID: xxxxx)
    └─ bash.exe (PID: yyyyy)
        └─ claude.exe (PID: zzzzz)
  ```

## UI Automation Element Structure

### Expected Hierarchy

```
Window (ControlType.Window)
  Name: "PowerShell" | "Command Prompt" | "Windows Terminal" | etc.
  ProcessId: [terminal process]
  
  └─ Document/Edit (ControlType.Edit or ControlType.Document)
      Name: Usually empty or terminal title
      ControlType: Likely Edit or Document
      
      Patterns:
        - TextPattern: Exposes terminal buffer text
        - ValuePattern: May expose current visible text
        - ScrollPattern: Terminal scrolling capability
```

### Key Properties for Detection

#### Terminal Window Properties
- **Name**: Window title (often shows current directory or "Administrator")
- **ClassName**: Varies by terminal
  - Windows Terminal: Often contains "XAML"
  - CMD: "ConsoleWindowClass"
  - PowerShell: "ConsoleWindowClass"
- **ProcessId**: Terminal emulator process
- **AutomationId**: Typically empty for terminal windows

#### Text Content Access
- **TextPattern**: Primary mechanism for reading terminal text
  - `DocumentRange`: Full terminal buffer
  - `GetVisibleRanges()`: Currently visible text
  - `GetText(-1)`: All available text
- **ValuePattern**: May provide current visible content
  - `Value` property: Potentially terminal text

## Claude Code Permission Prompt Structure

### Typical Prompt Format

Based on Claude Code CLI behavior, permission prompts follow this pattern:

```
Claude Code wants to use <Tool Name>

Description: <tool description>

<additional context>

Options:
  1. Allow
  2. Deny
  3. Always allow
  4. Never allow

Enter your choice:
```

### Text Characteristics

**Stable Markers**:
- Contains "Claude" or "Claude Code" in prompt text
- Contains tool/command description
- Contains numbered options (1, 2, 3, 4)
- Options include "Allow", "Deny", "Always", "Never"
- Prompt ends with "Enter your choice:" or similar

**Variable Content**:
- Tool name changes per request
- Description changes per tool
- Context information varies
- Number of options may vary

### Detection Signals

#### Strong Positive Signals
1. Text contains "Claude Code wants to" or similar permission language
2. Text contains numbered option list with "Allow" and "Deny"
3. Text ends with input prompt ("Enter your choice:", "Select option:", etc.)
4. Process tree includes `claude.exe` or similar Claude process

#### Weak Positive Signals
1. Terminal shows recent text activity
2. Terminal has focus
3. Cursor is at input position

#### Disqualifying Signals
1. No Claude process in process tree
2. Text pattern doesn't match permission structure
3. Window doesn't have text access patterns

## Pattern Support Analysis

### Expected Pattern Availability

#### TextPattern (IUIAutomationTextPattern)
- **Availability**: HIGH for modern terminals
- **Purpose**: Reading terminal buffer content
- **Methods**:
  - `DocumentRange`: Get full text range
  - `GetVisibleRanges()`: Get visible text
  - `GetSelection()`: Get selected text (if any)
- **Critical for**: Detecting prompt text content

#### ValuePattern (IUIAutomationValuePattern)
- **Availability**: MEDIUM
- **Purpose**: Get/Set value of controls
- **Methods**:
  - `Value`: Current control value
  - `SetValue()`: Modify value (read-only for terminals)
- **Critical for**: Alternative text access

#### ScrollPattern (IUIAutomationScrollPattern)
- **Availability**: HIGH
- **Purpose**: Terminal scroll state
- **Not needed for**: Detection (but useful for context)

#### InvokePattern, SelectionItemPattern
- **Availability**: NOT EXPECTED
- **Reason**: Permission options are text, not controls
- **Implication**: Cannot directly "click" options

## Interaction Model

### How Users Respond

Users respond to permission prompts via **keyboard input**:
1. Type option number (1, 2, 3, or 4)
2. Press Enter
3. Claude Code processes the response

### Automation Implications

**Cannot use**: `InvokePattern.Invoke()` on permission options  
**Must use**: Keyboard simulation via SendKeys or Input Injection

**Required Steps**:
1. Detect permission prompt via text pattern
2. Parse available options
3. Determine appropriate response (based on rules)
4. Send keyboard input to terminal window
5. Send Enter key to submit

**Safety Requirement**: Verify terminal has focus and is the active window before sending keys

## Terminal-Specific Differences

### Windows Terminal
- **Modern UI Automation**: Good pattern support
- **TextPattern**: Well-implemented
- **Multi-tab Support**: Must identify correct tab
- **Process Isolation**: Tabs may run in separate processes

### CMD / PowerShell (conhost.exe)
- **Legacy Console**: Basic UI Automation support
- **TextPattern**: Available but may be limited
- **Single Window**: Simpler process model
- **Console Host**: Actual window owned by conhost.exe

### Git Bash (MinTTY)
- **Third-party Terminal**: Variable UI Automation support
- **TextPattern**: May or may not be available
- **POSIX Environment**: Different behavior from Windows shells
- **Risk**: Lowest automation reliability

## Detection Strategy

### Multi-Stage Detection Pipeline

```
Stage 1: Terminal Window Identification
├─ Enumerate all windows
├─ Filter by known terminal process names
├─ Verify window has text access patterns
└─ Build candidate list

Stage 2: Claude Process Verification
├─ Get window process ID
├─ Walk process tree (parent/child)
├─ Search for claude.exe or related process
└─ Verify Claude Code is actually running

Stage 3: Text Content Analysis
├─ Extract terminal text via TextPattern or ValuePattern
├─ Parse text for permission prompt patterns
├─ Identify prompt type and available options
└─ Construct PermissionRequest object

Stage 4: Validation
├─ Verify all required fields present
├─ Confirm option format is valid
├─ Check for ambiguous state
└─ Return DetectedPrompt or null
```

### Conservative Detection Rules

**REQUIRE ALL**:
1. ✅ Terminal window identified by process name
2. ✅ Claude process found in process tree
3. ✅ Text content accessible via TextPattern or ValuePattern
4. ✅ Text matches permission prompt pattern
5. ✅ Numbered options with "Allow"/"Deny" present
6. ✅ Input prompt detected at end

**REJECT IF ANY**:
1. ❌ No Claude process in tree
2. ❌ Text pattern ambiguous or incomplete
3. ❌ Cannot access text content
4. ❌ Terminal window is not focused/active
5. ❌ Multiple potential prompts detected (ambiguous state)

## Limitations and Constraints

### Known Limitations

1. **Text-Only Detection**
   - Cannot differentiate permission types by UI structure
   - Must rely on text parsing
   - Vulnerable to text rendering issues

2. **Terminal Variability**
   - Different terminals expose text differently
   - Some terminals may not support TextPattern
   - Scrollback may hide prompt if terminal is scrolled

3. **Timing Sensitivity**
   - Prompt may appear and disappear quickly
   - User may manually respond before automation detects
   - Text content may change during analysis

4. **Process Tree Complexity**
   - Process relationships vary by terminal
   - Claude may run via wrapper scripts
   - Process tree walking may be expensive

5. **Focus Requirements**
   - Must verify terminal has focus before sending keys
   - Cannot reliably determine if user is actively typing
   - Risk of sending keys to wrong window

### Unsupported Scenarios

❌ **VSCode Integrated Terminal**: Different process model, UI Automation characteristics unknown  
❌ **Cygwin Terminal**: Non-standard Windows terminal, untested  
❌ **Third-party Terminals**: PuTTY, Alacritty, etc. - unknown support  
❌ **Remote/SSH Sessions**: Claude running on remote machine  
❌ **Elevated/UAC Prompts**: Cannot automate elevated permission dialogs  

## Required Manual Testing

### Critical Validations Needed

#### 1. Text Pattern Access ⚠️ UNTESTED
- **Test**: Can TextPattern or ValuePattern access terminal text?
- **Method**: Use Phase 1 inspector on terminal with Claude prompt
- **Verify**: Text content includes permission prompt text
- **Terminals**: Windows Terminal, CMD, PowerShell, Git Bash

#### 2. Text Pattern Format ⚠️ UNTESTED
- **Test**: What format does terminal text use?
- **Method**: Capture actual text returned by TextPattern.DocumentRange.GetText()
- **Verify**: Line breaks, formatting, cursor position
- **Critical**: Needed for reliable parsing

#### 3. Process Tree Correlation ⚠️ UNTESTED
- **Test**: Can we reliably find claude.exe from terminal window?
- **Method**: Inspect window ProcessId, walk tree to find claude.exe
- **Verify**: Relationship is stable across terminals
- **Critical**: Essential for confidence that prompt is from Claude

#### 4. Timing and State ⚠️ UNTESTED
- **Test**: How long does prompt remain detectable?
- **Method**: Trigger prompt, measure detection window
- **Verify**: Sufficient time for detection and response
- **Critical**: Determines polling frequency requirements

#### 5. Keyboard Input Delivery ⚠️ UNTESTED
- **Test**: Can we reliably send keyboard input to terminal?
- **Method**: Use SendKeys or other input methods
- **Verify**: Terminal receives input correctly
- **Risk**: High - wrong input could cause issues

## Recommended Architecture

### Interfaces

```csharp
IClaudeSessionDetector
    DetectActiveSessions() -> ClaudeSession[]
    IsClaudeProcess(int processId) -> bool
    
IClaudePromptDetector  
    DetectPrompt(ClaudeSession) -> DetectedPrompt?
    GetTerminalText(IntPtr windowHandle) -> string?
    
IClaudePromptParser
    ParsePermissionRequest(string text) -> PermissionRequest?
    IsValidPromptFormat(string text) -> bool
```

### Models

```csharp
ClaudeSession
    - IntPtr TerminalWindowHandle
    - int TerminalProcessId
    - int ClaudeProcessId
    - TerminalType (WindowsTerminal | CMD | PowerShell | GitBash)
    - DateTime DetectedAt
    
DetectedPrompt
    - ClaudeSession Session
    - string RawText
    - PermissionRequest Request
    - PromptLocation (line/column if available)
    - DateTime DetectedAt
    
PermissionRequest
    - string ToolName
    - string Description
    - PermissionOption[] Options
    - string Context
    
PermissionOption
    - int Number
    - string Text (Allow, Deny, Always, Never)
```

## Evidence vs. Assumptions

### ✅ Evidence-Based (Observable)
- Claude Code runs in terminal environments
- Permission prompts are CLI text output
- Users respond via keyboard number input
- Terminal windows are accessible via UI Automation
- Process trees can be traversed

### ⚠️ Assumption-Based (Requires Validation)
- TextPattern provides access to terminal text
- Text format is parsable and consistent
- Claude process is detectable in process tree
- Timing allows for reliable detection
- Keyboard input can be safely automated

### ❌ Unknown (Critical Gaps)
- Exact TextPattern text format for each terminal
- Reliability of process tree correlation
- Detection latency and timing constraints
- Edge cases in prompt format variations
- Behavior with multiple simultaneous prompts

## Next Steps

1. **Implement detection framework** with interfaces and models
2. **Create parser** for known prompt patterns (based on Claude CLI docs)
3. **Implement conservative detection** that defaults to ASK on ambiguity
4. **Create test infrastructure** using synthetic text samples
5. **Document manual testing requirements** for each terminal type
6. **Phase 3 requirement**: Capture real prompts using Phase 1 inspector before enabling automation

## References

- Windows UI Automation API: TextPattern, ValuePattern
- Terminal process models: conhost.exe, WindowsTerminal.exe
- Claude Code CLI: Permission prompt behavior (observed)
- Console Host architecture: Windows console subsystem

---

**Status**: Architecture designed based on research and Claude Code CLI behavior.  
**Confidence**: HIGH for architecture design | MEDIUM for implementation details | LOW for terminal-specific text access  
**Blocker**: Requires manual validation of text pattern access before automation can be safely enabled
