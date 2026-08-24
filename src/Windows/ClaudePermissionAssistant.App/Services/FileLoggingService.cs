using System.IO;
using System.Text;
using ClaudePermissionAssistant.App.Models;

namespace ClaudePermissionAssistant.App.Services;

/// <summary>
/// Simple persistent file logging service
/// </summary>
public class FileLoggingService
{
    private readonly string _logFilePath;
    private readonly object _lock = new();

    public FileLoggingService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ClaudePermissionAssistant"
        );

        Directory.CreateDirectory(appDataPath);

        _logFilePath = Path.Combine(appDataPath, $"log_{DateTime.Now:yyyyMMdd}.txt");
    }

    public void Log(LogEntry entry)
    {
        try
        {
            lock (_lock)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] {entry.Event}");

                if (entry.PromptType.HasValue)
                    sb.AppendLine($"  PromptType: {entry.PromptType}");

                if (entry.OptionNumber.HasValue)
                    sb.AppendLine($"  Option: {entry.OptionNumber}");

                if (entry.Success.HasValue)
                    sb.AppendLine($"  Success: {entry.Success}");

                if (entry.DurationMs.HasValue)
                    sb.AppendLine($"  Duration: {entry.DurationMs}ms");

                if (entry.TerminalPid.HasValue)
                    sb.AppendLine($"  Terminal PID: {entry.TerminalPid}");

                if (!string.IsNullOrEmpty(entry.Error))
                    sb.AppendLine($"  Error: {entry.Error}");

                sb.AppendLine();

                File.AppendAllText(_logFilePath, sb.ToString());
            }
        }
        catch
        {
            // Logging failure should not crash the app
        }
    }

    public void LogInfo(string message)
    {
        Log(new LogEntry { Event = message });
    }

    public void LogError(string message, Exception? ex = null)
    {
        Log(new LogEntry
        {
            Event = message,
            Error = ex?.Message
        });
    }
}
