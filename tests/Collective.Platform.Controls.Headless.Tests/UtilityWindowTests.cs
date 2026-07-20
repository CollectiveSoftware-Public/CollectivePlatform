// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Xunit;

namespace Collective.Platform.Controls.Headless.Tests;

public class UtilityWindowTests
{
    [AvaloniaFact]
    public void Defaults_to_center_owner_and_token_background()
    {
        var window = new UtilityWindow();
        Assert.Equal(WindowStartupLocation.CenterOwner, window.WindowStartupLocation);
        window.Show(); // TestApp merges Tokens.axaml, so Collective.Surface resolves
        Assert.NotNull(window.Background);
        window.Close();
    }

    [AvaloniaFact]
    public void Local_background_wins_over_the_token_binding()
    {
        var window = new UtilityWindow { Background = Brushes.Red };
        window.Show();
        Assert.Equal(Brushes.Red, window.Background);
        window.Close();
    }

    [AvaloniaFact]
    public void Owned_window_inherits_the_owner_icon_on_open()
    {
        var owner = new Window { Icon = TestIcons.Any() };
        owner.Show();
        var child = new UtilityWindow();
        child.Show(owner);
        Assert.Same(owner.Icon, child.Icon);
        child.Close();
        owner.Close();
    }

    [AvaloniaFact]
    public void Ownerless_window_falls_back_to_the_default_icon()
    {
        UtilityWindow.DefaultIcon = TestIcons.Any();
        try
        {
            var window = new UtilityWindow();
            window.Show();
            Assert.Same(UtilityWindow.DefaultIcon, window.Icon);
            window.Close();
        }
        finally { UtilityWindow.DefaultIcon = null; }
    }

    [AvaloniaFact]
    public void Icon_fallback_never_throws_when_nothing_is_set()
    {
        var window = new UtilityWindow();
        window.Show(); // no owner, no DefaultIcon — must not throw
        Assert.Null(window.Icon);
        window.Close();
    }

    [AvaloniaFact]
    public void Explicit_icon_is_never_overwritten()
    {
        var mine = TestIcons.Any();
        var owner = new Window { Icon = TestIcons.Any() };
        owner.Show();
        var child = new UtilityWindow { Icon = mine };
        child.Show(owner);
        Assert.Same(mine, child.Icon);
        child.Close();
        owner.Close();
    }
}
