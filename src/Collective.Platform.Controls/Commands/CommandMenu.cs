// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia.Controls;

namespace Collective.Platform.Controls;

/// <summary>
/// Builds menu items from a <see cref="CommandRegistry"/> so a head's menu is generated from the same
/// command source as its keymap and palette. Each command's gesture is shown as a display-only
/// <see cref="MenuItem.InputGesture"/> (the keymap, not the menu, actually executes it — which is what
/// avoids the documented double-firing); building in code also sidesteps the XAML-load gesture throws.
/// </summary>
public static class CommandMenu
{
    /// <summary>One top-level <see cref="MenuItem"/> per category, each holding its commands in order.</summary>
    public static IReadOnlyList<MenuItem> BuildCategories(CommandRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        var tops = new List<MenuItem>();
        foreach (var group in registry.ByCategory)
        {
            var top = new MenuItem { Header = group.Key };
            foreach (var cmd in group)
                top.Items.Add(BuildItem(cmd));
            tops.Add(top);
        }
        return tops;
    }

    /// <summary>A single menu item wired to <paramref name="cmd"/> (gesture display-only, Click runs it).</summary>
    public static MenuItem BuildItem(CollectiveCommand cmd)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var item = new MenuItem { Header = cmd.Title };
        if (cmd.DefaultGesture is { } g) item.InputGesture = g; // display only — not a hotkey binding
        item.Click += async (_, _) => await cmd.RunAsync();
        return item;
    }
}
