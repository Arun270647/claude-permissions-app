# Auto-Release Setup - Complete CI/CD Pipeline

**Created:** 2026-08-26  
**Status:** ✅ Ready to use  
**Workflow:** `.github/workflows/auto-release.yml`

---

## 🎯 What This Does

**One command releases everything:**
```bash
git push origin main  # That's it!
```

**Automatic actions:**
1. ✅ Detects version from CHANGELOG.md
2. ✅ Extracts patch notes from CHANGELOG.md
3. ✅ Runs all tests
4. ✅ Builds Windows .exe
5. ✅ Builds macOS .dmg (arm64 + x64)
6. ✅ Creates git tag automatically
7. ✅ Creates GitHub Release with patch notes
8. ✅ Generates update manifests with SHA-256 checksums
9. ✅ Updates main branch with new manifests
10. ✅ Triggers Vercel website redeploy (if configured)

---

## 📋 Complete Workflow

### Development → Release Flow

```
┌──────────────┐
│ 1. Work on   │
│    dev       │  You code, fix bugs, add features
│    branch    │
└──────┬───────┘
       │
       │ git push origin dev
       │
┌──────▼───────┐
│ 2. CI runs   │
│    on dev    │  Builds and tests automatically
└──────┬───────┘
       │
       │ User tests locally
       │
┌──────▼───────┐
│ 3. Update    │
│  CHANGELOG   │  Add your changes to CHANGELOG.md
│  .md         │  under ## [1.0.X] section
└──────┬───────┘
       │
       │ User approves: "merge to main"
       │
┌──────▼───────┐
│ 4. Merge to  │
│    main      │  git checkout main
│              │  git merge dev
└──────┬───────┘
       │
       │ git push origin main
       │
┌──────▼───────┐
│ 5. AUTO      │
│   RELEASE    │  Everything happens automatically:
│   MAGIC! ✨  │  - Version detection
│              │  - Builds
│              │  - Release creation
│              │  - Manifests update
│              │  - Website redeploy
└──────────────┘
```

---

## 📝 How to Use

### Step 1: Update CHANGELOG.md

**Before merging to main**, update CHANGELOG.md:

```markdown
## [1.0.2] - 2026-08-26

### Fixed
- **Accurate prompt counting** - Fixed misleading statistics
- **24/7 stability improvements** - Enhanced memory management

### Added
- **New feature** - Description here

## [1.0.1] - 2026-08-25
...
```

**CRITICAL:**
- Use format: `## [VERSION] - YYYY-MM-DD`
- Put the NEWEST version FIRST (at the top)
- Don't use `[Unreleased]` for the version you're releasing
- Patch notes can be multi-line and detailed

### Step 2: Merge to Main

```bash
# Ensure CHANGELOG.md is updated
git add CHANGELOG.md
git commit -m "chore: Prepare v1.0.2 release"
git push origin dev

# Merge to main (after user approval)
git checkout main
git pull origin main
git merge dev --no-ff -m "Release v1.0.2"
git push origin main
```

### Step 3: Wait for Automation

**GitHub Actions will automatically:**

1. Detect version `1.0.2` from CHANGELOG.md
2. Extract patch notes from that section
3. Run all 91 tests
4. Build binaries (if tests pass)
5. Create tag `v1.0.2`
6. Create GitHub Release with your patch notes
7. Upload binaries to release
8. Update manifests in main branch
9. Trigger Vercel website redeploy

**Time:** ~10-15 minutes

**Check progress:**
https://github.com/Arun270647/claude-permissions-app/actions

---

## 🚦 Triggers & Conditions

### When does it run?

✅ **Runs when:**
- Pushed to `main` branch
- CHANGELOG.md has a new version (e.g., `[1.0.2]`)
- Version tag doesn't exist yet

❌ **Skips when:**
- Only documentation changed (`docs/**`, `README.md`)
- Version already released (tag exists)
- Only manifest files changed (`latest-*.json`)
- CHANGELOG.md has `[Unreleased]` only

### What if I push without a new version?

**Safe!** The workflow checks and skips:
```
detect-version job:
  ✓ Extract version from CHANGELOG.md
  ✗ Version not found or tag exists
  → Skip all other jobs
```

