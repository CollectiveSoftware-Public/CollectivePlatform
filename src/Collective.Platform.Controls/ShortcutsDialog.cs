// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia.Controls;
using Avalonia.Media;

namespace Collective.Platform.Controls;

/// <summary>
/// A shared, grouped keyboard-shortcut reference dialog for the Collective Software desktop
/// heads — one consistent "Keyboard shortcuts" modal instead of each head hand-rolling its own.
/// Code-built like <see cref="AboutDialog"/>; content comes from a <see cref="ShortcutsDialogModel"/>.
/// </summary>
public sealed class ShortcutsDialog : DialogWindow
{
    public ShortcutsDialog() : this(new ShortcutsDialogModel("App", [])) { }

    public ShortcutsDialog(ShortcutsDialogModel model)
    {
        Title = model.Title;
        Width = 460;
        MaxHeight = 560;

        var body = new StackPanel { Spacing = 12 };
        foreach (var (category, items) in model.Groups)
        {
            var header = new TextBlock { Text = category };
            header.Classes.Add("section-header");
            body.Children.Add(header);

            foreach (var item in items)
            {
                var row = new Grid { ColumnDefinitions = new ColumnDefinitions("140,*") };

                var gesture = new TextBlock { Text = item.Gesture, MinWidth = 140 };
                gesture.Classes.Add("muted");
                Grid.SetColumn(gesture, 0);

                var description = new TextBlock { Text = item.Description, TextWrapping = TextWrapping.Wrap };
                Grid.SetColumn(description, 1);

                row.Children.Add(gesture);
                row.Children.Add(description);
                body.Children.Add(row);
            }
        }

        var scroller = new ScrollViewer { Content = body };

        var ok = DialogButtons.Primary("OK");
        ok.IsCancel = true;
        ok.Click += (_, _) => Close();

        Content = DialogScaffold.Build(scroller, ok);
    }

    /// <summary>Show the shortcuts dialog modally over <paramref name="owner"/>.</summary>
    public static Task ShowAsync(Window owner, ShortcutsDialogModel model)
        => new ShortcutsDialog(model).ShowDialog(owner);
}
