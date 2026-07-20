// SPDX-License-Identifier: GPL-3.0-or-later
using Collective.Platform.Controls;
using Xunit;

namespace Collective.Platform.Controls.Tests;

/// <summary>
/// Drives the toast queue through a fake clock: show/visible, newest-first capping, time-based
/// expiry, same-message de-duplication, dismissal, and Changed notifications.
/// </summary>
public sealed class ToastServiceTests
{
    private DateTime _now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private ToastService NewService(int maxVisible = 4) =>
        new(maxVisible, TimeSpan.FromSeconds(5), () => _now);

    [Fact]
    public void Show_makes_a_toast_visible()
    {
        var svc = NewService();
        svc.Show("Saved", ToastSeverity.Success);

        var visible = svc.Visible();
        var item = Assert.Single(visible);
        Assert.Equal("Saved", item.Message);
        Assert.Equal(ToastSeverity.Success, item.Severity);
    }

    [Fact]
    public void Visible_is_newest_first_and_capped()
    {
        var svc = NewService(maxVisible: 2);
        svc.Show("one");
        svc.Show("two");
        svc.Show("three");

        Assert.Equal(["three", "two"], svc.Visible().Select(t => t.Message));
    }

    [Fact]
    public void Expired_toasts_drop_out()
    {
        var svc = NewService();
        svc.Show("temp", ToastSeverity.Info, TimeSpan.FromSeconds(5));

        _now = _now.AddSeconds(6);
        Assert.Empty(svc.Visible());
    }

    [Fact]
    public void Duplicate_message_refreshes_instead_of_stacking()
    {
        var svc = NewService();
        var first = svc.Show("Copied");
        _now = _now.AddSeconds(2);
        var second = svc.Show("Copied");

        Assert.Same(first, second);                       // same item, refreshed
        Assert.Single(svc.Visible());
        Assert.Equal(_now, second.CreatedAt);             // timer reset
    }

    [Fact]
    public void Dismiss_removes_a_toast()
    {
        var svc = NewService();
        var item = svc.Show("bye");
        svc.Dismiss(item.Id);
        Assert.Empty(svc.Visible());
    }

    [Fact]
    public void Changed_fires_on_show_and_dismiss()
    {
        var svc = NewService();
        int changes = 0;
        svc.Changed += () => changes++;

        var item = svc.Show("hi");
        svc.Dismiss(item.Id);

        Assert.Equal(2, changes);
    }
}
