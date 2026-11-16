using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SilkThread.App.Services;
using SilkThread.App.ViewModels;
namespace SilkThread.App.Views;
public sealed partial class PcScannerPage : Page, IStaggeredPage
{
    public PcScannerViewModel ViewModel { get; }
    public IReadOnlyList<FrameworkElement> GetStaggeredElements() => new FrameworkElement[] { HeaderGrid, CardCpu, CardRam, CardGpu, CardOs, CardStorage };
    public PcScannerPage() { ViewModel = App.Services.GetRequiredService<PcScannerViewModel>(); InitializeComponent(); }
    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        PageTransitionHelper.AnimatePageEntrance(this);
        try { await ViewModel.InitializeAsync(); }
        catch (Exception ex) { App.Log($"[Scanner initialization]: {ex}"); }
    }
    private async void ScanButton_Click(object sender, RoutedEventArgs e)
    {
        try { await ViewModel.ScanAsync(); }
        catch (Exception ex) { App.Log($"[Scanner refresh]: {ex}"); }
    }
    private void CopyReportButton_Click(object sender, RoutedEventArgs e)
    {
        try { ViewModel.CopyReport(); }
        catch (Exception ex) { App.Log($"[Scanner report]: {ex}"); }
    }

    private bool? _compactLayout;
    private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (HardwareGrid == null) return;
        bool compact = e.NewSize.Width < 1000;
        if (_compactLayout == compact) return;
        _compactLayout = compact;
        HardwareGrid.ColumnDefinitions[1].Width = compact ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        FrameworkElement[] cards = { CardCpu, CardRam, CardGpu, CardOs, CardStorage };
        for (int i = 0; i < cards.Length; i++)
        {
            Grid.SetRow(cards[i], compact ? i : i / 2);
            Grid.SetColumn(cards[i], compact || i == 4 ? 0 : i % 2);
            Grid.SetColumnSpan(cards[i], !compact && i == 4 ? 2 : 1);
        }
    }
}
