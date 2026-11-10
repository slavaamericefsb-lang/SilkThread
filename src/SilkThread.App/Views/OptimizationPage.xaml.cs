using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using SilkThread.App.Services;
using SilkThread.App.ViewModels;

namespace SilkThread.App.Views;

public sealed partial class OptimizationPage : Page, IStaggeredPage
{
    public OptimizationViewModel ViewModel { get; }

    public IReadOnlyList<FrameworkElement> GetStaggeredElements()
    {
        return new FrameworkElement[]
        {
            HeaderGrid,
            CardProfiles,
            CardExecution,
            CardTerminal
        };
    }

    private Storyboard? _cursorBlinkStoryboard;
    private Storyboard? _livePulseStoryboard;

    public OptimizationPage()
    {
        ViewModel = App.Services.GetRequiredService<OptimizationViewModel>();
        InitializeComponent();

        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(OptimizationViewModel.IsExecuting))
            {
                StartLivePulseAnimation(ViewModel.IsExecuting);
            }
        };
    }

    private void StartCursorBlink()
    {
        if (TerminalCursor == null) return;

        _cursorBlinkStoryboard?.Stop();
        _cursorBlinkStoryboard = new Storyboard
        {
            RepeatBehavior = RepeatBehavior.Forever
        };

        var blinkAnim = new DoubleAnimationUsingKeyFrames();
        blinkAnim.KeyFrames.Add(new DiscreteDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 1.0 });
        blinkAnim.KeyFrames.Add(new DiscreteDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(500)), Value = 0.0 });
        blinkAnim.KeyFrames.Add(new DiscreteDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1000)), Value = 1.0 });

        Storyboard.SetTarget(blinkAnim, TerminalCursor);
        Storyboard.SetTargetProperty(blinkAnim, "Opacity");

        _cursorBlinkStoryboard.Children.Add(blinkAnim);
        _cursorBlinkStoryboard.Begin();
    }

    private void StartLivePulseAnimation(bool isFast)
    {
        if (TerminalLiveGlow == null || TerminalLiveScale == null) return;

        double durationSeconds = isFast ? 0.65 : 1.35;
        _livePulseStoryboard?.Stop();
        _livePulseStoryboard = new Storyboard
        {
            RepeatBehavior = RepeatBehavior.Forever,
            AutoReverse = true
        };

        var scaleX = new DoubleAnimation
        {
            From = 1.0,
            To = isFast ? 1.7 : 1.45,
            Duration = new Duration(TimeSpan.FromSeconds(durationSeconds)),
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        Storyboard.SetTarget(scaleX, TerminalLiveScale);
        Storyboard.SetTargetProperty(scaleX, "ScaleX");

        var scaleY = new DoubleAnimation
        {
            From = 1.0,
            To = isFast ? 1.7 : 1.45,
            Duration = new Duration(TimeSpan.FromSeconds(durationSeconds)),
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        Storyboard.SetTarget(scaleY, TerminalLiveScale);
        Storyboard.SetTargetProperty(scaleY, "ScaleY");

        var opacity = new DoubleAnimation
        {
            From = isFast ? 0.8 : 0.55,
            To = 0.1,
            Duration = new Duration(TimeSpan.FromSeconds(durationSeconds)),
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        Storyboard.SetTarget(opacity, TerminalLiveGlow);
        Storyboard.SetTargetProperty(opacity, "Opacity");

        _livePulseStoryboard.Children.Add(scaleX);
        _livePulseStoryboard.Children.Add(scaleY);
        _livePulseStoryboard.Children.Add(opacity);
        _livePulseStoryboard.Begin();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        PageTransitionHelper.AnimatePageEntrance(this);
        StartCursorBlink();
        StartLivePulseAnimation(ViewModel.IsExecuting);
        await ViewModel.InitializeAsync();
    }
}
