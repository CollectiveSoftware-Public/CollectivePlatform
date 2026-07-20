// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Collective.Platform.Controls;

/// <summary>
/// Installs a single <c>KeyDown</c> handler on a window (or any input element) that resolves the
/// pressed gesture through the registry's <see cref="KeymapModel"/> and runs the matching command.
/// This is the one place key presses become actions — it replaces each head's bespoke <c>OnKeyDown</c>
/// switch, and because the keymap (not the menu) owns gestures, it removes the double-fire hazard.
/// Handles on bubble so a focused editor/text box still gets first crack at the key.
/// </summary>
public sealed class KeymapBehavior
{
    private readonly CommandRegistry _registry;
    private readonly KeymapModel _keymap;

    private KeymapBehavior(CommandRegistry registry)
    {
        _registry = registry;
        _keymap = registry.BuildKeymap();
    }

    /// <summary>Gesture clashes detected while building the keymap (first claim kept).</summary>
    public IReadOnlyList<KeymapConflict> Conflicts => _keymap.Conflicts;

    /// <summary>Attaches the keymap to <paramref name="target"/> and returns the behaviour (for inspection).</summary>
    public static KeymapBehavior Attach(InputElement target, CommandRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(registry);
        var behavior = new KeymapBehavior(registry);
        target.AddHandler(InputElement.KeyDownEvent, behavior.OnKeyDown, RoutingStrategies.Bubble);
        return behavior;
    }

    private async void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled) return;
        if (_keymap.Resolve(new KeyGesture(e.Key, e.KeyModifiers)) is not { } id) return;
        if (_registry.Get(id) is not { } cmd || !cmd.CanRun) return;

        e.Handled = true;
        await cmd.RunAsync();
    }
}
