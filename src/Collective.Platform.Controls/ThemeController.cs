// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;

namespace Collective.Platform.Controls;

/// <summary>
/// Shared theme-mode helpers: maps the <c>"System"</c>/<c>"Light"</c>/<c>"Dark"</c> strings every head
/// persists to an Avalonia <see cref="ThemeVariant"/>, applies it app-wide, and wires a self-drawn
/// surface to re-theme when the resolved OS variant changes (System-mode live-follow).
/// </summary>
public static class ThemeController
{
    /// <summary>Map a persisted theme-mode string to a variant. Unknown/empty → System (Default).</summary>
    public static ThemeVariant For(string? mode) => mode switch
    {
        "Light" => ThemeVariant.Light,
        "Dark" => ThemeVariant.Dark,
        _ => ThemeVariant.Default, // "System" and anything unrecognized follow the OS
    };

    /// <summary>Apply a theme mode to the whole application.</summary>
    public static void Apply(string? mode)
    {
        if (Application.Current is { } app)
            app.RequestedThemeVariant = For(mode);
    }

    /// <summary>
    /// Invoke <paramref name="onChanged"/> now with the current resolved variant and again whenever it
    /// changes (e.g. the OS switches Light/Dark while in System mode). Use it to re-paint a self-drawn
    /// surface. Returns an <see cref="IDisposable"/> that unsubscribes.
    /// </summary>
    public static IDisposable OnActualThemeChanged(Control control, Action<ThemeVariant> onChanged)
    {
        void Handler(object? sender, EventArgs e) => onChanged(control.ActualThemeVariant);
        control.ActualThemeVariantChanged += Handler;
        onChanged(control.ActualThemeVariant);
        return new Unsubscriber(() => control.ActualThemeVariantChanged -= Handler);
    }

    private sealed class Unsubscriber : IDisposable
    {
        private Action? _dispose;
        public Unsubscriber(Action dispose) => _dispose = dispose;
        public void Dispose()
        {
            _dispose?.Invoke();
            _dispose = null;
        }
    }
}
