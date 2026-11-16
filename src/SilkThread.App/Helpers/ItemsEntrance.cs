using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Runtime.CompilerServices;
using Windows.Foundation.Collections;

namespace SilkThread.App.Helpers;

/// <summary>Fades realized rows in batches without touching layout or scrolling physics.</summary>
public static class ItemsEntrance
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(ItemsEntrance), new PropertyMetadata(false, Changed));
    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);
    private static readonly ConditionalWeakTable<ItemsControl, Motion> States = new();
    private static void Changed(DependencyObject obj, DependencyPropertyChangedEventArgs args)
    {
        if (obj is not ItemsControl control) return;
        if ((bool)args.NewValue) States.GetValue(control, c => new Motion(c)).Attach();
        else if (States.TryGetValue(control, out var state)) state.Detach();
    }
    private sealed class Motion(ItemsControl control)
    {
        private Storyboard? _animation;
        private bool _queued;
        private bool _attached;
        private bool _watching;
        public void Attach()
        {
            if (_attached) return;
            _attached = true;
            control.Loaded += Loaded;
            control.Unloaded += Unloaded;
            if (control.IsLoaded) Loaded(control, new RoutedEventArgs());
        }
        public void Detach()
        {
            Unloaded(control, new RoutedEventArgs());
            control.Loaded -= Loaded;
            control.Unloaded -= Unloaded;
            _attached = false;
        }
        private void Loaded(object sender, RoutedEventArgs args)
        {
            if (!_watching && control is ListViewBase list)
            {
                list.Items.VectorChanged += Changed;
                _watching = true;
            }
            Queue();
        }
        private void Changed(IObservableVector<object> sender, IVectorChangedEventArgs args) => Queue();
        private void Unloaded(object sender, RoutedEventArgs args)
        {
            if (_watching && control is ListViewBase list) list.Items.VectorChanged -= Changed;
            _watching = false;
            Stop();
        }
        private void Stop()
        {
            _animation?.Stop();
            _animation?.Children.Clear();
            _animation = null;
        }
        private void Queue()
        {
            if (_queued || !control.IsLoaded) return;
            _queued = true;
            control.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                _queued = false;
                Stop();
                if (!control.IsLoaded || !MotionPreferences.AnimationsEnabled) return;
                control.UpdateLayout();
                if (control.ItemsPanelRoot is not Panel panel) return;
                var storyboard = new Storyboard();
                int index = 0;
                foreach (var child in panel.Children)
                {
                    if (child is not FrameworkElement row || row.Visibility != Visibility.Visible || row.ActualHeight == 0) continue;
                    // Limit work to the first realized batch, preserving virtualization.
                    if (index >= 16) break;
                    int delay = index++ * 35;
                    var fade = new DoubleAnimationUsingKeyFrames();
                    fade.KeyFrames.Add(new DiscreteDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 0 });
                    fade.KeyFrames.Add(new DiscreteDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(delay)), Value = 0 });
                    fade.KeyFrames.Add(new EasingDoubleKeyFrame
                    {
                        KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(delay + 200)), Value = row.Opacity,
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    });
                    Storyboard.SetTarget(fade, row);
                    Storyboard.SetTargetProperty(fade, "Opacity");
                    storyboard.Children.Add(fade);
                }
                _animation = storyboard;
                storyboard.Completed += (_, _) => { if (ReferenceEquals(_animation, storyboard)) Stop(); };
                if (storyboard.Children.Count > 0) storyboard.Begin();
            });
        }
    }
}
