using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SilkThread.App.Services;
using SilkThread.App.ViewModels;
using SilkThread.Core.Models;
using SilkThread.Core.SystemOperations;

namespace SilkThread.App.Views;

public sealed partial class HistoryPage : Page, IStaggeredPage
{
    public HistoryViewModel ViewModel { get; }

    public IReadOnlyList<FrameworkElement> GetStaggeredElements()
    {
        return new FrameworkElement[]
        {
            HeaderGrid,
            HistoryCard
        };
    }

    public HistoryPage()
    {
        ViewModel = App.Services.GetRequiredService<HistoryViewModel>();
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
            App.Log($"[HistoryPage.Page_Loaded exception]: {ex.Message}");
        }
    }

    private async void RollbackButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is Button btn && (btn.DataContext is TweakAuditRecord record || btn.Tag is TweakAuditRecord recordTag && (record = recordTag) != null))
            {
                var loc = LocalizationService.Instance;
                var xamlRoot = this.XamlRoot ?? App.MainWindowInstance?.Content?.XamlRoot;
                if (xamlRoot == null) return;

                if (record.TweakId == PageFileService.Resource)
                {
                    var service = App.Services.GetRequiredService<IPageFileService>();
                    var review = await service.PreviewRestoreAsync(record.Id);
                    if (review != null && await ConfigurationReviewDialog.ShowAsync(xamlRoot, loc["Page_Restore"],
                        PageFileReviewText.Restore(review, loc), loc[review.Conflict ? "Page_OverwriteRestore" : "Page_Restore"]))
                    {
                        var operation = await service.RestoreAsync(review, review.Conflict);
                        ViewModel.StatusMessage = loc["Page_Result_" + operation.Status];
                        await ViewModel.LoadHistoryAsync();
                    }
                    return;
                }

                var confirmDialog = new ContentDialog
                {
                    Title = loc["Hist_Rollback_Dialog_Title"],
                    Content = string.Format(loc["Hist_Rollback_Dialog_Content"], record.TweakName),
                    PrimaryButtonText = loc["Hist_Rollback_Action"],
                    CloseButtonText = loc["Hist_Dialog_Cancel"],
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = xamlRoot
                };

                var result = await confirmDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    await ViewModel.RollbackRecordAsync(record);
                }
            }
        }
        catch (Exception ex)
        {
            App.Log($"[HistoryPage.RollbackButton_Click exception]: {ex.Message}");
        }
    }

    private async void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var loc = LocalizationService.Instance;
            var xamlRoot = this.XamlRoot ?? App.MainWindowInstance?.Content?.XamlRoot;
            if (xamlRoot == null) return;

            var confirmDialog = new ContentDialog
            {
                Title = loc["Hist_Clear_Dialog_Title"],
                Content = loc["Hist_Clear_Dialog_Content"],
                PrimaryButtonText = loc["Opt_Clear_Logs"],
                CloseButtonText = loc["Hist_Dialog_Cancel"],
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = xamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.ClearHistoryAsync();
            }
        }
        catch (Exception ex)
        {
            App.Log($"[HistoryPage.ClearHistoryButton_Click exception]: {ex.Message}");
        }
    }
}
