// SPDX-License-Identifier: GPL-3.0-or-later
using Collective.Platform.Abstractions;

namespace Collective.Platform;

/// <summary>
/// A most-recent-first string list (recent folders/repos/files) persisted as its own blob via
/// <see cref="ISettingsStore"/> — the shared version of GIT's FileRecentRepositories and Code's
/// RecentProjects. Case-insensitive dedup, capped at <paramref name="max"/>. <b>Best-effort by
/// contract:</b> an MRU is a convenience, so I/O failures (a concurrent instance holding the blob,
/// a denied write) degrade — Get falls back to empty, Add/Remove return the computed list without
/// persisting — and never throw. A thrown MRU write once rode an async-void startup handler down
/// and took the whole app with it (CollectiveWiki, 2026-07-29).
/// </summary>
public sealed class RecentItemsStore(ISettingsStore settings, string key = "recent", int max = RecentItems.DefaultMax)
{
    public async Task<IReadOnlyList<string>> GetAsync(CancellationToken ct = default)
    {
        try
        {
            return (await settings.LoadAsync<RecentItemsList>(key, ct).ConfigureAwait(false))?.Items ?? new List<string>();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new List<string>();
        }
    }

    public async Task<IReadOnlyList<string>> AddAsync(string item, CancellationToken ct = default)
    {
        var updated = RecentItems.Add(await GetAsync(ct).ConfigureAwait(false), item, max);
        await TrySaveAsync(updated, ct).ConfigureAwait(false);
        return updated;
    }

    public async Task<IReadOnlyList<string>> RemoveAsync(string item, CancellationToken ct = default)
    {
        var updated = RecentItems.Remove(await GetAsync(ct).ConfigureAwait(false), item);
        await TrySaveAsync(updated, ct).ConfigureAwait(false);
        return updated;
    }

    private async Task TrySaveAsync(List<string> items, CancellationToken ct)
    {
        try
        {
            await settings.SaveAsync(key, new RecentItemsList { Items = items }, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Skipped write: the MRU misses one entry rather than the app missing its process.
        }
    }
}
