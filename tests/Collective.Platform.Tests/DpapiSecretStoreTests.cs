// SPDX-License-Identifier: GPL-3.0-or-later
using Collective.Platform.Secrets;
using Xunit;

namespace Collective.Platform.Tests;

/// <summary>
/// Exercises the Windows DPAPI secret store with a real round-trip. DPAPI is Windows-only, so the
/// assertions run on the Windows leg of the CI matrix; on macOS/Linux the tests return early (DPAPI
/// would throw <see cref="PlatformNotSupportedException"/> there by design).
/// </summary>
public sealed class DpapiSecretStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "cplat-secrets-" + Guid.NewGuid().ToString("N"));

    private DpapiSecretStore Store() => new(_dir);

    [Fact]
    public async Task Set_then_get_round_trips()
    {
        if (!OperatingSystem.IsWindows()) return;
        var store = Store();
        await store.SetAsync("master", "hunter2");
        Assert.Equal("hunter2", await store.GetAsync("master"));
    }

    [Fact]
    public async Task Set_replaces_an_existing_value()
    {
        if (!OperatingSystem.IsWindows()) return;
        var store = Store();
        await store.SetAsync("k", "one");
        await store.SetAsync("k", "two");
        Assert.Equal("two", await store.GetAsync("k"));
    }

    [Fact]
    public async Task Get_missing_returns_null()
    {
        if (!OperatingSystem.IsWindows()) return;
        Assert.Null(await Store().GetAsync("nope"));
    }

    [Fact]
    public async Task Remove_deletes_the_secret()
    {
        if (!OperatingSystem.IsWindows()) return;
        var store = Store();
        await store.SetAsync("k", "v");
        await store.RemoveAsync("k");
        Assert.Null(await store.GetAsync("k"));
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true); }
        catch { /* best-effort temp cleanup */ }
    }
}
