# Auto-Update System - Complete Guide

This document explains how automatic updates work for Claude Permission Assistant.

## 🔄 How It Works

```
User's App                GitHub Actions              GitHub Releases
    |                           |                            |
    |--- Check for updates --->|                            |
    |                           |--- Fetch latest.json ---->|
    |<------ New version? ------|                            |
    |                           |                            |
    |--- Download update -------|--------------------------->|
    |<------ Download exe -------|                           |
    |                           |                            |
    |--- Install & Restart ---->|                            |
```

### Workflow

1. **You push code** to `windows` or `macos` branch
2. **GitHub Actions** automatically builds the app
3. **New release** is created on GitHub
4. **Users' apps** check for updates every 4 hours
5. **Notification** shown to user: "Update available!"
6. **User clicks** "Update Now"
7. **App downloads** new version, installs it, and restarts

---

## 🚀 What's Already Set Up

### 1. GitHub Actions Workflows

**`.github/workflows/build-windows.yml`**
- Triggers on push to `windows` or `main` branch
- Builds Windows exe
- Runs tests
- Creates GitHub Release
- Generates `latest-windows.json` manifest

**`.github/workflows/build-macos.yml`**
- Triggers on push to `macos` or `main` branch
- Builds macOS app (arm64 + x64)
- Runs tests
- Creates GitHub Release
- Generates `latest-macos.json` manifest

### 2. Auto-Update Service

**`src/Shared/ClaudePermissionAssistant.Core/Services/AutoUpdateService.cs`**
- Checks GitHub for new versions
- Downloads updates
- Installs updates (platform-specific)
- Auto-restarts app

---

## 📝 Setup Instructions

### Step 1: Add Version to Your .csproj Files

**Windows:** Edit `src/Windows/ClaudePermissionAssistant.App/ClaudePermissionAssistant.App.csproj`

Add this inside `<PropertyGroup>`:
```xml
<Version>1.0.0</Version>
```

**macOS:** Edit `src/macOS/ClaudePermissionAssistant.MacApp/ClaudePermissionAssistant.MacApp.csproj`

Add this inside `<PropertyGroup>`:
```xml
<Version>1.0.0</Version>
```

### Step 2: Integrate Auto-Update into Windows App

Edit `src/Windows/ClaudePermissionAssistant.App/App.xaml.cs`:

```csharp
using ClaudePermissionAssistant.Core.Services;

public partial class App : Application
{
    private AutoUpdateService? _updateService;
    private const string CURRENT_VERSION = "1.0.0"; // Update this when you release

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Initialize auto-update
        _updateService = new AutoUpdateService(CURRENT_VERSION, "windows");
        _updateService.UpdateAvailable += OnUpdateAvailable;
        _updateService.UpdateCheckFailed += OnUpdateCheckFailed;

        // Check for updates on startup
        Task.Run(async () => await _updateService.CheckForUpdatesAsync());

        // Rest of your startup code...
    }

    private void OnUpdateAvailable(object? sender, UpdateAvailableEventArgs e)
    {
        // Show notification to user
        Dispatcher.Invoke(() =>
        {
            var result = MessageBox.Show(
                $"A new version ({e.UpdateInfo.Version}) is available!\n\n" +
                $"Would you like to update now?\n\n" +
                $"Changelog: {e.UpdateInfo.Changelog}",
                "Update Available",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                // Download and install update
                Task.Run(async () =>
                {
                    var progress = new Progress<int>(percent =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            // Show progress (optional: create a progress window)
                            Console.WriteLine($"Download progress: {percent}%");
                        });
                    });

                    await _updateService.DownloadAndApplyUpdateAsync(e.UpdateInfo, progress);
                });
            }
        });
    }

    private void OnUpdateCheckFailed(object? sender, string error)
    {
        // Silently log error - don't bother user
        Console.WriteLine($"Update check failed: {error}");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _updateService?.Dispose();
        base.OnExit(e);
    }
}
```

### Step 3: Integrate Auto-Update into macOS App

Similar integration for macOS (if you have the app structure ready).

### Step 4: Commit Update Manifest Files

After GitHub Actions runs, it will generate `latest-windows.json` and `latest-macos.json`. These need to be committed to `main` branch.

**Option A: Automatic (using another workflow)**

Create `.github/workflows/commit-manifests.yml`:

```yaml
name: Commit Update Manifests

on:
  workflow_run:
    workflows: ["Build Windows", "Build macOS"]
    types:
      - completed

jobs:
  commit:
    runs-on: ubuntu-latest
    if: ${{ github.event.workflow_run.conclusion == 'success' }}

    steps:
    - name: Checkout
      uses: actions/checkout@v4

    - name: Download artifacts
      uses: actions/download-artifact@v4
      with:
        name: update-manifest-windows
        path: .

    - name: Download artifacts
      uses: actions/download-artifact@v4
      with:
        name: update-manifest-macos
        path: .

    - name: Commit manifests
      run: |
        git config user.name "github-actions[bot]"
        git config user.email "github-actions[bot]@users.noreply.github.com"
        git add latest-windows.json latest-macos.json
        git commit -m "Update manifest files" || exit 0
        git push
```

