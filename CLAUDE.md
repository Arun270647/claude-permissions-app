# Claude Code Project Instructions

> **⚠️ CRITICAL:** Read this entire file before making any changes. These are mandatory rules that MUST be followed in every session.

---

## 🔴 CRITICAL RULES - NEVER BREAK THESE

### Rule #1: ALWAYS Work on Dev Branch

```bash
# ✅ CORRECT - Always do this
git checkout dev

# ❌ WRONG - Never commit directly to main
git checkout main  # Only for merging after approval
```

**ABSOLUTE REQUIREMENTS:**
- ✅ ALL development work happens on `dev` branch
- ✅ ALL commits go to `dev` branch
- ✅ ALL pushes go to `dev` branch  
- ❌ NEVER commit directly to `main`
- ❌ NEVER push directly to `main`
- ❌ NEVER assume changes are ready for `main`

### Rule #2: Merge to Main ONLY After Explicit User Approval

**User MUST explicitly say one of these phrases:**
- "merge to main"
- "push to main"
- "ready for production"
- "deploy to main"
- "merge dev to main"

**If user has NOT said these phrases → DO NOT MERGE TO MAIN**

**Merge process (only after approval):**
```bash
git checkout main
git pull origin main
git merge dev --no-ff -m "Merge dev: [summary of changes]"
git push origin main
git checkout dev  # Return to dev immediately
```

### Rule #3: Never Assume, Always Ask

**If unclear whether to merge to main:**
- ❌ DO NOT merge
- ✅ Ask: "Would you like me to merge these changes to main?"

**If user says "push":**
- Default: Push to `dev`
- Ask: "Push to dev (testing) or main (production)?"

### Rule #4: Commit Locally, Push ONLY After Approval

**CRITICAL WORKFLOW:**
- ✅ Make changes on `dev` branch
- ✅ Run tests and verify everything works
- ✅ `git add` and `git commit` locally
- ⚠️ **STOP! DO NOT PUSH!**
- ✅ Show user what was changed
- ✅ Wait for user to say "push to dev" or "looks good"
- ✅ ONLY THEN: `git push origin dev`

**Why this matters:**
- Multiple small changes shouldn't create multiple versions
- User reviews changes before they go to remote
- Prevents accidental version increments
- Allows grouping related changes together

### Rule #5: Versions Created ONLY When Dev Merges to Main

**VERSION MANAGEMENT:**
- ❌ DO NOT create new version for every commit
- ❌ DO NOT increment version when pushing to dev
- ✅ Accumulate changes on dev branch
- ✅ Version numbers change ONLY when dev merges to main
- ✅ Main branch push triggers auto-release workflow
- ✅ Each main merge = one new version

**Current versioning:**
- Latest released: v1.0.1 (on main branch)
- Next version: v1.0.2 (when dev merges to main next time)

**Example workflow:**
```
Change 1 → commit to dev (no version change)
Change 2 → commit to dev (no version change)
Change 3 → commit to dev (no version change)
User approves → push to dev (still no version change)
User says "merge to main" → dev merges to main → v1.0.4 created!
```

---

## 📋 Project Overview

**Project Name:** Claude Prompter (formerly Claude Permission Assistant)  
**Purpose:** Desktop automation tool that auto-approves Claude Code permission prompts  
**Platforms:** Windows (WPF) and macOS (Avalonia)  
**Tech Stack:** C# 12, .NET 8.0  
**Current Version:** v1.0.1 (released on main)  
**Next Version:** v1.0.2 (when dev merges to main)  
**Dev Branch Status:** Contains unreleased changes (critical security fix)

**What it does:**
Monitors terminal windows (CMD, PowerShell, Terminal.app) and automatically selects "Yes, allow from this project" when Claude Code asks for permissions, eliminating manual approval interruptions.

---

## 📁 Repository Structure

