using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace SilkThread.App.Helpers;

/// <summary>
/// Provides inertial smooth scrolling with physical deceleration (braking effect)
/// for WinUI 3 ScrollViewer, ListView, and other scrollable containers.
/// </summary>
public static class SmoothScroll
{
    public const double DefaultScrollMultiplier = 0.14;
    public const double DefaultDecelerationFriction = 0.88;
    public const double StopVelocityThreshold = 0.1;
    public const double MaxVelocity = 250.0;

    private static readonly ConditionalWeakTable<ScrollViewer, SmoothScrollController> Controllers = new();

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(SmoothScroll),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) =>
        (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) =>
        obj.SetValue(IsEnabledProperty, value);

    public static readonly DependencyProperty ScrollMultiplierProperty =
        DependencyProperty.RegisterAttached(
            "ScrollMultiplier",
            typeof(double),
            typeof(SmoothScroll),
            new PropertyMetadata(DefaultScrollMultiplier));

    public static double GetScrollMultiplier(DependencyObject obj) =>
        (double)obj.GetValue(ScrollMultiplierProperty);

    public static void SetScrollMultiplier(DependencyObject obj, double value) =>
        obj.SetValue(ScrollMultiplierProperty, value);

    public static readonly DependencyProperty DecelerationFrictionProperty =
        DependencyProperty.RegisterAttached(
            "DecelerationFriction",
            typeof(double),
            typeof(SmoothScroll),
            new PropertyMetadata(DefaultDecelerationFriction));

    public static double GetDecelerationFriction(DependencyObject obj) =>
        (double)obj.GetValue(DecelerationFrictionProperty);

    public static void SetDecelerationFriction(DependencyObject obj, double value) =>
        obj.SetValue(DecelerationFrictionProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe) return;

        bool isEnabled = (bool)e.NewValue;
        if (isEnabled)
        {
            if (fe.IsLoaded)
            {
                Attach(fe);
            }
            else
            {
                RoutedEventHandler? loadedHandler = null;
                loadedHandler = (s, args) =>
                {
                    fe.Loaded -= loadedHandler;
                    if (GetIsEnabled(fe))
                    {
                        Attach(fe);
                    }
                };
                fe.Loaded += loadedHandler;
            }
        }
        else
        {
            Detach(fe);
        }
    }

    public static SmoothScrollController? Register(ScrollViewer scrollViewer)
    {
        SetIsEnabled(scrollViewer, true);
        return Attach(scrollViewer);
    }

    public static SmoothScrollController? Register(UIElement element)
    {
        if (element is FrameworkElement fe)
        {
            SetIsEnabled(fe, true);
            return Attach(fe);
        }
        return null;
    }

    public static void Unregister(UIElement element)
    {
        if (element is FrameworkElement fe)
        {
            SetIsEnabled(fe, false);
            Detach(fe);
        }
    }

    public static void ScrollBy(UIElement element, double deltaX, double deltaY)
    {
        var controller = GetController(element);
        controller?.AddImpulse(deltaX, deltaY);
    }

    public static SmoothScrollController? GetController(UIElement element)
    {
        if (element is ScrollViewer sv && Controllers.TryGetValue(sv, out var c))
        {
            return c;
        }

        var descendantSv = element as ScrollViewer ?? FindDescendant<ScrollViewer>(element);
        if (descendantSv != null && Controllers.TryGetValue(descendantSv, out var dc))
        {
            return dc;
        }

        return null;
    }

    private static SmoothScrollController? Attach(FrameworkElement fe)
    {
        if (fe is ScrollViewer sv)
        {
            if (Controllers.TryGetValue(sv, out var existing))
            {
                return existing;
            }

            var controller = new SmoothScrollController(sv);
            Controllers.AddOrUpdate(sv, controller);
            return controller;
        }

        if (fe is Control ctrl)
        {
            ctrl.ApplyTemplate();
        }

        var descendantSv = FindDescendant<ScrollViewer>(fe);
        if (descendantSv != null)
        {
            return Attach(descendantSv);
        }

        fe.DispatcherQueue?.TryEnqueue(() =>
        {
            if (!GetIsEnabled(fe)) return;
            var lateSv = FindDescendant<ScrollViewer>(fe);
            if (lateSv != null)
            {
                Attach(lateSv);
            }
        });

        return null;
    }

    private static void Detach(FrameworkElement fe)
    {
        var sv = fe as ScrollViewer ?? FindDescendant<ScrollViewer>(fe);
        if (sv != null)
        {
            if (Controllers.TryGetValue(sv, out var controller))
            {
                controller.Dispose();
                Controllers.Remove(sv);
            }
        }
    }

    public static T? FindDescendant<T>(DependencyObject element) where T : DependencyObject
    {
        int count = VisualTreeHelper.GetChildrenCount(element);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(element, i);
            if (child is T match)
                return match;
            var descendant = FindDescendant<T>(child);
            if (descendant != null)
                return descendant;
        }
        return null;
    }
}

