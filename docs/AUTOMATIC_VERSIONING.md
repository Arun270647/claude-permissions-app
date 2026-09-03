# Automatic Versioning & Release System

This document explains how versioning and releases work automatically when pushing to the `main` branch.

## Overview

The project uses **automatic semantic versioning** - when you push to `main`, the system:
1. Auto-increments the version number
2. Builds Windows and macOS binaries
3. Creates a GitHub Release with download links
4. Updates the website with the new version
5. Deploys the updated website to Vercel

**Zero manual version management required.**

---

## How It Works

### 1. Push to Main Trigger

```bash
# On dev branch - make changes
git commit -m "fix: Something"
git push origin dev

# When ready for release
git checkout main
git merge dev
git push origin main  # ← THIS TRIGGERS THE WORKFLOW
```

### 2. Auto-Versioning

The workflow reads `CHANGELOG.md` and:

**If `[Unreleased]` section exists:**
- Auto-calculates next version by bumping patch number
- Example: `v1.0.2` → `v1.0.3`
- Replaces `[Unreleased]` with `[1.0.3] - 2026-09-03`
- Commits the updated CHANGELOG back to main

**If versioned section exists (e.g., `[1.0.3]`):**
- Uses that version
- Checks if tag exists
- If not tagged, creates release for that version

### 3. Build & Release

Workflow then:
- **Builds Windows** (self-contained .exe, ~70MB)
- **Builds macOS** (arm64 + x64 .dmg packages)
- **Creates GitHub Release** with:
  - Tag: `v1.0.3`
  - Title: `v1.0.3`
  - Binaries attached
  - Release notes from CHANGELOG
  
### 4. Update Manifests

Auto-generates:
- `latest-windows.json` (with SHA-256 checksum)
- `latest-macos-arm64.json`
- `latest-macos-x64.json`

These files tell the app's auto-updater where to download new versions.

### 5. Update Website

Runs `scripts/update-website-version.sh`:
- Updates `website/version.json` with:
  - Version number
  - Release date
  - Download URLs for all platforms

### 6. Deploy Website

Vercel auto-detects the change and redeploys:
- Website shows new version in badge
- Download buttons link to new binaries
- All happens in <1 minute

---

## Version Number Format

**Semantic Versioning:** `MAJOR.MINOR.PATCH`

- **MAJOR**: Breaking changes (e.g., 1.x.x → 2.0.0)
- **MINOR**: New features (e.g., 1.0.x → 1.1.0)
- **PATCH**: Bug fixes (e.g., 1.0.2 → 1.0.3)

**Current auto-versioning: Patch bump only**  
To bump MINOR or MAJOR, manually edit CHANGELOG.md before pushing.

---

## CHANGELOG.md Format

The workflow parses CHANGELOG.md. Required format:

```markdown
# Changelog

## [Unreleased]

### Added
- New feature description

### Fixed
- Bug fix description

### Changed
- Improvement description

## [1.0.2] - 2026-09-03

### Fixed
- Previous bug fix

...
```

**Rules:**
1. Always add changes under `[Unreleased]`
2. Use standard sections: `Added`, `Fixed`, `Changed`, `Removed`
3. Be specific - these become release notes
4. Keep format consistent

---

## Workflow File

**Location:** `.github/workflows/auto-release.yml`

**Triggers:**
- Push to `main` branch
- Manual trigger (Actions tab)

**Steps:**
1. `detect-version` - Read CHANGELOG, auto-version if needed
2. `build-windows` - Build Windows .exe
3. `build-macos` - Build macOS .dmg (arm64 + x64)
4. `create-release` - Create GitHub Release, update manifests, deploy website

**Permissions Required:**
- `contents: write` (for creating releases and tags)
- `actions: read`
- `id-token: write`

---

## Website Integration

### Structure

```
website/
├── index.html          # Main marketing page
├── version.json        # Auto-updated by workflow
├── vercel.json         # Deployment config
├── assets/
│   ├── style.css      # Styles
│   └── script.js      # Fetches version.json
└── README.md
```

### Version Display

The website automatically:
1. Fetches `/version.json` on page load
2. Updates badge: `v1.0.3 • Open Source • MIT Licensed`
3. Updates download URLs to versioned binaries
4. Falls back to GitHub API if version.json unavailable

### Deployment

**Vercel Configuration:**
- **Root Directory**: `website/`
- **Framework**: Other (static site)
- **Build Command**: None
- **Output Directory**: `.` (website root)

**Auto-Deploy Flow:**
```
Push to main
  ↓
Workflow updates version.json
  ↓
Commits to main [skip ci]
  ↓
Vercel detects change
  ↓
Redeploys website (<30 seconds)
```

---

## Manual Version Control

### Bump Patch (1.0.2 → 1.0.3)

Just push to main with `[Unreleased]` changes - **automatic!**

### Bump Minor (1.0.x → 1.1.0)

Edit CHANGELOG.md manually:

```markdown
## [1.1.0] - 2026-09-03

### Added
- Major new feature

## [1.0.2] - 2026-09-03
...
```

Push to main - workflow uses `1.1.0`.

### Bump Major (1.x.x → 2.0.0)

Same as minor - manually edit CHANGELOG.md:

```markdown
## [2.0.0] - 2026-09-03

### Changed
- **BREAKING**: Complete API redesign

## [1.0.2] - 2026-09-03
...
```

### Skip Release

Add `[skip ci]` to commit message:

```bash
git commit -m "docs: Update README [skip ci]"
git push origin main  # No release triggered
```

