// SPDX-License-Identifier: GPL-3.0-or-later
namespace Collective.Platform.Controls;

/// <summary>
/// The persisted geometry of a desktop window: restore size, optional position, and whether it was
/// maximized. A reference type (record class) so it satisfies <c>ISettingsStore</c>'s
/// <c>where T : class</c> constraint. <see cref="X"/>/<see cref="Y"/> are null when no position has
/// been saved yet (the window should center).
/// </summary>
public sealed record WindowGeometry
{
    public double Width { get; init; }
    public double Height { get; init; }
    public int? X { get; init; }
    public int? Y { get; init; }
    public bool Maximized { get; init; }
}
