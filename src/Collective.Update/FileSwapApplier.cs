// SPDX-License-Identifier: GPL-3.0-or-later
using System.Runtime.InteropServices;

namespace Collective.Update;

/// <summary>Applies an update by an atomic single-file swap (spec §5.4). The launch and process-exit are
/// injected so the swap is unit-testable without actually restarting. Failure mode is always "nothing
/// happened", never "no app": every move — including the very first rename — AND the relaunch are
/// inside the guarded block, and a failure restores the original and returns an outcome rather than
/// throwing (so a relaunch that fails reverts the swap instead of leaving the app silently un-restarted).
/// Only exit() runs outside the guard, after a successful launch, so a started child is never reverted.</summary>
public sealed class FileSwapApplier(Action<string> launch, Action<int> exit) : IUpdateApplier
{
    public bool IsInstallDirWritable(string currentExePath)
    {
        try
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(currentExePath))!;
            var probe = Path.Combine(dir, $".wcw-write-{Guid.NewGuid():N}");
            File.WriteAllText(probe, "");
            File.Delete(probe);
            return true;
        }
        catch { return false; }
    }

    public ApplyOutcome Apply(StagedUpdate staged, string currentExePath)
    {
        var cur = Path.GetFullPath(currentExePath);
        if (!IsInstallDirWritable(cur)) return ApplyOutcome.NotWritable;
        var old = cur + ".old";
        try { if (File.Exists(old)) File.Delete(old); } catch { /* stale .old; best effort */ }

        var swapped = false;
        try
        {
            File.Move(cur, old);                       // running exe can be renamed
            File.Move(staged.FilePath, cur);
            swapped = true;
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                File.SetUnixFileMode(cur, UnixFileModeExecutable());
            launch(cur);                               // INSIDE the guard: a failed relaunch is recoverable, never silent
        }
        catch
        {
            if (swapped)
            {
                // New bytes are at cur, old bytes at old -> move the original back over the new in ONE
                // step. If even this throws, cur still holds a valid (new) exe rather than nothing —
                // no transient window where cur is absent. "Nothing happened", never "no app".
                try { File.Move(old, cur, overwrite: true); } catch { /* cur retains the new exe; .old kept for recovery */ }
            }
            // Pre-swap failure: if the original was moved aside and the new file is not in place, restore it.
            // Holds even if the very first rename threw (old absent -> nothing to undo, cur intact).
            else if (File.Exists(old) && !File.Exists(cur))
            {
                try { File.Move(old, cur); } catch { /* leave .old for next-launch recovery */ }
            }
            return ApplyOutcome.Failed;
        }
        exit(0);                                       // only after a successful launch; a started child is never reverted
        return ApplyOutcome.Failed;                // unreachable when exit() really exits
    }

    private static UnixFileMode UnixFileModeExecutable()
        => UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
         | UnixFileMode.GroupRead | UnixFileMode.GroupExecute
         | UnixFileMode.OtherRead | UnixFileMode.OtherExecute;
}
