using System.Diagnostics;
using ClaudePermissionAssistant.Automation.Services;
using ClaudePermissionAssistant.Core.Models;
using ClaudePermissionAssistant.Core.Services;

namespace ClaudePermissionAssistant.App.Services;

/// <summary>
/// Filters windows to show only terminal candidates
/// </summary>
public class TerminalFilterService
{
    private readonly WindowInspectorService _inspectorService;
    private readonly ClaudeSessionDetector _sessionDetector;
    private readonly ClaudePromptDetector _promptDetector;

    public TerminalFilterService()
    {
        _inspectorService = new WindowInspectorService();
        _sessionDetector = new ClaudeSessionDetector();
        _promptDetector = new ClaudePromptDetector(new ClaudePromptParserSimple());
    }

    /// <summary>
    /// Gets all terminal candidates, filtering out non-terminal windows
    /// </summary>
    public List<TerminalCandidate> GetTerminalCandidates()
    {
        var allWindows = _inspectorService.GetAllWindows();
        var candidates = new List<TerminalCandidate>();

        foreach (var window in allWindows)
        {
            if (IsTerminalCandidate(window, out var terminalType))
            {
                var candidate = new TerminalCandidate
                {
                    WindowInfo = window,
                    TerminalType = terminalType,
                    TextPatternAvailable = _promptDetector.CanAccessTerminalText(window.WindowHandle),
                    ClaudeSession = TryDetectClaudeSession(window, terminalType)
                };

                candidates.Add(candidate);
            }
        }

        // Sort: Claude sessions first, then by terminal type, then by PID
        return candidates
            .OrderByDescending(c => c.ClaudeSession != null)
            .ThenBy(c => c.TerminalType)
            .ThenBy(c => c.WindowInfo.ProcessId)
            .ToList();
    }

    private bool IsTerminalCandidate(WindowInfo window, out TerminalType terminalType)
    {
        terminalType = TerminalType.Unknown;

        var processName = window.ProcessName.ToLowerInvariant();

        // Known terminal processes
        if (processName == "conhost" || processName == "conhost.exe")
        {
            // CMD or PowerShell via conhost
            terminalType = DetectConhostType(window);
            return true;
        }

        if (processName == "windowsterminal" || processName == "windowsterminal.exe")
        {
            terminalType = TerminalType.WindowsTerminal;
            return true;
        }

        if (processName == "powershell" || processName == "powershell.exe")
        {
            terminalType = TerminalType.PowerShell;
            return true;
        }

        if (processName == "pwsh" || processName == "pwsh.exe")
        {
            terminalType = TerminalType.PowerShell7;
            return true;
        }

        if (processName == "cmd" || processName == "cmd.exe")
        {
            terminalType = TerminalType.CMD;
            return true;
        }

        // Check for GitBash (optional)
        if (processName.Contains("bash") || processName.Contains("mintty"))
        {
            terminalType = TerminalType.GitBash;
            return true;
        }

        // Claude Code direct terminal (opened via desktop app or Start Menu)
        if (processName.Contains("claude"))
        {
            terminalType = TerminalType.ClaudeTerminal;
            return true;
        }

        // Not a known terminal
        return false;
    }

    private TerminalType DetectConhostType(WindowInfo window)
    {
        // Conhost can host CMD or PowerShell
        // Check window title for hints
        var title = window.WindowTitle.ToLowerInvariant();

        if (title.Contains("powershell"))
            return TerminalType.PowerShell;

        if (title.Contains("cmd"))
            return TerminalType.CMD;

        // Check parent process if possible
        try
        {
            using var process = Process.GetProcessById(window.ProcessId);
            // If we can access the process, assume it's CMD by default for conhost
            return TerminalType.CMD;
        }
        catch
        {
            return TerminalType.Unknown;
        }
    }

    private ClaudeSession? TryDetectClaudeSession(WindowInfo window, TerminalType terminalType)
    {
        try
        {
            // Try to detect if Claude Code is running in this terminal
            var sessions = _sessionDetector.DetectActiveSessions();

            // Match by terminal window handle
            return sessions.FirstOrDefault(s => s.TerminalWindowHandle == window.WindowHandle);
        }
        catch
        {
            // Claude detection failed, but terminal is still usable
            return null;
        }
    }
}

/// <summary>
/// Represents a terminal that can potentially be monitored
/// </summary>
public class TerminalCandidate
{
    public required WindowInfo WindowInfo { get; init; }
    public required TerminalType TerminalType { get; init; }
    public bool TextPatternAvailable { get; init; }
    public ClaudeSession? ClaudeSession { get; init; }

    public bool IsClaudeTerminal => ClaudeSession != null && ClaudeSession.IsVerified;

    public string DisplayName
    {
        get
        {
            if (IsClaudeTerminal)
            {
                return $"Claude Code — {TerminalType}";
            }

            return $"{TerminalType} Terminal";
        }
    }

    public string DisplayDetails
    {
        get
        {
            var details = $"PID: {WindowInfo.ProcessId}";

            if (!string.IsNullOrEmpty(WindowInfo.WindowTitle))
            {
                var title = WindowInfo.WindowTitle.Length > 50
                    ? WindowInfo.WindowTitle.Substring(0, 47) + "..."
                    : WindowInfo.WindowTitle;
                details += $" | {title}";
            }

            if (!TextPatternAvailable)
            {
                details += " | TextPattern unavailable";
            }

            return details;
        }
    }
}
