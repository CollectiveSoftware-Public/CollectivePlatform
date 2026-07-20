// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia.Input;

namespace Collective.Platform.Controls;

/// <summary>
/// A single named action in a desktop head — the unit the whole command system is built from.
/// One registration carries everything the suite's UI mechanics need: a stable <see cref="Id"/>,
/// a human <see cref="Title"/> for the palette/menu, a <see cref="Category"/> for grouping, an
/// optional <see cref="DefaultGesture"/> for the keymap, and the action to <see cref="RunAsync"/>.
///
/// This is the single source of truth that replaces the historic per-head triplication of the same
/// action across a menu <c>Click</c> handler, a palette entry, and a key switch (the documented
/// double-fire / XAML-throw hazard). The type holds no UI state, so it is unit-testable on its own.
/// </summary>
public sealed class CollectiveCommand
{
    private readonly Func<Task> _run;
    private readonly Func<bool>? _canRun;

    /// <summary>Stable identifier, e.g. <c>"file.save"</c>. Unique within a <see cref="CommandRegistry"/>.</summary>
    public string Id { get; }

    /// <summary>Human-readable label shown in the palette, menu, and shortcuts help.</summary>
    public string Title { get; }

    /// <summary>Grouping bucket for menus / shortcuts help, e.g. <c>"File"</c>. Never null.</summary>
    public string Category { get; }

    /// <summary>The default keyboard gesture, or null if the command has none.</summary>
    public KeyGesture? DefaultGesture { get; }

    public CollectiveCommand(string id, string title, string category, Func<Task> run,
        KeyGesture? defaultGesture = null, Func<bool>? canRun = null)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Command id is required.", nameof(id));
        Id = id;
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Category = category ?? "";
        _run = run ?? throw new ArgumentNullException(nameof(run));
        DefaultGesture = defaultGesture;
        _canRun = canRun;
    }

    /// <summary>Convenience overload for a synchronous action.</summary>
    public CollectiveCommand(string id, string title, string category, Action run,
        KeyGesture? defaultGesture = null, Func<bool>? canRun = null)
        : this(id, title, category, ToAsync(run), defaultGesture, canRun) { }

    /// <summary>Whether the command is currently runnable (drives menu/palette enablement).</summary>
    public bool CanRun => _canRun?.Invoke() ?? true;

    /// <summary>Runs the command if <see cref="CanRun"/>; otherwise a no-op.</summary>
    public Task RunAsync() => CanRun ? _run() : Task.CompletedTask;

    private static Func<Task> ToAsync(Action run)
    {
        ArgumentNullException.ThrowIfNull(run);
        return () => { run(); return Task.CompletedTask; };
    }
}
