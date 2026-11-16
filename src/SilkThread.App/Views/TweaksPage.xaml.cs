using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SilkThread.App.Services;
using SilkThread.App.ViewModels;

namespace SilkThread.App.Views;

public sealed partial class TweaksPage : Page, IStaggeredPage
{
    private string? _requestedTweak;
    public TweaksViewModel ViewModel { get; }

    public TweaksPage()
    {
        ViewModel = App.Services.GetRequiredService<TweaksViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _requestedTweak = e.Parameter as string;
    }

    public IReadOnlyList<FrameworkElement> GetStaggeredElements() =>
        new FrameworkElement[] { HeaderGrid, CategoriesPanel, LeftPanelBorder, RightPanelBorder };

    private void WorkspaceGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // The normal 1280-pixel window has room for all three panes. At higher
        // display scaling the category rail remains visible and details move below the list.
        bool compact = e.NewSize.Width < 1000;
        CategoriesColumn.Width = new GridLength(196);
        BrowserColumn.Width = new GridLength(compact ? 1 : 0.95, GridUnitType.Star);
        InspectorColumn.Width = compact ? new GridLength(0) : new GridLength(1.15, GridUnitType.Star);
        InspectorRow.Height = compact ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        WorkspaceGrid.RowSpacing = compact ? 14 : 0;
        Grid.SetRowSpan(CategoriesPanel, compact ? 2 : 1);
        Grid.SetColumn(RightPanelBorder, compact ? 1 : 2);
        Grid.SetRow(RightPanelBorder, compact ? 1 : 0);
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            PageTransitionHelper.AnimatePageEntrance(this);
            await ViewModel.InitializeAsync();
            if (_requestedTweak != null) await ViewModel.FocusTweakAsync(_requestedTweak);
        }
        catch (Exception ex)
        {
            App.Log($"[TweaksPage.Page_Loaded exception]: {ex.Message}");
        }
    }
}
