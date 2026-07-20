// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia.Input;
using Collective.Platform.Controls;
using Xunit;

namespace Collective.Platform.Controls.Tests;

/// <summary>
/// The keymap is the one place a gesture resolves to an action; these cover resolution, unbound
/// gestures, commands without a gesture, and the conflict detection that replaces silent overrides.
/// </summary>
public sealed class KeymapModelTests
{
    private static CollectiveCommand Cmd(string id, Key key, KeyModifiers mods) =>
        new(id, id, "X", () => { }, new KeyGesture(key, mods));

    [Fact]
    public void Resolves_a_bound_gesture()
    {
        var map = new KeymapModel([Cmd("save", Key.S, KeyModifiers.Control)]);
        Assert.Equal("save", map.Resolve(Key.S, KeyModifiers.Control));
    }

    [Fact]
    public void Unbound_gesture_resolves_to_null()
    {
        var map = new KeymapModel([Cmd("save", Key.S, KeyModifiers.Control)]);
        Assert.Null(map.Resolve(Key.P, KeyModifiers.Control));
        Assert.Null(map.Resolve(Key.S, KeyModifiers.None));   // modifiers matter
    }

    [Fact]
    public void Commands_without_a_gesture_are_ignored()
    {
        var noGesture = new CollectiveCommand("x", "X", "X", () => { });
        var map = new KeymapModel([noGesture]);
        Assert.Empty(map.Bindings);
        Assert.Empty(map.Conflicts);
    }

    [Fact]
    public void First_claim_wins_and_conflict_is_recorded()
    {
        var map = new KeymapModel(
        [
            Cmd("first", Key.K, KeyModifiers.Control),
            Cmd("second", Key.K, KeyModifiers.Control),
        ]);

        Assert.Equal("first", map.Resolve(Key.K, KeyModifiers.Control));
        var conflict = Assert.Single(map.Conflicts);
        Assert.Equal("first", conflict.ExistingCommandId);
        Assert.Equal("second", conflict.AttemptedCommandId);
    }
}
