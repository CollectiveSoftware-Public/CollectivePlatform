// SPDX-License-Identifier: GPL-3.0-or-later
namespace Collective.Update;

public sealed record UpdateInfo(string Version, ManifestArtifact Artifact, string NotesUrl);

/// <summary>A verified artifact waiting on disk. <paramref name="Sha256"/> is the hash from the SIGNED
/// manifest that the bytes matched when they were staged; it is carried here (rather than discarded at
/// staging time) so the applier can confirm the same bytes are still there at the moment of the swap.</summary>
public sealed record StagedUpdate(string FilePath, string Version, string Sha256);

public abstract record UpdateCheck
{
    public sealed record UpToDate : UpdateCheck;
    public sealed record Available(UpdateInfo Info) : UpdateCheck;
    public sealed record Failed(string Reason) : UpdateCheck;
}

// success does not return (process restarts). Tampered is distinct from Failed on purpose: Failed means
// the swap did not take, Tampered means the staged bytes were not the ones that were verified — the same
// user-visible outcome, but the only one that warrants suspicion rather than a retry.
public enum ApplyOutcome { NotWritable, Failed, Tampered }
