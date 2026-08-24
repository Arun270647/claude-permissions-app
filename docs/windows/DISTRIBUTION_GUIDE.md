# Distribution Guide - Making the App Publicly Downloadable

## Overview

This guide shows how to distribute the Claude Permission Assistant for public download on Windows and macOS.

## Quick Start

### 1. Build Release Versions

```bash
# Windows (run this on Windows)
cd "D:\projects\claude-permission app"
rebuild.bat

# macOS (run this on Mac)
cd ~/claude-permission-app
./build-macos.sh
```

### 2. Upload to GitHub Releases
- Create GitHub repository
- Create a new release (tag: v1.0.0)
- Upload built executables
- Users can download directly

---

## Windows Distribution

### Option A: Single .exe (Recommended - Easiest)

✅ **Current setup - already working**

**Build:**
```bash
rebuild.bat
# Output: publish/win-x64/ClaudePermissionAssistant.exe (70MB)
```

**Pros:**
- No installation required
- Single file, easy to download
- Works on any Windows 10/11 x64

**Cons:**
- 70MB download (includes .NET runtime)
- No Start Menu entry
- No automatic updates
- Windows SmartScreen warning (not signed)

**To distribute:**
1. Rename: `ClaudePermissionAssistant.exe` → `ClaudePermissionAssistant-Windows-v1.0.0.exe`
2. Upload to GitHub Releases
3. Users download and run

### Option B: MSI Installer (More Professional)

