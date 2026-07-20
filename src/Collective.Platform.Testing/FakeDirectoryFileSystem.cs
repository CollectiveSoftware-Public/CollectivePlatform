// SPDX-License-Identifier: GPL-3.0-or-later
using Collective.Platform.Abstractions;

namespace Collective.Platform.Testing;

/// <summary>
/// In-memory <see cref="IDirectoryFileSystem"/>. Paths are '/'-separated and relative to the
/// fake's root, which is the empty string ("") — <c>AddFile("a/b.txt")</c> creates directory
/// "a" under the root with file "b.txt" inside it.
/// </summary>
public sealed class FakeDirectoryFileSystem : IDirectoryFileSystem
{
    private static readonly DateTimeOffset DefaultModified = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private readonly Dictionary<string, List<DirectoryEntry>> _dirs = new(StringComparer.Ordinal) { [""] = [] };

    public void AddDirectory(string path) => EnsureDirectory(path);

    public void AddFile(string path, long length = 0)
    {
        int cut = path.LastIndexOf('/');
        string dir = cut < 0 ? "" : path[..cut];
        string name = cut < 0 ? path : path[(cut + 1)..];
        EnsureDirectory(dir).Add(new DirectoryEntry(name, path, false, length, DefaultModified));
    }

    public Task<IReadOnlyList<DirectoryEntry>> ListAsync(string path, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<DirectoryEntry>>(
            _dirs.TryGetValue(path, out var list)
                ? list.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase).ToArray()
                : []);

    public Task<bool> DirectoryExistsAsync(string path, CancellationToken cancellationToken = default)
        => Task.FromResult(_dirs.ContainsKey(path));

    private List<DirectoryEntry> EnsureDirectory(string path)
    {
        if (_dirs.TryGetValue(path, out var list))
            return list;

        _dirs[path] = list = [];
        int cut = path.LastIndexOf('/');
        string parent = cut < 0 ? "" : path[..cut];
        string name = cut < 0 ? path : path[(cut + 1)..];
        var parentList = EnsureDirectory(parent);
        if (!parentList.Any(e => e.Name == name && e.IsDirectory))
            parentList.Add(new DirectoryEntry(name, path, true, 0, DefaultModified));
        return list;
    }
}
