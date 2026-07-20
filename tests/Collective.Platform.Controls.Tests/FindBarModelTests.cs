// SPDX-License-Identifier: GPL-3.0-or-later
using Collective.Platform.Controls;
using Xunit;

namespace Collective.Platform.Controls.Tests;

/// <summary>
/// Covers the shared find bar's navigation state: match-count reporting + clamping, wrap-around
/// next/previous, the empty/no-results states, and the status label.
/// </summary>
public sealed class FindBarModelTests
{
    [Fact]
    public void SetMatches_clamps_current_into_range()
    {
        var m = new FindBarModel();
        m.SetMatches(total: 3, current: 9);
        Assert.Equal(3, m.CurrentIndex);

        m.SetMatches(total: 3, current: 0);
        Assert.Equal(1, m.CurrentIndex);
    }

    [Fact]
    public void Next_wraps_past_the_end()
    {
        var m = new FindBarModel();
        m.SetMatches(2);
        Assert.Equal(2, m.Next());
        Assert.Equal(1, m.Next());                        // wrapped
    }

    [Fact]
    public void Previous_wraps_past_the_start()
    {
        var m = new FindBarModel();
        m.SetMatches(2, current: 1);
        Assert.Equal(2, m.Previous());                    // wrapped to the end
        Assert.Equal(1, m.Previous());
    }

    [Fact]
    public void No_matches_keeps_index_at_zero()
    {
        var m = new FindBarModel();
        m.SetMatches(0);
        Assert.Equal(0, m.CurrentIndex);
        Assert.Equal(0, m.Next());
        Assert.Equal(0, m.Previous());
    }

    [Fact]
    public void Status_text_reflects_state()
    {
        var m = new FindBarModel();
        Assert.Equal("", m.StatusText);                   // empty query, no matches

        m.Query = "foo";
        m.SetMatches(0);
        Assert.Equal("No results", m.StatusText);

        m.SetMatches(12, current: 3);
        Assert.Equal("3 of 12", m.StatusText);
    }

    [Fact]
    public void Options_are_independent_toggles()
    {
        var m = new FindBarModel { MatchCase = true, UseRegex = true };
        Assert.True(m.MatchCase);
        Assert.False(m.WholeWord);
        Assert.True(m.UseRegex);
    }
}
