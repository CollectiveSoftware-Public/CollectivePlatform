// SPDX-License-Identifier: GPL-3.0-or-later
using Collective.Platform.Secrets;
using Xunit;

namespace Collective.Platform.Secrets.Tests;

public class PosixFileSecretStoreTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("cps-secrets-").FullName;
    public void Dispose() { try { Directory.Delete(_dir, recursive: true); } catch { } }

    [Fact]
    public async Task Set_get_remove_round_trips()
    {
        var store = new PosixFileSecretStore(_dir);
        await store.SetAsync("k", "value");
        Assert.Equal("value", await store.GetAsync("k"));
        await store.RemoveAsync("k");
        Assert.Null(await store.GetAsync("k"));
    }

    [Fact]
    public async Task Missing_key_is_null()
        => Assert.Null(await new PosixFileSecretStore(_dir).GetAsync("absent"));

    [Fact]
    public async Task Overwrite_replaces()
    {
        var store = new PosixFileSecretStore(_dir);
        await store.SetAsync("k", "one");
        await store.SetAsync("k", "two");
        Assert.Equal("two", await store.GetAsync("k"));
    }

    [Fact]
    public async Task Secret_files_are_owner_only_on_posix()
    {
        var store = new PosixFileSecretStore(_dir);
        await store.SetAsync("k", "value");
        if (!OperatingSystem.IsWindows())
        {
            string file = Directory.GetFiles(_dir).Single();
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(file));
        }
    }

    [Fact]
    public void CreateDefault_picks_the_os_store()
    {
        var store = SecretStores.CreateDefault(_dir);
        if (OperatingSystem.IsWindows())
            Assert.IsType<DpapiSecretStore>(store);
        else
            Assert.IsType<EncryptedFileSecretStore>(store); // encrypted-at-rest is the non-Windows default (strictly ≥ PosixFileSecretStore)
    }
}
