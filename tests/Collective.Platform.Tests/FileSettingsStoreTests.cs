// SPDX-License-Identifier: GPL-3.0-or-later
using Collective.Platform.Testing;
using Xunit;

namespace Collective.Platform.Tests;

public class FileSettingsStoreTests
{
    private sealed record Sample
    {
        public int Number { get; init; }
        public string Text { get; init; } = "";
        public bool Flag { get; init; }
    }

    [Fact]
    public async Task SaveThenLoad_RoundTrips()
    {
        var store = new FileSettingsStore(new FakeFileSystem());
        await store.SaveAsync("settings", new Sample { Number = 7, Text = "hi", Flag = true });

        var loaded = await store.LoadAsync<Sample>("settings");
        Assert.NotNull(loaded);
        Assert.Equal(7, loaded!.Number);
        Assert.Equal("hi", loaded.Text);
        Assert.True(loaded.Flag);
    }

    [Fact]
    public async Task Load_Missing_ReturnsNull()
    {
        var store = new FileSettingsStore(new FakeFileSystem());
        Assert.Null(await store.LoadAsync<Sample>("nope"));
    }

    [Fact]
    public async Task Load_CorruptJson_ReturnsNull_NotThrow()
    {
        var fs = new FakeFileSystem();
        fs.Set("/fake/appdata/settings.json", "{ not valid json ");
        var store = new FileSettingsStore(fs);
        Assert.Null(await store.LoadAsync<Sample>("settings"));
    }
}
