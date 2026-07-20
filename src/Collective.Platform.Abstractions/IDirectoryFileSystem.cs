// SPDX-License-Identifier: GPL-3.0-or-later
namespace Collective.Platform.Abstractions;

/// <summary>One directory child. For directories, <see cref="Length"/> is 0.</summary>
public sealed record DirectoryEntry(string Name, string FullPath, bool IsDirectory, long Length, DateTimeOffset Modified);

/// <summary>
/// Directory-aware listing. Async because implementations span local disk and remote (SFTP)
/// filesystems. Contract: <see cref="ListAsync"/> returns entries name-sorted (OrdinalIgnoreCase);
/// implementations are best-effort — an unreadable or missing directory lists as empty rather
/// than throwing (matching the suite's tree-UI behavior).
/// </summary>
public interface IDirectoryFileSystem
{
    Task<IReadOnlyList<DirectoryEntry>> ListAsync(string path, CancellationToken cancellationToken = default);
    Task<bool> DirectoryExistsAsync(string path, CancellationToken cancellationToken = default);
}
