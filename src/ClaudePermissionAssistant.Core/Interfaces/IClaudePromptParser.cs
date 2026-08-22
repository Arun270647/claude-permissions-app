using ClaudePermissionAssistant.Core.Models;

namespace ClaudePermissionAssistant.Core.Interfaces;

public interface IClaudePromptParser
{
    PermissionRequest? ParsePermissionRequest(string text);

    bool IsValidPromptFormat(string text);

    bool ContainsPromptMarkers(string text);
}
