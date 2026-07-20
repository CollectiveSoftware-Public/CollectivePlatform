// SPDX-License-Identifier: GPL-3.0-or-later
using System.Collections.Concurrent;
using Collective.Platform.Abstractions;

namespace Collective.Platform.Testing;

/// <summary>
/// In-memory <see cref="IFileSystem"/> for deterministic tests (no disk). Superset of the copies
/// it replaces: seed with <see cref="Set"/>, assert via <see cref="Files"/>, and pass an
/// <paramref name="appDataDirectory"/> of "" for relative-path tests (the SSH/Auth style).
/// </summary>
public sealed class FakeFileSystem(string appDataDirectory = "/fake/appdata") : IFileSystem
{
    private readonly ConcurrentDictionary<string, string> _files = new(StringComparer.Ordinal);

    public string AppDataDirectory { get; } = appDataDirectory;

    /// <summary>Read-only view of the stored files (assert on contents/count).</summary>
    public IReadOnlyDictionary<string, string> Files => _files;

    public bool FileExists(string path) => _files.ContainsKey(path);

    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
        => _files.TryGetValue(path, out var v) ? Task.FromResult(v) : throw new FileNotFoundException(path);

    public Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default)
    {
        _files[path] = contents;
        return Task.CompletedTask;
    }

    /// <summary>Seed or corrupt a file directly (for arranging test state).</summary>
    public void Set(string path, string contents) => _files[path] = contents;
}