```
claude-permissions-app/
├── src/
│   ├── Shared/
│   │   └── ClaudePermissionAssistant.Core/     # Cross-platform code
│   │       ├── Models/                         # Data models
│   │       ├── Services/                       # Business logic
│   │       │   ├── ClaudePromptParserSimple.cs # Regex-based parser
│   │       │   └── AutoUpdateService.cs        # Auto-update system
│   │       └── Interfaces/                     # Contracts
│   ├── Windows/
│   │   ├── ClaudePermissionAssistant.App/      # WPF UI
│   │   │   ├── DashboardWindow.xaml            # Main window
│   │   │   ├── Services/                       # Windows services
│   │   │   └── ViewModels/                     # MVVM pattern
│   │   └── ClaudePermissionAssistant.Automation/ # UI Automation
│   │       ├── Services/
│   │       │   ├── ClaudePermissionPromptExecutorHardened.cs
│   │       │   ├── WindowsTerminalMonitor.cs
│   │       │   └── KeyboardInjector.cs         # SendInput API
│   │       └── PInvoke/                        # Windows API calls
│   └── macOS/
│       ├── ClaudePermissionAssistant.MacApp/   # Avalonia UI
│       │   ├── Views/                          # XAML views
│       │   ├── ViewModels/                     # MVVM pattern
│       │   └── Services/                       # macOS services
│       └── ClaudePermissionAssistant.MacOS/    # AppleScript automation
│           └── Services/
│               └── MacOSPromptExecutor.cs      # AppleScript + System Events
├── tests/
│   └── ClaudePermissionAssistant.Automation.Tests/  # 91 unit tests
├── docs/
│   ├── internal/                               # Developer docs
│   ├── windows/                                # Windows-specific docs
│   └── macos/                                  # macOS-specific docs
├── .github/workflows/
│   ├── build-windows.yml                       # Windows CI/CD
│   ├── build-macos.yml                         # macOS CI/CD (x64 + arm64)
│   └── release.yml                             # Release automation
├── CLAUDE.md                                   # ← You are here
├── README.md                                   # User-facing documentation
├── SECURITY_AUDIT_REPORT.md                    # Security audit findings
└── CHANGELOG.md                                # Version history
```

---

## 🌿 Branch Strategy (MANDATORY)

### Branches

```
dev   ← 🔥 ACTIVE DEVELOPMENT (work here 99% of the time)
 ↓
main  ← 🔒 PRODUCTION (merge only after user approval)
```

**Additional branches:**
- `web` - Website (moved to separate repo, archived)

### Branch Descriptions

**`dev` Branch:**
- Purpose: Active development
- Who commits: You (always)
- When to use: All feature work, bug fixes, changes
- CI/CD: Triggers builds on push
- Testing: User tests from this branch

**`main` Branch:**
- Purpose: Production-ready code
- Who commits: Only after explicit user approval
- When to use: After dev is tested and approved
- CI/CD: Triggers builds on push
- Releases: All version tags from main

### Workflow Visualization

```
Day 1:  dev ──┬─ Feature A ──┬─ Bug fix ──┬─ [push to dev]
              │              │            │
              └─ Build ✅    └─ Test ✅   └─ User tests locally

Day 2:  User says: "merge to main"
              ↓
        main ─────────────────┬─ [merge from dev]
                              │
                              └─ Production ready ✅
```

---

## 🛠️ Development Workflow

### Starting Work (Every Session)

```bash
# 1. Check you're on dev
git branch
# Should show: * dev

# If not on dev:
git checkout dev

# 2. Get latest changes
git pull origin dev

# 3. Verify branch
git status
# Should show: On branch dev
```

### Making Changes

```bash
# 1. Ensure on dev branch
git checkout dev

# 2. Make your changes
# - Windows-specific → edit src/Windows/
# - macOS-specific → edit src/macOS/
# - Cross-platform → edit src/Shared/
# - Documentation → edit docs/ or README.md

# 3. MANDATORY TESTING (for all code changes and bug fixes)
#    MUST complete ALL steps before committing:

# 3a. Build the application
./scripts/rebuild.bat  # Windows
# OR
./scripts/build-macos.sh  # macOS

# 3b. Run all unit tests
dotnet test
# MUST show: All 91 tests passing

# 3c. E2E testing (for bug fixes and features)
# - Start the built application
# - Test the specific fix/feature manually
# - Test common workflows (add terminal, detect prompts, auto-approve)
# - Verify statistics are accurate
# - Check logs for errors

# 3d. Document the fix (for bug fixes)
# - Create/update docs/internal/[FIX_NAME].md with:
#   - Problem description
#   - Root cause analysis
#   - Solution implemented
#   - Testing instructions
#   - Verification steps

# 3e. Update CHANGELOG.md
# - Add entry under "Unreleased" or next version
# - Include fix description and impact
# - Reference issue numbers if applicable

# 4. Stage and commit locally (only after all tests pass)
git add .
git commit -m "Descriptive commit message"

# 5. STOP! DO NOT PUSH YET!
# Changes must be reviewed and approved by user BEFORE pushing to dev

# 6. Wait for user approval
# User must explicitly say: "push to dev" or "looks good, push it"

# 7. Push to dev ONLY after approval
git push origin dev
# This triggers CI/CD builds but does NOT create a release

# 6. Check GitHub Actions
# https://github.com/Arun270647/claude-permissions-app/actions
# Verify builds pass (green ✅)

# 7. User tests locally

# 8. Wait for user to approve merge to main
```

