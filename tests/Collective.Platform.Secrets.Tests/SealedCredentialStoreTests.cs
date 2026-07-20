// SPDX-License-Identifier: GPL-3.0-or-later
using System.Text;
using System.Text.Json;
using Collective.Platform.Abstractions;
using Collective.Platform.Secrets;
using Xunit;

namespace Collective.Platform.Secrets.Tests;

public class SealedCredentialStoreTests
{
    private sealed class FakeSettingsStore : ISettingsStore
    {
        public readonly Dictionary<string, object> Data = new();
        public Task<T?> LoadAsync<T>(string name, CancellationToken ct = default) where T : class
            => Task.FromResult(Data.TryGetValue(name, out var v) ? (T?)v : null);
        public Task SaveAsync<T>(string name, T value, CancellationToken ct = default) where T : class
        { Data[name] = value; return Task.CompletedTask; }
    }

    private sealed class FakeSecretStore : ISecretStore
    {
        private readonly Dictionary<string, string> _data = new();
        public Task SetAsync(string k, string s, CancellationToken ct = default) { _data[k] = s; return Task.CompletedTask; }
        public Task<string?> GetAsync(string k, CancellationToken ct = default)
            => Task.FromResult(_data.TryGetValue(k, out var v) ? v : null);
        public Task RemoveAsync(string k, CancellationToken ct = default) { _data.Remove(k); return Task.CompletedTask; }
    }

    private static SealedCredentialStore NewStore(FakeSettingsStore settings) => new(
        settings,
        new DeviceKeyProvider(new FakeSecretStore(), "test.devicekey"),
        new DeviceKeyCipher("CTS1"u8));

    [Fact]
    public async Task Save_get_remove_round_trips()
    {
        var settings = new FakeSettingsStore();
        var store = NewStore(settings);
        await store.SaveAsync("acct-1", "hunter2");
        Assert.Equal("hunter2", await store.GetAsync("acct-1"));
        await store.RemoveAsync("acct-1");
        Assert.Null(await store.GetAsync("acct-1"));
    }

    [Fact]
    public async Task Unknown_id_is_null()
        => Assert.Null(await NewStore(new FakeSettingsStore()).GetAsync("nope"));

    [Fact]
    public async Task Persisted_blob_never_contains_the_plaintext()
    {
        var settings = new FakeSettingsStore();
        await NewStore(settings).SaveAsync("acct-1", "hunter2");
        string persisted = JsonSerializer.Serialize(settings.Data.Values.Single());
        Assert.DoesNotContain("hunter2", persisted, StringComparison.Ordinal);
    }
}
