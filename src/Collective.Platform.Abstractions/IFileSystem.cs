// SPDX-License-Identifier: GPL-3.0-or-later
namespace Collective.Platform.Abstractions;

/// <summary>
/// Filesystem access for app data and settings. The concrete is provided per host (desktop,
/// mobile). Shared across the Collective Software stack so each product stops re-declaring it.
/// </summary>
public interface IFileSystem
{
    /// <summary>Per-user, per-product app data directory (created if missing).</summary>
    string AppDataDirectory { get; }

    bool FileExists(string path);
    Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default);
    Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default);
}
