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

        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        Unloaded += GamesPage_Unloaded;
    }

    private void GameCards_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (GameCardsControl.ItemsPanelRoot is not ItemsWrapGrid panel || e.NewSize.Width <= 0) return;
        int columns = Math.Clamp((int)(e.NewSize.Width / 260), 1, 4);
        panel.MaximumRowsOrColumns = columns;
        panel.ItemWidth = Math.Floor(e.NewSize.Width / columns);
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GamesViewModel.IsCopied) && ViewModel.IsCopied)
        {
            DispatcherQueue?.TryEnqueue(() =>
            {
                try { AnimateCopyButtonSpring(); }
                catch (Exception ex) { App.Log($"[GamesPage AnimateCopy exception]: {ex.Message}"); }
            });
        }
    }

    private void GamesPage_Unloaded(object sender, RoutedEventArgs e)
    {
        try
        {
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
        catch (Exception ex)
        {
            App.Log($"[GamesPage_Unloaded exception]: {ex.Message}");
        }
    }

    private void CopyLaunchOptionsButton_Click(object sender, RoutedEventArgs e)
    {
        AnimateCopyButtonSpring();
    }

    private void AnimateCopyButtonSpring()
    {
        // Native pressed/hover feedback stays crisp; no scaling of glyphs or text.
        if (CopyButtonScale == null) return;
        CopyButtonScale.ScaleX = CopyButtonScale.ScaleY = 1;
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
