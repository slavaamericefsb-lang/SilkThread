using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SilkThread.App.Services;
using SilkThread.App.ViewModels;
using SilkThread.Core.Models;

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
        PageTransitionHelper.AnimatePageEntrance(this);
        await ViewModel.InitializeAsync();
    }

    private async void RollbackButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && (btn.DataContext is TweakAuditRecord record || btn.Tag is TweakAuditRecord recordTag && (record = recordTag) != null))
        {
            var loc = LocalizationService.Instance;
            var confirmDialog = new ContentDialog
            {
                Title = loc["Hist_Rollback_Dialog_Title"],
                Content = string.Format(loc["Hist_Rollback_Dialog_Content"], record.TweakName),
                PrimaryButtonText = loc["Hist_Rollback_Action"],
                CloseButtonText = loc["Hist_Dialog_Cancel"],
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.RollbackRecordAsync(record);
            }
        }
    }

    private async void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
    {
        var loc = LocalizationService.Instance;
        var confirmDialog = new ContentDialog
        {
            Title = loc["Hist_Clear_Dialog_Title"],
            Content = loc["Hist_Clear_Dialog_Content"],
            PrimaryButtonText = loc["Opt_Clear_Logs"],
            CloseButtonText = loc["Hist_Dialog_Cancel"],
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };

        var result = await confirmDialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.ClearHistoryAsync();
        }
    }
}
