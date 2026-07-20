// SPDX-License-Identifier: GPL-3.0-or-later
using Collective.Platform.Controls;
using Xunit;

namespace Collective.Platform.Controls.Tests;

public class BreadcrumbTrailTests
{
    [Fact]
    public void Unclamped_walks_to_the_drive_root()
    {
        var segs = BreadcrumbTrail.Build(@"C:\a\b\c");

        Assert.Equal(new[] { @"C:\", "a", "b", "c" }, segs.Select(s => s.Name));
        Assert.Equal(@"C:\", segs[0].FullPath);
        Assert.Equal(@"C:\a", segs[1].FullPath);
        Assert.Equal(@"C:\a\b", segs[2].FullPath);
        Assert.Equal(@"C:\a\b\c", segs[3].FullPath);
    }

    [Fact]
    public void Clamped_starts_at_the_root_leaf()
    {
        var segs = BreadcrumbTrail.Build(@"C:\a\b\c", clampRoot: @"C:\a");

        Assert.Equal(new[] { "a", "b", "c" }, segs.Select(s => s.Name));
        Assert.Equal(@"C:\a", segs[0].FullPath);
        Assert.Equal(@"C:\a\b", segs[1].FullPath);
        Assert.Equal(@"C:\a\b\c", segs[2].FullPath);
    }

    [Fact]
    public void Path_equal_to_clamp_is_a_single_segment()
    {
        var segs = BreadcrumbTrail.Build(@"C:\proj", clampRoot: @"C:\proj");

        Assert.Single(segs);
        Assert.Equal("proj", segs[0].Name);
        Assert.Equal(@"C:\proj", segs[0].FullPath);
    }

    [Fact]
    public void Forward_slashes_are_normalized()
    {
        var segs = BreadcrumbTrail.Build("C:/a/b", clampRoot: "C:/a");

        Assert.Equal(new[] { "a", "b" }, segs.Select(s => s.Name));
        Assert.Equal(@"C:\a\b", segs[1].FullPath);
    }

    [Fact]
    public void Trailing_separators_are_ignored()
    {
        var segs = BreadcrumbTrail.Build(@"C:\a\b\", clampRoot: @"C:\a");
        Assert.Equal(new[] { "a", "b" }, segs.Select(s => s.Name));
    }

    [Theory]
    [InlineData(@"C:\proj", @"C:\proj", true)]
    [InlineData(@"C:\proj\src", @"C:\proj", true)]
    [InlineData(@"C:\other", @"C:\proj", false)]
    [InlineData(@"C:\projx", @"C:\proj", false)] // prefix but not a path boundary
    public void IsWithin_guards_the_clamp(string path, string root, bool expected)
        => Assert.Equal(expected, BreadcrumbTrail.IsWithin(path, root));
}
