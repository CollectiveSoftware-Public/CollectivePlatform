// SPDX-License-Identifier: GPL-3.0-or-later
using System.Security.Cryptography;
using System.Text;

namespace Collective.Platform.Secrets;

/// <summary>
/// <see cref="ISecretStore"/> backed by an AES-256-GCM encrypted file per secret, under a single
/// per-user random key kept in an owner-only (0600) key file in the same directory. BCL-only — no
/// DPAPI, no libsecret/D-Bus — so it gives Linux (and macOS) the persistent secret storage Windows
/// gets from <see cref="DpapiSecretStore"/>, while staying autarkic. This is the non-Windows default a
/// head selects at composition time.
///
/// Threat model ≈ DPAPI <c>CurrentUser</c>: the key file's owner-only permissions protect secrets from
/// other users and from offline disk access by others; a process already running as the signed-in user
/// can read the key (just as it could decrypt DPAPI data). It is not a defence against malware running
/// as you. The key name is bound as AEAD associated data, so a secret file cannot be silently swapped
/// to a different key.
/// </summary>
public sealed class EncryptedFileSecretStore : ISecretStore
{
    private const int KeySize = 32;    // AES-256
    private const int NonceSize = 12;  // AES-GCM standard nonce
    private const int TagSize = 16;    // AES-GCM tag

    private readonly string _directory;
    private readonly string _keyPath;
    private readonly object _keyLock = new();
    private byte[]? _key;

    public EncryptedFileSecretStore(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        _directory = directory;
        _keyPath = Path.Combine(directory, ".secretkey");
    }

    public async Task SetAsync(string key, string secret, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        byte[] dek = GetOrCreateKey();
        byte[] plaintext = Encoding.UTF8.GetBytes(secret ?? "");
        try
        {
            byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
            byte[] cipher = new byte[plaintext.Length];
            byte[] tag = new byte[TagSize];
            using (var gcm = new AesGcm(dek, TagSize))
                gcm.Encrypt(nonce, plaintext, cipher, tag, AssociatedData(key));

            byte[] blob = new byte[NonceSize + TagSize + cipher.Length];
            Buffer.BlockCopy(nonce, 0, blob, 0, NonceSize);
            Buffer.BlockCopy(tag, 0, blob, NonceSize, TagSize);
            Buffer.BlockCopy(cipher, 0, blob, NonceSize + TagSize, cipher.Length);

            await File.WriteAllBytesAsync(PathFor(key), blob, cancellationToken).ConfigureAwait(false);
            SetOwnerOnly(PathFor(key));
        }
        finally { CryptographicOperations.ZeroMemory(plaintext); }
    }

    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        string path = PathFor(key);
        if (!File.Exists(path) || !File.Exists(_keyPath))
            return null;
        try
        {
            byte[] dek = GetOrCreateKey();
            byte[] blob = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
            if (blob.Length < NonceSize + TagSize)
                return null;

            var nonce = blob.AsSpan(0, NonceSize);
            var tag = blob.AsSpan(NonceSize, TagSize);
            var cipher = blob.AsSpan(NonceSize + TagSize);
            byte[] plaintext = new byte[cipher.Length];
            using (var gcm = new AesGcm(dek, TagSize))
                gcm.Decrypt(nonce, cipher, tag, plaintext, AssociatedData(key));
            try { return Encoding.UTF8.GetString(plaintext); }
            finally { CryptographicOperations.ZeroMemory(plaintext); }
        }
        catch (CryptographicException)
        {
            return null; // tampered, truncated, or written under a different key — treat as absent
        }
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        string path = PathFor(key);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }

    private byte[] GetOrCreateKey()
    {
        lock (_keyLock)
        {
            if (_key is not null)
                return _key;

            Directory.CreateDirectory(_directory);
            if (File.Exists(_keyPath))
            {
                byte[] existing = File.ReadAllBytes(_keyPath);
                if (existing.Length == KeySize)
                    return _key = existing;
            }

            byte[] fresh = RandomNumberGenerator.GetBytes(KeySize);
            WriteKeyFile(_keyPath, fresh); // 0600 from creation on Unix
            return _key = fresh;
        }
    }

    private static void WriteKeyFile(string path, byte[] key)
    {
        var options = new FileStreamOptions { Mode = FileMode.Create, Access = FileAccess.Write };
        if (!OperatingSystem.IsWindows())
            options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite; // create as 0600, no world-readable window
        using var fs = new FileStream(path, options);
        fs.Write(key);
    }

    private static void SetOwnerOnly(string path)
    {
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    // Bind the key name into the AEAD so a secret file can't be swapped between keys.
    private static byte[] AssociatedData(string key) => Encoding.UTF8.GetBytes("cps:" + key);

    // Filenames are the SHA-256 of the key (no raw key on disk; safe characters), mirroring DpapiSecretStore.
    private string PathFor(string key) =>
        Path.Combine(_directory, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key))) + ".aesgcm");
}
