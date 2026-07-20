// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia.Controls;
using Avalonia.Input;

namespace Collective.Platform.Controls;

/// <summary>
/// The modal-prompt flavor of <see cref="UtilityWindow"/> — input/confirm/message-style dialogs.
/// Height follows content, the window is fixed-size with no taskbar entry, and Escape closes it
/// even when no <c>IsCancel</c> button exists. Every default is overridable per window (set the
/// property after the base ctor runs — object initializers and axaml attributes both qualify).
/// </summary>
public class DialogWindow : UtilityWindow
{
    public DialogWindow()
    {
        Width = 440;
        MinWidth = 320;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        ShowInTaskbar = false;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        // Only act on unhandled events: IsCancel buttons, find bars, and palettes that consume
        // Escape keep winning. Close() with no result keeps ShowDialog<T> cancel semantics
        // (default(T) — false/null), same as every existing Cancel path.
        if (!e.Handled && e.Key == Key.Escape)
        {
            e.Handled = true;
            Close();
        }
    }
}
