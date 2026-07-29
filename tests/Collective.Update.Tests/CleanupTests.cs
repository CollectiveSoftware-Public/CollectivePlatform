// SPDX-License-Identifier: GPL-3.0-or-later
namespace Collective.Update.Tests;

public class CleanupTests
{
    private static string TempExePath()
    {
        var dir = Path.Combine(Path.GetTempPath(), "cu-clean-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "App.exe");
    }

    [Fact]
    public async Task DeletesTheOldFileBesideTheExe()
    {
        var exe = TempExePath();
        var old = exe + ".old";
        await File.WriteAllTextAsync(old, "stale");

        await UpdateCleanup.DeleteOldAsync(exe, maxAttempts: 3, retryDelay: TimeSpan.FromMilliseconds(1));

        Assert.False(File.Exists(old));
    }

    [Fact]
    public async Task CompletesImmediatelyWhenThereIsNoOldFile()
    {
        var exe = TempExePath();
        await UpdateCleanup.DeleteOldAsync(exe, maxAttempts: 3, retryDelay: TimeSpan.FromMilliseconds(1));
        Assert.False(File.Exists(exe + ".old"));   // and no throw
    }

    [Fact]
    public async Task RetriesWhileLockedThenSucceeds()
    {
        // Windows-only: POSIX allows deleting an open file, so the lock never bites there.
        if (!OperatingSystem.IsWindows()) return;

        var exe = TempExePath();
        var old = exe + ".old";
        await File.WriteAllTextAsync(old, "stale");

        var gate = File.Open(old, FileMode.Open, FileAccess.Read, FileShare.None);
        var sweep = UpdateCleanup.DeleteOldAsync(exe, maxAttempts: 100, retryDelay: TimeSpan.FromMilliseconds(20));
        await Task.Delay(100);
        gate.Dispose();                            // release mid-sweep

        await sweep;
        Assert.False(File.Exists(old));
    }

    [Fact]
    public async Task GivesUpSilentlyWhenLockedForever()
    {
        if (!OperatingSystem.IsWindows()) return;

        var exe = TempExePath();
        var old = exe + ".old";
        await File.WriteAllTextAsync(old, "stale");

        using var gate = File.Open(old, FileMode.Open, FileAccess.Read, FileShare.None);
        await UpdateCleanup.DeleteOldAsync(exe, maxAttempts: 3, retryDelay: TimeSpan.FromMilliseconds(1));

        Assert.True(File.Exists(old));             // still there — and no exception escaped
    }
}
