using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SilkThread.App.ViewModels;
using SilkThread.Core.PresetManager;
using SilkThread.Core.TweakEngine;

namespace SilkThread.App.Views;

public sealed partial class PresetReviewDialog : ContentDialog
{
    public PresetReviewViewModel ViewModel { get; }

    public int AppliedCount { get; private set; }
    public bool WasApplied { get; private set; }

    public PresetReviewDialog(EvaluatedPreset evaluated, ITweakEngine tweakEngine)
    {
        ViewModel = new PresetReviewViewModel(evaluated, tweakEngine);
        DataContext = ViewModel;
        InitializeComponent();
    }

    private void RejectButton_Click(object sender, RoutedEventArgs e)
    {
        WasApplied = false;
        AppliedCount = 0;
        Hide();
    }

    private async void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedCount == 0)
        {
            Hide();
            return;
        }

        int count = await ViewModel.ApplySelectedTweaksAsync();
        AppliedCount = count;
        WasApplied = true;
        Hide();
    }
}
