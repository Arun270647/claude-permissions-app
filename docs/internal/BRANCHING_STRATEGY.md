# Branching Strategy

This project uses a platform-specific branching strategy to organize development across Windows, macOS, and web platforms.

## Branch Structure

```
main        ← Production-ready code, releases cut from here
├── windows ← Windows-specific development
├── macos   ← macOS-specific development
└── web     ← Website/landing page development (future)
```

## Branch Descriptions

### `main` (default)
- **Purpose:** Production-ready code
- **Contains:** All cross-platform code, releases, stable features
- **Protected:** Yes (requires PR review before merge)
- **Releases:** All version tags created from this branch

**When to use:**
- Creating releases
- Hotfixes that affect all platforms
- Documentation updates that apply to all platforms

### `windows`
- **Purpose:** Windows-specific development
- **Contains:** Windows app code (`src/Windows/`), Windows-specific features
- **Merges to:** `main` via PR

**When to use:**
- Working on Windows UI (WPF)
- Windows automation improvements
- Windows-specific bug fixes
- Windows build/publish changes

### `macos`
- **Purpose:** macOS-specific development
- **Contains:** macOS app code (`src/macOS/`), macOS-specific features
- **Merges to:** `main` via PR

**When to use:**
- Working on macOS UI (Avalonia)
- AppleScript improvements
- macOS-specific bug fixes
- macOS build/publish changes

### `web`
- **Purpose:** Website/landing page development (future)
- **Contains:** Website source, download page, documentation site
- **Merges to:** `main` via PR

**When to use:**
- Building project website
- Creating download page
- Documentation improvements for web

## Workflow

### Making Changes

#### For Cross-Platform Changes (e.g., parser, models)

```bash
# Work on main branch
git checkout main
git pull origin main

# Make changes to src/Shared/
# ... edit files ...

# Commit and push
git add .
git commit -m "Update prompt parser logic"
git push origin main
```

#### For Platform-Specific Changes

**Windows Example:**
```bash
# Switch to windows branch
git checkout windows
git pull origin windows

# Merge latest main to stay current
git merge main

# Make Windows-specific changes
# ... edit src/Windows/ files ...

# Commit
git add .
git commit -m "Add Windows system tray icon improvements"
git push origin windows

# When ready, create PR to merge back to main
```

**macOS Example:**
```bash
# Switch to macos branch
git checkout macos
git pull origin macos

# Merge latest main
git merge main

# Make macOS-specific changes
# ... edit src/macOS/ files ...

# Commit
git add .
git commit -m "Fix AppleScript text extraction for iTerm2"
git push origin macos

# Create PR to merge back to main
```

**Web Example:**
```bash
# Switch to web branch
git checkout web
git pull origin web

# Make website changes
# ... edit website files ...

# Commit
git add .
git commit -m "Add download page with installation guide"
git push origin web

# Create PR to merge back to main
```

### Pull Request Flow

```
1. Developer creates feature on platform branch (windows/macos/web)
2. Developer opens PR: [platform-branch] → main
3. CI runs tests (GitHub Actions)
4. Reviewer approves
5. Merge to main
6. Main automatically deploys/releases
```

## Keeping Branches in Sync

Platform branches should regularly merge from `main` to stay current:

```bash
# On windows branch
git checkout windows
git merge main
git push origin windows

# On macos branch
git checkout macos
git merge main
git push origin macos

# On web branch
git checkout web
git merge main
git push origin web
```

**When to sync:**
- Before starting new work
- After any main branch release
- Daily during active development

## Release Process

Releases are always cut from `main`:

```bash
# Ensure main has latest from all platforms
git checkout main
git merge windows  # If windows has unreleased changes
git merge macos    # If macos has unreleased changes
git merge web      # If web has unreleased changes

# Tag the release
git tag v1.0.1
git push origin v1.0.1

# GitHub Actions builds Windows + macOS binaries
# Creates GitHub Release with downloads
```

## Branch Protection Rules

### `main` branch
- ✅ Require pull request before merging
- ✅ Require status checks to pass (CI tests)
- ✅ Require branches to be up to date before merging
- ❌ Allow force push: **NEVER**
- ❌ Allow deletions: **NEVER**

### Platform branches (`windows`, `macos`, `web`)
- ✅ Allow direct commits (for rapid development)
- ✅ Require PR to merge to main
- ❌ Allow force push: Use with caution
- ❌ Allow deletions: **NEVER**

## Examples

### Example 1: Add Windows-only feature

```bash
git checkout windows
git pull origin windows
git merge main                    # Get latest shared code

# Add feature
# ... edit src/Windows/ClaudePermissionAssistant.App/DashboardWindow.xaml.cs ...

git add .
git commit -m "Add Windows notification sounds"
git push origin windows

# Open PR on GitHub: windows → main
```

### Example 2: Fix bug affecting all platforms

```bash
git checkout main
git pull origin main

# Fix bug
# ... edit src/Shared/ClaudePermissionAssistant.Core/Services/ClaudePromptParserSimple.cs ...

git add .
git commit -m "Fix regex pattern for multiline prompts"
git push origin main

# No PR needed - direct commit to main
# All platform branches should merge main to get the fix
```

### Example 3: Build website for downloads

```bash
git checkout web
git pull origin web
git merge main                    # Get latest docs/README

# Create website
mkdir website
cd website
# ... create index.html, download.html, etc ...

git add .
git commit -m "Add project website with download page"
git push origin web

# Open PR on GitHub: web → main
# After merge, deploy website to GitHub Pages
```

## FAQ

**Q: Which branch should I use for a new feature?**  
A: If it touches only Windows code → `windows`, only macOS → `macos`, only website → `web`, multiple platforms → `main`

**Q: Can I merge between platform branches?**  
A: No. Always merge platform branches → `main`, then `main` → other platforms

**Q: What if I accidentally commit to the wrong branch?**  
A: Use `git cherry-pick` to move the commit to the correct branch

**Q: How do I see what's different between branches?**  
```bash
git diff main..windows           # Changes in windows not in main
git log main..windows --oneline  # Commits in windows not in main
```

**Q: Should I delete branches after merging?**  
A: **NO.** These are long-lived platform branches, not feature branches. Keep them forever.

## Visual Example

```
Time →

main:     v1.0.0 ─────────── v1.0.1 ─────────── v1.1.0
                    ↑            ↑            ↑
windows:  feat A ───┘  feat B ──┘  feat C ───┘
                       ↑
macos:    feat X ──────┴──────────────────────
                                   ↑
web:      website ─────────────────┘
```

Each platform branch merges to main when ready, and main creates releases.

---

**Remember:** Merges always flow **platform → main**, never the reverse (except for syncing).
