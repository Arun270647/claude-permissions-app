# Documentation

This directory contains all project documentation organized by category.

## 📁 Structure

```
docs/
├── README.md                       # This file
├── TECH_STACK.md                   # Complete technical overview
├── PROJECT_STRUCTURE.md            # Codebase organization
├── CONTRIBUTING.md                 # Contribution guidelines
├── CODE_OF_CONDUCT.md              # Community standards
├── SECURITY.md                     # Security policy
├── DEV_WORKFLOW.md                 # Development setup and workflow
├── AUTO_UPDATE_ENABLED.md          # Auto-update system documentation
├── BRANCHING_STRATEGY.md           # Git workflow and branch structure
├── BRANCH_STRUCTURE_COMPLETE.md    # Branch structure completion guide
├── MACOS_DOWNLOADS.md              # macOS distribution guide
├── MACOS_SETUP.md                  # macOS installation instructions
├── CREATE_FIRST_RELEASE.md         # Guide for creating releases
├── CREATE_RELEASE.md               # Detailed release process
├── GITHUB_PAGES_SETUP.md           # GitHub Pages configuration
├── RELEASE_NOTES_v1.0.0.md         # Initial release notes
├── windows/                        # Windows-specific documentation
│   ├── BUILD_STATUS.md
│   ├── DISTRIBUTION_GUIDE.md
│   ├── PROJECT_SETUP_COMPLETE.md
│   └── PUBLISH_CHECKLIST.md
├── macos/                          # macOS-specific documentation
│   ├── MACOS_SETUP_COMPLETE.md
│   └── README_MACOS.md
└── fixes/                          # Historical bug fix documentation
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

### Features & Systems

- **[AUTO_UPDATE_ENABLED.md](AUTO_UPDATE_ENABLED.md)** - Auto-update system documentation (v1.0.1+)
- **[BRANCHING_STRATEGY.md](BRANCHING_STRATEGY.md)** - Git workflow: main/windows/macos/web branches
- **[BRANCH_STRUCTURE_COMPLETE.md](BRANCH_STRUCTURE_COMPLETE.md)** - Branch structure implementation

### Platform-Specific

#### Windows
- **[windows/BUILD_STATUS.md](windows/BUILD_STATUS.md)** - Windows build configuration status
- **[windows/DISTRIBUTION_GUIDE.md](windows/DISTRIBUTION_GUIDE.md)** - How to distribute the Windows app
- **[windows/PROJECT_SETUP_COMPLETE.md](windows/PROJECT_SETUP_COMPLETE.md)** - Project setup completion guide
- **[windows/PUBLISH_CHECKLIST.md](windows/PUBLISH_CHECKLIST.md)** - Release checklist for Windows

#### macOS
- **[MACOS_SETUP.md](MACOS_SETUP.md)** - macOS installation and setup instructions
- **[MACOS_DOWNLOADS.md](MACOS_DOWNLOADS.md)** - macOS distribution guide (.dmg packaging)
- **[macos/MACOS_SETUP_COMPLETE.md](macos/MACOS_SETUP_COMPLETE.md)** - macOS setup completion guide
- **[macos/README_MACOS.md](macos/README_MACOS.md)** - macOS-specific README

### Release Management

- **[CREATE_FIRST_RELEASE.md](CREATE_FIRST_RELEASE.md)** - Guide for creating the first release
- **[CREATE_RELEASE.md](CREATE_RELEASE.md)** - Detailed release process documentation
- **[RELEASE_NOTES_v1.0.0.md](RELEASE_NOTES_v1.0.0.md)** - Initial release notes
- **[GITHUB_PAGES_SETUP.md](GITHUB_PAGES_SETUP.md)** - GitHub Pages configuration (historical)

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
- [macOS Setup Guide](MACOS_SETUP.md)

### For Developers
- [Development Workflow](DEV_WORKFLOW.md) - **Start here!**
- [Contributing Guide](CONTRIBUTING.md)
- [Tech Stack Overview](TECH_STACK.md)
- [Project Structure](PROJECT_STRUCTURE.md)
- [Branch Strategy](BRANCHING_STRATEGY.md)

### For Distributors
- [Windows Distribution](windows/DISTRIBUTION_GUIDE.md)
- [macOS Distribution](MACOS_DOWNLOADS.md)
- [Publishing Checklist](windows/PUBLISH_CHECKLIST.md)
- [Creating Releases](CREATE_RELEASE.md)
- [Security Policy](SECURITY.md)

### For Maintainers
- [Auto-Update System](AUTO_UPDATE_ENABLED.md)
- [Release Process](CREATE_RELEASE.md)
- [Branch Management](BRANCHING_STRATEGY.md)

## 📝 Documentation Standards

When adding new documentation:

1. **Choose the right location:**
   - General docs → `docs/`
   - Windows-specific → `docs/windows/`
   - macOS-specific → `docs/macos/`
   - Bug fix history → `docs/fixes/`
   - Feature documentation → `docs/` (root level)

2. **Follow naming conventions:**
   - Use `UPPER_SNAKE_CASE.md` for documentation files
   - Use descriptive names (e.g., `DISTRIBUTION_GUIDE.md` not `DIST.md`)
   - Version-specific docs: `RELEASE_NOTES_v1.0.0.md`

3. **Update this README:**
   - Add new docs to the appropriate section above
   - Keep the structure tree up to date
   - Add to Quick Links if relevant

4. **Link properly:**
   - Use relative links (`[text](../path/file.md)`)
   - Test all links before committing
   - Cross-reference related documents

5. **Keep it current:**
   - Update docs when code changes
   - Archive outdated docs to `fixes/` with context
   - Mark deprecated features clearly

6. **Follow markdown standards:**
   - Use proper heading hierarchy (H1 → H2 → H3)
   - Add table of contents for long documents
   - Use code blocks with syntax highlighting
   - Include examples where applicable

## 🔍 Finding Documentation

**Can't find what you're looking for?**

1. **Check the main [README.md](../README.md)** - Start here for overview
2. **Search this directory:**
   ```bash
   grep -r "your search term" docs/
   ```
3. **Check GitHub Issues:** https://github.com/Arun270647/claude-permissions-app/issues
4. **Browse by topic:**
   - Setup & Installation → [MACOS_SETUP.md](MACOS_SETUP.md), [windows/](windows/)
   - Development → [DEV_WORKFLOW.md](DEV_WORKFLOW.md), [CONTRIBUTING.md](CONTRIBUTING.md)
   - Architecture → [TECH_STACK.md](TECH_STACK.md), [PROJECT_STRUCTURE.md](PROJECT_STRUCTURE.md)
   - Distribution → [windows/DISTRIBUTION_GUIDE.md](windows/DISTRIBUTION_GUIDE.md), [MACOS_DOWNLOADS.md](MACOS_DOWNLOADS.md)
   - Features → [AUTO_UPDATE_ENABLED.md](AUTO_UPDATE_ENABLED.md), [BRANCHING_STRATEGY.md](BRANCHING_STRATEGY.md)

**Still stuck?**

Open an issue: https://github.com/Arun270647/claude-permissions-app/issues/new

## 📊 Documentation Status

### ✅ Complete
- Core documentation (README, TECH_STACK, PROJECT_STRUCTURE)
- Platform-specific guides (Windows, macOS)
- Development workflow and contributing guide
- Auto-update system documentation
- Branch strategy and structure
- Release management process

### 🚧 In Progress
- Video tutorials
- Advanced troubleshooting guide
- Performance optimization guide
- Security best practices

### 📋 Planned
- API documentation (if applicable)
- Plugin/Extension development guide
- Localization guide
- Advanced customization guide
- Docker deployment guide (future)
- Kubernetes setup (future)

## 🎯 Documentation Goals

1. **Comprehensive** - Cover all aspects of the project
2. **Up-to-date** - Reflect current codebase state
3. **Accessible** - Easy to find and understand
4. **Practical** - Include examples and real-world scenarios
5. **Maintainable** - Easy to update and extend

## 🙏 Contributing to Documentation

Documentation contributions are highly valued! To contribute:

1. **Find what needs improvement:**
   - Check [open documentation issues](https://github.com/Arun270647/claude-permissions-app/issues?q=is%3Aissue+is%3Aopen+label%3Adocumentation)
   - Look for missing topics in this README
   - Update outdated information

2. **Make your changes:**
   - Fork the repository
   - Create a branch: `docs/your-improvement`
   - Write or update documentation
   - Test all links and code examples
   - Update this README if adding new docs

3. **Submit your contribution:**
   - Open a Pull Request
   - Describe what you've documented
   - Link to related issues if applicable

See [CONTRIBUTING.md](CONTRIBUTING.md) for detailed guidelines.

## 📈 Recent Updates

### v1.0.1 (Current)
- ✅ Added AUTO_UPDATE_ENABLED.md
- ✅ Updated main README with new name "Claude Prompter"
- ✅ Added MACOS_DOWNLOADS.md for DMG packaging
- ✅ Updated installation instructions
- ✅ Added roadmap section
- ✅ Updated all download links to v1.0.1

### v1.0.0 (Initial Release)
- ✅ Created comprehensive documentation structure
- ✅ Added TECH_STACK.md with detailed technical overview
- ✅ Created platform-specific documentation
- ✅ Added contribution guidelines
- ✅ Created bug fix history archive

---

<p align="center">
  <strong>Claude Prompter v1.0.1</strong> - Well-documented, community-friendly
</p>

<p align="center">
  Questions? Check the <a href="../README.md#faq">FAQ</a> or <a href="https://github.com/Arun270647/claude-permissions-app/issues">open an issue</a>
</p>
