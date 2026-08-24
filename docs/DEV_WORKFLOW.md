# Development Workflow

This document explains how to work on the Claude Permission Assistant codebase efficiently.

## Quick Start

### First-Time Setup

1. **Install Prerequisites**
   ```bash
   # Verify .NET SDK is installed
   dotnet --version
   # Should show 8.0.x or later
   ```

2. **Initial Build**
   ```bash
   rebuild.bat
   ```
   This creates: `publish\win-x64\ClaudePermissionAssistant.exe`

3. **Run the app**
   ```bash
   publish\win-x64\ClaudePermissionAssistant.exe
   ```

## Development Mode (Auto-Rebuild on Save)

Instead of manually rebuilding after every code change, use the dev watch script:

### Option 1: Double-click (Easy)
```
dev-watch.bat
```
Double-click this file in File Explorer.

### Option 2: Command Line
```bash
# Batch file
dev-watch.bat

# Or PowerShell directly
powershell -ExecutionPolicy Bypass -File dev-watch.ps1
```

### What It Does

1. **Initial build** - Runs `rebuild.bat` and starts the app
2. **Watches for changes** - Monitors all `.cs`, `.xaml`, `.csproj` files in `src/`
3. **Auto-rebuilds** - When you save a file:
   - Stops the running app
   - Rebuilds the project
   - Restarts the app automatically
4. **Debouncing** - Waits 1 second after the last change to avoid rebuilding on every keystroke

### Example Workflow

```
1. Run dev-watch.bat
2. App opens in system tray
3. Add a terminal to monitor
4. Edit src/Windows/ClaudePermissionAssistant.App/DashboardWindow.xaml.cs
5. Save the file (Ctrl+S)
6. Script detects change → rebuilds → restarts app
7. Test your changes
8. Repeat steps 4-7
```

### Output

```
========================================
Claude Permission Assistant - Dev Watch
========================================

Starting initial build...

[15:30:45] ========== REBUILDING ==========
[15:30:45] Stopping running app...
Cleaning...
Building...
Publishing...
[15:30:52] Build successful! Starting app...
[15:30:52] App started. Watching for changes...
[15:30:52] ===============================

Watching for changes in: D:\projects\claude-permission app\src
Press Ctrl+C to stop

[15:31:05] Changed: D:\...\DashboardWindow.xaml.cs

[15:31:06] ========== REBUILDING ==========
[15:31:06] Stopping running app...
...
```

## Manual Build Commands

### Full Rebuild (Production)
```bash
rebuild.bat
```
- Cleans everything
- Builds all projects
- Publishes self-contained exe to `publish\win-x64\`

### Quick Debug Build
```bash
dotnet build
```
- Builds Debug configuration
- Outputs to `bin\Debug\net8.0-windows\`
- Faster but not self-contained

### Run Tests
```bash
dotnet test
```
- Runs all 91 tests
- Must pass before committing

### Restore Dependencies
```bash
dotnet restore
```
- Downloads NuGet packages
- Rarely needed (automatic on build)

## Project Structure

```
src/
├── Shared/
│   └── ClaudePermissionAssistant.Core/     # Change → Affects Windows + macOS
├── Windows/
│   ├── ClaudePermissionAssistant.App/      # Change → Affects Windows app only
│   └── ClaudePermissionAssistant.Automation/  # Change → Affects Windows only
└── macOS/
    ├── ClaudePermissionAssistant.MacApp/   # Change → Affects macOS only
    └── ClaudePermissionAssistant.MacOS/    # Change → Affects macOS only
```

**Important:**
- Changes in `Shared/Core` require testing on both Windows and macOS
- Changes in `Windows/` only affect Windows build
- Changes in `macOS/` only affect macOS build

## Common Tasks

### Add a New File

1. Create the file in appropriate folder
2. Save it
3. dev-watch.bat will detect and rebuild automatically

### Change UI (XAML)

1. Edit `.xaml` or `.xaml.cs` file
2. Save
3. Auto-rebuild happens
4. New UI appears instantly

### Fix a Bug

1. Run dev-watch.bat
2. Add terminal to app
3. Reproduce the bug
4. Edit code
5. Save → Auto-rebuild
6. Test the fix immediately

### Debug with Logs

App logs to: `%APPDATA%\ClaudePermissionAssistant\logs\`

```bash
# Watch logs in real-time (PowerShell)
Get-Content "$env:APPDATA\ClaudePermissionAssistant\logs\app-20260824.log" -Wait -Tail 20
```

### Before Committing

```bash
# 1. Stop dev-watch (Ctrl+C)

# 2. Run full rebuild
rebuild.bat

# 3. Run all tests
dotnet test

# 4. Test the published exe
publish\win-x64\ClaudePermissionAssistant.exe

# 5. If everything works, commit
git add .
git commit -m "Your commit message"
```

## Troubleshooting

### "App won't stop" when rebuilding

**Cause:** App crashed but process is still running

**Fix:**
```bash
taskkill /F /IM ClaudePermissionAssistant.exe
```

### "Build failed" on dev-watch

**Cause:** Syntax error in code

**Fix:**
1. Check the build output in console
2. Fix the error
3. Save again → Auto-rebuild retries

### "File is locked" during build

**Cause:** Antivirus or previous process holding file

**Fix:**
```bash
# Stop dev-watch (Ctrl+C)
taskkill /F /IM ClaudePermissionAssistant.exe
# Restart dev-watch
dev-watch.bat
```

### Dev-watch not detecting changes

**Cause:** File saved outside `src/` folder or wrong extension

**Fix:** Only these trigger rebuild:
- `.cs` (C# code)
- `.xaml` (UI markup)
- `.csproj` (project files)
- `.xml` (config files)

### PowerShell execution policy error

**Error:** "cannot be loaded because running scripts is disabled"

**Fix:**
```powershell
# Run PowerShell as Administrator
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

Or use the batch file: `dev-watch.bat` (sets policy automatically)

## Tips

### Multiple Terminals

You can run multiple dev-watch instances if working on Windows + macOS simultaneously:
- Terminal 1: `dev-watch.bat` (Windows watch)
- Terminal 2: `./build-macos.sh` (macOS manual build)

### Fast Iteration

For fastest development cycle:
1. Keep dev-watch running
2. Make small, focused changes
3. Save frequently
4. Test immediately after each rebuild

### Disable Auto-Rebuild Temporarily

If you're making many changes and don't want constant rebuilds:
- Stop dev-watch (Ctrl+C)
- Make all your changes
- Run `rebuild.bat` manually when ready
- Test
- Restart dev-watch

### Code Editor Setup

**VS Code:**
- Auto-save: File → Auto Save (enables on-type rebuild)
- Terminal: Ctrl+` to show integrated terminal for dev-watch

**Visual Studio:**
- Build on Save: Tools → Options → Projects and Solutions → Build and Run → "On Run, when projects are out of date: Always build"

**Rider:**
- Auto-save is on by default
- Run dev-watch.bat in external terminal

---

## Summary

**For rapid development:**
```bash
dev-watch.bat    # Start this, keep it running, code → save → auto-rebuild
```

**For production build:**
```bash
rebuild.bat      # Full clean rebuild, run before committing
```

**For testing:**
```bash
dotnet test      # Run all tests, must pass
```

Happy coding! 🚀
