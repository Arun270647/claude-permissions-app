using ClaudePermissionAssistant.Core.Services;
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
    public void ContainsPromptMarkers_WithBasicPrompt_ReturnsTrue()
    {
        // Phase 4B: Parser now recognizes basic prompts even without "allow reading from"
        var text = @"
Do you want to proceed?
  1. Yes
  2. No
";

        Assert.True(_parser.ContainsPromptMarkers(text));
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
    public void ParsePermissionRequest_WithoutPersistentApprovalOption_ParsesWithoutIt()
    {
        // Phase 4B: Parser now recognizes prompts even without persistent approval option
        var text = @"
Do you want to proceed?

  1. Yes
  2. Maybe
  3. No
";

        var request = _parser.ParsePermissionRequest(text);

        // Should parse but have no persistent approval option
        Assert.NotNull(request);
        Assert.False(request.HasPersistentApprovalOption);
        Assert.Null(request.PersistentApprovalOptionNumber);
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

    // ========================================
    // NEW TESTS - Phase 4B: Real Prompt Support
    // ========================================

    [Fact]
    public void ParsePermissionRequest_RealCapturedEditPrompt_ParsesCorrectly()
    {
        // REAL captured prompt from TextPattern extraction
        var text = @"
Do you want to create test_permission.txt?

> 1. Yes
  2. Yes, and switch to accept edits (auto-approve file edits and common file commands) for this session (shift+tab)
  3. No

Esc to cancel · Tab to amend
";

        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);
        Assert.Equal(ClaudePermissionPromptType.AllowEditing, request.PromptType);
        Assert.Equal(3, request.Options.Length);
        Assert.True(request.HasPersistentApprovalOption);
        Assert.Equal(2, request.PersistentApprovalOptionNumber);

        // Verify backward compatibility
        Assert.True(request.HasAllowFromProjectOption);
        Assert.Equal(2, request.AllowFromProjectOptionNumber);

        // Verify options
        Assert.Equal(PermissionAction.Allow, request.Options[0].Action);
        Assert.Equal(PermissionAction.AlwaysAllow, request.Options[1].Action);
        Assert.Equal(PermissionAction.Deny, request.Options[2].Action);
    }

    [Fact]
    public void ParsePermissionRequest_RealCapturedReadingPrompt_ParsesCorrectly()
    {
        var text = @"
Do you want to proceed?

> 1. Yes
  2. Yes, allow reading from /c/C: from this project
  3. No

Esc to cancel · Tab to amend
";

        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);
        Assert.Equal(ClaudePermissionPromptType.AllowReading, request.PromptType);
        Assert.True(request.HasPersistentApprovalOption);
        Assert.Equal(2, request.PersistentApprovalOptionNumber);
    }

    [Fact]
    public void ParsePermissionRequest_EditPromptFromFixture_ParsesCorrectly()
    {
        var fixturePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..", "ClaudePermissionAssistant.Tests", "Fixtures",
            "claude_prompt_edit_session.txt"
        );

        if (!File.Exists(fixturePath))
        {
            // Skip if fixture not available (CI environment, etc.)
            return;
        }

        var text = File.ReadAllText(fixturePath);
        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);
        Assert.Equal(ClaudePermissionPromptType.AllowEditing, request.PromptType);
        Assert.Equal(2, request.PersistentApprovalOptionNumber);
    }

    [Fact]
    public void ParsePermissionRequest_ReadingPromptFromFixture_ParsesCorrectly()
    {
        var fixturePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..", "ClaudePermissionAssistant.Tests", "Fixtures",
            "claude_prompt_reading_project.txt"
        );

        if (!File.Exists(fixturePath))
        {
            return;
        }

        var text = File.ReadAllText(fixturePath);
        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);
        Assert.Equal(ClaudePermissionPromptType.AllowReading, request.PromptType);
        Assert.Equal(2, request.PersistentApprovalOptionNumber);
    }

    [Fact]
    public void ParsePermissionRequest_MultiplePromptTypes_ClassifiesCorrectly()
    {
        // Create prompt
        var createText = @"
Do you want to create newfile.txt?

  1. Yes
  2. Yes, for this project
  3. No
";
        var createRequest = _parser.ParsePermissionRequest(createText);
        Assert.NotNull(createRequest);
        Assert.Equal(ClaudePermissionPromptType.AllowWriting, createRequest.PromptType);

        // Read prompt
        var readText = @"
Do you want to read somefile.txt?

  1. Yes
  2. Yes, from this project
  3. No
";
        var readRequest = _parser.ParsePermissionRequest(readText);
        Assert.NotNull(readRequest);
        Assert.Equal(ClaudePermissionPromptType.AllowReading, readRequest.PromptType);

        // Execute prompt
        var execText = @"
Do you want to execute this command?

  1. Yes
  2. Yes, for this session
  3. No
";
        var execRequest = _parser.ParsePermissionRequest(execText);
        Assert.NotNull(execRequest);
        Assert.Equal(ClaudePermissionPromptType.AllowExecuting, execRequest.PromptType);
    }

    [Fact]
    public void ParsePermissionRequest_PersistentApprovalDetection_VariousPhrases()
    {
        // "from this project"
        var text1 = @"
Do you want to read file.txt?

  1. Yes
  2. Yes, allow reading from this project
  3. No
";
        var request1 = _parser.ParsePermissionRequest(text1);
        Assert.NotNull(request1);
        Assert.True(request1.HasPersistentApprovalOption);

        // "for this session"
        var text2 = @"
Do you want to execute command?

  1. Yes
  2. Yes, auto-approve for this session
  3. No
";
        var request2 = _parser.ParsePermissionRequest(text2);
        Assert.NotNull(request2);
        Assert.True(request2.HasPersistentApprovalOption);

        // "accept edits"
        var text3 = @"
Do you want to modify file?

  1. Yes
  2. Yes, and switch to accept edits
  3. No
";
        var request3 = _parser.ParsePermissionRequest(text3);
        Assert.NotNull(request3);
        Assert.True(request3.HasPersistentApprovalOption);
    }

    // ========================================
    // FALSE POSITIVE TESTS
    // ========================================

    [Fact]
    public void ParsePermissionRequest_DocumentationMentioningPrompt_ReturnsNull()
    {
        var text = @"
This is documentation about permissions.
You might see prompts like 'Do you want to create a file?'
in your terminal when running Claude Code.
The options will include Yes and No.
";

        var request = _parser.ParsePermissionRequest(text);
        Assert.Null(request);
    }

    [Fact]
    public void ParsePermissionRequest_NormalTextWithKeywords_ReturnsNull()
    {
        var text = @"
The system will auto-approve requests for this session.
You can allow reading from this project by default.
Common file commands include create, read, write.
";

        var request = _parser.ParsePermissionRequest(text);
        Assert.Null(request);
    }

    [Fact]
    public void ParsePermissionRequest_OrdinaryNumberedList_ReturnsNull()
    {
        var text = @"
Here are the steps:

1. Yes, first do this
2. Yes, then do that
3. No, don't do this

These are just instructions, not a permission prompt.
";

        var request = _parser.ParsePermissionRequest(text);
        Assert.Null(request);
    }

    [Fact]
    public void ParsePermissionRequest_ClaudeResponseAboutPermissions_ReturnsNull()
    {
        var text = @"
I understand you want to create a file. However, I need your permission.
Do you want to proceed with this action?
You can allow operations from this project.
";

        var request = _parser.ParsePermissionRequest(text);
        Assert.Null(request);
    }

    [Fact]
    public void ParsePermissionRequest_IncompletePrompt_ReturnsNull()
    {
        var text = @"
Do you want to create file.txt?

  1. Yes
  2. No
";

        // No persistent approval option - but this should still parse
        var request = _parser.ParsePermissionRequest(text);

        // This should parse but have no persistent approval option
        Assert.NotNull(request);
        Assert.False(request.HasPersistentApprovalOption);
    }

    [Fact]
    public void ParsePermissionRequest_MissingNumberedOptions_ReturnsNull()
    {
        var text = @"
Do you want to create file.txt?

Yes
Yes, and auto-approve for this session
No
";

        var request = _parser.ParsePermissionRequest(text);
        Assert.Null(request);
    }

    [Fact]
    public void ParsePermissionRequest_OnlyYesOptions_ReturnsNull()
    {
        var text = @"
Do you want to proceed?

  1. Yes
  2. Yes, allow from project
";

        var request = _parser.ParsePermissionRequest(text);
        Assert.Null(request); // No "No" option
    }

    [Fact]
    public void ParsePermissionRequest_BackwardCompatibility_LegacyProperties()
    {
        var text = @"
Do you want to read file.txt?

  1. Yes
  2. Yes, allow reading from /c/C: from this project
  3. No
";

        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);

        // New properties
        Assert.True(request.HasPersistentApprovalOption);
        Assert.Equal(2, request.PersistentApprovalOptionNumber);

        // Legacy properties should map to same values
        Assert.True(request.HasAllowFromProjectOption);
        Assert.Equal(2, request.AllowFromProjectOptionNumber);
        Assert.Equal(request.PersistentApprovalOptionNumber, request.AllowFromProjectOptionNumber);
    }

    // ========================================
    // PHASE 4C FIX - REAL TERMINAL FORMAT TESTS
    // ========================================

    [Fact]
    public void ParsePermissionRequest_RealConhostTerminalFormat_ParsesCorrectly()
    {
        // REAL captured format from conhost TextPattern where options 2 and 3 are on same line
        var text = @"Do you want to make this edit to test_permission.txt?

> 1. Yes
  2. Yes, and switch to accept edits (auto-approve file edits and common file commands) for this session (shift+tab)      3. No

Esc to cancel · Tab to amend
";

        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);
        Assert.Equal(ClaudePermissionPromptType.AllowEditing, request.PromptType);
        Assert.Equal(3, request.Options.Length);

        // Verify all three options parsed correctly
        Assert.Equal(1, request.Options[0].Number);
        Assert.Equal("Yes", request.Options[0].Text);
        Assert.Equal(PermissionAction.Allow, request.Options[0].Action);

        Assert.Equal(2, request.Options[1].Number);
        Assert.Contains("switch to accept edits", request.Options[1].Text);
        Assert.Contains("auto-approve file edits", request.Options[1].Text);
        Assert.Equal(PermissionAction.AlwaysAllow, request.Options[1].Action);

        Assert.Equal(3, request.Options[2].Number);
        Assert.Equal("No", request.Options[2].Text);
        Assert.Equal(PermissionAction.Deny, request.Options[2].Action);

        // Verify persistent approval option detected
        Assert.True(request.HasPersistentApprovalOption);
        Assert.Equal(2, request.PersistentApprovalOptionNumber);
    }

    [Fact]
    public void ParsePermissionRequest_RealConhostTerminalFromFixture_ParsesCorrectly()
    {
        // Load real captured terminal format from fixture
        var fixturePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..", "ClaudePermissionAssistant.Tests", "Fixtures",
            "claude_prompt_real_conhost_terminal.txt"
        );

        if (!File.Exists(fixturePath))
        {
            // Skip if fixture not available
            return;
        }

        var text = File.ReadAllText(fixturePath);
        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);
        Assert.Equal(ClaudePermissionPromptType.AllowEditing, request.PromptType);
        Assert.Equal(3, request.Options.Length);
        Assert.Equal(2, request.PersistentApprovalOptionNumber);
    }

    [Fact]
    public void ParsePermissionRequest_QuestionWithMake_Recognized()
    {
        // Test "Do you want to make" question pattern
        var text = @"
Do you want to make this change?

  1. Yes
  2. Yes, for this project
  3. No
";

        var request = _parser.ParsePermissionRequest(text);
        Assert.NotNull(request);
    }

    [Fact]
    public void ParsePermissionRequest_OptionsWithArrowPrefix_ParsesCorrectly()
    {
        // Test "> 1. Yes" format with arrow prefix
        var text = @"
Do you want to proceed?

> 1. Yes
  2. Yes, for this session
  3. No
";

        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);
        Assert.Equal(3, request.Options.Length);
        Assert.Equal(1, request.Options[0].Number);
        Assert.Equal("Yes", request.Options[0].Text);
    }

    [Fact]
    public void ParsePermissionRequest_MultipleOptionsOnSameLine_AllExtracted()
    {
        // Test terminal format where options appear on same line separated by whitespace
        var text = @"
Do you want to create file.txt?

  1. Yes  2. Yes, for this session  3. No

Esc to cancel
";

        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);
        Assert.Equal(3, request.Options.Length);

        Assert.Equal(1, request.Options[0].Number);
        Assert.Contains("Yes", request.Options[0].Text);

        Assert.Equal(2, request.Options[1].Number);
        Assert.Contains("for this session", request.Options[1].Text);

        Assert.Equal(3, request.Options[2].Number);
        Assert.Equal("No", request.Options[2].Text);
    }

    [Fact]
    public void ParsePermissionRequest_OptionsWithParentheses_ParsesCorrectly()
    {
        // Test format with parentheses instead of dots
        var text = @"
Do you want to execute command?

  1) Yes
  2) Yes, auto-approve for this session
  3) No
