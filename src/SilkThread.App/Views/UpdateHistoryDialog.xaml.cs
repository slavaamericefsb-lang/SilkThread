using System;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SilkThread.App.Services;

namespace SilkThread.App.Views;

public sealed partial class UpdateHistoryDialog : ContentDialog, INotifyPropertyChanged
{
    private readonly ILocalizationService _loc;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string DialogTitle => _loc["UpdHist_DialogTitle"];
    public string DialogSubtitle => _loc["UpdHist_DialogSubtitle"];
    public string CloseText => _loc["UpdHist_Close"];
    public string LatestBadgeText => _loc["UpdHist_Badge_Latest"];

    public string V20_Title => _loc["UpdHist_v20_Title"];
    public string V20_Date => _loc["UpdHist_v20_Date"];
    public string V20_Item1 => _loc["UpdHist_v20_Item1"];
    public string V20_Item2 => _loc["UpdHist_v20_Item2"];
    public string V20_Item3 => _loc["UpdHist_v20_Item3"];
    public string V20_Item4 => _loc["UpdHist_v20_Item4"];
    public string V20_Item5 => _loc["UpdHist_v20_Item5"];

    public string V15_Title => _loc["UpdHist_v15_Title"];
    public string V15_Date => _loc["UpdHist_v15_Date"];
    public string V15_Item1 => _loc["UpdHist_v15_Item1"];
    public string V15_Item2 => _loc["UpdHist_v15_Item2"];
    public string V15_Item3 => _loc["UpdHist_v15_Item3"];

    public string V10_Title => _loc["UpdHist_v10_Title"];
    public string V10_Date => _loc["UpdHist_v10_Date"];
    public string V10_Item1 => _loc["UpdHist_v10_Item1"];
    public string V10_Item2 => _loc["UpdHist_v10_Item2"];
    public string V10_Item3 => _loc["UpdHist_v10_Item3"];

    public UpdateHistoryDialog()
    {
        _loc = LocalizationService.Instance;
        RequestedTheme = ElementTheme.Dark;

        _loc.LanguageChanged += OnLanguageChanged;
        this.Closed += (s, e) => _loc.LanguageChanged -= OnLanguageChanged;

        InitializeComponent();
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }
}