**Option B: Manual**

After a build completes, download the artifacts and commit them manually.

---

## 🔢 Version Bumping

When you want to release a new version:

### 1. Update Version in .csproj

**Windows:**
```xml
<Version>1.0.1</Version>  <!-- Was 1.0.0 -->
```

**macOS:**
```xml
<Version>1.0.1</Version>  <!-- Was 1.0.0 -->
```

### 2. Update Version in App Code

**Windows App.xaml.cs:**
```csharp
private const string CURRENT_VERSION = "1.0.1"; // Update this!
```

### 3. Push to Branch

```bash
git checkout windows
# Make your changes
git add .
git commit -m "Bump version to 1.0.1 - Add new feature"
git push origin windows
```

### 4. GitHub Actions Runs Automatically

- Builds the app
- Creates release `v1.0.1`
- Uploads exe to GitHub Releases
- Updates `latest-windows.json`

### 5. Users Get Notified

- Within 4 hours (or on next app restart)
- They see "Update available" dialog
- Click "Update Now" → app updates itself!

---

## 🧪 Testing Auto-Update Locally

### Test Update Check

```csharp
var updateService = new AutoUpdateService("0.9.0", "windows"); // Lower version
var updateInfo = await updateService.CheckForUpdatesAsync();

if (updateInfo != null)
{
    Console.WriteLine($"Update available: {updateInfo.Version}");
    Console.WriteLine($"Download URL: {updateInfo.Url}");
}
```

### Test Update Download

```csharp
var progress = new Progress<int>(percent =>
{
    Console.WriteLine($"Download: {percent}%");
});

var success = await updateService.DownloadAndApplyUpdateAsync(updateInfo, progress);
```

---

## 📊 Release Process Summary

```bash
# 1. Make changes on platform branch
git checkout windows
# ... edit code ...

# 2. Bump version in .csproj
# Edit: src/Windows/.../App.csproj
# Change: <Version>1.0.1</Version>

# 3. Update version constant in App.xaml.cs
# Change: CURRENT_VERSION = "1.0.1"

# 4. Commit and push
git add .
git commit -m "v1.0.1: Add new feature"
git push origin windows

# 5. Wait for GitHub Actions (2-3 minutes)
# Check: https://github.com/Arun270647/claude-permissions-app/actions

# 6. Release is created automatically
# Check: https://github.com/Arun270647/claude-permissions-app/releases

# 7. Users get notified within 4 hours!
```

---

## 🔧 Configuration Options

### Change Update Check Interval

In `AutoUpdateService.cs`, modify:

```csharp
// Check every 4 hours (default)
_updateCheckTimer = new Timer(CheckForUpdatesCallback, null, TimeSpan.FromMinutes(5), TimeSpan.FromHours(4));

// Change to check every 24 hours:
_updateCheckTimer = new Timer(CheckForUpdatesCallback, null, TimeSpan.FromMinutes(5), TimeSpan.FromHours(24));
```

### Disable Auto-Updates

Users can disable by setting an environment variable:

```csharp
if (Environment.GetEnvironmentVariable("DISABLE_AUTO_UPDATE") == "1")
{
    // Skip auto-update initialization
}
```

---

## 🐛 Troubleshooting

### "Update check always fails"

**Check:**
1. Is `latest-windows.json` in the `main` branch on GitHub?
2. Is the URL correct in `AutoUpdateService.cs`?
3. Check network connectivity

**Fix:** Run workflow manually or commit manifest files.

### "Update downloads but doesn't install"

**Windows:**
- Check if antivirus is blocking the batch script
- Try running app as Administrator

**macOS:**
- Check if file permissions are correct
- Ensure `chmod +x` works

### "Version not detected"

**Check:**
- Version format in `.csproj` must be valid (e.g., `1.0.0`, not `v1.0.0`)
- Version in app code matches `.csproj`

---

## ✅ Checklist

Before first auto-update release:

- [ ] Add `<Version>1.0.0</Version>` to Windows .csproj
- [ ] Add `<Version>1.0.0</Version>` to macOS .csproj
- [ ] Integrate `AutoUpdateService` into Windows app
- [ ] Integrate `AutoUpdateService` into macOS app
- [ ] Test update check locally
- [ ] Push code to trigger GitHub Actions
- [ ] Verify release is created
- [ ] Verify `latest-windows.json` is generated
- [ ] Commit manifest files to main branch
- [ ] Test update flow end-to-end

---

## 🎯 Benefits

✅ **Zero-downtime updates** - Users don't have to manually download  
✅ **Automatic builds** - Push code → release created  
✅ **Always latest** - Users stay up-to-date automatically  
✅ **Version control** - Semantic versioning (1.0.0 → 1.0.1 → 1.1.0)  
✅ **Rollback support** - Users can download old versions from Releases page  

---

## 📞 Support

If updates aren't working:
1. Check GitHub Actions logs
2. Verify release was created
3. Check manifest file exists
4. Test update check manually

**Next:** Commit these changes and push to trigger your first auto-build!
