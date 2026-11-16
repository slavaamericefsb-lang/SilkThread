using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SilkThread.App.Services;

namespace SilkThread.App.Views;

public static class ConfigurationReviewDialog
{
    public static async Task<bool> ShowAsync(XamlRoot root, string title, string text, string primary)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = root, Title = title, PrimaryButtonText = primary,
            CloseButtonText = LocalizationService.Instance["Hist_Dialog_Cancel"], DefaultButton = ContentDialogButton.Close,
            Content = new ScrollViewer { MaxHeight = 440, Content = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, IsTextSelectionEnabled = true } }
        };
        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }
}
