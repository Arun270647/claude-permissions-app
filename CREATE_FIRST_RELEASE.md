# Creating Your First Release (v1.0.0)

## 🎯 **The Issue**

Your website download buttons are showing **404 errors** because there's no v1.0.0 release yet!

The buttons try to download from:
- `https://github.com/.../releases/latest/download/ClaudePermissionAssistant-Windows-v1.0.0.exe`
- But `/releases/latest` doesn't exist yet → **404 error**

---

## ✅ **Quick Fix (Done)**

Website now points to the releases page temporarily:
- Users will see "There aren't any releases here"
- They'll know when v1.0.0 is available

---

## 🚀 **Permanent Solution: Create v1.0.0 Release**

### **Option 1: Automatic (Recommended)**

Let GitHub Actions build and create the release automatically:

```bash
# 1. Make sure all your code is committed
git status

# 2. Create and push v1.0.0 tag
git tag v1.0.0
git push origin v1.0.0
```

**What happens:**
1. GitHub Actions detects the tag
2. Builds Windows executable
3. Builds macOS .zip files (both architectures)
4. Creates GitHub Release automatically
5. Uploads all files
6. Generates release notes

**Wait 3-5 minutes**, then check:
https://github.com/Arun270647/claude-permissions-app/releases

---

### **Option 2: Manual (If Actions Fail)**

If GitHub Actions doesn't work, create manually:

#### **Step 1: Build Locally**

**Windows:**
```bash
# On Windows machine
rebuild.bat
# Output: publish/win-x64/ClaudePermissionAssistant.exe
```

**macOS:**
```bash
# On Mac (if you have one)
chmod +x build-macos.sh
./build-macos.sh
# Output: releases/ClaudePermissionAssistant-macOS-*.zip
```

#### **Step 2: Create Release on GitHub**

1. Go to: https://github.com/Arun270647/claude-permissions-app/releases
2. Click **"Create a new release"**
3. **Tag:** `v1.0.0` (create new tag)
4. **Title:** `v1.0.0 - Initial Release`
5. **Description:**
   ```markdown
   ## 🎉 First Public Release
   
   Auto-approve Claude Code permission prompts!
   
   ### 📦 Downloads
   
   **Windows:**
   - ClaudePermissionAssistant-Windows-v1.0.0.exe
   
   **macOS:**
   - ClaudePermissionAssistant-macOS-arm64-v1.0.0.zip (Apple Silicon)
   - ClaudePermissionAssistant-macOS-x64-v1.0.0.zip (Intel)
   
   ### 🚀 Quick Start
   
   **Windows:** Download .exe, run it
   **macOS:** Download .zip, extract, drag to Applications
   ```
6. **Upload files:**
   - Drag and drop the .exe and .zip files
7. Click **"Publish release"**

---

## 🔧 **After Release is Created**

### **Update Website URLs**

Once the release exists, update `cpa-web/assets/script.js`:

```javascript
// Change from:
const DOWNLOAD_URLS = {
    windows: 'https://github.com/.../releases',
    // ...
};

// To:
const DOWNLOAD_URLS = {
    windows: 'https://github.com/Arun270647/claude-permissions-app/releases/latest/download/ClaudePermissionAssistant-Windows-v1.0.0.exe',
    macIntel: 'https://github.com/Arun270647/claude-permissions-app/releases/latest/download/ClaudePermissionAssistant-macOS-x64-v1.0.0.zip',
    macArm: 'https://github.com/Arun270647/claude-permissions-app/releases/latest/download/ClaudePermissionAssistant-macOS-arm64-v1.0.0.zip'
};
```

Then commit and push:
```bash
cd ../cpa-web
git add assets/script.js
git commit -m "Update download URLs to point to v1.0.0 release"
git push origin main
```

---

## 🎯 **Test the Downloads**

After release is created and website is updated:

1. Visit: **https://cpa-web-swart.vercel.app/**
2. Click **"Download for Windows"**
3. Should **automatically download** the `.exe` file
4. Click **"macOS"** button
5. Should **automatically download** the `.zip` file

**No more 404 errors!** ✅

---

## 📊 **Verification Checklist**

- [ ] Tag `v1.0.0` created and pushed
- [ ] GitHub Actions workflow completed successfully
- [ ] Release visible at: `/releases`
- [ ] Windows `.exe` file downloadable
- [ ] macOS `.zip` files downloadable (both architectures)
- [ ] Website updated with direct download URLs
- [ ] Tested downloads from website
- [ ] Both downloads work automatically

---

## 🐛 **Troubleshooting**

### **"GitHub Actions failed"**

Check the workflow:
1. Go to: https://github.com/Arun270647/claude-permissions-app/actions
2. Click on the failed workflow
3. Read error messages
4. Fix issues and re-tag:
   ```bash
   git tag -d v1.0.0
   git push --delete origin v1.0.0
   # Fix issues
   git tag v1.0.0
   git push origin v1.0.0
   ```

### **"Downloads still 404"**

- Make sure release is **published** (not draft)
- File names must **exactly match** URLs in script.js
- Use `/releases/latest/download/` path (not `/download/v1.0.0/`)

### **"I don't have a Mac to build for macOS"**

That's fine! GitHub Actions builds on macOS runners automatically.
Just push the tag and let Actions handle it.

---

## 🎊 **Success Indicators**

When everything works:

1. **Releases page shows v1.0.0**
2. **3 downloadable files** (1 Windows + 2 macOS)
3. **Website downloads work** (no 404)
4. **Files download automatically** (not redirect to GitHub)

---

## 💡 **Quick Command**

Run this now to create the release:

```bash
cd "D:/projects/claude-permission app"
git tag v1.0.0
git push origin v1.0.0
```

Then wait 3-5 minutes and check:
https://github.com/Arun270647/claude-permissions-app/releases

---

**That's it! Once the release exists, all downloads will work perfectly!** 🚀
