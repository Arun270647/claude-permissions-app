# Development Branch Workflow

**Implemented:** 2026-08-25  
**Status:** Active

## Branch Strategy

```
dev  ← All development happens here
 ↓
main ← Production code (only after approval)
```

## Rules

### 1. Always Work on Dev

**NEVER commit directly to `main`** unless it's an absolute emergency hotfix.

```bash
# Always ensure you're on dev
git checkout dev
git pull origin dev

# Make your changes
# ... code ...

# Commit and push to dev
git add .
git commit -m "Your changes"
git push origin dev
```

### 2. Dev → Main Only After Approval

Merging to `main` requires:
- ✅ CI/CD passes on dev
- ✅ Local testing completed
- ✅ User explicitly approves with phrases like:
  - "merge to main"
  - "push to main"
  - "ready for production"
  - "deploy to main"

**Never assume it's ready for main without explicit approval.**

### 3. Merge Process

```bash
# After user approval
git checkout main
git pull origin main
git merge dev --no-ff  # Creates merge commit
git push origin main

# Return to dev for continued work
git checkout dev
```

## GitHub Actions

### Build Triggers

Both `build-windows.yml` and `build-macos.yml` trigger on:

**Push events:**
- `dev` branch
- `main` branch

**Pull request events:**
- PRs targeting `main`

**Path filters:**
- Windows: `src/Windows/**`, `src/Shared/**`
- macOS: `src/macOS/**`, `src/Shared/**`

### Why Actions Might Not Show

1. **Path filters** - Documentation-only changes don't trigger builds
2. **No code changes** - README, docs, markdown files won't build
3. **Wrong branch** - Must be on `dev` or `main`

To force a build, change a code file:
```bash
# Touch a code file to trigger CI
touch src/Shared/ClaudePermissionAssistant.Core/Models/DetectedPrompt.cs
git add .
git commit -m "Trigger CI"
git push origin dev
```

## Typical Workflow

### Day-to-Day Development

```bash
# Morning: Start work
git checkout dev
git pull origin dev

# Work on feature
# ... edit code ...

# Test locally
dotnet build
dotnet test

# Commit frequently
git add .
git commit -m "Add feature X"
git push origin dev

# Check GitHub Actions
# https://github.com/Arun270647/claude-permissions-app/actions

# Continue work or test locally
```

### Before Merging to Main

```bash
# Checklist:
# ✅ All commits pushed to dev
# ✅ GitHub Actions green
# ✅ Local testing completed
# ✅ User gave explicit approval

# Then merge
git checkout main
git pull origin main
git merge dev --no-ff -m "Merge dev: [brief summary]"
git push origin main

# Tag if it's a release
git tag v1.0.2
git push origin v1.0.2

# Back to dev
git checkout dev
```

## Emergency Hotfix (Rare)

If production is broken and needs immediate fix:

```bash
# Fix on main (emergency only)
git checkout main
git pull origin main

# Make minimal fix
# ... fix critical bug ...

# Commit and push
git add .
git commit -m "HOTFIX: Critical bug description"
git push origin main

# Merge back to dev immediately
git checkout dev
git merge main
git push origin dev
```

**Hotfixes are rare.** 99% of work should go through `dev` first.

## Why This Workflow?

**Before (single main branch):**
- ❌ Changes went straight to production
- ❌ No testing buffer
- ❌ Risky for users

**After (dev → main):**
- ✅ Safe testing on dev
- ✅ CI/CD validates before production
- ✅ User controls when changes go live
- ✅ Main always represents stable code

## Verifying GitHub Actions

After pushing to `dev`:

1. Go to: https://github.com/Arun270647/claude-permissions-app/actions
2. You should see workflow runs
3. Click on a run to see details
4. Green checkmark ✅ = success
5. Red X ❌ = failure (check logs)

**No actions showing?**
- Check if you changed code files (not just docs)
- Verify you pushed to `dev` or `main`
- Check workflow file syntax

## Branch Protection (Recommended)

To enforce this workflow on GitHub:

1. Go to: Settings → Branches
2. Add rule for `main`:
   - ✅ Require pull request before merging
   - ✅ Require status checks to pass
   - ✅ Require branches to be up to date
3. This forces PR review process

(Not enforced yet, but recommended for teams)

## Summary

| Branch | Purpose | Who Commits | When to Use |
|--------|---------|-------------|-------------|
| `dev` | Active development | Developers | Always (99% of time) |
| `main` | Production code | After approval only | Post-testing, explicit OK |

**Remember:** When in doubt, work on `dev`. Only touch `main` after explicit approval.

---

**Questions?** See CLAUDE.md for full instructions.
