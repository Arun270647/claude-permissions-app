using ClaudePermissionAssistant.Core.Models;

namespace ClaudePermissionAssistant.Core.Interfaces;

public interface IClaudeSessionDetector
{
    ClaudeSession[] DetectActiveSessions();

    bool IsClaudeProcess(int processId);

    ClaudeSession? GetSessionByWindowHandle(IntPtr windowHandle);
}
