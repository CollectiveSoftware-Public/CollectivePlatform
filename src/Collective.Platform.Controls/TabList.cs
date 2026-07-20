// SPDX-License-Identifier: GPL-3.0-or-later
using System.Collections.ObjectModel;

namespace Collective.Platform.Controls;

/// <summary>
/// The tab mechanics every head kept rewriting (Code's WorkspaceViewModel, Wiki's
/// OpenTabsViewModel, SSH/Term's TabControl wiring): an ordered tab list with an active tab,
/// open-or-activate by key, Wiki-style replace-active-slot navigation, and close with a veto
/// hook (dirty prompt), a dispose hook (live sessions) and neighbor selection. Pure — no UI
/// thread — so it is unit-tested directly; <see cref="DocumentTabStrip"/> renders it.
/// </summary>
public sealed class TabList<T>(Func<T, string?> keyOf) where T : class
{
    private T? _active;

    public ObservableCollection<T> Tabs { get; } = new();

    public T? Active
    {
        get => _active;
        set
        {
            if (ReferenceEquals(_active, value))
                return;
            _active = value;
            ActiveChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? ActiveChanged;

    /// <summary>Return false from this hook to cancel a <see cref="CloseAsync"/> (dirty prompt).</summary>
    public Func<T, Task<bool>>? ClosingAsync { get; set; }

    /// <summary>Runs after a tab has been removed (dispose live sessions here).</summary>
    public Action<T>? Closed { get; set; }

    public T OpenOrActivate(string? key, Func<T> create, bool activate = true)
    {
        var existing = FindByKey(key);
        if (existing is not null)
        {
            if (activate)
                Active = existing;
            return existing;
        }

        var tab = create();
        Tabs.Add(tab);
        if (activate)
            Active = tab;
        return tab;
    }

    public T NavigateActive(string? key, Func<T> create)
    {
        var existing = FindByKey(key);
        if (existing is not null)
        {
            Active = existing;
            return existing;
        }

        int index = _active is null ? -1 : Tabs.IndexOf(_active);
        var tab = create();
        if (index < 0)
        {
            Tabs.Add(tab);
        }
        else
        {
            Tabs[index] = tab; // reuse the slot — the replaced tab simply disappears
        }
        Active = tab;
        return tab;
    }

    public async Task<bool> CloseAsync(T tab)
    {
        if (ClosingAsync is { } veto && !await veto(tab).ConfigureAwait(true))
            return false;
        Close(tab);
        return true;
    }

    public void Close(T tab)
    {
        int index = Tabs.IndexOf(tab);
        if (index < 0)
            return;

        Tabs.RemoveAt(index);
        if (ReferenceEquals(_active, tab))
            Active = Tabs.Count == 0 ? null : Tabs[Math.Min(index, Tabs.Count - 1)];
        Closed?.Invoke(tab);
    }

    // ---- scoped bulk closes (the tab right-click menu: Close Others / to the Left / to the Right /
    // All). Each runs its target set through the guarded CloseAsync, so dirty tabs still prompt via
    // ClosingAsync and a vetoed close aborts the rest (matching editors like VS Code). Defined here
    // once so every head sharing TabList gets them; DocumentTabStrip.Bind wires them to the menu. ----

    /// <summary>Closes every tab except <paramref name="keep"/>.</summary>
    public Task CloseOthersAsync(T keep) => CloseManyAsync(Tabs.Where(t => !ReferenceEquals(t, keep)));

    /// <summary>Closes every tab positioned before <paramref name="anchor"/> in the strip.</summary>
    public Task CloseToLeftAsync(T anchor)
    {
        int idx = Tabs.IndexOf(anchor);
        return idx < 0 ? Task.CompletedTask : CloseManyAsync(Tabs.Take(idx));
    }

    /// <summary>Closes every tab positioned after <paramref name="anchor"/> in the strip.</summary>
    public Task CloseToRightAsync(T anchor)
    {
        int idx = Tabs.IndexOf(anchor);
        return idx < 0 ? Task.CompletedTask : CloseManyAsync(Tabs.Skip(idx + 1));
    }

    /// <summary>Closes every tab (guarded — prompts on dirty via <see cref="ClosingAsync"/>).</summary>
    public Task CloseAllAsync() => CloseManyAsync(Tabs);

    // Closes a snapshot of the target set (closing mutates Tabs); a vetoed close stops the rest.
    private async Task CloseManyAsync(IEnumerable<T> targets)
    {
        foreach (var tab in targets.ToArray())
            if (!await CloseAsync(tab).ConfigureAwait(true))
                return;
    }

    private T? FindByKey(string? key)
    {
        if (key is null)
            return null;
        foreach (var tab in Tabs)
            if (string.Equals(keyOf(tab), key, StringComparison.Ordinal))
                return tab;
        return null;
    }
}
