// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Collective.Platform.Controls;

/// <summary>
/// The suite-wide command palette: type to fuzzy-filter, Up/Down to move, Enter to run, Esc to cancel.
/// Generalises CollectiveCode's original palette so every head gets one for free — it is driven by the
/// head's <see cref="CommandRegistry"/> and ranks with <see cref="CommandMatcher"/>, and shows each
/// command's gesture on the right. The chosen command is exposed via <see cref="Chosen"/>; the static
/// <see cref="ShowAsync"/> helper also runs it.
/// </summary>
public sealed class CommandPalette : DialogWindow
{
    private readonly IReadOnlyList<CollectiveCommand> _all;
    private readonly TextBox _box;
    private readonly ListBox _list;

    /// <summary>The command the user picked, or null if they cancelled.</summary>
    public CollectiveCommand? Chosen { get; private set; }

    public CommandPalette(IReadOnlyList<CollectiveCommand> commands)
    {
        _all = commands;
        Title = "Commands";
        SizeToContent = SizeToContent.Manual; // fixed palette height, not height-to-content
        Width = 520;
        Height = 420;

        _box = new TextBox { PlaceholderText = "Type a command…", Margin = new Thickness(8) };
        _list = new ListBox
        {
            Margin = new Thickness(8, 0, 8, 8),
            ItemTemplate = new FuncDataTemplate<CollectiveCommand>((c, _) => BuildRow(c), supportsRecycling: true),
        };
        SetItems(_all);

        var dock = new DockPanel();
        DockPanel.SetDock(_box, Dock.Top);
        dock.Children.Add(_box);
        dock.Children.Add(_list);
        Content = dock;

        _box.TextChanged += (_, _) => Filter();
        _box.KeyDown += OnKey;
        _list.DoubleTapped += (_, _) => Accept();
        Opened += (_, _) => _box.Focus();
    }

    private static Control BuildRow(CollectiveCommand? c)
    {
        var title = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        var gesture = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Opacity = 0.6,
            Margin = new Thickness(12, 0, 0, 0),
        };
        if (c is not null)
        {
            title.Text = c.Title;
            gesture.Text = c.DefaultGesture?.ToString() ?? "";
        }
        var dock = new DockPanel();
        DockPanel.SetDock(gesture, Dock.Right);
        dock.Children.Add(gesture);
        dock.Children.Add(title);
        return dock;
    }

    private void SetItems(IReadOnlyList<CollectiveCommand> items)
    {
        _list.ItemsSource = items;
        _list.SelectedIndex = items.Count > 0 ? 0 : -1;
    }

    private void Filter() => SetItems(CommandMatcher.Filter(_all, _box.Text));

    private void OnKey(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter: Accept(); e.Handled = true; break;
            case Key.Escape: Close(); e.Handled = true; break;
            case Key.Down: if (_list.SelectedIndex < _list.ItemCount - 1) _list.SelectedIndex++; e.Handled = true; break;
            case Key.Up: if (_list.SelectedIndex > 0) _list.SelectedIndex--; e.Handled = true; break;
        }
    }

    private void Accept()
    {
        Chosen = _list.SelectedItem as CollectiveCommand;
        Close();
    }

    /// <summary>Shows the palette modally over <paramref name="owner"/> and runs the chosen command.</summary>
    public static async Task ShowAsync(Window owner, IReadOnlyList<CollectiveCommand> commands)
    {
        var palette = new CommandPalette(commands);
        await palette.ShowDialog(owner);
        if (palette.Chosen is { } c) await c.RunAsync();
    }
}