---

## Testing the Workflow

### Test Locally

```bash
# Build locally
./scripts/rebuild.bat  # Windows
./scripts/build-macos.sh  # macOS

# Test version update script
./scripts/update-website-version.sh 1.0.3
cat website/version.json  # Verify
```

### Test Workflow (Dry Run)

1. Create test branch: `git checkout -b test-release`
2. Update CHANGELOG.md with `[Unreleased]` changes
3. Push to main: `git push origin test-release:main --force`
4. Check Actions tab: `https://github.com/Arun270647/claude-permissions-app/actions`
5. Verify:
   - ✅ Version detected
   - ✅ Builds succeed
   - ✅ Release created
   - ✅ Website updated

---

## Troubleshooting

### "No version found in CHANGELOG.md"

**Cause:** Missing `[Unreleased]` or versioned section  
**Fix:** Add `[Unreleased]` section with changes

### "Tag vX.X.X already exists"

**Cause:** Trying to release same version twice  
**Fix:** Bump version or delete tag:

```bash
git tag -d v1.0.3
git push origin :refs/tags/v1.0.3
```

### Workflow fails at "Commit update manifests"

**Cause:** Workflow permission restriction (expected)  
**Impact:** Manifests will be committed on next manual push  
**Fix:** None needed - this is a soft failure

### Website shows old version

**Cause:** Browser cache or version.json not updated  
**Fix:**
1. Check `website/version.json` in repo - should have new version
2. Hard refresh: Ctrl+Shift+R (Windows) / Cmd+Shift+R (Mac)
3. Check Vercel deployment status

### Binaries not attaching to release

**Cause:** Build failed or path issue  
**Fix:**
1. Check Actions logs for build errors
2. Verify artifact upload succeeded
3. Re-trigger workflow from Actions tab

---

## Monitoring

### What to Watch

After pushing to main:

1. **Actions Tab**: `https://github.com/Arun270647/claude-permissions-app/actions`
   - All jobs should be green ✅
   - Typical duration: 5-10 minutes

2. **Releases**: `https://github.com/Arun270647/claude-permissions-app/releases`
   - New release should appear with version tag
   - Binaries attached (3 files: Windows, macOS arm64, macOS x64)

3. **Website**: `https://cpa-web-swart.vercel.app/`
   - Badge shows new version
   - Download links work

4. **Main Branch**:
   - `latest-*.json` files updated
   - `website/version.json` updated
   - `CHANGELOG.md` versioned (if was `[Unreleased]`)

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────┐
│ Developer: git push origin main                         │
└────────────────────┬────────────────────────────────────┘
                     │
                     ↓
         ┌───────────────────────┐
         │ GitHub Actions        │
         │ (.github/workflows/)  │
         └───────────┬───────────┘
                     │
        ┌────────────┴────────────┐
        ↓                         ↓
┌──────────────┐          ┌──────────────┐
│ detect-version│          │ Build Jobs   │
│ - Read       │          │ - Windows    │
│   CHANGELOG  │          │ - macOS arm64│
│ - Auto-bump  │          │ - macOS x64  │
│   version    │          └──────┬───────┘
│ - Extract    │                 │
│   notes      │                 ↓
└──────┬───────┘          ┌──────────────┐
       │                  │ Artifacts    │
       │                  │ - .exe       │
       │                  │ - .dmg (2)   │
       │                  └──────┬───────┘
       │                         │
       └────────────┬────────────┘
                    ↓
         ┌────────────────────┐
         │ create-release     │
         │ - GitHub Release   │
         │ - Attach binaries  │
         │ - Update manifests │
         │ - Update website   │
         └─────────┬──────────┘
                   │
        ┌──────────┴──────────┐
        ↓                     ↓
┌───────────────┐    ┌────────────────┐
│ Commit to main│    │ Vercel Webhook │
│ - manifests   │    │ (optional)     │
│ - version.json│    └────────┬───────┘
└───────┬───────┘             │
        │                     ↓
        │              ┌──────────────┐
        └─────────────→│ Website      │
                       │ Deployed     │
                       │ (Vercel)     │
                       └──────────────┘
```

---

## Best Practices

1. **Always update CHANGELOG.md** - This becomes your release notes
2. **Test on dev first** - Merge to main only when ready
3. **Monitor Actions** - Check builds succeed before announcing
4. **Use [Unreleased]** - Let auto-versioning handle the numbers
5. **Skip CI sparingly** - Only for pure documentation changes
6. **Keep manifests** - Don't delete `latest-*.json` files

---

## Quick Reference

**New release (patch bump):**
```bash
# 1. Update CHANGELOG.md with [Unreleased] changes
# 2. Push to main
git checkout main
git merge dev
git push origin main
# Done! Workflow handles the rest.
```

**Check release status:**
```
https://github.com/Arun270647/claude-permissions-app/actions
```

**Manual version:**
```bash
# Edit CHANGELOG.md - add [1.1.0] section
# Push to main
```

**Update website only:**
```bash
./scripts/update-website-version.sh 1.0.3
git add website/version.json
git commit -m "chore: Update website version [skip ci]"
git push origin main
```

---

## Related Files

- `.github/workflows/auto-release.yml` - Main workflow
- `scripts/update-website-version.sh` - Version update script
- `website/version.json` - Website version info
- `CHANGELOG.md` - Source of truth for versions
- `latest-*.json` - Auto-update manifests

---

**Last Updated:** 2026-09-03  
**Maintained by:** GitHub Actions + Vercel
