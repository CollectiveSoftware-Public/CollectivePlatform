// SPDX-License-Identifier: GPL-3.0-or-later
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;

namespace Collective.Platform.Controls;

/// <summary>
/// A shared "About" dialog for the Collective Software desktop heads — one consistent modal instead of
/// each head hand-rolling its own. Inherits the host theme as a child window, like
/// <see cref="ConfirmDialog"/> and <see cref="FolderBrowserDialog"/>.
/// </summary>
public sealed class AboutDialog : DialogWindow
{
    public AboutDialog() : this(new AboutDialogModel("About")) { }

    public AboutDialog(AboutDialogModel model)
    {
        Title = "About " + model.AppName;

        var stack = new StackPanel { Spacing = 8 };

        stack.Children.Add(new TextBlock
        {
            Text = model.AppName,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
        });

        if (model.Version is { } version)
        {
            var v = new TextBlock { Text = "Version " + version };
            v.Classes.Add("muted"); // shared design-system class (ignored if undefined)
            stack.Children.Add(v);
        }

        if (model.Tagline is { } tagline)
            stack.Children.Add(new TextBlock { Text = tagline, TextWrapping = TextWrapping.Wrap });

        foreach (var line in model.Lines)
            stack.Children.Add(new TextBlock { Text = line, TextWrapping = TextWrapping.Wrap });

        var ok = DialogButtons.Primary("OK");
        ok.IsCancel = true;
        ok.Click += (_, _) => Close();

        Content = DialogScaffold.Build(stack, ok);
    }

    /// <summary>Show the About dialog modally over <paramref name="owner"/>.</summary>
    public static Task ShowAsync(Window owner, AboutDialogModel model)
        => new AboutDialog(model).ShowDialog(owner);
}
