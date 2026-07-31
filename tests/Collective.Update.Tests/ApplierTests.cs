// SPDX-License-Identifier: GPL-3.0-or-later
using System.Security.Cryptography;
using System.Text;

namespace Collective.Update.Tests;

public class ApplierTests
{
    static string TempDir() { var d = Path.Combine(Path.GetTempPath(), "wa-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(d); return d; }

    /// <summary>The applier re-checks the staged bytes against the manifest hash before swapping, so these
    /// tests carry the real hash of what they wrote — otherwise they would all stop at that gate and stop
    /// exercising the swap/restore behaviour they exist to pin down.</summary>
    static string Sha256Of(string content)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();

    [Fact] public void Apply_swaps_new_over_current_and_launches_it()
    {
        var dir = TempDir();
        var cur = Path.Combine(dir, "App.exe"); File.WriteAllText(cur, "OLD");
        var stagedPath = Path.Combine(dir, "1.1.1-win-x64"); File.WriteAllText(stagedPath, "NEW");
        string? launched = null;
        int? code = null;
        var applier = new FileSwapApplier(p => launched = p, c => code = c);

        applier.Apply(new StagedUpdate(stagedPath, "1.1.1", Sha256Of("NEW")), cur);

        Assert.Equal("NEW", File.ReadAllText(cur));            // current is now the new bytes
        Assert.Equal(cur, launched);                          // relaunched the same path
        Assert.Equal(0, code);                                // exit(0)
        Assert.True(File.Exists(cur + ".old"));               // old kept for next-launch cleanup
    }

    [Fact] public void Apply_refuses_a_staged_file_that_is_not_there()
    {
        var dir = TempDir();
        var cur = Path.Combine(dir, "App.exe"); File.WriteAllText(cur, "OLD");
        // An absent staged file used to be caught by the move throwing, mid-swap, and then undone. It is
        // now refused by the pre-swap hash check instead, so no rename is ever attempted — same guarantee
        // for the user ("nothing happened"), reached without touching the installed binary at all.
        var applier = new FileSwapApplier(_ => { }, _ => { });
        var outcome = applier.Apply(new StagedUpdate(Path.Combine(dir, "missing"), "1.1.1", Sha256Of("NEW")), cur);
        Assert.Equal(ApplyOutcome.Tampered, outcome);
        Assert.Equal("OLD", File.ReadAllText(cur));           // original intact: "nothing happened"
        Assert.False(File.Exists(cur + ".old"));              // no rename to undo
    }

    [Fact] public void IsInstallDirWritable_true_for_a_temp_dir()
    {
        var dir = TempDir();
        var cur = Path.Combine(dir, "App.exe"); File.WriteAllText(cur, "x");
        Assert.True(new FileSwapApplier(_ => { }, _ => { }).IsInstallDirWritable(cur));
    }

    [Fact] public void Apply_returns_Failed_not_throws_when_the_first_rename_fails()
    {
        var dir = TempDir();
        var cur = Path.Combine(dir, "App.exe");                        // deliberately NOT created -> cur->old move throws
        var stagedPath = Path.Combine(dir, "1.1.1-win-x64"); File.WriteAllText(stagedPath, "NEW");
        var applier = new FileSwapApplier(_ => { }, _ => { });
        var outcome = applier.Apply(new StagedUpdate(stagedPath, "1.1.1", Sha256Of("NEW")), cur);
        Assert.Equal(ApplyOutcome.Failed, outcome);                   // caught and reported, not thrown
        Assert.False(File.Exists(cur));                               // nothing conjured
        Assert.False(File.Exists(cur + ".old"));                     // no stray .old
    }

    [Fact] public void Apply_restores_old_and_returns_Failed_when_launch_throws_after_the_swap()
    {
        var dir = TempDir();
        var cur = Path.Combine(dir, "App.exe"); File.WriteAllText(cur, "OLD");
        var stagedPath = Path.Combine(dir, "1.1.1-win-x64"); File.WriteAllText(stagedPath, "NEW");
        int? code = null;
        // launch throws (simulating a failed relaunch); exit must NEVER run on this path.
        var applier = new FileSwapApplier(_ => throw new InvalidOperationException("cannot launch"), c => code = c);

        var outcome = applier.Apply(new StagedUpdate(stagedPath, "1.1.1", Sha256Of("NEW")), cur);

        Assert.Equal(ApplyOutcome.Failed, outcome);            // reported, not thrown, not silent
        Assert.Equal("OLD", File.ReadAllText(cur));            // swap reverted: "nothing happened"
        Assert.False(File.Exists(cur + ".old"));               // .old cleaned up on revert
        Assert.Null(code);                                     // exit(0) was NOT reached after a failed launch
    }
}
