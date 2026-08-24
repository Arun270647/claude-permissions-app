# Project Structure - Platform Organized

The project is now organized into **Windows**, **macOS**, and **Shared** folders for better clarity.

## Directory Structure

```
claude-permission-app/
├── src/
│   ├── Shared/
│   │   └── ClaudePermissionAssistant.Core/      # Cross-platform models, interfaces, parser
│   │
│   ├── Windows/
│   │   ├── ClaudePermissionAssistant.Automation/   # Windows UI Automation
│   │   └── ClaudePermissionAssistant.App/          # Windows WPF app
│   │
│   └── macOS/
│       ├── ClaudePermissionAssistant.MacOS/        # macOS AppleScript automation  
│       └── ClaudePermissionAssistant.MacApp/       # Avalonia cross-platform UI
│
├── tests/
│   └── ClaudePermissionAssistant.Automation.Tests/
│
├── publish/
│   └── win-x64/
│       └── ClaudePermissionAssistant.exe (70MB)
│
├── ClaudePermissionAssistant.sln
├── rebuild.bat
├── README.md
├── README_MACOS.md
└── BUILD_STATUS.md
```

## What's Where

### Shared (Cross-Platform)
**Path:** `src/Shared/ClaudePermissionAssistant.Core/`

Contains platform-agnostic code:
- **Models** - Data structures (WindowInfo, PermissionRequest, etc.)
- **Interfaces** - Contracts for platform implementations
- **Services** - ClaudePromptParserSimple (regex-based parser)

Used by both Windows and macOS projects.

### Windows
**Path:** `src/Windows/`

#### ClaudePermissionAssistant.Automation
- Windows UI Automation for text extraction
- SendInput API for keyboard injection
- ClaudePromptDetector
- ClaudePermissionPromptExecutorHardened
- Session detection

#### ClaudePermissionAssistant.App (WPF)
- System tray application
- Dashboard UI
- Multi-terminal monitoring
- Background monitoring service
- File logging

**Build:** 
```bash
rebuild.bat
# Output: publish/win-x64/ClaudePermissionAssistant.exe
```

### macOS
**Path:** `src/macOS/`

#### ClaudePermissionAssistant.MacOS
- AppleScript-based text extraction
- AppleScript + System Events for keystrokes
- MacOSTerminalAccessor
- MacOSPromptExecutor

#### ClaudePermissionAssistant.MacApp (Avalonia)
- Cross-platform UI (works on Windows/macOS/Linux)
- Similar layout to Windows WPF version
- Menu bar integration (pending)

**Build:**
```bash
dotnet publish src/macOS/ClaudePermissionAssistant.MacApp/ClaudePermissionAssistant.MacApp.csproj \
  -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true \
  -o publish/osx-arm64
```

## Building

### Windows App
```bash
# Quick rebuild (uses rebuild.bat)
rebuild.bat

# Manual build
dotnet build src/Windows/ClaudePermissionAssistant.App/ClaudePermissionAssistant.App.csproj

# Publish single-file exe
dotnet publish src/Windows/ClaudePermissionAssistant.App/ClaudePermissionAssistant.App.csproj \
  -c Release -r win-x64 --self-contained -p:PublishSingleFile=true \
  -o publish/win-x64
```

### macOS App
```bash
# Build macOS projects
dotnet build src/macOS/ClaudePermissionAssistant.MacOS/ClaudePermissionAssistant.MacOS.csproj
dotnet build src/macOS/ClaudePermissionAssistant.MacApp/ClaudePermissionAssistant.MacApp.csproj

# Publish for Apple Silicon
dotnet publish src/macOS/ClaudePermissionAssistant.MacApp/ClaudePermissionAssistant.MacApp.csproj \
  -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true \
  -o publish/osx-arm64

# Or for Intel Macs
dotnet publish src/macOS/ClaudePermissionAssistant.MacApp/ClaudePermissionAssistant.MacApp.csproj \
  -c Release -r osx-x64 --self-contained -p:PublishSingleFile=true \
  -o publish/osx-x64
```

### All Projects
```bash
# Build everything
dotnet build

# Run tests
dotnet test
```

## Dependencies

### Shared ← Windows
- `Windows/Automation` → references `Shared/Core`
- `Windows/App` → references `Shared/Core` + `Windows/Automation`

### Shared ← macOS
- `macOS/MacOS` → references `Shared/Core`
- `macOS/MacApp` → references `Shared/Core` + `macOS/MacOS`

### Tests
- `tests/Automation.Tests` → references `Windows/Automation` + `Shared/Core`

## Benefits of This Structure

✅ **Clear platform separation** - Easy to see what's Windows vs macOS  
✅ **Shared code reuse** - Parser and models in one place  
✅ **Independent development** - Work on Windows or macOS without affecting the other  
✅ **Better for open source** - Contributors can easily find platform-specific code  
✅ **CI/CD ready** - Can build Windows and macOS in separate pipelines  

## Migration Notes

All project references have been updated:
- `.csproj` files now reference `..\..\Shared\ClaudePermissionAssistant.Core\`
- Solution file updated with new paths
- `rebuild.bat` updated to point to `src/Windows/...`
- All builds and tests passing

No code changes were required - only file moves and reference updates.
