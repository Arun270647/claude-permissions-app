# Tech Stack - Complete Breakdown

## Overview

Claude Permission Assistant is a **cross-platform desktop application** that automatically approves permission prompts in Claude Code. It uses **Windows-specific automation** on Windows and **macOS-specific automation** on macOS, with a **shared parsing engine** for both platforms.

---

## Architecture at a Glance

```
┌─────────────────────────────────────────────────────────────┐
│                   User Interface Layer                       │
│  Windows: WPF (Windows-only)                                │
│  macOS: Avalonia (Cross-platform XAML)                      │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│              Platform-Specific Automation                    │
│  Windows: UI Automation + SendInput API                     │
│  macOS: AppleScript + System Events                         │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│                  Shared Core Logic                           │
│  - Prompt Parser (Regex-based)                              │
│  - Data Models (PermissionRequest, ClaudeSession)           │
│  - Interfaces (cross-platform contracts)                    │
└─────────────────────────────────────────────────────────────┘
```

---

## Core Technologies

### 1. Language & Runtime

**C# 12 with .NET 8.0**

- **Why C#?** Strong Windows integration (WPF, UI Automation), cross-platform with .NET
- **Why .NET 8?** Latest LTS (Long Term Support), best performance, AOT compilation support
- **Nullable Reference Types** - Enabled for better null-safety
- **ImplicitUsings** - Modern C# project style

**Key Benefits:**
- Native Windows API access
- Strong typing and IntelliSense
- Excellent tooling (Visual Studio, Rider, VS Code)
- Cross-platform (runs on Windows, macOS, Linux)

---

## Platform-Specific Technology

### Windows Stack

#### 1. **WPF (Windows Presentation Foundation)**

**What:** Microsoft's mature desktop UI framework for Windows

**File:** `src/Windows/ClaudePermissionAssistant.App/`

**Uses:**
- XAML for declarative UI markup
- Data binding (connects UI to code)
- System tray integration via `Hardcodet.NotifyIcon.Wpf`
- Window management

**Why WPF?**
- Native Windows look and feel
- Mature, stable, well-documented
- Excellent designer support
- Deep Windows integration

**Example XAML:**
```xml
<Window x:Class="ClaudePermissionAssistant.App.DashboardWindow"
        Title="Claude Permission Assistant">
    <TextBlock Text="Statistics" FontWeight="Bold"/>
    <TextBlock Name="PromptsDetectedTextBlock" Text="0"/>
</Window>
```

#### 2. **Windows UI Automation**

**What:** Microsoft's accessibility API for inspecting and controlling UI elements

**File:** `src/Windows/ClaudePermissionAssistant.Automation/Services/ClaudePromptDetector.cs`

**How it works:**
```csharp
// Access any window's UI tree
var element = AutomationElement.FromHandle(windowHandle);

// Extract text via TextPattern
var textPattern = element.GetCurrentPattern(TextPattern.Pattern) as TextPattern;
var text = textPattern.DocumentRange.GetText(-1);
```

**Uses:**
- Read terminal text content
- Inspect UI element properties
- Navigate element hierarchy

**Why UI Automation?**
- Standard Windows accessibility API
- Works with CMD, PowerShell, Windows Terminal
- No screen scraping needed
- Official Microsoft API

#### 3. **SendInput API (Win32)**

**What:** Windows API for programmatically sending keyboard input

**File:** `ClaudePermissionPromptExecutorHardened.cs`

**How it works:**
```csharp
[DllImport("user32.dll")]
private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

// Send "2" + Enter to terminal
SendKeyPress('2');
SendKeyPress('\r');
```

**Uses:**
- Send keyboard input to terminal
- Simulate user typing
- Approve prompts programmatically

**Why SendInput?**
- Low-level, reliable
- Works regardless of window state
- Standard Windows input injection

### macOS Stack

#### 1. **Avalonia UI**

**What:** Cross-platform XAML-based UI framework (similar to WPF)

**File:** `src/macOS/ClaudePermissionAssistant.MacApp/`

**Uses:**
- XAML syntax (almost identical to WPF)
- Runs on Windows, macOS, Linux
- Modern reactive UI patterns

**Why Avalonia?**
- Cross-platform (one codebase)
- XAML familiarity for WPF developers
- Active development, growing ecosystem
- Native look on each platform

**Example AXAML:**
```xml
<Window xmlns="https://github.com/avaloniaui"
        Title="Claude Permission Assistant">
    <StackPanel>
        <TextBlock Text="Statistics"/>
    </StackPanel>
</Window>
```

