using System.Diagnostics;
using System.Windows.Automation;
using ClaudePermissionAssistant.Core.Interfaces;
using ClaudePermissionAssistant.Core.Models;

namespace ClaudePermissionAssistant.Automation.Services;

public class ClaudeSessionDetector : IClaudeSessionDetector
{
    private static readonly string[] KnownTerminalProcesses =
    {
        "WindowsTerminal",
        "cmd",
        "powershell",
        "pwsh",
        "bash",
        "mintty"
    };

    private static readonly string[] ClaudeProcessNames =
    {
        "claude",
        "claude.exe",
        "node" // Claude may run via Node.js wrapper
    };

    public ClaudeSession[] DetectActiveSessions()
    {
        var sessions = new List<ClaudeSession>();

        try
        {
            var terminalWindows = GetTerminalWindows();

            foreach (var window in terminalWindows)
            {
                try
                {
                    var session = CreateSession(window);
                    if (session != null)
                    {
                        sessions.Add(session);
                    }
                }
                catch
                {
                    // Skip windows we can't process
                }
            }
        }
        catch
        {
            // Return empty list on error
        }

        return sessions.ToArray();
    }

    public bool IsClaudeProcess(int processId)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            var processName = process.ProcessName.ToLowerInvariant();

            return ClaudeProcessNames.Any(name =>
                processName.Contains(name.ToLowerInvariant()));
        }
        catch
        {
            return false;
        }
    }

    public ClaudeSession? GetSessionByWindowHandle(IntPtr windowHandle)
    {
        try
        {
            var element = AutomationElement.FromHandle(windowHandle);
            if (element == null)
                return null;

            var processId = element.Current.ProcessId;
            var process = Process.GetProcessById(processId);
            var processName = process.ProcessName;

            if (!IsTerminalProcess(processName))
                return null;

            return CreateSession(element);
        }
        catch
        {
            return null;
        }
    }

    private List<AutomationElement> GetTerminalWindows()
    {
        var terminals = new List<AutomationElement>();
        var condition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window);
        var rootElement = AutomationElement.RootElement;
        var windowElements = rootElement.FindAll(TreeScope.Children, condition);

        foreach (AutomationElement element in windowElements)
        {
            try
            {
                var processId = element.Current.ProcessId;
                var process = Process.GetProcessById(processId);
                var processName = process.ProcessName;

                if (IsTerminalProcess(processName))
                {
                    terminals.Add(element);
                }
            }
            catch
            {
                // Skip windows we can't access
            }
        }

        return terminals;
    }

    private bool IsTerminalProcess(string processName)
    {
        var lowerName = processName.ToLowerInvariant();
        return KnownTerminalProcesses.Any(term =>
            lowerName.Contains(term.ToLowerInvariant()));
    }

    private ClaudeSession? CreateSession(AutomationElement windowElement)
    {
        try
        {
            var processId = windowElement.Current.ProcessId;
            var process = Process.GetProcessById(processId);
            var windowHandle = new IntPtr(windowElement.Current.NativeWindowHandle);

            var terminalType = DetermineTerminalType(process.ProcessName);
            var claudeProcessId = FindClaudeProcess(processId);

            return new ClaudeSession
            {
                TerminalWindowHandle = windowHandle,
                TerminalProcessId = processId,
                ClaudeProcessId = claudeProcessId,
                TerminalType = terminalType,
                TerminalProcessName = process.ProcessName,
                WindowTitle = windowElement.Current.Name
            };
        }
        catch
        {
            return null;
        }
    }

    private TerminalType DetermineTerminalType(string processName)
    {
        var lower = processName.ToLowerInvariant();

        if (lower.Contains("windowsterminal"))
            return TerminalType.WindowsTerminal;
        if (lower.Contains("cmd"))
            return TerminalType.CMD;
        if (lower.Contains("pwsh"))
            return TerminalType.PowerShell7;
        if (lower.Contains("powershell"))
            return TerminalType.PowerShell;
        if (lower.Contains("bash") || lower.Contains("mintty"))
            return TerminalType.GitBash;

        return TerminalType.Unknown;
    }

    private int? FindClaudeProcess(int terminalProcessId)
    {
        try
        {
            var childProcesses = GetChildProcesses(terminalProcessId);

            foreach (var pid in childProcesses)
            {
                if (IsClaudeProcess(pid))
                    return pid;

                var grandChildren = GetChildProcesses(pid);
                foreach (var grandPid in grandChildren)
                {
                    if (IsClaudeProcess(grandPid))
                        return grandPid;
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private List<int> GetChildProcesses(int parentProcessId)
    {
        var children = new List<int>();

        try
        {
            var allProcesses = Process.GetProcesses();

            foreach (var process in allProcesses)
            {
                try
                {
                    if (GetParentProcessId(process) == parentProcessId)
                    {
                        children.Add(process.Id);
                    }
                }
                catch
                {
                    // Skip processes we can't access
                }
            }
        }
        catch
        {
            // Return empty list on error
        }

        return children;
    }

    private int GetParentProcessId(Process process)
    {
        try
        {
            using var query = new System.Management.ManagementObjectSearcher(
                $"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {process.Id}");

            var results = query.Get().GetEnumerator();
            if (results.MoveNext())
            {
                var parentId = results.Current["ParentProcessId"];
                return Convert.ToInt32(parentId);
            }

            return 0;
        }
        catch
        {
            return 0;
        }
    }
}
