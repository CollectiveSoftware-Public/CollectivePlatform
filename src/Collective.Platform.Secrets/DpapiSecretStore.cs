// SPDX-License-Identifier: GPL-3.0-or-later
using System.Security.Cryptography;
using System.Text;

namespace Collective.Platform.Secrets;

/// <summary>
/// <see cref="ISecretStore"/> backed by Windows DPAPI (<see cref="DataProtectionScope.CurrentUser"/>):
/// each secret is encrypted with the signed-in user's key and written as a file under the configured
/// directory. Only the same Windows user can decrypt it and no key material lives in the app. Windows
/// only — every method throws <see cref="PlatformNotSupportedException"/> elsewhere (a head supplies a
/// platform-specific store on macOS/Linux).
/// </summary>
public sealed class DpapiSecretStore : ISecretStore
{
    private readonly string _directory;

    public DpapiSecretStore(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        _directory = directory;
    }

    public Task SetAsync(string key, string secret, CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
            throw NotSupported();
        Directory.CreateDirectory(_directory);
        byte[] plaintext = Encoding.UTF8.GetBytes(secret ?? "");
        byte[] encrypted = ProtectedData.Protect(plaintext, optionalEntropy: null, DataProtectionScope.CurrentUser);
        CryptographicOperations.ZeroMemory(plaintext);
        return File.WriteAllBytesAsync(PathFor(key), encrypted, cancellationToken);
    }

    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
            throw NotSupported();
        string path = PathFor(key);
        if (!File.Exists(path))
            return null;
        try
        {
            byte[] encrypted = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
            byte[] plaintext = ProtectedData.Unprotect(encrypted, optionalEntropy: null, DataProtectionScope.CurrentUser);
            try { return Encoding.UTF8.GetString(plaintext); }
            finally { CryptographicOperations.ZeroMemory(plaintext); }
        }
        catch (CryptographicException)
        {
            return null; // corrupt blob or a different user — treat as absent
        }
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        string path = PathFor(key);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }

    // Filenames are the SHA-256 of the key (no raw key on disk; safe characters).
    private string PathFor(string key) =>
        Path.Combine(_directory, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key))) + ".dpapi");

    private static PlatformNotSupportedException NotSupported() =>
        new("DpapiSecretStore requires Windows; supply a platform-specific ISecretStore on macOS/Linux.");
}
