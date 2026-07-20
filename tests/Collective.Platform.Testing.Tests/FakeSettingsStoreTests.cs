// SPDX-License-Identifier: GPL-3.0-or-later
using Collective.Platform.Testing;
using Xunit;

namespace Collective.Platform.Testing.Tests;

public class FakeSettingsStoreTests
{
    private sealed class Prefs { public string Theme { get; set; } = "System"; public int Zoom { get; set; } = 100; }

    [Fact]
    public async Task Save_then_load_round_trips()
    {
        var store = new FakeSettingsStore();
        await store.SaveAsync("prefs", new Prefs { Theme = "Dark", Zoom = 120 });
        var loaded = await store.LoadAsync<Prefs>("prefs");
        Assert.Equal("Dark", loaded!.Theme);
        Assert.Equal(120, loaded.Zoom);
    }

    [Fact]
    public async Task Missing_name_loads_null()
    {
        Assert.Null(await new FakeSettingsStore().LoadAsync<Prefs>("nope"));
    }

    [Fact]
    public async Task Saved_object_mutation_does_not_leak_into_store()
    {
        var store = new FakeSettingsStore();
        var prefs = new Prefs { Theme = "Dark" };
        await store.SaveAsync("prefs", prefs);
        prefs.Theme = "Light"; // mutate AFTER save — a dictionary fake would leak this
        Assert.Equal("Dark", (await store.LoadAsync<Prefs>("prefs"))!.Theme);
    }

    [Fact]
    public async Task Corrupt_json_loads_null_like_the_real_store()
    {
        var store = new FakeSettingsStore();
        store.SetRaw("prefs", "{ not json !");
        Assert.Null(await store.LoadAsync<Prefs>("prefs"));
    }
}
