using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using Windows.UI;

namespace SilkThread.App.Views;

public sealed partial class DeveloperAuthDialog : ContentDialog
{
    private static readonly SolidColorBrush DefaultBorderBrush = new(Color.FromArgb(255, 43, 48, 64)); // #2B3040
    private static readonly SolidColorBrush ErrorBorderBrush = new(Color.FromArgb(255, 239, 68, 68));   // #EF4444

    public bool IsAuthenticated { get; private set; }

    public DeveloperAuthDialog()
    {
        InitializeComponent();
        XamlRoot = App.MainWindowInstance?.Content?.XamlRoot ?? this.XamlRoot;
    }

    private void KeyPasswordBox_Loaded(object sender, RoutedEventArgs e)
    {
        KeyPasswordBox.Focus(FocusState.Programmatic);
    }

    private void KeyPasswordBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            e.Handled = true;
            ValidateAndSubmit();
        }
    }

    private void KeyPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (ErrorTextBlock.Visibility == Visibility.Visible)
        {
            ErrorTextBlock.Visibility = Visibility.Collapsed;
            KeyPasswordBox.BorderBrush = DefaultBorderBrush;
        }
    }

    private void BtnSubmit_Click(object sender, RoutedEventArgs e)
    {
        ValidateAndSubmit();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        IsAuthenticated = false;
        Hide();
    }

    private void ValidateAndSubmit()
    {
        var input = KeyPasswordBox.Password ?? string.Empty;

        bool isValid = input.Trim() == "FE55eWEj%jdgolkNhfYduiSoEFETD432ErlRdks";

        if (isValid)
        {
            IsAuthenticated = true;
            ErrorTextBlock.Visibility = Visibility.Collapsed;
            KeyPasswordBox.BorderBrush = DefaultBorderBrush;
            Hide();
        }
        else
        {
            IsAuthenticated = false;
            ErrorTextBlock.Text = "Неверный ключ доступа";
            ErrorTextBlock.Visibility = Visibility.Visible;
            KeyPasswordBox.BorderBrush = ErrorBorderBrush;
            KeyPasswordBox.SelectAll();
            KeyPasswordBox.Focus(FocusState.Programmatic);
        }
    }
}
