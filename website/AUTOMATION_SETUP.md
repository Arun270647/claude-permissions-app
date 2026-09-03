# Website Automation Setup Guide

## Overview

The website now automatically updates to reflect new releases from the main repository. When a new version is pushed to `main` branch in `claude-permissions-app`, the website will:

1. Detect the new release
2. Fetch version info and download URLs from GitHub Releases API
3. Update download links automatically

## ✅ What's Already Done

### 1. Auto-Release Workflow (claude-permissions-app repo)
- `.github/workflows/auto-release.yml` already has Vercel webhook trigger (lines 359-367)
- Workflow runs on every push to `main` branch
- Creates GitHub Release with binaries
- Triggers Vercel deployment via webhook (if `VERCEL_DEPLOY_HOOK` secret is set)

### 2. Website Dynamic Version Loading (cpa-web repo)
- `assets/script.js` now fetches latest release from GitHub API on page load
- Automatically updates download URLs for Windows and macOS
- Fallback to v1.0.4 if API fetch fails

### 3. Git Commit
- Changes committed to `cpa-web` main branch
- Ready to push to GitHub

---

## 🚀 Setup Instructions

### Step 1: Get Vercel Deploy Hook URL

1. **Go to Vercel Dashboard:**
   - Navigate to: https://vercel.com/dashboard
   - Select your project (e.g., `cpa-web`)

2. **Navigate to Settings:**
   - Click on "Settings" tab
   - Click "Git" in the left sidebar

3. **Create Deploy Hook:**
   - Scroll down to "Deploy Hooks" section
   - Click "+ Create Hook"
   - Name: `GitHub Auto Release`
   - Branch: `main` (or whichever branch Vercel deploys from)
   - Click "Create Hook"

