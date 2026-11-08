using System;
using System.Threading;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SilkThread.App.Services;

namespace SilkThread.App.Views;

public sealed partial class UpdateDialog : ContentDialog
{
    private readonly UpdateInfo _update;
    private readonly IUpdateService _updateService;
    private readonly CancellationTokenSource _cts = new();

    public UpdateDialog(UpdateInfo update, IUpdateService updateService)
    {
        _update = update ?? throw new ArgumentNullException(nameof(update));
        _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));

        InitializeComponent();

        // Populate metadata
        BadgeVersionText.Text = update.Version.StartsWith('v') || update.Version.StartsWith('V')
            ? update.Version
            : $"v{update.Version}";

        ReleaseTitleText.Text = string.IsNullOrWhiteSpace(update.Title)
            ? $"Обновление {update.Version}"
            : update.Title;

        ReleaseDateText.Text = string.IsNullOrWhiteSpace(update.ReleaseDate)
            ? string.Empty
            : $"Дата релиза: {update.ReleaseDate}";

        DescriptionText.Text = string.IsNullOrWhiteSpace(update.Description)
            ? "Улучшения производительности и исправления стабильности."
            : update.Description;

        CurrentVersionHintText.Text = $"Установлена версия v{_updateService.CurrentVersion} • Доступна более новая";
    }

    private void BtnLater_Click(object sender, RoutedEventArgs e)
    {
        _cts.Cancel();
        Hide();
    }

    private async void BtnUpdate_Click(object sender, RoutedEventArgs e)
    {
        BtnUpdate.IsEnabled = false;
        BtnLater.IsEnabled = false;
        ErrorMessageText.Text = string.Empty;
        DownloadProgressPanel.Visibility = Visibility.Visible;
        DownloadStatusText.Text = "Подключение к серверу загрузки...";

        var progress = new Progress<UpdateDownloadProgress>(p =>
        {
            DownloadProgressBar.Value = p.Percentage;
            DownloadPercentText.Text = $"{(int)p.Percentage}%";

            if (p.TotalBytes.HasValue && p.TotalBytes.Value > 0)
            {
                double receivedMb = p.BytesReceived / 1048576.0;
                double totalMb = p.TotalBytes.Value / 1048576.0;
                DownloadStatusText.Text = $"Загрузка: {receivedMb:F1} МБ / {totalMb:F1} МБ";
            }
            else
            {
                double receivedMb = p.BytesReceived / 1048576.0;
                DownloadStatusText.Text = $"Загрузка: {receivedMb:F1} МБ...";
            }
        });

        try
        {
            await _updateService.DownloadAndInstallUpdateAsync(_update, progress, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            DownloadStatusText.Text = "Загрузка отменена";
            BtnLater.IsEnabled = true;
            BtnUpdate.IsEnabled = true;
        }
        catch (Exception ex)
        {
            App.Log($"[UpdateDialog] Download error: {ex.Message}");
            ErrorMessageText.Text = $"Ошибка: {ex.Message}";
            DownloadStatusText.Text = "Не удалось завершить загрузку.";
            BtnLater.IsEnabled = true;
            BtnUpdate.IsEnabled = true;
        }
    }
}
