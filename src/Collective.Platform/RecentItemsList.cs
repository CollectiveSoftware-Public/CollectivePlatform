// SPDX-License-Identifier: GPL-3.0-or-later
namespace Collective.Platform;

/// <summary>Persistence shape for <see cref="RecentItemsStore"/> (reference type for <c>ISettingsStore</c>).</summary>
public sealed class RecentItemsList
{
    public List<string> Items { get; set; } = new();
}
