// SPDX-License-Identifier: GPL-3.0-or-later
using System.Security.Cryptography;
using Collective.Platform.Secrets;
using Xunit;

namespace Collective.Platform.Tests;

/// <summary>
/// Exercises the autarkic AES-256-GCM file secret store — the non-Windows persistent secret backend.
/// Unlike DPAPI these assertions run on every OS (BCL crypto), covering round-trip, replacement,
/// persistence across instances, tamper/wrong-key rejection, key-name binding, and key-file perms.
/// </summary>
public sealed class EncryptedFileSecretStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "cplat-encsecrets-" + Guid.NewGuid().ToString("N"));

    private EncryptedFileSecretStore Store() => new(_dir);

    [Fact]
    public async Task Set_then_get_round_trips()
    {
        var store = Store();
        await store.SetAsync("master", "hunter2");
        Assert.Equal("hunter2", await store.GetAsync("master"));
    }

    [Fact]
    public async Task Set_replaces_an_existing_value()
    {
        var store = Store();
        await store.SetAsync("k", "one");
        await store.SetAsync("k", "two");
        Assert.Equal("two", await store.GetAsync("k"));
    }

    [Fact]
    public async Task Get_missing_returns_null()
    {
        Assert.Null(await Store().GetAsync("nope"));
    }

    [Fact]
    public async Task Remove_deletes_the_secret()
    {
        var store = Store();
        await store.SetAsync("k", "v");
        await store.RemoveAsync("k");
        Assert.Null(await store.GetAsync("k"));
    }

    [Fact]
    public async Task A_new_instance_over_the_same_dir_reads_the_secret()
    {
        await Store().SetAsync("token", "abc123");          // first instance creates the key file
        Assert.Equal("abc123", await Store().GetAsync("token")); // second instance reuses it
    }

    [Fact]
    public async Task Tampered_ciphertext_is_rejected_as_absent()
    {
        var store = Store();
        await store.SetAsync("k", "secret");

        string file = Directory.GetFiles(_dir, "*.aesgcm").Single();
        byte[] bytes = await File.ReadAllBytesAsync(file);
        bytes[^1] ^= 0xFF;                                   // flip a ciphertext/tag bit
        await File.WriteAllBytesAsync(file, bytes);

        Assert.Null(await store.GetAsync("k"));
    }

    [Fact]
    public async Task A_secret_file_cannot_be_read_under_a_different_key_name()
    {
        var store = Store();
        await store.SetAsync("alpha", "value-a");

        // Move alpha's ciphertext to beta's filename; the key-name AAD must make it undecryptable.
        string alpha = Path.Combine(_dir, Convert.ToHexString(SHA256.HashData("alpha"u8.ToArray())) + ".aesgcm");
        string beta = Path.Combine(_dir, Convert.ToHexString(SHA256.HashData("beta"u8.ToArray())) + ".aesgcm");
        File.Copy(alpha, beta, overwrite: true);

        Assert.Null(await store.GetAsync("beta"));
    }

    [Fact]
    public async Task Key_file_is_owner_only_on_unix()
    {
        if (OperatingSystem.IsWindows()) return; // perms are a POSIX concept
        await Store().SetAsync("k", "v");

        var mode = File.GetUnixFileMode(Path.Combine(_dir, ".secretkey"));
        Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, mode);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true); }
        catch { /* best-effort temp cleanup */ }
    }
}
