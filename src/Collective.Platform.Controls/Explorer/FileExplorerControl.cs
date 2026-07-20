// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Media;
using Collective.Platform;
using Collective.Platform.Abstractions;

namespace Collective.Platform.Controls;

/// <summary>How much chrome the list shows.</summary>
public enum DisplayMode
{
    /// <summary>Narrow sidebar: no column headers, name-only rows (size/modified hidden).</summary>
    Compact,
    /// <summary>Full window: sortable Name/Size/Modified column headers and metadata columns.</summary>
    Details,
}

/// <summary>
/// The suite's shared file explorer: a breadcrumb + contents list over any
/// <see cref="IDirectoryFileSystem"/> (local disk or remote SFTP). Navigation replaces the folder in
/// place (no ever-expanding tree); folders open by double-click, files raise <see cref="EntryActivated"/>.
/// Code-built and delegate-driven — no <c>ICommand</c>/DI, the <see cref="FolderBrowserDialog"/> /
/// <see cref="DocumentTabStrip"/> pattern. Styled entirely through the suite's <c>DynamicResource</c>
/// tokens so it themes with the host. Edit actions (New/Rename/Delete/Copy/Cut/Paste) are enabled by
/// <see cref="AllowEdit"/> and operate on the physical disk via <see cref="FileSystemOperations"/>.
/// </summary>
public sealed class FileExplorerControl : UserControl
{
    private readonly ExplorerHistory _history = new();
    private readonly FolderListing _listing = new();

    private readonly Button _backBtn = NavButton("←", "Back");
    private readonly Button _fwdBtn = NavButton("→", "Forward");
    private readonly Button _upBtn = NavButton("↑", "Up");
    private readonly Button _homeBtn = NavButton("⌂", "Home");
    private readonly Button _newFileBtn = new() { Content = "New File", Padding = new Thickness(8, 4) };
    private readonly Button _newFolderBtn = new() { Content = "New Folder", Padding = new Thickness(8, 4) };
    private readonly Button _hiddenBtn = new() { Content = "Hidden", Padding = new Thickness(8, 4) };
    private readonly StackPanel _breadcrumb = new() { Orientation = Orientation.Horizontal, Spacing = 1, VerticalAlignment = VerticalAlignment.Center };
    private readonly TextBox _filterBox = new() { PlaceholderText = "Filter…", Width = 150 };
    private readonly Grid _header = new() { ColumnDefinitions = new ColumnDefinitions("24,*,72,150"), Margin = new Thickness(8, 2) };
    private readonly ListBox _list = new() { SelectionMode = SelectionMode.Single };

    private IDirectoryFileSystem? _fs;
    private IReadOnlyList<DirectoryEntry> _currentEntries = [];
    private string? _homePath;
    private string? _clipboardPath;
    private bool _clipboardIsCut;
    private bool _allowEdit = true;

    public FileExplorerControl()
    {
        var toolbar = new Border
        {
            Padding = new Thickness(6, 5),
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4,
                Children = { _backBtn, _fwdBtn, _upBtn, _homeBtn, Sep(), _newFileBtn, _newFolderBtn, _hiddenBtn },
            },
        };
        Dyn(toolbar, Border.BackgroundProperty, "Collective.Chrome");

        var crumbScroller = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = _breadcrumb,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var crumbRow = new Border
        {
            Padding = new Thickness(8, 4),
            Child = new DockPanel
            {
                Children =
                {
                    DockRight(_filterBox),
                    crumbScroller,
                },
            },
        };
        Dyn(crumbRow, Border.BackgroundProperty, "Collective.Chrome");

        BuildHeader();

        _list.ItemTemplate = new FuncDataTemplate<ListingRow>((row, _) => BuildRow(row), supportsRecycling: false);
        _list.SelectionChanged += (_, _) => SelectionChanged?.Invoke(this, SelectedPath ?? "");
        _list.KeyDown += OnListKeyDown;

        var root = new DockPanel();
        DockPanel.SetDock(toolbar, Dock.Top);
        DockPanel.SetDock(crumbRow, Dock.Top);
        DockPanel.SetDock(_header, Dock.Top);
        root.Children.Add(toolbar);
        root.Children.Add(crumbRow);
        root.Children.Add(_header);
        root.Children.Add(_list);
        Dyn(root, DockPanel.BackgroundProperty, "Collective.Surface");
        Content = root;

