# Publish Checklist - Make Your First Public Release

Follow these steps to make the app publicly downloadable.

## Quick Path (Simplest - No Code Signing)

### Step 1: Prepare Repository

```bash
cd "D:\projects\claude-permission app"

# Initialize git if not already
git init
git add .
git commit -m "Initial commit: Claude Permission Assistant v1.0.0"

# Create GitHub repository
gh auth login  # If not already logged in
gh repo create claude-permission-assistant --public --source=. --remote=origin --push
```

### Step 2: Build Release Files

**On Windows (your current machine):**
```bash
rebuild.bat
# Output: publish/win-x64/ClaudePermissionAssistant.exe
```

**On Mac (requires access to a Mac):**
```bash
# Copy the project to Mac, then:
chmod +x build-macos.sh
./build-macos.sh
# Output: publish/osx-arm64/ClaudePermissionAssistant-macOS-arm64-v1.0.0
#         publish/osx-x64/ClaudePermissionAssistant-macOS-x64-v1.0.0
```

### Step 3: Create GitHub Release

```bash
# Tag the release
git tag v1.0.0
git push origin v1.0.0

# Create release and upload files
gh release create v1.0.0 \
  --title "v1.0.0 - Initial Release" \
  --notes "First public release of Claude Permission Assistant

## Features
- Automatically approves Claude Code permission prompts
- Multi-terminal support
- System tray integration
- Cross-platform (Windows + macOS)

## Installation

### Windows
1. Download ClaudePermissionAssistant-Windows-x64-v1.0.0.exe
2. Run the exe (click 'More info' → 'Run anyway' if SmartScreen appears)
3. App opens in system tray

### macOS
1. Download the appropriate version for your Mac:
   - Apple Silicon (M1/M2/M3): ClaudePermissionAssistant-macOS-arm64-v1.0.0
   - Intel: ClaudePermissionAssistant-macOS-x64-v1.0.0
2. Open Terminal and run:
   \`\`\`bash
   chmod +x ~/Downloads/ClaudePermissionAssistant-macOS-*
   ./Downloads/ClaudePermissionAssistant-macOS-*
   \`\`\`
3. Grant Accessibility permissions when prompted

## Notes
- **Windows**: First run will show SmartScreen warning (app is not code-signed)
- **macOS**: First run requires right-click → Open (app is not notarized)
" \
  publish/win-x64/ClaudePermissionAssistant.exe#Windows-x64 \
  publish/osx-arm64/ClaudePermissionAssistant-macOS-arm64-v1.0.0#macOS-ARM64-Apple-Silicon \
  publish/osx-x64/ClaudePermissionAssistant-macOS-x64-v1.0.0#macOS-Intel
```

### Step 4: Test Downloads

```bash
# Get the release URL
gh release view v1.0.0 --web

# Or get direct download links
echo "Windows: https://github.com/YOUR_USERNAME/claude-permission-assistant/releases/download/v1.0.0/ClaudePermissionAssistant.exe"
echo "macOS ARM: https://github.com/YOUR_USERNAME/claude-permission-assistant/releases/download/v1.0.0/ClaudePermissionAssistant-macOS-arm64-v1.0.0"
```

**Done!** Users can now download from your GitHub Releases page.

---

## Automated Releases with GitHub Actions

The `.github/workflows/release.yml` file is already set up. It will:
- Build Windows + macOS automatically
- Run tests
- Create GitHub release
- Upload all binaries

**To use it:**

1. Push the workflow file:
```bash
git add .github/workflows/release.yml
git commit -m "Add automated release workflow"
git push origin master
```

2. Create a release by pushing a tag:
```bash
git tag v1.0.1
git push origin v1.0.1
```

3. GitHub Actions will build everything and create the release automatically.

**Check progress:**
- Go to: `https://github.com/YOUR_USERNAME/claude-permission-assistant/actions`
- Click on the running workflow
- Download artifacts from the release page when complete

---

## Pre-Release Checklist

Before your first public release:

### Code
- [ ] Remove all debug/developer-only features
- [ ] Clean up commented-out code
- [ ] Update version numbers in .csproj files
- [ ] Test on clean machines (Windows without .NET SDK, fresh Mac)

### Documentation
- [ ] Update README.md with installation instructions
- [ ] Add LICENSE file (recommend MIT or Apache 2.0)
- [ ] Add CHANGELOG.md
- [ ] Document known limitations
- [ ] Add security/privacy notice about Accessibility permissions

### Testing
- [ ] All 91 tests pass
- [ ] Test on Windows 10 and Windows 11
- [ ] Test on Intel Mac and Apple Silicon Mac
- [ ] Test with multiple terminals
- [ ] Verify no keystroke spam
- [ ] Check approval success rate is high

### Security
- [ ] No credentials or API keys in code
- [ ] No personal information in logs
- [ ] Add .gitignore for sensitive files
- [ ] Review what permissions app requires

