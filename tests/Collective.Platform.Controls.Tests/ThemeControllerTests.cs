// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia.Styling;
using Collective.Platform.Controls;
using Xunit;

namespace Collective.Platform.Controls.Tests;

public class ThemeControllerTests
{
    [Fact]
    public void For_light_maps_to_light()
        => Assert.Equal(ThemeVariant.Light, ThemeController.For("Light"));

    [Fact]
    public void For_dark_maps_to_dark()
        => Assert.Equal(ThemeVariant.Dark, ThemeController.For("Dark"));

    [Theory]
    [InlineData("System")]
    [InlineData("")]
    [InlineData("bogus")]
    [InlineData(null)]
    public void For_system_or_unknown_maps_to_default(string? mode)
        => Assert.Equal(ThemeVariant.Default, ThemeController.For(mode));
}
