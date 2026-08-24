# Contributing to Claude Permission Assistant

Thank you for considering contributing! This document provides guidelines for contributing to the project.

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Git
- Windows 10/11 (for Windows development)
- macOS 10.15+ (for macOS development)
- IDE: Visual Studio 2022, Rider, or VS Code

### Clone and Build

```bash
git clone https://github.com/Arun270647/claude-permissions-app.git
cd claude-permissions-app
dotnet build
dotnet test
```

## Development Workflow

### 1. Create a Branch

```bash
git checkout -b feature/your-feature-name
# or
git checkout -b fix/bug-description
```

### 2. Make Changes

- Write code following existing patterns
- Add tests for new functionality
- Ensure all tests pass: `dotnet test`
- Update documentation if needed

### 3. Test Your Changes

**Run tests:**
```bash
dotnet test
```

**Build Windows app:**
```bash
rebuild.bat
# Test: publish/win-x64/ClaudePermissionAssistant.exe
```

**Build macOS app (on Mac):**
```bash
./build-macos.sh
# Test the executables
```

### 4. Commit

```bash
git add .
git commit -m "Brief description of changes"
```

**Commit message format:**
- Use present tense ("Add feature" not "Added feature")
- Be concise but descriptive
- Reference issues: "Fix #123: Terminal detection bug"

### 5. Push and Create PR

```bash
git push origin feature/your-feature-name
```

Then create a Pull Request on GitHub.

## Code Style

### C# Conventions

- Follow [C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use nullable reference types (`#nullable enable`)
- Prefer `var` for obvious types
- Use modern C# features (pattern matching, records, etc.)

**Example:**
```csharp
// Good
public record PermissionRequest
{
    public required string ToolName { get; init; }
    public required PermissionOption[] Options { get; init; }
}

// Avoid
public class PermissionRequest
{
    public string ToolName { get; set; }
    public PermissionOption[] Options { get; set; }
}
```

### Naming

- **PascalCase**: Classes, methods, properties, public fields
- **camelCase**: Local variables, parameters
- **_camelCase**: Private fields
- **UPPER_CASE**: Constants

### Project Organization

```
src/
  Shared/      - Cross-platform code
  Windows/     - Windows-specific code
  macOS/       - macOS-specific code
```

Place code in the appropriate folder:
- Platform-agnostic models/interfaces → `Shared/Core`
- Windows automation → `Windows/Automation`
- macOS automation → `macOS/MacOS`

## Testing

### Writing Tests

- Use xUnit
- One test class per production class
- Name tests descriptively: `MethodName_Scenario_ExpectedBehavior`
- Test edge cases, not just happy paths

**Example:**
```csharp
[Fact]
public void ParsePermissionRequest_WithMalformedPrompt_ReturnsNull()
{
    var text = "This is not a Claude prompt";
    var result = _parser.ParsePermissionRequest(text);
    Assert.Null(result);
}
```

### Running Tests

```bash
# All tests
dotnet test

# Specific test class
dotnet test --filter "ClaudePromptParserSimpleTests"

# Specific test
dotnet test --filter "ParsePermissionRequest_WithPersistentOption_FindsCorrectOption"
```

## Pull Request Guidelines

### Before Submitting

- [ ] All tests pass (`dotnet test`)
- [ ] Code builds without warnings
- [ ] Manual testing completed (if applicable)
- [ ] Documentation updated (if needed)
- [ ] No debug code or commented-out code

### PR Description Template

```markdown
## Description
Brief description of what this PR does

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Testing
How did you test this?

## Screenshots (if applicable)
Add screenshots for UI changes

## Related Issues
Fixes #123
```

## Areas Open for Contribution

### High Priority

- **iTerm2 support** (macOS) - Extend AppleScript to work with iTerm2
- **Alacritty support** - Add support for Alacritty terminal
- **Better error messages** - User-friendly error reporting
- **Logging improvements** - Better diagnostic logs

### Medium Priority

- **Settings UI** - Let users configure polling interval, cooldown
- **Custom prompt patterns** - Support non-standard prompts
- **Multi-monitor improvements** - Better window focus handling
- **Performance optimization** - Reduce CPU usage during polling

### Low Priority

- **Themes** - Dark/light theme toggle
- **Keyboard shortcuts** - Add hotkeys for common actions
- **Export statistics** - CSV/JSON export of approval data
- **Localization** - Multi-language support

## Bug Reports

When filing a bug, include:

1. **Environment**
   - OS version (Windows 10/11, macOS version)
   - .NET version (`dotnet --version`)
   - Terminal type (CMD, PowerShell, Windows Terminal, Terminal.app)

2. **Steps to Reproduce**
   - Exact steps to trigger the bug
   - Expected behavior
   - Actual behavior

3. **Logs**
   - Check `%APPDATA%/ClaudePermissionAssistant/logs` (Windows)
   - Check `~/Library/Logs/ClaudePermissionAssistant` (macOS)
   - Include relevant log snippets

4. **Screenshots**
   - If UI-related, include screenshots

## Feature Requests

When requesting a feature:

1. **Use Case**: Describe the problem you're trying to solve
2. **Proposed Solution**: How you envision it working
3. **Alternatives**: Other approaches you've considered
4. **Additional Context**: Anything else relevant

## Architecture Guidelines

### Platform Abstraction

When adding cross-platform features:

1. Define interface in `Core/Interfaces`
2. Implement for Windows in `Windows/Automation`
3. Implement for macOS in `macOS/MacOS`
4. Use dependency injection to resolve platform-specific implementation

**Example:**
```csharp
// Core/Interfaces/ITerminalTextExtractor.cs
public interface ITerminalTextExtractor
{
    string? GetText(IntPtr windowHandle);
}

// Windows/WindowsTerminalTextExtractor.cs
public class WindowsTerminalTextExtractor : ITerminalTextExtractor
{
    public string? GetText(IntPtr windowHandle)
    {
        // Use UI Automation
    }
}

// macOS/MacOSTerminalTextExtractor.cs
public class MacOSTerminalTextExtractor : ITerminalTextExtractor
{
    public string? GetText(IntPtr windowHandle)
    {
        // Use AppleScript
    }
}
```

### Safety First

When modifying automation logic:

- **Never blind automation** - Always verify state before acting
- **Use global locks** - Prevent race conditions
- **Add cooldowns** - Prevent duplicate actions
- **Verify targets** - Confirm window/element identity
- **Log everything** - Diagnostic logs help debugging

## Code Review Process

1. **Automated checks** - GitHub Actions runs tests
2. **Maintainer review** - At least one maintainer approval required
3. **Feedback iteration** - Address review comments
4. **Merge** - Squash and merge to main

## Questions?

- **General questions**: Open a Discussion on GitHub
- **Bug reports**: Open an Issue
- **Security issues**: Email privately (see SECURITY.md)
- **Feature requests**: Open an Issue with "Feature Request" label

## Code of Conduct

Be respectful, constructive, and professional. We're all here to build something useful.

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

---

Thank you for contributing! 🎉
