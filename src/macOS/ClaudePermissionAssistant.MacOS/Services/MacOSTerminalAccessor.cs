using ClaudePermissionAssistant.Core.Interfaces;
using ClaudePermissionAssistant.Core.Models;
using System.Diagnostics;

namespace ClaudePermissionAssistant.MacOS.Services;

/// <summary>
/// macOS-specific terminal text extraction using AppleScript and Accessibility API
/// </summary>
public class MacOSTerminalAccessor : IClaudePromptDetector
{
    private readonly IClaudePromptParser _parser;

    public MacOSTerminalAccessor(IClaudePromptParser parser)
    {
        _parser = parser;
    }

    public DetectedPrompt? DetectPrompt(ClaudeSession session)
    {
        if (!session.IsVerified)
            return null;

        var text = GetTerminalText(session.TerminalWindowHandle);
        if (string.IsNullOrWhiteSpace(text))
            return null;

        if (!_parser.ContainsPromptMarkers(text))
            return null;

        var request = _parser.ParsePermissionRequest(text);
        if (request == null || !request.IsValid)
            return null;

        return new DetectedPrompt
        {
            Session = session,
            RawText = text,
            Request = request
        };
    }

    public string? GetTerminalText(IntPtr windowHandle)
    {
        // macOS implementation: Use AppleScript to extract terminal text
        // For Terminal.app and iTerm2, we can get the visible text via AppleScript

        var script = @"
tell application ""Terminal""
    try
        set activeWindow to front window
        set activeTab to selected tab of activeWindow
        return contents of activeTab
    on error
        return """"
    end try
end tell
";

        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "osascript",
                Arguments = "-e " + script.Replace("\"", "\\\""),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
                return null;

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return process.ExitCode == 0 ? output : null;
        }
        catch
        {
            return null;
        }
    }

    public bool CanAccessTerminalText(IntPtr windowHandle)
    {
        // On macOS, we need to check if Terminal.app or iTerm2 are accessible
        // This requires Accessibility permissions
        var text = GetTerminalText(windowHandle);
        return !string.IsNullOrWhiteSpace(text);
    }
}
