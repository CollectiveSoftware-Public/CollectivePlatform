// SPDX-License-Identifier: GPL-3.0-or-later
using System.Collections.Generic;
using System.Linq;
using Collective.Platform;
using Xunit;

namespace Collective.Platform.Tests;

public class RecentItemsTests
{
    [Fact]
    public void Add_puts_the_item_first()
        => Assert.Equal(new[] { "b", "a" }, RecentItems.Add(new[] { "a" }, "b"));

    [Fact]
    public void Add_dedupes_case_insensitively()
        => Assert.Equal(new[] { "C:\\Repo", "x" }, RecentItems.Add(new[] { "x", "c:\\repo" }, "C:\\Repo"));

    [Fact]
    public void Add_caps_the_list()
    {
        var ten = Enumerable.Range(0, 10).Select(i => $"p{i}").ToList();
        var result = RecentItems.Add(ten, "new", max: 10);
        Assert.Equal(10, result.Count);
        Assert.Equal("new", result[0]);
        Assert.DoesNotContain("p9", result);
    }

    [Fact]
    public void Remove_is_case_insensitive_and_missing_is_noop()
    {
        Assert.Equal(new[] { "b" }, RecentItems.Remove(new[] { "A", "b" }, "a"));
        Assert.Equal(new[] { "A", "b" }, RecentItems.Remove(new[] { "A", "b" }, "zzz"));
    }
}
