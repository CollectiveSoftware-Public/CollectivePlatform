// SPDX-License-Identifier: GPL-3.0-or-later
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;

namespace Collective.Platform.Controls;

/// <summary>
/// Pure geometry math for restoring/capturing a window's placement — the subtle rules desktop heads
/// tend to get wrong: an off-screen guard (don't restore a position that lands on no connected
/// screen) and preserving the restore bounds while maximized. No UI thread needed, so it is
/// unit-tested directly.
/// </summary>
public static class WindowPlacement
{
    /// <summary>The resolved placement to apply to a window before it is shown.</summary>
    public readonly record struct Placement(
        double Width,
        double Height,
        WindowStartupLocation StartupLocation,
        PixelPoint? Position,
        bool Maximized);

    /// <summary>
    /// Decide how to place a window given its saved <paramref name="geometry"/> and the bounds of the
    /// currently connected <paramref name="screens"/>. The saved position is honored only when it
    /// lands inside one of the screens; otherwise the window centers (a monitor may have been
    /// unplugged since the position was saved). An empty screen list means "can't tell" — trust the
    /// saved value.
    /// </summary>
    public static Placement Resolve(WindowGeometry geometry, IReadOnlyList<PixelRect> screens)
    {
        if (geometry.X is { } x && geometry.Y is { } y)
        {
            var point = new PixelPoint(x, y);
            if (IsOnAScreen(point, screens))
                return new Placement(geometry.Width, geometry.Height,
                    WindowStartupLocation.Manual, point, geometry.Maximized);
        }

        return new Placement(geometry.Width, geometry.Height,
            WindowStartupLocation.CenterScreen, null, geometry.Maximized);
    }

    /// <summary>
    /// Capture the geometry to persist. While maximized, the live size/position are the maximized
    /// ones, so the prior restore bounds are kept (re-opening shouldn't trap the window full-size);
    /// only <see cref="WindowGeometry.Maximized"/> flips true. When not maximized, the current
    /// size/position become the restore bounds.
    /// </summary>
    public static WindowGeometry Capture(
        double width, double height, PixelPoint position, bool maximized, WindowGeometry? prior)
    {
        if (maximized)
        {
            return new WindowGeometry
            {
                Width = prior?.Width ?? width,
                Height = prior?.Height ?? height,
                X = prior?.X ?? position.X,
                Y = prior?.Y ?? position.Y,
                Maximized = true,
            };
        }

        return new WindowGeometry
        {
            Width = width,
            Height = height,
            X = position.X,
            Y = position.Y,
            Maximized = false,
        };
    }

    private static bool IsOnAScreen(PixelPoint point, IReadOnlyList<PixelRect> screens)
    {
        if (screens.Count == 0)
            return true; // can't tell — trust the saved value
        foreach (var bounds in screens)
            if (bounds.Contains(point))
                return true;
        return false;
    }
}
