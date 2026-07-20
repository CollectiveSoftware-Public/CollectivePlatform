// SPDX-License-Identifier: GPL-3.0-or-later
using Collective.Platform.Testing;
using Xunit;

namespace Collective.Platform.Tests;

public class FileTreeNodeTests
{
    private static FakeDirectoryFileSystem SampleFs()
    {
        var fs = new FakeDirectoryFileSystem();
        fs.AddFile("root/readme.md");
        fs.AddFile("root/src/a.cs");
        fs.AddDirectory("root/empty");
        fs.AddFile("root/bin/skipped.dll");
        return fs;
    }

    [Fact]
    public async Task Root_loads_immediate_children_dirs_first_filtered()
    {
        var root = await FileTreeNode.CreateRootAsync(SampleFs(), "root");
        Assert.Equal("root", root.Name);
        // bin is filtered; dirs (empty, src) come before files (readme.md)
        Assert.Equal(new[] { "empty", "src", "readme.md" }, root.Children.Select(c => c.Name));
    }

    [Fact]
    public async Task Probe_mode_gives_chevron_placeholder_only_to_nonempty_folders()
    {
        var root = await FileTreeNode.CreateRootAsync(SampleFs(), "root");
        var empty = root.Children.Single(c => c.Name == "empty");
        var src = root.Children.Single(c => c.Name == "src");
        Assert.Empty(empty.Children);        // no placeholder — nothing visible inside
        Assert.Single(src.Children);         // placeholder present, real content not yet loaded
        Assert.Equal("", src.Children[0].Name);
    }

    [Fact]
    public async Task AlwaysShow_mode_skips_probing()
    {
        var root = await FileTreeNode.CreateRootAsync(SampleFs(), "root",
            mode: FileTreeExpanderMode.AlwaysShowForDirectories);
        var empty = root.Children.Single(c => c.Name == "empty");
        Assert.Single(empty.Children);       // unconditional placeholder
    }

    [Fact]
    public async Task Expanding_loads_real_children_once()
    {
        var root = await FileTreeNode.CreateRootAsync(SampleFs(), "root");
        var src = root.Children.Single(c => c.Name == "src");
        src.IsExpanded = true;               // fire-and-forget kick…
        await src.EnsureChildrenLoadedAsync(); // …idempotent explicit await for the test
        Assert.Equal(new[] { "a.cs" }, src.Children.Select(c => c.Name));
        Assert.Same(src, src.Children[0].Parent);
    }

    [Fact]
    public async Task Reload_rereads_and_keeps_expansion()
    {
        var fs = SampleFs();
        var root = await FileTreeNode.CreateRootAsync(fs, "root");
        var src = root.Children.Single(c => c.Name == "src");
        src.IsExpanded = true;
        await src.EnsureChildrenLoadedAsync();

        fs.AddFile("root/src/b.cs");
        await src.ReloadAsync();

        Assert.Equal(new[] { "a.cs", "b.cs" }, src.Children.Select(c => c.Name));
        Assert.True(src.IsExpanded);
    }

    [Fact]
    public async Task File_reload_delegates_to_parent()
    {
        var fs = SampleFs();
        var root = await FileTreeNode.CreateRootAsync(fs, "root");
        fs.AddFile("root/new.txt");
        await root.Children.Single(c => c.Name == "readme.md").ReloadAsync();
        Assert.Contains(root.Children, c => c.Name == "new.txt");
    }

    [Fact]
    public async Task Drive_like_root_name_falls_back_to_raw_path()
    {
        var fs = new FakeDirectoryFileSystem(); // "" root lists nothing for "/"
        var root = await FileTreeNode.CreateRootAsync(fs, "/");
        Assert.Equal("/", root.Name);
    }
}
