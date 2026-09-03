using ClaudePermissionAssistant.Core.Models;

namespace ClaudePermissionAssistant.Core.Interfaces;

public interface IClaudePermissionPromptExecutor
{
    ExecutionResult Execute(DetectedPrompt prompt);

    bool IsPromptAlreadyHandled(DetectedPrompt prompt);

    void MarkPromptAsHandled(DetectedPrompt prompt);

    void ClearHandledPrompts();

    void CleanupOldHandledPrompts();

    void IncrementContextSequence();

    int GetContextSequence();
}
