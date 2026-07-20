// SPDX-License-Identifier: GPL-3.0-or-later
namespace Collective.Platform.Controls;

/// <summary>Visual weight of a toast — drives its accent/icon in <c>ToastHost</c>.</summary>
public enum ToastSeverity { Info, Success, Warning, Error }

/// <summary>One transient notification: a message, a severity, and when it was raised / expires.</summary>
public sealed class ToastItem
{
    public long Id { get; }
    public string Message { get; }
    public ToastSeverity Severity { get; }
    public TimeSpan Duration { get; }

    /// <summary>When the toast was (re)raised; reset on a duplicate to refresh its timer.</summary>
    public DateTime CreatedAt { get; internal set; }

    internal ToastItem(long id, string message, ToastSeverity severity, TimeSpan duration, DateTime createdAt)
    {
        Id = id;
        Message = message;
        Severity = severity;
        Duration = duration;
        CreatedAt = createdAt;
    }

    public DateTime ExpiresAt => CreatedAt + Duration;
}

/// <summary>
/// The transient-feedback queue shared by every desktop head — the mechanic no head had before
/// (saves, copies, and errors used to happen silently or hijack a modal box). Pure and clock-injected
/// so its queueing, expiry, capping, and de-duplication are deterministically unit-testable; the
/// Avalonia <c>ToastHost</c> overlay subscribes to <see cref="Changed"/> and renders <see cref="Visible()"/>.
/// </summary>
public sealed class ToastService
{
    private readonly Func<DateTime> _clock;
    private readonly List<ToastItem> _items = new();
    private long _nextId;

    /// <summary>Most toasts shown at once; older ones beyond this are hidden (still in the list).</summary>
    public int MaxVisible { get; }

    /// <summary>Default lifetime applied when <see cref="Show"/> is called without one.</summary>
    public TimeSpan DefaultDuration { get; }

    /// <summary>Raised whenever the active set changes (a show, dismiss, or explicit prune).</summary>
    public event Action? Changed;

    public ToastService(int maxVisible = 4, TimeSpan? defaultDuration = null, Func<DateTime>? clock = null)
    {
        if (maxVisible < 1) throw new ArgumentOutOfRangeException(nameof(maxVisible));
        MaxVisible = maxVisible;
        DefaultDuration = defaultDuration ?? TimeSpan.FromSeconds(4);
        _clock = clock ?? (() => DateTime.UtcNow);
    }

    /// <summary>
    /// Shows a toast. A still-active toast with the same message and severity is refreshed (its timer
    /// resets and it moves to the front) instead of being duplicated.
    /// </summary>
    public ToastItem Show(string message, ToastSeverity severity = ToastSeverity.Info, TimeSpan? duration = null)
    {
        ArgumentNullException.ThrowIfNull(message);
        DateTime now = _clock();
        RemoveExpired(now);

        var existing = _items.FirstOrDefault(t => t.Message == message && t.Severity == severity);
        if (existing is not null)
        {
            existing.CreatedAt = now;                       // refresh timer
            _items.Remove(existing);
            _items.Add(existing);                           // move to front (newest)
            Changed?.Invoke();
            return existing;
        }

        var item = new ToastItem(++_nextId, message, severity, duration ?? DefaultDuration, now);
        _items.Add(item);
        Changed?.Invoke();
        return item;
    }

    /// <summary>Removes the toast with <paramref name="id"/> if present.</summary>
    public void Dismiss(long id)
    {
        if (_items.RemoveAll(t => t.Id == id) > 0) Changed?.Invoke();
    }

    /// <summary>Drops expired toasts as of <paramref name="now"/>, raising <see cref="Changed"/> if any went.</summary>
    public void Prune(DateTime now)
    {
        if (RemoveExpired(now)) Changed?.Invoke();
    }

    /// <summary>The toasts to display as of <paramref name="now"/>: newest first, capped to <see cref="MaxVisible"/>.</summary>
    public IReadOnlyList<ToastItem> Visible(DateTime now)
    {
        RemoveExpired(now);                                 // silent — callers poll this, no re-entrant event
        var result = new List<ToastItem>(Math.Min(MaxVisible, _items.Count));
        for (int i = _items.Count - 1; i >= 0 && result.Count < MaxVisible; i--)
            result.Add(_items[i]);
        return result;
    }

    /// <summary>The visible toasts as of the current clock.</summary>
    public IReadOnlyList<ToastItem> Visible() => Visible(_clock());

    private bool RemoveExpired(DateTime now) => _items.RemoveAll(t => now >= t.ExpiresAt) > 0;
}