---

## Marketing Your Release

### README.md

Add badges:
```markdown
[![GitHub release](https://img.shields.io/github/v/release/YOUR_USERNAME/claude-permission-assistant)](https://github.com/YOUR_USERNAME/claude-permission-assistant/releases)
[![Downloads](https://img.shields.io/github/downloads/YOUR_USERNAME/claude-permission-assistant/total)](https://github.com/YOUR_USERNAME/claude-permission-assistant/releases)
[![License](https://img.shields.io/github/license/YOUR_USERNAME/claude-permission-assistant)](LICENSE)
```

### Share On
- Reddit: r/ClaudeAI, r/programming
- Hacker News
- Twitter/X
- Dev.to blog post
- Product Hunt

### Landing Page (Optional)

Create a simple website:
- GitHub Pages (free)
- Netlify (free)
- Vercel (free)

Simple landing page:
```html
<!DOCTYPE html>
<html>
<head>
  <title>Claude Permission Assistant</title>
</head>
<body>
  <h1>Claude Permission Assistant</h1>
  <p>Automatically approves Claude Code permission prompts</p>
  
  <h2>Download</h2>
  <a href="https://github.com/YOUR_USERNAME/claude-permission-assistant/releases/latest/download/ClaudePermissionAssistant.exe">
    Download for Windows
  </a>
  <br>
  <a href="https://github.com/YOUR_USERNAME/claude-permission-assistant/releases/latest/download/ClaudePermissionAssistant-macOS-arm64">
    Download for macOS (Apple Silicon)
  </a>
</body>
</html>
```

---

## Future Improvements

After first release:

### Short Term (Free)
- [ ] Add screenshots to README
- [ ] Create demo video
- [ ] Add FAQ section
- [ ] Set up issue templates
- [ ] Add contributing guidelines

### Medium Term (Costs Money)
- [ ] Windows code signing certificate ($100-400/year)
- [ ] Apple Developer account for notarization ($99/year)
- [ ] Custom domain ($10-20/year)

### Long Term
- [ ] Auto-update mechanism
- [ ] Analytics/usage tracking (privacy-respecting)
- [ ] Crash reporting
- [ ] Homebrew formula (macOS)
- [ ] Chocolatey package (Windows)
- [ ] Winget package (Windows)

---

## Expected User Experience

### Windows (Unsigned)
1. Download .exe
2. Double-click
3. Windows SmartScreen: "Windows protected your PC"
4. Click "More info" → "Run anyway"
5. App starts in system tray

**Note:** ~50% of users will trust and run it, others will be scared off by SmartScreen. Code signing eliminates this warning.

### macOS (Unsigned)
1. Download file
2. Open Terminal
3. Run: `chmod +x ~/Downloads/ClaudePermissionAssistant-macOS-*`
4. Run: `./Downloads/ClaudePermissionAssistant-macOS-*`
5. macOS: "cannot be opened because the developer cannot be verified"
6. Right-click → Open → "Open" button
7. Grant Accessibility permissions in System Settings

**Note:** This is friction. Notarization removes most of it (just one "Open" dialog, no Terminal needed).

---

## Support Questions to Expect

**"Why does Windows block it?"**
→ The app isn't code-signed. Click "More info" → "Run anyway" to proceed.

**"Why does macOS block it?"**
→ The app isn't notarized. Right-click the file → Open → Open to proceed.

**"Is it safe?"**
→ The source code is public on GitHub. You can review it or build it yourself.

**"Why does it need Accessibility permissions?"**
→ To read terminal text and send keystrokes to approve prompts automatically.

**"Does it spy on me?"**
→ No. The app has no network access and doesn't send any data anywhere.

---

## Quick Commands Reference

```bash
# Create repository
gh repo create claude-permission-assistant --public --source=. --remote=origin --push

# Build Windows
rebuild.bat

# Build macOS (on Mac)
./build-macos.sh

# Create release
git tag v1.0.0
git push origin v1.0.0
gh release create v1.0.0 --title "v1.0.0" --notes "Release notes" file1 file2 file3

# View releases
gh release list

# Delete a release (if you mess up)
gh release delete v1.0.0 --yes
git tag -d v1.0.0
git push origin :refs/tags/v1.0.0
```

---

## Cost Breakdown

| What | Cost | When |
|------|------|------|
| GitHub repository | Free | Always |
| GitHub Releases hosting | Free | Always |
| GitHub Actions (2000 min/month) | Free | Always |
| Extra GitHub Actions minutes | $0.008/min | If >2000 min/month |
| Windows code signing cert | $100-400/year | Optional |
| Apple Developer Program | $99/year | Optional |
| Domain name | $10-20/year | Optional |

**To start:** $0

**For professional distribution:** ~$200-500/year
