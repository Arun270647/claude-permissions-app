using ClaudePermissionAssistant.Core.Services;
using ClaudePermissionAssistant.Automation.Services;
using ClaudePermissionAssistant.Core.Models;

namespace ClaudePermissionAssistant.Automation.Tests;

/// <summary>
/// PHASE 5 CRITICAL FIX - Full Buffer Scanning Tests
/// Tests that parser finds prompts anywhere in terminal scrollback,
/// not just at the beginning
/// </summary>
public class ClaudePromptParserBufferScanningTests
{
    private readonly ClaudePromptParserSimple _parser;

    public ClaudePromptParserBufferScanningTests()
    {
        _parser = new ClaudePromptParserSimple();
    }

    [Fact]
    public void ParsePermissionRequest_PromptAtBeginningOfBuffer_FindsPrompt()
    {
        var text = @"Do you want to create file.txt?

  1. Yes
  2. Yes, for this project
  3. No

Esc to cancel

[Rest of terminal output follows...]
Some Claude response
More terminal output
";

        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);
        Assert.Equal(3, request.Options.Length);
        Assert.Equal(2, request.PersistentApprovalOptionNumber);
    }

    [Fact]
    public void ParsePermissionRequest_PromptAtMiddleOfBuffer_FindsPrompt()
    {
        var text = @"
[Previous terminal output]
Claude response from earlier
Tool execution results
Some file diffs
More output
Another Claude response

Do you want to create file.txt?

  1. Yes
  2. Yes, for this project
  3. No

Esc to cancel

[More terminal output after prompt]
";

        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);
        Assert.Equal(3, request.Options.Length);
        Assert.Equal(2, request.PersistentApprovalOptionNumber);
    }

    [Fact]
    public void ParsePermissionRequest_PromptAtEndOfBuffer_FindsPrompt()
    {
        var text = @"
[Lots of previous terminal output]
Previous Claude conversation
Thought: analyzing the code
Tool: Read
Result: file contents here
Thought: making changes
Tool: Edit
Result: changes applied

Many lines of output
More Claude responses
Previous interactions

Do you want to create file.txt?

  1. Yes
  2. Yes, for this project
  3. No

Esc to cancel";

        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);
        Assert.Equal(3, request.Options.Length);
        Assert.Equal(2, request.PersistentApprovalOptionNumber);
    }

    [Fact]
    public void ParsePermissionRequest_LargeBufferWithPromptAtEnd_FindsPrompt()
    {
        // Simulate real-world: thousands of characters of previous output
        var previousOutput = string.Join("\n", Enumerable.Range(1, 200).Select(i =>
            $"Line {i}: Previous terminal output, Claude conversation, tool results, etc."));

        var text = previousOutput + @"

Do you want to run command 'curl --version'?

  1. Yes
  2. Yes, and don't ask again for: curl *
  3. No

Esc to cancel · Tab to amend";

        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);
        Assert.Equal(3, request.Options.Length);
        Assert.Equal(2, request.PersistentApprovalOptionNumber);
        Assert.NotNull(request.PromptRegion);
        Assert.Contains("curl --version", request.PromptRegion);
    }

    [Fact]
    public void ParsePermissionRequest_MultipleHistoricalPrompts_FindsMostRecent()
    {
        var text = @"
[Old prompt 1 - already answered]
Do you want to create oldfile1.txt?

  1. Yes
  2. Yes, for this project
  3. No

[Claude response after old prompt 1]
Created oldfile1.txt successfully.
More output...

[Old prompt 2 - already answered]
Do you want to create oldfile2.txt?

  1. Yes
  2. Yes, for this project
  3. No

[Claude response after old prompt 2]
Created oldfile2.txt successfully.
More output...

[CURRENT ACTIVE PROMPT]
Do you want to create newfile.txt?

  1. Yes
  2. Yes, for this project
  3. No

Esc to cancel";

        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);
        Assert.Equal(3, request.Options.Length);
        Assert.Equal(2, request.PersistentApprovalOptionNumber);

        // Verify it found the LAST (most recent) prompt
        Assert.NotNull(request.PromptRegion);
        Assert.Contains("newfile.txt", request.PromptRegion);
        Assert.DoesNotContain("oldfile1.txt", request.PromptRegion);
        Assert.DoesNotContain("oldfile2.txt", request.PromptRegion);
    }

    [Fact]
    public void ParsePermissionRequest_OldInvalidPromptAndCurrentValidPrompt_FindsValid()
    {
        var text = @"
[Old prompt - incomplete/invalid format]
Do you want to do something?
[No numbered options - invalid]

Some Claude output

[CURRENT VALID PROMPT]
Do you want to create file.txt?

  1. Yes
  2. Yes, for this project
  3. No

Esc to cancel";

        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);
        Assert.Equal(3, request.Options.Length);
        Assert.Contains("file.txt", request.PromptRegion!);
    }

    [Fact]
    public void ParsePermissionRequest_IdenticalQuestionsButDifferentContexts_FindsLast()
    {
        // Two prompts with identical question text but different file contexts
        var text = @"
[First prompt]
Do you want to create file.txt?

  1. Yes
  2. Yes, for this project
  3. No

Created file in /path/one/

[Second prompt - same question, different context]
Do you want to create file.txt?

  1. Yes
  2. Yes, for this project
  3. No

Esc to cancel";

        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);
        Assert.Equal(3, request.Options.Length);

        // Should find the LAST occurrence
        // (Both are valid, but we want the most recent)
        Assert.NotNull(request.PromptRegion);
    }

    [Fact]
    public void ParsePermissionRequest_RealWorldCurlCommandPrompt_ParsesCorrectly()
    {
        // Real-world scenario from user report
        var text = @"
[Previous Claude conversation with lots of output]
Here's the solution to your problem:
1. First, check the version
2. Then run the command
3. Verify the output

Some code examples:
```
function example() {
    console.log(""test"");
}
```

More discussion and analysis...

Bash command

curl --version

Check if curl is available

This command requires approval

Do you want to proceed?

> 1. Yes
  2. Yes, and don't ask again for: curl *
  3. No

Esc to cancel · Tab to amend · ctrl+e to explain";

        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);
        // Note: PromptType may be Unknown because "Do you want to proceed?" is generic
        // The important part is that it parses correctly
        Assert.Equal(3, request.Options.Length);

        // Verify persistent approval option detected
        Assert.True(request.HasPersistentApprovalOption);
        Assert.Equal(2, request.PersistentApprovalOptionNumber);

        // Verify prompt region extracted
        Assert.NotNull(request.PromptRegion);
        Assert.Contains("Do you want to proceed", request.PromptRegion);

        // Verify options parsed correctly
        Assert.Equal(1, request.Options[0].Number);
        Assert.Equal("Yes", request.Options[0].Text);

        Assert.Equal(2, request.Options[1].Number);
        Assert.Contains("don't ask again", request.Options[1].Text);
        Assert.Contains("curl *", request.Options[1].Text);

        Assert.Equal(3, request.Options[2].Number);
        Assert.Equal("No", request.Options[2].Text);
    }

    [Fact]
    public void ParsePermissionRequest_PromptRegionProperty_ContainsOnlyPromptNotFullBuffer()
    {
        var largeBuffer = string.Join("\n", Enumerable.Range(1, 100).Select(i =>
            $"Previous line {i}"));

        var text = largeBuffer + @"

Do you want to create test.txt?

  1. Yes
  2. Yes, for this project
  3. No

Esc to cancel

" + string.Join("\n", Enumerable.Range(1, 100).Select(i =>
            $"Following line {i}"));

        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);
        Assert.NotNull(request.PromptRegion);

        // PromptRegion should contain ONLY the prompt, not the entire buffer
        Assert.Contains("Do you want to create test.txt", request.PromptRegion);
        Assert.Contains("1. Yes", request.PromptRegion);
        Assert.Contains("2. Yes, for this project", request.PromptRegion);
        Assert.Contains("3. No", request.PromptRegion);

        // Should NOT contain the previous or following content
        Assert.DoesNotContain("Previous line 1", request.PromptRegion);
        Assert.DoesNotContain("Previous line 50", request.PromptRegion);
        Assert.DoesNotContain("Following line 1", request.PromptRegion);

        // Prompt region should be much smaller than full buffer
        Assert.True(request.PromptRegion.Length < text.Length / 2);
    }

    [Fact]
    public void ParsePermissionRequest_ThreeHistoricalPromptsAndOneCurrent_FindsCurrent()
    {
        var text = @"
[Historical prompt 1]
Do you want to read file1.txt?
  1. Yes
  2. Yes, from this project
  3. No

Response: Reading file1.txt...

[Historical prompt 2]
Do you want to create file2.txt?
  1. Yes
  2. Yes, for this project
  3. No

Response: Created file2.txt

[Historical prompt 3]
Do you want to execute command1?
  1. Yes
  2. Yes, for this session
  3. No

Response: Executed command1

[CURRENT PROMPT]
Do you want to execute curl --version?

  1. Yes
  2. Yes, and don't ask again for: curl *
  3. No

Esc to cancel";

        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);
        Assert.Equal(ClaudePermissionPromptType.AllowExecuting, request.PromptType);

        // Verify it found the CURRENT prompt, not historical ones
        Assert.NotNull(request.PromptRegion);
        Assert.Contains("curl --version", request.PromptRegion);
        Assert.DoesNotContain("file1.txt", request.PromptRegion);
        Assert.DoesNotContain("file2.txt", request.PromptRegion);
        Assert.DoesNotContain("command1", request.PromptRegion);
    }

    [Fact]
    public void ParsePermissionRequest_OnlyHistoricalPromptsNoCurrentPrompt_FindsMostRecent()
    {
        // Edge case: all prompts in buffer are old, but the LAST one is the "current" one
        var text = @"
[Old prompt 1]
Do you want to create file1.txt?

  1. Yes
  2. Yes, for this project
  3. No

[Old prompt 2]
Do you want to create file2.txt?

  1. Yes
  2. Yes, for this project
  3. No

[No newer prompts - last one is most recent]";

        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);

        // Should find the LAST valid prompt
        Assert.NotNull(request.PromptRegion);
        Assert.Contains("file2.txt", request.PromptRegion);
    }

    [Fact]
    public void ParsePermissionRequest_PromptWithMassivePrecedingContent_StillFinds()
    {
        // Extreme case: 5000+ lines before the prompt
        var massiveContent = string.Join("\n", Enumerable.Range(1, 5000).Select(i =>
            $"Terminal line {i}: Previous output including code, diffs, Claude thoughts, tool results, etc."));

        var text = massiveContent + @"

Bash command

test -f myfile.txt

Check if file exists

This command requires approval

Do you want to proceed?

  1. Yes
  2. Yes, and don't ask again for: test *
  3. No

Esc to cancel";

        var request = _parser.ParsePermissionRequest(text);

        Assert.NotNull(request);
        Assert.Equal(3, request.Options.Length);
        Assert.Equal(2, request.PersistentApprovalOptionNumber);

        // Verify it parsed the actual prompt
        Assert.NotNull(request.PromptRegion);
        Assert.Contains("Do you want to proceed", request.PromptRegion);
        Assert.Contains("test *", request.PromptRegion);

        // Prompt region should not contain the massive preceding content
        Assert.DoesNotContain("Terminal line 1:", request.PromptRegion);
        Assert.DoesNotContain("Terminal line 5000:", request.PromptRegion);
    }
}
