// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Xunit;

namespace Collective.Platform.Controls.Headless.Tests;

public class DialogWindowTests
{
    [AvaloniaFact]
    public void Defaults_are_fixed_size_no_taskbar_width_440()
    {
        var dialog = new DialogWindow();
        Assert.Equal(440, dialog.Width);
        Assert.Equal(320, dialog.MinWidth);
        Assert.Equal(SizeToContent.Height, dialog.SizeToContent);
        Assert.False(dialog.CanResize);
        Assert.False(dialog.ShowInTaskbar);
        Assert.Equal(WindowStartupLocation.CenterOwner, dialog.WindowStartupLocation);
    }

    [AvaloniaFact]
    public void Escape_closes_the_dialog()
    {
        var dialog = new DialogWindow();
        bool closed = false;
        dialog.Closed += (_, _) => closed = true;
        dialog.Show();
        dialog.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        Assert.True(closed);
    }

    [AvaloniaFact]
    public void Escape_already_handled_by_content_does_not_close()
    {
        var dialog = new DialogWindow();
        var consumer = new TextBox();
        consumer.KeyDown += (_, e) => e.Handled = true; // a find-bar-style Escape consumer
        dialog.Content = consumer;
        bool closed = false;
        dialog.Closed += (_, _) => closed = true;
        dialog.Show();
        consumer.Focus();
        dialog.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        Assert.False(closed);
        dialog.Close();
    }

    [AvaloniaFact]
    public void Defaults_are_overridable_per_window()
    {
        var dialog = new DialogWindow
        {
            Width = 580,
            SizeToContent = SizeToContent.Manual,
            CanResize = true,
        };
        Assert.Equal(580, dialog.Width);
        Assert.Equal(SizeToContent.Manual, dialog.SizeToContent);
        Assert.True(dialog.CanResize);
    }
}
