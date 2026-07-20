// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia.Media;
using Collective.Platform.Abstractions;

namespace Collective.Platform.Controls;

/// <summary>
/// Vector glyphs + type tints for explorer rows — no external image assets, so the control stays
/// self-contained. A folder gets the folder glyph in amber; a file gets a page glyph tinted by its
/// extension (matching the design mockup: cs violet, json/config, md blue, images teal, else slate).
/// </summary>
public static class FileIconFactory
{
    // 16x16 design box. Folder: back tab + body. File: page with a folded corner.
    private static readonly Geometry FolderGeometry = Geometry.Parse("M2,4 H6 L7.5,5.5 H14 V13 H2 Z");
    private static readonly Geometry FileGeometry = Geometry.Parse("M4,2 H10 L13,5 V14 H4 Z M10,2 V5 H13");

    private static readonly IBrush Folder = Solid("#E0A94A");
    private static readonly IBrush Cs = Solid("#B084F0");
    private static readonly IBrush Json = Solid("#E0A94A");
    private static readonly IBrush Md = Solid("#63A6E0");
    private static readonly IBrush Cfg = Solid("#7FB98A");
    private static readonly IBrush Img = Solid("#56B6A4");
    private static readonly IBrush Default = Solid("#7C93B8");

    public static Geometry ForEntry(DirectoryEntry entry) => entry.IsDirectory ? FolderGeometry : FileGeometry;

    public static IBrush TintFor(DirectoryEntry entry)
    {
        if (entry.IsDirectory)
            return Folder;

        return ExtensionOf(entry.Name) switch
        {
            ".cs" => Cs,
            ".json" => Json,
            ".md" or ".markdown" or ".txt" or ".rst" => Md,
            ".yml" or ".yaml" or ".toml" or ".ini" or ".config" or ".xml"
                or ".props" or ".targets" or ".csproj" or ".editorconfig" => Cfg,
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".svg" or ".webp" or ".bmp" or ".ico" => Img,
            _ => Default,
        };
    }

    private static string ExtensionOf(string name)
    {
        int dot = name.LastIndexOf('.');
        return dot <= 0 ? "" : name[dot..].ToLowerInvariant();
    }

    private static IBrush Solid(string hex) => new SolidColorBrush(Color.Parse(hex));
}
