using System;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace SilkThread.App.Helpers;

/// <summary>
/// Preserves native ScrollViewer wheel/touch behavior and exposes animated programmatic scrolling.
/// The legacy physics parameters remain available for source compatibility.
/// </summary>
public static class SmoothScroll
{
    public const double DefaultScrollMultiplier = 0.14;
    public const double DefaultDecelerationFriction = 0.88;
    public const double StopVelocityThreshold = 0.1;
    public const double MaxVelocity = 250.0;

    /// <summary>Integrates one decay interval in units of a 60 Hz frame, independent of refresh rate.</summary>
    public static (double Travel, double Velocity) IntegrateDecay(double velocity, double friction, double frameRatio)
    {
        if (!double.IsFinite(velocity) || !double.IsFinite(frameRatio) || frameRatio <= 0) return (0, 0);
        if (!double.IsFinite(friction) || friction <= 0 || friction >= 1) friction = DefaultDecelerationFriction;
        double decay = Math.Pow(friction, frameRatio);
        return (velocity * (1 - decay) / (1 - friction), velocity * decay);
    }

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

        fe.Loaded -= ElementLoaded;
        fe.Unloaded -= ElementUnloaded;
        if ((bool)e.NewValue)
        {
            fe.Loaded += ElementLoaded;
            fe.Unloaded += ElementUnloaded;
            if (fe.IsLoaded) Attach(fe);
        }
        else Detach(fe);
    }

    private static void ElementLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && GetIsEnabled(fe)) Attach(fe);
    }
    private static void ElementUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe) Detach(fe);
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

    private static SmoothScrollController? Attach(FrameworkElement fe, FrameworkElement? optionsOwner = null)
    {
        if (fe is ScrollViewer sv)
        {
            if (Controllers.TryGetValue(sv, out var existing))
            {
                return existing;
            }

            var controller = new SmoothScrollController(sv, optionsOwner ?? fe);
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
            return Attach(descendantSv, fe);
        }

        fe.DispatcherQueue?.TryEnqueue(() =>
        {
            if (!fe.IsLoaded || !GetIsEnabled(fe)) return;
            var lateSv = FindDescendant<ScrollViewer>(fe);
            if (lateSv != null)
            {
                Attach(lateSv, fe);
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
/// Compatibility controller for programmatic arrows. Wheel/touch/trackpad scrolling stays
/// entirely owned by ScrollViewer so nested controls and layout changes have one scroll owner.
/// </summary>
public sealed class SmoothScrollController : IDisposable
{
    private readonly ScrollViewer _viewer;
    private bool _disposed;
    public SmoothScrollController(ScrollViewer viewer) { _viewer = viewer; }
    internal SmoothScrollController(ScrollViewer viewer, FrameworkElement optionsOwner) : this(viewer) { }

    public void AddImpulse(double deltaX, double deltaY)
    {
        if (_disposed || !double.IsFinite(deltaX) || !double.IsFinite(deltaY)) return;
        double? x = Math.Abs(deltaX) > 0.01 ? Math.Clamp(_viewer.HorizontalOffset + deltaX, 0, Math.Max(0, _viewer.ScrollableWidth)) : null;
        double? y = Math.Abs(deltaY) > 0.01 ? Math.Clamp(_viewer.VerticalOffset + deltaY, 0, Math.Max(0, _viewer.ScrollableHeight)) : null;
        _viewer.ChangeView(x, y, null, disableAnimation: !MotionPreferences.AnimationsEnabled);
    }

    public void Dispose() => _disposed = true;
}
