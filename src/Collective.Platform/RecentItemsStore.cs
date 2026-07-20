// SPDX-License-Identifier: GPL-3.0-or-later
using Collective.Platform.Abstractions;

namespace Collective.Platform;

/// <summary>
/// A most-recent-first string list (recent folders/repos/files) persisted as its own blob via
/// <see cref="ISettingsStore"/> — the shared version of GIT's FileRecentRepositories and Code's
/// RecentProjects. Case-insensitive dedup, capped at <paramref name="max"/>.
/// </summary>
public sealed class RecentItemsStore(ISettingsStore settings, string key = "recent", int max = RecentItems.DefaultMax)
{
    public async Task<IReadOnlyList<string>> GetAsync(CancellationToken ct = default)
        => (await settings.LoadAsync<RecentItemsList>(key, ct).ConfigureAwait(false))?.Items ?? new List<string>();

    public async Task<IReadOnlyList<string>> AddAsync(string item, CancellationToken ct = default)
    {
        var updated = RecentItems.Add(await GetAsync(ct).ConfigureAwait(false), item, max);
        await settings.SaveAsync(key, new RecentItemsList { Items = updated }, ct).ConfigureAwait(false);
        return updated;
    }

    public async Task<IReadOnlyList<string>> RemoveAsync(string item, CancellationToken ct = default)
    {
        var updated = RecentItems.Remove(await GetAsync(ct).ConfigureAwait(false), item);
        await settings.SaveAsync(key, new RecentItemsList { Items = updated }, ct).ConfigureAwait(false);
        return updated;
    }
}
