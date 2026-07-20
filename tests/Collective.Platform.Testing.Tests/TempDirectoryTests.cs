// SPDX-License-Identifier: GPL-3.0-or-later
using Collective.Platform.Testing;
using Xunit;

namespace Collective.Platform.Testing.Tests;

public class TempDirectoryTests
{
    [Fact]
    public void Creates_a_unique_existing_directory()
    {
        using var a = new TempDirectory();
        using var b = new TempDirectory();
        Assert.True(Directory.Exists(a.Path));
        Assert.NotEqual(a.Path, b.Path);
    }

    [Fact]
    public void File_returns_a_nested_path_with_parents_created()
    {
        using var dir = new TempDirectory();
        string path = dir.File("sub/deeper/x.txt");
        System.IO.File.WriteAllText(path, "hi");
        Assert.True(System.IO.File.Exists(path));
        Assert.StartsWith(dir.Path, path);
    }

    [Fact]
    public void Dispose_deletes_recursively()
    {
        var dir = new TempDirectory();
        System.IO.File.WriteAllText(dir.File("sub/x.txt"), "hi");
        dir.Dispose();
        Assert.False(Directory.Exists(dir.Path));
    }
}
