# macOS Setup Guide - Claude Permission Assistant

Complete guide to install and run Claude Permission Assistant on macOS.

---

## 🍎 System Requirements

- **macOS:** 10.15 (Catalina) or later
- **Terminal:** Terminal.app (iTerm2 support coming soon)
- **Permissions:** Accessibility permissions required
- **Architecture:** Works on both Intel and Apple Silicon (M1/M2/M3)

---

## 📦 Installation

### Step 1: Download the App

Choose the version for your Mac:

#### **Apple Silicon (M1/M2/M3):**
```bash
# Download
curl -L -o ClaudePermissionAssistant \
  https://github.com/Arun270647/claude-permissions-app/releases/latest/download/ClaudePermissionAssistant-macOS-arm64-v1.0.0

# Or use wget
wget https://github.com/Arun270647/claude-permissions-app/releases/latest/download/ClaudePermissionAssistant-macOS-arm64-v1.0.0 \
  -O ClaudePermissionAssistant
```

#### **Intel Mac:**
```bash
# Download
curl -L -o ClaudePermissionAssistant \
  https://github.com/Arun270647/claude-permissions-app/releases/latest/download/ClaudePermissionAssistant-macOS-x64-v1.0.0

# Or use wget
wget https://github.com/Arun270647/claude-permissions-app/releases/latest/download/ClaudePermissionAssistant-macOS-x64-v1.0.0 \
  -O ClaudePermissionAssistant
```

**Not sure which Mac you have?**
```bash
# Check your architecture
uname -m

# Result:
# arm64  → Apple Silicon (M1/M2/M3)
# x86_64 → Intel
```

---

### Step 2: Make Executable

```bash
chmod +x ClaudePermissionAssistant
```

---

### Step 3: Move to Applications (Optional)

```bash
# Create app directory if it doesn't exist
mkdir -p ~/Applications

# Move the app
mv ClaudePermissionAssistant ~/Applications/

# Or move to system Applications
sudo mv ClaudePermissionAssistant /Applications/
```

---

## 🚀 First Launch

### Step 1: Run the App

```bash
# If in current directory
./ClaudePermissionAssistant

# If moved to Applications
~/Applications/ClaudePermissionAssistant

# Or
open ~/Applications/ClaudePermissionAssistant
```

### Step 2: Handle Security Warning

**macOS will block the app on first run** because it's not notarized.

You'll see: *"ClaudePermissionAssistant cannot be opened because it is from an unidentified developer."*

#### **Option A: Right-Click Method (Easiest)**

1. **Right-click** (or Control+Click) on `ClaudePermissionAssistant`
2. Select **"Open"**
3. Click **"Open"** again in the dialog
4. App will launch

#### **Option B: System Settings Method**

1. Try to open the app (it will be blocked)
2. Open **System Settings** → **Privacy & Security**
3. Scroll down to **Security**
4. You'll see: *"ClaudePermissionAssistant was blocked"*
5. Click **"Open Anyway"**
6. Click **"Open"** in confirmation dialog

#### **Option C: Command Line Method**

```bash
# Remove quarantine attribute
xattr -d com.apple.quarantine ClaudePermissionAssistant

# Now open normally
./ClaudePermissionAssistant
```

---

## 🔐 Grant Accessibility Permissions

**The app MUST have Accessibility permissions to work.**

### When Prompted:

1. App will request **Accessibility** permissions
2. macOS will show a dialog: *"ClaudePermissionAssistant would like to control this computer using accessibility features"*
3. Click **"Open System Settings"**

### In System Settings:

1. Go to **System Settings** → **Privacy & Security** → **Accessibility**
2. Find **ClaudePermissionAssistant** in the list
3. **Toggle it ON** (enable)
4. You may need to enter your password
5. **Restart the app** for permissions to take effect

### If App Doesn't Appear in List:

1. Click the **"+"** button
2. Navigate to where you saved `ClaudePermissionAssistant`
3. Select it and click **"Open"**
4. Toggle it ON

---

## 🖥️ Adding Terminal Windows

### Step 1: Open Terminal.app

```bash
# Launch Terminal if not already open
open -a Terminal
```

### Step 2: Start Claude Code

```bash
cd /path/to/your/project
claude
```

### Step 3: Add Terminal to Monitor

In the ClaudePermissionAssistant window:

1. Click **"+ Add Terminal"**
2. Select your **Terminal.app** window from the list
3. Click **"Select"** or **"Add"**

**The app will now monitor this terminal for Claude permission prompts!**

---

## ✅ Verify It's Working

### Test the Setup:

