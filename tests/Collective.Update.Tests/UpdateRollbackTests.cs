// SPDX-License-Identifier: GPL-3.0-or-later
namespace Collective.Update.Tests;

/// <summary>Signatures prove a release is genuine, not that it is current. Every published manifest and
/// its signature stay downloadable at their release tag forever, so whoever controls the update host — or
/// the network path to it — can replay an OLD but perfectly-signed manifest. Verification passes, and if
/// that old version is merely newer than what the user is running, they get walked backwards into a
/// release whose vulnerabilities are already public.
///
/// The defence is a high-water mark: remember the newest version ever offered, and never offer anything
/// below it, no matter how well signed.</summary>
public class UpdateRollbackTests
{
    [Fact]
    public void A_version_older_than_the_highest_ever_seen_is_not_offered()
    {
        // The user is on 1.0.0 and has previously been offered 1.3.1. An attacker replays 1.1.0: newer
        // than what is installed, so the old rule would offer it.
        Assert.False(UpdatePolicy.ShouldOffer("1.1.0", current: "1.0.0", skipped: null, highestSeen: "1.3.1"));
    }

    [Fact]
    public void The_highest_seen_version_itself_is_still_offered()
    {
        // Declining once must not permanently lock the user out of that release.
        Assert.True(UpdatePolicy.ShouldOffer("1.3.1", current: "1.0.0", skipped: null, highestSeen: "1.3.1"));
    }

    [Fact]
    public void A_genuinely_newer_version_is_still_offered()
    {
        Assert.True(UpdatePolicy.ShouldOffer("1.4.0", current: "1.0.0", skipped: null, highestSeen: "1.3.1"));
    }

    [Fact]
    public void With_no_high_water_mark_recorded_behaviour_is_unchanged()
    {
        // A first run, or an app that has not adopted the mark yet, must keep working.
        Assert.True(UpdatePolicy.ShouldOffer("1.1.0", current: "1.0.0", skipped: null, highestSeen: null));
        Assert.False(UpdatePolicy.ShouldOffer("0.9.0", current: "1.0.0", skipped: null, highestSeen: null));
    }

    [Fact]
    public void Skipping_still_wins_over_the_high_water_mark()
    {
        Assert.False(UpdatePolicy.ShouldOffer("1.3.1", current: "1.0.0", skipped: "1.3.1", highestSeen: "1.3.1"));
    }

    [Fact]
    public void The_mark_advances_to_the_newest_version_seen()
    {
        Assert.Equal("1.3.1", UpdatePolicy.AdvanceHighestSeen("1.3.1", null));
        Assert.Equal("1.3.1", UpdatePolicy.AdvanceHighestSeen("1.1.0", "1.3.1"));   // a replay never lowers it
        Assert.Equal("1.4.0", UpdatePolicy.AdvanceHighestSeen("1.4.0", "1.3.1"));
    }
}
