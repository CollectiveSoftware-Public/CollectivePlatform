// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Collective.Platform.Controls;
using Xunit;

namespace Collective.Platform.Controls.Tests;

public class WindowPlacementTests
{
    private static readonly IReadOnlyList<PixelRect> OneScreen =
        new[] { new PixelRect(0, 0, 1920, 1080) };

    [Fact]
    public void Resolve_position_on_screen_is_honored_manually()
    {
        var g = new WindowGeometry { Width = 800, Height = 600, X = 100, Y = 100 };
        var p = WindowPlacement.Resolve(g, OneScreen);
        Assert.Equal(WindowStartupLocation.Manual, p.StartupLocation);
        Assert.Equal(new PixelPoint(100, 100), p.Position);
        Assert.Equal(800, p.Width);
        Assert.Equal(600, p.Height);
    }

    [Fact]
    public void Resolve_offscreen_position_centers()
    {
        var g = new WindowGeometry { Width = 800, Height = 600, X = 5000, Y = 5000 };
        var p = WindowPlacement.Resolve(g, OneScreen);
        Assert.Equal(WindowStartupLocation.CenterScreen, p.StartupLocation);
        Assert.Null(p.Position);
    }

    [Fact]
    public void Resolve_null_position_centers()
    {
        var g = new WindowGeometry { Width = 800, Height = 600, X = null, Y = null };
        var p = WindowPlacement.Resolve(g, OneScreen);
        Assert.Equal(WindowStartupLocation.CenterScreen, p.StartupLocation);
    }

    [Fact]
    public void Resolve_empty_screens_trusts_saved_value()
    {
        var g = new WindowGeometry { Width = 800, Height = 600, X = 5000, Y = 5000 };
        var p = WindowPlacement.Resolve(g, Array.Empty<PixelRect>());
        Assert.Equal(WindowStartupLocation.Manual, p.StartupLocation);
    }

    [Fact]
    public void Resolve_point_on_second_monitor_is_honored()
    {
        var screens = new[]
        {
            new PixelRect(0, 0, 1920, 1080),
            new PixelRect(1920, 0, 1920, 1080),
        };
        var g = new WindowGeometry { Width = 800, Height = 600, X = 2500, Y = 200 };
        var p = WindowPlacement.Resolve(g, screens);
        Assert.Equal(WindowStartupLocation.Manual, p.StartupLocation);
    }

    [Fact]
    public void Capture_when_maximized_preserves_prior_restore_bounds()
    {
        var prior = new WindowGeometry { Width = 800, Height = 600, X = 100, Y = 120 };
        var g = WindowPlacement.Capture(1920, 1080, new PixelPoint(0, 0), maximized: true, prior);
        Assert.True(g.Maximized);
        Assert.Equal(800, g.Width);
        Assert.Equal(600, g.Height);
        Assert.Equal(100, g.X);
        Assert.Equal(120, g.Y);
    }

    [Fact]
    public void Capture_when_not_maximized_uses_current_bounds()
    {
        var g = WindowPlacement.Capture(1024, 768, new PixelPoint(50, 60), maximized: false, prior: null);
        Assert.False(g.Maximized);
        Assert.Equal(1024, g.Width);
        Assert.Equal(768, g.Height);
        Assert.Equal(50, g.X);
        Assert.Equal(60, g.Y);
    }
}
