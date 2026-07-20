// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia.Input;

namespace Collective.Platform.Controls;

/// <summary>
/// The per-head registry of <see cref="CollectiveCommand"/>s — the backbone of the shared UI
/// mechanics. A head registers each action once; the menu, keymap, command palette, and the
/// shortcuts-help list are all derived from this single collection, so they can no longer drift.
///
/// Registration order is preserved (it drives the natural ordering of menus and the palette).
/// Ids are unique; a duplicate is a programming error and throws.
/// </summary>
public sealed class CommandRegistry
{
    private readonly List<CollectiveCommand> _ordered = new();
    private readonly Dictionary<string, CollectiveCommand> _byId = new(StringComparer.Ordinal);

    /// <summary>Registers a pre-built command. Throws if its id is already taken.</summary>
    public CollectiveCommand Register(CollectiveCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!_byId.TryAdd(command.Id, command))
            throw new ArgumentException($"A command with id '{command.Id}' is already registered.", nameof(command));
        _ordered.Add(command);
        return command;
    }

    /// <summary>Registers a synchronous command inline.</summary>
    public CollectiveCommand Register(string id, string title, string category, Action run,
        KeyGesture? gesture = null, Func<bool>? canRun = null)
        => Register(new CollectiveCommand(id, title, category, run, gesture, canRun));

    /// <summary>Registers an asynchronous command inline.</summary>
    public CollectiveCommand Register(string id, string title, string category, Func<Task> run,
        KeyGesture? gesture = null, Func<bool>? canRun = null)
        => Register(new CollectiveCommand(id, title, category, run, gesture, canRun));

    /// <summary>The command with <paramref name="id"/>, or null if none is registered.</summary>
    public CollectiveCommand? Get(string id) => _byId.GetValueOrDefault(id);

    /// <summary>All commands, in registration order.</summary>
    public IReadOnlyList<CollectiveCommand> All => _ordered;

    /// <summary>Commands grouped by <see cref="CollectiveCommand.Category"/>, preserving first-seen order.</summary>
    public IReadOnlyList<IGrouping<string, CollectiveCommand>> ByCategory =>
        _ordered.GroupBy(c => c.Category).ToList();

    /// <summary>Builds the gesture → command map (with conflict detection) for the keymap behaviour.</summary>
    public KeymapModel BuildKeymap() => new(_ordered);
}
