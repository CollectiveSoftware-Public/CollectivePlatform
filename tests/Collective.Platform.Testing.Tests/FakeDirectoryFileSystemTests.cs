// SPDX-License-Identifier: GPL-3.0-or-later
using Collective.Platform.Abstractions;
using Collective.Platform.Testing;
using Xunit;

namespace Collective.Platform.Testing.Tests;

public class FakeDirectoryFileSystemTests
{
    [Fact]
    public async Task AddFile_creates_intermediate_directories()
    {
        var fs = new FakeDirectoryFileSystem();
        fs.AddFile("root/src/a.cs", length: 10);
        var top = await fs.ListAsync("");
        Assert.Single(top);
        Assert.True(top[0].IsDirectory);
        Assert.Equal("root", top[0].Name);
        var src = await fs.ListAsync("root/src");
        Assert.Single(src);
        Assert.Equal(("a.cs", false, 10L), (src[0].Name, src[0].IsDirectory, src[0].Length));
    }

    [Fact]
    public async Task Listing_is_name_sorted_case_insensitive()
    {
        var fs = new FakeDirectoryFileSystem();
        fs.AddFile("d/b.txt");
        fs.AddFile("d/A.txt");
        var entries = await fs.ListAsync("d");
        Assert.Equal(new[] { "A.txt", "b.txt" }, entries.Select(e => e.Name));
    }

    [Fact]
    public async Task Missing_directory_lists_empty_and_does_not_exist()
    {
        var fs = new FakeDirectoryFileSystem();
        Assert.Empty(await fs.ListAsync("nope"));
        Assert.False(await fs.DirectoryExistsAsync("nope"));
    }

    [Fact]
    public async Task AddDirectory_creates_an_empty_existing_directory()
    {
        var fs = new FakeDirectoryFileSystem();
        fs.AddDirectory("root/empty");
        Assert.True(await fs.DirectoryExistsAsync("root/empty"));
        Assert.Empty(await fs.ListAsync("root/empty"));
    }
}
