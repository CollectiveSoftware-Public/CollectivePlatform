// SPDX-License-Identifier: GPL-3.0-or-later
using System.Security.Cryptography;
using System.Text;

namespace Collective.Update.Tests;

/// <summary>The staged artifact is verified once, when it is downloaded — and then it sits on disk while
/// the user is asked to save their work, which can take arbitrarily long. Verifying at download time says
/// nothing about the bytes that actually get swapped in later, so the hash has to be re-checked at the
/// moment of the swap: that is the only check whose result is still true when the file is executed.</summary>
public class ApplierIntegrityTests
{
    private static string TempDir()
    {
        var d = Path.Combine(Path.GetTempPath(), "wa-int-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(d);
        return d;
    }

    private static string Sha256Of(string content)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();

    [Fact]
    public void Apply_refuses_a_staged_file_whose_bytes_changed_after_it_was_verified()
    {
        var dir = TempDir();
        var cur = Path.Combine(dir, "App.exe");
        File.WriteAllText(cur, "OLD");
        var stagedPath = Path.Combine(dir, "1.1.1-win-x64");
        File.WriteAllText(stagedPath, "NEW");
        var verifiedHash = Sha256Of("NEW");

        // ...and now something rewrites the staged file between staging and applying.
        File.WriteAllText(stagedPath, "TAMPERED");

        string? launched = null;
        int? code = null;
        var applier = new FileSwapApplier(p => launched = p, c => code = c);

        var outcome = applier.Apply(new StagedUpdate(stagedPath, "1.1.1", verifiedHash), cur);

        Assert.Equal(ApplyOutcome.Tampered, outcome);
        Assert.Equal("OLD", File.ReadAllText(cur));   // the swap never happened
        Assert.Null(launched);                        // and nothing was executed
        Assert.Null(code);
        Assert.False(File.Exists(cur + ".old"));      // no half-finished rename left behind
    }

    [Fact]
    public void Apply_still_swaps_when_the_staged_bytes_match_what_was_verified()
    {
        var dir = TempDir();
        var cur = Path.Combine(dir, "App.exe");
        File.WriteAllText(cur, "OLD");
        var stagedPath = Path.Combine(dir, "1.1.1-win-x64");
        File.WriteAllText(stagedPath, "NEW");

        string? launched = null;
        var applier = new FileSwapApplier(p => launched = p, _ => { });

        applier.Apply(new StagedUpdate(stagedPath, "1.1.1", Sha256Of("NEW")), cur);

        Assert.Equal("NEW", File.ReadAllText(cur));
        Assert.Equal(cur, launched);
    }

    [Fact]
    public void Apply_refuses_when_the_staged_file_has_vanished_rather_than_reporting_success()
    {
        var dir = TempDir();
        var cur = Path.Combine(dir, "App.exe");
        File.WriteAllText(cur, "OLD");

        var applier = new FileSwapApplier(_ => { }, _ => { });

        var outcome = applier.Apply(
            new StagedUpdate(Path.Combine(dir, "missing"), "1.1.1", Sha256Of("NEW")), cur);

        Assert.Equal(ApplyOutcome.Tampered, outcome);
        Assert.Equal("OLD", File.ReadAllText(cur));
    }
}
