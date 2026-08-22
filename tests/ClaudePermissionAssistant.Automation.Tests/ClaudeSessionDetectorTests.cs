using ClaudePermissionAssistant.Automation.Services;

namespace ClaudePermissionAssistant.Automation.Tests;

public class ClaudeSessionDetectorTests
{
    private readonly ClaudeSessionDetector _detector;

    public ClaudeSessionDetectorTests()
    {
        _detector = new ClaudeSessionDetector();
    }

    [Fact]
    public void DetectActiveSessions_ReturnsArray()
    {
        var sessions = _detector.DetectActiveSessions();

        Assert.NotNull(sessions);
    }

    [Fact]
    public void IsClaudeProcess_WithInvalidProcessId_ReturnsFalse()
    {
        Assert.False(_detector.IsClaudeProcess(-1));
        Assert.False(_detector.IsClaudeProcess(0));
        Assert.False(_detector.IsClaudeProcess(999999));
    }

    [Fact]
    public void GetSessionByWindowHandle_WithInvalidHandle_ReturnsNull()
    {
        var session = _detector.GetSessionByWindowHandle(IntPtr.Zero);

        Assert.Null(session);
    }

    [Fact]
    public void DetectActiveSessions_ReturnsOnlyVerifiedSessions()
    {
        var sessions = _detector.DetectActiveSessions();

        foreach (var session in sessions)
        {
            Assert.NotEqual(IntPtr.Zero, session.TerminalWindowHandle);
            Assert.True(session.TerminalProcessId > 0);
            Assert.NotEqual(string.Empty, session.TerminalProcessName);
        }
    }

    [Fact]
    public void DetectActiveSessions_SetsTerminalType()
    {
        var sessions = _detector.DetectActiveSessions();

        foreach (var session in sessions)
        {
            Assert.NotEqual(Core.Models.TerminalType.Unsupported, session.TerminalType);
        }
    }
}
