// SPDX-License-Identifier: GPL-3.0-or-later
using Collective.Platform;
using Collective.Platform.Abstractions;

namespace Collective.Platform.Controls;

/// <summary>Which column a <see cref="FolderListing"/> sorts on.</summary>
public enum ListingSort { Name, Size, Modified }

/// <summary>One row as the list will show it. <see cref="IsHidden"/> flags a dot-entry that is
/// present only because the "show hidden" toggle is on (the view greys it); when the toggle is off
/// hidden entries are absent from the result entirely.</summary>
public sealed record ListingRow(DirectoryEntry Entry, bool IsHidden);

/// <summary>
/// Presentation state for one folder's contents: the sort key + direction, the "show hidden" toggle,
/// and a substring quick-filter. Pure — the caller fetches entries via <see cref="IDirectoryFileSystem"/>
/// and hands them to <see cref="Apply"/>, keeping this model synchronous and unit-testable. Folders
/// always sort before files; the chosen key orders within each group.
/// </summary>
public sealed class FolderListing
{
    public ListingSort Sort { get; set; } = ListingSort.Name;
    public bool Descending { get; set; }
    public bool ShowHidden { get; set; }
    public string Filter { get; set; } = "";

    /// <summary>Optional structural skip rules (e.g. hide <c>bin</c>/<c>obj</c>/<c>node_modules</c>/
    /// <c>.git</c> for a code-editor sidebar). Applied before the quick-filter and hidden toggle; null
    /// shows everything (a general file manager). Independent of <see cref="ShowHidden"/>, which only
    /// governs the greying/exclusion of dot-entries the skip filter still allowed through.</summary>
    public FileTreeFilter? SkipFilter { get; set; }

    /// <summary>Show only directories (a folder-picker mode); files are dropped entirely.</summary>
    public bool DirectoriesOnly { get; set; }

    /// <summary>Set the sort key, or flip the direction if it is already the key (a column-header click).</summary>
    public void ToggleSort(ListingSort key)
    {
        if (Sort == key)
        {
            Descending = !Descending;
        }
        else
        {
            Sort = key;
            Descending = false;
        }
    }

    /// <summary>Project <paramref name="entries"/> through the filter, hidden toggle, and sort.</summary>
    public IReadOnlyList<ListingRow> Apply(IReadOnlyList<DirectoryEntry> entries)
    {
        var rows = new List<ListingRow>(entries.Count);
        foreach (var e in entries)
        {
            if (DirectoriesOnly && !e.IsDirectory)
                continue;
            if (SkipFilter is { } skip && !skip.Shows(e))
                continue;
            if (Filter.Length > 0 && !e.Name.Contains(Filter, StringComparison.OrdinalIgnoreCase))
                continue;
            bool hidden = e.Name.StartsWith('.');
            if (hidden && !ShowHidden)
                continue;
            rows.Add(new ListingRow(e, hidden));
        }

        rows.Sort(Compare);
        return rows;
    }

    private int Compare(ListingRow a, ListingRow b)
    {
        // Folders always precede files, regardless of key/direction.
        if (a.Entry.IsDirectory != b.Entry.IsDirectory)
            return a.Entry.IsDirectory ? -1 : 1;

        int c = Sort switch
        {
            ListingSort.Size => a.Entry.Length.CompareTo(b.Entry.Length),
            ListingSort.Modified => a.Entry.Modified.CompareTo(b.Entry.Modified),
            _ => StringComparer.OrdinalIgnoreCase.Compare(a.Entry.Name, b.Entry.Name),
        };
        if (c == 0)
            c = StringComparer.OrdinalIgnoreCase.Compare(a.Entry.Name, b.Entry.Name);
        return Descending ? -c : c;
    }
}
