using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ClaudePermissionAssistant.App.Models;

namespace ClaudePermissionAssistant.App.Services;

/// <summary>
/// Simple persistent file logging service with security sanitization
/// </summary>
public class FileLoggingService
{
    private readonly string _logFilePath;
    private readonly object _lock = new();

    // SECURITY FIX: Patterns to redact sensitive information from logs
    private static readonly Regex[] SensitivePatterns = new[]
    {
        new Regex(@"password[=:\s]+\S+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"token[=:\s]+\S+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"api[-_]?key[=:\s]+\S+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"secret[=:\s]+\S+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"bearer\s+\S+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"authorization[=:\s]+\S+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"\b[A-Za-z0-9]{20,}\b", RegexOptions.Compiled), // Long alphanumeric strings (likely tokens)
    };

    public FileLoggingService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ClaudePermissionAssistant"
        );

        Directory.CreateDirectory(appDataPath);

        _logFilePath = Path.Combine(appDataPath, $"log_{DateTime.Now:yyyyMMdd}.txt");
    }

    /// <summary>
    /// SECURITY FIX: Sanitize log message to remove sensitive information
    /// </summary>
    private string SanitizeLogMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        var sanitized = message;

        foreach (var pattern in SensitivePatterns)
        {
            sanitized = pattern.Replace(sanitized, match =>
            {
                var prefix = match.Value.Substring(0, Math.Min(match.Value.Length, 10));
                return $"{prefix}[REDACTED]";
            });
        }

        return sanitized;
    }

    public void Log(LogEntry entry)
    {
        try
        {
            lock (_lock)
            {
                var sb = new StringBuilder();

                // SECURITY FIX: Sanitize event message
                var sanitizedEvent = SanitizeLogMessage(entry.Event);
                sb.AppendLine($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] {sanitizedEvent}");

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
                {
                    // SECURITY FIX: Sanitize error messages
                    var sanitizedError = SanitizeLogMessage(entry.Error);
                    sb.AppendLine($"  Error: {sanitizedError}");
                }

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
