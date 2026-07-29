// SPDX-License-Identifier: GPL-3.0-or-later
namespace Collective.Update;

/// <summary>Best-effort sweep of the "&lt;exe&gt;.old" leftover a <see cref="FileSwapApplier"/> swap leaves
/// beside the executable. Called by the NEW process at startup: the parent still runs from that file
/// for its first moments (Windows holds the lock until it exits), so deletion is retried briefly and
/// then abandoned silently — cleanup must never affect startup, so nothing here ever throws.</summary>
public static class UpdateCleanup
{
    public static async Task DeleteOldAsync(string currentExePath,
        int maxAttempts = 30, TimeSpan? retryDelay = null, CancellationToken ct = default)
    {
        var old = Path.GetFullPath(currentExePath) + ".old";
        var delay = retryDelay ?? TimeSpan.FromMilliseconds(500);   // 30 × 500 ms ≈ 15 s
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                if (!File.Exists(old)) return;
                File.Delete(old);
                return;
            }
            catch
            {
                // Locked (parent still exiting) or otherwise undeletable — wait and retry.
            }
            try { await Task.Delay(delay, ct); } catch (OperationCanceledException) { return; }
        }
    }
}
