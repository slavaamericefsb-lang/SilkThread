using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using SilkThread.App.Helpers;

namespace SilkThread.App.Services;

public interface IStaggeredPage
{
    IReadOnlyList<FrameworkElement> GetStaggeredElements();
}

/// <summary>Section entrance fades without changing layout or text transforms.</summary>
public static class PageTransitionHelper
{
    private static readonly ConditionalWeakTable<Page, PageMotion> States = new();

    public static Task TransitionNavigateAsync(Frame frame, Type pageType, object? parameter = null)
    {
        if (frame == null || frame.CurrentSourcePageType == pageType && parameter == null)
            return Task.CompletedTask;
        if (frame.Content is Page previous) Stop(previous);
        frame.Navigate(pageType, parameter, new SuppressNavigationTransitionInfo());
        if (frame.Content is Page next) AnimatePageEntrance(next);
        return Task.CompletedTask;
    }

    public static Task AnimatePageExitAsync(Page page) => Animate(page, entrance: false);

    public static void AnimatePageEntrance(Page page)
    {
        if (page == null || !page.IsLoaded) return;
        var state = States.GetOrCreateValue(page);
        // Loaded and the navigation callback may arrive together. Do not replay an active entrance.
        if (state.Entrance && state.Storyboard != null) return;
        _ = Animate(page, entrance: true);
    }

    private static Task Animate(Page page, bool entrance)
    {
        Stop(page);
        if (!page.IsLoaded || !MotionPreferences.AnimationsEnabled) return Task.CompletedTask;
        var state = States.GetOrCreateValue(page);
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var storyboard = new Storyboard();
        state.Entrance = entrance;
        state.Storyboard = storyboard;
        if (entrance && page is IStaggeredPage sections)
        {
            int index = 0;
            foreach (var element in sections.GetStaggeredElements())
            {
                if (element.Visibility != Visibility.Visible) continue;
                Add(storyboard, element, "Opacity", 0,
                    element.Opacity, MotionPreferences.EntranceMilliseconds, Math.Min(index++, 12) * 55);
            }
        }
        if (storyboard.Children.Count == 0)
            Add(storyboard, page, "Opacity", entrance ? page.Opacity * 0.65 : page.Opacity,
                entrance ? page.Opacity : 0, entrance ? MotionPreferences.EntranceMilliseconds : MotionPreferences.ExitMilliseconds, 0);
        state.Finish = () =>
        {
            storyboard.Stop();
            storyboard.Children.Clear();
            page.Unloaded -= Page_Unloaded;
            state.Storyboard = null;
            state.Finish = null;
            completion.TrySetResult(true);
        };
        page.Unloaded += Page_Unloaded;
        storyboard.Completed += (_, _) =>
        {
            if (ReferenceEquals(state.Storyboard, storyboard)) state.Finish?.Invoke();
        };
        storyboard.Begin();
        return completion.Task;
    }

    private static void Add(Storyboard storyboard, DependencyObject target, string property,
        double from, double to, int duration, int delay)
    {
        var animation = new DoubleAnimationUsingKeyFrames();
        animation.KeyFrames.Add(new DiscreteDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = from });
        animation.KeyFrames.Add(new DiscreteDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(delay)), Value = from });
        animation.KeyFrames.Add(new EasingDoubleKeyFrame
        {
            KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(delay + duration)), Value = to,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        });
        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, property);
        storyboard.Children.Add(animation);
    }

    private static void Page_Unloaded(object sender, RoutedEventArgs e) => Stop((Page)sender);
    private static void Stop(Page page)
    {
        if (States.TryGetValue(page, out var state)) state.Finish?.Invoke();
    }
    private sealed class PageMotion
    {
        public bool Entrance;
        public Storyboard? Storyboard;
        public Action? Finish;
    }

}
