// SPDX-License-Identifier: GPL-3.0-or-later
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Collective.Platform.Abstractions;

namespace Collective.Platform;

public enum FileTreeExpanderMode
{
    /// <summary>Probe each child folder once so only non-empty folders show a chevron (local FS).</summary>
    ProbeChildren,
    /// <summary>Show a chevron on every folder without probing — probing a remote FS costs one
    /// round-trip per folder.</summary>
    AlwaysShowForDirectories,
}

/// <summary>
/// A lazy, observable file-tree node over <see cref="IDirectoryFileSystem"/> — the canonical
/// generalization of CollectiveCode's FileNode (API names preserved). The root's immediate
/// children load up front; each folder's contents load when expanded, keeping "open folder"
/// instant on arbitrarily large trees.
/// THREADING: children are mutated on the caller's context (no ConfigureAwait(false)) — drive a
/// TreeView-bound node from the UI thread, exactly as CollectiveCode drives FileNode today.
/// </summary>
public sealed class FileTreeNode : INotifyPropertyChanged
{
    private readonly IDirectoryFileSystem _fs;
    private readonly FileTreeFilter _filter;
    private readonly FileTreeExpanderMode _mode;
    private bool _loaded;
    private bool _loading;
    private bool _isExpanded;

    private FileTreeNode(IDirectoryFileSystem fs, FileTreeFilter filter, FileTreeExpanderMode mode,
                         string name, string fullPath, bool isDirectory)
    {
        _fs = fs; _filter = filter; _mode = mode;
        Name = name; FullPath = fullPath; IsDirectory = isDirectory;
    }

    public string Name { get; }
    public string FullPath { get; }
    public bool IsDirectory { get; }
    public ObservableCollection<FileTreeNode> Children { get; } = new();

    /// <summary>The parent folder node, or null for the root. Lets context-menu edits refresh in place.</summary>
    public FileTreeNode? Parent { get; private set; }

    /// <summary>Bound two-way to the TreeViewItem; expanding a not-yet-loaded folder populates it
    /// (fire-and-forget — see the threading note on the class).</summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value) return;
            _isExpanded = value;
            OnPropertyChanged();
            if (value) _ = EnsureChildrenLoadedAsync();
        }
    }

    /// <summary>Builds the root node and loads its immediate children.</summary>
    public static async Task<FileTreeNode> CreateRootAsync(IDirectoryFileSystem fileSystem, string rootPath,
        FileTreeFilter? filter = null, FileTreeExpanderMode mode = FileTreeExpanderMode.ProbeChildren)
    {
        string trimmed = rootPath.TrimEnd('/', '\\');
        int cut = Math.Max(trimmed.LastIndexOf('/'), trimmed.LastIndexOf('\\'));
        string name = cut >= 0 ? trimmed[(cut + 1)..] : trimmed;
        var root = new FileTreeNode(fileSystem, filter ?? FileTreeFilter.Default, mode,
                                    string.IsNullOrEmpty(name) ? rootPath : name, rootPath, true);
        await root.EnsureChildrenLoadedAsync();
        return root;
    }

    /// <summary>Lists this folder's immediate children once (idempotent). In
    /// <see cref="FileTreeExpanderMode.ProbeChildren"/> each child folder with visible content
    /// gets a placeholder child so its expander chevron shows.</summary>
    public async Task EnsureChildrenLoadedAsync()
    {
        if (_loaded || _loading || !IsDirectory) return;
        _loading = true;
        try
        {
            var entries = await _fs.ListAsync(FullPath);
            Children.Clear(); // drop the placeholder, if any

            foreach (var e in entries)
            {
                if (!e.IsDirectory || !_filter.Shows(e)) continue;
                var child = new FileTreeNode(_fs, _filter, _mode, e.Name, e.FullPath, true) { Parent = this };
                if (await ShowsChevronAsync(e.FullPath))
                    child.Children.Add(new FileTreeNode(_fs, _filter, _mode, "", "", false));
                Children.Add(child);
            }
            foreach (var e in entries)
            {
                if (e.IsDirectory || !_filter.Shows(e)) continue;
                Children.Add(new FileTreeNode(_fs, _filter, _mode, e.Name, e.FullPath, false) { Parent = this });
            }
            _loaded = true;
        }
        finally { _loading = false; }
    }

    /// <summary>Re-read this folder (after a create/rename/delete). For a file node, reloads its
    /// parent instead. Keeps the folder expanded if it already was.</summary>
    public async Task ReloadAsync()
    {
        if (!IsDirectory)
        {
            if (Parent is not null) await Parent.ReloadAsync();
            return;
        }
        bool wasExpanded = _isExpanded || Parent is null;
        _loaded = false;
        Children.Clear();
        await EnsureChildrenLoadedAsync();
        _isExpanded = wasExpanded;
    }

    private async Task<bool> ShowsChevronAsync(string dirPath)
    {
        if (_mode == FileTreeExpanderMode.AlwaysShowForDirectories) return true;
        var probe = await _fs.ListAsync(dirPath);
        return probe.Any(_filter.Shows);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
