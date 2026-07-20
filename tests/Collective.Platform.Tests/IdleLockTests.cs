// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Threading;
using Collective.Platform;
using Xunit;

namespace Collective.Platform.Tests;

public class IdleLockTests
{
    /// <summary>Hand-rolled deterministic <see cref="TimeProvider"/> — no test package (no new dependencies).</summary>
    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UnixEpoch;
        private TimerCallback? Callback;
        private object? CallbackState;
        public TimeSpan? LastDueTime;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;

        public void FireTimer() => Callback?.Invoke(CallbackState);

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            Callback = callback;
            CallbackState = state;
            LastDueTime = dueTime;
            return new FakeTimer(this);
        }

        private sealed class FakeTimer(ManualTimeProvider owner) : ITimer
        {
            public bool Change(TimeSpan dueTime, TimeSpan period) { owner.LastDueTime = dueTime; return true; }
            public void Dispose() { }
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }

    [Fact]
    public void Fires_after_the_idle_window()
    {
        var time = new ManualTimeProvider();
        using var idle = new IdleLock(time, TimeSpan.FromMinutes(5));
        int fired = 0;
        idle.Locked += (_, _) => fired++;
        idle.Start();

        time.Advance(TimeSpan.FromMinutes(5));
        time.FireTimer();
        Assert.Equal(1, fired);
    }

    [Fact]
    public void Touch_defers_the_fire()
    {
        var time = new ManualTimeProvider();
        using var idle = new IdleLock(time, TimeSpan.FromMinutes(5));
        int fired = 0;
        idle.Locked += (_, _) => fired++;
        idle.Start();

        time.Advance(TimeSpan.FromMinutes(4));
        idle.Touch();
        time.FireTimer(); // elapses at original due time — activity was newer
        Assert.Equal(0, fired);

        time.Advance(TimeSpan.FromMinutes(5));
        time.FireTimer();
        Assert.Equal(1, fired);
    }

    [Fact]
    public void Stop_prevents_firing()
    {
        var time = new ManualTimeProvider();
        using var idle = new IdleLock(time, TimeSpan.FromMinutes(5));
        int fired = 0;
        idle.Locked += (_, _) => fired++;
        idle.Start();
        idle.Stop();
        time.Advance(TimeSpan.FromMinutes(10));
        time.FireTimer();
        Assert.Equal(0, fired);
    }

    [Fact]
    public void Does_not_double_fire()
    {
        var time = new ManualTimeProvider();
        using var idle = new IdleLock(time, TimeSpan.FromMinutes(5));
        int fired = 0;
        idle.Locked += (_, _) => fired++;
        idle.Start();

        time.Advance(TimeSpan.FromMinutes(6));
        time.FireTimer();
        time.FireTimer();
        Assert.Equal(1, fired);
    }
}
