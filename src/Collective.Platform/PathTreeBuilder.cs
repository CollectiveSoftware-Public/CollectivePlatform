// SPDX-License-Identifier: GPL-3.0-or-later
namespace Collective.Platform;

/// <summary>One node in a path tree: a folder (<see cref="Path"/> null, with children) or a leaf
/// (<see cref="Path"/> = its '/'-relative source path).</summary>
public sealed record PathNode(string Name, string? Path, IReadOnlyList<PathNode> Children)
{
    public bool IsLeaf => Path is not null;
}

/// <summary>Pure: turns a flat list of '/'-relative paths into a nested folder tree — folders
/// sort before leaves, both alphabetically case-insensitive. (Canonical copy of CollectiveWiki's
/// proven VaultTreeBuilder, with the leaf display name customizable — Wiki strips ".md".)</summary>
public static class PathTreeBuilder
{
    public static IReadOnlyList<PathNode> Build(IReadOnlyList<string> relativePaths, Func<string, string>? leafName = null)
    {
        leafName ??= static n => n;
        var root = new Dir();
        foreach (var path in relativePaths)
        {
            var parts = path.Split('/');
            var dir = root;
            for (int i = 0; i < parts.Length - 1; i++)
                dir = dir.Folder(parts[i]);
            dir.Leaves.Add((leafName(parts[^1]), path));
        }
        return root.ToNodes();
    }

    // Mutable builder mirror of the immutable PathNode tree.
    private sealed class Dir
    {
        private readonly Dictionary<string, Dir> _folders = new(StringComparer.Ordinal);
        public List<(string Name, string Path)> Leaves { get; } = new();

        public Dir Folder(string name)
        {
            if (!_folders.TryGetValue(name, out var d)) _folders[name] = d = new Dir();
            return d;
        }

        public IReadOnlyList<PathNode> ToNodes()
        {
            var nodes = new List<PathNode>();
            foreach (var kv in _folders.OrderBy(f => f.Key, StringComparer.OrdinalIgnoreCase))
                nodes.Add(new PathNode(kv.Key, null, kv.Value.ToNodes()));
            foreach (var (name, path) in Leaves.OrderBy(n => n.Name, StringComparer.OrdinalIgnoreCase))
                nodes.Add(new PathNode(name, path, Array.Empty<PathNode>()));
            return nodes;
        }
    }
}
