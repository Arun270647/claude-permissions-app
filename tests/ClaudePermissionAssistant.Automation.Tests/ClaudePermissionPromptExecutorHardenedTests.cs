using ClaudePermissionAssistant.Automation.Services;
using ClaudePermissionAssistant.Core.Interfaces;
using ClaudePermissionAssistant.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ClaudePermissionAssistant.Automation.Tests;

public class ClaudePermissionPromptExecutorHardenedTests
{
    private readonly Mock<IClaudePromptDetector> _mockDetector;
    private readonly ExecutorConfiguration _config;
    private readonly ClaudePermissionPromptExecutorHardened _executor;

    public ClaudePermissionPromptExecutorHardenedTests()
    {
        _mockDetector = new Mock<IClaudePromptDetector>();

        _config = new ExecutorConfiguration
        {
            FocusDelayMs = 10,  // Short delays for tests
            KeyPressDelayMs = 10,
            VerificationDelayMs = 10,
            MaxRetryAttempts = 1,
            RetryDelayMs = 10,
            RequireForegroundVerification = false  // Disable for unit tests (no real HWNDs)
        };

        _executor = new ClaudePermissionPromptExecutorHardened(
            _mockDetector.Object,
            NullLogger<ClaudePermissionPromptExecutorHardened>.Instance,
            _config
        );
    }

    [Fact]
    public void Execute_WithNoPersistentApprovalOption_UsesSimpleYes()
    {
        // Arrange
        var session = CreateTestSession();
        var request = new PermissionRequest
        {
            ToolName = "Test",
            Description = "Test prompt",
            Options = new[]
            {
                new PermissionOption { Number = 1, Text = "Yes", Action = PermissionAction.Allow },
                new PermissionOption { Number = 2, Text = "No", Action = PermissionAction.Deny }
            },
            PromptType = ClaudePermissionPromptType.Unknown,
            PersistentApprovalOptionNumber = null  // No persistent approval — falls back to simple Yes
        };

        var prompt = new DetectedPrompt
        {
            Session = session,
            RawText = "test prompt",
            Request = request
        };

        // Re-detection returns same prompt
        _mockDetector.Setup(d => d.DetectPrompt(It.IsAny<ClaudeSession>()))
            .Returns(prompt);

        // Act
        var result = _executor.Execute(prompt);

        // Assert - should attempt execution with option 1 (simple Yes)
        Assert.Equal(1, result.SelectedOptionNumber);
    }

    [Fact]
    public void Execute_WithPromptDisappeared_Fails()
    {
        // Arrange
        var session = CreateTestSession();
        var prompt = CreateTestPrompt(session, optionNumber: 2);

        // Re-detection returns null (prompt disappeared)
        _mockDetector.Setup(d => d.DetectPrompt(It.IsAny<ClaudeSession>()))
            .Returns((DetectedPrompt?)null);

        // Act
        var result = _executor.Execute(prompt);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("no longer present", result.ErrorMessage);
        Assert.Equal(ExecutionState.Failed, result.FinalState);
    }

    [Fact]
    public void Execute_WithOptionNumber2_UsesDetectedNumber()
    {
        // Arrange
        var session = CreateTestSession();
        var prompt = CreateTestPrompt(session, optionNumber: 2);

        // Re-detection returns same prompt
        _mockDetector.SetupSequence(d => d.DetectPrompt(It.IsAny<ClaudeSession>()))
            .Returns(prompt)  // Re-detection before execution
            .Returns((DetectedPrompt?)null);  // Verification after execution (prompt disappeared)

        // Act
        var result = _executor.Execute(prompt);

        // Assert
        // Note: We can't fully test keyboard input in unit tests without actual HWND
        // But we can verify the option number was used
        Assert.Equal(2, result.SelectedOptionNumber);
    }

    [Fact]
    public void Execute_WithOptionNumber3_UsesDetectedNumber()
    {
        // Arrange
        var session = CreateTestSession();
        var prompt = CreateTestPrompt(session, optionNumber: 3);

        // Re-detection returns same prompt
        _mockDetector.SetupSequence(d => d.DetectPrompt(It.IsAny<ClaudeSession>()))
            .Returns(prompt)
            .Returns((DetectedPrompt?)null);

        // Act
        var result = _executor.Execute(prompt);

        // Assert
        Assert.Equal(3, result.SelectedOptionNumber);
    }

    [Fact]
    public void Execute_DuplicatePrompt_PreventsSecondExecution()
    {
        // Arrange
        var session = CreateTestSession();
        var prompt = CreateTestPrompt(session, optionNumber: 2);

        // First execution: redetect (same), verify (disappeared)
        // Second execution: should fail immediately with "already handled"
        _mockDetector.SetupSequence(d => d.DetectPrompt(It.IsAny<ClaudeSession>()))
            .Returns(prompt)  // First: redetect for execution
            .Returns((DetectedPrompt?)null);  // First: verify prompt disappeared

        // First execution
        var result1 = _executor.Execute(prompt);

        // Act - Second execution of same prompt (should be rejected immediately)
        var result2 = _executor.Execute(prompt);

        // Assert
        Assert.False(result2.Success);
        Assert.Contains("already handled", result2.ErrorMessage);
    }

    [Fact]
    public void IsPromptAlreadyHandled_WithNewPrompt_ReturnsFalse()
    {
        // Arrange
        var session = CreateTestSession();
        var prompt = CreateTestPrompt(session, optionNumber: 2);

        // Act
        var handled = _executor.IsPromptAlreadyHandled(prompt);

        // Assert
        Assert.False(handled);
    }

