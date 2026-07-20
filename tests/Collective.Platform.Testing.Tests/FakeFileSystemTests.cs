// SPDX-License-Identifier: GPL-3.0-or-later
using Collective.Platform.Testing;
using Xunit;

namespace Collective.Platform.Testing.Tests;

public class FakeFileSystemTests
{
    [Fact]
    public async Task Write_then_read_round_trips()
    {
        var fs = new FakeFileSystem();
        await fs.WriteAllTextAsync("a/b.txt", "hello");
        Assert.True(fs.FileExists("a/b.txt"));
        Assert.Equal("hello", await fs.ReadAllTextAsync("a/b.txt"));
    }

    [Fact]
    public async Task Missing_file_throws_FileNotFound()
    {
        var fs = new FakeFileSystem();
        await Assert.ThrowsAsync<FileNotFoundException>(() => fs.ReadAllTextAsync("nope"));
    }

    [Fact]
    public async Task Set_seeds_and_Files_exposes_contents()
    {
        var fs = new FakeFileSystem();
        fs.Set("seed.json", "{}");
        Assert.Single(fs.Files);
        Assert.Equal("{}", await fs.ReadAllTextAsync("seed.json"));
    }

    [Fact]
    public void AppDataDirectory_is_configurable_and_defaults()
    {
        Assert.Equal("/fake/appdata", new FakeFileSystem().AppDataDirectory);
        Assert.Equal("", new FakeFileSystem("").AppDataDirectory);
    }
}
