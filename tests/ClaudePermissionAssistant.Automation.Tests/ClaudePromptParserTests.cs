using ClaudePermissionAssistant.Automation.Services;
using ClaudePermissionAssistant.Core.Models;

namespace ClaudePermissionAssistant.Automation.Tests;

public class ClaudePromptParserTests
{
    private readonly ClaudePromptParser _parser;

    public ClaudePromptParserTests()
    {
        _parser = new ClaudePromptParser();
    }

    [Fact]
    public void ContainsPromptMarkers_WithValidPrompt_ReturnsTrue()
    {
        var text = @"
Claude Code wants to use Bash

Description: Execute bash command

  1. Allow
  2. Deny
";

        Assert.True(_parser.ContainsPromptMarkers(text));
    }

    [Fact]
    public void ContainsPromptMarkers_WithInvalidText_ReturnsFalse()
    {
        var text = "This is just regular terminal output";

        Assert.False(_parser.ContainsPromptMarkers(text));
    }

    [Fact]
    public void ContainsPromptMarkers_WithEmptyString_ReturnsFalse()
    {
        Assert.False(_parser.ContainsPromptMarkers(string.Empty));
        Assert.False(_parser.ContainsPromptMarkers(null!));
    }

    [Fact]
    public void IsValidPromptFormat_WithCompletePrompt_ReturnsTrue()
    {
        var text = @"
Claude Code wants to use Bash

Description: Execute bash command

  1. Allow
  2. Deny
";

        Assert.True(_parser.IsValidPromptFormat(text));
    }

    [Fact]
    public void IsValidPromptFormat_WithMissingOptions_ReturnsFalse()
    {
        var text = @"
Claude Code wants to use Bash

Description: Execute bash command
";

        Assert.False(_parser.IsValidPromptFormat(text));
    }

    [Fact]
    public void IsValidPromptFormat_WithMissingToolName_ReturnsFalse()
    {
        var text = @"
Description: Execute bash command

  1. Allow
  2. Deny
";

        Assert.False(_parser.IsValidPromptFormat(text));
    }

    [Fact]
    public void ParsePermissionRequest_WithValidPrompt_ParsesCorrectly()
    {
        var text = @"
Claude Code wants to use Bash

Description: Execute bash command

  1. Allow
  2. Deny
  3. Always allow
  4. Never allow

Enter your choice:
";

        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);
        Assert.Equal("Bash", request.ToolName);
        Assert.Equal("Execute bash command", request.Description);
        Assert.Equal(4, request.Options.Length);
        Assert.True(request.IsValid);
    }

    [Fact]
    public void ParsePermissionRequest_WithDifferentFormat_ParsesToolName()
    {
        var text = @"
Claude wants to use Read

Description: Read file contents

  1) Allow
  2) Deny
";

        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);
        Assert.Equal("Read", request.ToolName);
    }

    [Fact]
    public void ParsePermissionRequest_ParsesOptionsWithCorrectActions()
    {
        var text = @"
Claude Code wants to use Edit

Description: Edit file

  1. Allow
  2. Deny
  3. Always allow
  4. Never allow
";

        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);
        Assert.Equal(PermissionAction.Allow, request.Options[0].Action);
        Assert.Equal(PermissionAction.Deny, request.Options[1].Action);
        Assert.Equal(PermissionAction.AlwaysAllow, request.Options[2].Action);
        Assert.Equal(PermissionAction.NeverAllow, request.Options[3].Action);
    }

    [Fact]
    public void ParsePermissionRequest_ParsesOptionNumbers()
    {
        var text = @"
Claude Code wants to use Write

Description: Write file

  1. Allow
  2. Deny
  3. Always allow
";

        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);
        Assert.Equal(1, request.Options[0].Number);
        Assert.Equal(2, request.Options[1].Number);
        Assert.Equal(3, request.Options[2].Number);
    }

    [Fact]
    public void ParsePermissionRequest_WithInvalidText_ReturnsNull()
    {
        var text = "This is not a permission prompt";

        var request = _parser.ParsePermissionRequest(text);

        Assert.Null(request);
    }

    [Fact]
    public void ParsePermissionRequest_WithEmptyString_ReturnsNull()
    {
        Assert.Null(_parser.ParsePermissionRequest(string.Empty));
        Assert.Null(_parser.ParsePermissionRequest(null!));
    }

    [Fact]
    public void ParsePermissionRequest_WithMissingAllowOption_ReturnsNull()
    {
        var text = @"
Claude Code wants to use Test

Description: Test tool

  1. Deny only
  2. Cancel
";

        var request = _parser.ParsePermissionRequest(text);

        Assert.Null(request);
    }

    [Fact]
    public void ParsePermissionRequest_WithMissingDenyOption_ReturnsNull()
    {
        var text = @"
Claude Code wants to use Test

Description: Test tool

  1. Allow only
  2. Always allow
";

        var request = _parser.ParsePermissionRequest(text);

        Assert.Null(request);
    }

    [Fact]
    public void ParsePermissionRequest_HandlesRealWorldFormat()
    {
        var text = @"
Claude Code wants to use Bash

Description: Run a bash command

Command: ls -la

Working Directory: /home/user

  1. Allow
  2. Deny
  3. Always allow for this session
  4. Never allow

Enter your choice (1-4):
";

        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);
        Assert.Equal("Bash", request.ToolName);
        Assert.Contains("Run a bash command", request.Description);
        Assert.True(request.IsValid);
        Assert.True(request.HasAllowOption);
        Assert.True(request.HasDenyOption);
    }

    [Fact]
    public void ParsePermissionRequest_HandlesVariousOptionFormats()
    {
        var text = @"
Claude Code wants to use Read

Description: Read file

  1) Allow
  2) Deny
  3) Always
";

        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);
        Assert.Equal(3, request.Options.Length);
    }

    [Fact]
    public void ParsePermissionRequest_ParsesOptionTextCorrectly()
    {
        var text = @"
Claude Code wants to use Test

Description: Test

  1. Allow
  2. Deny
  3. Always allow
  4. Never allow
";

        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);
        Assert.Equal("Allow", request.Options[0].Text);
        Assert.Equal("Deny", request.Options[1].Text);
        Assert.Equal("Always allow", request.Options[2].Text);
        Assert.Equal("Never allow", request.Options[3].Text);
    }
}
