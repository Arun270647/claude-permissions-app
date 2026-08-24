# Changelog

All notable changes to Claude Permission Assistant will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned
- iTerm2 support for macOS
- Alacritty terminal support
- Homebrew formula for easy macOS installation
- Chocolatey/Winget packages for Windows
- Auto-update mechanism
- macOS notarization (remove security warnings)
- Windows code signing (remove SmartScreen warnings)

## [1.0.0] - 2024-XX-XX

### Added
- **Multi-terminal support** - Monitor multiple terminals simultaneously
- **System tray integration** - Runs quietly in background, accessible via tray icon
- **Dashboard UI** - Real-time statistics and terminal management
- **Windows support** - Full automation via Windows UI Automation + SendInput API
- **macOS support** - Full automation via AppleScript + System Events
- **Smart detection** - Regex-based parser identifies Claude Code permission prompts
- **Safety features**:
  - Global execution lock (prevents simultaneous automation)
  - 5-second cooldown (prevents duplicate handling)
  - Foreground verification (optional, logs warning if fails)
- **Statistics tracking** - Prompts detected, approved, failed counts
- **Cross-platform architecture** - Shared core, platform-specific automation
- **Comprehensive logging** - Diagnostic logs for troubleshooting
- **91 test suite** - Unit tests for parser and executor logic

### Technical Details
- C# 12 with .NET 8.0
- WPF (Windows UI)
- Avalonia UI (macOS UI)
- Single-file self-contained publishing (~70MB)
- No external dependencies
- No network access
- MIT License

### Documentation
- README.md with installation instructions
- TECH_STACK.md with technical deep-dive
- CONTRIBUTING.md with contribution guidelines
- PUBLISH_CHECKLIST.md with release instructions
- SECURITY.md with security policy
- LICENSE (MIT)

### Platform Support
- **Windows:** 10, 11 (x64)
- **macOS:** 10.15+ (x64, arm64)
- **Terminals:**
  - Windows: CMD, PowerShell, Windows Terminal
  - macOS: Terminal.app

### Known Limitations
- macOS: Terminal.app only (iTerm2 not yet supported)
- Windows: May show SmartScreen warning (not code-signed)
- macOS: Requires Accessibility permissions grant
- macOS: Not notarized (requires right-click → Open on first run)

## Development History

### Phase 5 - Multi-Terminal + Public Release Setup
- Refactored dashboard to support multiple terminals
- Added system tray integration
- Removed developer tools (UI Inspector, Prompt Test, Executor Test)
- Reorganized project structure (Shared/Windows/macOS folders)
- Created GitHub Actions workflow for automated builds
- Wrote comprehensive documentation for public release

### Phase 4 - Hardening & Bug Fixes
- **Fixed:** Keystroke spam bug (3,2,1 sent repeatedly)
- **Fixed:** Foreground verification blocking ~88% of approvals
- **Fixed:** Two-tier cooldown causing false failures
- **Improved:** Simplified execution logic (mark on success only)
- **Improved:** Removed retry logic (single attempt, always success after sending keys)
- **Improved:** Made foreground verification informational only (doesn't block)
- **Result:** ~100% approval rate

### Phase 3 - Core Functionality
- Implemented regex-based prompt parser
- Built Windows automation (UI Automation + SendInput)
- Built macOS automation (AppleScript + System Events)
- Added global execution lock
- Added 5-second cooldown mechanism
- Created test suite (91 tests)

### Phase 2 - Architecture
- Designed cross-platform architecture
- Created Core library (shared models/parser)
- Created platform-specific Automation libraries
- Set up dependency injection

### Phase 1 - POC
- Proof of concept: Detect Claude prompts in terminal
- Proof of concept: Send keyboard input automatically
- Verified Windows UI Automation works with CMD/PowerShell
- Verified AppleScript works with Terminal.app

---

## Version History

- **v1.0.0** - Initial public release

---

## Upgrade Instructions

### From Development to v1.0.0
This is the first public release. If you were using development builds:
1. Stop the old app
2. Delete old executable
3. Download v1.0.0 from GitHub Releases
4. Run new executable
5. Re-add terminals (settings are not migrated)

### Future Upgrades
Check the GitHub Releases page for new versions:
https://github.com/Arun270647/claude-permissions-app/releases

**Note:** Auto-update mechanism is planned for v1.1.0

---

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for how to contribute changes.

---

## Questions?

- **Bug reports:** https://github.com/Arun270647/claude-permissions-app/issues
- **Feature requests:** https://github.com/Arun270647/claude-permissions-app/issues
- **Security issues:** See [SECURITY.md](SECURITY.md)
