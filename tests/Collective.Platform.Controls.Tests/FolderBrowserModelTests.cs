// SPDX-License-Identifier: GPL-3.0-or-later
using Collective.Platform.Controls;
using Xunit;

namespace Collective.Platform.Controls.Tests;

/// <summary>
/// Exercises the pure navigation logic behind the shared <see cref="FolderBrowserDialog"/> against a
/// real temp directory tree — the part of the folder chooser that replaces Avalonia's crash-prone
/// native picker. No Avalonia application is started.
/// </summary>
public sealed class FolderBrowserModelTests : IDisposable
{
    private readonly string _root;

    public FolderBrowserModelTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "cpc-folderbrowser-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_root, "alpha"));
        Directory.CreateDirectory(Path.Combine(_root, "beta"));
        Directory.CreateDirectory(Path.Combine(_root, "beta", "nested"));
        File.WriteAllText(Path.Combine(_root, "afile.txt"), "x"); // files must not appear as entries
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }

    [Fact]
    public void Lists_only_subdirectories_sorted_by_name()
    {
        var model = new FolderBrowserModel(_root);

        var names = model.ListEntries().Select(e => e.Name).ToArray();

        Assert.Equal(["alpha", "beta"], names);
        Assert.All(model.ListEntries(), e => Assert.True(Directory.Exists(e.FullPath)));
    }

    [Fact]
    public void Navigating_into_then_up_returns_to_parent()
    {
        var model = new FolderBrowserModel(_root);
        string beta = Path.Combine(_root, "beta");

        Assert.True(model.TryNavigate(beta));
        Assert.Equal(beta, model.CurrentPath);
        Assert.Equal(["nested"], model.ListEntries().Select(e => e.Name));

        model.GoUp();
        Assert.Equal(Path.GetFullPath(_root), model.CurrentPath);
    }

    [Fact]
    public void Navigating_to_a_missing_path_is_rejected_and_keeps_current()
    {
        var model = new FolderBrowserModel(_root);
        string before = model.CurrentPath!;

        Assert.False(model.TryNavigate(Path.Combine(_root, "does-not-exist")));
        Assert.Equal(before, model.CurrentPath);
    }

    [Fact]
    public void Roots_view_lists_drives_and_cannot_go_up()
    {
        var model = new FolderBrowserModel(_root);

        Assert.True(model.TryNavigate(null)); // computer / roots view
        Assert.Null(model.CurrentPath);
        Assert.False(model.CanGoUp);
        Assert.NotEmpty(model.ListEntries()); // at least one logical drive / root
    }

    [Fact]
    public void GoUp_from_a_drive_root_drops_to_the_roots_view()
    {
        var model = new FolderBrowserModel(_root);
        // Walk up to the very top of this path's drive, then once more.
        while (model.CanGoUp && model.CurrentPath is not null)
        {
            string? prev = model.CurrentPath;
            model.GoUp();
            if (model.CurrentPath == prev) break; // safety
        }
        Assert.Null(model.CurrentPath); // reached the roots view
        Assert.False(model.CanGoUp);
    }

    [Fact]
    public void A_missing_start_path_falls_back_without_throwing()
    {
        var model = new FolderBrowserModel(Path.Combine(_root, "nope"));
        // Falls back to the user profile (or roots) — never throws, and is in a usable state.
        Assert.True(model.CurrentPath is null || Directory.Exists(model.CurrentPath));
    }
}
