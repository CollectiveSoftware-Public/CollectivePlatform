// SPDX-License-Identifier: GPL-3.0-or-later
namespace Collective.Platform.Testing;

/// <summary>A unique temp directory created on construction and best-effort deleted (recursively)
/// on dispose — the standard fixture for tests that need a real disk tree.</summary>
public sealed class TempDirectory : IDisposable
{
    public string Path { get; } =
        System.IO.Path.Combine(System.IO.Path.GetTempPath(), "collective-test-" + Guid.NewGuid().ToString("N"));

    public TempDirectory() => Directory.CreateDirectory(Path);

    /// <summary>Absolute path for <paramref name="relativePath"/> under the directory ('/' or '\'
    /// separated), with parent directories created so the caller can write to it immediately.</summary>
    public string File(string relativePath)
    {
        string full = System.IO.Path.Combine(Path, relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
        return full;
    }

    public void Dispose()
    {
        try { Directory.Delete(Path, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