### Merging to Main (Only After Approval)

**Prerequisites:**
- ✅ User explicitly approved (said "merge to main" or similar)
- ✅ All commits pushed to dev
- ✅ GitHub Actions passing (green)
- ✅ Local testing completed

```bash
# 1. Switch to main
git checkout main
git pull origin main

# 2. Merge dev
git merge dev --no-ff -m "Merge dev: [brief summary]"

# 3. Push to main
git push origin main

# 4. If this is a release, tag it
git tag v1.0.x
git push origin v1.0.x

# 5. Return to dev immediately
git checkout dev
```

---

## 🏗️ Building & Testing

### Quick Build (Verify Compilation)

**Windows:**
```bash
dotnet build src/Windows/ClaudePermissionAssistant.App/ClaudePermissionAssistant.App.csproj --configuration Release
```

**macOS:**
```bash
dotnet build src/macOS/ClaudePermissionAssistant.MacApp/ClaudePermissionAssistant.MacApp.csproj --configuration Release
```

**Both Platforms:**
```bash
dotnet build --configuration Release
```

### Full Build (Production Binaries)

**Windows:**
```bash
# Run rebuild script
rebuild.bat

# Output: publish/win-x64/ClaudePrompter.exe
# Size: ~70MB (self-contained, single-file)
```

**macOS:**
```bash
# Make script executable (first time only)
chmod +x build-macos.sh

# Build DMG files
./build-macos.sh

# Output:
# - releases/ClaudePrompter-macOS-arm64-v1.0.x.dmg
# - releases/ClaudePrompter-macOS-x64-v1.0.x.dmg
```

### Running Tests

```bash
# Run all tests (91 tests)
dotnet test

# Expected output: All 91 tests pass
# If any fail, investigate before committing
```

### Testing Locally

**After building, test the actual application:**
1. Run the built executable
2. Add a terminal to monitor
3. Start Claude Code in that terminal
4. Verify prompts are auto-approved
5. Check statistics (detected, approved, failed counts)

---

## 🤖 GitHub Actions (CI/CD)

### When Builds Trigger

**build-windows.yml** runs when:
- Push to `dev` or `main` branches
- Pull request to `main` branch
- Files changed in:
  - `src/Windows/**`
  - `src/Shared/**`
  - `.github/workflows/build-windows.yml`

**build-macos.yml** runs when:
- Push to `dev` or `main` branches
- Pull request to `main` branch
- Files changed in:
  - `src/macOS/**`
  - `src/Shared/**`
  - `.github/workflows/build-macos.yml`

### Why Actions Might Not Appear

**Path Filters:**
- Documentation changes (README, docs/) don't trigger builds
- Only code changes trigger builds
- This is intentional (saves CI minutes)

**Branch Filters:**
- Must push to `dev` or `main`
- Other branches don't trigger builds

**To Force a Build:**
Change a code file, even trivially:
```bash
# Add a comment to a shared file
echo "// Trigger CI" >> src/Shared/ClaudePermissionAssistant.Core/Models/DetectedPrompt.cs
git add .
git commit -m "Trigger CI build"
git push origin dev
```

### Checking Build Status

1. **Go to Actions page:**
   https://github.com/Arun270647/claude-permissions-app/actions

2. **Look for your commit:**
   - Should see workflow runs with your commit message
   - Yellow circle ⭕ = Running
   - Green check ✅ = Success
   - Red X ❌ = Failed

3. **If failed:**
   - Click on the failed workflow
   - View logs to see error
   - Fix the issue on `dev` branch
   - Push again

### Build Artifacts

