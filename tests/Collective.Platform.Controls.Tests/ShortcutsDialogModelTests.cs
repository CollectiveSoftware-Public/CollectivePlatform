// SPDX-License-Identifier: GPL-3.0-or-later
using System.Linq;
using Collective.Platform.Controls;
using Xunit;

namespace Collective.Platform.Controls.Tests;

public class ShortcutsDialogModelTests
{
    [Fact]
    public void Groups_by_category_preserving_declaration_order()
    {
        var m = new ShortcutsDialogModel("App", new ShortcutItem[]
        {
            new("Ctrl+S", "Save", "File"),
            new("Ctrl+F", "Find", "Edit"),
            new("Ctrl+O", "Open", "File"),
        });
        Assert.Equal(new[] { "File", "Edit" }, m.Groups.Select(g => g.Category));
        Assert.Equal(new[] { "Save", "Open" }, m.Groups[0].Items.Select(i => i.Description));
    }

    [Fact]
    public void Null_category_becomes_general_and_blank_items_are_dropped()
    {
        var m = new ShortcutsDialogModel("App", new ShortcutItem[]
        {
            new("Ctrl+Q", "Quit"),
            new("", "  "),
        });
        Assert.Single(m.Groups);
        Assert.Equal("General", m.Groups[0].Category);
        Assert.Single(m.Groups[0].Items);
    }

    [Fact]
    public void Title_names_the_app()
        => Assert.Equal("Keyboard shortcuts — CollectiveGit",
            new ShortcutsDialogModel("CollectiveGit", []).Title);
}
