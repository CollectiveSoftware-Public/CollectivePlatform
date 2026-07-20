// SPDX-License-Identifier: GPL-3.0-or-later
using Xunit;

namespace Collective.Platform.Tests;

public class PathTreeBuilderTests
{
    [Fact]
    public void Builds_nested_tree_with_folders_before_leaves()
    {
        var nodes = PathTreeBuilder.Build(new[] { "zeta.md", "a/inner.md", "beta.md" });
        Assert.Equal(new[] { "a", "beta.md", "zeta.md" }, nodes.Select(n => n.Name)); // folder first, then leaves sorted
        Assert.False(nodes[0].IsLeaf);
        Assert.Equal("a/inner.md", nodes[0].Children.Single().Path);
        Assert.True(nodes[1].IsLeaf);
    }

    [Fact]
    public void Sorting_is_case_insensitive()
    {
        var nodes = PathTreeBuilder.Build(new[] { "b.md", "A.md" });
        Assert.Equal(new[] { "A.md", "b.md" }, nodes.Select(n => n.Name));
    }

    [Fact]
    public void LeafName_customizes_display_names_only()
    {
        var nodes = PathTreeBuilder.Build(new[] { "note.md" },
            leafName: n => n.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ? n[..^3] : n);
        Assert.Equal("note", nodes.Single().Name);
        Assert.Equal("note.md", nodes.Single().Path); // path untouched
    }

    [Fact]
    public void Empty_input_yields_empty_tree()
    {
        Assert.Empty(PathTreeBuilder.Build(Array.Empty<string>()));
    }
}