/// <summary>
/// Alias for SmoothScroll helper for convenient access.
/// </summary>
public static class SmoothScrollHelper
{
    public static bool GetIsEnabled(DependencyObject obj) => SmoothScroll.GetIsEnabled(obj);
    public static void SetIsEnabled(DependencyObject obj, bool value) => SmoothScroll.SetIsEnabled(obj, value);
    public static SmoothScrollController? Register(ScrollViewer scrollViewer) => SmoothScroll.Register(scrollViewer);
    public static SmoothScrollController? Register(UIElement element) => SmoothScroll.Register(element);
    public static void Unregister(UIElement element) => SmoothScroll.Unregister(element);
    public static void ScrollBy(UIElement element, double deltaX, double deltaY) => SmoothScroll.ScrollBy(element, deltaX, deltaY);
    public static SmoothScrollController? GetController(UIElement element) => SmoothScroll.GetController(element);
}

/// <summary>
/// Manages the physics animation loop and pointer wheel event interception for a ScrollViewer.
/// </summary>
public sealed class SmoothScrollController : IDisposable
{
    private readonly ScrollViewer _scrollViewer;
    private readonly PointerEventHandler _wheelHandler;
    private UIElement? _registeredContent;

    private double _currentVerticalOffset;
    private double _verticalVelocity;
    private double _currentHorizontalOffset;
    private double _horizontalVelocity;

    private bool _isRenderingHooked;
    private readonly Stopwatch _stopwatch = new();
    private TimeSpan _lastElapsed;
    private PointerRoutedEventArgs? _lastHandledArgs;

    public SmoothScrollController(ScrollViewer scrollViewer)
    {
        _scrollViewer = scrollViewer;
        _wheelHandler = new PointerEventHandler(OnPointerWheelChanged);

        _scrollViewer.AddHandler(UIElement.PointerWheelChangedEvent, _wheelHandler, handledEventsToo: true);

        HookContent();

        _scrollViewer.Loaded += OnScrollViewerLoaded;
        _scrollViewer.Unloaded += OnScrollViewerUnloaded;
    }

    private void HookContent()
    {
        if (_scrollViewer.Content is UIElement content && !ReferenceEquals(_registeredContent, content))
        {
            if (_registeredContent != null)
            {
                _registeredContent.RemoveHandler(UIElement.PointerWheelChangedEvent, _wheelHandler);
            }
            _registeredContent = content;
            _registeredContent.AddHandler(UIElement.PointerWheelChangedEvent, _wheelHandler, handledEventsToo: true);
        }
    }

    private void OnScrollViewerLoaded(object sender, RoutedEventArgs e)
    {
        HookContent();
    }

    private void OnScrollViewerUnloaded(object sender, RoutedEventArgs e)
    {
        StopTicker();
    }

    public void AddImpulse(double deltaX, double deltaY)
    {
        double friction = SmoothScroll.GetDecelerationFriction(_scrollViewer);
        double factor = Math.Max(0.01, 1.0 - friction);

        if (Math.Abs(deltaX) > 0.01)
        {
            _currentHorizontalOffset = _scrollViewer.HorizontalOffset;
            _horizontalVelocity += deltaX * factor;
            _horizontalVelocity = Math.Clamp(_horizontalVelocity, -SmoothScroll.MaxVelocity, SmoothScroll.MaxVelocity);
        }

        if (Math.Abs(deltaY) > 0.01)
        {
            _currentVerticalOffset = _scrollViewer.VerticalOffset;
            _verticalVelocity += deltaY * factor;
            _verticalVelocity = Math.Clamp(_verticalVelocity, -SmoothScroll.MaxVelocity, SmoothScroll.MaxVelocity);
        }

        StartTicker();
    }

