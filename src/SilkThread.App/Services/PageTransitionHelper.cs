using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace SilkThread.App.Services;

/// <summary>
/// Interface implemented by views to explicitly specify the cards/tiles
/// participating in staggered entrance and exit animations.
/// </summary>
public interface IStaggeredPage
{
    IReadOnlyList<FrameworkElement> GetStaggeredElements();
}

/// <summary>
/// Handles smooth transition navigation and staggered tile animations
/// across pages in SilkThread.
/// </summary>
public static class PageTransitionHelper
{
    private static Storyboard? _currentExitStoryboard;
    private static Storyboard? _currentEntranceStoryboard;

    /// <summary>
    /// Executes a fluid transition navigation on the target Frame:
    /// 1. Staggered fade-out of current page tiles (with subtle upward offset)
    /// 2. Frame.Navigate to the new page
    /// 3. Staggered fade-in and upward slide (10px -> 0px) of new page tiles
    /// </summary>
    public static async Task TransitionNavigateAsync(Frame frame, Type pageType, object? parameter = null)
    {
        if (frame == null) return;
        if (frame.CurrentSourcePageType == pageType) return;

        // Stop any currently running animations
        _currentExitStoryboard?.Stop();
        _currentEntranceStoryboard?.Stop();

        // 1. If currently displaying a page, animate its exit
        if (frame.Content is Page currentPage)
        {
            await AnimatePageExitAsync(currentPage);
        }

        // 2. Perform navigation
        frame.Navigate(pageType, parameter);

        // 3. Animate entrance of the newly loaded page
        if (frame.Content is Page newPage)
        {
            AnimatePageEntrance(newPage);
        }
    }

    /// <summary>
    /// Animates exit of a page: tiles fade out and subtly slide upward with progressive delays.
    /// </summary>
    public static Task AnimatePageExitAsync(Page page)
    {
        var tcs = new TaskCompletionSource<bool>();
        var elements = GetElementsToAnimate(page);

        if (elements.Count == 0)
        {
            return Task.CompletedTask;
        }

        var sb = new Storyboard();
        _currentExitStoryboard = sb;

        for (int i = 0; i < elements.Count; i++)
        {
            var elem = elements[i];
            int delayMs = Math.Min(i * 25, 120);

            var transform = elem.RenderTransform as TranslateTransform;
            if (transform == null)
            {
                transform = new TranslateTransform();
                elem.RenderTransform = transform;
            }

            // Opacity: current -> 0.0
            var opacityAnim = new DoubleAnimation
            {
                From = elem.Opacity,
                To = 0.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(130)),
                BeginTime = TimeSpan.FromMilliseconds(delayMs),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            Storyboard.SetTarget(opacityAnim, elem);
            Storyboard.SetTargetProperty(opacityAnim, "Opacity");
            sb.Children.Add(opacityAnim);

            // Subtle vertical translation: 0px -> -8px
            var transAnim = new DoubleAnimation
            {
                From = transform.Y,
                To = -8.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(130)),
                BeginTime = TimeSpan.FromMilliseconds(delayMs),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            Storyboard.SetTarget(transAnim, transform);
            Storyboard.SetTargetProperty(transAnim, "Y");
            sb.Children.Add(transAnim);
        }

        sb.Completed += (s, e) =>
        {
            _currentExitStoryboard = null;
            tcs.TrySetResult(true);
        };

        sb.Begin();

        // Fallback safety timeout so navigation never blocks indefinitely
        _ = Task.Delay(220).ContinueWith(_ => tcs.TrySetResult(true));

        return tcs.Task;
    }

    /// <summary>
    /// Animates entrance of a page: tiles fade in from opacity 0 to 1 and translate upward from 10px to 0px
    /// with progressive staggered delays (0ms, 60ms, 120ms, 180ms, 240ms, etc.) using smooth CubicEase.
    /// </summary>
    public static void AnimatePageEntrance(Page page)
    {
        var elements = GetElementsToAnimate(page);
        if (elements.Count == 0) return;

        _currentEntranceStoryboard?.Stop();

        var sb = new Storyboard();
        _currentEntranceStoryboard = sb;

        for (int i = 0; i < elements.Count; i++)
        {
            var elem = elements[i];
            int delayMs = i * 60; // Card 1 at 0ms, Card 2 at 60ms, Card 3 at 120ms, Card 4 at 180ms, etc.

            var transform = elem.RenderTransform as TranslateTransform;
            if (transform == null)
            {
                transform = new TranslateTransform();
                elem.RenderTransform = transform;
            }

            // Set initial state before animation starts
            elem.Opacity = 0.0;
            transform.Y = 10.0;

            // Opacity: 0.0 -> 1.0 (Duration 280ms)
            var opacityAnim = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(280)),
                BeginTime = TimeSpan.FromMilliseconds(delayMs),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(opacityAnim, elem);
            Storyboard.SetTargetProperty(opacityAnim, "Opacity");
            sb.Children.Add(opacityAnim);

            // Translate: 10px -> 0px (Duration 320ms)
            var transAnim = new DoubleAnimation
            {
                From = 10.0,
                To = 0.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(320)),
                BeginTime = TimeSpan.FromMilliseconds(delayMs),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(transAnim, transform);
            Storyboard.SetTargetProperty(transAnim, "Y");
            sb.Children.Add(transAnim);
        }

        sb.Completed += (s, e) =>
        {
            _currentEntranceStoryboard = null;
            // Guarantee final clean state
            foreach (var elem in elements)
            {
                elem.Opacity = 1.0;
                if (elem.RenderTransform is TranslateTransform t)
                {
                    t.Y = 0.0;
                }
            }
        };

        sb.Begin();
    }

    private static IReadOnlyList<FrameworkElement> GetElementsToAnimate(Page page)
    {
        if (page is IStaggeredPage staggeredPage)
        {
            var list = staggeredPage.GetStaggeredElements();
            if (list != null && list.Count > 0)
            {
                return list;
            }
        }

        // Fallback: discover top-level card-like elements
        var discovered = new List<FrameworkElement>();
        if (page.Content is Panel rootPanel)
        {
            foreach (var child in rootPanel.Children)
            {
                if (child is FrameworkElement fe && fe.Visibility == Visibility.Visible)
                {
                    discovered.Add(fe);
                }
            }
        }
        else if (page.Content is ScrollViewer sv && sv.Content is Panel svPanel)
        {
            foreach (var child in svPanel.Children)
            {
                if (child is FrameworkElement fe && fe.Visibility == Visibility.Visible)
                {
                    discovered.Add(fe);
                }
            }
        }

        return discovered;
    }
}
