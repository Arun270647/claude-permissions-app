using ClaudePermissionAssistant.Core.Models;

namespace ClaudePermissionAssistant.Core.Interfaces;

public interface IClaudePromptDetector
{
    DetectedPrompt? DetectPrompt(ClaudeSession session);

    string? GetTerminalText(IntPtr windowHandle);

    bool CanAccessTerminalText(IntPtr windowHandle);
}
