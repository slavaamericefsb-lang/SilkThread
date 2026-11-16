using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SilkThread.App.Helpers;

/// <summary>
/// Compatibility attached value for progress meters. Readings are committed directly;
/// a dependent Value storyboard must not race live metric bindings or native template animation.
/// </summary>
public static class ProgressBarAnimation
{
    public static readonly DependencyProperty AnimatedValueProperty = DependencyProperty.RegisterAttached(
        "AnimatedValue", typeof(double), typeof(ProgressBarAnimation), new PropertyMetadata(0.0, OnAnimatedValueChanged));
    public static double GetAnimatedValue(DependencyObject obj) => (double)obj.GetValue(AnimatedValueProperty);
    public static void SetAnimatedValue(DependencyObject obj, double value) => obj.SetValue(AnimatedValueProperty, value);
    private static void OnAnimatedValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ProgressBar bar) AnimateTo(bar, (double)e.NewValue);
    }

    // Kept for existing callers; the native ProgressBar now owns the presentation timing.
    public static void AnimateTo(ProgressBar bar, double targetValue, double durationMs = MotionPreferences.ProgressMilliseconds)
    {
        if (bar == null) return;
        // A regular binding already owns this value; never replace it with a local value.
        if (bar.GetBindingExpression(Microsoft.UI.Xaml.Controls.Primitives.RangeBase.ValueProperty) != null) return;
        bar.Value = Math.Clamp(double.IsFinite(targetValue) ? targetValue : bar.Minimum, bar.Minimum, bar.Maximum);
    }
}
