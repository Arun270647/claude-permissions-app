# Claude Permission Assistant

> Automatically approve Claude Code permission prompts so you can focus on coding.

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20macOS-lightgrey.svg)]()

<p align="center">
  <img src="https://img.shields.io/badge/Windows-10%2F11-0078D6?logo=windows&logoColor=white" alt="Windows 10/11"/>
  <img src="https://img.shields.io/badge/macOS-10.15+-000000?logo=apple&logoColor=white" alt="macOS 10.15+"/>
</p>

> 🌐 **[View the Website](https://github.com/Arun270647/cpa-web)** • Modern landing page with downloads and documentation

## What It Does

Claude Code asks for permission every time it wants to read files, write code, or run commands. This app automatically approves those prompts by selecting "Yes, allow from this project" so Claude can work without interruptions.

**Before:**
```
Do you want to proceed?
  1. Yes
> 2. Yes, allow reading from /c/project from this project  
  3. No

[You have to press 2 + Enter manually every single time]
```

**After:**
```
Do you want to proceed?
  1. Yes
> 2. Yes, allow reading from /c/project from this project  ← Auto-approved!
  3. No

[App detects prompt and automatically selects option 2]
```

## Features

✅ **Multi-terminal support** - Monitor multiple terminals simultaneously  
✅ **System tray integration** - Runs quietly in the background  
✅ **Real-time statistics** - See how many prompts were auto-approved  
✅ **Cross-platform** - Works on Windows and macOS  
✅ **Safe & local** - No network access, no data leaves your machine  
✅ **Smart detection** - Only acts on genuine Claude Code prompts  

## Installation

### Windows

1. **Download** the latest release:
   - [ClaudePermissionAssistant-Windows-x64-v1.0.0.exe](https://github.com/Arun270647/claude-permissions-app/releases/latest)

2. **Run the exe**
   - Double-click the downloaded file
   - Windows SmartScreen may appear → Click "More info" → "Run anyway"
   - The app will open in your system tray (bottom-right corner)

3. **Add terminals to monitor**
   - Click the tray icon → Open
   - Click "+ Add Terminal"
   - Select your CMD/PowerShell/Terminal window
   - Click "Select"

### macOS

1. **Download** the appropriate version for your Mac:
   - Apple Silicon (M1/M2/M3): [ClaudePermissionAssistant-macOS-arm64-v1.0.0](https://github.com/Arun270647/claude-permissions-app/releases/latest)
   - Intel Mac: [ClaudePermissionAssistant-macOS-x64-v1.0.0](https://github.com/Arun270647/claude-permissions-app/releases/latest)

2. **Make it executable and run**
   ```bash
   chmod +x ~/Downloads/ClaudePermissionAssistant-macOS-*
   ./Downloads/ClaudePermissionAssistant-macOS-*
   ```

3. **Grant permissions**
   - macOS will block it on first run (not notarized)
   - Right-click the file → Open → Click "Open" to confirm
   - Grant Accessibility permissions in System Settings

## Quick Start

1. **Launch the app** (system tray icon appears)
2. **Open Claude Code** in your terminal
3. **Add the terminal** via the app's dashboard
4. **Start coding** - permissions are now auto-approved!

## How It Works

```
Claude shows prompt → App detects pattern → Sends "2 + Enter" → Prompt approved
     (500ms polling)      (regex parser)       (keyboard input)      (~300ms total)
```

1. **Monitors terminals** - Polls every 500ms via Windows UI Automation (Windows) or AppleScript (macOS)
2. **Detects prompts** - Regex parser identifies Claude's permission pattern
3. **Selects option** - Sends keyboard input to choose "allow from this project"
4. **Tracks statistics** - Shows detected, approved, and failed counts

**Safety features:**
- Global lock (only one terminal at a time)
- 5-second cooldown (won't re-handle same prompt)
- No blind automation (always parses before acting)
- Local only (no network, no telemetry)

## Screenshots

### Dashboard
Monitor multiple terminals with real-time statistics:

```
┌─────────────────────────────────────────┐
│  CLAUDE PERMISSION ASSISTANT            │
├─────────────────────────────────────────┤
│  Statistics                             │
│  Prompts Detected: 42                   │
│  Prompts Approved: 40                   │
│  Prompts Failed: 2                      │
│                                         │
│  Monitored Terminals                    │
│  ● CMD Terminal (PID: 12345)  [Remove] │
│  ● PowerShell (PID: 67890)    [Remove] │
│                                         │
│  [+ Add Terminal]              [STOP ALL]│
└─────────────────────────────────────────┘
```

## Tech Stack

**Core:**
- C# 12 with .NET 8.0
- Regex-based prompt parser (fast, deterministic, offline)

**Windows:**
- WPF for UI
- Windows UI Automation for text extraction
- SendInput API for keyboard injection

**macOS:**
- Avalonia UI (cross-platform XAML)
- AppleScript for text extraction
- System Events for keyboard injection

**See [TECH_STACK.md](docs/TECH_STACK.md) for complete technical details.**

## Building from Source

### Prerequisites
- .NET 8.0 SDK or later
- Windows 10/11 (for Windows build)
- macOS 10.15+ (for macOS build)

### Build

**Windows:**
```bash
git clone https://github.com/Arun270647/claude-permissions-app.git
cd claude-permissions-app
rebuild.bat
# Output: publish/win-x64/ClaudePermissionAssistant.exe
```

**macOS:**
```bash
git clone https://github.com/Arun270647/claude-permissions-app.git
cd claude-permissions-app
chmod +x build-macos.sh
./build-macos.sh
# Output: publish/osx-arm64/ClaudePermissionAssistant-macOS-arm64-v1.0.0
```

### Run Tests
```bash
dotnet test
# All 91 tests should pass
```

## Project Structure

```
src/
├── Shared/
│   └── ClaudePermissionAssistant.Core/        # Cross-platform parser, models
├── Windows/
│   ├── ClaudePermissionAssistant.Automation/  # Windows UI Automation
│   └── ClaudePermissionAssistant.App/         # WPF app
└── macOS/
    ├── ClaudePermissionAssistant.MacOS/       # AppleScript automation
    └── ClaudePermissionAssistant.MacApp/      # Avalonia UI

tests/
└── ClaudePermissionAssistant.Automation.Tests/

publish/
├── win-x64/                                   # Windows executable
└── osx-arm64/                                 # macOS executables
```

See [PROJECT_STRUCTURE.md](docs/PROJECT_STRUCTURE.md) for detailed organization.

## FAQ

**Q: Is it safe?**  
A: Yes. The code is open source, runs locally, has no network access, and doesn't collect any data. It only reads terminal text and sends keyboard input.

**Q: Why does Windows block it?**  
A: The app isn't code-signed (costs $200/year). Click "More info" → "Run anyway" to proceed. You can review the source code or build it yourself.

**Q: Why does macOS need Accessibility permissions?**  
A: macOS requires Accessibility permissions to read terminal text and send keyboard input. The app cannot function without them.

**Q: Does it work with all terminals?**  
A: Windows: CMD, PowerShell, Windows Terminal. macOS: Terminal.app (iTerm2 support coming soon).

**Q: What if I don't want a specific prompt auto-approved?**  
A: The app only approves prompts with "allow from this project" options. One-time "Yes" prompts are not auto-approved.

**Q: Can I customize which terminals are monitored?**  
A: Yes. Use the "+ Add Terminal" button in the dashboard to add/remove terminals.

## Troubleshooting

**Windows: "Prompts detected but not approved"**
- Check if terminal is in the monitored list
- Try running app as Administrator
- Verify UI Automation works (test with inspector tool)

**macOS: "Cannot extract terminal text"**
- Grant Accessibility permissions in System Settings
- Restart the app after granting permissions
- Check Terminal.app is the active terminal type

**High failure rate**
- Close and reopen the dashboard (it may steal focus)
- Minimize the dashboard window
- Check logs in the app's data directory

## Repository Branches

This repository uses platform-specific branches:

- **[main](https://github.com/Arun270647/claude-permissions-app)** - Production code (all platforms)
- **[windows](https://github.com/Arun270647/claude-permissions-app/tree/windows)** - Windows development (WPF, UI Automation)
- **[macos](https://github.com/Arun270647/claude-permissions-app/tree/macos)** - macOS development (Avalonia, AppleScript)

**Website:** Separate repository at **[cpa-web](https://github.com/Arun270647/cpa-web)**

**Each branch shows only relevant content:**
- `windows` branch: Windows source code only
- `macos` branch: macOS source code only
- `main` branch: Complete project (all platforms)

See [BRANCHING_STRATEGY.md](BRANCHING_STRATEGY.md) for workflow details.

## Contributing

Contributions welcome! Please:

1. Fork the repository
2. Choose the right branch:
   - Windows-specific → `windows` branch
   - macOS-specific → `macos` branch
   - Website → **[cpa-web repository](https://github.com/Arun270647/cpa-web)**
   - Cross-platform → `main` branch
3. Run tests (`dotnet test`)
4. Commit changes
5. Push to your branch
6. Open a Pull Request to `main`

See [CONTRIBUTING.md](docs/CONTRIBUTING.md) for detailed guidelines.

## Roadmap

- [ ] macOS notarization (remove security warnings)
- [ ] Windows code signing (remove SmartScreen warnings)
- [ ] iTerm2 support (macOS)
- [ ] Alacritty/Kitty support
- [ ] Auto-update mechanism
- [ ] Homebrew formula (macOS)
- [ ] Chocolatey package (Windows)

## License

MIT License - see [LICENSE](LICENSE) for details.

## Acknowledgments

- Built for the [Claude Code](https://claude.ai/code) community
- Inspired by the need for uninterrupted AI-assisted coding
- Thanks to all contributors and testers

## Links

- **Repository:** https://github.com/Arun270647/claude-permissions-app
- **Issues:** https://github.com/Arun270647/claude-permissions-app/issues
- **Releases:** https://github.com/Arun270647/claude-permissions-app/releases
- **Tech Stack:** [TECH_STACK.md](docs/TECH_STACK.md)
- **Distribution:** [DISTRIBUTION_GUIDE.md](docs/windows/DISTRIBUTION_GUIDE.md)
- **Development:** [DEV_WORKFLOW.md](docs/DEV_WORKFLOW.md)

---

<p align="center">
Made with ❤️ for a smoother Claude Code experience
</p>
