// SPDX-License-Identifier: GPL-3.0-or-later
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Collective.Platform.Controls.Tests;

/// <summary>
/// Guards the shared design system (<c>Themes/Tokens.axaml</c> + <c>Themes/Controls.axaml</c>) that
/// every Collective desktop head consumes. These are structural, runtime-free checks (the package
/// build already validates XAML syntax); they catch the two regressions that would silently break
/// all five apps at once:
/// <list type="bullet">
///   <item>a colour token defined in one theme variant but missing from the other (asymmetry), so
///   the app looks wrong in only Dark or only Light;</item>
///   <item>the style pack referencing a <c>Collective.*</c> token that no longer exists (a typo),
///   so controls fall back to unthemed defaults.</item>
/// </list>
/// </summary>
public sealed class ThemeResourceTests
{
    private static readonly XNamespace X = "http://schemas.microsoft.com/winfx/2006/xaml";

    private static string ThemesDir()
    {
        // Walk up from the test output dir to the package's Themes folder.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (; dir is not null; dir = dir.Parent)
        {
            string candidate = Path.Combine(dir.FullName, "src", "Collective.Platform.Controls", "Themes");
            if (Directory.Exists(candidate))
                return candidate;
        }
        throw new DirectoryNotFoundException("Could not locate Collective.Platform.Controls/Themes from " + AppContext.BaseDirectory);
    }

    private static XDocument Load(string file) => XDocument.Load(Path.Combine(ThemesDir(), file));

    /// <summary>Keys declared directly on the root dictionary (theme-invariant tokens).</summary>
    private static HashSet<string> TopLevelKeys(XDocument doc)
    {
        var root = doc.Root!;
        return root.Elements()
            .Where(e => e.Name.LocalName != "ResourceDictionary.ThemeDictionaries")
            .Select(e => (string?)e.Attribute(X + "Key"))
            .Where(k => k is not null)
            .Select(k => k!)
            .ToHashSet();
    }

    /// <summary>Keys declared inside the named theme variant ("Dark" / "Light").</summary>
    private static HashSet<string> VariantKeys(XDocument doc, string variant)
    {
        var themeDicts = doc.Root!.Elements()
            .First(e => e.Name.LocalName == "ResourceDictionary.ThemeDictionaries");
        var variantDict = themeDicts.Elements()
            .First(e => (string?)e.Attribute(X + "Key") == variant);
        return variantDict.Elements()
            .Select(e => (string?)e.Attribute(X + "Key"))
            .Where(k => k is not null)
            .Select(k => k!)
            .ToHashSet();
    }

    [Fact]
    public void Dark_and_light_define_exactly_the_same_colour_tokens()
    {
        var doc = Load("Tokens.axaml");
        var dark = VariantKeys(doc, "Dark");
        var light = VariantKeys(doc, "Light");

        Assert.True(dark.SetEquals(light),
            "Dark/Light token sets diverge. Only in Dark: [" +
            string.Join(", ", dark.Except(light).OrderBy(s => s)) + "]; only in Light: [" +
            string.Join(", ", light.Except(dark).OrderBy(s => s)) + "].");
        Assert.NotEmpty(dark);
    }

    [Fact]
    public void Required_tokens_are_present()
    {
        var doc = Load("Tokens.axaml");
        var top = TopLevelKeys(doc);
        var dark = VariantKeys(doc, "Dark");

        // Theme-invariant essentials (corner radius, accent, fonts, spacing).
        foreach (var key in new[]
                 {
                     "Collective.CornerRadius", "ControlCornerRadius", "Collective.Accent",
                     "Collective.OnAccent", "Collective.FontFamilyMono", "Collective.SpaceM",
                 })
            Assert.Contains(key, top);

        // Core colour tokens the heads rely on (in both variants, asserted symmetric above).
        foreach (var key in new[]
                 {
                     "Collective.Background", "Collective.Surface", "Collective.SurfaceAlt",
                     "Collective.Chrome", "Collective.Border", "Collective.Text",
                     "Collective.TextMuted", "Collective.Success", "Collective.Danger",
                 })
            Assert.Contains(key, dark);
    }

    [Fact]
    public void Style_pack_only_references_tokens_that_exist()
    {
        var tokens = Load("Tokens.axaml");
        var defined = TopLevelKeys(tokens);
        defined.UnionWith(VariantKeys(tokens, "Dark")); // Dark==Light asserted elsewhere

        string controls = File.ReadAllText(Path.Combine(ThemesDir(), "Controls.axaml"));
        var referenced = Regex.Matches(controls, @"\{(?:Dynamic|Static)Resource\s+(Collective\.[A-Za-z.]+)\}")
            .Select(m => m.Groups[1].Value)
            .Distinct();

        foreach (var key in referenced)
            Assert.True(defined.Contains(key),
                $"Controls.axaml references '{key}', which is not defined in Tokens.axaml.");
    }
}
