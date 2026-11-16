using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
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

        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        Unloaded += OptimizationPage_Unloaded;
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OptimizationViewModel.IsExecuting))
        {
            DispatcherQueue?.TryEnqueue(() =>
            {
                try { StartLivePulseAnimation(ViewModel.IsExecuting); }
                catch (Exception ex) { App.Log($"[OptimizationPage LivePulse exception]: {ex.Message}"); }
            });
        }
    }

    private void OptimizationPage_Unloaded(object sender, RoutedEventArgs e)
    {
        try
        {
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
        catch (Exception ex)
        {
            App.Log($"[OptimizationPage_Unloaded exception]: {ex.Message}");
        }
    }

    private void StartCursorBlink()
    {
        // Output is read-only; a blinking insertion cursor suggests an editable terminal.
        if (TerminalCursor != null) TerminalCursor.Opacity = 0;
    }

    private void StartLivePulseAnimation(bool isFast)
    {
        if (TerminalLiveGlow == null) return;
        Helpers.PulseAnimation.SetMinOpacity(TerminalLiveGlow, 0.35);
        Helpers.PulseAnimation.SetDurationSeconds(TerminalLiveGlow, 1.6);
        Helpers.PulseAnimation.SetIsEnabled(TerminalLiveGlow, isFast);
        if (!isFast) TerminalLiveGlow.Opacity = 0.35;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            PageTransitionHelper.AnimatePageEntrance(this);
            StartCursorBlink();
            StartLivePulseAnimation(ViewModel.IsExecuting);
            await ViewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            App.Log($"[OptimizationPage.Page_Loaded exception]: {ex.Message}");
        }
    }
}
