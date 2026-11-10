using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SilkThread.App.Services;
using SilkThread.App.ViewModels;
using SilkThread.Core.Models;

namespace SilkThread.App.Views;

public sealed partial class RestoreBackupPage : Page, IStaggeredPage
{
    public RestoreBackupViewModel ViewModel { get; }

    public IReadOnlyList<FrameworkElement> GetStaggeredElements()
    {
        return new FrameworkElement[]
        {
            HeaderPanel,
            CardProtection,
            CardCreate,
            CardTimeline
        };
    }

    public RestoreBackupPage()
    {
        ViewModel = App.Services.GetRequiredService<RestoreBackupViewModel>();
        InitializeComponent();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        PageTransitionHelper.AnimatePageEntrance(this);
        await ViewModel.InitializeAsync();
    }

    private async void RollbackPointButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && (btn.DataContext is RestorePointItem point || btn.Tag is RestorePointItem pointTag && (point = pointTag) != null))
        {
            var loc = LocalizationService.Instance;
            var confirmDialog = new ContentDialog
            {
                Title = loc["Rest_Rollback_Dialog_Title"],
                Content = string.Format(loc["Rest_Rollback_Dialog_Content"], point.Description, point.SequenceNumber),
                PrimaryButtonText = loc["Rest_Rollback_Dialog_Btn"],
                CloseButtonText = loc["Rest_Dialog_Cancel"],
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.RollbackRestorePointAsync(point);
            }
        }
    }

    private async void DeletePointButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && (btn.DataContext is RestorePointItem point || btn.Tag is RestorePointItem pointTag && (point = pointTag) != null))
        {
            var loc = LocalizationService.Instance;
            var confirmDialog = new ContentDialog
            {
                Title = loc["Rest_Delete_Dialog_Title"],
                Content = string.Format(loc["Rest_Delete_Dialog_Content"], point.Description, point.SequenceNumber),
                PrimaryButtonText = loc["Rest_Delete_Dialog_Btn"],
                CloseButtonText = loc["Rest_Dialog_Cancel"],
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.DeleteRestorePointAsync(point);
            }
        }
    }

    private void AnchorPulseRing_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe)
        {
            var sb = new Microsoft.UI.Xaml.Media.Animation.Storyboard 
            { 
                RepeatBehavior = Microsoft.UI.Xaml.Media.Animation.RepeatBehavior.Forever, 
                AutoReverse = true 
            };

            var scaleX = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
            {
                From = 1.0,
                To = 1.15,
                Duration = new Duration(TimeSpan.FromSeconds(1.5)),
                EasingFunction = new Microsoft.UI.Xaml.Media.Animation.SineEase { EasingMode = Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseInOut }
            };
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(scaleX, fe);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(scaleX, "(UIElement.RenderTransform).(ScaleTransform.ScaleX)");

            var scaleY = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
            {
                From = 1.0,
                To = 1.15,
                Duration = new Duration(TimeSpan.FromSeconds(1.5)),
                EasingFunction = new Microsoft.UI.Xaml.Media.Animation.SineEase { EasingMode = Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseInOut }
            };
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(scaleY, fe);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(scaleY, "(UIElement.RenderTransform).(ScaleTransform.ScaleY)");

            var opacity = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
            {
                From = 0.85,
                To = 0.25,
                Duration = new Duration(TimeSpan.FromSeconds(1.5)),
                EasingFunction = new Microsoft.UI.Xaml.Media.Animation.SineEase { EasingMode = Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseInOut }
            };
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(opacity, fe);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(opacity, "Opacity");

            sb.Children.Add(scaleX);
            sb.Children.Add(scaleY);
            sb.Children.Add(opacity);
            sb.Begin();
        }
    }
}
