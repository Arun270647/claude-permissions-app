# Claude Code Instructions

This file contains instructions for Claude Code when working on this project.

## Project Overview

**Claude Prompter** (formerly Claude Permission Assistant) is a cross-platform desktop automation tool that automatically approves Claude Code permission prompts. Built with C# and .NET 8.0, it supports Windows (WPF) and macOS (Avalonia).

## Development Workflow

### IMPORTANT: Always Use Dev Branch

**⚠️ CRITICAL RULE:** All development work MUST be done on the `dev` branch. Never commit directly to `main`.

```bash
# Always work here
git checkout dev

# Make changes, commit, push
git add .
git commit -m "Your changes"
git push origin dev

# ONLY merge to main after manual user confirmation
# User will explicitly say: "merge to main" or "push to main"
```

### Branch Strategy

- **`dev`** - Active development branch (all work happens here)
- **`main`** - Production-ready code (only updated after testing + user approval)

**Workflow:**
1. Make all changes on `dev` branch
2. Push to `dev` for CI/CD testing
3. User tests locally
4. User gives explicit confirmation
5. Only then merge `dev` → `main`

### Merging to Main (Only After User Approval)

```bash
# User must explicitly approve with phrases like:
# "merge to main", "push to main", "ready for production", etc.

git checkout main
git pull origin main
git merge dev
git push origin main
```

**Never assume it's ready for main. Always wait for explicit user confirmation.**

## Repository Structure

```
src/
├── Shared/
│   └── ClaudePermissionAssistant.Core/      # Cross-platform (parser, models, auto-update)
├── Windows/
│   ├── ClaudePermissionAssistant.App/       # WPF UI
│   └── ClaudePermissionAssistant.Automation/  # Windows UI Automation + SendInput
└── macOS/
    ├── ClaudePermissionAssistant.MacApp/    # Avalonia UI
    └── ClaudePermissionAssistant.MacOS/     # AppleScript + System Events

tests/
└── ClaudePermissionAssistant.Automation.Tests/  # 91 unit tests

docs/                                         # Documentation
.github/workflows/                            # CI/CD
```

## GitHub Actions / CI/CD

### Build Triggers

**Windows Build** (`build-windows.yml`):
- Triggers on push to `dev` or `main`
- Triggers on PR to `main`
- Only if files in `src/Windows/**`, `src/Shared/**`, or workflow file change

**macOS Build** (`build-macos.yml`):
- Triggers on push to `dev` or `main`
- Triggers on PR to `main`
- Only if files in `src/macOS/**`, `src/Shared/**`, or workflow file change

**Why Actions Might Not Show:**
- Path filters: Only code changes trigger builds, not docs
- Branch filters: Must push to `dev` or `main`
- Workflow syntax errors

### Verifying Actions Run

After pushing to `dev`, check:
1. Go to: https://github.com/Arun270647/claude-permissions-app/actions
2. Should see workflow runs for commits
3. Green ✅ = success, Red ❌ = failed

If no actions appear: Check path filters (documentation changes don't trigger builds).

## Building Locally

### Windows
```bash
# Quick build
dotnet build src/Windows/ClaudePermissionAssistant.App/ClaudePermissionAssistant.App.csproj --configuration Release

# Full publish
rebuild.bat
# Output: publish/win-x64/ClaudePrompter.exe
```

### macOS
```bash
# Quick build
dotnet build src/macOS/ClaudePermissionAssistant.MacApp/ClaudePermissionAssistant.MacApp.csproj --configuration Release

# Full publish (creates .dmg)
chmod +x build-macos.sh
./build-macos.sh
# Output: releases/ClaudePrompter-macOS-{arch}-v{version}.dmg
```

### Tests
```bash
dotnet test
# All 91 tests should pass
```

## Current Status

**Version:** v1.0.1 (released)  
**Stage:** Security hardening phase  
**Recent Work:** Fixed critical security vulnerabilities (CRIT-001 through MED-004)  
**Next Release:** v1.0.2 (code signing, notarization)

## Security Considerations

### Recent Security Fixes (2026-08-25)
- ✅ CRIT-001: Added SHA-256 checksum verification for updates
- ✅ CRIT-002: Fixed command injection in update scripts
- ✅ CRIT-003: Fixed AppleScript injection vulnerability
- ✅ HIGH-001: Enforced HTTPS for update manifests
- ✅ HIGH-002: Strengthened race condition protection
- ✅ MED-001 through MED-004: Various security improvements

### When Making Changes
- Never skip security checks (checksums, input validation)
- Properly escape shell scripts (batch, bash, AppleScript)
- Validate all external input (update URLs, file paths)
- Use cryptographic hashing (SHA256) not `GetHashCode()`
- Log security events without exposing sensitive data

## Code Style

### C# Conventions
- C# 12 with .NET 8.0
- Nullable reference types enabled (`#nullable enable`)
- Use `var` for obvious types
- Modern patterns (records, pattern matching, etc.)
- Follow Microsoft C# conventions

### Testing
- Write tests for new functionality
- Maintain 91 test suite pass rate
- Test both happy path and edge cases

## Common Tasks

### Adding a New Feature

1. Ensure on `dev` branch: `git checkout dev`
2. Determine scope:
   - Windows-only → Edit `src/Windows/`
   - macOS-only → Edit `src/macOS/`
   - Cross-platform → Edit `src/Shared/`
3. Write tests if applicable
4. Build locally: `dotnet build`
5. Run tests: `dotnet test`
6. Commit and push to `dev`
7. Wait for CI/CD to pass
8. Test locally
9. **Wait for user to say "merge to main"**

### Fixing a Bug

Same process as adding a feature - always on `dev` branch first.

### Security Fixes

1. Work on `dev` branch
2. Document the vulnerability in commit message
3. Test thoroughly
4. Get CI/CD green
5. User tests locally
6. User approves merge to `main`

## Release Process

Releases are cut from `main` (after merging from `dev`):

1. User approves merge: `dev` → `main`
2. Update version in manifests
3. Create git tag: `git tag v1.0.x`
4. Push tag: `git push origin v1.0.x`
5. GitHub Actions creates release with binaries

## Important Files

- `latest-windows.json` - Windows update manifest
- `latest-macos-arm64.json` - macOS Apple Silicon update manifest
- `latest-macos-x64.json` - macOS Intel update manifest
- `SECURITY_AUDIT_REPORT.md` - Complete security audit findings
- `rebuild.bat` - Windows build script
- `build-macos.sh` - macOS build script

## Contact & Issues

- Repository: https://github.com/Arun270647/claude-permissions-app
- Issues: https://github.com/Arun270647/claude-permissions-app/issues
- Website: https://cpa-web-swart.vercel.app/

## Remember

1. ✅ Always work on `dev` branch
2. ✅ Push to `dev` for testing
3. ✅ Wait for explicit user approval before merging to `main`
4. ✅ Never commit directly to `main`
5. ✅ Check GitHub Actions after every push
6. ✅ Security first - validate all inputs

---

**Current Branch:** Should always be `dev` during development  
**Production Branch:** `main` (only updated after approval)