After successful build:
- Windows: `ClaudePermissionAssistant.exe` (~70MB)
- macOS: `ClaudePermissionAssistant` binaries (arm64 + x64)
- Available to download from Actions page
- Retained for 90 days

---

## 🔒 Security Guidelines (CRITICAL)

### Recent Security Fixes (v1.0.1)

**CRIT-001: Update Signature Verification**
- ✅ Added SHA-256 checksum verification
- ✅ Enforce HTTPS for update URLs
- ✅ Validate checksums before installing

**CRIT-002: Command Injection Prevention**
- ✅ Properly escape batch script paths (Windows)
- ✅ Properly escape bash script paths (macOS)
- ✅ Validate paths for shell metacharacters

**CRIT-003: AppleScript Injection Prevention**
- ✅ Input validation for keystroke injection
- ✅ Proper escaping in AppleScript generation

**HIGH-001: Update Manifest Security**
- ✅ HTTPS enforcement
- ✅ Checksum validation from manifest

**HIGH-002: Race Condition Protection**
- ✅ Strengthened foreground window verification
- ✅ Global execution lock

### Security Requirements When Making Changes

**1. Input Validation:**
```csharp
// ✅ GOOD - Validate all external input
if (!updateInfo.Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
{
    throw new SecurityException("Update URLs must use HTTPS");
}

// ❌ BAD - Trusting external input
var url = updateInfo.Url;
```

**2. Shell Command Escaping:**
```csharp
// ✅ GOOD - Properly escape paths
var escapedPath = EscapeBashPath(filePath);
var script = $"cp {escapedPath} /dest/";

// ❌ BAD - Direct interpolation
var script = $"cp {filePath} /dest/";  // Command injection risk
```

**3. Cryptographic Operations:**
```csharp
// ✅ GOOD - Use SHA256 for integrity
using var sha256 = SHA256.Create();
var hash = sha256.ComputeHash(stream);

// ❌ BAD - Use GetHashCode() (not cryptographic)
var hash = filePath.GetHashCode();
```

**4. Sensitive Data in Logs:**
```csharp
// ✅ GOOD - Sanitize before logging
_logger.LogInfo($"Processing file: {SanitizePath(path)}");

// ❌ BAD - Raw logging (might expose secrets)
_logger.LogInfo($"Terminal text: {terminalText}");
```

### Security Checklist Before Committing

- [ ] All external input validated
- [ ] Shell commands properly escaped
- [ ] Cryptographic operations use strong algorithms (SHA256+)
- [ ] No sensitive data logged
- [ ] Update manifests include checksums
- [ ] File paths validated for malicious content
- [ ] Foreground window verified before keyboard injection

---

## 📝 Code Style & Conventions

### C# Style

```csharp
// Use modern C# 12 features
public record PermissionRequest
{
    public required string ToolName { get; init; }
    public required PermissionOption[] Options { get; init; }
}

// Nullable reference types enabled
#nullable enable
public string? OptionalValue { get; set; }

// Use var for obvious types
var detector = new ClaudePromptDetector();

// Pattern matching
if (prompt is { IsValid: true, Request.BestApprovalOptionNumber: not null })
{
    // Handle valid prompt
}
```

### Naming Conventions

- Classes: `PascalCase` (e.g., `ClaudePromptDetector`)
- Methods: `PascalCase` (e.g., `DetectPrompt()`)
- Private fields: `_camelCase` (e.g., `_executionGate`)
- Constants: `PascalCase` (e.g., `DuplicateCooldown`)
- Interfaces: `IPascalCase` (e.g., `IClaudePromptDetector`)

### Documentation

```csharp
/// <summary>
/// Brief description of what this does
/// </summary>
/// <param name="prompt">Description of parameter</param>
/// <returns>Description of return value</returns>
public ExecutionResult Execute(DetectedPrompt prompt)
{
    // Implementation
}
```

### Testing

- Write tests for new features
- Maintain 91 test pass rate
- Test happy path + edge cases
- Use descriptive test names:
  ```csharp
  [Fact]
  public void Parser_ValidPrompt_ReturnsDetectedPrompt()
  ```

---

## 🚀 Release Process

### Preparing for Release

1. **Ensure all changes on dev:**
   ```bash
   git checkout dev
   git status  # Should be clean
   ```

2. **Update version numbers:**
   - `latest-windows.json`
   - `latest-macos-arm64.json`
   - `latest-macos-x64.json`
   - Assembly versions in `.csproj` files

