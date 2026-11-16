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
        try
        {
            PageTransitionHelper.AnimatePageEntrance(this);
            await ViewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            App.Log($"[RestoreBackupPage.Page_Loaded exception]: {ex.Message}");
        }
    }

    private async void RollbackPointButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is Button btn && (btn.DataContext is RestorePointItem point || btn.Tag is RestorePointItem pointTag && (point = pointTag) != null))
            {
                var loc = LocalizationService.Instance;
                var xamlRoot = this.XamlRoot ?? App.MainWindowInstance?.Content?.XamlRoot;
                if (xamlRoot == null) return;

                var confirmDialog = new ContentDialog
                {
                    Title = loc["Rest_Rollback_Dialog_Title"],
                    Content = string.Format(loc["Rest_Rollback_Dialog_Content"], point.Description, point.SequenceNumber),
                    PrimaryButtonText = loc["Rest_Rollback_Dialog_Btn"],
                    CloseButtonText = loc["Rest_Dialog_Cancel"],
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = xamlRoot
                };

                var result = await confirmDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    await ViewModel.RollbackRestorePointAsync(point);
                }
            }
        }
        catch (Exception ex)
        {
            App.Log($"[RestoreBackupPage.RollbackPointButton_Click exception]: {ex.Message}");
        }
    }

    private async void DeletePointButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is Button btn && (btn.DataContext is RestorePointItem point || btn.Tag is RestorePointItem pointTag && (point = pointTag) != null))
            {
                var loc = LocalizationService.Instance;
                var xamlRoot = this.XamlRoot ?? App.MainWindowInstance?.Content?.XamlRoot;
                if (xamlRoot == null) return;

                var confirmDialog = new ContentDialog
                {
                    Title = loc["Rest_Delete_Dialog_Title"],
                    Content = string.Format(loc["Rest_Delete_Dialog_Content"], point.Description, point.SequenceNumber),
                    PrimaryButtonText = loc["Rest_Delete_Dialog_Btn"],
                    CloseButtonText = loc["Rest_Dialog_Cancel"],
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = xamlRoot
                };

                var result = await confirmDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    await ViewModel.DeleteRestorePointAsync(point);
                }
            }
        }
        catch (Exception ex)
        {
            App.Log($"[RestoreBackupPage.DeletePointButton_Click exception]: {ex.Message}");
        }
    }

    private void AnchorPulseRing_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement ring) ring.Opacity = 0.35;
    }
}