1. **Open Terminal.app**
2. **Start Claude Code:** `claude`
3. **Wait for a permission prompt**
4. **Watch it auto-approve** in ~300ms!

### You'll See:

**In ClaudePermissionAssistant Dashboard:**
```
Statistics:
Prompts Detected: 1
Prompts Approved: 1
Prompts Failed: 0

Monitored Terminals:
● Terminal (PID: 12345)  [Remove]
```

**In Terminal:**
```
Do you want to proceed?
  1. Yes
> 2. Yes, allow from this project ✓  ← Auto-approved!
  3. No

Approved automatically!
```

---

## 🎯 Usage Tips

### Multiple Terminals

You can monitor **multiple Terminal windows** simultaneously:

1. Open multiple Terminal windows/tabs
2. Run Claude Code in each
3. Add each window to the app
4. All will be auto-approved!

### Running in Background

The app runs as a **menu bar application**:

- Look for the icon in your menu bar (top-right)
- Click to show/hide dashboard
- Runs quietly in background
- Auto-starts monitoring

### Stopping Monitoring

To stop monitoring a terminal:

1. Open dashboard
2. Find the terminal in the list
3. Click **"Remove"** button

Or click **"Stop All"** to stop all monitoring.

---

## 🔧 Troubleshooting

### "Prompts Detected but Not Approved"

**Issue:** App detects prompts but doesn't approve them.

**Solutions:**

1. **Check Accessibility Permissions:**
   ```bash
   # Verify permissions
   sudo sqlite3 /Library/Application\ Support/com.apple.TCC/TCC.db \
     "SELECT * FROM access WHERE service='kTCCServiceAccessibility'"
   ```

2. **Restart the app** after granting permissions

3. **Make sure Terminal.app is active:**
   - Terminal must be in foreground
   - Try bringing Terminal to front

4. **Try re-adding the terminal:**
   - Remove terminal from monitoring
   - Add it again

### "Cannot Extract Terminal Text"

**Issue:** App can't read terminal content.

**Solutions:**

1. **Grant Accessibility permissions** (see above)

2. **Restart Terminal.app:**
   ```bash
   killall Terminal
   open -a Terminal
   ```

3. **Restart the app:**
   ```bash
   killall ClaudePermissionAssistant
   ./ClaudePermissionAssistant
   ```

4. **Check Terminal.app is supported:**
   - Currently only Terminal.app works
   - iTerm2 support coming soon

### "App Won't Open at All"

**Issue:** App crashes or won't start.

**Solutions:**

1. **Check you downloaded the right version:**
   ```bash
   # Check your architecture
   uname -m
   
   # Check file type
   file ClaudePermissionAssistant
   ```

2. **Re-download from releases:**
   - Delete current file
   - Download fresh copy
   - Make executable again

3. **Check macOS version:**
   ```bash
   sw_vers -productVersion
   # Should be 10.15 or higher
   ```

4. **Check system logs:**
   ```bash
   log show --predicate 'process == "ClaudePermissionAssistant"' \
     --last 5m
   ```

### High Failure Rate

**Issue:** Many prompts fail to approve.

**Solutions:**

1. **Minimize the dashboard window**
   - Dashboard steals focus
   - Keep it minimized or in background

2. **Don't switch windows during approval**
   - Let the app work
   - Don't interact with Terminal during detection

3. **Check terminal is in foreground:**
   - App works best when Terminal is active window

### Permission Denied Errors

**Issue:** macOS blocks the app from certain actions.

**Solutions:**

1. **Grant Full Disk Access** (optional but helpful):
   - System Settings → Privacy & Security → Full Disk Access
   - Add ClaudePermissionAssistant
   - Toggle ON

2. **Run from a standard location:**
   - Move to ~/Applications/ or /Applications/
   - Don't run from Downloads folder

---

## 🔄 Auto-Start on Login (Optional)

### Make it start automatically:

1. **System Settings** → **General** → **Login Items**
2. Click **"+"** button
3. Navigate to `ClaudePermissionAssistant`
4. Add it to the list
5. App will start when you log in

### Or use command line:

```bash
# Add to login items
osascript -e 'tell application "System Events" to make login item at end with properties {path:"/path/to/ClaudePermissionAssistant", hidden:false}'
```

---

## 📊 Statistics & Monitoring

### View Statistics

The dashboard shows real-time stats:

```
Prompts Detected: 42
Prompts Approved: 40
Prompts Failed: 2
```

### What Counts as Failure?

- Terminal not in foreground
- Prompt pattern not recognized
- AppleScript error
- Timing issue (rare)

**Note:** 95%+ success rate is normal!

---

## 🗑️ Uninstall

