// SPDX-License-Identifier: GPL-3.0-or-later
using Collective.Platform.Testing;
using Xunit;

namespace Collective.Platform.Tests;

public class FileSystemOperationsTests
{
    [Fact]
    public void CreateFile_creates_and_suffixes_on_collision()
    {
        using var dir = new TempDirectory();

        string first = FileSystemOperations.CreateFile(dir.Path, "note.txt");
        string second = FileSystemOperations.CreateFile(dir.Path, "note.txt");

        Assert.True(File.Exists(first));
        Assert.True(File.Exists(second));
        Assert.Equal("note.txt", Path.GetFileName(first));
        Assert.Equal("note (2).txt", Path.GetFileName(second));
    }

    [Fact]
    public void CreateDirectory_creates_and_suffixes_on_collision()
    {
        using var dir = new TempDirectory();

        string first = FileSystemOperations.CreateDirectory(dir.Path, "src");
        string second = FileSystemOperations.CreateDirectory(dir.Path, "src");

        Assert.True(Directory.Exists(first));
        Assert.True(Directory.Exists(second));
        Assert.Equal("src", Path.GetFileName(first));
        Assert.Equal("src (2)", Path.GetFileName(second));
    }

    [Fact]
    public void Rename_file_moves_to_new_name()
    {
        using var dir = new TempDirectory();
        string path = dir.File("old.txt");
        File.WriteAllText(path, "x");

        string renamed = FileSystemOperations.Rename(path, "new.txt");

        Assert.False(File.Exists(path));
        Assert.True(File.Exists(renamed));
        Assert.Equal("new.txt", Path.GetFileName(renamed));
        Assert.Equal("x", File.ReadAllText(renamed));
    }

    [Fact]
    public void Rename_directory_moves_to_new_name()
    {
        using var dir = new TempDirectory();
        string sub = FileSystemOperations.CreateDirectory(dir.Path, "old");
        File.WriteAllText(Path.Combine(sub, "f.txt"), "y");

        string renamed = FileSystemOperations.Rename(sub, "new");

        Assert.False(Directory.Exists(sub));
        Assert.True(Directory.Exists(renamed));
        Assert.True(File.Exists(Path.Combine(renamed, "f.txt")));
    }

    [Fact]
    public void Delete_removes_file()
    {
        using var dir = new TempDirectory();
        string path = dir.File("gone.txt");
        File.WriteAllText(path, "z");

        FileSystemOperations.Delete(path);

        Assert.False(File.Exists(path));
    }

    [Fact]
    public void Delete_removes_directory_recursively()
    {
        using var dir = new TempDirectory();
        string sub = FileSystemOperations.CreateDirectory(dir.Path, "tree");
        File.WriteAllText(Path.Combine(sub, "a.txt"), "a");
        string nested = FileSystemOperations.CreateDirectory(sub, "deep");
        File.WriteAllText(Path.Combine(nested, "b.txt"), "b");

        FileSystemOperations.Delete(sub);

        Assert.False(Directory.Exists(sub));
    }

    [Fact]
    public void Copy_file_into_directory()
    {
        using var dir = new TempDirectory();
        string src = dir.File("data.bin");
        File.WriteAllText(src, "payload");
        string dest = FileSystemOperations.CreateDirectory(dir.Path, "dest");

        string copied = FileSystemOperations.Copy(src, dest);

        Assert.True(File.Exists(src));
        Assert.True(File.Exists(copied));
        Assert.Equal("payload", File.ReadAllText(copied));
        Assert.Equal(dest, Path.GetDirectoryName(copied));
    }

    [Fact]
    public void Copy_directory_recursively()
    {
        using var dir = new TempDirectory();
        string src = FileSystemOperations.CreateDirectory(dir.Path, "proj");
        File.WriteAllText(Path.Combine(src, "top.txt"), "t");
        string srcSub = FileSystemOperations.CreateDirectory(src, "inner");
        File.WriteAllText(Path.Combine(srcSub, "leaf.txt"), "l");
        string dest = FileSystemOperations.CreateDirectory(dir.Path, "out");

        string copied = FileSystemOperations.Copy(src, dest);

        Assert.True(File.Exists(Path.Combine(copied, "top.txt")));
        Assert.True(File.Exists(Path.Combine(copied, "inner", "leaf.txt")));
        Assert.True(Directory.Exists(src)); // original untouched
    }

    [Fact]
    public void Copy_into_same_directory_suffixes()
    {
        using var dir = new TempDirectory();
        string src = dir.File("clone.txt");
        File.WriteAllText(src, "c");

        string copied = FileSystemOperations.Copy(src, dir.Path);

        Assert.Equal("clone (2).txt", Path.GetFileName(copied));
    }

    [Fact]
    public void Move_file_into_directory()
    {
        using var dir = new TempDirectory();
        string src = dir.File("moving.txt");
        File.WriteAllText(src, "m");
        string dest = FileSystemOperations.CreateDirectory(dir.Path, "target");

        string moved = FileSystemOperations.Move(src, dest);

        Assert.False(File.Exists(src));
        Assert.True(File.Exists(moved));
        Assert.Equal(dest, Path.GetDirectoryName(moved));
        Assert.Equal("m", File.ReadAllText(moved));
    }

    [Fact]
    public void UniquePath_returns_input_when_free()
    {
        using var dir = new TempDirectory();
        string p = FileSystemOperations.UniquePath(dir.Path, "free.txt", isDirectory: false);
        Assert.Equal(Path.Combine(dir.Path, "free.txt"), p);
    }
}
