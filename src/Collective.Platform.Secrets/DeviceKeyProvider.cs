// SPDX-License-Identifier: GPL-3.0-or-later
using System.Security.Cryptography;

namespace Collective.Platform.Secrets;

/// <summary>
/// Gets (or lazily creates) an app's 32-byte device key, stored base64 in the OS-backed
/// <see cref="ISecretStore"/> under a per-app <paramref name="keyName"/> — the acquisition logic
/// previously copied into Auth's EncryptedSeedStore, SSH's EncryptedIdentityStore and Wiki's
/// AtRestSealer. Key names stay per-app so existing stored keys keep resolving.
/// </summary>
public sealed class DeviceKeyProvider(ISecretStore secrets, string keyName)
{
    private readonly ISecretStore _secrets = secrets;
    private readonly string _keyName = keyName;

    public async Task<byte[]> GetOrCreateAsync(CancellationToken ct = default)
    {
        string? existing = await _secrets.GetAsync(_keyName, ct).ConfigureAwait(false);
        if (existing is not null)
            return Convert.FromBase64String(existing);
        byte[] key = RandomNumberGenerator.GetBytes(32);
        await _secrets.SetAsync(_keyName, Convert.ToBase64String(key), ct).ConfigureAwait(false);
        return key;
    }
}
