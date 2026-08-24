using System.Threading;

namespace ClaudePermissionAssistant.App.Services;

/// <summary>
/// Ensures only one instance of the application runs at a time
/// </summary>
public class SingleInstanceManager : IDisposable
{
    private readonly Mutex _mutex;
    private readonly bool _isOwner;

    public SingleInstanceManager()
    {
        const string mutexName = "ClaudePermissionAssistant_SingleInstance_Mutex";

        _mutex = new Mutex(true, mutexName, out _isOwner);
    }

    public bool IsFirstInstance => _isOwner;

    public void Dispose()
    {
        if (_isOwner)
        {
            _mutex?.ReleaseMutex();
        }
        _mutex?.Dispose();
    }
}
