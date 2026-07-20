// SPDX-License-Identifier: GPL-3.0-or-later
using Collective.Platform.Controls;
using Xunit;

namespace Collective.Platform.Controls.Tests;

public class ConfirmDialogModelTests
{
    [Fact]
    public void Supplies_default_captions_when_blank()
    {
        var model = new ConfirmDialogModel(title: null, message: null);

        Assert.Equal("Confirm", model.Title);
        Assert.Equal("", model.Message);
        Assert.Equal("OK", model.ConfirmText);
        Assert.Equal("Cancel", model.CancelText);
        Assert.False(model.IsDestructive);
    }

    [Fact]
    public void Keeps_and_trims_supplied_captions()
    {
        var model = new ConfirmDialogModel("  Delete item  ", "Are you sure?", "  Delete  ", "  Keep  ");

        Assert.Equal("Delete item", model.Title);
        Assert.Equal("Are you sure?", model.Message);
        Assert.Equal("Delete", model.ConfirmText);
        Assert.Equal("Keep", model.CancelText);
    }

    [Fact]
    public void Confirm_button_class_reflects_destructiveness()
    {
        Assert.Equal("accent", new ConfirmDialogModel("t", "m").ConfirmButtonClass);
        Assert.Equal("danger", new ConfirmDialogModel("t", "m", destructive: true).ConfirmButtonClass);
    }
}
