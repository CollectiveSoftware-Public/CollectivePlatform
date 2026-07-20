// SPDX-License-Identifier: GPL-3.0-or-later
namespace Collective.Platform.Controls;

/// <summary>UI-free content model for <see cref="ShortcutsDialog"/>: filters blank rows and groups
/// by category (declaration order; null → "General").</summary>
public sealed class ShortcutsDialogModel
{
    public ShortcutsDialogModel(string appName, IReadOnlyList<ShortcutItem> items)
    {
        Title = $"Keyboard shortcuts — {appName}";
        var groups = new List<(string Category, List<ShortcutItem> Items)>();
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Gesture) && string.IsNullOrWhiteSpace(item.Description))
                continue;
            string category = string.IsNullOrWhiteSpace(item.Category) ? "General" : item.Category!;
            var group = groups.FirstOrDefault(g => g.Category == category);
            if (group.Items is null)
                groups.Add((category, group.Items = new List<ShortcutItem>()));
            group.Items.Add(item);
        }
        Groups = groups.Select(g => (g.Category, (IReadOnlyList<ShortcutItem>)g.Items)).ToList();
    }

    public string Title { get; }
    public IReadOnlyList<(string Category, IReadOnlyList<ShortcutItem> Items)> Groups { get; }
}
