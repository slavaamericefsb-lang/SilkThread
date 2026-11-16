using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace SilkThread.App.Helpers;

/// <summary>
/// A compositor-owned outline above the element. It never replaces Background/BorderBrush,
/// changes layout, or scales text. Control templates keep ownership of their own visual states.
/// </summary>
public static class HoverAnimation
{
    private static readonly ConditionalWeakTable<FrameworkElement, HoverState> States = new();
    #region Attached Dependency Properties

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(HoverAnimation),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) =>
        (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) =>
        obj.SetValue(IsEnabledProperty, value);

    /// <summary>
    /// Legacy Scale property maintained for backward compatibility.
    /// Scale animations are intentionally disabled to guarantee 100% DirectWrite font sharpness
    /// and prevent cursor jitter on card boundaries.
    /// </summary>
    public static readonly DependencyProperty ScaleProperty =
        DependencyProperty.RegisterAttached(
            "Scale",
            typeof(double),
            typeof(HoverAnimation),
            new PropertyMetadata(1.0));

    public static double GetScale(DependencyObject obj) =>
        (double)obj.GetValue(ScaleProperty);

    public static void SetScale(DependencyObject obj, double value) =>
        obj.SetValue(ScaleProperty, value);

    public static readonly DependencyProperty HoverBorderColorProperty =
        DependencyProperty.RegisterAttached(
            "HoverBorderColor",
            typeof(object), // Color or string hex "#806BFF"
            typeof(HoverAnimation),
            new PropertyMetadata(null));

    public static object? GetHoverBorderColor(DependencyObject obj) =>
        obj.GetValue(HoverBorderColorProperty);

    public static void SetHoverBorderColor(DependencyObject obj, object? value) =>
        obj.SetValue(HoverBorderColorProperty, value);

    public static readonly DependencyProperty HoverBackgroundColorProperty =
        DependencyProperty.RegisterAttached(
            "HoverBackgroundColor",
            typeof(object), // Color or string hex "#1F2330"
            typeof(HoverAnimation),
            new PropertyMetadata(null));

    public static object? GetHoverBackgroundColor(DependencyObject obj) =>
        obj.GetValue(HoverBackgroundColorProperty);

    public static void SetHoverBackgroundColor(DependencyObject obj, object? value) =>
        obj.SetValue(HoverBackgroundColorProperty, value);

    public static readonly DependencyProperty DurationMsProperty =
        DependencyProperty.RegisterAttached(
            "DurationMs",
            typeof(int),
            typeof(HoverAnimation),
            new PropertyMetadata(MotionPreferences.HoverMilliseconds));

    public static int GetDurationMs(DependencyObject obj) =>
        (int)obj.GetValue(DurationMsProperty);

    public static void SetDurationMs(DependencyObject obj, int value) =>
        obj.SetValue(DurationMsProperty, value);

    #endregion

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element) return;
        element.PointerEntered -= PointerEntered;
        element.PointerExited -= PointerExited;
        element.PointerCanceled -= PointerCanceled;
        element.SizeChanged -= SizeChanged;
        element.Unloaded -= Unloaded;
        if ((bool)e.NewValue)
        {
            element.PointerEntered += PointerEntered;
            element.PointerExited += PointerExited;
            element.PointerCanceled += PointerCanceled;
            element.SizeChanged += SizeChanged;
            element.Unloaded += Unloaded;
        }
        else Release(element);
    }

    private static void PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.IsLoaded && element.IsHitTestVisible
            && (element is not Control control || control.IsEnabled)) SetHovered(element, true);
    }

    private static void PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not FrameworkElement element) return;
        var point = e.GetCurrentPoint(element).Position;
        // Entering an interactive child is not an exit from its parent card.
        if (point.X >= 0 && point.Y >= 0 && point.X < element.ActualWidth && point.Y < element.ActualHeight) return;
        SetHovered(element, false);
    }

    private static void PointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement element) SetHovered(element, false);
    }

    private static void SetHovered(FrameworkElement element, bool hovered)
    {
        if (!States.TryGetValue(element, out var state))
        {
            if (!hovered || ElementCompositionPreview.GetElementChildVisual(element) != null) return;
            state = Create(element);
            States.Add(element, state);
        }
        if (state.Hovered == hovered) return;
        state.Hovered = hovered;
        state.Brush.Color = ResolveColor(GetHoverBorderColor(element));
        if (!MotionPreferences.AnimationsEnabled)
        {
            state.Visual.StopAnimation("Opacity");
            state.Visual.Opacity = hovered ? 1 : 0;
            return;
        }
        var animation = state.Visual.Compositor.CreateScalarKeyFrameAnimation();
        // StartingValue samples the presentation value of the animation being replaced.
        // There is no Stop/rebase interval that can expose a different brush for one frame.
        animation.InsertExpressionKeyFrame(0, "this.StartingValue");
        animation.InsertKeyFrame(1, hovered ? 1 : 0);
        int duration = Math.Clamp(GetDurationMs(element), 80, 240);
        animation.Duration = TimeSpan.FromMilliseconds(duration);
        state.Visual.StartAnimation("Opacity", animation);
    }

    private static HoverState Create(FrameworkElement element)
    {
        var compositor = ElementCompositionPreview.GetElementVisual(element).Compositor;
        var geometry = compositor.CreateRoundedRectangleGeometry();
        var brush = compositor.CreateColorBrush(ResolveColor(GetHoverBorderColor(element)));
        var shape = compositor.CreateSpriteShape(geometry);
        shape.StrokeBrush = brush;
        shape.StrokeThickness = 1;
        var visual = compositor.CreateShapeVisual();
        visual.Shapes.Add(shape);
        visual.Opacity = 0;
        var state = new HoverState(visual, geometry, shape, brush);
        UpdateSize(element, state);
        ElementCompositionPreview.SetElementChildVisual(element, visual);
        return state;
    }

    private static void SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is FrameworkElement element && States.TryGetValue(element, out var state)) UpdateSize(element, state);
    }
    private static void UpdateSize(FrameworkElement element, HoverState state)
    {
        float width = (float)Math.Max(0, element.ActualWidth);
        float height = (float)Math.Max(0, element.ActualHeight);
        state.Visual.Size = new Vector2(width, height);
        state.Geometry.Offset = new Vector2(0.5f);
        state.Geometry.Size = new Vector2(Math.Max(0, width - 1), Math.Max(0, height - 1));
        var radius = element is Border border ? border.CornerRadius : element is Control control ? control.CornerRadius : new CornerRadius(0);
        float corner = (float)Math.Min(Math.Min(width, height) / 2, radius.TopLeft);
        state.Geometry.CornerRadius = new Vector2(Math.Max(0, corner - 0.5f));
    }

    private static Color ResolveColor(object? value)
    {
        if (value is Color color) return color;
        if (value is string text)
        {
            string hex = text.Trim().TrimStart('#');
            if (uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var argb))
            {
                if (hex.Length == 6) argb |= 0xFF000000;
                if (hex.Length is 6 or 8) return Color.FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb);
            }
        }
        return Color.FromArgb(255, 128, 107, 255);
    }

    private static void Unloaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element) Release(element);
    }
    private static void Release(FrameworkElement element)
    {
        if (!States.TryGetValue(element, out var state)) return;
        if (ReferenceEquals(ElementCompositionPreview.GetElementChildVisual(element), state.Visual))
            ElementCompositionPreview.SetElementChildVisual(element, null);
        state.Visual.StopAnimation("Opacity");
        state.Visual.Dispose();
        state.Shape.Dispose();
        state.Geometry.Dispose();
        state.Brush.Dispose();
        States.Remove(element);
    }

    private sealed class HoverState(ShapeVisual visual, CompositionRoundedRectangleGeometry geometry,
        CompositionSpriteShape shape, CompositionColorBrush brush)
    {
        public ShapeVisual Visual { get; } = visual;
        public CompositionRoundedRectangleGeometry Geometry { get; } = geometry;
        public CompositionSpriteShape Shape { get; } = shape;
        public CompositionColorBrush Brush { get; } = brush;
        public bool Hovered;
    }
}