---

## 📦 What Gets Released

### GitHub Release

**URL:** `https://github.com/Arun270647/claude-permissions-app/releases/tag/vX.Y.Z`

**Contents:**
```
Release vX.Y.Z
├── ClaudePrompter-Windows-vX.Y.Z.exe        (70 MB)
├── ClaudePrompter-macOS-arm64-vX.Y.Z.dmg   (varies)
├── ClaudePrompter-macOS-x64-vX.Y.Z.dmg     (varies)
└── Release notes (from CHANGELOG.md)
```

### Update Manifests

**Updated in main branch:**
- `latest-windows.json`
- `latest-macos-arm64.json`
- `latest-macos-x64.json`

**Contains:**
```json
{
  "version": "X.Y.Z",
  "url": "https://github.com/.../download/vX.Y.Z/...",
  "checksum": "SHA-256 hash for security",
  "patchNotes": "First 500 chars from CHANGELOG",
  "publishedAt": "2026-08-26T12:34:56Z",
  "mandatory": false
}
```

**Apps check these files for updates** → Download if newer → Verify checksum → Install

---

## 🌐 Website Integration

### Current Setup

**Website:** Separate repo at https://github.com/Arun270647/cpa-web  
**Hosting:** Vercel  
**URL:** https://cpa-web-swart.vercel.app/

### Auto-Update Options

#### Option 1: Vercel Deploy Hook (RECOMMENDED)

1. **Go to Vercel Dashboard:**
   https://vercel.com/dashboard

2. **Navigate to your project:** `cpa-web`

3. **Settings → Git → Deploy Hooks**

4. **Create deploy hook:**
   - Name: "Release Trigger"
   - Branch: `main`
   - Copy the webhook URL

5. **Add to GitHub Secrets:**
   ```
   Repository Settings → Secrets → Actions
   → New repository secret
   Name: VERCEL_DEPLOY_HOOK
   Value: <paste webhook URL>
   ```

**Result:** Website auto-deploys when release completes ✅

#### Option 2: GitHub → Vercel Git Integration

**If website repo watches this repo:**

1. Website repo imports latest-*.json from this repo
2. Vercel auto-deploys on commit to website repo
3. Users see new downloads immediately

**Current:** Website is separate, so you need Option 1 OR manual update

#### Option 3: Manual (Current)

After release completes:
1. Go to Vercel dashboard
2. Click "Redeploy" on cpa-web project
3. Website updates with new downloads

---

## 🛠️ Configuration

### Workflow Settings

**File:** `.github/workflows/auto-release.yml`

**Customizable:**
```yaml
# Line 6-8: Ignore certain files
paths-ignore:
  - 'docs/**'
  - 'README.md'

# Line 64: .NET version
dotnet-version: '8.0.x'

# Line 375: Vercel deploy hook
if: success()
curl -X POST "${{ secrets.VERCEL_DEPLOY_HOOK }}"
```

### Manifest Settings

**Checksum:** SHA-256 (secure)  
**Mandatory:** `false` (users can skip updates)  
**Patch notes:** First 500 chars from CHANGELOG

---

## 🧪 Testing the Workflow

### Dry Run (Recommended First Time)

1. **Create a test version:**
   ```markdown
   ## [1.0.2-test1] - 2026-08-26
   ### Fixed
   - Test release automation
   ```

2. **Merge to main:**
   ```bash
   git push origin main
   ```

3. **Watch GitHub Actions:**
   https://github.com/Arun270647/claude-permissions-app/actions

4. **Verify:**
   - Release created: https://github.com/Arun270647/claude-permissions-app/releases
   - Binaries uploaded
   - Manifests updated
   - Patch notes correct

5. **Clean up test release (if needed):**
   - Delete release on GitHub
   - Delete tag: `git push --delete origin v1.0.2-test1`

---

## 🚨 Troubleshooting

### Issue: "No version found in CHANGELOG.md"

**Cause:** Version format incorrect

