using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SilkThread.App.Services;
using SilkThread.App.ViewModels;

namespace SilkThread.App.Views;

public sealed partial class PcScannerPage : Page, IStaggeredPage
{
    public PcScannerViewModel ViewModel { get; }

    public IReadOnlyList<FrameworkElement> GetStaggeredElements()
    {
        return new FrameworkElement[]
        {
            HeaderGrid,
            CardCpu,
            CardRam,
            CardGpu,
            CardOs,
            CardStorage
        };
    }

    public PcScannerPage()
    {
        ViewModel = App.Services.GetRequiredService<PcScannerViewModel>();
        InitializeComponent();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        PageTransitionHelper.AnimatePageEntrance(this);
        await ViewModel.InitializeAsync();
    }

    private async void ScanButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.ScanAsync();
    }

    private void CopyReportButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CopyReport();
    }
}
