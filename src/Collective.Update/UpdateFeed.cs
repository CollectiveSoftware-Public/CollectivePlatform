// SPDX-License-Identifier: GPL-3.0-or-later
namespace Collective.Update;

/// <summary>Everything the service needs, supplied by the app — no app-specific constant is hardcoded
/// in the module (key-agnostic, spec §5.1). TrustedKeys is a list for rotation (D10).</summary>
public sealed record UpdateFeed(
    Uri ManifestUrl,
    Uri SignatureUrl,
    IReadOnlyList<string> TrustedKeys,
    string CurrentVersion,
    string Rid,
    string? SkippedVersion,

    /// <summary>The newest version this install has ever been offered, persisted by the app alongside
    /// <see cref="SkippedVersion"/>. Supplying it enables anti-rollback: see
    /// <see cref="UpdatePolicy.ShouldOffer"/>. Advance it with
    /// <see cref="UpdatePolicy.AdvanceHighestSeen"/> whenever a check returns
    /// <see cref="UpdateCheck.Available"/>. Left null, checks behave exactly as they did before the floor
    /// existed — so adopting it is a one-line change in the host, not a migration.</summary>
    string? HighestSeenVersion = null);