### Complete Removal:

```bash
# Stop the app
killall ClaudePermissionAssistant

# Remove app
rm -rf ~/Applications/ClaudePermissionAssistant
# or
sudo rm -rf /Applications/ClaudePermissionAssistant

# Remove from login items (if added)
# System Settings → General → Login Items → Remove

# Remove accessibility permissions (optional)
# System Settings → Privacy & Security → Accessibility → Remove
```

**No other files are installed!** Clean removal.

---

## 🔒 Security & Privacy

### What the App Does:

✅ **Reads terminal text** (via AppleScript)  
✅ **Sends keyboard input** (number + Enter)  
✅ **Runs locally** (no network access)  
✅ **No data collection** (zero telemetry)  

### What It DOESN'T Do:

❌ No network access  
❌ No data uploaded  
❌ No logs sent anywhere  
❌ No telemetry or analytics  
❌ No background data collection  

### Verify Yourself:

```bash
# Check network connections (should be empty)
lsof -i -P -n | grep ClaudePermissionAssistant

# Check file access
fs_usage -f filesys | grep ClaudePermissionAssistant

# Review source code
# https://github.com/Arun270647/claude-permissions-app
```

---

## 🆕 Updates

### Check for Updates:

Visit: https://github.com/Arun270647/claude-permissions-app/releases

### Manual Update:

1. Download new version
2. Stop old version: `killall ClaudePermissionAssistant`
3. Replace old file
4. Start new version

### Auto-Update (Coming Soon):

Future versions will check for updates automatically.

---

## 💡 Tips & Tricks

### Use with Multiple Projects:

```bash
# Project 1
cd ~/projects/project1
claude
# Add terminal to app

# Project 2 (new window)
cd ~/projects/project2
claude
# Add this terminal too

# Both are monitored simultaneously!
```

### Keyboard Shortcuts:

```bash
# Launch app quickly
alias cpa="~/Applications/ClaudePermissionAssistant &"

# Then just type:
cpa
```

### Check if Running:

```bash
ps aux | grep ClaudePermissionAssistant | grep -v grep
```

### View Logs (if you built from source):

```bash
# App logs location (if implemented)
~/Library/Logs/ClaudePermissionAssistant/
```

---

## 🐛 Known Issues

### Terminal.app Only

- **iTerm2** support coming soon
- **Alacritty/Kitty** not yet supported
- Only Terminal.app works currently

### First Prompt May Fail

- First prompt after adding terminal may fail
- Subsequent prompts work fine
- This is normal behavior

### AppleScript Permissions

- macOS 10.14+ requires explicit permissions
- Must grant Accessibility access
- Can't work around this restriction

---

## 📞 Need Help?

### Report Issues:

https://github.com/Arun270647/claude-permissions-app/issues

### Check Documentation:

https://github.com/Arun270647/claude-permissions-app#readme

### Build from Source:

```bash
# Clone repository
git clone https://github.com/Arun270647/claude-permissions-app.git
cd claude-permissions-app

# Build for macOS
chmod +x build-macos.sh
./build-macos.sh

# Output: publish/osx-arm64/ClaudePermissionAssistant
```

---

## ✨ Quick Reference

```bash
# Download (Apple Silicon)
curl -L -o ClaudePermissionAssistant \
  https://github.com/Arun270647/claude-permissions-app/releases/latest/download/ClaudePermissionAssistant-macOS-arm64-v1.0.0

# Make executable
chmod +x ClaudePermissionAssistant

# Remove quarantine
xattr -d com.apple.quarantine ClaudePermissionAssistant

# Run
./ClaudePermissionAssistant

# Grant Accessibility permissions when prompted

# Add Terminal.app windows to monitor

# Done! 🎉
```

---

## 🎯 Success Checklist

- [ ] Downloaded correct version (Intel vs Apple Silicon)
- [ ] Made file executable (`chmod +x`)
- [ ] Bypassed security warning (right-click → Open)
- [ ] Granted Accessibility permissions
- [ ] Restarted app after granting permissions
- [ ] Opened Terminal.app
- [ ] Started Claude Code
- [ ] Added Terminal window to app
- [ ] Tested with a permission prompt
- [ ] Saw auto-approval happen!

---

**Enjoy uninterrupted Claude Code sessions on macOS!** 🚀🍎

---

## 📝 Version Info

**Current Version:** v1.0.0  
**Compatibility:** macOS 10.15+  
**Architecture:** Universal (Intel + Apple Silicon)  
**Terminal Support:** Terminal.app only  
**Status:** Stable  

---

Made with ❤️ for macOS developers using Claude Code
