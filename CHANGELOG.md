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
- macOS notarization (remove security warnings)
- Windows code signing (remove SmartScreen warnings)

## [1.0.3] - 2026-08-26

### Changed
- **UI improvement** - Moved version number from title bar to bottom left corner
  - Removed version from window title ("Claude Prompter v1.0.1" → "Claude Prompter")
  - Removed version from main heading ("CLAUDE PROMPTER v1.0.1" → "CLAUDE PROMPTER")
  - Added version display in bottom left corner (small, gray text)
  - Provides cleaner, less cluttered interface

### Infrastructure
- **Workflow reliability** - Made auto-release workflow idempotent
  - Workflow now checks if git tag already exists before creating it
  - Prevents "tag already exists" errors when retrying failed releases
  - Allows safe re-runs after partial failures without manual intervention

## [1.0.2] - 2026-08-26

### Fixed
- **Accurate prompt counting** - Fixed misleading statistics showing "6 detected, 1 approved" for single prompts
  - Root cause: Polling every 500ms incremented counter even for already-handled prompts
  - Solution: Check for duplicates BEFORE incrementing detection counter
  - Impact: Statistics now accurately reflect unique prompts detected
  
- **24/7 stability improvements** - Enhanced memory management for long-running sessions
  - Added periodic cleanup of handled prompts (every 10 minutes)
  - More aggressive inline cleanup (every 100 entries instead of 1000)
  - Prevents memory leaks during days/weeks of continuous operation
  - Ensures consistent behavior over extended periods

- **Test compatibility** - Fixed failing unit test for foreground verification in mock environments
  - Executor now respects `RequireForegroundVerification` config setting
  - Test mode can run without real window handles
  - Production mode still enforces security checks

### Technical Details
- Modified `BackgroundMonitorService.cs` - Reordered duplicate detection logic
- Enhanced `ClaudePermissionPromptExecutorHardened.cs` - Added periodic cleanup mechanism
- All 91 unit tests passing
- No breaking changes
- Fully backward compatible

### Documentation
- Added `docs/internal/24_7_STABILITY_AND_ACCURATE_COUNTING_FIX.md` - Comprehensive fix documentation
- Updated testing workflow in `CLAUDE.md`
- Added patch notes requirements to release process

## [1.0.1] - 2026-08-25

### Added
- **Auto-update system** - Checks GitHub for updates automatically on launch
  - SHA-256 checksum verification for downloaded files
  - HTTPS enforcement for update URLs
  - User-friendly update notifications
  
- **UI Automation cache management** - Prevents detection failures in long-running sessions
  - Intelligent 30-second auto-refresh cache
  - Tracks consecutive failures per window
  - Forces cache refresh after 3 consecutive failures
  - Periodic cache cleanup every 5 minutes

- **Automatic recovery system** - Self-healing for text extraction failures
  - Monitors consecutive text extraction failures
  - Triggers recovery after 10 consecutive failures
  - Clears caches and handled prompts
  - Comprehensive logging of recovery attempts

### Fixed
- **Long-running session stability** - App now works correctly after 30+ minutes of operation
  - Root cause: UI Automation elements became stale over time
  - Solution: Multi-layer cache management and recovery system
  - Impact: Can now run 24/7 without restart

### Security
- **CRIT-001: Update signature verification** - Added SHA-256 checksum validation
- **CRIT-002: Command injection prevention** - Proper escaping for shell paths
- **CRIT-003: AppleScript injection prevention** - Input validation for keystroke injection
- **HIGH-001: Update manifest security** - HTTPS enforcement + checksum validation
- **HIGH-002: Race condition protection** - Strengthened foreground window verification

### Technical Details
- Added 250+ lines of cache management and recovery logic
- Enhanced security in update system
- All 90 unit tests passing (increased to 91 in v1.0.2)
- See `SECURITY_AUDIT_REPORT.md` for complete security audit

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
