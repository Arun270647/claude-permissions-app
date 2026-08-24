using Avalonia.Controls;

namespace ClaudePermissionAssistant.MacApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // TODO: Initialize monitoring services based on platform
        // - macOS: Use MacOSTerminalAccessor and MacOSPromptExecutor
        // - Windows: Use existing Windows services

        NoTerminalsTextBlock.IsVisible = true;
        TerminalListBox.IsVisible = false;
    }
}
