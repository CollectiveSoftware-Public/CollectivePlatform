// SPDX-License-Identifier: GPL-3.0-or-later
using Collective.Platform.Abstractions;

namespace Collective.Platform;

/// <summary>What a file tree shows. <see cref="Default"/> carries the suite's canonical skip rules:
/// dot-files and dot-directories hidden, build/VCS/IDE directories hidden. Dot handling is split by
/// kind so a code file tree can show dot-<em>files</em> (<c>.gitignore</c>, <c>.env</c> — real files
/// the user edits) while still hiding dot-<em>directories</em> (<c>.git</c>, <c>.vscode</c>): set
/// <see cref="SkipDotFiles"/> false and keep <see cref="SkipDotDirectories"/> true.
/// <see cref="SkipNames"/> applies to directories only — a file named "bin" is shown.</summary>
public sealed class FileTreeFilter
{
    public static readonly FileTreeFilter Default = new();
    public static readonly FileTreeFilter None = new()
    {
        SkipDotFiles = false,
        SkipDotDirectories = false,
        SkipNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
    };

    /// <summary>Hide files whose name starts with '.' (e.g. <c>.gitignore</c>).</summary>
    public bool SkipDotFiles { get; init; } = true;

    /// <summary>Hide directories whose name starts with '.' (e.g. <c>.git</c>, <c>.vscode</c>).</summary>
    public bool SkipDotDirectories { get; init; } = true;

    public IReadOnlySet<string> SkipNames { get; init; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "bin", "obj", "node_modules", ".git", ".vs", ".idea", "dist" };

    public bool Shows(DirectoryEntry entry)
    {
        if (entry.IsDirectory)
        {
            if (SkipDotDirectories && entry.Name.StartsWith('.')) return false;
            return !SkipNames.Contains(entry.Name);
        }
        return !(SkipDotFiles && entry.Name.StartsWith('.'));
    }
}
