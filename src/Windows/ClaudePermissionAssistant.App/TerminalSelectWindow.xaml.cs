using System.Windows;
using System.Windows.Input;
using ClaudePermissionAssistant.App.Services;

namespace ClaudePermissionAssistant.App;

public partial class TerminalSelectWindow : Window
{
    private readonly TerminalFilterService _terminalFilter;

    public TerminalCandidate? SelectedTerminal { get; private set; }

    public TerminalSelectWindow(TerminalFilterService terminalFilter)
    {
        InitializeComponent();
        _terminalFilter = terminalFilter;
        LoadTerminals();
    }

    private void LoadTerminals()
    {
        try
        {
            var terminals = _terminalFilter.GetTerminalCandidates()
                .Where(t => t.TextPatternAvailable)
                .ToList();

            TerminalListBox.ItemsSource = terminals;

            if (terminals.Count == 0)
            {
                MessageBox.Show(
                    "No supported terminals with TextPattern found.\n\n" +
                    "Make sure you have a terminal open (CMD, PowerShell, or Windows Terminal).",
                    "No Terminals Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading terminals: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        LoadTerminals();
    }

    private void TerminalListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (TerminalListBox.SelectedItem is TerminalCandidate terminal)
        {
            SelectedTerminal = terminal;
            DialogResult = true;
        }
    }

    private void Select_Click(object sender, RoutedEventArgs e)
    {
        if (TerminalListBox.SelectedItem is TerminalCandidate terminal)
        {
            SelectedTerminal = terminal;
            DialogResult = true;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    // Enable Select button when an item is selected
    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        TerminalListBox.SelectionChanged += (s, args) =>
        {
            SelectButton.IsEnabled = TerminalListBox.SelectedItem != null;
        };
    }
}
