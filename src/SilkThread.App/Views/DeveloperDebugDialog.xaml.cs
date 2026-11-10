using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SilkThread.App.Services;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI;

namespace SilkThread.App.Views;

public sealed partial class DeveloperDebugDialog : ContentDialog
{
    private string _currentLogText = string.Empty;
    private readonly string _logFilePath;
    private CancellationTokenSource? _statusCts;

    public DeveloperDebugDialog()
    {
        InitializeComponent();
        XamlRoot = App.MainWindowInstance?.Content?.XamlRoot ?? this.XamlRoot;

        _logFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SilkThread", "startup_debug.log");

        ToolTipService.SetToolTip(ChipLogBorder, _logFilePath);

        PopulateTelemetry();
        LoadLogs();
    }

    private void PopulateTelemetry()
    {
        try
        {
            // 1. Version: v1.1.2 (.NET 10 x64)
            string versionStr = "v1.1.2";
            try
            {
                var updateService = App.Services?.GetService<IUpdateService>();
                if (updateService != null && !string.IsNullOrWhiteSpace(updateService.CurrentVersion))
                {
                    versionStr = updateService.CurrentVersion.StartsWith("v", StringComparison.OrdinalIgnoreCase)
                        ? updateService.CurrentVersion
                        : $"v{updateService.CurrentVersion}";
                }
                else
                {
                    var ver = Assembly.GetExecutingAssembly().GetName().Version;
                    if (ver != null)
                    {
                        versionStr = ver.Build >= 0
                            ? (ver.Revision > 0 ? $"v{ver.Major}.{ver.Minor}.{ver.Build}.{ver.Revision}" : $"v{ver.Major}.{ver.Minor}.{ver.Build}")
                            : $"v{ver.Major}.{ver.Minor}.0";
                    }
                }
            }
            catch { }

            string arch = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
            ChipVersionText.Text = $"{versionStr} (.NET {Environment.Version.Major} {arch})";

            // 2. RAM
            using var proc = Process.GetCurrentProcess();
            proc.Refresh();
            double ramMb = proc.WorkingSet64 / (1024.0 * 1024.0);
            ChipRamText.Text = $"{ramMb:F1} MB RAM";

            // 3. Log file
            ChipLogFileText.Text = Path.GetFileName(_logFilePath);

            // 4. Session status
            ChipSessionStatusText.Text = "Активна";
        }
        catch (Exception ex)
        {
            App.Log($"[DeveloperDebugDialog] Telemetry error: {ex.Message}");
        }
    }

