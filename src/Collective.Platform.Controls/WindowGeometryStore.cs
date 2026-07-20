// SPDX-License-Identifier: GPL-3.0-or-later
using System.Threading;
using System.Threading.Tasks;
using Collective.Platform.Abstractions;

namespace Collective.Platform.Controls;

/// <summary>
/// Persists a <see cref="WindowGeometry"/> as its own JSON blob (default key <c>"window"</c>) via an
/// <see cref="ISettingsStore"/> — decoupled from each head's own settings record, so migrating a head
/// never risks clobbering its other settings.
/// </summary>
public sealed class WindowGeometryStore
{
    private readonly ISettingsStore _store;
    private readonly string _key;

    public WindowGeometryStore(ISettingsStore store, string key = "window")
    {
        _store = store;
        _key = key;
    }

    public Task<WindowGeometry?> LoadAsync(CancellationToken cancellationToken = default)
        => _store.LoadAsync<WindowGeometry>(_key, cancellationToken);

    public Task SaveAsync(WindowGeometry geometry, CancellationToken cancellationToken = default)
        => _store.SaveAsync(_key, geometry, cancellationToken);
}
