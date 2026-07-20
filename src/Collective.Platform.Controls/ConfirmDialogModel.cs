// SPDX-License-Identifier: GPL-3.0-or-later
namespace Collective.Platform.Controls;

/// <summary>
/// The pure, UI-free content of a <see cref="ConfirmDialog"/>: the title, message, and button
/// captions (with sensible defaults), plus whether the confirm action is destructive. Kept testable
/// so the dialog's text/normalization rules can be verified without a UI thread.
/// </summary>
public sealed class ConfirmDialogModel
{
    public ConfirmDialogModel(string? title, string? message, string? confirmText = null,
                              string? cancelText = null, bool destructive = false)
    {
        Title = string.IsNullOrWhiteSpace(title) ? "Confirm" : title!.Trim();
        Message = message ?? "";
        ConfirmText = string.IsNullOrWhiteSpace(confirmText) ? "OK" : confirmText!.Trim();
        CancelText = string.IsNullOrWhiteSpace(cancelText) ? "Cancel" : cancelText!.Trim();
        IsDestructive = destructive;
    }

    public string Title { get; }
    public string Message { get; }
    public string ConfirmText { get; }
    public string CancelText { get; }
    public bool IsDestructive { get; }

    /// <summary>Style class for the confirm button. Destructive actions use the suite "danger"
    /// accent; everything else uses the standard "accent" class. (An undefined class is ignored.)</summary>
    public string ConfirmButtonClass => IsDestructive ? "danger" : "accent";
}
