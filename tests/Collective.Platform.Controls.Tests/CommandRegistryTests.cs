// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia.Input;
using Collective.Platform.Controls;
using Xunit;

namespace Collective.Platform.Controls.Tests;

/// <summary>
/// The registry is the single source of truth a head registers each action into; these cover
/// ordering, lookup, the duplicate-id guard, category grouping, and CanRun/RunAsync dispatch.
/// </summary>
public sealed class CommandRegistryTests
{
    [Fact]
    public void All_preserves_registration_order()
    {
        var r = new CommandRegistry();
        r.Register("a", "Alpha", "File", () => { });
        r.Register("b", "Beta", "File", () => { });
        r.Register("c", "Gamma", "Edit", () => { });

        Assert.Equal(["a", "b", "c"], r.All.Select(c => c.Id));
    }

    [Fact]
    public void Get_returns_the_command_or_null()
    {
        var r = new CommandRegistry();
        r.Register("file.save", "Save", "File", () => { });

        Assert.Equal("Save", r.Get("file.save")!.Title);
        Assert.Null(r.Get("missing"));
    }

    [Fact]
    public void Duplicate_id_throws()
    {
        var r = new CommandRegistry();
        r.Register("dup", "One", "File", () => { });

        Assert.Throws<ArgumentException>(() => r.Register("dup", "Two", "Edit", () => { }));
    }

    [Fact]
    public void ByCategory_groups_in_first_seen_order()
    {
        var r = new CommandRegistry();
        r.Register("a", "Alpha", "File", () => { });
        r.Register("c", "Gamma", "Edit", () => { });
        r.Register("b", "Beta", "File", () => { });

        var groups = r.ByCategory;
        Assert.Equal(["File", "Edit"], groups.Select(g => g.Key));
        Assert.Equal(["a", "b"], groups[0].Select(c => c.Id));
    }

    [Fact]
    public async Task RunAsync_honours_CanRun()
    {
        int ran = 0;
        var enabled = new CollectiveCommand("on", "On", "X", () => ran++, canRun: () => true);
        var disabled = new CollectiveCommand("off", "Off", "X", () => ran++, canRun: () => false);

        await enabled.RunAsync();
        await disabled.RunAsync();

        Assert.Equal(1, ran);
        Assert.True(enabled.CanRun);
        Assert.False(disabled.CanRun);
    }

    [Fact]
    public void Gesture_is_carried_through_to_the_keymap()
    {
        var r = new CommandRegistry();
        r.Register("file.save", "Save", "File", () => { }, new KeyGesture(Key.S, KeyModifiers.Control));

        Assert.Equal("file.save", r.BuildKeymap().Resolve(Key.S, KeyModifiers.Control));
    }
}