#### 2. **AppleScript**

**What:** macOS scripting language for controlling applications

**File:** `src/macOS/ClaudePermissionAssistant.MacOS/Services/MacOSTerminalAccessor.cs`

**How it works:**
```csharp
// Extract Terminal.app text via AppleScript
var script = @"
tell application ""Terminal""
    return contents of selected tab of front window
end tell
";

Process.Start("osascript", $"-e \"{script}\"");
```

**Uses:**
- Read Terminal.app content
- Send keystrokes to applications
- Control macOS apps programmatically

**Why AppleScript?**
- Native macOS automation
- Works with Terminal.app and iTerm2
- No external dependencies
- Standard macOS approach

#### 3. **System Events (macOS Accessibility)**

**What:** macOS system service for UI automation

**How it works:**
```applescript
tell application "System Events"
    tell process "Terminal"
        keystroke "2"
        keystroke return
    end tell
end tell
```

**Uses:**
- Send keyboard input to applications
- Requires Accessibility permissions

**Why System Events?**
- Official macOS automation API
- Works system-wide
- Accessibility framework integration

---

## Shared Core Components

### 1. **Regex-Based Parser**

**File:** `src/Shared/ClaudePermissionAssistant.Core/Services/ClaudePromptParserSimple.cs`

**Technology:** .NET Regular Expressions (Regex)

**What it does:**
```csharp
// Finds Claude prompts like:
// "Do you want to proceed?"
// "  1. Yes"
// "  2. Yes, allow reading from /path from this project"
// "  3. No"

var questionPattern = new Regex(@"Do you want to (proceed|create|read|write|...)");
var optionPattern = new Regex(@"^[\s>]*(\d+)[\.\)]\s*(.+?)$");
```

**Why Regex?**
- Fast pattern matching
- No ML/AI needed
- Deterministic (same input = same output)
- Works offline

### 2. **Data Models**

**File:** `src/Shared/ClaudePermissionAssistant.Core/Models/`

**Uses C# Records:**
```csharp
public class PermissionRequest
{
    public required string ToolName { get; init; }
    public required PermissionOption[] Options { get; init; }
    public int? PersistentApprovalOptionNumber { get; init; }
}
```

**Benefits:**
- Immutable by default
- Value-based equality
- Clean syntax
- Thread-safe

### 3. **Interface-Based Design**

**File:** `src/Shared/ClaudePermissionAssistant.Core/Interfaces/`

**Pattern:**
```csharp
public interface IClaudePromptDetector
{
    DetectedPrompt? DetectPrompt(ClaudeSession session);
    string? GetTerminalText(IntPtr windowHandle);
}

// Windows implements it one way
// macOS implements it another way
```

**Benefits:**
- Platform abstraction
- Easy to test (mock interfaces)
- Clean separation of concerns

---

## Key Libraries & NuGet Packages

### Windows App

| Package | Version | Purpose |
|---------|---------|---------|
| **Hardcodet.NotifyIcon.Wpf** | 2.0.1 | System tray icon support |
| **Microsoft.Extensions.DependencyInjection** | 8.0.0 | Dependency injection container |
| **Microsoft.Extensions.Hosting** | 8.0.0 | Application lifetime management |
| **Microsoft.Extensions.Logging** | 8.0.0 | Structured logging |
| **System.Management** | 8.0.0 | Windows process management |

### macOS App

| Package | Version | Purpose |
|---------|---------|---------|
| **Avalonia** | 11.0.10 | Cross-platform UI framework |
| **Avalonia.Desktop** | 11.0.10 | Desktop-specific features |
| **Avalonia.Themes.Fluent** | 11.0.10 | Modern Fluent Design theme |
| **Avalonia.Fonts.Inter** | 11.0.10 | Inter font family |

### Testing

| Package | Version | Purpose |
|---------|---------|---------|
| **xUnit** | 2.5.3 | Testing framework |
| **Moq** | 4.20.70 | Mocking framework |
| **coverlet.collector** | 6.0.0 | Code coverage |

---

## Design Patterns Used

### 1. **Singleton Pattern**

**Where:** System tray application

**Why:** Only one instance should run

```csharp
public class SingleInstanceManager
{
    private Mutex _mutex;
    public bool IsFirstInstance => _mutex != null;
}
```

### 2. **Observer Pattern**

**Where:** Background monitoring service