4. **Copy the Webhook URL:**
   - You'll get a URL like: `https://api.vercel.com/v1/integrations/deploy/prj_xxxxxxxxxxxx/yyyyyyyyyyy`
   - Copy this URL (you'll need it in Step 2)

### Step 2: Add Secret to GitHub

1. **Go to GitHub Repository:**
   - Navigate to: https://github.com/Arun270647/claude-permissions-app

2. **Open Settings:**
   - Click "Settings" tab
   - Click "Secrets and variables" → "Actions" in left sidebar

3. **Add New Secret:**
   - Click "New repository secret"
   - Name: `VERCEL_DEPLOY_HOOK`
   - Value: Paste the webhook URL from Step 1
   - Click "Add secret"

### Step 3: Push Website Changes

```bash
cd D:\projects\cpa-web
git push origin main
```

This will deploy the updated website with dynamic version fetching.

### Step 4: Test the Complete Flow

1. **Make a test change in claude-permissions-app:**
   ```bash
   cd D:\projects\claude-permission app
   git checkout dev
   # Make a small change (e.g., update a comment)
   git add .
   git commit -m "test: Verify automation flow"
   git push origin dev
   ```

2. **Merge to main (after approval):**
   ```bash
   git checkout main
   git pull origin main
   git merge dev --no-ff -m "test: Verify automation flow"
   git push origin main
   ```

3. **Watch the automation:**
   - **GitHub Actions:** https://github.com/Arun270647/claude-permissions-app/actions
     - Should see workflow run
     - Should create new release
     - Should trigger Vercel webhook
   
   - **Vercel Dashboard:** https://vercel.com/dashboard
     - Should see new deployment triggered
     - Wait for deployment to complete (~1-2 minutes)
   
   - **Visit Website:** https://cpa-web-swart.vercel.app/
     - Open browser console (F12)
     - Should see: `Claude Prompter website loaded 🚀 (v1.0.x)`
     - Download links should point to latest version

---

## 🔍 How It Works

### Flow Diagram

```
┌─────────────────────────────────────────────────────────────┐
│  Developer pushes to main (claude-permissions-app)          │
└──────────────────┬──────────────────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────────────────┐
│  GitHub Actions: auto-release.yml                           │
│  1. Extract version from CHANGELOG.md                       │
│  2. Build Windows .exe + macOS .dmg                         │
│  3. Create GitHub Release with binaries                     │
│  4. Generate update manifests (latest-*.json)               │
│  5. Commit manifests back to main                           │
│  6. Trigger Vercel webhook (POST to VERCEL_DEPLOY_HOOK)    │
└──────────────────┬──────────────────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────────────────┐
│  Vercel receives webhook                                    │
│  - Pulls latest code from cpa-web repo                      │
│  - Deploys updated website                                  │
└──────────────────┬──────────────────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────────────────┐
│  User visits website                                        │
│  - Browser loads index.html                                 │
│  - JavaScript calls GitHub Releases API                     │
│  - Fetches latest release info (version, download URLs)     │
│  - Updates download links dynamically                       │
└─────────────────────────────────────────────────────────────┘
```

### Key Components

**1. GitHub Actions Workflow (claude-permissions-app)**
```yaml
- name: Trigger website deployment (Vercel)
  if: success()
  run: |
    if [ -n "${{ secrets.VERCEL_DEPLOY_HOOK }}" ]; then
      curl -X POST "${{ secrets.VERCEL_DEPLOY_HOOK }}"
    else
      echo "Vercel deploy hook not configured"
    fi
```

**2. Website JavaScript (cpa-web)**
```javascript
async function fetchLatestRelease() {
    const response = await fetch('https://api.github.com/repos/Arun270647/claude-permissions-app/releases/latest');
    const release = await response.json();
    const version = release.tag_name.replace(/^v/, '');
    
    // Find assets and update download URLs
    const windowsAsset = release.assets.find(a => a.name.includes('Windows'));
    const macArmAsset = release.assets.find(a => a.name.includes('arm64'));
    // ... update DOWNLOAD_URLS
}
```

---

## 🔧 Troubleshooting

### Website not updating after release?

1. **Check GitHub Actions:**
   - Go to: https://github.com/Arun270647/claude-permissions-app/actions
   - Look for failed workflows
   - Check if webhook step succeeded

2. **Check Vercel Deployments:**
   - Go to: https://vercel.com/dashboard
   - Look for recent deployments
   - Check deployment logs for errors

3. **Check Browser Console:**
   - Visit website with F12 open
   - Look for errors in console
   - Check what version is logged: `Claude Prompter website loaded 🚀 (v1.0.x)`

4. **Check GitHub API Rate Limits:**
   - GitHub API has rate limits (60 requests/hour for unauthenticated)
   - If exceeded, website will fall back to hardcoded v1.0.4

### Vercel webhook not triggering?

1. **Verify secret is set:**
   ```bash
   # Cannot view secret value, but can verify it exists
   # Go to: https://github.com/Arun270647/claude-permissions-app/settings/secrets/actions
   # Should see: VERCEL_DEPLOY_HOOK
   ```

2. **Test webhook manually:**
   ```bash
   curl -X POST "YOUR_VERCEL_DEPLOY_HOOK_URL"
   ```
   - Should trigger a deployment in Vercel dashboard

3. **Check workflow logs:**
   - Go to failed workflow run
   - Look at "Trigger website deployment" step
   - Should see: `curl -X POST ...` executed

### Download links not updating?

1. **Check GitHub Release:**
   - Go to: https://github.com/Arun270647/claude-permissions-app/releases/latest
   - Verify assets are uploaded (ClaudePrompter-Windows-v1.0.x.exe, etc.)

2. **Check browser cache:**
   - Hard refresh website (Ctrl+Shift+R or Cmd+Shift+R)
   - Or clear browser cache

3. **Check API response:**
   - Open browser console
   - Run: `fetch('https://api.github.com/repos/Arun270647/claude-permissions-app/releases/latest').then(r => r.json()).then(console.log)`
   - Verify response contains correct version and assets

---

## 📊 Cost Analysis

**Vercel Deploy Hook:**
- ✅ **FREE** - No additional cost
- Included in Vercel free tier
- Unlimited webhook triggers

**GitHub Actions:**
- ✅ **FREE** - For public repositories
- 2,000 minutes/month for private repos (paid plans)

**GitHub API:**
- ✅ **FREE** - Rate limited
- 60 requests/hour unauthenticated
- 5,000 requests/hour authenticated (if needed later)

**Total Cost: $0** 🎉

---

## 🎯 Next Steps

1. ✅ **Complete Step 1:** Get Vercel Deploy Hook URL
2. ✅ **Complete Step 2:** Add secret to GitHub
3. ✅ **Complete Step 3:** Push website changes
4. ✅ **Complete Step 4:** Test the flow with a new release

Once complete, every push to `main` will:
- Automatically create a release
- Automatically update the website
- Zero manual intervention needed!

---

## 📝 Maintenance

### Updating the fallback version

If GitHub API fails, website falls back to hardcoded version. To update:

```javascript
// In assets/script.js, update the catch block:
DOWNLOAD_URLS = {
    windows: 'https://github.com/Arun270647/claude-permissions-app/releases/download/v1.0.X/...',
    // Update version number here
    version: '1.0.X'  // ← Update this
};
```

### Monitoring

Monitor webhook triggers:
- **GitHub Actions:** Check workflow run logs
- **Vercel:** Check deployment logs and history
- **Website:** Check browser console for version logs

---

## ✅ Checklist

- [ ] Get Vercel Deploy Hook URL
- [ ] Add `VERCEL_DEPLOY_HOOK` secret to GitHub
- [ ] Push website changes to GitHub
- [ ] Test with a new release
- [ ] Verify website updates automatically
- [ ] Check browser console shows correct version
- [ ] Verify download links work

---

**Questions?**
- Vercel Documentation: https://vercel.com/docs/concepts/git/deploy-hooks
- GitHub Actions Secrets: https://docs.github.com/en/actions/security-guides/encrypted-secrets
