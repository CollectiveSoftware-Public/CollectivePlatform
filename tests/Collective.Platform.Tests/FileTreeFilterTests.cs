// SPDX-License-Identifier: GPL-3.0-or-later
using Collective.Platform.Abstractions;
using Xunit;

namespace Collective.Platform.Tests;

public class FileTreeFilterTests
{
    private static DirectoryEntry Dir(string name) => new(name, name, true, 0, DateTimeOffset.UnixEpoch);
    private static DirectoryEntry File(string name) => new(name, name, false, 1, DateTimeOffset.UnixEpoch);

    [Fact]
    public void Default_skips_build_and_vcs_directories_case_insensitively()
    {
        var f = FileTreeFilter.Default;
        Assert.False(f.Shows(Dir("bin")));
        Assert.False(f.Shows(Dir("OBJ")));
        Assert.False(f.Shows(Dir("node_modules")));
        Assert.True(f.Shows(Dir("src")));
    }

    [Fact]
    public void Default_skips_dot_entries_both_kinds_but_skipnames_only_hits_directories()
    {
        var f = FileTreeFilter.Default;
        Assert.False(f.Shows(Dir(".git")));
        Assert.False(f.Shows(File(".editorconfig")));
        Assert.True(f.Shows(File("bin"))); // a FILE named bin is shown
    }

    [Fact]
    public void None_shows_everything()
    {
        Assert.True(FileTreeFilter.None.Shows(Dir(".git")));
        Assert.True(FileTreeFilter.None.Shows(File(".hidden")));
    }

    [Fact]
    public void SkipDotFiles_false_shows_dot_files_but_still_hides_dot_directories_and_build_dirs()
    {
        var f = new FileTreeFilter { SkipDotFiles = false };
        Assert.True(f.Shows(File(".gitignore"))); // a code tree wants dot-files visible
        Assert.False(f.Shows(Dir(".git")));       // dot-directories still hidden
        Assert.False(f.Shows(Dir("bin")));        // named build dirs still hidden
    }
}
