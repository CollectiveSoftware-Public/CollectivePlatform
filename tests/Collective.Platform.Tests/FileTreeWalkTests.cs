// SPDX-License-Identifier: GPL-3.0-or-later
using Collective.Platform.Testing;
using Xunit;

namespace Collective.Platform.Tests;

public class FileTreeWalkTests
{
    [Fact]
    public async Task Enumerates_files_recursively_applying_skip_rules()
    {
        var fs = new FakeDirectoryFileSystem();
        fs.AddFile("root/a.cs");
        fs.AddFile("root/sub/b.cs");
        fs.AddFile("root/bin/skipped.dll");
        fs.AddFile("root/.hidden");

        var files = new List<string>();
        await foreach (var e in FileTreeWalk.EnumerateFilesAsync(fs, "root"))
            files.Add(e.FullPath);

        Assert.Equal(2, files.Count);
        Assert.Contains("root/a.cs", files);
        Assert.Contains("root/sub/b.cs", files);
    }

    [Fact]
    public async Task None_filter_includes_everything()
    {
        var fs = new FakeDirectoryFileSystem();
        fs.AddFile("root/bin/x.dll");
        var files = new List<string>();
        await foreach (var e in FileTreeWalk.EnumerateFilesAsync(fs, "root", FileTreeFilter.None))
            files.Add(e.FullPath);
        Assert.Equal(new[] { "root/bin/x.dll" }, files);
    }

    [Fact]
    public async Task Cancellation_stops_enumeration()
    {
        var fs = new FakeDirectoryFileSystem();
        fs.AddFile("root/a.cs");
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in FileTreeWalk.EnumerateFilesAsync(fs, "root", ct: cts.Token)) { }
        });
    }
}
