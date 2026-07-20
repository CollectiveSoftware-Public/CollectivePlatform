// SPDX-License-Identifier: GPL-3.0-or-later
using Collective.Platform.Controls;
using Xunit;

namespace Collective.Platform.Controls.Tests;

public class BreadcrumbTrailTests
{
    // BreadcrumbTrail normalizes to the OS-native separator and derives the root via Path.GetPathRoot,
    // so its output is OS-native: "C:\..." on Windows, "/..." on Unix. Build inputs and expectations
    // from the running OS's root + separator so the same behaviour is exercised on every platform.
    private static readonly char Sep = Path.DirectorySeparatorChar;
    private static readonly char Foreign = Sep == '\\' ? '/' : '\\';
    private static readonly string Root = OperatingSystem.IsWindows() ? @"C:\" : "/";

    // An absolute, OS-native path: P("a","b") => "C:\a\b" on Windows, "/a/b" on Unix.
    private static string P(params string[] parts) => Root.TrimEnd(Sep) + Sep + string.Join(Sep, parts);

    [Fact]
    public void Unclamped_walks_to_the_filesystem_root()
    {
        var segs = BreadcrumbTrail.Build(P("a", "b", "c"));

        Assert.Equal(new[] { Root, "a", "b", "c" }, segs.Select(s => s.Name));
        Assert.Equal(Root, segs[0].FullPath);
        Assert.Equal(P("a"), segs[1].FullPath);
        Assert.Equal(P("a", "b"), segs[2].FullPath);
        Assert.Equal(P("a", "b", "c"), segs[3].FullPath);
    }

    [Fact]
    public void Clamped_starts_at_the_root_leaf()
    {
        var segs = BreadcrumbTrail.Build(P("a", "b", "c"), clampRoot: P("a"));

        Assert.Equal(new[] { "a", "b", "c" }, segs.Select(s => s.Name));
        Assert.Equal(P("a"), segs[0].FullPath);
        Assert.Equal(P("a", "b"), segs[1].FullPath);
        Assert.Equal(P("a", "b", "c"), segs[2].FullPath);
    }

    [Fact]
    public void Path_equal_to_clamp_is_a_single_segment()
    {
        var segs = BreadcrumbTrail.Build(P("proj"), clampRoot: P("proj"));

        Assert.Single(segs);
        Assert.Equal("proj", segs[0].Name);
        Assert.Equal(P("proj"), segs[0].FullPath);
    }

    [Fact]
    public void Foreign_separators_are_normalized()
    {
        // Feed the NON-native separator; the trail must still produce OS-native full paths.
        var path = Root + "a" + Foreign + "b";
        var root = Root + "a";
        var segs = BreadcrumbTrail.Build(path, clampRoot: root);

        Assert.Equal(new[] { "a", "b" }, segs.Select(s => s.Name));
        Assert.Equal(P("a", "b"), segs[1].FullPath);
    }

    [Fact]
    public void Trailing_separators_are_ignored()
    {
        var segs = BreadcrumbTrail.Build(P("a", "b") + Sep, clampRoot: P("a"));
        Assert.Equal(new[] { "a", "b" }, segs.Select(s => s.Name));
    }

    [Theory]
    [InlineData(new[] { "proj" }, new[] { "proj" }, true)]
    [InlineData(new[] { "proj", "src" }, new[] { "proj" }, true)]
    [InlineData(new[] { "other" }, new[] { "proj" }, false)]
    [InlineData(new[] { "projx" }, new[] { "proj" }, false)] // prefix but not a path boundary
    public void IsWithin_guards_the_clamp(string[] pathParts, string[] rootParts, bool expected)
        => Assert.Equal(expected, BreadcrumbTrail.IsWithin(P(pathParts), P(rootParts)));
}
