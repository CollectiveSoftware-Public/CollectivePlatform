// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Xunit;

namespace Collective.Platform.Controls.Tests;

public class DialogScaffoldTests
{
    [Fact]
    public void Build_wraps_body_with_padding_and_right_aligned_footer()
    {
        var body = new TextBlock();
        var ok = DialogButtons.Primary("OK");
        var cancel = DialogButtons.Secondary("Cancel");

        var root = Assert.IsType<StackPanel>(DialogScaffold.Build(body, ok, cancel));

        Assert.Equal(new Thickness(20), root.Margin);
        Assert.Equal(16, root.Spacing);
        Assert.Same(body, root.Children[0]);

        var footer = Assert.IsType<StackPanel>(root.Children[1]);
        Assert.Equal(Orientation.Horizontal, footer.Orientation);
        Assert.Equal(HorizontalAlignment.Right, footer.HorizontalAlignment);
        Assert.Equal(8, footer.Spacing);
        Assert.Contains("dialog-footer", footer.Classes);
        Assert.Same(ok, footer.Children[0]);
        Assert.Same(cancel, footer.Children[1]);
    }

    [Fact]
    public void Build_without_buttons_adds_no_footer_row()
    {
        var body = new TextBlock();
        var root = Assert.IsType<StackPanel>(DialogScaffold.Build(body));
        Assert.Single(root.Children);
    }

    [Fact]
    public void Primary_is_the_accent_default_button()
    {
        var b = DialogButtons.Primary("Save");
        Assert.Equal("Save", b.Content);
        Assert.Equal(80, b.MinWidth);
        Assert.True(b.IsDefault);
        Assert.False(b.IsCancel);
        Assert.Contains("accent", b.Classes);
    }

    [Fact]
    public void Secondary_is_the_plain_cancel_button()
    {
        var b = DialogButtons.Secondary("Cancel");
        Assert.Equal(80, b.MinWidth);
        Assert.False(b.IsDefault);
        Assert.True(b.IsCancel);
        Assert.DoesNotContain("accent", b.Classes);
        Assert.DoesNotContain("danger", b.Classes);
    }

    [Fact]
    public void Danger_is_the_destructive_default_button()
    {
        var b = DialogButtons.Danger("Delete");
        Assert.Equal(80, b.MinWidth);
        Assert.True(b.IsDefault);
        Assert.Contains("danger", b.Classes);
    }
}
