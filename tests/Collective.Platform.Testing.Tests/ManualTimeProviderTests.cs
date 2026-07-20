// SPDX-License-Identifier: GPL-3.0-or-later
using Collective.Platform.Testing;
using Xunit;

namespace Collective.Platform.Testing.Tests;

public class ManualTimeProviderTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Advance_moves_the_clock()
    {
        var time = new ManualTimeProvider(T0);
        time.Advance(TimeSpan.FromMinutes(5));
        Assert.Equal(T0 + TimeSpan.FromMinutes(5), time.GetUtcNow());
    }

    [Fact]
    public void Timer_fires_synchronously_when_due_time_is_reached()
    {
        var time = new ManualTimeProvider(T0);
        int fired = 0;
        using var _ = time.CreateTimer(_ => fired++, null, TimeSpan.FromSeconds(10), Timeout.InfiniteTimeSpan);
        time.Advance(TimeSpan.FromSeconds(9));
        Assert.Equal(0, fired);
        time.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(1, fired);
        time.Advance(TimeSpan.FromMinutes(1)); // one-shot: never refires
        Assert.Equal(1, fired);
    }

    [Fact]
    public void Periodic_timer_refires_each_period()
    {
        var time = new ManualTimeProvider(T0);
        int fired = 0;
        using var _ = time.CreateTimer(_ => fired++, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        time.Advance(TimeSpan.FromSeconds(1));
        time.Advance(TimeSpan.FromSeconds(1));
        time.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(3, fired);
    }

    [Fact]
    public void Disposed_timer_stops_firing()
    {
        var time = new ManualTimeProvider(T0);
        int fired = 0;
        var timer = time.CreateTimer(_ => fired++, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        time.Advance(TimeSpan.FromSeconds(1));
        timer.Dispose();
        time.Advance(TimeSpan.FromSeconds(5));
        Assert.Equal(1, fired);
    }

    [Fact]
    public void Infinite_due_time_never_fires()
    {
        var time = new ManualTimeProvider(T0);
        int fired = 0;
        using var _ = time.CreateTimer(_ => fired++, null, Timeout.InfiniteTimeSpan, TimeSpan.FromSeconds(1));
        time.Advance(TimeSpan.FromHours(1));
        Assert.Equal(0, fired);
    }
}
