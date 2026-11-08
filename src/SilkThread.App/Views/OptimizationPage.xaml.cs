using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SilkThread.App.Services;
using SilkThread.App.ViewModels;

namespace SilkThread.App.Views;

public sealed partial class OptimizationPage : Page, IStaggeredPage
{
    public OptimizationViewModel ViewModel { get; }

    public IReadOnlyList<FrameworkElement> GetStaggeredElements()
    {
        return new FrameworkElement[]
        {
            HeaderGrid,
            CardProfiles,
            CardExecution,
            CardTerminal
        };
    }

    public OptimizationPage()
    {
        ViewModel = App.Services.GetRequiredService<OptimizationViewModel>();
        InitializeComponent();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        PageTransitionHelper.AnimatePageEntrance(this);
        await ViewModel.InitializeAsync();
    }
}
