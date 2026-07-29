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

    // ---- transient sharing violations (two app instances touching the same blob) are retried
    // briefly instead of surfacing an IOException to the caller ----

    [Fact]
    public async Task Save_RetriesThroughATransientLock()
    {
        if (!OperatingSystem.IsWindows()) return;   // POSIX has no mandatory sharing locks

        var fs = new TempDirFileSystem();
        var store = new FileSettingsStore(fs);
        var path = System.IO.Path.Combine(fs.AppDataDirectory, "settings.json");
        await System.IO.File.WriteAllTextAsync(path, "{}");

        var gate = System.IO.File.Open(path, System.IO.FileMode.Open,
            System.IO.FileAccess.Read, System.IO.FileShare.None);
        var save = store.SaveAsync("settings", new Sample { Number = 42 });
        await Task.Delay(80);
        gate.Dispose();                              // lock released mid-retry

        await save;                                  // must succeed, not throw
        Assert.Equal(42, (await store.LoadAsync<Sample>("settings"))!.Number);
    }

    [Fact]
    public async Task Load_RetriesThroughATransientLock()
    {
        if (!OperatingSystem.IsWindows()) return;

        var fs = new TempDirFileSystem();
        var store = new FileSettingsStore(fs);
        await store.SaveAsync("settings", new Sample { Number = 7 });
        var path = System.IO.Path.Combine(fs.AppDataDirectory, "settings.json");

        var gate = System.IO.File.Open(path, System.IO.FileMode.Open,
            System.IO.FileAccess.Read, System.IO.FileShare.None);
        var load = store.LoadAsync<Sample>("settings");
        await Task.Delay(80);
        gate.Dispose();

        Assert.Equal(7, (await load)!.Number);
    }
}
