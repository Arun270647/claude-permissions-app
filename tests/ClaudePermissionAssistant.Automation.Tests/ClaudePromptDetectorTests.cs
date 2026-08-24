using ClaudePermissionAssistant.Core.Services;
using ClaudePermissionAssistant.Automation.Services;
using ClaudePermissionAssistant.Core.Models;

namespace ClaudePermissionAssistant.Automation.Tests;

public class ClaudePromptDetectorTests
{
    private readonly ClaudePromptDetector _detector;

    public ClaudePromptDetectorTests()
    {
        var parser = new ClaudePromptParser();
        _detector = new ClaudePromptDetector(parser);
    }

    [Fact]
    public void DetectPrompt_WithUnverifiedSession_ReturnsNull()
    {
        var session = new ClaudeSession
        {
            TerminalWindowHandle = IntPtr.Zero,
            TerminalProcessId = 1,
            ClaudeProcessId = null, // Not verified
            TerminalType = TerminalType.CMD,
            TerminalProcessName = "cmd",
            WindowTitle = "Test"
        };

        var prompt = _detector.DetectPrompt(session);

        Assert.Null(prompt);
    }

    [Fact]
    public void DetectPrompt_WithInvalidWindowHandle_ReturnsNull()
    {
        var session = new ClaudeSession
        {
            TerminalWindowHandle = IntPtr.Zero,
            TerminalProcessId = 1,
            ClaudeProcessId = 100,
            TerminalType = TerminalType.CMD,
            TerminalProcessName = "cmd",
            WindowTitle = "Test"
        };

        var prompt = _detector.DetectPrompt(session);

        Assert.Null(prompt);
    }

    [Fact]
    public void GetTerminalText_WithInvalidHandle_ReturnsNull()
    {
        var text = _detector.GetTerminalText(IntPtr.Zero);

        Assert.Null(text);
    }

    [Fact]
    public void CanAccessTerminalText_WithInvalidHandle_ReturnsFalse()
    {
        var canAccess = _detector.CanAccessTerminalText(IntPtr.Zero);

        Assert.False(canAccess);
    }
}
