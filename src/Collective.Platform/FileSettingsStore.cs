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

    // Two app instances (or an AV scanner) touching the same blob produce millisecond-scale
    // sharing violations; ~5 × 40 ms rides those out. A lock that outlasts the retries is not
    // transient, so the final attempt's exception surfaces to the caller.
    private const int IoAttempts = 5;
    private static readonly TimeSpan IoRetryDelay = TimeSpan.FromMilliseconds(40);

    public async Task<T?> LoadAsync<T>(string name, CancellationToken cancellationToken = default) where T : class
    {
        string path = Path.Combine(_fileSystem.AppDataDirectory, name + ".json");
        if (!_fileSystem.FileExists(path))
            return null;
        // ConfigureAwait(false): callers may block on this with GetAwaiter().GetResult() during
        // app startup (before the UI dispatcher loop runs); capturing the UI context would deadlock.
        string json = await WithIoRetry(ct => _fileSystem.ReadAllTextAsync(path, ct), cancellationToken)
            .ConfigureAwait(false);
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
        return WithIoRetry(async ct => { await _fileSystem.WriteAllTextAsync(path, json, ct).ConfigureAwait(false); return true; },
            cancellationToken);
    }

    private static async Task<TResult> WithIoRetry<TResult>(Func<CancellationToken, Task<TResult>> op, CancellationToken ct)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await op(ct).ConfigureAwait(false);
            }
            catch (IOException) when (attempt < IoAttempts)
            {
                await Task.Delay(IoRetryDelay, ct).ConfigureAwait(false);
            }
        }
    }
}
