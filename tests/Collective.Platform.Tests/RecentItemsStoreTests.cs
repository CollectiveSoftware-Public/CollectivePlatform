// SPDX-License-Identifier: GPL-3.0-or-later
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Collective.Platform;
using Collective.Platform.Abstractions;
using Xunit;

namespace Collective.Platform.Tests;

public class RecentItemsStoreTests
{
    private sealed class FakeSettingsStore : ISettingsStore
    {
        private readonly Dictionary<string, object> _data = new();

        public Task<T?> LoadAsync<T>(string name, CancellationToken cancellationToken = default) where T : class
            => Task.FromResult(_data.TryGetValue(name, out var v) ? (T?)v : null);

        public Task SaveAsync<T>(string name, T value, CancellationToken cancellationToken = default) where T : class
        {
            _data[name] = value;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task GetAsync_returns_empty_when_nothing_stored()
        => Assert.Empty(await new RecentItemsStore(new FakeSettingsStore()).GetAsync());

    [Fact]
    public async Task AddAsync_persists_and_returns_the_updated_list()
    {
        var store = new RecentItemsStore(new FakeSettingsStore());
        Assert.Equal(new[] { "a" }, await store.AddAsync("a"));
        Assert.Equal(new[] { "b", "a" }, await store.AddAsync("b"));
        Assert.Equal(new[] { "b", "a" }, await store.GetAsync());
    }

    [Fact]
    public async Task RemoveAsync_is_case_insensitive()
    {
        var store = new RecentItemsStore(new FakeSettingsStore());
        await store.AddAsync("a");
        await store.RemoveAsync("A");
        Assert.Empty(await store.GetAsync());
    }
}
