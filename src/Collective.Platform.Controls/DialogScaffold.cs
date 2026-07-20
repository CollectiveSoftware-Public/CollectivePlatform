// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Collective.Platform.Controls;

/// <summary>
/// The standard content scaffold for code-built dialogs: 20px outer padding, 16px section
/// spacing, and a right-aligned footer button row with 8px gaps — so every prompt in the suite
/// reads the same. <c>.axaml</c>-built windows get the identical footer via the
/// <c>StackPanel.dialog-footer</c> style class instead.
/// </summary>
public static class DialogScaffold
{
    public static Control Build(Control body, params Button[] footer)
    {
        var root = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 16,
            Children = { body },
        };

        if (footer.Length > 0)
        {
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 8,
            };
            row.Classes.Add("dialog-footer");
            foreach (var button in footer) row.Children.Add(button);
            root.Children.Add(row);
        }

        return root;
    }
}

/// <summary>Standard dialog buttons: Primary = accent + default, Secondary = plain + cancel,
/// Danger = destructive + default. All <c>MinWidth = 80</c>.</summary>
public static class DialogButtons
{
    public static Button Primary(string text)
    {
        var button = new Button { Content = text, MinWidth = 80, IsDefault = true };
        button.Classes.Add("accent");
        return button;
    }

    public static Button Secondary(string text)
        => new() { Content = text, MinWidth = 80, IsCancel = true };

    public static Button Danger(string text)
    {
        var button = new Button { Content = text, MinWidth = 80, IsDefault = true };
        button.Classes.Add("danger");
        return button;
    }
}
