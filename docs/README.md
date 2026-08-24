# Documentation

This directory contains all project documentation organized by category.

## 📁 Structure

```
docs/
├── README.md                    # This file
├── TECH_STACK.md               # Complete technical overview
├── PROJECT_STRUCTURE.md        # Codebase organization
├── CONTRIBUTING.md             # Contribution guidelines
├── CODE_OF_CONDUCT.md          # Community standards
├── SECURITY.md                 # Security policy
├── DEV_WORKFLOW.md            # Development setup and workflow
├── windows/                    # Windows-specific documentation
│   ├── BUILD_STATUS.md
│   ├── DISTRIBUTION_GUIDE.md
│   ├── PROJECT_SETUP_COMPLETE.md
│   └── PUBLISH_CHECKLIST.md
├── macos/                      # macOS-specific documentation
│   ├── MACOS_SETUP_COMPLETE.md
│   └── README_MACOS.md
└── fixes/                      # Historical bug fix documentation
    ├── FINAL_FIX.md
    ├── KEYSTROKE_SPAM_FIX.md
    └── TWO_TIER_COOLDOWN_FIX.md
```

## 📚 Documentation Index

### General

- **[TECH_STACK.md](TECH_STACK.md)** - Deep dive into the technology stack (C#, .NET, WPF, Avalonia, UI Automation, AppleScript)
- **[PROJECT_STRUCTURE.md](PROJECT_STRUCTURE.md)** - Codebase organization and architecture
- **[DEV_WORKFLOW.md](DEV_WORKFLOW.md)** - Development setup, auto-rebuild scripts, workflow guide
- **[CONTRIBUTING.md](CONTRIBUTING.md)** - How to contribute code, tests, documentation
- **[CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md)** - Community guidelines
- **[SECURITY.md](SECURITY.md)** - Security policy and vulnerability reporting

### Platform-Specific

#### Windows
- **[windows/BUILD_STATUS.md](windows/BUILD_STATUS.md)** - Windows build configuration status
- **[windows/DISTRIBUTION_GUIDE.md](windows/DISTRIBUTION_GUIDE.md)** - How to distribute the Windows app
- **[windows/PROJECT_SETUP_COMPLETE.md](windows/PROJECT_SETUP_COMPLETE.md)** - Project setup completion guide
- **[windows/PUBLISH_CHECKLIST.md](windows/PUBLISH_CHECKLIST.md)** - Release checklist for Windows

#### macOS
- **[macos/MACOS_SETUP_COMPLETE.md](macos/MACOS_SETUP_COMPLETE.md)** - macOS setup completion guide
- **[macos/README_MACOS.md](macos/README_MACOS.md)** - macOS-specific README

### Historical

#### Bug Fixes
- **[fixes/FINAL_FIX.md](fixes/FINAL_FIX.md)** - Final fix for foreground verification issue
- **[fixes/KEYSTROKE_SPAM_FIX.md](fixes/KEYSTROKE_SPAM_FIX.md)** - Fix for keystroke spam bug
- **[fixes/TWO_TIER_COOLDOWN_FIX.md](fixes/TWO_TIER_COOLDOWN_FIX.md)** - Two-tier cooldown implementation

These documents are kept for historical reference and learning about past issues.

## 🚀 Quick Links

### For Users
- [Installation Guide](../README.md#installation)
- [Quick Start](../README.md#quick-start)
- [FAQ](../README.md#faq)
- [Troubleshooting](../README.md#troubleshooting)

### For Developers
- [Development Workflow](DEV_WORKFLOW.md) - **Start here!**
- [Contributing Guide](CONTRIBUTING.md)
- [Tech Stack Overview](TECH_STACK.md)
- [Project Structure](PROJECT_STRUCTURE.md)

### For Distributors
- [Windows Distribution](windows/DISTRIBUTION_GUIDE.md)
- [Publishing Checklist](windows/PUBLISH_CHECKLIST.md)
- [Security Policy](SECURITY.md)

## 📝 Documentation Standards

When adding new documentation:

1. **Choose the right location:**
   - General docs → `docs/`
   - Windows-specific → `docs/windows/`
   - macOS-specific → `docs/macos/`
   - Bug fix history → `docs/fixes/`

2. **Follow naming conventions:**
   - Use `UPPER_SNAKE_CASE.md` for documentation files
   - Use descriptive names (e.g., `DISTRIBUTION_GUIDE.md` not `DIST.md`)

3. **Update this README:**
   - Add new docs to the appropriate section above
   - Keep the structure tree up to date

4. **Link properly:**
   - Use relative links (`[text](../path/file.md)`)
   - Test all links before committing

5. **Keep it current:**
   - Update docs when code changes
   - Archive outdated docs to `fixes/` with context

## 🔍 Finding Documentation

**Can't find what you're looking for?**

1. Check the main [README.md](../README.md)
2. Search this directory: `grep -r "your search term" docs/`
3. Check GitHub Issues: https://github.com/Arun270647/claude-permissions-app/issues

**Still stuck?**

Open an issue: https://github.com/Arun270647/claude-permissions-app/issues/new
