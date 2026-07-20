// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;

[assembly: AvaloniaTestApplication(typeof(Collective.Platform.Controls.Headless.Tests.HeadlessAppBuilder))]

namespace Collective.Platform.Controls.Headless.Tests;

/// <summary>A minimal Application that merges the suite tokens, booted against the headless
/// Avalonia platform so tests construct real windows without a display.</summary>
public sealed class TestApp : Application
{
    public override void Initialize()
    {
        Resources.MergedDictionaries.Add(
            new ResourceInclude(new Uri("avares://Collective.Platform.Controls/"))
            {
                Source = new Uri("avares://Collective.Platform.Controls/Themes/Tokens.axaml"),
            });
    }
}

public static class HeadlessAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<TestApp>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
