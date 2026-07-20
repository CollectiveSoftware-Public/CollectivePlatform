// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia.Controls;

namespace Collective.Platform.Controls;

public enum SaveChangesResult { Save, Discard, Cancel }

/// <summary>
/// The 3-way data-safety prompt ("Save / Don't save / Cancel") for closing modified documents —
/// complements the 2-way <see cref="ConfirmDialog"/>. Save is the default; Esc cancels.
/// </summary>
public sealed class SaveChangesDialog : DialogWindow
{
    private SaveChangesResult _result = SaveChangesResult.Cancel;

    public SaveChangesDialog() : this("Save changes before closing?", "Save") { }

    public SaveChangesDialog(string message, string saveLabel)
    {
        Title = "Unsaved changes";

        var text = new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap };

        var save = DialogButtons.Primary(saveLabel);
        save.Click += (_, _) => { _result = SaveChangesResult.Save; Close(); };

        var discard = new Button { Content = "Don't save", MinWidth = 80 };
        discard.Click += (_, _) => { _result = SaveChangesResult.Discard; Close(); };

        var cancel = DialogButtons.Secondary("Cancel");
        cancel.Click += (_, _) => { _result = SaveChangesResult.Cancel; Close(); };

        Content = DialogScaffold.Build(text, save, discard, cancel);
    }

    public static async Task<SaveChangesResult> ShowAsync(Window owner, string message, string saveLabel = "Save")
    {
        var dialog = new SaveChangesDialog(message, saveLabel);
        await dialog.ShowDialog(owner);
        return dialog._result;
    }
}
