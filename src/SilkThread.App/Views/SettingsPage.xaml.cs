using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SilkThread.App.Services;
using SilkThread.App.ViewModels;

namespace SilkThread.App.Views;

public sealed partial class SettingsPage : Page, IStaggeredPage
{
    public SettingsViewModel ViewModel { get; }

    public IReadOnlyList<FrameworkElement> GetStaggeredElements()
    {
        return new FrameworkElement[]
        {
            HeaderPanel,
            CardSafety,
            CardLanguage,
            CardAi,
            CardAbout
        };
    }

    public SettingsPage()
    {
        ViewModel = App.Services.GetRequiredService<SettingsViewModel>();
        InitializeComponent();
    }

    private Microsoft.UI.Xaml.Media.Animation.Storyboard? _iostPulseStoryboard;

    private void Page_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        PageTransitionHelper.AnimatePageEntrance(this);
        StartIostPulseAnimation();

        if (OpenRouterKeyBox != null)
        {
            OpenRouterKeyBox.Password = ViewModel.OpenRouterApiKey;
            OpenRouterKeyBox.PasswordChanged -= OpenRouterKeyBox_PasswordChanged;
            OpenRouterKeyBox.PasswordChanged += OpenRouterKeyBox_PasswordChanged;
        }
    }

    private void StartIostPulseAnimation()
    {
        if (IostStatusPulseRing == null || IostStatusPulseScale == null) return;

        _iostPulseStoryboard?.Stop();
        _iostPulseStoryboard = new Microsoft.UI.Xaml.Media.Animation.Storyboard
        {
            RepeatBehavior = Microsoft.UI.Xaml.Media.Animation.RepeatBehavior.Forever,
            AutoReverse = true
        };

        var scaleX = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
        {
            From = 1.0,
            To = 1.65,
            Duration = new Duration(System.TimeSpan.FromSeconds(1.25)),
            EasingFunction = new Microsoft.UI.Xaml.Media.Animation.SineEase { EasingMode = Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseInOut }
        };
        Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(scaleX, IostStatusPulseScale);
        Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(scaleX, "ScaleX");

        var scaleY = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
        {
            From = 1.0,
            To = 1.65,
            Duration = new Duration(System.TimeSpan.FromSeconds(1.25)),
            EasingFunction = new Microsoft.UI.Xaml.Media.Animation.SineEase { EasingMode = Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseInOut }
        };
        Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(scaleY, IostStatusPulseScale);
        Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(scaleY, "ScaleY");

        var opacity = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
        {
            From = 0.55,
            To = 0.12,
            Duration = new Duration(System.TimeSpan.FromSeconds(1.25)),
            EasingFunction = new Microsoft.UI.Xaml.Media.Animation.SineEase { EasingMode = Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseInOut }
        };
        Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(opacity, IostStatusPulseRing);
        Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(opacity, "Opacity");

        _iostPulseStoryboard.Children.Add(scaleX);
        _iostPulseStoryboard.Children.Add(scaleY);
        _iostPulseStoryboard.Children.Add(opacity);
        _iostPulseStoryboard.Begin();
    }

    private void OpenRouterKeyBox_PasswordChanged(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewModel.OpenRouterApiKey = OpenRouterKeyBox.Password;
    }

    private async void UpdateHistoryButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            var xamlRoot = App.MainWindowInstance?.Content?.XamlRoot ?? this.XamlRoot;
            if (xamlRoot != null)
            {
                var dialog = new UpdateHistoryDialog
                {
                    XamlRoot = xamlRoot
                };
                await dialog.ShowAsync();
            }
        }
        catch (System.Exception ex)
        {
            App.Log($"[UpdateHistoryDialog Error]: {ex}");
        }
    }

    private async void CheckUpdatesButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var updateService = App.Services.GetRequiredService<IUpdateService>();
        var loc = App.Services.GetRequiredService<ILocalizationService>();

        CheckUpdatesButton.IsEnabled = false;
        CheckUpdatesStatusText.Visibility = Visibility.Visible;
        CheckUpdatesStatusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 150, 156, 171));
        CheckUpdatesStatusText.Text = loc["Update_Checking"];

        try
        {
            var update = await updateService.CheckForUpdatesAsync(isManualCheck: true);
            ViewModel.RefreshVersionDebugInfo(updateService.LastRemoteVersion);

            if (update != null)
            {
                CheckUpdatesStatusText.Text = loc["Update_Available"];
                CheckUpdatesStatusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 128, 107, 255));

                var xamlRoot = App.MainWindowInstance?.Content?.XamlRoot ?? this.XamlRoot;
                if (xamlRoot != null)
                {
                    var dialog = new UpdateDialog(update, updateService)
                    {
                        XamlRoot = xamlRoot
                    };
                    await dialog.ShowAsync();
                }
                else
                {
                    App.Log("[CheckUpdatesButton_Click] Could not show UpdateDialog: XamlRoot is null.");
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(updateService.LastRemoteVersion))
                {
                    string verComparison = loc.CurrentLanguage == "ru"
                        ? $"Текущая версия: {updateService.CurrentVersion} | Версия на сервере: {updateService.LastRemoteVersion}"
                        : $"Current version: {updateService.CurrentVersion} | Server version: {updateService.LastRemoteVersion}";

                    CheckUpdatesStatusText.Text = $"{loc["Update_UpToDate"]}\n{verComparison}";
                    CheckUpdatesStatusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 34, 197, 94));
                }
                else
                {
                    CheckUpdatesStatusText.Text = loc["Update_Error"];
                    CheckUpdatesStatusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68));
                }
            }
        }
        catch (System.Exception ex)
        {
            App.Log($"[CheckUpdatesButton_Click Exception]: {ex.Message}");
            CheckUpdatesStatusText.Text = loc["Update_Error"];
            CheckUpdatesStatusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68));
        }
        finally
        {
            CheckUpdatesButton.IsEnabled = true;
        }
    }
}
