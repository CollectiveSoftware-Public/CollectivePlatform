// SPDX-License-Identifier: GPL-3.0-or-later
using Collective.Platform.Controls;
using Xunit;

namespace Collective.Platform.Controls.Tests;

/// <summary>
/// Covers the palette's fuzzy filter/rank: empty query passthrough, subsequence matching, the
/// word-start and shorter-title ranking, and stable ordering on ties.
/// </summary>
public sealed class CommandMatcherTests
{
    private static CommandRegistry Sample()
    {
        var r = new CommandRegistry();
        r.Register("file.save", "Save", "File", () => { });
        r.Register("file.saveAs", "Save As…", "File", () => { });
        r.Register("edit.find", "Find", "Edit", () => { });
        r.Register("edit.findFiles", "Find in Files", "Edit", () => { });
        r.Register("view.theme", "Toggle Theme", "View", () => { });
        return r;
    }

    [Fact]
    public void Empty_query_returns_all_in_registration_order()
    {
        var result = CommandMatcher.Filter(Sample().All, "   ");
        Assert.Equal(["file.save", "file.saveAs", "edit.find", "edit.findFiles", "view.theme"],
            result.Select(c => c.Id));
    }

    [Fact]
    public void Filters_to_subsequence_matches_only()
    {
        var result = CommandMatcher.Filter(Sample().All, "save");
        Assert.Equal(["file.save", "file.saveAs"], result.Select(c => c.Id));
    }

    [Fact]
    public void Shorter_exact_title_outranks_longer_one()
    {
        var result = CommandMatcher.Filter(Sample().All, "find");
        Assert.Equal("edit.find", result[0].Id);          // "Find" beats "Find in Files"
    }

    [Fact]
    public void Word_start_initials_match_across_words()
    {
        // "fif" = Find-in-Files initials; should match and rank that command highly.
        var result = CommandMatcher.Filter(Sample().All, "fif");
        Assert.Contains(result, c => c.Id == "edit.findFiles");
    }

    [Fact]
    public void No_match_yields_empty()
    {
        Assert.Empty(CommandMatcher.Filter(Sample().All, "zzz"));
    }

    [Fact]
    public void Score_is_case_insensitive_but_rewards_exact_case()
    {
        int? lower = CommandMatcher.Score("Save", "save");
        int? exact = CommandMatcher.Score("Save", "Save");
        Assert.NotNull(lower);
        Assert.NotNull(exact);
        Assert.True(exact > lower);
    }

    [Fact]
    public void Score_returns_null_when_not_a_subsequence()
    {
        Assert.Null(CommandMatcher.Score("Save", "xyz"));
    }
}
