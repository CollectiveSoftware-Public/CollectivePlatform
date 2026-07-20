// SPDX-License-Identifier: GPL-3.0-or-later
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;

namespace Collective.Platform.Controls;

/// <summary>
/// Restores a window's geometry before it is shown and persists it on close, via a
/// <see cref="WindowGeometryStore"/>. Carries the three nuances every head should have: an off-screen
/// guard, preserving the restore bounds while maximized, and running the close-time file I/O off the
/// UI thread (blocking on it from the UI sync-context would deadlock).
/// </summary>
public sealed class WindowStateService
{
    private readonly WindowGeometryStore _store;
    private WindowGeometry? _loaded;

    public WindowStateService(WindowGeometryStore store) => _store = store;

    /// <summary>
    /// Load the persisted geometry. Safe to block on from the UI thread at startup: the only await is
    /// file I/O, marshalled off any captured context via <c>ConfigureAwait(false)</c>, so a caller with
    /// no running message pump can do <c>Apply(window, Load())</c> synchronously before Show() without
    /// risking a deadlock. Returns null when nothing has been saved yet.
    /// </summary>
    public WindowGeometry? Load()
        => _store.LoadAsync().ConfigureAwait(false).GetAwaiter().GetResult();

    /// <summary>
    /// Apply already-loaded <paramref name="geometry"/> to <paramref name="window"/>. Call on the UI
    /// thread before Show(). No-op when <paramref name="geometry"/> is null.
    /// </summary>
    public void Apply(Window window, WindowGeometry? geometry)
    {
        _loaded = geometry;
        if (geometry is null)
            return;

        IReadOnlyList<PixelRect> screens =
            window.Screens?.All.Select(s => s.Bounds).ToList() ?? new List<PixelRect>();
        var placement = WindowPlacement.Resolve(geometry, screens);

        window.Width = placement.Width;
        window.Height = placement.Height;
        window.WindowStartupLocation = placement.StartupLocation;
        if (placement.Position is { } pos)
            window.Position = pos;
        if (placement.Maximized)
            window.WindowState = WindowState.Maximized;
    }

    /// <summary>
    /// Convenience for callers already on a running UI message pump: load + apply in one await. Callers
    /// on the startup thread with no pump should use <see cref="Load"/> + <see cref="Apply"/> instead
    /// (blocking on this method there can deadlock).
    /// </summary>
    public async Task ApplyAsync(Window window)
        => Apply(window, await _store.LoadAsync());

    /// <summary>Wire <paramref name="window"/> to persist its geometry when it closes.</summary>
    public void PersistOnClose(Window window)
        => window.Closing += (_, _) => Save(window);

    private void Save(Window window)
    {
        // Read live values on the UI thread (Width/Height are the restore size even while maximized).
        var maximized = window.WindowState == WindowState.Maximized;
        var geometry = WindowPlacement.Capture(
            window.Width, window.Height, window.Position, maximized, _loaded);

        // Run the file I/O on a pool thread and block; the wait must not need the (blocked) UI
        // sync-context, or close would deadlock.
        Task.Run(async () => await _store.SaveAsync(geometry)).GetAwaiter().GetResult();
    }
}
