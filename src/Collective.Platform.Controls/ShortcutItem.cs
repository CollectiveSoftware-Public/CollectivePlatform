// SPDX-License-Identifier: GPL-3.0-or-later
namespace Collective.Platform.Controls;

/// <summary>One row in the shared shortcuts dialog. Gesture is display text (e.g. "Ctrl+Shift+P").</summary>
public sealed record ShortcutItem(string Gesture, string Description, string? Category = null);
