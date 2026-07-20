// SPDX-License-Identifier: GPL-3.0-or-later
using Collective.Platform.Controls;
using Xunit;

namespace Collective.Platform.Controls.Tests;

public class ExplorerHistoryTests
{
    [Fact]
    public void New_history_has_no_current_and_cannot_move()
    {
        var h = new ExplorerHistory();
        Assert.Null(h.Current);
        Assert.False(h.CanBack);
        Assert.False(h.CanForward);
    }

    [Fact]
    public void Navigate_sets_current_and_enables_back_after_second()
    {
        var h = new ExplorerHistory();
        h.Navigate(@"C:\a");
        Assert.Equal(@"C:\a", h.Current);
        Assert.False(h.CanBack);

        h.Navigate(@"C:\a\b");
        Assert.Equal(@"C:\a\b", h.Current);
        Assert.True(h.CanBack);
    }

    [Fact]
    public void Back_and_forward_walk_the_stack()
    {
        var h = new ExplorerHistory();
        h.Navigate(@"C:\a");
        h.Navigate(@"C:\a\b");
        h.Navigate(@"C:\a\b\c");

        Assert.Equal(@"C:\a\b", h.Back());
        Assert.Equal(@"C:\a", h.Back());
        Assert.True(h.CanForward);
        Assert.Equal(@"C:\a\b", h.Forward());
        Assert.Equal(@"C:\a\b\c", h.Forward());
        Assert.False(h.CanForward);
    }

    [Fact]
    public void Navigate_clears_forward_stack()
    {
        var h = new ExplorerHistory();
        h.Navigate(@"C:\a");
        h.Navigate(@"C:\a\b");
        h.Back();
        Assert.True(h.CanForward);

        h.Navigate(@"C:\a\z");
        Assert.False(h.CanForward);
        Assert.Equal(@"C:\a\z", h.Current);
    }

    [Fact]
    public void Renavigating_current_is_a_noop()
    {
        var h = new ExplorerHistory();
        h.Navigate(@"C:\a");
        h.Navigate(@"c:\A"); // same path, different case
        Assert.False(h.CanBack);
    }
}
