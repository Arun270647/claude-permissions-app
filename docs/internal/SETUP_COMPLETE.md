# Dev Branch Workflow - Setup Complete ✅

**Date:** 2026-08-25  
**Status:** Fully configured and active

## What Was Set Up

### 1. ✅ Dev Branch Created
```bash
dev   ← All development work (ACTIVE)
main  ← Production code (only after approval)
```

**Current branch:** `dev` (where you should always work)

### 2. ✅ GitHub Actions Fixed

**Updated workflows to trigger on:**
- Push to `dev` branch ← **NEW**
- Push to `main` branch
- Pull requests to `main` ← **NEW**

**Why Actions are now visible:**
- Before: Only triggered on deleted branches (`windows`, `macos`)
- Now: Triggers on `dev` and `main`
- Path filters still active (only builds when code changes)

**Latest commit** (24e5b9e) changed `src/Shared/` which triggers **BOTH** Windows and macOS builds!

### 3. ✅ Documentation Created

**CLAUDE.md** - Main instructions for Claude Code:
- Project overview
- Dev branch workflow (CRITICAL: never commit to main)
- Build instructions
- Security guidelines
- Current status

**docs/internal/DEV_BRANCH_WORKFLOW.md** - Detailed workflow:
- Branch strategy
- Merge process
- GitHub Actions troubleshooting
- Emergency hotfix process

### 4. ✅ Memory Updated

**feedback_branch_workflow.md:**
- Always work on `dev` branch
- Only merge to `main` after explicit user approval
- Never assume changes are ready for main

**project_branch_structure.md:**
- Dev/main branch strategy
- Platform folders structure
- CI/CD path filters

## How to Verify GitHub Actions

1. **Go to GitHub Actions:**
   https://github.com/Arun270647/claude-permissions-app/actions

2. **You should see:**
   - "Add XML documentation to DetectedPrompt model" workflow runs
   - Two builds: **Build Windows** and **Build macOS** (both for x64 and arm64)
   - Status: Running or Completed

3. **Click on a workflow run** to see:
   - Build logs
   - Test results
   - Artifacts (if any)

## Current Repository State

```
Repository: claude-permissions-app
├── Branches:
│   ├── dev  ← ✅ ACTIVE (you are here)
│   └── main ← Production (needs approval to update)
│
├── Latest commits on dev:
│   ├── 24e5b9e - Add XML documentation (triggers CI) ← YOU ARE HERE
│   ├── e726684 - Set up dev branch workflow
│   └── 879a1ab - Document branch deletion
│
└── GitHub Actions:
    ├── build-windows.yml (triggers on dev/main pushes)
    └── build-macos.yml (triggers on dev/main pushes)
```

## Your Workflow Going Forward

### Every Day Development

```bash
# 1. Ensure on dev branch
git checkout dev
git pull origin dev

# 2. Make changes
# ... edit files ...

# 3. Test locally
dotnet build
dotnet test

# 4. Commit and push to dev
git add .
git commit -m "Descriptive message"
git push origin dev

# 5. Check GitHub Actions
# https://github.com/Arun270647/claude-permissions-app/actions
# Both Windows and macOS should build (if you changed shared/platform code)

# 6. Test the app locally
# ... download artifacts or build locally ...

# 7. When ready for production, say:
# "merge to main" or "push to main"
```

### When Ready for Production

**You will explicitly say one of these:**
- "merge to main"
- "push to main"
- "ready for production"
- "deploy to main"

**Only then will I run:**
```bash
git checkout main
git pull origin main
git merge dev --no-ff
git push origin main
```

## Verification Checklist

To verify everything is working:

- [x] Dev branch exists and is active
- [x] GitHub Actions configured for dev
- [x] CLAUDE.md created
- [x] Memory updated
- [x] Documentation created
- [x] Code change pushed (triggers CI)
- [ ] **YOU:** Check GitHub Actions page - builds should be running/completed

## Next Steps

1. **Check GitHub Actions:**
   Visit: https://github.com/Arun270647/claude-permissions-app/actions
   You should see workflows running for commit 24e5b9e

2. **Continue Development:**
   All work on `dev` branch from now on

3. **When Ready:**
   Tell me "merge to main" and I'll merge after you approve

## Questions?

**Q: Why didn't I see Actions before?**  
A: Workflows were configured for deleted branches (`windows`, `macos`). Now they trigger on `dev`.

**Q: What if I only change docs?**  
A: Actions won't run (path filters). Only code changes trigger builds.

**Q: Can I commit directly to main?**  
A: **NO.** Always work on `dev`, merge to `main` only after approval.

**Q: How do I trigger Actions?**  
A: Change any file in `src/Windows/`, `src/macOS/`, or `src/Shared/`, then push to `dev`.

---

**Setup Status:** ✅ **COMPLETE**  
**Current Branch:** `dev` (active development)  
**GitHub Actions:** Should be visible now!

**Go check:** https://github.com/Arun270647/claude-permissions-app/actions
