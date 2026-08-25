# Claude Prompter

> Automatically approve Claude Code permission prompts so you can focus on coding.

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20macOS-lightgrey.svg)]()
[![Version](https://img.shields.io/badge/version-1.0.1-green.svg)](https://github.com/Arun270647/claude-permissions-app/releases/latest)

<p align="center">
  <img src="https://img.shields.io/badge/Windows-10%2F11-0078D6?logo=windows&logoColor=white" alt="Windows 10/11"/>
  <img src="https://img.shields.io/badge/macOS-10.15+-000000?logo=apple&logoColor=white" alt="macOS 10.15+"/>
</p>

> 🌐 **[Visit Website](https://cpa-web-swart.vercel.app/)** • Modern landing page with downloads and documentation

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
✅ **Auto-update system** - Checks for updates automatically (v1.0.1+)  
✅ **Professional packaging** - .exe for Windows, .dmg for macOS

## Installation

### Quick Download

**[Download Latest Release (v1.0.1)](https://github.com/Arun270647/claude-permissions-app/releases/latest)**

Or visit the website: **[cpa-web-swart.vercel.app](https://cpa-web-swart.vercel.app/)**

### Windows

1. **Download** the latest release:
   - [ClaudePrompter-Windows-v1.0.1.exe](https://github.com/Arun270647/claude-permissions-app/releases/download/v1.0.1/ClaudePrompter-Windows-v1.0.1.exe)

2. **Run the .exe**
   - Double-click the downloaded file
   - Windows SmartScreen may appear → Click "More info" → "Run anyway"
   - The app will open in your system tray (bottom-right corner)

3. **Add terminals to monitor**
   - Click the tray icon → Open
   - Click "+ Add Terminal"
   - Select your CMD/PowerShell/Terminal window
   - Click "Select"

### macOS

1. **Download** the appropriate .dmg for your Mac:
   - **Apple Silicon (M1/M2/M3/M4)**: [ClaudePrompter-macOS-arm64-v1.0.1.dmg](https://github.com/Arun270647/claude-permissions-app/releases/download/v1.0.1/ClaudePrompter-macOS-arm64-v1.0.1.dmg)
   - **Intel Mac**: [ClaudePrompter-macOS-x64-v1.0.1.dmg](https://github.com/Arun270647/claude-permissions-app/releases/download/v1.0.1/ClaudePrompter-macOS-x64-v1.0.1.dmg)

2. **Install from DMG**
   - Double-click the downloaded .dmg file
   - Drag the app to Applications folder (or run directly)
   - macOS may block it on first run (not notarized yet)
   - Right-click the app → Open → Click "Open" to confirm

3. **Grant permissions**
   - Grant Accessibility permissions when prompted
   - System Settings → Privacy & Security → Accessibility
   - Enable for Claude Prompter

See [MACOS_SETUP.md](docs/MACOS_SETUP.md) for detailed macOS setup instructions.

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
│  CLAUDE PROMPTER v1.0.1                 │
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

## What's Been Done

### v1.0.1 (Current Release)
✅ **Rebranded** from "Claude Permission Assistant" to "Claude Prompter"  
✅ **Auto-update system** - Checks GitHub for updates automatically  
✅ **Professional packaging** - .exe for Windows, .dmg for macOS  
✅ **Improved macOS distribution** - Proper .app bundles with metadata  
✅ **Modern website** - Deployed at [cpa-web-swart.vercel.app](https://cpa-web-swart.vercel.app/)  
✅ **GitHub Actions CI/CD** - Automated builds and releases  
✅ **Branch strategy** - Organized main/windows/macos/web branches  
✅ **Comprehensive documentation** - Setup guides, tech stack, architecture

### v1.0.0 (Initial Release)
✅ Core automation functionality  
✅ Multi-terminal support  
✅ Cross-platform (Windows & macOS)  
✅ System tray integration  
✅ Real-time statistics  
✅ 91 passing tests

## Roadmap

### Next Release (v1.0.2)
- [ ] **macOS notarization** - Remove security warnings for new users
- [ ] **Windows code signing** - Remove SmartScreen warnings
- [ ] **Silent update mode** - Update in background without user interaction
- [ ] **Update notifications** - Show changelog in dashboard

### Future Enhancements
- [ ] **iTerm2 support** (macOS)
- [ ] **Alacritty/Kitty support** (cross-platform)
- [ ] **Custom prompt patterns** - User-defined automation rules
- [ ] **Dark/Light theme toggle** - UI customization
- [ ] **Homebrew formula** (macOS) - `brew install claude-prompter`
- [ ] **Chocolatey package** (Windows) - `choco install claude-prompter`
- [ ] **Scoop manifest** (Windows) - `scoop install claude-prompter`
- [ ] **Multi-language support** - i18n for global users
- [ ] **Settings panel** - Customizable polling interval, cooldown duration
- [ ] **Export/Import config** - Share configurations across machines
- [ ] **Windows Terminal integration** - Deeper integration with Windows Terminal
- [ ] **Terminal.app integration** - Deeper integration with macOS Terminal
- [ ] **Sound notifications** - Optional audio feedback for approvals
- [ ] **Logs viewer** - In-app log viewing and debugging

### Under Consideration
- [ ] **Linux support** (requires X11/Wayland automation research)
- [ ] **VS Code extension** - Integrate with Claude in VS Code
- [ ] **JetBrains plugin** - Support for IntelliJ, PyCharm, etc.
- [ ] **Cloud sync** - Sync settings across devices
- [ ] **Team mode** - Share configurations across teams
- [ ] **Analytics dashboard** - Track automation efficiency over time

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

**Infrastructure:**
- GitHub Actions for CI/CD
- Vercel for website hosting
- Auto-update manifest system

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
# Output: publish/win-x64/ClaudePrompter.exe
```

**macOS:**
```bash
git clone https://github.com/Arun270647/claude-permissions-app.git
cd claude-permissions-app
chmod +x build-macos.sh
./build-macos.sh
# Output: releases/ClaudePrompter-macOS-arm64-v1.0.1.dmg
#         releases/ClaudePrompter-macOS-x64-v1.0.1.dmg
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

docs/                                          # Comprehensive documentation
├── TECH_STACK.md                             # Technical overview
├── PROJECT_STRUCTURE.md                      # Codebase organization
├── AUTO_UPDATE_ENABLED.md                    # Auto-update system docs
├── BRANCHING_STRATEGY.md                     # Branch workflow
└── BRANCH_STRUCTURE_COMPLETE.md             # Branch structure guide

.github/
└── workflows/
    ├── release.yml                           # Release automation
    ├── build-windows.yml                     # Windows CI
    └── build-macos.yml                       # macOS CI
```

See [PROJECT_STRUCTURE.md](docs/PROJECT_STRUCTURE.md) for detailed organization.

## FAQ

**Q: Is it safe?**  
A: Yes. The code is open source, runs locally, has no network access (except update checks), and doesn't collect any data. It only reads terminal text and sends keyboard input.

**Q: Why does Windows block it?**  
A: The app isn't code-signed yet (costs $200/year). Click "More info" → "Run anyway" to proceed. You can review the source code or build it yourself.

**Q: Why does macOS need Accessibility permissions?**  
A: macOS requires Accessibility permissions to read terminal text and send keyboard input. The app cannot function without them.

**Q: Does it work with all terminals?**  
A: **Windows:** CMD, PowerShell, Windows Terminal. **macOS:** Terminal.app (iTerm2 support coming soon).

**Q: What if I don't want a specific prompt auto-approved?**  
A: The app only approves prompts with "allow from this project" options. One-time "Yes" prompts are not auto-approved.

**Q: Can I customize which terminals are monitored?**  
A: Yes. Use the "+ Add Terminal" button in the dashboard to add/remove terminals.

**Q: Does it auto-update?**  
A: Yes! Starting with v1.0.1, the app checks for updates on launch and notifies you when a new version is available.

**Q: Why the name change from "Claude Permission Assistant"?**  
A: "Claude Prompter" is shorter, more memorable, and better reflects what the app does - it prompts on your behalf!

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

**Update not working**
- Check internet connection
- Manually download from [releases page](https://github.com/Arun270647/claude-permissions-app/releases)
- See [AUTO_UPDATE_ENABLED.md](docs/AUTO_UPDATE_ENABLED.md) for troubleshooting

## Repository Structure

This repository uses a single `main` branch with platform-specific folders:

```
src/
├── Shared/           # Cross-platform core library
├── Windows/          # Windows-specific code (WPF, UI Automation)
└── macOS/            # macOS-specific code (Avalonia, AppleScript)
```

**Website:** Separate repository at **[cpa-web](https://github.com/Arun270647/cpa-web)**

GitHub Actions automatically builds both platforms when relevant code changes.

## Contributing

Contributions welcome! Please:

1. Fork the repository
2. Create a feature branch from `main`
3. Make your changes:
   - Windows-specific → `src/Windows/`
   - macOS-specific → `src/macOS/`
   - Shared code → `src/Shared/`
   - Website → **[cpa-web repository](https://github.com/Arun270647/cpa-web)**
4. Run tests (`dotnet test`)
5. Commit your changes
6. Push to your fork
7. Open a Pull Request to `main`

See [CONTRIBUTING.md](docs/CONTRIBUTING.md) for detailed guidelines.

## License

MIT License - see [LICENSE](LICENSE) for details.

## Acknowledgments

- Built for the [Claude Code](https://claude.ai/code) community
- Inspired by the need for uninterrupted AI-assisted coding
- Thanks to all contributors and testers

## Links

- **Website:** https://cpa-web-swart.vercel.app/
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

<p align="center">
  <strong>Claude Prompter v1.0.1</strong> - Your AI coding companion's best friend
</p>
