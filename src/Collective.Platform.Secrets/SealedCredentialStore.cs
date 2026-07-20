// SPDX-License-Identifier: GPL-3.0-or-later
using System.Text;
using Collective.Platform.Abstractions;

namespace Collective.Platform.Secrets;

/// <summary>
/// A small id→secret map (account passwords, OAuth tokens, connection passwords) where every value
/// is sealed under the app's device key before it is persisted through <see cref="ISettingsStore"/>.
/// Mirrors CollectiveMail's CredentialStore API minus the master-password parameter — the key comes
/// from the OS secret store, so there is nothing to prompt for.
/// </summary>
public sealed class SealedCredentialStore(
    ISettingsStore settings, DeviceKeyProvider deviceKey, DeviceKeyCipher cipher,
    string settingsKey = "credentials")
{
    private readonly ISettingsStore _settings = settings;
    private readonly DeviceKeyProvider _deviceKey = deviceKey;
    private readonly DeviceKeyCipher _cipher = cipher;
    private readonly string _settingsKey = settingsKey;

    public sealed class Record
    {
        public Dictionary<string, string> Entries { get; set; } = new(StringComparer.Ordinal);
    }

    public async Task SaveAsync(string id, string secret, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentNullException.ThrowIfNull(secret);
        byte[] key = await _deviceKey.GetOrCreateAsync(cancellationToken).ConfigureAwait(false);
        var record = await LoadAsync(cancellationToken).ConfigureAwait(false);
        record.Entries[id] = Convert.ToBase64String(_cipher.Encrypt(Encoding.UTF8.GetBytes(secret), key));
        await _settings.SaveAsync(_settingsKey, record, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var record = await LoadAsync(cancellationToken).ConfigureAwait(false);
        if (!record.Entries.TryGetValue(id, out var sealedValue))
            return null;
        byte[] key = await _deviceKey.GetOrCreateAsync(cancellationToken).ConfigureAwait(false);
        return Encoding.UTF8.GetString(_cipher.Decrypt(Convert.FromBase64String(sealedValue), key));
    }

    public async Task RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        var record = await LoadAsync(cancellationToken).ConfigureAwait(false);
        if (record.Entries.Remove(id))
            await _settings.SaveAsync(_settingsKey, record, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Record> LoadAsync(CancellationToken ct) =>
        await _settings.LoadAsync<Record>(_settingsKey, ct).ConfigureAwait(false) ?? new Record();
}