    private void LoadLogs()
    {
        try
        {
            if (File.Exists(_logFilePath))
            {
                using var fs = new FileStream(_logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sr = new StreamReader(fs);
                _currentLogText = sr.ReadToEnd();
            }
            else
            {
                _currentLogText = string.Empty;
            }

            if (string.IsNullOrWhiteSpace(_currentLogText))
            {
                LogContentText.Text = "[Журнал startup_debug.log пуст или файл не найден]";
                LogContentText.Foreground = new SolidColorBrush(Color.FromArgb(255, 150, 156, 171)); // #969CAB
                LogStatsText.Text = "Строк: 0 | Размер: 0.0 КБ";
            }
            else
            {
                LogContentText.Text = _currentLogText;
                LogContentText.Foreground = new SolidColorBrush(Color.FromArgb(255, 196, 181, 253)); // #C4B5FD

                var lines = _currentLogText.Split('\n');
                int lineCount = lines.Length;
                long byteCount = System.Text.Encoding.UTF8.GetByteCount(_currentLogText);
                double kb = byteCount / 1024.0;
                LogStatsText.Text = $"Строк: {lineCount:N0} | Размер: {kb:F1} КБ";
            }

            // Scroll to end of logs
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                try
                {
                    LogScrollViewer.UpdateLayout();
                    LogScrollViewer.ChangeView(null, LogScrollViewer.ScrollableHeight, null, true);
                }
                catch { }
            });
        }
        catch (Exception ex)
        {
            LogContentText.Text = $"[Ошибка при чтении логов: {ex.Message}]";
            LogContentText.Foreground = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)); // #EF4444
            LogStatsText.Text = "Ошибка чтения";
        }
    }

    private void BtnRefreshLog_Click(object sender, RoutedEventArgs e)
    {
        PopulateTelemetry();
        LoadLogs();
        ShowStatus("Логи обновлены", isSuccess: true);
    }

    private async void BtnSaveToFile_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string? chosenPath = await FilePickerHelper.PickSaveLogFileAsync("SilkThread_Debug_Logs.txt");
            string finalPath;
            bool usedDesktopFallback = false;

            if (!string.IsNullOrWhiteSpace(chosenPath))
            {
                finalPath = chosenPath;
            }
            else
            {
                // Fallback directly to Desktop if picker was canceled or unavailable
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                finalPath = Path.Combine(desktop, "SilkThread_Debug_Logs.txt");
                usedDesktopFallback = true;
            }

            string contentToSave = string.IsNullOrEmpty(_currentLogText) ? "(Журнал отладки пуст)" : _currentLogText;
            await File.WriteAllTextAsync(finalPath, contentToSave, System.Text.Encoding.UTF8);

            if (usedDesktopFallback)
            {
                ShowStatus($"Логи сохранены на Рабочий стол: {Path.GetFileName(finalPath)}", isSuccess: true);
            }
            else
            {
                ShowStatus($"Логи успешно сохранены: {Path.GetFileName(finalPath)}", isSuccess: true);
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"Ошибка сохранения: {ex.Message}", isSuccess: false);
        }
    }

    private async void BtnCopyClipboard_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var package = new DataPackage();
            package.SetText(string.IsNullOrEmpty(_currentLogText) ? "(Журнал отладки пуст)" : _currentLogText);
            Clipboard.SetContent(package);

            BtnCopyClipboardText.Text = "Скопировано!";
            ShowStatus("Логи успешно скопированы в буфер обмена!", isSuccess: true);

            await Task.Delay(2500);
            BtnCopyClipboardText.Text = "Копировать";
        }
        catch (Exception ex)
        {
            ShowStatus($"Ошибка копирования: {ex.Message}", isSuccess: false);
        }
    }

    private void BtnClearLog_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (File.Exists(_logFilePath))
            {
                using var fs = new FileStream(_logFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                // Truncate to zero bytes
            }

            App.Log("Логи сессии были очищены через панель отладки разработчика.");
            LoadLogs();
            ShowStatus("Логи успешно очищены", isSuccess: true);
        }
        catch (Exception ex)
        {
            ShowStatus($"Ошибка очистки: {ex.Message}", isSuccess: false);
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void DismissStatusBadge_Click(object sender, RoutedEventArgs e)
    {
        _statusCts?.Cancel();
        StatusBadgeBorder.Visibility = Visibility.Collapsed;
    }

    private void ShowStatus(string message, bool isSuccess)
    {
        _statusCts?.Cancel();
        _statusCts = new CancellationTokenSource();
        var token = _statusCts.Token;

        StatusBadgeText.Text = message;
        if (isSuccess)
        {
            StatusBadgeBorder.Background = new SolidColorBrush(Color.FromArgb(255, 22, 46, 34)); // #162E22
            StatusBadgeBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 34, 197, 94)); // #22C55E
            StatusBadgeIcon.Glyph = "\uE73E";
            StatusBadgeIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 34, 197, 94));
            StatusBadgeText.Foreground = new SolidColorBrush(Color.FromArgb(255, 34, 197, 94));
        }
        else
        {
            StatusBadgeBorder.Background = new SolidColorBrush(Color.FromArgb(255, 46, 22, 22)); // #2E1616
            StatusBadgeBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)); // #EF4444
            StatusBadgeIcon.Glyph = "\uE783";
            StatusBadgeIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68));
            StatusBadgeText.Foreground = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68));
        }

        StatusBadgeBorder.Visibility = Visibility.Visible;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(4000, token);
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (!token.IsCancellationRequested)
                    {
                        StatusBadgeBorder.Visibility = Visibility.Collapsed;
                    }
                });
            }
            catch (TaskCanceledException) { }
        });
    }
}