**Why:** UI needs to react to detection events

```csharp
public event EventHandler<StatisticsUpdatedEventArgs>? StatisticsUpdated;
public event EventHandler<string>? StatusChanged;
```

### 3. **Strategy Pattern**

**Where:** Platform-specific automation

**Why:** Different algorithms for Windows vs macOS

```csharp
interface IClaudePromptDetector
{
    // Windows: ClaudePromptDetector (UI Automation)
    // macOS: MacOSTerminalAccessor (AppleScript)
}
```

### 4. **Global Lock Pattern**

**Where:** Executor prevents race conditions

**Why:** Multiple terminals shouldn't fight for focus

```csharp
private static readonly object _executionGate = new();

lock (_executionGate)
{
    // Only one terminal processes at a time
}
```

### 5. **Cooldown Pattern**

**Where:** Duplicate prompt prevention

**Why:** Don't re-handle same prompt multiple times

```csharp
private readonly Dictionary<string, DateTime> _handledPrompts = new();
if (DateTime.UtcNow - handledAt < TimeSpan.FromSeconds(5))
    return; // Skip - handled recently
```

---

## Architecture Decisions

### Why Not Electron/Web?

**Pros of Native:**
- Direct Windows API access (UI Automation, SendInput)
- Smaller memory footprint (70MB vs 150MB+)
- Better performance
- Native system tray

**Cons:**
- Need separate codebases for Windows/macOS
- More complex build process

### Why Not Python?

**Pros of C#/.NET:**
- Native Windows integration
- Strong typing (catches bugs at compile time)
- Better performance
- Easier to distribute (single exe)
- Cross-platform with .NET

**Cons:**
- Steeper learning curve than Python
- Larger runtime (~70MB with .NET included)

### Why Not Screen Scraping?

**Pros of UI Automation:**
- Accessibility API (official, supported)
- Text-based (no OCR needed)
- Reliable across resolutions/themes
- Respects accessibility standards

**Cons:**
- Requires terminals to expose text via UI Automation
- More complex than screenshot + OCR

---

## Data Flow

### Complete Flow from Terminal → Approval

```
1. DETECTION (500ms polling loop)
   ↓
   Background Monitor Service
   ↓
   Platform-specific detector (Windows: UI Automation, macOS: AppleScript)
   ↓
   Extract terminal text
   
2. PARSING
   ↓
   ClaudePromptParserSimple (shared, regex-based)
   ↓
   Match pattern: "Do you want to proceed?" + options
   ↓
   Identify persistent approval option number (usually 2)
   
3. VALIDATION
   ↓
   Check duplicate cooldown (5 seconds)
   ↓
   Verify prompt is valid format
   ↓
   Confirm approval option exists
   
4. EXECUTION (global lock - single-threaded)
   ↓
   Acquire _executionGate lock
   ↓
   Call SetForegroundWindow (Windows) or activate (macOS)
   ↓
   Wait 200ms for focus to settle
   ↓
   Send keys: "2" + Enter
   ↓
   Wait 500ms for processing
   ↓
   Mark as handled (5 second cooldown)
   ↓
   Release lock
   
5. REPORTING
   ↓
   Update statistics (detected, approved, failed)
   ↓
   Log to file
   ↓
   Update UI
```

---

## Testing Strategy

### Unit Tests (91 tests)

**Framework:** xUnit

**Coverage:**
- Prompt parser (various formats)
- Session detection
- Option identification
- Edge cases (malformed prompts, missing options)

**Example:**
```csharp
[Fact]
public void ParsePermissionRequest_WithPersistentOption_FindsCorrectOption()
{
    var text = @"
Do you want to proceed?
  1. Yes
  2. Yes, allow reading from /path from this project
  3. No
";
    
    var request = _parser.ParsePermissionRequest(text);
    
    Assert.Equal(2, request.PersistentApprovalOptionNumber);
}
```

### Integration Testing

**Manual testing required:**
- Real terminal windows
- Real Claude Code sessions
- Verify text extraction works
- Confirm keyboard input is received

---

## Performance Characteristics

### Polling Frequency
- **500ms** polling interval
- Checks all monitored terminals every cycle
- Low CPU usage when idle

### Execution Speed
- **Detection:** <10ms (regex matching)
- **Focus + Input:** ~300ms total
  - 200ms focus delay
  - 100ms key press delay
  - Not blocking (global lock)

