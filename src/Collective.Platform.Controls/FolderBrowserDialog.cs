// SPDX-License-Identifier: GPL-3.0-or-later
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;

namespace Collective.Platform.Controls;

/// <summary>
/// An in-app folder chooser that navigates the filesystem itself instead of invoking the Win32
/// native folder picker. The native picker (which <c>StorageProvider.OpenFolderPickerAsync</c>
/// wraps) deadlocks or crashes on some Windows machines due to an STA/COM apartment mismatch — a
/// native fault that managed <c>try/catch</c> cannot recover from. This dialog avoids that path.
///
/// Shared across the Collective Software desktop heads via the <c>Collective.Platform.Controls</c>
/// package. Inherits the host application's theme (shown as a child window of the owner).
/// </summary>
public sealed class FolderBrowserDialog : DialogWindow
{
    private readonly FolderBrowserModel _model;
    private readonly ObservableCollection<FolderEntry> _entries = new();
    private readonly TextBox _pathBox;
    private readonly ListBox _list;
    private readonly Button _up;
    private readonly Button _ok;

    public FolderBrowserDialog() : this(null) { }

    public FolderBrowserDialog(string? startPath)
    {
        _model = new FolderBrowserModel(startPath);

        Title = "Open Folder";
        // A resizable browsing surface, not a height-to-content prompt.
        SizeToContent = SizeToContent.Manual;
        CanResize = true;
        Width = 580;
        Height = 480;
        MinWidth = 380;
        MinHeight = 300;

        _pathBox = new TextBox { PlaceholderText = "Type or paste a folder path", VerticalAlignment = VerticalAlignment.Center };
        _up = new Button { Content = "Up", VerticalAlignment = VerticalAlignment.Center };
        var go = new Button { Content = "Go", VerticalAlignment = VerticalAlignment.Center };

        var topBar = new DockPanel { Margin = new Avalonia.Thickness(10, 10, 10, 6) };
        DockPanel.SetDock(_up, Dock.Left);
        DockPanel.SetDock(go, Dock.Right);
        _up.Margin = new Avalonia.Thickness(0, 0, 6, 0);
        go.Margin = new Avalonia.Thickness(6, 0, 0, 0);
        topBar.Children.Add(_up);
        topBar.Children.Add(go);
        topBar.Children.Add(_pathBox); // fills the remaining space

        _list = new ListBox
        {
            Margin = new Avalonia.Thickness(10, 0),
            ItemsSource = _entries,
            // Each row is a folder name. Records are immutable, so recycling is off and we can set
            // the text directly rather than relying on a reflection binding.
            ItemTemplate = new FuncDataTemplate<FolderEntry>(
                (entry, _) => new TextBlock
                {
                    Text = entry is null ? "" : entry.Name,
                    Margin = new Avalonia.Thickness(4, 3),
                },
                supportsRecycling: false),
        };

        _ok = new Button { Content = "Select Folder", IsDefault = true };
        var cancel = new Button { Content = "Cancel", IsCancel = true };
        cancel.Margin = new Avalonia.Thickness(8, 0, 0, 0);

        var buttonBar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Avalonia.Thickness(10),
        };
        buttonBar.Children.Add(_ok);
        buttonBar.Children.Add(cancel);

        var root = new DockPanel();
        DockPanel.SetDock(topBar, Dock.Top);
        DockPanel.SetDock(buttonBar, Dock.Bottom);
        root.Children.Add(topBar);
        root.Children.Add(buttonBar);
        root.Children.Add(_list); // fills the centre
        Content = root;

        _up.Click += (_, _) => { _model.GoUp(); Refresh(); };
        go.Click += (_, _) => NavigateToPathBox();
        _pathBox.KeyDown += OnPathBoxKeyDown;
        _list.DoubleTapped += (_, _) => DescendIntoSelection();
        _ok.Click += (_, _) => Confirm();
        cancel.Click += (_, _) => Close(null);

        Refresh();
    }

    /// <summary>Shows the chooser modally over <paramref name="owner"/>; resolves to the chosen
    /// absolute path, or null if the user cancelled.</summary>
    public static Task<string?> ShowAsync(Window owner, string? startPath = null)
        => new FolderBrowserDialog(startPath).ShowDialog<string?>(owner);

    private void OnPathBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            NavigateToPathBox();
            e.Handled = true;
        }
    }

    private void NavigateToPathBox()
    {
        string? text = _pathBox.Text?.Trim();
        if (!string.IsNullOrEmpty(text)) _model.TryNavigate(text);
        Refresh();
    }

    private void DescendIntoSelection()
    {
        if (_list.SelectedItem is FolderEntry entry && _model.TryNavigate(entry.FullPath))
            Refresh();
    }

    private void Confirm()
    {
        // Prefer a highlighted entry (so a single click + Select picks that folder); otherwise take
        // the directory currently being viewed. Never returns the "This PC" (roots) view.
        string? chosen = _list.SelectedItem is FolderEntry entry ? entry.FullPath : _model.CurrentPath;
        if (!string.IsNullOrEmpty(chosen)) Close(chosen);
    }

    private void Refresh()
    {
        _pathBox.Text = _model.CurrentPath ?? "";
        _entries.Clear();
        foreach (var entry in _model.ListEntries()) _entries.Add(entry);
        _up.IsEnabled = _model.CanGoUp;
        _ok.IsEnabled = _model.CurrentPath is not null;
        Title = _model.CurrentPath is null ? "Open Folder — This PC" : $"Open Folder — {_model.CurrentPath}";
    }
}
