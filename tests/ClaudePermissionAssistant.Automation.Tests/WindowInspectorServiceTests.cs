using ClaudePermissionAssistant.Automation.Services;

namespace ClaudePermissionAssistant.Automation.Tests;

public class WindowInspectorServiceTests
{
    private readonly WindowInspectorService _service;

    public WindowInspectorServiceTests()
    {
        _service = new WindowInspectorService();
    }

    [Fact]
    public void GetAllWindows_ReturnsWindowList()
    {
        var windows = _service.GetAllWindows();

        Assert.NotNull(windows);
    }

    [Fact]
    public void GetAllWindows_ReturnsOrderedWindows()
    {
        var windows = _service.GetAllWindows();

        if (windows.Count > 1)
        {
            for (int i = 0; i < windows.Count - 1; i++)
            {
                var comparison = string.Compare(windows[i].ProcessName, windows[i + 1].ProcessName, StringComparison.OrdinalIgnoreCase);
                Assert.True(comparison <= 0, "Windows should be ordered by ProcessName");
            }
        }
    }

    [Fact]
    public void InspectWindow_WithInvalidHandle_ReturnsFailure()
    {
        var result = _service.InspectWindow(IntPtr.Zero);

        Assert.NotNull(result);
        Assert.False(result.Success);
    }

    [Fact]
    public void ExportTreeToText_WithValidResult_ReturnsFormattedText()
    {
        var windows = _service.GetAllWindows();

        if (windows.Count > 0)
        {
            var result = _service.InspectWindow(windows[0].WindowHandle);

            if (result.Success)
            {
                var exportText = _service.ExportTreeToText(result);

                Assert.NotNull(exportText);
                Assert.Contains("UI Automation Inspection Report", exportText);
                Assert.Contains("Window Information:", exportText);
                Assert.Contains("Process ID:", exportText);
            }
        }
    }
}
