// SPDX-License-Identifier: GPL-3.0-or-later
namespace Collective.Platform.Secrets;

/// <summary>
/// Picks the right <see cref="ISecretStore"/> for the current OS so heads stop hand-writing the
/// IsWindows() guard: Windows → DPAPI (<see cref="DpapiSecretStore"/>), Linux/macOS →
/// <see cref="EncryptedFileSecretStore"/> (AES-256-GCM at rest under an owner-only key file). The
/// lighter permissions-only <see cref="PosixFileSecretStore"/> (~/.ssh model, plaintext at rest)
/// remains available for heads that deliberately want no key management; it is strictly weaker, so
/// it is not the default. See each type's docs for the at-rest guarantees.
/// </summary>
public static class SecretStores
{
    public static ISecretStore CreateDefault(string directory) =>
        OperatingSystem.IsWindows()
            ? new DpapiSecretStore(directory)
            : new EncryptedFileSecretStore(directory);
}