### Memory Usage
- **Windows:** ~50-80MB RAM (WPF app)
- **macOS:** ~60-90MB RAM (Avalonia app)
- Published exe: ~70MB (includes .NET runtime)

### Scalability
- **Multi-terminal:** Yes (each monitored independently)
- **Global lock:** Prevents race conditions
- **Cooldown:** 5 seconds per unique prompt text

---

## Security Considerations

### Permissions Required

**Windows:**
- No special permissions (runs as regular user)
- UI Automation API is standard Windows feature

**macOS:**
- **Accessibility permissions** required
- User must grant in System Preferences → Security & Privacy → Accessibility

### What the App Can Do

✅ **Can:**
- Read text from terminal windows
- Send keyboard input to terminals
- Monitor processes

❌ **Cannot:**
- Access files directly
- Make network connections
- Modify system settings
- Run as administrator/root

### Safety Features

1. **Global execution lock** - Only one terminal at a time
2. **Duplicate detection** - Won't re-handle same prompt
3. **Cooldown period** - 5 seconds between attempts
4. **No blind automation** - Always parses before acting
5. **Local only** - No network, no telemetry, no data leaves machine

---

## Build & Distribution

### Build Tools

**Requirements:**
- .NET 8.0 SDK
- Windows 10/11 (for Windows build)
- macOS 10.15+ (for macOS build)

**Build Commands:**
```bash
# Windows
dotnet publish src/Windows/ClaudePermissionAssistant.App/ClaudePermissionAssistant.App.csproj \
  -c Release -r win-x64 --self-contained -p:PublishSingleFile=true

# macOS
dotnet publish src/macOS/ClaudePermissionAssistant.MacApp/ClaudePermissionAssistant.MacApp.csproj \
  -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true
```

### Publishing Configuration

**Windows (.csproj):**
```xml
<PublishSingleFile>true</PublishSingleFile>
<SelfContained>true</SelfContained>
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
<IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
<EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
```

**Result:**
- Single 70MB .exe
- No .NET installation required
- Runs on any Windows 10/11 x64

---

## CI/CD Pipeline

### GitHub Actions Workflow

**File:** `.github/workflows/release.yml`

**Triggers:**
- Push tags matching `v*.*.*`
- Manual workflow dispatch

**Jobs:**
1. **Build Windows** (runs-on: windows-latest)
2. **Build macOS** (runs-on: macos-latest, matrix: x64 + arm64)
3. **Run Tests** (runs-on: windows-latest)
4. **Create Release** (uploads all binaries)

**Benefits:**
- Automated builds on every release tag
- Cross-platform builds
- Automated testing
- Automatic GitHub Releases

---

## Future Technology Considerations

### Potential Improvements

1. **Auto-update mechanism**
   - Squirrel.Windows (Windows)
   - Sparkle (macOS)

2. **Crash reporting**
   - Sentry
   - Application Insights

3. **Analytics**
   - Privacy-respecting usage stats
   - Error frequency tracking

4. **Extended terminal support**
   - iTerm2 (macOS)
   - Alacritty
   - Kitty

---

## Learning Resources

### For Understanding the Code

**C# / .NET:**
- Microsoft Learn: https://learn.microsoft.com/dotnet/
- C# docs: https://learn.microsoft.com/dotnet/csharp/

**WPF:**
- WPF tutorial: https://learn.microsoft.com/dotnet/desktop/wpf/

**Avalonia:**
- Avalonia docs: https://docs.avaloniaui.net/

**UI Automation:**
- Windows UI Automation: https://learn.microsoft.com/windows/win32/winauto/

**AppleScript:**
- AppleScript guide: https://developer.apple.com/library/archive/documentation/AppleScript/

### For Contributing

1. Clone the repo
2. Open `ClaudePermissionAssistant.sln` in Visual Studio or Rider
3. Run tests: `dotnet test`
4. Read `PROJECT_STRUCTURE.md` for organization

---

## Summary

| Aspect | Technology |
|--------|------------|
| **Language** | C# 12 |
| **Runtime** | .NET 8.0 |
| **UI Framework** | WPF (Windows), Avalonia (macOS) |
| **Automation** | Windows UI Automation, AppleScript |
| **Parser** | Regular Expressions |
| **Testing** | xUnit + Moq |
| **Distribution** | Single-file executables (70MB) |
| **CI/CD** | GitHub Actions |
| **License** | MIT |

**Key Strength:** Platform-native automation with shared parsing logic.

**Trade-off:** More complex than a web app, but much better Windows integration and performance.
