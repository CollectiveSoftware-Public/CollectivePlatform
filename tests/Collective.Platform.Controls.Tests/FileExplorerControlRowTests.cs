// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia.Controls;
using Collective.Platform.Controls;
using Xunit;

namespace Collective.Platform.Controls.Tests;

public class FileExplorerControlRowTests
{
    // A null item can reach the row template during Avalonia's virtualization clear/recycle
    // (ContentPresenter rebuilds on container clear with null content). BuildRow used to
    // dereference row.Entry and throw NullReferenceException, crashing the whole render loop —
    // observed in CollectiveFiles, and in CollectiveWiki via its Open-Vault FolderPickerDialog.
    // The control constructs without a running Avalonia app, and the guard returns before any
    // render-interface-dependent work (FileIconFactory), so this needs no headless session.
    [Fact]
    public void BuildRow_tolerates_a_null_row()
    {
        var explorer = new FileExplorerControl();
        Control built = explorer.BuildRow(null);
        Assert.NotNull(built);
    }
}
