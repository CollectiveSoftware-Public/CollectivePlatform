// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Collective.Platform.Abstractions;

namespace Collective.Platform.Tests;

/// <summary>Real-disk <see cref="IFileSystem"/> over a fresh temp directory — for tests that need
/// genuine OS file semantics (sharing violations, locks) the in-memory fake cannot express.
/// Mirrors DesktopFileSystem's I/O exactly (bare File.* calls, exclusive opens).</summary>
internal sealed class TempDirFileSystem : IFileSystem
{
    public TempDirFileSystem()
    {
        AppDataDirectory = Path.Combine(Path.GetTempPath(), "cp-fs-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(AppDataDirectory);
    }

    public string AppDataDirectory { get; }

    public bool FileExists(string path) => File.Exists(path);

    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
        => File.ReadAllTextAsync(path, cancellationToken);

    public Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default)
        => File.WriteAllTextAsync(path, contents, cancellationToken);
}
