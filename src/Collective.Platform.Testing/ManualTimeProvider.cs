// SPDX-License-Identifier: GPL-3.0-or-later
namespace Collective.Platform.Testing;

/// <summary>
/// A <see cref="TimeProvider"/> whose clock only moves when the test calls
/// <see cref="Advance"/>. Timers created through it fire synchronously during
/// <see cref="Advance"/> once their due time is reached, so time-based behaviour
/// (idle locks, auto-clear, debounce) is fully deterministic in tests.
/// (Canonical copy of CollectiveVault's proven ManualTimeProvider.)
/// </summary>
public sealed class ManualTimeProvider(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset _now = start;
    private readonly List<FakeTimer> _timers = [];

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by)
    {
        _now += by;
        foreach (FakeTimer timer in _timers.ToArray())
            timer.MaybeFire(_now);
    }

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new FakeTimer(callback, state, this);
        timer.Change(dueTime, period);
        _timers.Add(timer);
        return timer;
    }

    private void Remove(FakeTimer timer) => _timers.Remove(timer);

    private sealed class FakeTimer(TimerCallback callback, object? state, ManualTimeProvider owner) : ITimer
    {
        private DateTimeOffset? _dueAt;
        private TimeSpan _period;

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            _dueAt = dueTime == Timeout.InfiniteTimeSpan ? null : owner.GetUtcNow() + dueTime;
            _period = period;
            return true;
        }

        public void MaybeFire(DateTimeOffset now)
        {
            if (_dueAt is not { } due || now < due)
                return;

            _dueAt = _period <= TimeSpan.Zero || _period == Timeout.InfiniteTimeSpan ? null : now + _period;
            callback(state);
        }

        public void Dispose()
        {
            _dueAt = null;
            owner.Remove(this);
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
