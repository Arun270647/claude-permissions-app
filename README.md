# Claude Permission Assistant

A Windows desktop application that detects and manages Claude Code permission prompts using UI Automation.

## Project Status

**Current Phase: Phase 1 - UI Automation Inspector**

Phase 1 is complete and provides a tool to inspect Windows UI Automation trees.

## Phase 1: UI Automation Inspector

### What Was Implemented

1. **Solution Architecture**
   - `ClaudePermissionAssistant.Core` - Core models and data structures
   - `ClaudePermissionAssistant.Automation` - UI Automation inspection services
   - `ClaudePermissionAssistant.App` - WPF desktop application
   - `ClaudePermissionAssistant.Automation.Tests` - Unit tests

2. **Core Models**
   - `WindowInfo` - Captures window metadata (PID, process name, title, handle)
   - `AutomationElementInfo` - Complete UI Automation element properties
   - `InspectionResult` - Contains window info and automation tree

3. **UI Automation Service**
   - `WindowInspectorService` - Main service for inspecting windows
     - Lists all available windows
     - Builds complete automation element tree
     - Extracts comprehensive properties from each element
     - Exports tree structure to text file

4. **WPF Inspector Application**
   - Window selection from all available windows
   - Real-time tree view of UI Automation hierarchy
   - Detailed property display for selected elements
   - Export functionality for diagnostic files

### Key Properties Captured

For each UI Automation element:
- Name, AutomationId, ClassName
- ControlType (Button, Edit, Window, etc.)
- RuntimeId, ProcessId
- IsEnabled, IsOffscreen
- BoundingRectangle (position and size)
- Supported patterns (Invoke, Value, Selection, etc.)
- AcceleratorKey, AccessKey, HelpText
- ItemStatus, ItemType
- Parent/child relationships

### How to Run

#### From Visual Studio / Rider
1. Open `ClaudePermissionAssistant.sln`
2. Set `ClaudePermissionAssistant.App` as startup project
3. Press F5 or click Run

#### From Command Line
```bash
dotnet build
dotnet run --project src/ClaudePermissionAssistant.App/ClaudePermissionAssistant.App.csproj
```

### How to Use the Inspector

1. **Launch the application**
   - The inspector window will open with a list of available windows

2. **Select a window to inspect**
   - Click the "Refresh Windows" button to update the list
   - Select a window from the dropdown (format: ProcessName - WindowTitle (PID))

3. **Inspect the window**
   - Click "Inspect Selected" to analyze the window
   - The left panel shows the automation tree hierarchy
   - Click any element to view its properties in the right panel

4. **Export the tree**
   - Click "Export to File" to save the complete tree structure
   - Files are saved with timestamp: `UIAutomation_ProcessName_YYYYMMDD_HHMMSS.txt`

### Testing with Claude Code

To inspect a Claude Code permission prompt:

1. Start the inspector application
2. In another terminal, run a command in Claude Code that triggers a permission prompt
3. Quickly switch to the inspector and click "Refresh Windows"
4. Find the Claude Code window in the list (look for process names like "cmd", "powershell", "WindowsTerminal")
5. Select and inspect the window
6. Look for elements with:
   - ControlType containing "Button"
   - Names like "Allow", "Deny", "Always", "Never"
   - AutomationIds that might identify permission controls

### Running Tests

```bash
dotnet test
```

Tests verify:
- Window enumeration
- Window ordering by process name
- Handling of invalid window handles
- Export text formatting

### Files Created

**Source Files:**
- `src/ClaudePermissionAssistant.Core/Models/WindowInfo.cs`
- `src/ClaudePermissionAssistant.Core/Models/AutomationElementInfo.cs`
- `src/ClaudePermissionAssistant.Core/Models/InspectionResult.cs`
- `src/ClaudePermissionAssistant.Automation/Services/WindowInspectorService.cs`
- `src/ClaudePermissionAssistant.App/MainWindow.xaml`
- `src/ClaudePermissionAssistant.App/MainWindow.xaml.cs`

**Test Files:**
- `tests/ClaudePermissionAssistant.Automation.Tests/WindowInspectorServiceTests.cs`

**Project Files:**
- `ClaudePermissionAssistant.sln`
- `src/ClaudePermissionAssistant.Core/ClaudePermissionAssistant.Core.csproj`
- `src/ClaudePermissionAssistant.Automation/ClaudePermissionAssistant.Automation.csproj`
- `src/ClaudePermissionAssistant.App/ClaudePermissionAssistant.App.csproj`
- `tests/ClaudePermissionAssistant.Automation.Tests/ClaudePermissionAssistant.Automation.Tests.csproj`

## Technology Stack

- **.NET 8.0** - Target framework
- **WPF** - Windows Presentation Foundation for UI
- **Windows UI Automation** - For inspecting and interacting with UI elements
- **xUnit** - Unit testing framework
- **C# 12** with nullable reference types enabled

## Important Findings

### UI Automation Considerations

1. **Access Limitations**
   - UI Automation requires appropriate permissions to access window elements
   - Some windows (elevated processes, secure desktop) may not be accessible
   - The inspector gracefully handles inaccessible windows

2. **Performance**
   - Tree depth is limited to 50 levels to prevent infinite recursion
   - Large windows with many elements may take time to inspect
   - Element count is displayed in the status bar

3. **Property Availability**
   - Not all elements expose all properties
   - Empty strings indicate property not available or not applicable
   - RuntimeId uniquely identifies elements but may change between sessions

4. **Pattern Support**
   - UI Automation patterns indicate available interactions
   - Common patterns: Invoke, Value, Selection, Toggle, Window
   - Pattern availability varies by control type

### Next Phase Requirements

To implement automatic permission handling, Phase 2 should:

1. **Identify Claude Code Prompts**
   - Use the inspector to capture actual Claude Code permission prompt structure
   - Document specific AutomationIds, Names, and ControlTypes
   - Identify unique characteristics that distinguish Claude prompts

2. **Implement Detection**
   - Monitor for new windows from Claude Code processes
   - Verify window structure matches expected prompt pattern
   - Extract permission details (command, tool, etc.)

3. **Safety Considerations**
   - Never interact with UAC or secure desktop
   - Verify target element belongs to expected Claude session
   - Log all automated actions with full context

## Building and Development

### Prerequisites
- .NET 8.0 SDK or later
- Windows 10/11
- Visual Studio 2022 or Rider (optional)

### Build
```bash
dotnet build
```

### Clean
```bash
dotnet clean
```

### Restore Dependencies
```bash
dotnet restore
```

## License

This is a development tool for internal use.
