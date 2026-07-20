// SPDX-License-Identifier: GPL-3.0-or-later
namespace Collective.Platform.Controls;

/// <summary>A directory shown in the browser: its display name and full path.</summary>
public sealed record FolderEntry(string Name, string FullPath);

/// <summary>
/// Pure navigation logic for <see cref="FolderBrowserDialog"/> — no UI types, so it is unit-testable
/// without an Avalonia application. Models a current directory and lists its immediate subdirectories;
/// when <see cref="CurrentPath"/> is null it is at the "computer" view that lists the drive roots.
///
/// This exists because the Win32 native folder picker that Avalonia wraps deadlocks or crashes on
/// some machines (an STA/COM apartment issue), so the Collective heads navigate the filesystem
/// themselves through this model instead.
/// </summary>
public sealed class FolderBrowserModel
{
    /// <summary>The directory currently shown, or null for the drive-roots ("computer") view.</summary>
    public string? CurrentPath { get; private set; }

    public FolderBrowserModel(string? startPath = null)
    {
        // Fall back to the user profile, then to the roots view, if the start path is missing.
        if (!TryNavigate(startPath) &&
            !TryNavigate(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)))
        {
            CurrentPath = null;
        }
    }

    /// <summary>True when <see cref="GoUp"/> would change the view (i.e. not already at the roots).</summary>
    public bool CanGoUp => CurrentPath is not null;

    /// <summary>
    /// The entries to display: the subdirectories of <see cref="CurrentPath"/>, or the drive roots
    /// when at the computer view. Inaccessible directories yield an empty list rather than throwing.
    /// </summary>
    public IReadOnlyList<FolderEntry> ListEntries()
    {
        if (CurrentPath is null)
        {
            var roots = new List<FolderEntry>();
            foreach (string drive in SafeGetLogicalDrives())
                roots.Add(new FolderEntry(drive, drive));
            return roots;
        }

        string[] dirs;
        try { dirs = Directory.GetDirectories(CurrentPath); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return []; }

        Array.Sort(dirs, StringComparer.OrdinalIgnoreCase);
        var entries = new List<FolderEntry>(dirs.Length);
        foreach (string d in dirs)
        {
            string name = Path.GetFileName(d);
            entries.Add(new FolderEntry(string.IsNullOrEmpty(name) ? d : name, d));
        }
        return entries;
    }

    /// <summary>
    /// Move to <paramref name="path"/> if it exists; null moves to the roots view. Returns false
    /// (leaving the current view unchanged) when the path does not exist or cannot be read.
    /// </summary>
    public bool TryNavigate(string? path)
    {
        if (path is null)
        {
            CurrentPath = null;
            return true;
        }

        try
        {
            if (!Directory.Exists(path)) return false;
            CurrentPath = Path.GetFullPath(path);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// Go to the parent directory; at a drive root this drops to the roots view. No-op at the roots.
    /// </summary>
    public void GoUp()
    {
        if (CurrentPath is null) return;
        DirectoryInfo? parent;
        try { parent = Directory.GetParent(CurrentPath); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException) { parent = null; }

        if (parent is null) CurrentPath = null;        // was a drive root
        else TryNavigate(parent.FullName);
    }

    private static string[] SafeGetLogicalDrives()
    {
        try { return Directory.GetLogicalDrives(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return []; }
    }
}
