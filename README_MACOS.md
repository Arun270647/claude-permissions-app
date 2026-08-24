# Claude Permission Assistant - macOS Build Guide

## Overview

The macOS version uses:
- **Avalonia UI** - Cross-platform XAML framework (similar to WPF)
- **AppleScript** - For terminal text extraction and keystroke injection
- **macOS Accessibility API** - For terminal access (requires permissions)

## Project Structure

```
src/
├── ClaudePermissionAssistant.Core/          # Shared (Windows + macOS)
│   ├── Models/                              # Platform-agnostic data models
│   ├── Interfaces/                          # Platform-agnostic interfaces
│   └── Services/
│       └── ClaudePromptParserSimple.cs     # Prompt parser (moved from Windows)
│
├── ClaudePermissionAssistant.MacOS/         # macOS automation layer
│   └── Services/
│       ├── MacOSTerminalAccessor.cs        # Text extraction via AppleScript
│       └── MacOSPromptExecutor.cs          # Keystroke injection via AppleScript
│
├── ClaudePermissionAssistant.MacApp/        # Cross-platform UI (Avalonia)
│   ├── Program.cs
│   ├── App.axaml                           # Application XAML
│   ├── App.axaml.cs
│   ├── MainWindow.axaml                    # Main UI (similar to Windows version)
│   └── MainWindow.axaml.cs
│
├── ClaudePermissionAssistant.Automation/    # Windows automation (existing)
└── ClaudePermissionAssistant.App/          # Windows WPF app (existing)
```

## Prerequisites

### On macOS

1. **.NET 8 SDK**
   ```bash
   brew install dotnet-sdk
   ```

2. **Accessibility Permissions**
   - System Settings → Privacy & Security → Accessibility
   - Grant permission to Terminal.app (or the app you're running from)

3. **Terminal.app or iTerm2**
   - The app currently supports Terminal.app by default
   - iTerm2 support requires modifying the AppleScript

## Building

### macOS Build

```bash
# Build macOS automation library
dotnet build src/ClaudePermissionAssistant.MacOS/ClaudePermissionAssistant.MacOS.csproj

# Build Avalonia UI app for macOS
dotnet build src/ClaudePermissionAssistant.MacApp/ClaudePermissionAssistant.MacApp.csproj
```

### Publish as .app Bundle (macOS)

```bash
dotnet publish src/ClaudePermissionAssistant.MacApp/ClaudePermissionAssistant.MacApp.csproj \
  -c Release \
  -r osx-x64 \
  --self-contained \
  -p:PublishSingleFile=true \
  -o publish/osx-x64
```

For Apple Silicon (M1/M2/M3):
```bash
dotnet publish src/ClaudePermissionAssistant.MacApp/ClaudePermissionAssistant.MacApp.csproj \
  -c Release \
  -r osx-arm64 \
  --self-contained \
  -p:PublishSingleFile=true \
  -o publish/osx-arm64
```

## How It Works (macOS)

### Terminal Text Extraction

Uses AppleScript to read Terminal.app contents:

```applescript
tell application "Terminal"
    try
        set activeWindow to front window
        set activeTab to selected tab of activeWindow
        return contents of activeTab
    on error
        return ""
    end try
end tell
```

This extracts the visible terminal text that contains Claude's permission prompts.

### Keystroke Injection

Uses AppleScript with System Events:

```applescript
tell application "System Events"
    tell process "Terminal"
        keystroke "2"      -- Select option 2
        delay 0.1
        keystroke return   -- Press Enter
    end tell
end tell
```

### Prompt Detection

The `ClaudePromptParserSimple` (now in Core) works identically on both platforms:
- Searches for "Do you want to proceed?" pattern
- Identifies numbered options (1. Yes, 2. Yes from this project, 3. No)
- Extracts the persistent approval option number

## Current Status

### ✅ Completed
- Project structure created
- Parser moved to Core (platform-agnostic)
- macOS automation layer (basic structure)
- Avalonia UI app (basic structure)
- Windows app still fully functional

### 🚧 In Progress
- macOS automation testing
- iTerm2 support
- macOS-specific terminal detection

### 📋 Todo
- Test on actual macOS machine
- Handle macOS Accessibility permissions gracefully
- Create .app bundle with icon
- Add macOS menu bar integration (equivalent to Windows tray)
- Support for multiple terminal types (iTerm2, Alacritty, etc.)

## Testing on macOS

1. **Grant Accessibility Permissions**
   ```bash
   # Run this once to trigger permission dialog
   dotnet run --project src/ClaudePermissionAssistant.MacApp/ClaudePermissionAssistant.MacApp.csproj
   ```

2. **Test Terminal Text Extraction**
   - Open Terminal.app
   - Trigger a Claude Code permission prompt
   - Verify the app can read the terminal text

3. **Test Keystroke Injection**
   - Ensure focus is on Terminal.app
   - App should automatically send "2" + Enter

## Cross-Platform Development

The app now supports both Windows and macOS:

### Windows (WPF)
```bash
dotnet publish src/ClaudePermissionAssistant.App/ClaudePermissionAssistant.App.csproj \
  -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish/win-x64
```

### macOS (Avalonia)
```bash
dotnet publish src/ClaudePermissionAssistant.MacApp/ClaudePermissionAssistant.MacApp.csproj \
  -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true -o publish/osx-arm64
```

## Known Limitations (macOS)

1. **Accessibility API** - Requires user to grant permissions manually
2. **Terminal.app only** - Currently hardcoded for Terminal.app
3. **AppleScript latency** - ~50-100ms overhead per script execution
4. **No background mode** - Unlike Windows tray, macOS dock/menu bar integration pending

## Future Enhancements

- Support for iTerm2, Alacritty, Kitty
- Native macOS menu bar (NSStatusItem)
- Launch at login support
- Sandboxed .app for App Store distribution
- Universal binary (x64 + ARM64 combined)

## Troubleshooting

### "Operation not permitted" error
→ Grant Accessibility permissions in System Settings

### AppleScript "Can't get contents of tab" error
→ Make sure Terminal.app is the active application

### Terminal text extraction returns empty string
→ Check that the terminal window has focus and contains text

## Contributing

When adding platform-specific code:
- Windows: Put in `ClaudePermissionAssistant.Automation`
- macOS: Put in `ClaudePermissionAssistant.MacOS`
- Shared: Put in `ClaudePermissionAssistant.Core`
