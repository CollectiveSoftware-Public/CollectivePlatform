// SPDX-License-Identifier: GPL-3.0-or-later
using Collective.Platform.Controls;
using Xunit;

namespace Collective.Platform.Controls.Tests;

public class AboutDialogModelTests
{
    [Fact]
    public void Blank_app_name_falls_back_to_about()
        => Assert.Equal("About", new AboutDialogModel("   ").AppName);

    [Fact]
    public void Trims_and_keeps_values()
    {
        var m = new AboutDialogModel("  CollectiveCode  ", "1.2.3", "  A code editor ",
            new[] { " line one ", "  ", "line two" });
        Assert.Equal("CollectiveCode", m.AppName);
        Assert.Equal("1.2.3", m.Version);
        Assert.Equal("A code editor", m.Tagline);
        Assert.Equal(new[] { "line one", "line two" }, m.Lines);
    }

    [Fact]
    public void Null_version_and_lines_normalize()
    {
        var m = new AboutDialogModel("X");
        Assert.Null(m.Version);
        Assert.Null(m.Tagline);
        Assert.Empty(m.Lines);
    }
}
