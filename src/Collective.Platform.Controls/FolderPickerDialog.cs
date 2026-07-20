// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Collective.Platform;

namespace Collective.Platform.Controls;

/// <summary>
/// A folder chooser built on the shared <see cref="FileExplorerControl"/> — the breadcrumb + contents
/// list navigation, in a directories-only, read-only mode with a "Select Folder" button. The richer
/// alternative to <see cref="FolderBrowserDialog"/> (which uses a plain sub-folder list); same
/// <c>ShowAsync(owner, startPath)</c> signature, so it is a drop-in replacement. Like the other shared
/// dialogs it avoids Avalonia's Win32 native folder picker (STA/COM deadlock on some machines) and
/// inherits the host theme as a child window.
/// </summary>
public sealed class FolderPickerDialog : DialogWindow
{
    private readonly FileExplorerControl _explorer;

    public FolderPickerDialog() : this(null) { }

    public FolderPickerDialog(string? startPath)
    {
        Title = "Open Folder";
        // A resizable browsing surface, not a height-to-content prompt.
        SizeToContent = SizeToContent.Manual;
        CanResize = true;
        Width = 660;
        Height = 540;
        MinWidth = 440;
        MinHeight = 340;

        _explorer = new FileExplorerControl
        {
            FileSystem = new PhysicalDirectoryFileSystem(),
            Mode = DisplayMode.Compact,
            AllowEdit = false,        // read-only picker: no New/Rename/Delete
            DirectoriesOnly = true,   // folders only — files are noise here
        };

        var ok = new Button { Content = "Select Folder", IsDefault = true };
        var cancel = new Button { Content = "Cancel", IsCancel = true, Margin = new Thickness(8, 0, 0, 0) };
        var buttonBar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(10),
            Children = { ok, cancel },
        };

        var root = new DockPanel();
        DockPanel.SetDock(buttonBar, Dock.Bottom);
        root.Children.Add(buttonBar);
        root.Children.Add(_explorer);
        Content = root;

        ok.Click += (_, _) =>
        {
            // Prefer a highlighted folder (single-click + Select), else the folder being viewed.
            string? chosen = _explorer.SelectedPath ?? _explorer.CurrentPath;
            if (!string.IsNullOrEmpty(chosen))
                Close(chosen);
        };
        cancel.Click += (_, _) => Close(null);

        Opened += async (_, _) =>
        {
            string start = FirstUsable(startPath)
                ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            await _explorer.OpenAsync(start);
        };
    }

    /// <summary>Shows the chooser modally over <paramref name="owner"/>; resolves to the chosen
    /// absolute path, or null if the user cancelled.</summary>
    public static Task<string?> ShowAsync(Window owner, string? startPath = null)
        => new FolderPickerDialog(startPath).ShowDialog<string?>(owner);

    private static string? FirstUsable(string? path)
        => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path) ? path : null;
}
