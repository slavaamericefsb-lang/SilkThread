using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
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

    public TweaksPage()
    {
        ViewModel = App.Services.GetRequiredService<TweaksViewModel>();
        InitializeComponent();
        CategoriesScrollViewer.AddHandler(UIElement.PointerWheelChangedEvent, new PointerEventHandler(CategoriesScrollViewer_PointerWheelChanged), true);
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
        var targetOffset = Math.Max(0, CategoriesScrollViewer.HorizontalOffset - 220);
        CategoriesScrollViewer.ChangeView(targetOffset, null, null, false);
    }

    private void ScrollCategoriesRight_Click(object sender, RoutedEventArgs e)
    {
        var targetOffset = CategoriesScrollViewer.HorizontalOffset + 220;
        CategoriesScrollViewer.ChangeView(targetOffset, null, null, false);
    }

    private void ClearSearch_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SearchQuery = string.Empty;
        (FindName("SearchInput") as TextBox)?.Focus(FocusState.Programmatic);
    }

    private void CategoriesScrollViewer_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var props = e.GetCurrentPoint(CategoriesScrollViewer).Properties;
        var delta = props.MouseWheelDelta;
        if (delta != 0)
        {
            // Translate vertical mouse wheel into horizontal scroll:
            // Vertical wheel down (delta < 0) -> scroll right (positive horizontal offset)
            // Vertical wheel up (delta > 0) -> scroll left (negative horizontal offset)
            // If native horizontal wheel: delta > 0 is scroll right
            double step = props.IsHorizontalMouseWheel ? (delta * 0.8) : (-delta * 0.8);
            var targetOffset = Math.Max(0, CategoriesScrollViewer.HorizontalOffset + step);
            CategoriesScrollViewer.ChangeView(targetOffset, null, null, false);
            e.Handled = true;
        }
    }
}
