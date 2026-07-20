// SPDX-License-Identifier: GPL-3.0-or-later
using Collective.Platform.Secrets;
using Xunit;

namespace Collective.Platform.Secrets.Tests;

public class DeviceKeyProviderTests
{
    private sealed class FakeSecretStore : ISecretStore
    {
        public readonly Dictionary<string, string> Data = new();
        public Task SetAsync(string key, string secret, CancellationToken ct = default)
        { Data[key] = secret; return Task.CompletedTask; }
        public Task<string?> GetAsync(string key, CancellationToken ct = default)
            => Task.FromResult(Data.TryGetValue(key, out var v) ? v : null);
        public Task RemoveAsync(string key, CancellationToken ct = default)
        { Data.Remove(key); return Task.CompletedTask; }
    }

    [Fact]
    public async Task Creates_a_32_byte_key_and_persists_it_base64()
    {
        var store = new FakeSecretStore();
        var key = await new DeviceKeyProvider(store, "app.devicekey").GetOrCreateAsync();
        Assert.Equal(32, key.Length);
        Assert.Equal(key, Convert.FromBase64String(store.Data["app.devicekey"]));
    }

    [Fact]
    public async Task Returns_the_existing_key_unchanged()
    {
        var store = new FakeSecretStore();
        var provider = new DeviceKeyProvider(store, "app.devicekey");
        var first = await provider.GetOrCreateAsync();
        Assert.Equal(first, await provider.GetOrCreateAsync());
        Assert.Equal(first, await new DeviceKeyProvider(store, "app.devicekey").GetOrCreateAsync());
    }

    [Fact]
    public async Task Distinct_key_names_get_distinct_keys()
    {
        var store = new FakeSecretStore();
        var a = await new DeviceKeyProvider(store, "a").GetOrCreateAsync();
        var b = await new DeviceKeyProvider(store, "b").GetOrCreateAsync();
        Assert.NotEqual(a, b);
    }
}
