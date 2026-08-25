# Branching Strategy

This project uses a **single-branch strategy** with folder-based organization for platform-specific code.

## Branch Structure

```
main        ← All development happens here
```

That's it! One branch, simple workflow.

## Folder Structure

Platform-specific code is organized in folders:

```
src/
├── Shared/
│   └── ClaudePermissionAssistant.Core/     # Cross-platform parser, models, auto-update
├── Windows/
│   ├── ClaudePermissionAssistant.App/      # WPF UI
│   └── ClaudePermissionAssistant.Automation/  # Windows UI Automation
└── macOS/
    ├── ClaudePermissionAssistant.MacApp/   # Avalonia UI
    └── ClaudePermissionAssistant.MacOS/    # AppleScript automation
```

## Why Single Branch?

**Previously:** Separate `windows`, `macos` branches
- ❌ Complex to maintain
- ❌ Hard to sync shared code
- ❌ Merge conflicts
- ❌ Contributors confused about which branch to use

**Now:** Single `main` branch
- ✅ Simple workflow
- ✅ Shared code immediately benefits both platforms
- ✅ No branch syncing needed
- ✅ Easier for contributors
- ✅ CI/CD handles platform builds automatically

## Workflow

### Making Changes

All changes go directly to `main`:

```bash
git checkout main
git pull origin main

# Make your changes
# - Windows-specific → edit src/Windows/
# - macOS-specific → edit src/macOS/
# - Shared code → edit src/Shared/

git add .
git commit -m "Your descriptive commit message"
git push origin main
```

### For Contributors

1. Fork the repository
2. Create a feature branch from `main`:
   ```bash
   git checkout -b feature/my-feature
   ```
3. Make your changes
4. Run tests: `dotnet test`
5. Push to your fork
6. Open Pull Request to `main`

## Continuous Integration

GitHub Actions automatically builds when code changes:

**build-windows.yml** triggers on:
- `src/Windows/**` changes
- `src/Shared/**` changes
- `.github/workflows/build-windows.yml` changes

**build-macos.yml** triggers on:
- `src/macOS/**` changes
- `src/Shared/**` changes
- `.github/workflows/build-macos.yml` changes

**Smart:** Only the affected platform builds!

## Release Process

Releases are cut from `main`:

```bash
# Tag the release
git tag v1.0.2
git push origin v1.0.2

# GitHub Actions automatically:
# - Builds Windows .exe
# - Builds macOS .dmg (arm64 + x64)
# - Creates GitHub Release
# - Uploads binaries
```

## Examples

### Example 1: Add Windows-only feature

```bash
git checkout main
git pull origin main

# Edit Windows code
code src/Windows/ClaudePermissionAssistant.App/DashboardWindow.xaml.cs

git add .
git commit -m "Add notification sound setting to Windows app"
git push origin main

# GitHub Actions builds only Windows
```

### Example 2: Add macOS-only feature

```bash
git checkout main
git pull origin main

# Edit macOS code
code src/macOS/ClaudePermissionAssistant.MacOS/Services/MacOSPromptExecutor.cs

git add .
git commit -m "Add iTerm2 support for macOS"
git push origin main

# GitHub Actions builds only macOS
```

### Example 3: Fix bug in shared code

```bash
git checkout main
git pull origin main

# Edit shared code
code src/Shared/ClaudePermissionAssistant.Core/Services/ClaudePromptParserSimple.cs

git add .
git commit -m "Fix regex pattern for multiline prompts"
git push origin main

# GitHub Actions builds BOTH platforms (shared code affects both)
```

### Example 4: Security fix affecting all platforms

```bash
git checkout main
git pull origin main

# Edit shared auto-update service
code src/Shared/ClaudePermissionAssistant.Core/Services/AutoUpdateService.cs

# Edit Windows executor
code src/Windows/ClaudePermissionAssistant.Automation/Services/ClaudePermissionPromptExecutorHardened.cs

# Edit macOS executor
code src/macOS/ClaudePermissionAssistant.MacOS/Services/MacOSPromptExecutor.cs

git add .
git commit -m "Fix critical security vulnerabilities (CRIT-001 through MED-004)"
git push origin main

# GitHub Actions builds BOTH platforms
```

## Branch Protection

### `main` branch protection
- ✅ Status checks must pass (CI builds)
- ❌ Force push: **NEVER**
- ❌ Allow deletions: **NEVER**

Optional (for teams):
- ✅ Require pull request before merging
- ✅ Require code review

## FAQ

**Q: What happened to the `windows` and `macos` branches?**  
A: They were archived. The folder structure provides the same organization without the complexity.

**Q: How do I work on just Windows code?**  
A: Edit files in `src/Windows/` folder. CI will only build Windows.

**Q: What if I change shared code?**  
A: CI builds both platforms automatically.

**Q: Can I create feature branches?**  
A: Yes! Create branches from `main`, work on them, then PR back to `main`.

**Q: What about the website?**  
A: The website lives in a separate repository: [cpa-web](https://github.com/Arun270647/cpa-web)

## Migration Notes

**If you cloned before 2026-08-25:**

Old way (separate branches):
```bash
git checkout windows  # Work here for Windows
git checkout macos    # Work here for macOS
git merge main        # Sync shared code
```

New way (single branch):
```bash
git checkout main     # Everything happens here
# Edit src/Windows/ or src/macOS/ as needed
```

---

**Remember:** Keep it simple. One branch, organized folders, automatic builds.
