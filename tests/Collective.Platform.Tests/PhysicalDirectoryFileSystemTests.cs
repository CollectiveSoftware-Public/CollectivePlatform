// SPDX-License-Identifier: GPL-3.0-or-later
using Collective.Platform.Testing;
using Xunit;

namespace Collective.Platform.Tests;

public class PhysicalDirectoryFileSystemTests
{
    [Fact]
    public async Task Lists_files_and_directories_name_sorted()
    {
        using var dir = new TempDirectory();
        File.WriteAllText(dir.File("b.txt"), "b");
        File.WriteAllText(dir.File("sub/a.txt"), "a");
        var fs = new PhysicalDirectoryFileSystem();

        var entries = await fs.ListAsync(dir.Path);

        Assert.Equal(new[] { "b.txt", "sub" }, entries.Select(e => e.Name));
        Assert.False(entries[0].IsDirectory);
        Assert.Equal(1, entries[0].Length);
        Assert.True(entries[1].IsDirectory);
        Assert.Equal(0, entries[1].Length);
    }

    [Fact]
    public async Task Missing_directory_lists_empty_and_does_not_exist()
    {
        var fs = new PhysicalDirectoryFileSystem();
        string missing = Path.Combine(Path.GetTempPath(), "collective-missing-" + Guid.NewGuid().ToString("N"));
        Assert.Empty(await fs.ListAsync(missing));
        Assert.False(await fs.DirectoryExistsAsync(missing));
    }

    [Fact]
    public async Task Existing_directory_exists()
    {
        using var dir = new TempDirectory();
        Assert.True(await new PhysicalDirectoryFileSystem().DirectoryExistsAsync(dir.Path));
    }
}
