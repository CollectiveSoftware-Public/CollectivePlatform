// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace Collective.Platform.Controls;

/// <summary>
/// The bottom-right overlay that renders a <see cref="ToastService"/>'s active toasts — the shared
/// transient-feedback surface no head had before. A head drops one of these over its root content and
/// points it at the service; the host subscribes to <see cref="ToastService.Changed"/> and also polls
/// on a timer so time-expired toasts disappear. Click a toast to dismiss it. Self-contained colours
/// (no external resource dependency) so it works in any head regardless of its accent.
/// </summary>
public sealed class ToastHost : UserControl
{
    private readonly StackPanel _stack;
    private readonly DispatcherTimer _timer;
    private ToastService? _service;

    public ToastHost()
    {
        HorizontalAlignment = HorizontalAlignment.Right;
        VerticalAlignment = VerticalAlignment.Bottom;
        Margin = new Thickness(16);
        IsHitTestVisible = true;

        _stack = new StackPanel { Spacing = 8 };
        Content = _stack;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _timer.Tick += (_, _) => Rebuild();
    }

    /// <summary>The toast queue to display. Setting it (re)subscribes and redraws.</summary>
    public ToastService? Service
    {
        get => _service;
        set
        {
            if (_service is not null) _service.Changed -= OnChanged;
            _service = value;
            if (_service is not null) _service.Changed += OnChanged;
            Rebuild();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _timer.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _timer.Stop();
    }

    private void OnChanged()
    {
        if (Dispatcher.UIThread.CheckAccess()) Rebuild();
        else Dispatcher.UIThread.Post(Rebuild);
    }

    private void Rebuild()
    {
        _stack.Children.Clear();
        if (_service is null) return;
        foreach (var toast in _service.Visible())
            _stack.Children.Add(BuildCard(toast));
    }

    private Control BuildCard(ToastItem toast)
    {
        var text = new TextBlock
        {
            Text = toast.Message,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 360,
            Foreground = Brushes.White,
        };
        var border = new Border
        {
            Background = SeverityBrush(toast.Severity),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(14, 10),
            Child = text,
            BoxShadow = BoxShadows.Parse("0 2 8 0 #40000000"),
        };
        border.PointerPressed += (_, _) => _service?.Dismiss(toast.Id);
        return border;
    }

    private static IBrush SeverityBrush(ToastSeverity severity) => severity switch
    {
        ToastSeverity.Success => new SolidColorBrush(Color.Parse("#16A34A")),
        ToastSeverity.Warning => new SolidColorBrush(Color.Parse("#D97706")),
        ToastSeverity.Error => new SolidColorBrush(Color.Parse("#DC2626")),
        _ => new SolidColorBrush(Color.Parse("#334155")),
    };
}
