// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Collective.Platform.Controls;

/// <summary>
/// The shared find-bar control: a query box, case/word/regex toggles, prev/next, an "n of m" status,
/// and a close button. It owns the navigation/options state via <see cref="FindBarModel"/> but does
/// <b>not</b> search anything itself — each head wires <see cref="QueryChanged"/> to its own matcher
/// (terminal scrollback, mail list, DB grid, diff, HTTP response), then calls <see cref="ReportMatches"/>
/// to update the status. Toggle visibility with <see cref="IsOpen"/>.
/// </summary>
public sealed class FindBar : UserControl
{
    private readonly TextBox _query;
    private readonly TextBlock _status;

    /// <summary>Navigation + options state shared with the head's search code.</summary>
    public FindBarModel Model { get; } = new();

    /// <summary>Raised when the query text or an option changes (the head should re-search).</summary>
    public event EventHandler? QueryChanged;

    /// <summary>Raised when the user asks for the next match.</summary>
    public event EventHandler? NextRequested;

    /// <summary>Raised when the user asks for the previous match.</summary>
    public event EventHandler? PreviousRequested;

    /// <summary>Raised when the user closes the bar (Esc or the ✕ button).</summary>
    public event EventHandler? CloseRequested;

    public FindBar()
    {
        _query = new TextBox { Width = 220, PlaceholderText = "Find…", VerticalAlignment = VerticalAlignment.Center };
        _status = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Opacity = 0.7, MinWidth = 64 };

        var matchCase = new ToggleButton { Content = "Aa", VerticalAlignment = VerticalAlignment.Center };
        ToolTip.SetTip(matchCase, "Match case");
        var wholeWord = new ToggleButton { Content = "W", VerticalAlignment = VerticalAlignment.Center };
        ToolTip.SetTip(wholeWord, "Whole word");
        var useRegex = new ToggleButton { Content = ".*", VerticalAlignment = VerticalAlignment.Center };
        ToolTip.SetTip(useRegex, "Regular expression");

        var prev = new Button { Content = "▲", VerticalAlignment = VerticalAlignment.Center };
        ToolTip.SetTip(prev, "Previous match (Shift+Enter)");
        var next = new Button { Content = "▼", VerticalAlignment = VerticalAlignment.Center };
        ToolTip.SetTip(next, "Next match (Enter)");
        var close = new Button { Content = "✕", VerticalAlignment = VerticalAlignment.Center };
        ToolTip.SetTip(close, "Close (Esc)");

        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { _query, matchCase, wholeWord, useRegex, prev, next, _status, close },
        };
        var border = new Border
        {
            Padding = new Thickness(8, 6),
            Child = row,
        };
        border.Classes.Add("toolbar"); // picks up the shared design-system toolbar styling if present
        Content = border;

        _query.TextChanged += (_, _) => { Model.Query = _query.Text ?? ""; RaiseQueryChanged(); };
        _query.KeyDown += OnQueryKey;
        matchCase.IsCheckedChanged += (_, _) => { Model.MatchCase = matchCase.IsChecked ?? false; RaiseQueryChanged(); };
        wholeWord.IsCheckedChanged += (_, _) => { Model.WholeWord = wholeWord.IsChecked ?? false; RaiseQueryChanged(); };
        useRegex.IsCheckedChanged += (_, _) => { Model.UseRegex = useRegex.IsChecked ?? false; RaiseQueryChanged(); };
        prev.Click += (_, _) => { Model.Previous(); SyncStatus(); PreviousRequested?.Invoke(this, EventArgs.Empty); };
        next.Click += (_, _) => { Model.Next(); SyncStatus(); NextRequested?.Invoke(this, EventArgs.Empty); };
        close.Click += (_, _) => CloseRequested?.Invoke(this, EventArgs.Empty);

        SyncStatus();
    }

    /// <summary>Show or hide the bar; showing it focuses and selects the query box.</summary>
    public bool IsOpen
    {
        get => IsVisible;
        set
        {
            IsVisible = value;
            if (value) FocusInput();
        }
    }

    /// <summary>Focuses the query box and selects its text (call when opening the bar).</summary>
    public void FocusInput()
    {
        _query.Focus();
        _query.SelectAll();
    }

    /// <summary>Updates the model's match count and refreshes the "n of m" status label.</summary>
    public void ReportMatches(int total, int current = 1)
    {
        Model.SetMatches(total, current);
        SyncStatus();
    }

    private void SyncStatus() => _status.Text = Model.StatusText;

    private void RaiseQueryChanged() => QueryChanged?.Invoke(this, EventArgs.Empty);

    private void OnQueryKey(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter when e.KeyModifiers.HasFlag(KeyModifiers.Shift):
                Model.Previous(); SyncStatus(); PreviousRequested?.Invoke(this, EventArgs.Empty); e.Handled = true; break;
            case Key.Enter:
                Model.Next(); SyncStatus(); NextRequested?.Invoke(this, EventArgs.Empty); e.Handled = true; break;
            case Key.Escape:
                CloseRequested?.Invoke(this, EventArgs.Empty); e.Handled = true; break;
        }
    }
}