    private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        if (!SmoothScroll.GetIsEnabled(_scrollViewer)) return;
        if (ReferenceEquals(e, _lastHandledArgs)) return;

        var point = e.GetCurrentPoint(_scrollViewer);
        var props = point.Properties;
        int wheelDelta = props.MouseWheelDelta;
        if (wheelDelta == 0) return;

        bool isHorizontalOnly = IsHorizontalOnlyScrollViewer(_scrollViewer);
        bool isHorizontal = props.IsHorizontalMouseWheel ||
                            e.KeyModifiers.HasFlag(Windows.System.VirtualKeyModifiers.Shift) ||
                            isHorizontalOnly;

        double multiplier = SmoothScroll.GetScrollMultiplier(_scrollViewer);

        if (isHorizontal)
        {
            double maxH = Math.Max(0, _scrollViewer.ScrollableWidth);
            if (maxH <= 0.5) return; // Not horizontally scrollable, bubble up

            double dir = props.IsHorizontalMouseWheel ? 1.0 : -1.0;
            double delta = wheelDelta * dir;

            bool isAtLeft = _scrollViewer.HorizontalOffset <= 0.5;
            bool isAtRight = _scrollViewer.HorizontalOffset >= maxH - 0.5;

            // If at bounds and not already moving, allow bubble to parent
            if (Math.Abs(_horizontalVelocity) < 0.1 && ((isAtLeft && delta < 0) || (isAtRight && delta > 0)))
            {
                return;
            }

            if (Math.Abs(_horizontalVelocity) < 0.1 || Math.Abs(_scrollViewer.HorizontalOffset - _currentHorizontalOffset) > 40)
            {
                _currentHorizontalOffset = _scrollViewer.HorizontalOffset;
            }

            _horizontalVelocity += delta * multiplier;
            _horizontalVelocity = Math.Clamp(_horizontalVelocity, -SmoothScroll.MaxVelocity, SmoothScroll.MaxVelocity);

            _lastHandledArgs = e;
            e.Handled = true;
            StartTicker();
        }
        else
        {
            double maxV = Math.Max(0, _scrollViewer.ScrollableHeight);
            if (maxV <= 0.5) return; // Not vertically scrollable, bubble up

            // Wheel down (delta < 0) -> list moves down (offset increases)
            // Wheel up (delta > 0) -> list moves up (offset decreases)
            double delta = -wheelDelta;

            bool isAtTop = _scrollViewer.VerticalOffset <= 0.5;
            bool isAtBottom = _scrollViewer.VerticalOffset >= maxV - 0.5;

            // If at bounds and not already moving, allow bubble to parent
            if (Math.Abs(_verticalVelocity) < 0.1 && ((isAtTop && delta < 0) || (isAtBottom && delta > 0)))
            {
                return;
            }

            if (Math.Abs(_verticalVelocity) < 0.1 || Math.Abs(_scrollViewer.VerticalOffset - _currentVerticalOffset) > 40)
            {
                _currentVerticalOffset = _scrollViewer.VerticalOffset;
            }

            _verticalVelocity += delta * multiplier;
            _verticalVelocity = Math.Clamp(_verticalVelocity, -SmoothScroll.MaxVelocity, SmoothScroll.MaxVelocity);

            _lastHandledArgs = e;
            e.Handled = true;
            StartTicker();
        }
    }

    private void StartTicker()
    {
        if (!_isRenderingHooked)
        {
            _stopwatch.Restart();
            _lastElapsed = TimeSpan.Zero;
            CompositionTarget.Rendering += OnRendering;
            _isRenderingHooked = true;
        }
    }

    private void StopTicker()
    {
        if (_isRenderingHooked)
        {
            CompositionTarget.Rendering -= OnRendering;
            _isRenderingHooked = false;
            _stopwatch.Stop();
        }
        _verticalVelocity = 0;
        _horizontalVelocity = 0;
    }

    private void OnRendering(object? sender, object e)
    {
        var now = _stopwatch.Elapsed;
        double dt = (now - _lastElapsed).TotalSeconds;
        _lastElapsed = now;

        if (dt <= 0 || dt > 0.05)
        {
            dt = 0.01667;
        }

        double frameRatio = dt / 0.01667;
        double friction = SmoothScroll.GetDecelerationFriction(_scrollViewer);
        double currentFriction = Math.Pow(friction, frameRatio);

        bool isMovingV = false;
        bool isMovingH = false;

        // Vertical physics
        if (Math.Abs(_verticalVelocity) >= SmoothScroll.StopVelocityThreshold)
        {
            if (Math.Abs(_scrollViewer.VerticalOffset - _currentVerticalOffset) > 40)
            {
                _verticalVelocity = 0;
                _currentVerticalOffset = _scrollViewer.VerticalOffset;
            }
            else
            {
                _currentVerticalOffset += _verticalVelocity * frameRatio;
                _verticalVelocity *= currentFriction;

                double maxV = Math.Max(0, _scrollViewer.ScrollableHeight);
                if (_currentVerticalOffset <= 0)
                {
                    _currentVerticalOffset = 0;
                    _verticalVelocity = 0;
                }
                else if (_currentVerticalOffset >= maxV)
                {
                    _currentVerticalOffset = maxV;
                    _verticalVelocity = 0;
                }

                if (Math.Abs(_verticalVelocity) >= SmoothScroll.StopVelocityThreshold)
                {
                    isMovingV = true;
                }
                else
                {
                    _verticalVelocity = 0;
                }
            }
        }
        else
        {
            _verticalVelocity = 0;
        }

        // Horizontal physics
        if (Math.Abs(_horizontalVelocity) >= SmoothScroll.StopVelocityThreshold)
        {
            if (Math.Abs(_scrollViewer.HorizontalOffset - _currentHorizontalOffset) > 40)
            {
                _horizontalVelocity = 0;
                _currentHorizontalOffset = _scrollViewer.HorizontalOffset;
            }
            else
            {
                _currentHorizontalOffset += _horizontalVelocity * frameRatio;
                _horizontalVelocity *= currentFriction;

                double maxH = Math.Max(0, _scrollViewer.ScrollableWidth);
                if (_currentHorizontalOffset <= 0)
                {
                    _currentHorizontalOffset = 0;
                    _horizontalVelocity = 0;
                }
                else if (_currentHorizontalOffset >= maxH)
                {
                    _currentHorizontalOffset = maxH;
                    _horizontalVelocity = 0;
                }

                if (Math.Abs(_horizontalVelocity) >= SmoothScroll.StopVelocityThreshold)
                {
                    isMovingH = true;
                }
                else
                {
                    _horizontalVelocity = 0;
                }
            }
        }
        else
        {
            _horizontalVelocity = 0;
        }

        double? targetH = isMovingH || Math.Abs(_scrollViewer.HorizontalOffset - _currentHorizontalOffset) > 0.01
            ? _currentHorizontalOffset
            : null;

        double? targetV = isMovingV || Math.Abs(_scrollViewer.VerticalOffset - _currentVerticalOffset) > 0.01
            ? _currentVerticalOffset
            : null;

        if (targetH.HasValue || targetV.HasValue)
        {
            _scrollViewer.ChangeView(targetH, targetV, null, disableAnimation: true);
        }

        if (!isMovingV && !isMovingH)
        {
            StopTicker();
        }
    }

    private static bool IsHorizontalOnlyScrollViewer(ScrollViewer sv)
    {
        if (sv.VerticalScrollMode == ScrollMode.Disabled) return true;
        if (sv.VerticalScrollBarVisibility == ScrollBarVisibility.Disabled && sv.HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled) return true;
        if (sv.ScrollableHeight <= 0.5 && sv.ScrollableWidth > 0.5) return true;
        return false;
    }

    public void Dispose()
    {
        StopTicker();

        try
        {
            _scrollViewer.RemoveHandler(UIElement.PointerWheelChangedEvent, _wheelHandler);
            if (_registeredContent != null)
            {
                _registeredContent.RemoveHandler(UIElement.PointerWheelChangedEvent, _wheelHandler);
                _registeredContent = null;
            }
            _scrollViewer.Loaded -= OnScrollViewerLoaded;
            _scrollViewer.Unloaded -= OnScrollViewerUnloaded;
        }
        catch
        {
            // Ignore during teardown
        }
    }
}
