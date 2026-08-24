# Create GitHub Release - Manual Instructions

The tag `v1.0.0` has been pushed to GitHub. Now create the release manually:

## Step 1: Go to Releases Page

Open in browser: https://github.com/Arun270647/claude-permissions-app/releases/new?tag=v1.0.0

## Step 2: Fill in Release Details

### Release Title
```
v1.0.0 - Initial Release
```

### Release Notes (copy-paste this)

```markdown
## 🎉 First Public Release

**Claude Permission Assistant** automatically approves Claude Code permission prompts so you can focus on coding without interruptions.

### ✨ Features

- ✅ **Multi-terminal support** - Monitor multiple terminals simultaneously
- ✅ **System tray integration** - Runs quietly in the background
- ✅ **Real-time statistics** - See how many prompts were auto-approved
- ✅ **Smart detection** - Regex-based parser identifies Claude Code prompts
- ✅ **Safety features** - Global lock, 5-second cooldown, verification
- ✅ **Cross-platform ready** - Windows support (macOS coming soon)

### 📦 Downloads

**Windows (10/11):**
- Download: `ClaudePermissionAssistant-Windows-v1.0.0.exe` (70MB)
- Self-contained executable (no .NET SDK required)
- Single file, no installation needed

**macOS:**
- Coming soon (use `build-macos.sh` to build from source)

### 🚀 Quick Start

1. Download the Windows exe
2. Run it (click "More info" → "Run anyway" if SmartScreen appears)
3. App opens in system tray
4. Click tray icon → Add Terminal
5. Start using Claude Code - prompts are now auto-approved!

### ⚠️ Known Issues

- Windows SmartScreen warning (app is not code-signed)
- Dashboard stealing focus can reduce approval rate (minimize it after adding terminals)
- Only Terminal.app supported on macOS (iTerm2 support coming)

### 📖 Documentation

- [README](https://github.com/Arun270647/claude-permissions-app/blob/main/README.md) - Installation guide
- [Tech Stack](https://github.com/Arun270647/claude-permissions-app/blob/main/docs/TECH_STACK.md) - Complete technical details
- [Contributing](https://github.com/Arun270647/claude-permissions-app/blob/main/docs/CONTRIBUTING.md) - How to contribute
- [Development Workflow](https://github.com/Arun270647/claude-permissions-app/blob/main/docs/DEV_WORKFLOW.md) - Dev setup

### 🐛 Report Issues

Found a bug? [Open an issue](https://github.com/Arun270647/claude-permissions-app/issues/new?template=bug_report.md)

### 💡 Feature Requests

Have an idea? [Request a feature](https://github.com/Arun270647/claude-permissions-app/issues/new?template=feature_request.md)

---

**Full Changelog:** This is the first release!
```

## Step 3: Upload Windows Executable

1. Click "Attach binaries by dropping them here or selecting them"
2. Navigate to: `D:\projects\claude-permission app\publish\win-x64\`
3. Select `ClaudePermissionAssistant.exe` (70MB)
4. Wait for upload to complete

**Optional:** Rename the uploaded file to: `ClaudePermissionAssistant-Windows-v1.0.0.exe`

## Step 4: Publish Release

1. Ensure "Set as the latest release" is checked
2. Click **"Publish release"** button

## ✅ Done!

Your release will be live at: https://github.com/Arun270647/claude-permissions-app/releases/tag/v1.0.0

Users can now download the app directly from GitHub!

---

## Alternative: Using GitHub CLI (if installed later)

Install gh CLI: https://cli.github.com/

Then run:

```bash
gh release create v1.0.0 \
  --title "v1.0.0 - Initial Release" \
  --notes-file RELEASE_NOTES.md \
  publish/win-x64/ClaudePermissionAssistant.exe#ClaudePermissionAssistant-Windows-v1.0.0.exe
```

## Verify Release

After publishing, check:

1. Release page shows v1.0.0
2. Download link works
3. README.md installation links point to correct URL

Update README.md if needed:
```markdown
[ClaudePermissionAssistant-Windows-v1.0.0.exe](https://github.com/Arun270647/claude-permissions-app/releases/download/v1.0.0/ClaudePermissionAssistant-Windows-v1.0.0.exe)
```
