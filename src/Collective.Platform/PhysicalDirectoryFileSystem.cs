// SPDX-License-Identifier: GPL-3.0-or-later
using Collective.Platform.Abstractions;

namespace Collective.Platform;

/// <summary>System.IO-backed <see cref="IDirectoryFileSystem"/>. Best-effort per the seam's
/// contract: unreadable/missing directories list as empty. Completes synchronously, so tree
/// components driven from a UI thread pay no context hops on local disks.</summary>
public sealed class PhysicalDirectoryFileSystem : IDirectoryFileSystem
{
    public Task<IReadOnlyList<DirectoryEntry>> ListAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            var entries = new List<DirectoryEntry>();
            foreach (var info in new DirectoryInfo(path).EnumerateFileSystemInfos())
            {
                bool isDir = (info.Attributes & FileAttributes.Directory) != 0;
                entries.Add(new DirectoryEntry(
                    info.Name, info.FullName, isDir,
                    isDir ? 0 : ((FileInfo)info).Length, info.LastWriteTimeUtc));
            }
            entries.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name));
            return Task.FromResult<IReadOnlyList<DirectoryEntry>>(entries);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Task.FromResult<IReadOnlyList<DirectoryEntry>>([]);
        }
    }

    public Task<bool> DirectoryExistsAsync(string path, CancellationToken cancellationToken = default)
        => Task.FromResult(Directory.Exists(path));
}