    [Fact]
    public void MarkPromptAsHandled_MarksPromptAsHandled()
    {
        // Arrange
        var session = CreateTestSession();
        var prompt = CreateTestPrompt(session, optionNumber: 2);

        // Act
        _executor.MarkPromptAsHandled(prompt);

        // Assert
        Assert.True(_executor.IsPromptAlreadyHandled(prompt));
    }

    [Fact]
    public void ClearHandledPrompts_ClearsAllHandledPrompts()
    {
        // Arrange
        var session = CreateTestSession();
        var prompt = CreateTestPrompt(session, optionNumber: 2);
        _executor.MarkPromptAsHandled(prompt);

        // Act
        _executor.ClearHandledPrompts();

        // Assert
        Assert.False(_executor.IsPromptAlreadyHandled(prompt));
    }

    [Fact]
    public void Execute_WithChangedOptionNumber_DetectsChange()
    {
        // Arrange
        var session = CreateTestSession();
        var originalPrompt = CreateTestPrompt(session, optionNumber: 2);

        // Re-detection returns prompt with different option number
        var changedPrompt = CreateTestPrompt(session, optionNumber: 3, rawText: "different text");

        _mockDetector.Setup(d => d.DetectPrompt(It.IsAny<ClaudeSession>()))
            .Returns(changedPrompt);

        // Act
        var result = _executor.Execute(originalPrompt);

        // Assert - executor uses re-detected prompt's option number
        Assert.Equal(3, result.SelectedOptionNumber);
    }

    [Fact]
    public void Execute_DifferentPromptsSameSession_BothExecute()
    {
        // Arrange
        var session = CreateTestSession();
        var prompt1 = CreateTestPrompt(session, optionNumber: 2, rawText: "First prompt");
        var prompt2 = CreateTestPrompt(session, optionNumber: 2, rawText: "Second prompt");

        // Each execution needs: redetect (same prompt), verify (prompt disappeared)
        _mockDetector.SetupSequence(d => d.DetectPrompt(It.IsAny<ClaudeSession>()))
            .Returns(prompt1)  // Execution 1: redetect
            .Returns((DetectedPrompt?)null)  // Execution 1: verify (disappeared)
            .Returns(prompt2)  // Execution 2: redetect
            .Returns((DetectedPrompt?)null);  // Execution 2: verify (disappeared)

        // Act
        var result1 = _executor.Execute(prompt1);
        var result2 = _executor.Execute(prompt2);

        // Assert - Different prompts should both execute
        // Note: Since we're in unit tests without real HWNDs, foreground verification may fail
        // but we can still check that the option numbers were correctly identified
        Assert.Equal(2, result1.SelectedOptionNumber);
        Assert.Equal(2, result2.SelectedOptionNumber);
    }

    [Fact]
    public void Execute_RecordsExecutionDetails()
    {
        // Arrange
        var session = CreateTestSession();
        var prompt = CreateTestPrompt(session, optionNumber: 2);

        _mockDetector.SetupSequence(d => d.DetectPrompt(It.IsAny<ClaudeSession>()))
            .Returns(prompt)
            .Returns((DetectedPrompt?)null);

        // Act
        var result = _executor.Execute(prompt);

        // Assert
        Assert.NotEqual(default(DateTime), result.ExecutedAt);
        Assert.True(result.ExecutionDuration.TotalMilliseconds > 0);
        Assert.NotNull(result.Prompt);
        Assert.Equal(prompt.Request.ToolName, result.Prompt.Request.ToolName);
    }

    [Fact]
    public void Execute_WithInvalidSession_Fails()
    {
        // Arrange
        var session = new ClaudeSession
        {
            ClaudeProcessId = null,  // No Claude process ID = not verified
            TerminalProcessId = 12345,
            TerminalWindowHandle = new IntPtr(0x12345),
            TerminalType = TerminalType.CMD,
            TerminalProcessName = "cmd.exe",
            WindowTitle = "Test",
            DetectedAt = DateTime.UtcNow
        };

        var prompt = CreateTestPrompt(session, optionNumber: 2);

        _mockDetector.Setup(d => d.DetectPrompt(It.IsAny<ClaudeSession>()))
            .Returns((DetectedPrompt?)null);  // Detector returns null for invalid session

        // Act
        var result = _executor.Execute(prompt);

        // Assert
        Assert.False(result.Success);
    }

    private ClaudeSession CreateTestSession()
    {
        return new ClaudeSession
        {
            ClaudeProcessId = 12345,
            TerminalProcessId = 12345,
            TerminalWindowHandle = new IntPtr(0x12345),
            TerminalType = TerminalType.CMD,
            TerminalProcessName = "cmd.exe",
            WindowTitle = "Test Terminal",
            DetectedAt = DateTime.UtcNow
        };
    }

    private DetectedPrompt CreateTestPrompt(ClaudeSession session, int optionNumber, string? rawText = null)
    {
        var request = new PermissionRequest
        {
            ToolName = "Test",
            Description = "Test prompt",
            Options = new[]
            {
                new PermissionOption { Number = 1, Text = "Yes", Action = PermissionAction.Allow },
                new PermissionOption { Number = optionNumber, Text = $"Yes, for this session", Action = PermissionAction.AlwaysAllow },
                new PermissionOption { Number = 3, Text = "No", Action = PermissionAction.Deny }
            },
            PromptType = ClaudePermissionPromptType.Unknown,
            PersistentApprovalOptionNumber = optionNumber
        };

        return new DetectedPrompt
        {
            Session = session,
            RawText = rawText ?? $"test prompt option {optionNumber}",
            Request = request
        };
    }
}
