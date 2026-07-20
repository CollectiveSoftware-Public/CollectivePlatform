// SPDX-License-Identifier: GPL-3.0-or-later
namespace Collective.Platform.Controls;

/// <summary>One clickable breadcrumb: its display name and the full path it navigates to.</summary>
public sealed record BreadcrumbSegment(string Name, string FullPath);

/// <summary>
/// Turns a directory path into cumulative, clickable breadcrumb segments. When
/// <c>clampRoot</c> is supplied the trail starts at that root (a sidebar host clamps to the project
/// root so the user cannot walk above it); when it is null the trail walks to the filesystem root
/// (the standalone app shows the full disk path). Pure and separator-agnostic ('/' and '\').
/// </summary>
public static class BreadcrumbTrail
{
    private static readonly char Sep = Path.DirectorySeparatorChar;

    public static IReadOnlyList<BreadcrumbSegment> Build(string path, string? clampRoot = null)
    {
        var result = new List<BreadcrumbSegment>();
        if (string.IsNullOrWhiteSpace(path))
            return result;

        if (clampRoot is not null && IsWithin(path, clampRoot))
        {
            string root = TrimTrail(Normalize(clampRoot));
            result.Add(new BreadcrumbSegment(LeafOrSelf(root), root));
            AppendBelow(result, root, Normalize(path));
            return result;
        }

        string np = Normalize(path);
        string fsRoot = Path.GetPathRoot(np) is { Length: > 0 } r ? r : np;
        result.Add(new BreadcrumbSegment(fsRoot, fsRoot));
        AppendBelow(result, TrimTrail(fsRoot), np);
        return result;
    }

    /// <summary>True when <paramref name="path"/> is the clamp root itself or a descendant of it —
    /// the guard a clamped host uses to reject navigating above its root.</summary>
    public static bool IsWithin(string path, string clampRoot)
    {
        string p = TrimTrail(Normalize(path));
        string root = TrimTrail(Normalize(clampRoot));
        return p.Equals(root, StringComparison.OrdinalIgnoreCase)
            || p.StartsWith(root + Sep, StringComparison.OrdinalIgnoreCase);
    }

    private static void AppendBelow(List<BreadcrumbSegment> acc, string baseTrimmed, string fullNorm)
    {
        if (fullNorm.Length <= baseTrimmed.Length)
            return;
        string rest = fullNorm[baseTrimmed.Length..].Trim(Sep);
        if (rest.Length == 0)
            return;
        string cur = baseTrimmed;
        foreach (string part in rest.Split(Sep, StringSplitOptions.RemoveEmptyEntries))
        {
            cur = cur.EndsWith(Sep) ? cur + part : cur + Sep + part;
            acc.Add(new BreadcrumbSegment(part, cur));
        }
    }

    private static string Normalize(string p) => p.Replace('/', Sep).Replace('\\', Sep);

    private static string TrimTrail(string p) => p.TrimEnd(Sep);

    private static string LeafOrSelf(string p)
    {
        string name = Path.GetFileName(p);
        return string.IsNullOrEmpty(name) ? p : name;
    }
}
