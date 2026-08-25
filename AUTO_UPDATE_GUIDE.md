# Silent Auto-Update System - Complete Guide

## 🎉 What's Been Set Up

Your app now has **fully automatic silent updates** with:
- ✅ **Automatic release creation** on version tags
- ✅ **Silent background updates** every 4 hours
- ✅ **No user prompts** - updates happen automatically
- ✅ **Auto-restart** after update completes
- ✅ **Cross-platform** - Windows and macOS

---

## 🚀 How It Works

```
1. You push a version tag (v1.0.1)
2. GitHub Actions builds all platforms
3. GitHub Release created automatically
4. Manifest files updated (latest-*.json)
5. Users' apps check for updates every 4 hours
6. Update downloads silently in background
7. App installs update and restarts
8. Users now have the new version!
```

**Users experience ZERO disruption** - the app just restarts with the new version.

---

## 📝 How to Release a New Version

### **Step 1: Bump Version in Code**

Edit **both** platform apps:

**Windows:** `src/Windows/ClaudePermissionAssistant.App/App.xaml.cs`
```csharp
private const string CURRENT_VERSION = "1.0.1"; // ← Change this
```

**macOS:** `src/macOS/ClaudePermissionAssistant.MacApp/App.axaml.cs`
```csharp
private const string CURRENT_VERSION = "1.0.1"; // ← Change this
```

### **Step 2: Commit Changes**

```bash
git add .
git commit -m "Bump version to v1.0.1"
git push origin main
```

### **Step 3: Create and Push Version Tag**

```bash
git tag v1.0.1
git push origin v1.0.1
```

**That's it!** GitHub Actions will automatically:
- Build Windows (x64)
- Build macOS (arm64 + x64)
- Create GitHub Release
- Upload binaries
- Generate manifest files
- Commit manifests to `main`

---

## ⏱️ Timeline

After you push the tag:

| Time | What Happens |
|------|-------------|
| **0 min** | You push `v1.0.1` tag |
| **2 min** | GitHub Actions builds complete |
| **2 min** | Release created with binaries |
| **3 min** | Manifests committed to `main` |
| **< 4 hrs** | All users' apps detect new version |
| **< 4 hrs** | Updates download silently |
| **< 4 hrs** | Apps restart with new version |

**Users get updates within 4 hours** (or immediately on next app restart).

---

## 🧪 Testing the Update System

### **Test 1: Local Version Check**

Run your app locally with an older version number:

```csharp
// In App.xaml.cs or App.axaml.cs
private const string CURRENT_VERSION = "0.9.0"; // Older than latest
```

The app will detect the update and silently install it.

### **Test 2: Dry Run Release**

Create a test tag to verify workflow:

```bash
git tag v1.0.0-test
git push origin v1.0.0-test
```

Check GitHub Actions to ensure:
- ✅ All builds pass
- ✅ Release created
- ✅ Manifests generated

Then delete the test release and tag:
```bash
git push --delete origin v1.0.0-test
git tag -d v1.0.0-test
```

---

## 🔧 Configuration

### **Change Update Check Interval**

Default: Every 4 hours

Edit `src/Shared/ClaudePermissionAssistant.Core/Services/AutoUpdateService.cs`:

```csharp
// Line 32 - Change from 4 hours to 24 hours:
_updateCheckTimer = new Timer(CheckForUpdatesCallback, null, 
    TimeSpan.FromMinutes(5),    // First check: 5 min after startup
    TimeSpan.FromHours(24));    // ← Change this
```

### **Disable Silent Updates (Show Prompt)**

If you want to ask users before updating, modify the event handler:

**Windows** (`src/Windows/ClaudePermissionAssistant.App/App.xaml.cs`):
```csharp
private async void OnUpdateAvailable(object? sender, UpdateAvailableEventArgs e)
{
    // Show confirmation dialog
    var result = MessageBox.Show(
        $"New version {e.UpdateInfo.Version} is available. Update now?",
        "Update Available",
        MessageBoxButton.YesNo,
        MessageBoxImage.Information
    );

    if (result == MessageBoxResult.Yes)
    {
        await _autoUpdateService!.DownloadAndApplyUpdateAsync(e.UpdateInfo);
    }
}
```

---

## 📊 Monitoring Releases

### **View All Releases**
https://github.com/Arun270647/claude-permissions-app/releases

### **Check Workflow Runs**
https://github.com/Arun270647/claude-permissions-app/actions

### **View Manifest Files**

After a release, these files are in the `main` branch:
- `latest-windows.json`
- `latest-macos-arm64.json`
- `latest-macos-x64.json`

Example:
```json
{
  "version": "1.0.1",
  "url": "https://github.com/.../ClaudePermissionAssistant-Windows-v1.0.1.exe",
  "changelog": "See release notes...",
  "publishedAt": "2026-08-25T10:30:00Z"
}
```

---

## 🎯 Version Numbering

Use **Semantic Versioning** (major.minor.patch):

- **1.0.0** → **1.0.1** - Bug fix (patch)
- **1.0.1** → **1.1.0** - New feature (minor)
- **1.1.0** → **2.0.0** - Breaking change (major)

---

## ✅ Release Checklist

Before releasing v1.0.1:

- [ ] Update `CURRENT_VERSION` in Windows app
- [ ] Update `CURRENT_VERSION` in macOS app
- [ ] Update `CHANGELOG.md` with release notes
- [ ] Test locally (both platforms if possible)
- [ ] Commit changes: `git commit -m "Bump version to v1.0.1"`
- [ ] Push to main: `git push origin main`
- [ ] Create tag: `git tag v1.0.1`
- [ ] Push tag: `git push origin v1.0.1`
- [ ] Wait 2-3 minutes for GitHub Actions
- [ ] Verify release created on GitHub
- [ ] Verify manifests committed to `main`
- [ ] Test update on a real device (optional)

---

## 🐛 Troubleshooting

### **Release workflow didn't trigger**

Check:
1. Tag format: Must be `v*.*.*` (e.g., `v1.0.1`)
2. Tag pushed: `git push origin v1.0.1`
3. Workflow exists: `.github/workflows/release.yml`

### **Builds failed**

1. Go to Actions tab
2. Click failed workflow
3. Read error logs
4. Fix code and re-tag:
   ```bash
   git tag -d v1.0.1
   git push --delete origin v1.0.1
   # Fix code, commit
   git tag v1.0.1
   git push origin v1.0.1
   ```

### **Users not getting updates**

Check:
1. Is `latest-*.json` in `main` branch?
2. Is the URL correct in manifest?
3. Is the version number higher than current?
4. Has it been more than 4 hours since user's app started?

---

## 🎉 Success Indicators

Your auto-update system is working when:

✅ Pushing a tag triggers GitHub Actions  
✅ Release appears in Releases page  
✅ Binaries are downloadable  
✅ Manifest files exist in `main` branch  
✅ Old version apps detect new version  
✅ Apps download and install silently  
✅ Apps restart with new version  

---

## 📞 Support

**Issues:** https://github.com/Arun270647/claude-permissions-app/issues  
**Actions:** https://github.com/Arun270647/claude-permissions-app/actions  
**Releases:** https://github.com/Arun270647/claude-permissions-app/releases

---

**Congratulations! You now have enterprise-grade automatic updates!** 🚀
