// SPDX-License-Identifier: GPL-3.0-or-later
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;

namespace Collective.Platform.Controls;

/// <summary>Which tabs a scoped close acts on, relative to the right-clicked (anchor) tab.</summary>
public enum TabCloseScope { Others, Left, Right, All }

/// <summary>Payload for <see cref="DocumentTabStrip.TabCloseScopeRequested"/>: the anchor tab + the scope.</summary>
public sealed class TabCloseScopeEventArgs : EventArgs
{
    public TabCloseScopeEventArgs(object item, TabCloseScope scope) { Item = item; Scope = scope; }

    /// <summary>The right-clicked tab the scope is measured against (e.g. "to the left of this one").</summary>
    public object Item { get; }
    public TabCloseScope Scope { get; }
}

/// <summary>
/// The suite's document tab strip (ports Wiki's Border.tab strip + Code's dirty-dot): renders
/// ItemsSource horizontally, highlights SelectedItem, raises events for activate / close
/// (✕ button and middle-click) / new-tab. A per-tab right-click menu adds Close / Close Others /
/// Close to the Left / Close to the Right / Close All (raised via <see cref="TabCloseScopeRequested"/>;
/// items that would do nothing are greyed out). Display is delegate-driven so heads keep their own
/// tab types. Content hosting stays in the head — this is only the strip.
/// </summary>
public sealed class DocumentTabStrip : UserControl
{
    private readonly StackPanel _panel = new() { Orientation = Orientation.Horizontal, Spacing = 2 };
    private IEnumerable? _itemsSource;
    private object? _selectedItem;

