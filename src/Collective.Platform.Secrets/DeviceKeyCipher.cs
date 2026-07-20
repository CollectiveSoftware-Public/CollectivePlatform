// SPDX-License-Identifier: GPL-3.0-or-later
using System.Security.Cryptography;

namespace Collective.Platform.Secrets;

/// <summary>
/// AES-256-GCM with a raw 32-byte key (sourced from the OS secret store via
/// <see cref="DeviceKeyProvider"/>) — the canonical device-key blob cipher previously copy-ported
/// into CollectiveAuth/SSH/Wiki — blob layout <c>magic(4) | nonce(12) | ciphertext | tag(16)</c>. The
/// 4-byte magic is per-app (CAS1/CSS1/CWK1/…) so each app's existing store stays readable. This is
/// intentionally NOT password-based: the password path is Collective.Vault.Crypto, used only for backups.
/// </summary>
public sealed class DeviceKeyCipher
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeySize = 32;

    private readonly byte[] _magic;

    public DeviceKeyCipher(ReadOnlySpan<byte> magic4)
    {
        if (magic4.Length != 4)
            throw new ArgumentException("Blob magic must be 4 bytes.", nameof(magic4));
        _magic = magic4.ToArray();
    }

    public byte[] Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> key32)
    {
        if (key32.Length != KeySize)
            throw new ArgumentException("Device key must be 32 bytes.", nameof(key32));

        Span<byte> nonce = stackalloc byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];
        using (var aes = new AesGcm(key32, TagSize))
            aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var output = new byte[_magic.Length + NonceSize + ciphertext.Length + TagSize];
        _magic.CopyTo(output.AsSpan(0));
        nonce.CopyTo(output.AsSpan(_magic.Length));
        ciphertext.CopyTo(output.AsSpan(_magic.Length + NonceSize));
        tag.CopyTo(output.AsSpan(_magic.Length + NonceSize + ciphertext.Length));
        return output;
    }

    public byte[] Decrypt(ReadOnlySpan<byte> blob, ReadOnlySpan<byte> key32)
    {
        if (key32.Length != KeySize)
            throw new ArgumentException("Device key must be 32 bytes.", nameof(key32));
        if (blob.Length < _magic.Length + NonceSize + TagSize || !blob[.._magic.Length].SequenceEqual(_magic))
            throw new CryptographicException("Not a recognized device-key blob.");

        var nonce = blob.Slice(_magic.Length, NonceSize);
        int ctLen = blob.Length - _magic.Length - NonceSize - TagSize;
        var ciphertext = blob.Slice(_magic.Length + NonceSize, ctLen);
        var tag = blob.Slice(_magic.Length + NonceSize + ctLen, TagSize);

        var plaintext = new byte[ctLen];
        using var aes = new AesGcm(key32, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext); // throws CryptographicException on mismatch
        return plaintext;
    }
}
