// SPDX-License-Identifier: GPL-3.0-or-later
namespace Collective.Platform.Controls;

/// <summary>
/// Back / forward navigation over path strings — the browser-style history behind
/// <see cref="FileExplorerControl"/>. Pure and UI-free so it is unit-testable. Navigating to a new
/// path pushes the current one onto the back stack and clears the forward stack; re-navigating to the
/// current path is a no-op (ordinal-ignore-case).
/// </summary>
public sealed class ExplorerHistory
{
    private readonly Stack<string> _back = new();
    private readonly Stack<string> _forward = new();

    /// <summary>The path currently shown, or null before the first navigation.</summary>
    public string? Current { get; private set; }

    public bool CanBack => _back.Count > 0;
    public bool CanForward => _forward.Count > 0;

    /// <summary>Go to <paramref name="path"/>, remembering the current path for <see cref="Back"/>.
    /// No-op when it equals the current path.</summary>
    public void Navigate(string path)
    {
        if (Current is not null && string.Equals(Current, path, StringComparison.OrdinalIgnoreCase))
            return;
        if (Current is not null)
            _back.Push(Current);
        _forward.Clear();
        Current = path;
    }

    /// <summary>Step back one entry; returns the new <see cref="Current"/>, or null if unable.</summary>
    public string? Back()
    {
        if (_back.Count == 0)
            return null;
        if (Current is not null)
            _forward.Push(Current);
        Current = _back.Pop();
        return Current;
    }

    /// <summary>Step forward one entry; returns the new <see cref="Current"/>, or null if unable.</summary>
    public string? Forward()
    {
        if (_forward.Count == 0)
            return null;
        if (Current is not null)
            _back.Push(Current);
        Current = _forward.Pop();
        return Current;
    }
}
