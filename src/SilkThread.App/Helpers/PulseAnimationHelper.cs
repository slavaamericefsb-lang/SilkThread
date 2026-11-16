using System;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.UI.ViewManagement;
using Windows.Foundation;

namespace SilkThread.App.Helpers;

/// <summary>
/// Provides smooth, CPU-efficient looping heartbeat pulsing animations (DoubleAnimation Opacity 0.6 <-> 1.0)
/// for live UI status indicators:
/// - Protection and optimization status on Dashboard
/// - Green active session indicator in developer debug dialog
/// - AI IOST "Ready" status indicator
/// - Latency optimization matrix badge
/// Automatically ceases all animation when elements are unloaded or hidden (Visibility != Visible)
/// to ensure 0% background CPU consumption.
/// </summary>
public static class PulseAnimation
{
    private static readonly ConditionalWeakTable<FrameworkElement, PulseState> s_pulseStates = new();

    #region Attached Dependency Properties

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(PulseAnimation),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) =>
        (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) =>
        obj.SetValue(IsEnabledProperty, value);

    public static readonly DependencyProperty MinOpacityProperty =
        DependencyProperty.RegisterAttached(
            "MinOpacity",
            typeof(double),
            typeof(PulseAnimation),
            new PropertyMetadata(0.6));

    public static double GetMinOpacity(DependencyObject obj) =>
        (double)obj.GetValue(MinOpacityProperty);

    public static void SetMinOpacity(DependencyObject obj, double value) =>
        obj.SetValue(MinOpacityProperty, value);

    public static readonly DependencyProperty MaxOpacityProperty =
        DependencyProperty.RegisterAttached(
            "MaxOpacity",
            typeof(double),
            typeof(PulseAnimation),
            new PropertyMetadata(1.0));

    public static double GetMaxOpacity(DependencyObject obj) =>
        (double)obj.GetValue(MaxOpacityProperty);

    public static void SetMaxOpacity(DependencyObject obj, double value) =>
        obj.SetValue(MaxOpacityProperty, value);

    public static readonly DependencyProperty DurationSecondsProperty =
        DependencyProperty.RegisterAttached(
            "DurationSeconds",
            typeof(double),
            typeof(PulseAnimation),
            new PropertyMetadata(2.2));

    public static double GetDurationSeconds(DependencyObject obj) =>
        (double)obj.GetValue(DurationSecondsProperty);

    public static void SetDurationSeconds(DependencyObject obj, double value) =>
        obj.SetValue(DurationSecondsProperty, value);

    #endregion

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe) return;

        if ((bool)e.NewValue)
        {
            var state = s_pulseStates.GetOrCreateValue(fe);

            fe.Loaded -= Element_Loaded;
            fe.Loaded += Element_Loaded;

            fe.Unloaded -= Element_Unloaded;
            fe.Unloaded += Element_Unloaded;

            if (state.VisibilityToken == 0)
            {
                state.VisibilityToken = fe.RegisterPropertyChangedCallback(
                    UIElement.VisibilityProperty,
                    OnElementVisibilityChanged);
            }

            if (fe.IsLoaded && fe.Visibility == Visibility.Visible)
            {
                StartPulse(fe);
            }
        }
        else
        {
            fe.Loaded -= Element_Loaded;
            fe.Unloaded -= Element_Unloaded;

            if (s_pulseStates.TryGetValue(fe, out var state))
            {
                if (state.VisibilityToken != 0)
                {
                    fe.UnregisterPropertyChangedCallback(UIElement.VisibilityProperty, state.VisibilityToken);
                    state.VisibilityToken = 0;
                }
                StopPulse(fe);
                s_pulseStates.Remove(fe);
            }

            StopPulse(fe);
        }
    }

    private static void OnElementVisibilityChanged(DependencyObject d, DependencyProperty dp)
    {
        if (d is not FrameworkElement fe) return;

        if (fe.Visibility == Visibility.Visible && fe.IsLoaded && GetIsEnabled(fe))
        {
            StartPulse(fe);
        }
        else
        {
            StopPulse(fe);
        }
    }

    private static void Element_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && GetIsEnabled(fe) && fe.Visibility == Visibility.Visible)
        {
            StartPulse(fe);
        }
    }

    private static void Element_Unloaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe)
        {
            StopPulse(fe);
        }
    }

    public static void StartPulse(FrameworkElement fe)
    {
        StopPulse(fe);

        if (!fe.IsLoaded || fe.Visibility != Visibility.Visible || !GetIsEnabled(fe)) return;
        var state = s_pulseStates.GetOrCreateValue(fe);
        // The OS raises this event off the UI thread. Use a weak target and marshal back.
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
        {
            var weak = new WeakReference<FrameworkElement>(fe);
            state.Settings = new UISettings();
            state.SettingsChanged = (_, _) =>
            {
                if (weak.TryGetTarget(out var target)) target.DispatcherQueue.TryEnqueue(() =>
                {
                    if (target.IsLoaded && GetIsEnabled(target)) StartPulse(target);
                });
            };
            state.Settings.AnimationsEnabledChanged += state.SettingsChanged;
        }
        if (!MotionPreferences.AnimationsEnabled) return;
        double maxOp = SafeOpacity(GetMaxOpacity(fe), 1);
        double minOp = Math.Min(maxOp, SafeOpacity(GetMinOpacity(fe), 0.6));
        double durationSec = GetDurationSeconds(fe);
        if (!double.IsFinite(durationSec) || durationSec <= 0.1 || durationSec > 30) durationSec = 2.2;

        var sb = new Storyboard
        {
            RepeatBehavior = RepeatBehavior.Forever,
            AutoReverse = true
        };

        var anim = new DoubleAnimation
        {
            From = maxOp,
            To = minOp,
            Duration = new Duration(TimeSpan.FromSeconds(durationSec / 2.0)),
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };

        Storyboard.SetTarget(anim, fe);
        Storyboard.SetTargetProperty(anim, "Opacity");
        sb.Children.Add(anim);

        state.Storyboard = sb;
        sb.Begin();
    }

    public static void StopPulse(FrameworkElement fe)
    {
        if (s_pulseStates.TryGetValue(fe, out var state))
        {
            state.Storyboard?.Stop();
            state.Storyboard = null;
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041) && state.Settings != null && state.SettingsChanged != null)
                state.Settings.AnimationsEnabledChanged -= state.SettingsChanged;
            state.Settings = null;
            state.SettingsChanged = null;
        }
        fe.Opacity = SafeOpacity(GetMaxOpacity(fe), 1);
    }

    private static double SafeOpacity(double value, double fallback) => double.IsFinite(value) ? Math.Clamp(value, 0, 1) : fallback;

    private sealed class PulseState
    {
        public Storyboard? Storyboard { get; set; }
        public UISettings? Settings;
        public TypedEventHandler<UISettings, UISettingsAnimationsEnabledChangedEventArgs>? SettingsChanged;
        public long VisibilityToken { get; set; }
    }
}
