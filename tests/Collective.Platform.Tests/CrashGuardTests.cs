// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.IO;
using Collective.Platform;
using Xunit;

namespace Collective.Platform.Tests;

public class CrashGuardTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("crashguard-").FullName;

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* best effort cleanup */ }
    }

    [Fact]
    public void Writes_the_product_crash_log_with_the_exception_text()
    {
        CrashGuard.WriteTo([_dir], "collectivetest", new InvalidOperationException("boom-marker"));
        string text = File.ReadAllText(Path.Combine(_dir, "collectivetest-crash.log"));
        Assert.Contains("boom-marker", text);
        Assert.Contains(nameof(InvalidOperationException), text);
    }

    [Fact]
    public void Unwritable_directory_is_swallowed()
    {
        var ex = Record.Exception(() =>
            CrashGuard.WriteTo([Path.Combine(_dir, "does", "not", "exist")], "collectivetest",
                new InvalidOperationException("boom")));
        Assert.Null(ex);
    }

    [Fact]
    public void Run_rethrows_after_logging()
    {
        Assert.Throws<InvalidOperationException>(() =>
            CrashGuard.Run("collectivetest", () => throw new InvalidOperationException("boom")));
    }

    [Fact]
    public void Two_crashes_are_both_retained()
    {
        CrashGuard.WriteTo([_dir], "collectivetest", new InvalidOperationException("first-marker"));
        CrashGuard.WriteTo([_dir], "collectivetest", new InvalidOperationException("second-marker"));
        string text = File.ReadAllText(Path.Combine(_dir, "collectivetest-crash.log"));
        Assert.Contains("first-marker", text);
        Assert.Contains("second-marker", text);
    }

    [Fact]
    public void Entry_includes_an_environment_header()
    {
        CrashGuard.WriteTo([_dir], "collectivetest", new InvalidOperationException("boom"));
        string text = File.ReadAllText(Path.Combine(_dir, "collectivetest-crash.log"));
        Assert.Contains("App:", text);
        Assert.Contains("Runtime:", text);
        Assert.Contains(".NET", text);   // from RuntimeInformation.FrameworkDescription
    }

    [Fact]
    public void Oversized_log_is_trimmed_but_keeps_recent_entries()
    {
        var big = new string('x', 20_000);
        for (int i = 0; i < 40; i++)
            CrashGuard.WriteTo([_dir], "collectivetest", new InvalidOperationException($"marker-{i}-{big}"));

        string path = Path.Combine(_dir, "collectivetest-crash.log");
        long size = new FileInfo(path).Length;
        string text = File.ReadAllText(path);

        // Soft cap: trim runs before each append, so the file may overshoot 512 KB by one entry.
        // The guarantee is it stays bounded — untrimmed this would be ~800 KB (40 × 20 KB).
        Assert.True(size < 600 * 1024, $"log was {size} bytes, expected < 600 KB (trim not applied?)");
        Assert.Contains("marker-39-", text);      // newest kept
        Assert.Contains("marker-38-", text);      // append retains multiple recent entries (not overwrite)
        Assert.DoesNotContain("marker-0-", text); // oldest trimmed away
    }
}
