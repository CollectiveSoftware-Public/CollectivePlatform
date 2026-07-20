// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia.Input;

namespace Collective.Platform.Controls;

/// <summary>A gesture bound to two commands — surfaced so a head can catch clashes at startup.</summary>
public sealed record KeymapConflict(KeyGesture Gesture, string ExistingCommandId, string AttemptedCommandId);

/// <summary>
/// The gesture → command-id map derived from a <see cref="CommandRegistry"/>. This is the single
/// place a key press is resolved to an action, replacing each head's bespoke <c>OnKeyDown</c> switch
/// and the menu's display-only <c>InputGesture</c>s — which is what stops the documented double-firing
/// (the keymap owns gestures; menu items only show them). The first command to claim a gesture wins;
/// any later claim is recorded in <see cref="Conflicts"/> rather than silently overriding.
/// </summary>
public sealed class KeymapModel
{
    private readonly Dictionary<KeyGesture, string> _map = new();
    private readonly List<KeymapConflict> _conflicts = new();

    public KeymapModel(IEnumerable<CollectiveCommand> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);
        foreach (var c in commands)
        {
            if (c.DefaultGesture is not { } g) continue;
            if (_map.TryGetValue(g, out string? existing))
                _conflicts.Add(new KeymapConflict(g, existing, c.Id));
            else
                _map[g] = c.Id;
        }
    }

    /// <summary>Gestures that were claimed by more than one command (first claim kept).</summary>
    public IReadOnlyList<KeymapConflict> Conflicts => _conflicts;

    /// <summary>The active bindings (the winning command per gesture).</summary>
    public IReadOnlyDictionary<KeyGesture, string> Bindings => _map;

    /// <summary>The command id bound to <paramref name="gesture"/>, or null if unbound.</summary>
    public string? Resolve(KeyGesture gesture)
    {
        ArgumentNullException.ThrowIfNull(gesture);
        return _map.GetValueOrDefault(gesture);
    }

    /// <summary>Convenience lookup from a raw key + modifiers.</summary>
    public string? Resolve(Key key, KeyModifiers modifiers) => Resolve(new KeyGesture(key, modifiers));
}
