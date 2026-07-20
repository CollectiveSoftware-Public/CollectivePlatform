// SPDX-License-Identifier: GPL-3.0-or-later
namespace Collective.Platform.Controls;

/// <summary>
/// The navigation + options state behind the shared <c>FindBar</c> control. It owns the query, the
/// search options, and the "n of m" position with wrap-around next/previous — the parts every head's
/// find experience shares. The actual matching is the head's job (terminal scrollback, mail list, DB
/// grid, diff, HTTP response): the head reports a match count via <see cref="SetMatches"/> and the
/// model tracks where you are. No UI types, so it is fully unit-testable.
/// </summary>
public sealed class FindBarModel
{
    /// <summary>The text to search for.</summary>
    public string Query { get; set; } = "";

    /// <summary>Case-sensitive matching (read by the head when it computes matches).</summary>
    public bool MatchCase { get; set; }

    /// <summary>Whole-word matching (read by the head).</summary>
    public bool WholeWord { get; set; }

    /// <summary>Treat <see cref="Query"/> as a regular expression (read by the head).</summary>
    public bool UseRegex { get; set; }

    /// <summary>Total matches the head last reported for the current query.</summary>
    public int TotalMatches { get; private set; }

    /// <summary>The 1-based current match, or 0 when there are none.</summary>
    public int CurrentIndex { get; private set; }

    /// <summary>
    /// Records how many matches the head found (e.g. after the query or options changed) and which one
    /// is current. The index is clamped into range; 0 matches resets the position to 0.
    /// </summary>
    public void SetMatches(int total, int current = 1)
    {
        TotalMatches = Math.Max(0, total);
        CurrentIndex = TotalMatches == 0 ? 0 : Math.Clamp(current, 1, TotalMatches);
    }

    /// <summary>Advances to the next match (wrapping past the end), returning the new 1-based index.</summary>
    public int Next()
    {
        if (TotalMatches == 0) return CurrentIndex = 0;
        CurrentIndex = CurrentIndex >= TotalMatches ? 1 : CurrentIndex + 1;
        return CurrentIndex;
    }

    /// <summary>Steps to the previous match (wrapping past the start), returning the new 1-based index.</summary>
    public int Previous()
    {
        if (TotalMatches == 0) return CurrentIndex = 0;
        CurrentIndex = CurrentIndex <= 1 ? TotalMatches : CurrentIndex - 1;
        return CurrentIndex;
    }

    /// <summary>The status label, e.g. <c>"3 of 12"</c>, <c>"No results"</c>, or empty for an empty query.</summary>
    public string StatusText => TotalMatches == 0
        ? (string.IsNullOrEmpty(Query) ? "" : "No results")
        : $"{CurrentIndex} of {TotalMatches}";
}