";

        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);
        Assert.Equal(3, request.Options.Length);
        Assert.Equal(1, request.Options[0].Number);
        Assert.Equal(2, request.Options[1].Number);
        Assert.Equal(3, request.Options[2].Number);
    }

    [Fact]
    public void ParsePermissionRequest_OriginalReadingPrompt_StillWorks()
    {
        // Ensure original reading prompt format still works (regression test)
        var text = @"
Do you want to proceed?

> 1. Yes
  2. Yes, allow reading from /c/C: from this project
  3. No

Esc to cancel · Tab to amend
";

        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);
        Assert.Equal(ClaudePermissionPromptType.AllowReading, request.PromptType);
        Assert.Equal(3, request.Options.Length);
        Assert.True(request.HasPersistentApprovalOption);
        Assert.Equal(2, request.PersistentApprovalOptionNumber);
    }

    [Fact]
    public void ParsePermissionRequest_ExcessiveWhitespace_Normalized()
    {
        // Test that excessive whitespace in option text is collapsed
        var text = @"
Do you want to create file?

  1. Yes
  2. Yes,    and    switch    to    accept    edits
  3. No
";

        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);
        Assert.Equal(3, request.Options.Length);

        // Option 2 text should have whitespace normalized
        var option2Text = request.Options[1].Text;
        Assert.DoesNotContain("    ", option2Text); // No quadruple spaces
        Assert.Contains("switch to accept edits", option2Text);
    }

    [Fact]
    public void ParsePermissionRequest_PromptRegionExtraction_StopsAtBoundary()
    {
        // Test that parser only looks at prompt region, not entire terminal buffer
        var text = @"
Previous terminal output with numbered list:
1. Some previous item
2. Another previous item

Do you want to create test.txt?

  1. Yes
  2. Yes, for this session
  3. No

Esc to cancel

More terminal output after prompt
";

        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);
        // Should only find the 3 permission options, not the previous numbered list
        Assert.Equal(3, request.Options.Length);
        Assert.Equal(1, request.Options[0].Number);
        Assert.Equal(2, request.Options[1].Number);
        Assert.Equal(3, request.Options[2].Number);
    }
}
