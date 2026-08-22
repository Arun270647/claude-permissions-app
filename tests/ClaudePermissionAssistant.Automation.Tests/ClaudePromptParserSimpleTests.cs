using ClaudePermissionAssistant.Automation.Services;
using ClaudePermissionAssistant.Core.Models;

namespace ClaudePermissionAssistant.Automation.Tests;

public class ClaudePromptParserSimpleTests
{
    private readonly ClaudePromptParserSimple _parser;

    public ClaudePromptParserSimpleTests()
    {
        _parser = new ClaudePromptParserSimple();
    }

    [Fact]
    public void ContainsPromptMarkers_WithValidPrompt_ReturnsTrue()
    {
        var text = @"
Do you want to proceed?

> 1. Yes
  2. Yes, allow reading from /c/C: from this project
  3. No
";

        Assert.True(_parser.ContainsPromptMarkers(text));
    }

    [Fact]
    public void ContainsPromptMarkers_WithoutProceedQuestion_ReturnsFalse()
    {
        var text = @"
Some other text
  1. Yes
  2. Yes, allow reading from /c/C: from this project
";

        Assert.False(_parser.ContainsPromptMarkers(text));
    }

    [Fact]
    public void ContainsPromptMarkers_WithoutAllowReading_ReturnsFalse()
    {
        var text = @"
Do you want to proceed?
  1. Yes
  2. No
";

        Assert.False(_parser.ContainsPromptMarkers(text));
    }

    [Fact]
    public void IsValidPromptFormat_WithCompletePrompt_ReturnsTrue()
    {
        var text = @"
Do you want to proceed?

> 1. Yes
  2. Yes, allow reading from /c/C: from this project
  3. No
";

        Assert.True(_parser.IsValidPromptFormat(text));
    }

    [Fact]
    public void IsValidPromptFormat_WithDifferentPath_ReturnsTrue()
    {
        var text = @"
Do you want to proceed?

> 1. Yes
  2. Yes, allow reading from /c/Users/USER/my-project from this project
  3. No
";

        Assert.True(_parser.IsValidPromptFormat(text));
    }

    [Fact]
    public void ParsePermissionRequest_WithValidPrompt_ParsesCorrectly()
    {
        var text = @"
Do you want to proceed?

> 1. Yes
  2. Yes, allow reading from /c/C: from this project
  3. No
";

        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);
        Assert.Equal("Read", request.ToolName);
        Assert.Equal(3, request.Options.Length);
        Assert.Equal(ClaudePermissionPromptType.AllowReading, request.PromptType);
        Assert.True(request.HasAllowFromProjectOption);
        Assert.Equal(2, request.AllowFromProjectOptionNumber);
    }

    [Fact]
    public void ParsePermissionRequest_WithDifferentPath_FindsCorrectOption()
    {
        var text = @"
Do you want to proceed?

  1. Yes
  2. Yes, allow reading from /c/Users/USER/Documents/my-project from this project
  3. No
";

        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);
        Assert.Equal(2, request.AllowFromProjectOptionNumber);
    }

    [Fact]
    public void ParsePermissionRequest_WithWindowsPath_ParsesCorrectly()
    {
        var text = @"
Do you want to proceed?

  1. Yes
  2. Yes, allow reading from /c/C:/Projects/MyApp from this project
  3. No
";

        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);
        Assert.Equal(2, request.AllowFromProjectOptionNumber);
    }

    [Fact]
    public void ParsePermissionRequest_WithoutAllowFromProjectOption_ReturnsNull()
    {
        var text = @"
Do you want to proceed?

  1. Yes
  2. Maybe
  3. No
";

        var request = _parser.ParsePermissionRequest(text);

        Assert.Null(request);
    }

    [Fact]
    public void ParsePermissionRequest_WithNormalText_ReturnsNull()
    {
        var text = @"
This is just normal terminal output.
Do you want to proceed? is mentioned here but this is not a permission prompt.
";

        var request = _parser.ParsePermissionRequest(text);

        Assert.Null(request);
    }

    [Fact]
    public void ParsePermissionRequest_ParsesAllOptions()
    {
        var text = @"
Do you want to proceed?

  1. Yes
  2. Yes, allow reading from /c/C: from this project
  3. No
";

        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);
        Assert.Equal(3, request.Options.Length);
        Assert.Equal("Yes", request.Options[0].Text);
        Assert.Contains("allow reading from", request.Options[1].Text);
        Assert.Equal("No", request.Options[2].Text);
    }

    [Fact]
    public void ParsePermissionRequest_WithArrowIndicator_StillParses()
    {
        var text = @"
Do you want to proceed?

> 1. Yes
  2. Yes, allow reading from /c/C: from this project
  3. No
";

        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);
        Assert.Equal(2, request.AllowFromProjectOptionNumber);
    }

    [Fact]
    public void ParsePermissionRequest_CaseInsensitive()
    {
        var text = @"
do you want to proceed?

  1. yes
  2. Yes, Allow Reading From /c/C: From This Project
  3. no
";

        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);
        Assert.Equal(2, request.AllowFromProjectOptionNumber);
    }

    [Fact]
    public void ParsePermissionRequest_WithCommaVariation_ParsesCorrectly()
    {
        var text = @"
Do you want to proceed?

  1. Yes
  2. Yes allow reading from /c/C: from this project
  3. No
";

        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);
        Assert.Equal(2, request.AllowFromProjectOptionNumber);
    }
}
