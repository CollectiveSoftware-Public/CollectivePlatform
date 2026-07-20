// SPDX-License-Identifier: GPL-3.0-or-later
using Collective.Platform.Controls;
using Xunit;

namespace Collective.Platform.Controls.Tests;

public class TabListTests
{
    private sealed class Doc(string? key)
    {
        public string? Key { get; } = key;
    }

    private static TabList<Doc> NewList() => new(d => d.Key);

    [Fact]
    public void Open_appends_and_activates()
    {
        var list = NewList();
        var a = list.OpenOrActivate("a", () => new Doc("a"));
        Assert.Single(list.Tabs);
        Assert.Same(a, list.Active);
    }

    [Fact]
    public void Open_existing_key_focuses_without_duplicating()
    {
        var list = NewList();
        var a = list.OpenOrActivate("a", () => new Doc("a"));
        list.OpenOrActivate("b", () => new Doc("b"));
        var again = list.OpenOrActivate("a", () => new Doc("a"));
        Assert.Same(a, again);
        Assert.Equal(2, list.Tabs.Count);
        Assert.Same(a, list.Active);
    }

    [Fact]
    public void Null_keys_always_append()
    {
        var list = NewList();
        list.OpenOrActivate(null, () => new Doc(null));
        list.OpenOrActivate(null, () => new Doc(null));
        Assert.Equal(2, list.Tabs.Count);
    }

    [Fact]
    public void Open_without_activate_keeps_the_current_active()
    {
        var list = NewList();
        var a = list.OpenOrActivate("a", () => new Doc("a"));
        list.OpenOrActivate("b", () => new Doc("b"), activate: false);
        Assert.Same(a, list.Active);
    }

    [Fact]
    public void NavigateActive_replaces_in_place_at_the_same_index()
    {
        var list = NewList();
        list.OpenOrActivate("a", () => new Doc("a"));
        var b = list.OpenOrActivate("b", () => new Doc("b"));
        list.OpenOrActivate("c", () => new Doc("c"), activate: false);
        list.Active = b;

        var d = list.NavigateActive("d", () => new Doc("d"));
        Assert.Equal(3, list.Tabs.Count);
        Assert.Same(d, list.Tabs[1]);            // b's slot
        Assert.Same(d, list.Active);
        Assert.DoesNotContain(b, list.Tabs);
    }

    [Fact]
    public void NavigateActive_to_an_already_open_key_focuses_it_instead()
    {
        var list = NewList();
        var a = list.OpenOrActivate("a", () => new Doc("a"));
        var b = list.OpenOrActivate("b", () => new Doc("b"));
        list.Active = b;

        var result = list.NavigateActive("a", () => new Doc("a"));
        Assert.Same(a, result);
        Assert.Same(a, list.Active);
        Assert.Equal(2, list.Tabs.Count);        // b untouched
    }

    [Fact]
    public void NavigateActive_with_no_tabs_appends()
    {
        var list = NewList();
        var a = list.NavigateActive("a", () => new Doc("a"));
        Assert.Single(list.Tabs);
        Assert.Same(a, list.Active);
    }

    [Fact]
    public void Close_active_selects_the_next_tab_else_the_previous()
    {
        var list = NewList();
        var a = list.OpenOrActivate("a", () => new Doc("a"));
        var b = list.OpenOrActivate("b", () => new Doc("b"));
        var c = list.OpenOrActivate("c", () => new Doc("c"));

        list.Active = b;
        list.Close(b);
        Assert.Same(c, list.Active);             // next wins

        list.Close(c);
        Assert.Same(a, list.Active);             // no next — previous

        list.Close(a);
        Assert.Null(list.Active);
    }

    [Fact]
    public void Close_inactive_keeps_the_active()
    {
        var list = NewList();
        var a = list.OpenOrActivate("a", () => new Doc("a"));
        var b = list.OpenOrActivate("b", () => new Doc("b"));
        list.Active = b;
        list.Close(a);
        Assert.Same(b, list.Active);
    }

    [Fact]
    public async Task Veto_cancels_the_close()
    {
        var list = NewList();
        var a = list.OpenOrActivate("a", () => new Doc("a"));
        list.ClosingAsync = _ => Task.FromResult(false);
        Assert.False(await list.CloseAsync(a));
        Assert.Contains(a, list.Tabs);
    }

    [Fact]
    public async Task Closed_hook_fires_once_per_close()
    {
        var list = NewList();
        var closed = new List<Doc>();
        list.Closed = closed.Add;
        var a = list.OpenOrActivate("a", () => new Doc("a"));
        Assert.True(await list.CloseAsync(a));
        list.Close(a);                            // already gone — no second fire
        Assert.Single(closed);
    }

    [Fact]
    public void Setting_Active_raises_ActiveChanged()
    {
        var list = NewList();
        var a = list.OpenOrActivate("a", () => new Doc("a"));
        var b = list.OpenOrActivate("b", () => new Doc("b"));
        int raised = 0;
        list.ActiveChanged += (_, _) => raised++;
        list.Active = a;
        list.Active = a;                          // no-op — same value
        Assert.Equal(1, raised);
    }

    // ---- scoped bulk closes (the tab right-click menu) ----

    private static (TabList<Doc>, Doc, Doc, Doc, Doc) FourTabs()
    {
        var list = NewList();
        var a = list.OpenOrActivate("a", () => new Doc("a"));
        var b = list.OpenOrActivate("b", () => new Doc("b"));
        var c = list.OpenOrActivate("c", () => new Doc("c"));
        var d = list.OpenOrActivate("d", () => new Doc("d"));
        return (list, a, b, c, d);
    }

    [Fact]
    public async Task CloseOthers_keeps_only_the_anchor()
    {
        var (list, _, b, _, _) = FourTabs();
        await list.CloseOthersAsync(b);
        Assert.Single(list.Tabs);
        Assert.Same(b, list.Tabs[0]);
    }

    [Fact]
    public async Task CloseToLeft_closes_only_earlier_tabs()
    {
        var (list, _, _, c, d) = FourTabs();
        await list.CloseToLeftAsync(c);
        Assert.Equal(new[] { c, d }, list.Tabs);
    }

    [Fact]
    public async Task CloseToRight_closes_only_later_tabs()
    {
        var (list, a, b, _, _) = FourTabs();
        await list.CloseToRightAsync(b);
        Assert.Equal(new[] { a, b }, list.Tabs);
    }

    [Fact]
    public async Task CloseAll_empties_the_list()
    {
        var (list, _, _, _, _) = FourTabs();
        await list.CloseAllAsync();
        Assert.Empty(list.Tabs);
        Assert.Null(list.Active);
    }

    [Fact]
    public async Task Scoped_close_stops_when_a_save_is_vetoed()
    {
        var (list, _, b, _, _) = FourTabs();
        list.ClosingAsync = _ => Task.FromResult(false);   // user cancels every save prompt
        await list.CloseOthersAsync(b);
        Assert.Equal(4, list.Tabs.Count);                  // veto aborted the whole batch
    }
}
