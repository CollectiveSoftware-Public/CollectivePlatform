// SPDX-License-Identifier: GPL-3.0-or-later
using System.Collections.Concurrent;
using System.Text.Json;
using Collective.Platform.Abstractions;

namespace Collective.Platform.Testing;

/// <summary>
/// In-memory <see cref="ISettingsStore"/> for deterministic tests. Values are JSON round-tripped
/// on save/load — matching <c>FileSettingsStore</c>'s persistence semantics — so mutating a saved
/// object doesn't leak into the store, non-serializable settings fail in the test, and corrupt
/// JSON loads as null like the real store.
/// </summary>
public sealed class FakeSettingsStore : ISettingsStore
{
    private readonly ConcurrentDictionary<string, string> _blobs = new(StringComparer.Ordinal);

    public Task<T?> LoadAsync<T>(string name, CancellationToken cancellationToken = default) where T : class
    {
        if (!_blobs.TryGetValue(name, out var json))
            return Task.FromResult<T?>(null);
        try { return Task.FromResult(JsonSerializer.Deserialize<T>(json)); }
        catch (JsonException) { return Task.FromResult<T?>(null); }
    }

    public Task SaveAsync<T>(string name, T value, CancellationToken cancellationToken = default) where T : class
    {
        _blobs[name] = JsonSerializer.Serialize(value);
        return Task.CompletedTask;
    }

    /// <summary>Seed or corrupt the raw stored JSON (for testing load paths).</summary>
    public void SetRaw(string name, string json) => _blobs[name] = json;
}
