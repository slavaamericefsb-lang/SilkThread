using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using SilkThread.App.Helpers;
using SilkThread.App.Services;
using SilkThread.App.ViewModels;

namespace SilkThread.App.Views;

public sealed partial class TweaksPage : Page, IStaggeredPage
{
    public TweaksViewModel ViewModel { get; }

    public IReadOnlyList<FrameworkElement> GetStaggeredElements()
    {
        return new FrameworkElement[]
        {
            HeaderGrid,
            LeftPanelBorder,
            RightPanelBorder
        };
    }

    private TweakItemViewModel? _hookedTweak;

    public TweaksPage()
    {
        ViewModel = App.Services.GetRequiredService<TweaksViewModel>();
        InitializeComponent();

        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(TweaksViewModel.SelectedTweak))
            {
                HookSelectedTweak(ViewModel.SelectedTweak);
            }
        };
        HookSelectedTweak(ViewModel.SelectedTweak);
    }

    private void HookSelectedTweak(TweakItemViewModel? tweak)
    {
        if (_hookedTweak != null)
        {
            _hookedTweak.PropertyChanged -= SelectedTweak_PropertyChanged;
        }
        _hookedTweak = tweak;
        if (_hookedTweak != null)
        {
            _hookedTweak.PropertyChanged += SelectedTweak_PropertyChanged;
        }
    }

    private void SelectedTweak_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TweakItemViewModel.Status))
        {
            AnimateStatusBadgePop();
        }
    }

    private void AnimateStatusBadgePop()
    {
        if (InspectorStatusBadgeScale == null) return;

        var sb = new Storyboard();
        var animX = new DoubleAnimationUsingKeyFrames();
        animX.KeyFrames.Add(new DiscreteDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 0.82 });
        animX.KeyFrames.Add(new SplineDoubleKeyFrame
        {
            KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(130)),
            Value = 1.18,
            KeySpline = new KeySpline { ControlPoint1 = new Windows.Foundation.Point(0.2, 0.8), ControlPoint2 = new Windows.Foundation.Point(0.3, 1.0) }
        });
        animX.KeyFrames.Add(new SplineDoubleKeyFrame
        {
            KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(240)),
            Value = 0.96,
            KeySpline = new KeySpline { ControlPoint1 = new Windows.Foundation.Point(0.2, 0.8), ControlPoint2 = new Windows.Foundation.Point(0.3, 1.0) }
        });
        animX.KeyFrames.Add(new SplineDoubleKeyFrame
        {
            KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(340)),
            Value = 1.0
        });

        var animY = new DoubleAnimationUsingKeyFrames();
        animY.KeyFrames.Add(new DiscreteDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 0.82 });
        animY.KeyFrames.Add(new SplineDoubleKeyFrame
        {
            KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(130)),
            Value = 1.18,
            KeySpline = new KeySpline { ControlPoint1 = new Windows.Foundation.Point(0.2, 0.8), ControlPoint2 = new Windows.Foundation.Point(0.3, 1.0) }
        });
        animY.KeyFrames.Add(new SplineDoubleKeyFrame
        {
            KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(240)),
            Value = 0.96,
            KeySpline = new KeySpline { ControlPoint1 = new Windows.Foundation.Point(0.2, 0.8), ControlPoint2 = new Windows.Foundation.Point(0.3, 1.0) }
        });
        animY.KeyFrames.Add(new SplineDoubleKeyFrame
        {
            KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(340)),
            Value = 1.0
        });

        Storyboard.SetTarget(animX, InspectorStatusBadgeScale);
        Storyboard.SetTargetProperty(animX, "ScaleX");
        Storyboard.SetTarget(animY, InspectorStatusBadgeScale);
        Storyboard.SetTargetProperty(animY, "ScaleY");

        sb.Children.Add(animX);
        sb.Children.Add(animY);
        sb.Begin();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        PageTransitionHelper.AnimatePageEntrance(this);
        await ViewModel.InitializeAsync();
    }

    private void CategoryButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is CategoryTabItem item)
        {
            ViewModel.SelectedCategory = item;
        }
    }

    private void TweaksListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView lv && lv.SelectedItem is TweakItemViewModel item)
        {
            ViewModel.SelectedTweak = item;
        }
    }

    private void ScrollCategoriesLeft_Click(object sender, RoutedEventArgs e)
    {
        SmoothScroll.ScrollBy(CategoriesScrollViewer, -240, 0);
    }

    private void ScrollCategoriesRight_Click(object sender, RoutedEventArgs e)
    {
        SmoothScroll.ScrollBy(CategoriesScrollViewer, 240, 0);
    }

    private void ClearSearch_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SearchQuery = string.Empty;
        (FindName("SearchInput") as TextBox)?.Focus(FocusState.Programmatic);
    }
}
