// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace Collective.Platform.Controls;

/// <summary>
/// Base class for every secondary (non-main) window in the Collective Software desktop heads.
/// Fixes the stock "ugly black window" generically: the body uses the suite's
/// <c>Collective.Surface</c> token (theme-aware, bound at style priority so a window that sets
/// its own <see cref="Avalonia.Controls.Primitives.TemplatedControl.Background"/> wins), the
/// title-bar/Alt-Tab icon is inherited from the owner window (else the per-app
/// <see cref="DefaultIcon"/>), and windows center over their owner (ownerless ones center on
/// the screen). The OS-drawn title bar stays. Real tool windows derive from this directly;
/// modal prompts use <see cref="DialogWindow"/>.
/// </summary>
public class UtilityWindow : Window
{
    /// <summary>Fallback icon for ownerless utility windows — each head assigns it once at
    /// startup (typically its main window's icon). Owned windows inherit the owner's icon.</summary>
    public static WindowIcon? DefaultIcon { get; set; }

    public UtilityWindow()
    {
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        // Token background at Style priority: any local Background (axaml attribute or code)
        // outranks it, which is the suite's override story.
        Bind(BackgroundProperty, this.GetResourceObservable("Collective.Surface"), BindingPriority.Style);

        Opened += OnUtilityWindowOpened;
    }

    private void OnUtilityWindowOpened(object? sender, EventArgs e)
    {
        Icon ??= (Owner as Window)?.Icon ?? DefaultIcon;

        // CenterOwner has nothing to center on without an owner — center on the screen instead.
        if (Owner is null && WindowStartupLocation == WindowStartupLocation.CenterOwner)
            CenterOnScreen();
    }

    private void CenterOnScreen()
    {
        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        if (screen is null) return;
        var area = screen.WorkingArea;
        var size = PixelSize.FromSize(ClientSize, screen.Scaling);
        Position = new PixelPoint(
            area.X + (area.Width - size.Width) / 2,
            area.Y + (area.Height - size.Height) / 2);
    }
}
