// SPDX-License-Identifier: GPL-3.0-or-later
using System.Text.Json;
using System.Text.Json.Serialization;
using Collective.Platform.Abstractions;

namespace Collective.Platform;

/// <summary>JSON-backed <see cref="ISettingsStore"/> under <see cref="IFileSystem.AppDataDirectory"/>.</summary>
public sealed class FileSettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly IFileSystem _fileSystem;

    public FileSettingsStore(IFileSystem fileSystem) => _fileSystem = fileSystem;

    public async Task<T?> LoadAsync<T>(string name, CancellationToken cancellationToken = default) where T : class
    {
        string path = Path.Combine(_fileSystem.AppDataDirectory, name + ".json");
        if (!_fileSystem.FileExists(path))
            return null;
        // ConfigureAwait(false): callers may block on this with GetAwaiter().GetResult() during
        // app startup (before the UI dispatcher loop runs); capturing the UI context would deadlock.
        string json = await _fileSystem.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        try
        {
            return JsonSerializer.Deserialize<T>(json, Options);
        }
        catch (JsonException)
        {
            return null; // corrupt settings: fall back to defaults rather than crash
        }
    }

    public Task SaveAsync<T>(string name, T value, CancellationToken cancellationToken = default) where T : class
    {
        string path = Path.Combine(_fileSystem.AppDataDirectory, name + ".json");
        string json = JsonSerializer.Serialize(value, Options);
        return _fileSystem.WriteAllTextAsync(path, json, cancellationToken);
    }
}
