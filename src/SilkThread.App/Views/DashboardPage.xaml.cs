using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SilkThread.App.Services;
using SilkThread.App.ViewModels;
namespace SilkThread.App.Views;
public sealed partial class DashboardPage : Page, IStaggeredPage
{
    public DashboardViewModel ViewModel { get; }
    public IReadOnlyList<FrameworkElement> GetStaggeredElements() => new FrameworkElement[] { HeaderGrid, SummaryCard, HardwareCard, AiCard, LatencyCard, ActionsCard };
    public DashboardPage() { ViewModel = App.Services.GetRequiredService<DashboardViewModel>(); InitializeComponent(); }
    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        PageTransitionHelper.AnimatePageEntrance(this);
        try { await ViewModel.InitializeAsync(); }
        catch (Exception ex) { App.Log($"[Dashboard initialization]: {ex}"); }
    }
    private void AiScenario_Click(object sender, RoutedEventArgs e) { if (sender is FrameworkElement { DataContext: AiSurveyOptionItem item }) ViewModel.SelectAiScenario(item); }
    private void AiPriority_Click(object sender, RoutedEventArgs e) { if (sender is FrameworkElement { DataContext: AiSurveyOptionItem item }) ViewModel.SelectAiPriority(item); }
    private void AiRisk_Click(object sender, RoutedEventArgs e) { if (sender is FrameworkElement { DataContext: AiSurveyOptionItem item }) ViewModel.SelectAiRisk(item); }

    private bool? _compactLogic;
    private bool? _compactHardware;
    private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (LogicGrid == null || HardwareTiles == null) return;
        bool compact = e.NewSize.Width < 1000;
        if (_compactLogic != compact)
        {
            _compactLogic = compact;
            LogicGrid.ColumnDefinitions[1].Width = compact ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
            Grid.SetColumn(LatencyCard, compact ? 0 : 1);
            Grid.SetRow(LatencyCard, compact ? 1 : 0);
        }
        bool hardwareCompact = e.NewSize.Width < 760;
        if (_compactHardware != hardwareCompact)
        {
            _compactHardware = hardwareCompact;
            HardwareTiles.ColumnDefinitions[1].Width = hardwareCompact ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
            for (int i = 0; i < HardwareTiles.Children.Count; i++)
            {
                if (HardwareTiles.Children[i] is not FrameworkElement tile) continue;
                Grid.SetColumn(tile, hardwareCompact || i == 4 ? 0 : i % 2);
                Grid.SetRow(tile, hardwareCompact ? i : i / 2);
                Grid.SetColumnSpan(tile, !hardwareCompact && i == 4 ? 2 : 1);
            }
        }
    }
}
