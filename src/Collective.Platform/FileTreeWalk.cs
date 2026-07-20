// SPDX-License-Identifier: GPL-3.0-or-later
using System.Runtime.CompilerServices;
using Collective.Platform.Abstractions;

namespace Collective.Platform;

/// <summary>Recursive file enumeration over the directory seam, for project-wide search and
/// folder scans (canonical port of CollectiveCode's FileNode.EnumerateFiles). Data-only: uses
/// ConfigureAwait(false) and is safe to consume off the UI thread.</summary>
public static class FileTreeWalk
{
    public static async IAsyncEnumerable<DirectoryEntry> EnumerateFilesAsync(
        IDirectoryFileSystem fileSystem, string root, FileTreeFilter? filter = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        filter ??= FileTreeFilter.Default;
        var stack = new Stack<string>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            string dir = stack.Pop();
            var entries = await fileSystem.ListAsync(dir, ct).ConfigureAwait(false);
            foreach (var entry in entries)
            {
                if (!filter.Shows(entry)) continue;
                if (entry.IsDirectory) stack.Push(entry.FullPath);
                else yield return entry;
            }
        }
    }
}
