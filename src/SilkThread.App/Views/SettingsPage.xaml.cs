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

    private void Page_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        PageTransitionHelper.AnimatePageEntrance(this);

        if (OpenRouterKeyBox != null)
        {
            OpenRouterKeyBox.Password = ViewModel.OpenRouterApiKey;
            OpenRouterKeyBox.PasswordChanged -= OpenRouterKeyBox_PasswordChanged;
            OpenRouterKeyBox.PasswordChanged += OpenRouterKeyBox_PasswordChanged;
        }
    }

    private void OpenRouterKeyBox_PasswordChanged(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewModel.OpenRouterApiKey = OpenRouterKeyBox.Password;
    }

    private async void UpdateHistoryButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            var dialog = new UpdateHistoryDialog
            {
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
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
            if (update != null)
            {
                CheckUpdatesStatusText.Text = loc["Update_Available"];
                CheckUpdatesStatusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 128, 107, 255));

                var dialog = new UpdateDialog(update, updateService)
                {
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
            else
            {
                CheckUpdatesStatusText.Text = loc["Update_UpToDate"];
                CheckUpdatesStatusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 34, 197, 94));
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