3. **Update CHANGELOG.md:**
   Add new version section with changes

4. **Test thoroughly:**
   - Build on both platforms
   - Run all tests
   - Test actual app functionality

5. **Get user approval:**
   User must explicitly say "ready for release"

### Creating Release

```bash
# 1. Merge dev to main (after approval)
git checkout main
git pull origin main
git merge dev --no-ff -m "Release v1.0.x"
git push origin main

# 2. Tag the release
git tag v1.0.x
git push origin v1.0.x

# 3. GitHub Actions creates release
# - Builds Windows .exe
# - Builds macOS .dmg (arm64 + x64)
# - Creates GitHub Release
# - Uploads binaries

# 4. Return to dev
git checkout dev
```

### Post-Release

1. Verify release on GitHub
2. Test download links
3. Update website (if needed)
4. Monitor for issues

---

## 🐛 Common Issues & Solutions

### Issue: GitHub Actions Not Showing

**Cause:** Documentation-only changes don't trigger builds  
**Solution:** Change a code file or check path filters

### Issue: Merge Conflicts

**Cause:** Main and dev diverged  
**Solution:**
```bash
git checkout dev
git pull origin main  # Merge main into dev
# Resolve conflicts
git add .
git commit -m "Merge main into dev"
git push origin dev
```

### Issue: Build Fails

**Cause:** Missing dependencies, syntax errors  
**Solution:**
1. Check build logs on GitHub Actions
2. Reproduce locally: `dotnet build`
3. Fix errors
4. Push to dev again

### Issue: Tests Fail

**Cause:** Code changes broke existing tests  
**Solution:**
1. Run tests locally: `dotnet test`
2. Fix the failing tests or code
3. Ensure all 91 tests pass
4. Commit and push

---

## 📞 Important Links

- **Repository:** https://github.com/Arun270647/claude-permissions-app
- **Issues:** https://github.com/Arun270647/claude-permissions-app/issues
- **Actions:** https://github.com/Arun270647/claude-permissions-app/actions
- **Releases:** https://github.com/Arun270647/claude-permissions-app/releases
- **Website:** https://cpa-web-swart.vercel.app/

---

## 🎯 Quick Reference Card

### Every Time You Start Work

```bash
git checkout dev              # Ensure on dev
git pull origin dev           # Get latest
```

### Every Time You Commit

```bash
git add .
git commit -m "Message"
git push origin dev           # Always push to dev
```

### Every Time User Approves

```bash
git checkout main
git merge dev --no-ff
git push origin main
git checkout dev              # Return to dev
```

### Every Time You're Unsure

**ASK THE USER.** Don't assume.

---

## ⚠️ REMEMBER THESE RULES

1. ✅ **ALWAYS work on `dev` branch**
2. ❌ **NEVER commit to `main` without approval**
3. ✅ **Push to `dev` triggers CI/CD**
4. ❌ **Don't assume changes are ready for production**
5. ✅ **Wait for explicit "merge to main" approval**
6. ✅ **Security first: validate all inputs**
7. ✅ **Test before pushing**
8. ✅ **Check GitHub Actions after every push**

---

## 📋 Pre-Commit Checklist

Before every commit, verify:

- [ ] On `dev` branch (`git branch` shows `* dev`)
- [ ] Code compiles (`dotnet build`)
- [ ] Tests pass (`dotnet test`)
- [ ] No security issues (input validation, escaping)
- [ ] Meaningful commit message
- [ ] Will push to `dev` (not `main`)

---

## 🎓 Session Initialization

**At the start of every session, you should:**

1. ✅ Check current branch: `git branch`
2. ✅ If not on `dev`, switch: `git checkout dev`
3. ✅ Pull latest changes: `git pull origin dev`
4. ✅ Confirm understanding: "I'm on the dev branch and ready to work."

---

**Last Updated:** 2026-08-25  
**Current Version:** v1.0.1  
**Next Version:** v1.0.2  
**Branch:** dev (active development)

---

**🔥 MOST IMPORTANT RULE:**

## NEVER COMMIT TO MAIN WITHOUT EXPLICIT USER APPROVAL

**Approval phrases:**
- "merge to main"
- "push to main"
- "ready for production"
- "deploy to main"

**If user hasn't said these → STAY ON DEV** ✅
