// SPDX-License-Identifier: GPL-3.0-or-later
namespace Collective.Platform.Controls;

/// <summary>
/// Pure fuzzy filtering + ranking for the command palette. Generalises CollectiveCode's original
/// substring filter to a subsequence match with a small ranking heuristic (contiguous runs and
/// word-start hits score higher, shorter titles win ties), so typing "ff" surfaces "Find in Files".
/// No UI types — fully unit-testable.
/// </summary>
public static class CommandMatcher
{
    /// <summary>
    /// Returns the commands whose <see cref="CollectiveCommand.Title"/> matches <paramref name="query"/>
    /// as a subsequence, best match first. An empty/whitespace query returns every command in the
    /// original (registration) order.
    /// </summary>
    public static IReadOnlyList<CollectiveCommand> Filter(IEnumerable<CollectiveCommand> commands, string? query)
    {
        ArgumentNullException.ThrowIfNull(commands);
        var all = commands.ToList();
        if (string.IsNullOrWhiteSpace(query)) return all;

        string q = query.Trim();
        var scored = new List<(CollectiveCommand cmd, int score, int order)>();
        for (int i = 0; i < all.Count; i++)
        {
            if (Score(all[i].Title, q) is int s)
                scored.Add((all[i], s, i));
        }

        scored.Sort((a, b) => a.score != b.score ? b.score - a.score : a.order - b.order);
        return scored.Select(t => t.cmd).ToList();
    }

    /// <summary>
    /// Scores <paramref name="text"/> against <paramref name="query"/>: higher is better, null means
    /// <paramref name="query"/> is not a subsequence of <paramref name="text"/>. Case-insensitive,
    /// with bonuses for contiguous matches, word-start matches, and exact-case hits.
    /// </summary>
    public static int? Score(string text, string query)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (string.IsNullOrEmpty(query)) return 0;

        int score = 0, from = 0, prev = -2;
        foreach (char qRaw in query)
        {
            char q = char.ToLowerInvariant(qRaw);
            int at = -1;
            for (int j = from; j < text.Length; j++)
            {
                if (char.ToLowerInvariant(text[j]) == q) { at = j; break; }
            }
            if (at < 0) return null;                                    // not a subsequence

            score += 1;                                                 // base per matched char
            if (at == prev + 1) score += 5;                            // contiguous run
            if (at == 0 || IsBoundary(text[at - 1])) score += 8;       // start of a word
            if (text[at] == qRaw) score += 1;                          // exact-case bonus

            prev = at;
            from = at + 1;
        }

        score -= text.Length / 20;                                     // prefer shorter titles on ties
        return score;
    }

    private static bool IsBoundary(char c) => c is ' ' or '-' or '_' or '.' or '/' or ':';
}
