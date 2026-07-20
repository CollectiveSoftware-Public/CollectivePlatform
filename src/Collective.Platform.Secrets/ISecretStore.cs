// SPDX-License-Identifier: GPL-3.0-or-later
namespace Collective.Platform.Secrets;

/// <summary>
/// Persists small secrets (a master password, an OAuth token) encrypted at rest under the OS user
/// account, so a head can remember them without keeping plaintext or re-prompting every session.
/// Keyed by a stable string. The default desktop implementation is Windows DPAPI
/// (<see cref="DpapiSecretStore"/>); macOS Keychain and Linux libsecret implementations are future
/// per-OS additions behind this same seam, selected by the head at composition time.
/// </summary>
public interface ISecretStore
{
    /// <summary>Stores (or replaces) the secret for <paramref name="key"/>, encrypted at rest.</summary>
    Task SetAsync(string key, string secret, CancellationToken cancellationToken = default);

    /// <summary>Returns the stored secret for <paramref name="key"/>, or <c>null</c> if none / unreadable.</summary>
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Removes any stored secret for <paramref name="key"/>.</summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
