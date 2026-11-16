using System;
using System.Globalization;
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

        // Attach to the root window's visual tree for strict dead-center positioning
        if (App.MainWindowInstance?.Content?.XamlRoot != null)
        {
            XamlRoot = App.MainWindowInstance.Content.XamlRoot;
        }

        var loc = LocalizationService.Instance;
        bool isRu = loc.CurrentLanguage == "ru";

        DialogTitleText.Text = loc["Update_Available"];
        OfficialBadgeText.Text = loc["Update_OfficialRelease"];
        WhatsNewHeaderText.Text = loc["Update_WhatsNew"];
        BtnLaterText.Text = loc["Update_Later"];
        BtnUpdateText.Text = loc["Update_Install"];

        // Populate metadata
        BadgeVersionText.Text = update.Version.StartsWith("v", StringComparison.OrdinalIgnoreCase)
            ? update.Version
            : $"v{update.Version}";

        ReleaseTitleText.Text = string.IsNullOrWhiteSpace(update.Title)
            ? string.Format(loc["Update_DefaultTitle"], update.Version)
            : update.Title;

        // Clean size display: e.g. "Размер: 3.2 МБ" / "Size: 3.2 MB"
        long? sizeBytes = (update.PatchSize.HasValue && update.PatchSize.Value > 0)
            ? update.PatchSize
            : ((update.DownloadSize.HasValue && update.DownloadSize.Value > 0) ? update.DownloadSize : null);

        string sizeHint = string.Empty;
        if (sizeBytes.HasValue && sizeBytes.Value > 0)
        {
            string formattedSize = FormatFileSize(sizeBytes.Value, isRu);
            sizeHint = $"{loc["Update_Size"]} {formattedSize}";
        }

        string releaseDateLabel = loc["Update_ReleaseDate"];
        if (!string.IsNullOrWhiteSpace(update.ReleaseDate) && !string.IsNullOrWhiteSpace(sizeHint))
        {
            ReleaseDateText.Text = $"{releaseDateLabel} {update.ReleaseDate} • {sizeHint}";
        }
        else if (!string.IsNullOrWhiteSpace(update.ReleaseDate))
        {
            ReleaseDateText.Text = $"{releaseDateLabel} {update.ReleaseDate}";
        }
        else if (!string.IsNullOrWhiteSpace(sizeHint))
        {
            ReleaseDateText.Text = sizeHint;
        }
        else
        {
            ReleaseDateText.Text = string.Empty;
        }

        DescriptionText.Text = string.IsNullOrWhiteSpace(update.Description)
            ? loc["Update_DefaultDesc"]
            : update.Description;

        var cleanCurrent = _updateService.CurrentVersion?.TrimStart('v', 'V') ?? "1.0.0";
        CurrentVersionHintText.Text = string.Format(loc["Update_CurrentVersion"], cleanCurrent);
    }

    /// <summary>
    /// Formats raw byte count into human-friendly size representation (e.g. "3.2 МБ" or "3.2 MB").
    /// </summary>
    public static string FormatFileSize(long bytes, bool isRu = true)
    {
        string unitMb = isRu ? "МБ" : "MB";
        string unitKb = isRu ? "КБ" : "KB";
        if (bytes >= 1048576)
        {
            double mb = bytes / 1048576.0;
            return $"{mb.ToString("0.#", CultureInfo.InvariantCulture)} {unitMb}";
        }
        if (bytes >= 1024)
        {
            double kb = bytes / 1024.0;
            return $"{kb.ToString("0.#", CultureInfo.InvariantCulture)} {unitKb}";
        }
        return $"{bytes} {(isRu ? "Б" : "B")}";
    }

    private void BtnLater_Click(object sender, RoutedEventArgs e)
    {
        _cts.Cancel();
        Hide();
    }

    private async void BtnUpdate_Click(object sender, RoutedEventArgs e)
    {
        var loc = LocalizationService.Instance;
        bool isRu = loc.CurrentLanguage == "ru";

        try
        {
            BtnUpdate.IsEnabled = false;
            BtnLater.IsEnabled = false;
            ErrorMessageText.Text = string.Empty;
            DownloadProgressPanel.Visibility = Visibility.Visible;
            DownloadStatusText.Text = loc["Update_Downloading"];

            var progress = new Progress<UpdateDownloadProgress>(p =>
            {
                try
                {
                    DownloadProgressBar.Value = p.Percentage;
                    DownloadPercentText.Text = $"{(int)p.Percentage}%";

                    string mbUnit = isRu ? "МБ" : "MB";
                    string downloadWord = loc["Update_DownloadWord"];
                    if (p.TotalBytes.HasValue && p.TotalBytes.Value > 0)
                    {
                        double receivedMb = p.BytesReceived / 1048576.0;
                        double totalMb = p.TotalBytes.Value / 1048576.0;
                        DownloadStatusText.Text = string.Format(loc["Update_DownloadProgress"], downloadWord, receivedMb, mbUnit, totalMb);
                    }
                    else
                    {
                        double receivedMb = p.BytesReceived / 1048576.0;
                        DownloadStatusText.Text = string.Format(loc["Update_DownloadProgressSimple"], downloadWord, receivedMb, mbUnit);
                    }
                }
                catch { }
            });

            await _updateService.DownloadAndInstallUpdateAsync(_update, progress, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            DownloadStatusText.Text = loc["Update_DownloadCancelled"];
            BtnLater.IsEnabled = true;
            BtnUpdate.IsEnabled = true;
        }
        catch (Exception ex)
        {
            App.Log($"[UpdateDialog] Download error: {ex.Message}");
            ErrorMessageText.Text = string.Format(loc["Update_DownloadError"], ex.Message);
            DownloadStatusText.Text = loc["Update_DownloadFailed"];
            BtnLater.IsEnabled = true;
            BtnUpdate.IsEnabled = true;
        }
    }
}