        _backBtn.Click += async (_, _) => await StepAsync(_history.Back());
        _fwdBtn.Click += async (_, _) => await StepAsync(_history.Forward());
        _upBtn.Click += async (_, _) => await GoUpAsync();
        _homeBtn.Click += async (_, _) => { if (_homePath is not null) await OpenAsync(_homePath); };
        _newFileBtn.Click += async (_, _) => await NewEntryAsync(isDirectory: false);
        _newFolderBtn.Click += async (_, _) => await NewEntryAsync(isDirectory: true);
        _hiddenBtn.Click += (_, _) => { _listing.ShowHidden = !_listing.ShowHidden; RenderList(); };
        _filterBox.TextChanged += (_, _) => { _listing.Filter = _filterBox.Text ?? ""; RenderList(); };

        ApplyMode();
    }

    // ===== Configuration =====

    /// <summary>The filesystem to browse. Set once before <see cref="OpenAsync"/>.</summary>
    public IDirectoryFileSystem? FileSystem
    {
        get => _fs;
        set => _fs = value;
    }

    /// <summary>When set, the explorer cannot navigate above this root (a sidebar clamps to the
    /// project root); null lets it walk to the filesystem root (the standalone app).</summary>
    public string? ClampRoot { get; set; }

    /// <summary>Compact (sidebar) vs Details (window). Changing it re-applies the chrome.</summary>
    public DisplayMode Mode
    {
        get; set;
    } = DisplayMode.Details;

    /// <summary>Enables New/Rename/Delete/Copy/Cut/Paste. Requires a physical-disk filesystem. The
    /// toolbar's New File/New Folder buttons hide when this is off (a read-only browser/picker).</summary>
    public bool AllowEdit
    {
        get => _allowEdit;
        set
        {
            _allowEdit = value;
            _newFileBtn.IsVisible = value;
            _newFolderBtn.IsVisible = value;
        }
    }

    /// <summary>Show only directories (a folder-picker mode): files are hidden. Set before
    /// <see cref="OpenAsync"/>.</summary>
    public bool DirectoriesOnly
    {
        get => _listing.DirectoriesOnly;
        set => _listing.DirectoriesOnly = value;
    }

    /// <summary>Show dot/hidden entries (greyed) rather than skipping them. Re-renders when changed.</summary>
    public bool ShowHidden
    {
        get => _listing.ShowHidden;
        set { _listing.ShowHidden = value; RenderList(); }
    }

    /// <summary>Optional structural skip rules (hide <c>bin</c>/<c>obj</c>/<c>node_modules</c>/<c>.git</c>
    /// for a code-editor sidebar); null shows everything. Set before <see cref="OpenAsync"/>.</summary>
    public Collective.Platform.FileTreeFilter? SkipFilter
    {
        get => _listing.SkipFilter;
        set => _listing.SkipFilter = value;
    }

    /// <summary>Where the Home button navigates. Defaults to <see cref="ClampRoot"/>, else the first
    /// opened path.</summary>
    public string? HomePath
    {
        get => _homePath;
        set => _homePath = value;
    }

    public string? SelectedPath => (_list.SelectedItem as ListingRow)?.Entry.FullPath;

    /// <summary>The folder currently shown, or null before the first <see cref="OpenAsync"/>.</summary>
    public string? CurrentPath => _history.Current;

    // ===== Events =====

    /// <summary>A file was activated (double-clicked / Enter). Folders drill in internally.</summary>
    public event EventHandler<string>? EntryActivated;
    /// <summary>The shown folder changed (navigation). Argument is the new path.</summary>
    public event EventHandler<string>? PathChanged;
    /// <summary>The user asked to open a file beside the current view (context menu).</summary>
    public event EventHandler<string>? OpenToSideRequested;
    /// <summary>The user asked to reveal an entry in the OS file manager (host wires the shell).</summary>
    public event EventHandler<string>? RevealRequested;
    /// <summary>The selected entry changed. Argument is its full path (empty if none).</summary>
    public event EventHandler<string>? SelectionChanged;

    // ===== Navigation =====

    /// <summary>Navigate to <paramref name="path"/> and render it (also the initial load).</summary>
    public async Task OpenAsync(string path)
    {
        if (_fs is null)
            return;
        if (ClampRoot is { } cr && !BreadcrumbTrail.IsWithin(path, cr))
            return;
        _homePath ??= ClampRoot ?? path;
        _history.Navigate(path);
        await LoadCurrentAsync();
        PathChanged?.Invoke(this, path);
    }

    /// <summary>Re-read the current folder (after an external change).</summary>
    public Task RefreshAsync() => LoadCurrentAsync();

    private async Task LoadCurrentAsync()
    {
        string? p = _history.Current;
        _currentEntries = p is null || _fs is null ? [] : await _fs.ListAsync(p);
        RenderBreadcrumb();
        RenderList();
        UpdateNavButtons();
    }

    private async Task StepAsync(string? moved)
    {
        if (moved is null)
            return;
        await LoadCurrentAsync();
        PathChanged?.Invoke(this, moved);
    }

    private async Task GoUpAsync()
    {
        if (_history.Current is not { } cur)
            return;
        string? parent = ParentOf(cur);
        if (parent is null)
            return;
        if (ClampRoot is { } cr && !BreadcrumbTrail.IsWithin(parent, cr))
            return;
        await OpenAsync(parent);
    }

    private static string? ParentOf(string path)
    {
        string trimmed = path.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
        string? parent = System.IO.Path.GetDirectoryName(trimmed);
        return string.IsNullOrEmpty(parent) ? null : parent;
    }

    private void UpdateNavButtons()
    {
        _backBtn.IsEnabled = _history.CanBack;
        _fwdBtn.IsEnabled = _history.CanForward;
        _upBtn.IsEnabled = _history.Current is { } c && ParentOf(c) is { } p
            && (ClampRoot is not { } cr || BreadcrumbTrail.IsWithin(p, cr));
    }

    // ===== Rendering =====

    private void RenderBreadcrumb()
    {
        _breadcrumb.Children.Clear();
        if (_history.Current is not { } path)
            return;

        var segs = BreadcrumbTrail.Build(path, ClampRoot);
        for (int i = 0; i < segs.Count; i++)
        {
            var seg = segs[i];
            if (i > 0)
                _breadcrumb.Children.Add(new TextBlock { Text = "›", Opacity = 0.5, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0) });

            var crumb = new Button
            {
                Content = seg.Name,
                Padding = new Thickness(6, 2),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
            };
            bool last = i == segs.Count - 1;
            Dyn(crumb, ForegroundProperty, last ? "Collective.TextStrong" : "Collective.TextMuted");
            string target = seg.FullPath;
            crumb.Click += async (_, _) => await OpenAsync(target);
            _breadcrumb.Children.Add(crumb);

            if (last)
                _breadcrumb.Children.Add(BuildSiblingDropdown(seg.FullPath));
        }
    }

    private Button BuildSiblingDropdown(string currentPath)
    {
        var drop = new Button
        {
            Content = "▾",
            Padding = new Thickness(4, 2),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
        };
        Dyn(drop, ForegroundProperty, "Collective.TextSubtle");
        drop.Click += async (_, _) =>
        {
            string? parent = ParentOf(currentPath);
            if (parent is null || _fs is null)
                return;
            var siblings = await _fs.ListAsync(parent);
            var menu = new MenuFlyout();
            foreach (var e in siblings)
            {
                if (!e.IsDirectory)
                    continue;
                string dest = e.FullPath;
                var item = new MenuItem { Header = e.Name };
                item.Click += async (_, _) => await OpenAsync(dest);
                menu.Items.Add(item);
            }
            menu.ShowAt(drop);
        };
        return drop;
    }

    private void RenderList()
    {
        _list.ItemsSource = _listing.Apply(_currentEntries);
        _hiddenBtn.Opacity = _listing.ShowHidden ? 1.0 : 0.6;
    }

    internal Control BuildRow(ListingRow? row)
    {
        if (row is null) return new Control();   // transient null during virtualization clear/recycle
        bool details = Mode == DisplayMode.Details;
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("24,*,72,150") };

        var icon = new Viewbox
        {
            Width = 15,
            Height = 15,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Child = new Avalonia.Controls.Shapes.Path { Data = FileIconFactory.ForEntry(row.Entry), Fill = FileIconFactory.TintFor(row.Entry) },
        };
        Grid.SetColumn(icon, 0);
        grid.Children.Add(icon);

        var name = new TextBlock
        {
            Text = row.Entry.Name,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Opacity = row.IsHidden ? 0.55 : 1.0,
        };
        Dyn(name, ForegroundProperty, "Collective.Text");
        Grid.SetColumn(name, 1);
        grid.Children.Add(name);

        if (details)
        {
            var size = new TextBlock
            {
                Text = row.Entry.IsDirectory ? "" : HumanSize(row.Entry.Length),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12,
            };
            Dyn(size, ForegroundProperty, "Collective.TextSubtle");
            Grid.SetColumn(size, 2);
            grid.Children.Add(size);

            var modified = new TextBlock
            {
                Text = row.Entry.Modified.LocalDateTime.ToString("yyyy-MM-dd HH:mm"),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12,
                Margin = new Thickness(0, 0, 4, 0),
            };
            Dyn(modified, ForegroundProperty, "Collective.TextSubtle");
            Grid.SetColumn(modified, 3);
            grid.Children.Add(modified);
        }

        var container = new Border
        {
            Padding = new Thickness(4, 3),
            Background = Brushes.Transparent,
            Child = grid,
        };
        container.DoubleTapped += async (_, _) => await ActivateAsync(row.Entry);
        container.ContextFlyout = BuildRowMenu(row.Entry);
        return container;
    }

    private MenuFlyout BuildRowMenu(DirectoryEntry entry)
    {
        var menu = new MenuFlyout();
        AddItem(menu, "Open", async () => await ActivateAsync(entry));
        if (!entry.IsDirectory)
            AddItem(menu, "Open to the Side", () => OpenToSideRequested?.Invoke(this, entry.FullPath));
        menu.Items.Add(new Separator());
        if (AllowEdit)
        {
            AddItem(menu, "Rename", async () => await RenameAsync(entry));
            AddItem(menu, "Delete", async () => await DeleteAsync(entry));
            menu.Items.Add(new Separator());
            AddItem(menu, "Copy", () => SetClipboard(entry.FullPath, cut: false));
            AddItem(menu, "Cut", () => SetClipboard(entry.FullPath, cut: true));
            AddItem(menu, "Paste", async () => await PasteAsync());
            menu.Items.Add(new Separator());
        }
        AddItem(menu, "Copy Path", async () => await CopyPathAsync(entry.FullPath));
        AddItem(menu, "Reveal in File Manager", () => RevealRequested?.Invoke(this, entry.FullPath));
        return menu;
    }

    private async Task ActivateAsync(DirectoryEntry entry)
    {
        if (entry.IsDirectory)
            await OpenAsync(entry.FullPath);
        else
            EntryActivated?.Invoke(this, entry.FullPath);
    }

    // ===== Edit operations =====

    private async void OnListKeyDown(object? sender, KeyEventArgs e)
    {
        if (_list.SelectedItem is not ListingRow row)
        {
            if (e.Key == Key.Back) { await GoUpAsync(); e.Handled = true; }
            return;
        }
        switch (e.Key)
        {
            case Key.Enter: await ActivateAsync(row.Entry); e.Handled = true; break;
            case Key.Back: await GoUpAsync(); e.Handled = true; break;
            case Key.F2 when AllowEdit: await RenameAsync(row.Entry); e.Handled = true; break;
            case Key.Delete when AllowEdit: await DeleteAsync(row.Entry); e.Handled = true; break;
        }
    }

    private async Task NewEntryAsync(bool isDirectory)
    {
        if (!AllowEdit || _history.Current is not { } dir)
            return;
        string? name = await PromptAsync(isDirectory ? "New Folder" : "New File", isDirectory ? "New Folder" : "new-file.txt");
        if (string.IsNullOrWhiteSpace(name))
            return;
        try
        {
            if (isDirectory)
                FileSystemOperations.CreateDirectory(dir, name);
            else
                FileSystemOperations.CreateFile(dir, name);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException) { }
        await LoadCurrentAsync();
    }

    private async Task RenameAsync(DirectoryEntry entry)
    {
        if (!AllowEdit)
            return;
        string? name = await PromptAsync("Rename", entry.Name);
        if (string.IsNullOrWhiteSpace(name) || name == entry.Name)
            return;
        try { FileSystemOperations.Rename(entry.FullPath, name); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException) { }
        await LoadCurrentAsync();
    }

    private async Task DeleteAsync(DirectoryEntry entry)
    {
        if (!AllowEdit)
            return;
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;
        bool ok = await ConfirmDialog.ShowAsync(owner, "Delete",
            $"Delete “{entry.Name}”? This cannot be undone.", "Delete", "Cancel", destructive: true);
        if (!ok)
            return;
        try { FileSystemOperations.Delete(entry.FullPath); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        await LoadCurrentAsync();
    }

    private void SetClipboard(string path, bool cut)
    {
        _clipboardPath = path;
        _clipboardIsCut = cut;
    }

    private async Task PasteAsync()
    {
        if (!AllowEdit || _clipboardPath is not { } src || _history.Current is not { } dir)
            return;
        try
        {
            if (_clipboardIsCut)
            {
                FileSystemOperations.Move(src, dir);
                _clipboardPath = null;
            }
            else
            {
                FileSystemOperations.Copy(src, dir);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        await LoadCurrentAsync();
    }

    private async Task CopyPathAsync(string path)
    {
        if (TopLevel.GetTopLevel(this)?.Clipboard is { } cb)
            await cb.SetTextAsync(path);
    }

    // ===== Chrome =====

    private void BuildHeader()
    {
        _header.Children.Clear();
        var nameHeader = HeaderButton("Name", ListingSort.Name, 1);
        var sizeHeader = HeaderButton("Size", ListingSort.Size, 2);
        var modHeader = HeaderButton("Modified", ListingSort.Modified, 3);
        _header.Children.Add(nameHeader);
        _header.Children.Add(sizeHeader);
        _header.Children.Add(modHeader);
    }

    private Button HeaderButton(string text, ListingSort key, int column)
    {
        var btn = new Button
        {
            Content = text,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(2, 0),
            HorizontalAlignment = column == 1 ? HorizontalAlignment.Left : HorizontalAlignment.Right,
            FontSize = 12,
        };
        Dyn(btn, ForegroundProperty, "Collective.TextSubtle");
        btn.Click += (_, _) => { _listing.ToggleSort(key); RenderList(); };
        Grid.SetColumn(btn, column);
        return btn;
    }

    private void ApplyMode()
    {
        bool details = Mode == DisplayMode.Details;
        _header.IsVisible = details;
    }

    /// <summary>Re-apply chrome after changing <see cref="Mode"/> post-construction.</summary>
    public void RefreshChrome()
    {
        ApplyMode();
        RenderList();
    }

    // ===== Helpers =====

    private async Task<string?> PromptAsync(string title, string initial)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return null;

        var box = new TextBox { Text = initial, Width = 320 };
        var ok = new Button { Content = "OK", IsDefault = true };
        var cancel = new Button { Content = "Cancel", IsCancel = true, Margin = new Thickness(8, 0, 0, 0) };
        var dialog = new Window
        {
            Title = title,
            Width = 360,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Thickness(18),
                Spacing = 14,
                Children =
                {
                    box,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Children = { ok, cancel },
                    },
                },
            },
        };
        ok.Click += (_, _) => dialog.Close(box.Text);
        cancel.Click += (_, _) => dialog.Close(null);
        box.SelectAll();
        box.Focus();
        return await dialog.ShowDialog<string?>(owner);
    }

    private static Button NavButton(string glyph, string tip)
    {
        var b = new Button { Content = glyph, Padding = new Thickness(8, 4), FontSize = 14 };
        ToolTip.SetTip(b, tip);
        return b;
    }

    private static Control Sep() => new Border
    {
        Width = 1,
        Margin = new Thickness(4, 2),
        Background = new SolidColorBrush(Color.FromArgb(60, 128, 128, 128)),
    };

    private static Control DockRight(Control c)
    {
        DockPanel.SetDock(c, Dock.Right);
        c.Margin = new Thickness(8, 0, 0, 0);
        return c;
    }

    private static void AddItem(MenuFlyout menu, string header, Action action)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => action();
        menu.Items.Add(item);
    }

    private void Dyn(AvaloniaObject target, AvaloniaProperty property, string resourceKey)
        => target.Bind(property, this.GetResourceObservable(resourceKey));

    /// <summary>Compact human-readable byte size (matches the design mockup's meta column).</summary>
    private static string HumanSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double size = bytes;
        int unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }
        return unit == 0 ? $"{bytes} {units[unit]}" : $"{size:0.#} {units[unit]}";
    }
}
