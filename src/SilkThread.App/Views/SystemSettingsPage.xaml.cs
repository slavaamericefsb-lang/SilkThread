using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SilkThread.App.Services;
using SilkThread.App.ViewModels;
using Windows.ApplicationModel.DataTransfer;

namespace SilkThread.App.Views;

public sealed partial class SystemSettingsPage : Page, IStaggeredPage
{
    public SystemSettingsViewModel ViewModel { get; }
    public IReadOnlyList<FrameworkElement> GetStaggeredElements() => new FrameworkElement[] { HeaderPanel, Workspace };
    public SystemSettingsPage()
    {
        ViewModel = App.Services.GetRequiredService<SystemSettingsViewModel>();
        InitializeComponent();
    }
    private bool? _compact;
    private void Workspace_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        bool compact = e.NewSize.Width < 1000;
        if (_compact == compact) return;
        _compact = compact;
        InspectorColumn.Width = new GridLength(compact ? 0 : 280);
        Grid.SetColumn(InspectorPanel, compact ? 0 : 1);
        Grid.SetRow(InspectorPanel, compact ? 1 : 0);
        InspectorPanel.MaxHeight = compact ? 140 : double.PositiveInfinity;
        InspectorPanel.Margin = compact ? new Thickness(0, 12, 0, 0) : new Thickness(0, 48, 0, 0);
    }
    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        PageTransitionHelper.AnimatePageEntrance(this);
        await RefreshAsync();
    }
    private async Task RefreshAsync()
    {
        try { ViewModel.ActionStatus = ""; await Task.WhenAll(ViewModel.RefreshPowerAsync(), ViewModel.RefreshMemoryAsync(), ViewModel.RefreshDiagnosticsAsync()); }
        catch (Exception ex) { App.Log($"[System settings refresh]: {ex}"); }
    }
    private async void RefreshAll_Click(object sender, RoutedEventArgs e) => await RefreshAsync();
    private async void RestorePower_Click(object sender, RoutedEventArgs e) => await ViewModel.RestorePowerAsync();
    private void Discard_Click(object sender, RoutedEventArgs e) { ViewModel.DiscardMemoryDraft(); ViewModel.DiscardPowerDraft(); }
    private async void ReviewSelection_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var loc = LocalizationService.Instance;
            ViewModel.SelectDraftForReview();
            if (ViewModel.ActiveTab == 0)
            {
                var preview = await ViewModel.ReviewPowerAsync(); if (preview == null) return;
                if (await ConfigurationReviewDialog.ShowAsync(XamlRoot, loc["Power_Review"],
                    $"{preview.OriginalName}\n{preview.OriginalId}\n↓\n{preview.TargetName}\n{preview.TargetId}\n\n{loc["Power_Notice"]}", loc["Power_Apply"]))
                    await ViewModel.ApplyPowerAsync(preview);
            }
            else if (ViewModel.ActiveTab == 1)
            {
                var preview = await ViewModel.ReviewPageFilesAsync(); if (preview == null) return;
                if (await ConfigurationReviewDialog.ShowAsync(XamlRoot, loc["Page_Review"], PageFileReviewText.Apply(preview, loc) + "\n\n" + ViewModel.MemoryReport, loc["Page_Apply"]))
                    await ViewModel.ApplyPageFilesAsync(preview);
            }
        }
        catch (Exception ex) { App.Log($"[System settings review]: {ex}"); }
    }
    private async void RestoreMemory_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var review = await ViewModel.PreviewPageFileRestoreAsync(); if (review == null) return;
            var loc = LocalizationService.Instance;
            if (await ConfigurationReviewDialog.ShowAsync(XamlRoot, loc["Page_Restore"], PageFileReviewText.Restore(review, loc),
                loc[review.Conflict ? "Page_OverwriteRestore" : "Page_Restore"]))
                await ViewModel.RestorePageFilesAsync(review, review.Conflict);
        }
        catch (Exception ex) { App.Log($"[Page file restore dialog]: {ex}"); }
    }
    private void CopyReport_Click(object sender, RoutedEventArgs e)
    {
        try { var package = new DataPackage(); package.SetText(ViewModel.GetSystemReport()); Clipboard.SetContent(package); ViewModel.ActionStatus = LocalizationService.Instance["System_Copied"]; }
        catch (Exception ex) { App.Log($"[System report clipboard]: {ex}"); ViewModel.ActionStatus = LocalizationService.Instance["System_ActionFailed"]; }
    }
    private async void GraphicsSettings_Click(object sender, RoutedEventArgs e)
    {
        try { if (!await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:display-advancedgraphics"))) ViewModel.ActionStatus = LocalizationService.Instance["System_ActionFailed"]; }
        catch (Exception ex) { App.Log($"[Graphics settings link]: {ex}"); ViewModel.ActionStatus = LocalizationService.Instance["System_ActionFailed"]; }
    }
    private void HagsTweak_Click(object sender, RoutedEventArgs e) => ViewModel.OpenHagsTweak();
}
