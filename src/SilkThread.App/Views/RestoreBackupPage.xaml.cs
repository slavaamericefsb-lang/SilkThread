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
}
