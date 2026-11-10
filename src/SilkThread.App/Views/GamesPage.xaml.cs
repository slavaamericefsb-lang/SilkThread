using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SilkThread.App.Services;
using SilkThread.App.ViewModels;

namespace SilkThread.App.Views;

public sealed partial class GamesPage : Page, IStaggeredPage
{
    public GamesViewModel ViewModel { get; }

    public IReadOnlyList<FrameworkElement> GetStaggeredElements()
    {
        return new FrameworkElement[]
        {
            HeaderGrid,
            FilterGrid,
            GameCardsControl,
            DetailCard
        };
    }

    public GamesPage()
    {
        ViewModel = App.Services.GetRequiredService<GamesViewModel>();
        InitializeComponent();

        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(GamesViewModel.IsCopied) && ViewModel.IsCopied)
            {
                AnimateCopyButtonSpring();
            }
        };
    }

    private void CopyLaunchOptionsButton_Click(object sender, RoutedEventArgs e)
    {
        AnimateCopyButtonSpring();
    }

    private void AnimateCopyButtonSpring()
    {
        if (CopyButtonScale == null) return;

        var sb = new Microsoft.UI.Xaml.Media.Animation.Storyboard();

        var animX = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimationUsingKeyFrames();
        animX.KeyFrames.Add(new Microsoft.UI.Xaml.Media.Animation.DiscreteDoubleKeyFrame { KeyTime = Microsoft.UI.Xaml.Media.Animation.KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 0.8 });
        animX.KeyFrames.Add(new Microsoft.UI.Xaml.Media.Animation.SplineDoubleKeyFrame
        {
            KeyTime = Microsoft.UI.Xaml.Media.Animation.KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(160)),
            Value = 1.12,
            KeySpline = new Microsoft.UI.Xaml.Media.Animation.KeySpline { ControlPoint1 = new Windows.Foundation.Point(0.2, 0.8), ControlPoint2 = new Windows.Foundation.Point(0.3, 1.0) }
        });
        animX.KeyFrames.Add(new Microsoft.UI.Xaml.Media.Animation.SplineDoubleKeyFrame
        {
            KeyTime = Microsoft.UI.Xaml.Media.Animation.KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300)),
            Value = 0.97,
            KeySpline = new Microsoft.UI.Xaml.Media.Animation.KeySpline { ControlPoint1 = new Windows.Foundation.Point(0.2, 0.8), ControlPoint2 = new Windows.Foundation.Point(0.3, 1.0) }
        });
        animX.KeyFrames.Add(new Microsoft.UI.Xaml.Media.Animation.SplineDoubleKeyFrame
        {
            KeyTime = Microsoft.UI.Xaml.Media.Animation.KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(420)),
            Value = 1.0
        });

        var animY = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimationUsingKeyFrames();
        animY.KeyFrames.Add(new Microsoft.UI.Xaml.Media.Animation.DiscreteDoubleKeyFrame { KeyTime = Microsoft.UI.Xaml.Media.Animation.KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 0.8 });
        animY.KeyFrames.Add(new Microsoft.UI.Xaml.Media.Animation.SplineDoubleKeyFrame
        {
            KeyTime = Microsoft.UI.Xaml.Media.Animation.KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(160)),
            Value = 1.12,
            KeySpline = new Microsoft.UI.Xaml.Media.Animation.KeySpline { ControlPoint1 = new Windows.Foundation.Point(0.2, 0.8), ControlPoint2 = new Windows.Foundation.Point(0.3, 1.0) }
        });
        animY.KeyFrames.Add(new Microsoft.UI.Xaml.Media.Animation.SplineDoubleKeyFrame
        {
            KeyTime = Microsoft.UI.Xaml.Media.Animation.KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300)),
            Value = 0.97,
            KeySpline = new Microsoft.UI.Xaml.Media.Animation.KeySpline { ControlPoint1 = new Windows.Foundation.Point(0.2, 0.8), ControlPoint2 = new Windows.Foundation.Point(0.3, 1.0) }
        });
        animY.KeyFrames.Add(new Microsoft.UI.Xaml.Media.Animation.SplineDoubleKeyFrame
        {
            KeyTime = Microsoft.UI.Xaml.Media.Animation.KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(420)),
            Value = 1.0
        });

        Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(animX, CopyButtonScale);
        Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(animX, "ScaleX");
        Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(animY, CopyButtonScale);
        Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(animY, "ScaleY");

        sb.Children.Add(animX);
        sb.Children.Add(animY);
        sb.Begin();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        PageTransitionHelper.AnimatePageEntrance(this);
    }

    private void BtnFilterAll_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SetFilterCategory("all");
    }

    private void BtnFilterShooters_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SetFilterCategory("shooter");
    }

    private void BtnFilterMoba_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SetFilterCategory("moba");
    }

    private void BtnFilterBattleRoyale_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SetFilterCategory("battleroyale");
    }

    private void GameCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: GameCardViewModel card })
        {
            ViewModel.SelectGame(card);
        }
    }

    private void CopyConfigPath_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string path })
        {
            _ = ViewModel.CopyConfigPathAsync(path);
        }
    }
}
