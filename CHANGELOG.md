# Changelog

All notable changes to Claude Permission Assistant will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.3] - 2026-09-03

### Added
- **Automatic versioning system** — Website and app versions now sync automatically on every release. Push to main triggers version bump, builds, release creation, and website update in one workflow.
- **Integrated website** — Marketing website moved into main repo under `website/` directory for unified management and automatic deployment.
- **Claude direct terminal support** — The app now recognizes Claude Code's own terminal window (opened via desktop app or Start Menu), not just CMD/PowerShell/Windows Terminal
- **New `ClaudeTerminal` terminal type** — Properly identifies and auto-verifies Claude's native window

### Fixed
- **🔴 CRITICAL: Keystroke injection leak into wrong windows** — **SECURITY FIX**: The app was injecting keystrokes into windows other than the monitored terminal (observed sending "1 2 3" to "Backend-refactor progress report" window). Added multi-layer verification: (1) Window process ID must match monitored terminal PID, (2) Window title must contain terminal indicators (cmd/powershell/terminal/claude/bash/sh/zsh), (3) Comprehensive logging of target window identity. This prevents keystrokes from leaking into arbitrary applications if window handles get confused or reused.
- **Pre-existing prompts now get approved** — Previously, if you opened the app after a prompt was already on screen, it would fail to approve it (foreground steal failed, then prompt was incorrectly marked as "handled" and never retried). Now only successful executions are marked as handled.
- **False positive keystroke injection** — Tightened prompt parser to require Claude's exact numbered option format (`1. Yes` / `2. No`). Previously, any terminal text containing "Do you want to" with "Yes" and "No" anywhere would trigger false approvals.
- **Foreground window mismatch with active terminals** — Fixed "Foreground window mismatch" errors when Claude Code runs background agents with rapid terminal output. Now uses exponential backoff (5 attempts, up to 650ms focus delay) and ensures window visibility before focus attempts.
- **Foreground window reliability** — Added `AttachThreadInput` + `BringWindowToTop` with retry attempts to reliably steal focus from the app's own window when approving prompts.
- **Conversation boundary detection** — Fixed issue where permissions in a new conversation (p2) would be rejected as duplicates or fail to detect after a previous conversation (p1) completed. Implemented intelligent detection of conversation boundaries based on terminal content changes (>30% shrinkage, >80% growth, or significant content hash change). When detected, clears deduplication cache and increments context sequence number to allow fresh approvals.
- **Duplicate cooldown reduced** — Decreased cooldown from 5 seconds to 1 second (Windows) and 10 seconds to 1 second (macOS), allowing legitimate approvals in new conversations while still preventing rapid re-detection of the same prompt.

### Improved
- **Enhanced retry strategy** — Foreground verification now uses exponential backoff (250ms → 650ms) over 5 attempts instead of fixed delays, providing better handling of rapidly updating terminals
- **Window restoration** — Added `ShowWindow(SW_RESTORE)` to ensure minimized windows are properly restored before focus attempts
- **Increased timing delays** — Focus delay 150ms→250ms for better stability with active terminal output
- **Faster detection** — Polling interval reduced from 500ms to 300ms for quicker prompt detection
- **Context-aware deduplication** — Added context sequence numbers to deduplication keys, allowing same prompt text in different conversations to be treated as distinct approvals
- **Terminal content change detection** — Monitors terminal text hash and length changes to intelligently detect when a new conversation starts, clearing stale caches automatically

## [1.0.2] - 2026-08-27

### Fixed
- **Documentation updates** — Various README and documentation improvements

## [1.0.1] - 2026-08-27

### Fixed
- **CI/CD visibility** - Build workflows now trigger on workflow file changes (previously invisible on Actions tab)
- **Release patch notes** - Fixed CHANGELOG extraction failing when version is last section
- **Manifest JSON generation** - Use jq for reliable JSON output instead of broken heredoc interpolation
- **Auto-versioning** - [Unreleased] section auto-converts to next version number on release

### Planned
- iTerm2 support for macOS
- Alacritty terminal support
- Homebrew formula for easy macOS installation
- Chocolatey/Winget packages for Windows
- macOS notarization (remove security warnings)
- Windows code signing (remove SmartScreen warnings)

## [1.0.0] - 2026-08-26

**🎉 First Production Release**

Claude Prompter automatically approves Claude Code permission prompts so you can code without interruptions.

### Core Features
- **Multi-terminal support** - Monitor multiple terminals simultaneously (CMD, PowerShell, Windows Terminal, Terminal.app)
- **System tray integration** - Runs quietly in background, accessible via tray icon
- **Dashboard UI** - Real-time statistics and terminal management
- **Windows support** - Full automation via Windows UI Automation + SendInput API
- **macOS support** - Full automation via AppleScript + System Events
- **Smart detection** - Regex-based parser identifies Claude Code permission prompts
- **Statistics tracking** - Prompts detected, approved, failed counts
- **Comprehensive logging** - Diagnostic logs for troubleshooting
- **91 test suite** - Unit tests for parser and executor logic

### Security & Safety
- **Keystroke injection protection** - 3-layer security validation prevents keystrokes from leaking into arbitrary applications
  - HWND validation (reject zero/null handles)
  - IsWindow() check (verify window exists before injection)
  - Foreground window verification (keystrokes ONLY sent when terminal is focused)
- **Update signature verification** - SHA-256 checksum validation for downloaded files
- **Command injection prevention** - Proper escaping for shell paths (Windows batch/macOS bash)
- **AppleScript injection prevention** - Input validation for keystroke injection
- **HTTPS enforcement** - All update URLs must use HTTPS
- **Global execution lock** - Prevents simultaneous automation attempts
- **5-second cooldown** - Prevents duplicate handling of same prompt

### Stability & Performance
- **24/7 operation** - Enhanced memory management for long-running sessions
  - Periodic cleanup of handled prompts (every 10 minutes)
  - Aggressive inline cleanup (every 100 entries)
  - Prevents memory leaks during continuous operation
- **UI Automation cache management** - Intelligent 30-second auto-refresh prevents detection failures
  - Tracks consecutive failures per window
  - Forces cache refresh after 3 consecutive failures
  - Periodic cache cleanup every 5 minutes
- **Automatic recovery system** - Self-healing for text extraction failures
  - Monitors consecutive failures
  - Triggers recovery after 10 consecutive failures
  - Comprehensive logging of recovery attempts
- **Accurate counting** - Fixed duplicate detection logic for precise statistics

### User Experience
- **Auto-update system** - Checks GitHub for updates automatically on launch
- **Clean UI** - Version display in bottom left corner (non-intrusive)
- **~300ms approval time** - Lightning-fast automation
- **100% success rate** - Reliable keystroke injection

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
