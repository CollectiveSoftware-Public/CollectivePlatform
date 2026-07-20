// SPDX-License-Identifier: GPL-3.0-or-later
namespace Collective.Platform;

/// <summary>
/// Physical-disk mutation the file-exploring module lacked: create / rename / delete / copy / move.
/// The read side (<see cref="PhysicalDirectoryFileSystem"/>) is best-effort and swallows errors; this
/// write side instead throws on hard failure so the caller (a UI wired through a confirm dialog) can
/// surface it. Same-directory name collisions are resolved by <see cref="UniquePath"/> ("new",
/// "new (2)", …) rather than overwriting. Cross-volume moves fall back to copy-then-delete, since
/// <see cref="Directory.Move"/> / <see cref="File.Move"/> reject a move across drives.
/// </summary>
public static class FileSystemOperations
{
    /// <summary>Create an empty file named <paramref name="name"/> in <paramref name="dir"/>,
    /// suffixing on collision. Returns the created file's full path.</summary>
    public static string CreateFile(string dir, string name)
    {
        string path = UniquePath(dir, name, isDirectory: false);
        using (File.Create(path)) { }
        return path;
    }

    /// <summary>Create a directory named <paramref name="name"/> in <paramref name="dir"/>,
    /// suffixing on collision. Returns the created directory's full path.</summary>
    public static string CreateDirectory(string dir, string name)
    {
        string path = UniquePath(dir, name, isDirectory: true);
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>Rename a file or directory in place. Returns the new full path.</summary>
    public static string Rename(string path, string newName)
    {
        string parent = Path.GetDirectoryName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            ?? throw new IOException($"Cannot determine parent of '{path}'.");
        string target = Path.Combine(parent, newName);
        if (Directory.Exists(path))
            Directory.Move(path, target);
        else
            File.Move(path, target);
        return target;
    }

    /// <summary>Delete a file, or a directory and everything under it.</summary>
    public static void Delete(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
        else if (File.Exists(path))
            File.Delete(path);
    }

    /// <summary>Copy a file or directory (recursively) into <paramref name="destDir"/>, suffixing
    /// when the destination already holds an entry of that name. Returns the new full path.</summary>
    public static string Copy(string sourcePath, string destDir)
    {
        string name = LeafName(sourcePath);
        bool isDir = Directory.Exists(sourcePath);
        string target = UniquePath(destDir, name, isDir);
        if (isDir)
            CopyDirectory(sourcePath, target);
        else
            File.Copy(sourcePath, target);
        return target;
    }

    /// <summary>Move a file or directory into <paramref name="destDir"/>, suffixing on collision and
    /// falling back to copy-then-delete across volumes. Returns the new full path.</summary>
    public static string Move(string sourcePath, string destDir)
    {
        string name = LeafName(sourcePath);
        bool isDir = Directory.Exists(sourcePath);
        string target = UniquePath(destDir, name, isDir);
        try
        {
            if (isDir)
                Directory.Move(sourcePath, target);
            else
                File.Move(sourcePath, target);
        }
        catch (IOException)
        {
            // Cross-volume move: Directory.Move/File.Move refuse it — copy then delete instead.
            if (isDir)
            {
                CopyDirectory(sourcePath, target);
                Directory.Delete(sourcePath, recursive: true);
            }
            else
            {
                File.Copy(sourcePath, target);
                File.Delete(sourcePath);
            }
        }
        return target;
    }

    /// <summary>The path <paramref name="name"/> would take in <paramref name="dir"/>, made unique
    /// by appending " (2)", " (3)", … (before the extension for files) if it is already taken.</summary>
    public static string UniquePath(string dir, string name, bool isDirectory)
    {
        string candidate = Path.Combine(dir, name);
        if (!Exists(candidate))
            return candidate;

        string stem = isDirectory ? name : Path.GetFileNameWithoutExtension(name);
        string ext = isDirectory ? "" : Path.GetExtension(name);
        for (int i = 2; ; i++)
        {
            candidate = Path.Combine(dir, $"{stem} ({i}){ext}");
            if (!Exists(candidate))
                return candidate;
        }
    }

    private static bool Exists(string path) => File.Exists(path) || Directory.Exists(path);

    private static string LeafName(string path)
    {
        string trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return Path.GetFileName(trimmed);
    }

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (string file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)));
        foreach (string sub in Directory.GetDirectories(source))
            CopyDirectory(sub, Path.Combine(target, Path.GetFileName(sub)));
    }
}
