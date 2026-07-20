// SPDX-License-Identifier: GPL-3.0-or-later
namespace Collective.Platform;

/// <summary>
/// Fires <see cref="Locked"/> after an idle window with no <see cref="Touch"/> — shared
/// auto-lock core so CollectiveVault's VaultSession and Auth/SSH's app-lock build on the same
/// tested core. TimeProvider-driven (deterministic under test). Not thread-affine: Locked is
/// raised on the timer thread; UI consumers must marshal to their dispatcher.
/// </summary>
public sealed class IdleLock(TimeProvider time, TimeSpan after) : IDisposable
{
    private readonly object _gate = new();
    private DateTimeOffset _lastActivity;
    private ITimer? _timer;
    private bool _armed;

    public event EventHandler? Locked;

    public void Start()
    {
        lock (_gate)
        {
            _lastActivity = time.GetUtcNow();
            _armed = true;
            _timer ??= time.CreateTimer(_ => OnTimer(), null, after, Timeout.InfiniteTimeSpan);
            _timer.Change(after, Timeout.InfiniteTimeSpan);
        }
    }

    public void Touch()
    {
        lock (_gate)
            _lastActivity = time.GetUtcNow();
    }

    public void Stop()
    {
        lock (_gate)
        {
            _armed = false;
            _timer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }
    }

    private void OnTimer()
    {
        bool fire = false;
        lock (_gate)
        {
            if (!_armed)
                return;
            var idle = time.GetUtcNow() - _lastActivity;
            if (idle >= after)
            {
                _armed = false;
                fire = true;
            }
            else
            {
                _timer?.Change(after - idle, Timeout.InfiniteTimeSpan); // activity arrived — re-check later
            }
        }
        if (fire)
            Locked?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _armed = false;
            _timer?.Dispose();
            _timer = null;
        }
    }
}
