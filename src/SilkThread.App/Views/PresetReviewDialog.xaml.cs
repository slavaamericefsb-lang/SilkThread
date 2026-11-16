using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SilkThread.App.ViewModels;
using SilkThread.Core.PresetManager;
using SilkThread.Core.TweakEngine;

namespace SilkThread.App.Views;

public sealed partial class PresetReviewDialog : ContentDialog
{
    public PresetReviewViewModel ViewModel { get; }

    private readonly bool _selectionOnly;
    public bool WasConfirmed { get; private set; }
    public IReadOnlyList<string> SelectedTweakIds => ViewModel.Tweaks.Where(t => t.IsSelected && t.IsCompatible).Select(t => t.Id).ToList();

    public int AppliedCount { get; private set; }
    public bool WasApplied { get; private set; }

    public PresetReviewDialog(EvaluatedPreset evaluated, ITweakEngine tweakEngine, bool selectionOnly = false)
    {
        _selectionOnly = selectionOnly;
        ViewModel = new PresetReviewViewModel(evaluated, tweakEngine);
        DataContext = ViewModel;
        InitializeComponent();
        Closing += (_, args) => args.Cancel = ViewModel.IsApplying;
    }

    private void RejectButton_Click(object sender, RoutedEventArgs e)
    {
        WasApplied = false;
        AppliedCount = 0;
        Hide();
    }

    private async void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (ViewModel.SelectedCount == 0)
            {
                Hide();
                return;
            }

            if (_selectionOnly)
            {
                WasConfirmed = true;
                Hide();
                return;
            }

            int count = await ViewModel.ApplySelectedTweaksAsync();
            AppliedCount = count;
            WasApplied = true;
            Hide();
        }
        catch (Exception ex)
        {
            App.Log($"[PresetReviewDialog.ApplyButton_Click exception]: {ex.Message}");
            Hide();
        }
    }
}