    public DocumentTabStrip()
    {
        Content = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            Content = _panel,
        };
    }

    public Func<object, string>? TitleSelector { get; set; }
    public Func<object, bool>? IsDirtySelector { get; set; }
    public Func<object, string?>? ToolTipSelector { get; set; }
    public bool ShowNewTabButton { get; set; }

    public event EventHandler<object>? TabActivated;
    public event EventHandler<object>? TabCloseRequested;
    public event EventHandler? NewTabRequested;

    /// <summary>A scoped bulk close (Others / Left / Right / All) from the tab's right-click menu.</summary>
    public event EventHandler<TabCloseScopeEventArgs>? TabCloseScopeRequested;

    public IEnumerable? ItemsSource
    {
        get => _itemsSource;
        set
        {
            if (_itemsSource is INotifyCollectionChanged old)
                old.CollectionChanged -= OnItemsChanged;
            _itemsSource = value;
            if (_itemsSource is INotifyCollectionChanged next)
                next.CollectionChanged += OnItemsChanged;
            Rebuild();
        }
    }

    public object? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (ReferenceEquals(_selectedItem, value))
                return;
            _selectedItem = value;
            Rebuild(); // small lists — full rebuild is simple and fast enough
        }
    }

    /// <summary>Re-read titles/dirty dots without a collection change (e.g. after a save).</summary>
    public void Refresh() => Rebuild();

    /// <summary>
    /// Wires this strip to a <see cref="TabList{T}"/> in one call — items, title/dirty/tooltip selectors,
    /// activate, close (✕/middle-click), the right-click scoped closes, active→selection sync, and
    /// (when <paramref name="onNewTab"/> is set) the + button. A head adopting document tabs calls this
    /// instead of re-hand-wiring the events, so it inherits every current and future tab behaviour for
    /// free. <paramref name="onActivated"/> runs after a tab is activated (e.g. focus the pane).
    /// </summary>
    public void Bind<T>(
        TabList<T> list,
        Func<T, string> title,
        Func<T, bool>? isDirty = null,
        Func<T, string?>? toolTip = null,
        Action<T>? onActivated = null,
        Action? onNewTab = null) where T : class
    {
        TitleSelector = o => title((T)o);
        IsDirtySelector = isDirty is null ? null : o => isDirty((T)o);
        ToolTipSelector = toolTip is null ? null : o => toolTip((T)o);

        ItemsSource = list.Tabs;
        SelectedItem = list.Active;
        list.ActiveChanged += (_, _) => SelectedItem = list.Active;

        TabActivated += (_, o) => { list.Active = (T)o; onActivated?.Invoke((T)o); };
        TabCloseRequested += async (_, o) => await list.CloseAsync((T)o);
        TabCloseScopeRequested += async (_, e) =>
        {
            var anchor = (T)e.Item;
            switch (e.Scope)
            {
                case TabCloseScope.Others: await list.CloseOthersAsync(anchor); break;
                case TabCloseScope.Left: await list.CloseToLeftAsync(anchor); break;
                case TabCloseScope.Right: await list.CloseToRightAsync(anchor); break;
                case TabCloseScope.All: await list.CloseAllAsync(); break;
            }
        };
        if (onNewTab is not null)
        {
            ShowNewTabButton = true;
            NewTabRequested += (_, _) => onNewTab();
        }
    }

    private void OnItemsChanged(object? s, NotifyCollectionChangedEventArgs e) => Rebuild();

    private void Rebuild()
    {
        _panel.Children.Clear();
        if (_itemsSource is null)
            return;

        // Snapshot to a list so each tab knows its index/count — the context menu greys out
        // Close-Left/Right/Others when they'd be no-ops.
        var items = _itemsSource.Cast<object>().ToList();
        for (int i = 0; i < items.Count; i++)
            _panel.Children.Add(BuildTab(items[i], i, items.Count));

        if (ShowNewTabButton)
        {
            var plus = new Button { Content = "+", Padding = new Thickness(8, 2) };
            plus.Classes.Add("muted");
            plus.Click += (_, _) => NewTabRequested?.Invoke(this, EventArgs.Empty);
            _panel.Children.Add(plus);
        }
    }

    private Border BuildTab(object item, int index, int count)
    {
        bool active = ReferenceEquals(item, _selectedItem);
        string title = TitleSelector?.Invoke(item) ?? item.ToString() ?? "";
        if (IsDirtySelector?.Invoke(item) == true)
            title = "● " + title;

        var text = new TextBlock { Text = title, VerticalAlignment = VerticalAlignment.Center };
        var close = new Button
        {
            Content = "✕", Padding = new Thickness(4, 0), Background = Avalonia.Media.Brushes.Transparent,
            BorderThickness = new Thickness(0),
        };

        var border = new Border
        {
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal, Spacing = 6, Children = { text, close },
            },
            Padding = new Thickness(10, 5),
        };
        border.Classes.Add("tab");
        if (active)
            border.Classes.Add("active");
        if (ToolTipSelector?.Invoke(item) is { } tip)
            ToolTip.SetTip(border, tip);
        border.ContextMenu = BuildTabMenu(item, index, count);

        close.Click += (_, _) => TabCloseRequested?.Invoke(this, item);
        border.PointerPressed += (_, e) =>
        {
            var point = e.GetCurrentPoint(border);
            if (point.Properties.IsMiddleButtonPressed)
            {
                TabCloseRequested?.Invoke(this, item);
                e.Handled = true;
            }
            else if (point.Properties.IsLeftButtonPressed)
            {
                TabActivated?.Invoke(this, item);
            }
        };
        return border;
    }

    // The tab's right-click menu. "Close" reuses TabCloseRequested (same as ✕ / middle-click); the bulk
    // scopes raise TabCloseScopeRequested. Items that would be no-ops for this tab's position are disabled.
    private ContextMenu BuildTabMenu(object item, int index, int count)
    {
        MenuItem Entry(string header, bool enabled, TabCloseScope scope)
        {
            var mi = new MenuItem { Header = header, IsEnabled = enabled };
            mi.Click += (_, _) => TabCloseScopeRequested?.Invoke(this, new TabCloseScopeEventArgs(item, scope));
            return mi;
        }

        var closeThis = new MenuItem { Header = "Close" };
        closeThis.Click += (_, _) => TabCloseRequested?.Invoke(this, item);

        var menu = new ContextMenu();
        menu.Items.Add(closeThis);
        menu.Items.Add(Entry("Close Others", count > 1, TabCloseScope.Others));
        menu.Items.Add(Entry("Close to the Left", index > 0, TabCloseScope.Left));
        menu.Items.Add(Entry("Close to the Right", index < count - 1, TabCloseScope.Right));
        menu.Items.Add(new Separator());
        menu.Items.Add(Entry("Close All", count > 0, TabCloseScope.All));
        return menu;
    }
}