**Fix:**
```markdown
✗ Wrong: ## 1.0.2 - 2026-08-26
✗ Wrong: ## [Unreleased]
✓ Right: ## [1.0.2] - 2026-08-26
```

### Issue: "Tag already exists"

**Cause:** Version was already released

**Fix:**
- Bump version in CHANGELOG.md
- Or delete existing tag:
  ```bash
  git tag -d v1.0.2
  git push --delete origin v1.0.2
  ```

### Issue: "Tests failed"

**Cause:** Unit tests not passing

**Fix:**
- Fix tests locally first
- Run `dotnet test` before pushing
- Never skip tests

### Issue: "Build failed"

**Cause:** Compilation error

**Fix:**
- Build locally: `./scripts/rebuild.bat`
- Fix errors
- Test before pushing

### Issue: "Website not updating"

**Cause:** Vercel deploy hook not configured

**Fix:**
- See "Website Integration → Option 1" above
- Or manually redeploy on Vercel

---

## 📊 Monitoring

### GitHub Actions

**URL:** https://github.com/Arun270647/claude-permissions-app/actions

**Check:**
- ✅ Green = Success
- ❌ Red = Failed (click for logs)
- 🟡 Yellow = Running

**Notifications:**
- GitHub sends email on failure
- Enable: Settings → Notifications → Actions

### Release Status

**Latest release:**
https://github.com/Arun270647/claude-permissions-app/releases/latest

**Should show:**
- Version number
- Release date
- Patch notes from CHANGELOG
- Download links

### Update Manifests

**Check manifests updated:**
```bash
# Should show latest version
curl https://raw.githubusercontent.com/Arun270647/claude-permissions-app/main/latest-windows.json

# Should have SHA-256 checksum
curl https://raw.githubusercontent.com/Arun270647/claude-permissions-app/main/latest-windows.json | jq .checksum
```

---

## 🔒 Security

### Checksums

All manifests include SHA-256 checksums:
```json
{
  "checksum": "a1b2c3d4e5f6...",  // 64-char hex
}
```

**Auto-update system verifies:**
1. Download file
2. Compute SHA-256
3. Compare with manifest
4. Abort if mismatch

### Permissions

**GitHub Actions has:**
- `contents: write` - Create releases and tags
- `GITHUB_TOKEN` - Default token (scoped to repo)

**No external services** except Vercel deploy hook (optional)

---

## 📚 Related Documents

- [CLAUDE.md](../../CLAUDE.md) - Development workflow
- [CHANGELOG.md](../../CHANGELOG.md) - Version history
- [AUTO_UPDATE_ENABLED.md](../AUTO_UPDATE_ENABLED.md) - How auto-update works
- [SECURITY_AUDIT_REPORT.md](../../SECURITY_AUDIT_REPORT.md) - Security audit

---

## ✅ Checklist Before First Auto-Release

- [ ] CHANGELOG.md has new version (e.g., `[1.0.2]`)
- [ ] Patch notes are complete and accurate
- [ ] All tests passing locally (`dotnet test`)
- [ ] Build successful locally (`./scripts/rebuild.bat`)
- [ ] User approved merge to main
- [ ] Merged dev to main
- [ ] Pushed to main: `git push origin main`
- [ ] Watched GitHub Actions complete
- [ ] Verified release on GitHub
- [ ] Tested download links
- [ ] (Optional) Configured Vercel deploy hook
- [ ] (Optional) Manually redeployed website

---

## 🎉 Success!

After pushing to main, you'll see:

1. **GitHub Actions runs** (~10-15 minutes)
2. **Release appears** at https://github.com/Arun270647/claude-permissions-app/releases
3. **Manifests updated** in main branch
4. **Users get update notifications** within 4 hours or on next launch
5. **(Optional) Website shows new downloads**

**No more manual releases!** 🚀

---

**Questions?**
- Check [CLAUDE.md](../../CLAUDE.md) for development workflow
- Check GitHub Actions logs for errors
- Check this document for troubleshooting

**Ready to release v1.0.2?**
1. Update CHANGELOG.md ✅ (Done)
2. User tests fixes ⏳ (Pending)
3. Merge to main ⏳ (After approval)
4. Push and watch automation ⏳
