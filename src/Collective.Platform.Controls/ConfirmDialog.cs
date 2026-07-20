// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia.Controls;
using Avalonia.Media;

namespace Collective.Platform.Controls;

/// <summary>
/// A shared yes/no confirmation dialog for the Collective Software desktop heads. Resolves to
/// <c>true</c> when the user confirms and <c>false</c> when they cancel or close it — giving the suite
/// one consistent confirm/destructive-action prompt instead of each head rolling its own. Inherits the
/// host application's theme (shown as a child window of the owner), like <see cref="FolderBrowserDialog"/>.
/// </summary>
public sealed class ConfirmDialog : DialogWindow
{
    public ConfirmDialog() : this(new ConfirmDialogModel("Confirm", "")) { }

    public ConfirmDialog(ConfirmDialogModel model)
    {
        Title = model.Title;

        var message = new TextBlock
        {
            Text = model.Message,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 400,
        };

        var confirm = model.ConfirmButtonClass == "danger"
            ? DialogButtons.Danger(model.ConfirmText)
            : DialogButtons.Primary(model.ConfirmText);
        var cancel = DialogButtons.Secondary(model.CancelText);

        Content = DialogScaffold.Build(message, confirm, cancel);

        confirm.Click += (_, _) => Close(true);
        cancel.Click += (_, _) => Close(false);
    }

    /// <summary>Shows the prompt modally over <paramref name="owner"/>; resolves to <c>true</c> when
    /// confirmed. Set <paramref name="destructive"/> for irreversible actions (delete/discard).</summary>
    public static Task<bool> ShowAsync(Window owner, string title, string message,
        string? confirmText = null, string? cancelText = null, bool destructive = false)
        => new ConfirmDialog(new ConfirmDialogModel(title, message, confirmText, cancelText, destructive))
            .ShowDialog<bool>(owner);
}
