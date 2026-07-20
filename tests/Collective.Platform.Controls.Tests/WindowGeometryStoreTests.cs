// SPDX-License-Identifier: GPL-3.0-or-later
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Collective.Platform.Abstractions;
using Collective.Platform.Controls;
using Xunit;

namespace Collective.Platform.Controls.Tests;

public class WindowGeometryStoreTests
{
    private sealed class FakeSettingsStore : ISettingsStore
    {
        private readonly Dictionary<string, object> _data = new();

        public Task<T?> LoadAsync<T>(string name, CancellationToken ct = default) where T : class
            => Task.FromResult(_data.TryGetValue(name, out var v) ? (T?)v : null);

        public Task SaveAsync<T>(string name, T value, CancellationToken ct = default) where T : class
        {
            _data[name] = value;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Save_then_load_round_trips_geometry()
    {
        var store = new WindowGeometryStore(new FakeSettingsStore());
        var g = new WindowGeometry { Width = 1024, Height = 768, X = 10, Y = 20, Maximized = true };
        await store.SaveAsync(g);
        Assert.Equal(g, await store.LoadAsync());
    }

    [Fact]
    public async Task Load_returns_null_when_absent()
        => Assert.Null(await new WindowGeometryStore(new FakeSettingsStore()).LoadAsync());

    [Fact]
    public async Task Uses_the_window_key_by_default()
    {
        var fake = new FakeSettingsStore();
        await new WindowGeometryStore(fake).SaveAsync(new WindowGeometry { Width = 1, Height = 1 });
        Assert.NotNull(await new WindowGeometryStore(fake, "window").LoadAsync());
        Assert.Null(await new WindowGeometryStore(fake, "other").LoadAsync());
    }
}
