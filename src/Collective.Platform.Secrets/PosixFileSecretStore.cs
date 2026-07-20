// SPDX-License-Identifier: GPL-3.0-or-later
using System.Security.Cryptography;
using System.Text;

namespace Collective.Platform.Secrets;

/// <summary>
/// <see cref="ISecretStore"/> baseline for Linux/macOS: each secret is written to a file only the
/// owner can read (mode 0600, directory 0700) — the ~/.ssh model. NOT encrypted at rest; the
/// protection is POSIX permissions. A real Keychain/libsecret backend can replace this behind the
/// same seam later. Filenames are the SHA-256 of the key, mirroring <see cref="DpapiSecretStore"/>.
/// </summary>
public sealed class PosixFileSecretStore : ISecretStore
{
    private readonly string _directory;

    public PosixFileSecretStore(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        _directory = directory;
    }

    public async Task SetAsync(string key, string secret, CancellationToken cancellationToken = default)
    {
        var dir = Directory.CreateDirectory(_directory);
        if (!OperatingSystem.IsWindows())
            dir.UnixFileMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
        string path = PathFor(key);
        await File.WriteAllTextAsync(path, secret ?? "", Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        string path = PathFor(key);
        if (!File.Exists(path))
            return null;
        return await File.ReadAllTextAsync(path, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        string path = PathFor(key);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }

    private string PathFor(string key) =>
        Path.Combine(_directory, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key))) + ".secret");
}
