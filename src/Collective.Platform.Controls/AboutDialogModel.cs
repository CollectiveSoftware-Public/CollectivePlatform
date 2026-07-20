// SPDX-License-Identifier: GPL-3.0-or-later
using System.Collections.Generic;
using System.Linq;

namespace Collective.Platform.Controls;

/// <summary>
/// The pure, UI-free content of an <see cref="AboutDialog"/>: the app name, an optional version and
/// tagline, and optional extra lines. Normalizes blank input so the dialog always has a title.
/// </summary>
public sealed class AboutDialogModel
{
    public AboutDialogModel(string? appName, string? version = null, string? tagline = null,
                            IReadOnlyList<string>? lines = null)
    {
        AppName = string.IsNullOrWhiteSpace(appName) ? "About" : appName!.Trim();
        Version = string.IsNullOrWhiteSpace(version) ? null : version!.Trim();
        Tagline = string.IsNullOrWhiteSpace(tagline) ? null : tagline!.Trim();
        Lines = lines?.Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Trim()).ToList()
                ?? new List<string>();
    }

    public string AppName { get; }
    public string? Version { get; }
    public string? Tagline { get; }
    public IReadOnlyList<string> Lines { get; }
}
