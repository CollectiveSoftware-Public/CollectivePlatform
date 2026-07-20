// SPDX-License-Identifier: GPL-3.0-or-later
namespace Collective.Platform;

/// <summary>
/// Pure list management for "recent" MRU lists (recent folders/repos/files): promote a
/// just-used item to the front, de-duplicate case-insensitively, and cap the list. UI-free and
/// unit-testable; the head persists the result (see <see cref="RecentItemsStore"/>).
/// </summary>
public static class RecentItems
{
    public const int DefaultMax = 10;

    /// <summary>
    /// Returns a new most-recent-first list with <paramref name="item"/> at the front, any prior
    /// occurrence removed (case-insensitive), and the whole list capped at <paramref name="max"/>.
    /// </summary>
    public static List<string> Add(IEnumerable<string> current, string item, int max = DefaultMax)
    {
        var result = new List<string>(max) { item };
        foreach (string f in current)
        {
            if (result.Count >= max) break;
            bool duplicate = false;
            foreach (string r in result)
                if (string.Equals(r, f, StringComparison.OrdinalIgnoreCase)) { duplicate = true; break; }
            if (!duplicate) result.Add(f);
        }
        return result;
    }

    /// <summary>Returns a new list with any occurrence of <paramref name="item"/> removed (case-insensitive); a no-op if absent.</summary>
    public static List<string> Remove(IEnumerable<string> current, string item)
    {
        var result = new List<string>();
        foreach (string c in current)
            if (!string.Equals(c, item, StringComparison.OrdinalIgnoreCase))
                result.Add(c);
        return result;
    }
}
