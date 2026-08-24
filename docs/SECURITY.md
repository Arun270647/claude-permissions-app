# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 1.0.x   | :white_check_mark: |

## Security Considerations

### What This App Does

Claude Permission Assistant monitors terminal windows and sends keyboard input to automatically approve Claude Code permission prompts. Understanding what it does is critical to understanding its security model.

**Data Access:**
- Reads text content from terminal windows you explicitly add to the monitoring list
- No network access - the app never sends data anywhere
- No file system access beyond its own log directory
- Does not persist terminal contents

**Actions Taken:**
- Sends keyboard input (number + Enter) only when Claude permission prompts are detected
- Uses a global lock to prevent simultaneous actions on multiple terminals
- 5-second cooldown prevents duplicate actions

### Privacy Guarantees

1. **Local-only operation** - No telemetry, no analytics, no network calls
2. **No data persistence** - Terminal text is parsed and discarded immediately
3. **No clipboard access** - The app does not read or write clipboard contents
4. **No keylogging** - The app only reads terminal text via OS accessibility APIs
5. **No screenshots** - The app does not capture images

### Required Permissions

**Windows:**
- UI Automation access (to read terminal text)
- SendInput permission (to send keyboard input)
- No admin rights required

**macOS:**
- Accessibility permissions (to read terminal text and send keyboard input via AppleScript)

### Risks and Mitigations

| Risk | Mitigation |
|------|-----------|
| **Wrong window automation** | Global execution lock ensures only one terminal is automated at a time. Window handle verification confirms target. |
| **Malicious prompt spoofing** | Parser uses strict regex patterns matching only genuine Claude Code prompts. Random text won't trigger actions. |
| **Keystroke spam** | 5-second cooldown prevents re-handling the same prompt. Prompts are marked as handled before approval. |
| **Unauthorized terminal monitoring** | Terminals must be explicitly added via the UI. The app doesn't auto-discover terminals. |

### Safe Usage Guidelines

1. **Only monitor terminals running Claude Code** - Don't add terminals running other applications
2. **Review what gets auto-approved** - The app only approves "allow from this project" options, not one-time "Yes" prompts
3. **Keep the dashboard minimized** - Opening the dashboard can steal focus from terminals
4. **Don't run as Administrator** - The app doesn't need elevated privileges

## Reporting a Vulnerability

### What to Report

Report security issues such as:
- Unauthorized data exfiltration
- Keystroke injection outside expected behavior
- Privilege escalation vulnerabilities
- Memory corruption bugs
- Crashes that could be exploited

**Do NOT report:**
- Feature requests
- General bugs (use GitHub Issues for these)
- Configuration questions

### How to Report

**For non-critical issues:**
- Open a GitHub Issue: https://github.com/Arun270647/claude-permissions-app/issues
- Tag with "security" label

**For critical vulnerabilities:**
- **Do not** open a public issue
- Email: [Your security contact email - add this before publishing]
- Include: Detailed description, steps to reproduce, impact assessment, proof of concept (if available)

### Response Timeline

- **Acknowledgment:** Within 48 hours
- **Initial assessment:** Within 1 week
- **Fix timeline:** Depends on severity
  - Critical: Immediate (within days)
  - High: Within 2 weeks
  - Medium: Within 1 month
  - Low: Next release

### Disclosure Policy

- We follow coordinated disclosure
- Security fixes are released as patches without detailed exploit information
- Once a patch is available and most users have upgraded (~30 days), we publish a security advisory with full details
- Credit is given to reporters (unless they prefer anonymity)

## Security Best Practices for Contributors

If you're contributing code:

1. **Never log sensitive data** - Don't log full terminal contents, only detection events
2. **Validate all inputs** - Parse prompts defensively, assume malicious input
3. **Use safe APIs** - Prefer `SendInput` over `SendKeys`, use UI Automation over screen scraping
4. **Test edge cases** - What happens if a prompt has 100 options? What if the terminal closes mid-automation?
5. **Fail safely** - If something goes wrong, log and stop - don't keep retrying blindly

## Third-Party Dependencies

This project uses only official .NET libraries:
- No external NuGet packages
- No JavaScript/npm dependencies
- No C/C++ native libraries (except Windows/macOS system APIs)

The attack surface is minimal - only .NET runtime and OS-provided APIs.

## Audit History

| Date | Auditor | Scope | Findings |
|------|---------|-------|----------|
| 2024-XX-XX | Initial dev | Full codebase review | N/A - first release |

Future security audits will be listed here.

## Security Updates

Security patches are released as follows:

1. **GitHub Releases** - Binary updates with [SECURITY] tag
2. **README.md** - Updated with security advisory link
3. **This file** - Updated with patch notes

Subscribe to releases: https://github.com/Arun270647/claude-permissions-app/releases

---

**Remember:** This app has access to your terminal contents and can send keyboard input. Only use it with terminals running Claude Code in projects you trust.