**Requires:** WiX Toolset v4 (https://wixtoolset.org/)

**Benefits:**
- Proper installation to Program Files
- Start Menu shortcut
- Add/Remove Programs entry
- Option to run at startup
- Looks more professional

**Setup:**
```bash
# Install WiX
dotnet tool install --global wix

# Build installer (script below)
wix build installer.wxs -o publish/ClaudePermissionAssistant-Setup.msi
```

I'll create the installer script below.

### Option C: Code Signing (Removes Security Warnings)

**Cost:** $100-400/year for code signing certificate

**Providers:**
- DigiCert
- Sectigo
- GlobalSign

**Benefits:**
- No Windows SmartScreen warning
- Users trust it more
- Required for enterprise distribution

**How to sign:**
```bash
signtool sign /f certificate.pfx /p password /t http://timestamp.digicert.com publish/win-x64/ClaudePermissionAssistant.exe
```

---

## macOS Distribution

### Option A: Single Executable (Simplest)

**Build on Mac:**
```bash
# Apple Silicon (M1/M2/M3)
dotnet publish src/macOS/ClaudePermissionAssistant.MacApp/ClaudePermissionAssistant.MacApp.csproj \
  -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true \
  -o publish/osx-arm64

# Intel Macs
dotnet publish src/macOS/ClaudePermissionAssistant.MacApp/ClaudePermissionAssistant.MacApp.csproj \
  -c Release -r osx-x64 --self-contained -p:PublishSingleFile=true \
  -o publish/osx-x64
```

**Output:**
- `publish/osx-arm64/ClaudePermissionAssistant` (Apple Silicon)
- `publish/osx-x64/ClaudePermissionAssistant` (Intel)

**To distribute:**
1. Rename with version: `ClaudePermissionAssistant-macOS-arm64-v1.0.0`
2. Upload to GitHub Releases

**User installation:**
```bash
# Download
chmod +x ClaudePermissionAssistant-macOS-arm64-v1.0.0
./ClaudePermissionAssistant-macOS-arm64-v1.0.0
```

### Option B: .app Bundle (Better)

Create proper macOS application bundle.

**Build script (run on Mac):**
```bash
#!/bin/bash
# build-macos-app.sh

VERSION="1.0.0"
APP_NAME="Claude Permission Assistant"
BUNDLE_ID="com.claudepermissionassistant.app"

# Build
dotnet publish src/macOS/ClaudePermissionAssistant.MacApp/ClaudePermissionAssistant.MacApp.csproj \
  -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true \
  -o build/osx-arm64

# Create .app bundle
mkdir -p "build/$APP_NAME.app/Contents/MacOS"
mkdir -p "build/$APP_NAME.app/Contents/Resources"

# Copy executable
cp build/osx-arm64/ClaudePermissionAssistant "build/$APP_NAME.app/Contents/MacOS/"

# Create Info.plist
cat > "build/$APP_NAME.app/Contents/Info.plist" << 'EOF'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>ClaudePermissionAssistant</string>
    <key>CFBundleIdentifier</key>
    <string>com.claudepermissionassistant.app</string>
    <key>CFBundleName</key>
    <string>Claude Permission Assistant</string>
    <key>CFBundleVersion</key>
    <string>1.0.0</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>LSMinimumSystemVersion</key>
    <string>10.15</string>
    <key>NSHighResolutionCapable</key>
    <true/>
</dict>
</plist>
EOF

# Create .dmg installer
hdiutil create -volname "$APP_NAME" -srcfolder "build/$APP_NAME.app" \
  -ov -format UDZO "publish/ClaudePermissionAssistant-macOS-arm64-v$VERSION.dmg"

echo "✅ Created: publish/ClaudePermissionAssistant-macOS-arm64-v$VERSION.dmg"
```

**User installation:**
1. Download .dmg
2. Open .dmg
3. Drag to Applications folder

### Option C: Code Signing + Notarization (Required for Easy Distribution)

**Without this:** macOS Gatekeeper blocks the app ("cannot be opened because the developer cannot be verified")

**Cost:** $99/year for Apple Developer account

**Steps:**
1. Join Apple Developer Program ($99/year)
2. Create Developer ID certificate
3. Sign the app
4. Notarize with Apple
5. Staple the notarization

**Commands:**
```bash
# Sign
codesign --deep --force --verify --verbose --sign "Developer ID Application: Your Name" \
  "Claude Permission Assistant.app"

# Notarize
xcrun notarytool submit "ClaudePermissionAssistant.dmg" \
  --apple-id your@email.com --team-id TEAMID --password app-specific-password --wait

# Staple
xcrun stapler staple "Claude Permission Assistant.app"
```

---

## GitHub Release Distribution

### Step 1: Create GitHub Repository

```bash
cd "D:\projects\claude-permission app"

# Initialize git (if not already)
git init
git add .
git commit -m "Initial commit: Claude Permission Assistant v1.0.0"

# Create GitHub repo (via gh CLI)
gh repo create claude-permission-assistant --public --source=. --remote=origin
git push -u origin master
```

### Step 2: Create Release Workflow

Create `.github/workflows/release.yml`:

```yaml
name: Release

on:
  push:
    tags:
      - 'v*'

jobs:
  build-windows:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Build Windows
        run: |
          dotnet publish src/Windows/ClaudePermissionAssistant.App/ClaudePermissionAssistant.App.csproj `
            -c Release -r win-x64 --self-contained `
            -p:PublishSingleFile=true `
            -o publish/win-x64
      
      - name: Upload Windows artifact
        uses: actions/upload-artifact@v3
        with:
          name: windows-exe
          path: publish/win-x64/ClaudePermissionAssistant.exe

  build-macos:
    runs-on: macos-latest
    strategy:
      matrix:
        arch: [osx-x64, osx-arm64]
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Build macOS
        run: |
          dotnet publish src/macOS/ClaudePermissionAssistant.MacApp/ClaudePermissionAssistant.MacApp.csproj \
            -c Release -r ${{ matrix.arch }} --self-contained \
            -p:PublishSingleFile=true \
            -o publish/${{ matrix.arch }}
      
      - name: Upload macOS artifact
        uses: actions/upload-artifact@v3
        with:
          name: macos-${{ matrix.arch }}
          path: publish/${{ matrix.arch }}/ClaudePermissionAssistant

  release:
    needs: [build-windows, build-macos]
    runs-on: ubuntu-latest
    steps:
      - uses: actions/download-artifact@v3
      
      - name: Create Release
        uses: softprops/action-gh-release@v1
        with:
          files: |
            windows-exe/ClaudePermissionAssistant.exe
            macos-osx-x64/ClaudePermissionAssistant
            macos-osx-arm64/ClaudePermissionAssistant
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

### Step 3: Create a Release

```bash
# Tag the release
git tag v1.0.0
git push origin v1.0.0

# Or create manually via GitHub web interface
gh release create v1.0.0 \
  --title "v1.0.0 - Initial Release" \
  --notes "First public release of Claude Permission Assistant"

# Upload files
gh release upload v1.0.0 \
  publish/win-x64/ClaudePermissionAssistant.exe#Windows-x64 \
  publish/osx-arm64/ClaudePermissionAssistant#macOS-ARM64 \
  publish/osx-x64/ClaudePermissionAssistant#macOS-Intel
```

---

## Distribution Checklist

### Before First Release

- [ ] Clean up code, remove developer tools/debug features
- [ ] Update README with installation instructions
- [ ] Add LICENSE file (MIT, Apache 2.0, GPL, etc.)
- [ ] Test on clean Windows machine (no .NET SDK)
- [ ] Test on clean macOS machine (both Intel and Apple Silicon)
- [ ] Create proper app icon (.ico and .icns)
- [ ] Set proper version number in .csproj files
- [ ] Add security notice about Accessibility permissions (macOS)

### For Each Release

- [ ] Update version number in all .csproj files
- [ ] Update CHANGELOG.md
- [ ] Build Windows executable
- [ ] Build macOS executables (both architectures)
- [ ] Test downloads work correctly
- [ ] Create GitHub release with release notes
- [ ] Upload all binaries with clear names

### Optional (Professional Distribution)

- [ ] Buy code signing certificate (Windows)
- [ ] Join Apple Developer Program (macOS)
- [ ] Set up automatic update mechanism
- [ ] Create landing page/website
- [ ] Add analytics/crash reporting
- [ ] Create installer packages

---

## Download Links Format

Once released on GitHub, users can download from:

```
https://github.com/YOUR_USERNAME/claude-permission-assistant/releases/latest/download/ClaudePermissionAssistant.exe
https://github.com/YOUR_USERNAME/claude-permission-assistant/releases/latest/download/ClaudePermissionAssistant-macOS-arm64
https://github.com/YOUR_USERNAME/claude-permission-assistant/releases/latest/download/ClaudePermissionAssistant-macOS-x64
```

---

## Alternative Distribution Methods

### Homebrew (macOS)

Create a Homebrew formula for easy installation:

```bash
brew tap YOUR_USERNAME/tap
brew install claude-permission-assistant
```

### Chocolatey (Windows)

Publish to Chocolatey package manager:

```bash
choco install claude-permission-assistant
```

### Winget (Windows)

Submit to Microsoft's winget repository.

### Direct Download from Website

Host the files on your own website:
- Requires HTTPS
- Needs download statistics tracking
- Full control over presentation

---

## Recommended Approach for First Release

**Simplest path to public download:**

1. ✅ **Windows:** Use existing single .exe (no signing)
2. ✅ **macOS:** Build single executable for both architectures
3. ✅ **Distribution:** GitHub Releases (free, easy)
4. ⚠️ **Accept:** Windows SmartScreen warnings, macOS Gatekeeper warnings

**User experience:**
- Windows: Download → Click "More info" → "Run anyway"
- macOS: Download → Right-click → "Open" → "Open anyway"

**Later improvements:**
- Add code signing (better UX, costs money)
- Create proper installers (.msi, .dmg)
- Set up CI/CD for automated builds

---

## Cost Summary

| Feature | Free | Paid |
|---------|------|------|
| Basic .exe/.app | ✅ | |
| GitHub Releases | ✅ | |
| GitHub Actions CI/CD | ✅ (2000 min/month) | $4/month for more |
| Windows Code Signing | | $100-400/year |
| macOS Code Signing | | $99/year (Apple Developer) |
| Domain for website | | $10-20/year |

**Minimum to start:** $0 (GitHub Releases + unsigned binaries)

**Recommended:** $200/year (Windows cert + Apple Developer account)
