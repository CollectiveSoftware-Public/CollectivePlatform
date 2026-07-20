// SPDX-License-Identifier: GPL-3.0-or-later
using Collective.Platform;
using Collective.Platform.Abstractions;
using Collective.Platform.Controls;
using Collective.Platform.Testing;
using Xunit;

namespace Collective.Platform.Controls.Tests;

public class FolderListingTests
{
    private static async Task<IReadOnlyList<DirectoryEntry>> SampleAsync()
    {
        var fs = new FakeDirectoryFileSystem();
        fs.AddDirectory("zeta");
        fs.AddDirectory("alpha");
        fs.AddFile("readme.md", length: 100);
        fs.AddFile("build.cs", length: 30);
        fs.AddFile(".hidden", length: 5);
        return await fs.ListAsync("");
    }

    [Fact]
    public async Task Folders_sort_before_files()
    {
        var rows = new FolderListing().Apply(await SampleAsync());

        Assert.True(rows[0].Entry.IsDirectory);
        Assert.True(rows[1].Entry.IsDirectory);
        Assert.False(rows[2].Entry.IsDirectory);
        // hidden excluded by default → alpha, zeta, build.cs, readme.md
        Assert.Equal(new[] { "alpha", "zeta", "build.cs", "readme.md" }, rows.Select(r => r.Entry.Name));
    }

    [Fact]
    public async Task Name_descending_flips_within_groups()
    {
        var listing = new FolderListing { Descending = true };
        var rows = listing.Apply(await SampleAsync());

        Assert.Equal(new[] { "zeta", "alpha", "readme.md", "build.cs" }, rows.Select(r => r.Entry.Name));
    }

    [Fact]
    public async Task Size_sort_orders_files_by_length()
    {
        var listing = new FolderListing { Sort = ListingSort.Size };
        var rows = listing.Apply(await SampleAsync());

        var files = rows.Where(r => !r.Entry.IsDirectory).Select(r => r.Entry.Name).ToArray();
        Assert.Equal(new[] { "build.cs", "readme.md" }, files); // 30 before 100
    }

    [Fact]
    public async Task Show_hidden_keeps_dot_entries_flagged()
    {
        var listing = new FolderListing { ShowHidden = true };
        var rows = listing.Apply(await SampleAsync());

        var hidden = rows.Single(r => r.Entry.Name == ".hidden");
        Assert.True(hidden.IsHidden);
        Assert.All(rows.Where(r => r.Entry.Name != ".hidden"), r => Assert.False(r.IsHidden));
    }

    [Fact]
    public async Task Hidden_excluded_by_default()
    {
        var rows = new FolderListing().Apply(await SampleAsync());
        Assert.DoesNotContain(rows, r => r.Entry.Name == ".hidden");
    }

    [Fact]
    public async Task Filter_keeps_substring_matches()
    {
        var listing = new FolderListing { Filter = "rea" };
        var rows = listing.Apply(await SampleAsync());

        Assert.Single(rows);
        Assert.Equal("readme.md", rows[0].Entry.Name);
    }

    [Fact]
    public async Task SkipFilter_hides_build_and_vcs_dirs_but_keeps_dot_files()
    {
        var fs = new FakeDirectoryFileSystem();
        fs.AddDirectory("src");
        fs.AddDirectory("bin");
        fs.AddDirectory(".git");
        fs.AddFile(".gitignore", length: 10);
        fs.AddFile("Program.cs", length: 50);
        var entries = await fs.ListAsync("");

        // A code-editor sidebar: hide bin/.git, keep .gitignore (greyed via ShowHidden).
        var listing = new FolderListing
        {
            SkipFilter = new FileTreeFilter { SkipDotFiles = false },
            ShowHidden = true,
        };
        var names = listing.Apply(entries).Select(r => r.Entry.Name).ToArray();

        Assert.Contains("src", names);
        Assert.Contains(".gitignore", names);
        Assert.Contains("Program.cs", names);
        Assert.DoesNotContain("bin", names);   // SkipNames
        Assert.DoesNotContain(".git", names);  // dot-directory
    }

    [Fact]
    public async Task DirectoriesOnly_drops_files()
    {
        var listing = new FolderListing { DirectoriesOnly = true };
        var rows = listing.Apply(await SampleAsync());

        Assert.All(rows, r => Assert.True(r.Entry.IsDirectory));
        Assert.Equal(new[] { "alpha", "zeta" }, rows.Select(r => r.Entry.Name));
    }

    [Fact]
    public void ToggleSort_flips_direction_when_already_the_key()
    {
        var listing = new FolderListing();
        Assert.Equal(ListingSort.Name, listing.Sort);

        listing.ToggleSort(ListingSort.Size);
        Assert.Equal(ListingSort.Size, listing.Sort);
        Assert.False(listing.Descending);

        listing.ToggleSort(ListingSort.Size);
        Assert.True(listing.Descending);

        listing.ToggleSort(ListingSort.Name);
        Assert.Equal(ListingSort.Name, listing.Sort);
        Assert.False(listing.Descending);
    }
}
