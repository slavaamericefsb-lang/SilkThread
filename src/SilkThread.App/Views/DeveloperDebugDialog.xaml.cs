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
    private int _logReadVersion;
    private bool _closed;

    public DeveloperDebugDialog()
    {
        InitializeComponent();
        XamlRoot = App.MainWindowInstance?.Content?.XamlRoot ?? this.XamlRoot;

        _logFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SilkThread", "startup_debug.log");

        ToolTipService.SetToolTip(ChipLogBorder, _logFilePath);

        ApplyLocalization();
        PopulateTelemetry();
        Opened += async (_, _) => { _closed = false; await LoadLogsAsync(); };
        Closed += (_, _) => { _closed = true; ++_logReadVersion; _statusCts?.Cancel(); };
    }

    private void ApplyLocalization()
    {
        var loc = LocalizationService.Instance;
        DialogTitleText.Text = loc["DevDebug_Title"];
        DialogSubtitleText.Text = loc["DevDebug_Subtitle"];
        ToolTipService.SetToolTip(BtnCloseTop, loc["DevDebug_Close"]);
        ChipVersionLabel.Text = loc["DevDebug_ChipVersion"];
        ChipRamLabel.Text = loc["DevDebug_ChipRam"];
        ChipLogFileLabel.Text = loc["DevDebug_ChipLogFile"];
        ChipSessionStatusLabel.Text = loc["DevDebug_ChipSession"];
        TerminalHeaderLabel.Text = loc["DevDebug_LogHeader"];
        ToolTipService.SetToolTip(BtnRefreshLog, loc["DevDebug_RefreshTooltip"]);
        BtnRefreshLogText.Text = loc["DevDebug_Refresh"];
        BtnSaveToFileText.Text = loc["DevDebug_SaveLogs"];
        BtnCopyClipboardText.Text = loc["DevDebug_Copy"];
        BtnClearLogText.Text = loc["DevDebug_Clear"];
        BtnCloseText.Text = loc["DevDebug_Close"];
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
            ChipSessionStatusText.Text = LocalizationService.Instance["DevDebug_SessionActive"];
        }
        catch (Exception ex)
        {
            App.Log($"[DeveloperDebugDialog] Telemetry error: {ex.Message}");
        }
    }

    private async Task LoadLogsAsync()
    {
        var loc = LocalizationService.Instance;
        int version = ++_logReadVersion;
        try
        {
            var result = await Task.Run(() =>
            {
                if (!File.Exists(_logFilePath)) return (Full: "", Display: "", Lines: 0, Bytes: 0);
                using var fs = new FileStream(_logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fs);
                string full = reader.ReadToEnd();
                var lines = full.Split('\n');
                // Keep complete text for export/copy; bound only the expensive visible text layout.
                string display = lines.Length > 2000 ? string.Join("\n", lines.TakeLast(2000)) : full;
                return (Full: full, Display: display, Lines: lines.Length, Bytes: System.Text.Encoding.UTF8.GetByteCount(full));
            });
            if (_closed || version != _logReadVersion) return;
            _currentLogText = result.Full;
            if (string.IsNullOrWhiteSpace(_currentLogText))
            {
                LogContentText.Text = loc["DevDebug_EmptyLog"];
                LogContentText.Foreground = new SolidColorBrush(Color.FromArgb(255, 150, 156, 171)); // #969CAB
                LogStatsText.Text = string.Format(loc["DevDebug_Stats"], 0, 0.0);
            }
            else
            {
                LogContentText.Text = result.Lines > 2000
                    ? (loc.CurrentLanguage == "ru" ? "Показаны последние 2000 строк; копирование и экспорт содержат полный журнал.\n" : "Showing the last 2000 lines; copy and export contain the complete log.\n") + result.Display
                    : result.Display;
                LogContentText.Foreground = new SolidColorBrush(Color.FromArgb(255, 196, 181, 253)); // #C4B5FD

                LogStatsText.Text = string.Format(loc["DevDebug_Stats"], result.Lines, result.Bytes / 1024.0);
            }

            // Scroll to end of logs
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                try
                {
                    if (_closed) return;
                    LogScrollViewer.ChangeView(null, LogScrollViewer.ScrollableHeight, null, true);
                }
                catch { }
            });
        }
        catch (Exception ex)
        {
            if (_closed || version != _logReadVersion) return;
            LogContentText.Text = string.Format(loc["DevDebug_ReadError"], ex.Message);
            LogContentText.Foreground = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)); // #EF4444
            LogStatsText.Text = loc["DevDebug_StatsError"];
        }
    }

    private async void BtnRefreshLog_Click(object sender, RoutedEventArgs e)
    {
        PopulateTelemetry();
        await LoadLogsAsync();
        ShowStatus(LocalizationService.Instance["DevDebug_ToastRefreshed"], isSuccess: true);
    }

    private async void BtnSaveToFile_Click(object sender, RoutedEventArgs e)
    {
        var loc = LocalizationService.Instance;
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
                ShowStatus(string.Format(loc["DevDebug_ToastSavedDesktop"], Path.GetFileName(finalPath)), isSuccess: true);
            }
            else
            {
                ShowStatus(string.Format(loc["DevDebug_ToastSaved"], Path.GetFileName(finalPath)), isSuccess: true);
            }
        }
        catch (Exception ex)
        {
            ShowStatus(string.Format(loc["DevDebug_ToastSaveError"], ex.Message), isSuccess: false);
        }
    }

    private async void BtnCopyClipboard_Click(object sender, RoutedEventArgs e)
    {
        var loc = LocalizationService.Instance;
        try
        {
            var package = new DataPackage();
            package.SetText(string.IsNullOrEmpty(_currentLogText) ? "(Журнал отладки пуст)" : _currentLogText);
            Clipboard.SetContent(package);

            BtnCopyClipboardText.Text = loc["DevDebug_Copied"];
            ShowStatus(loc["DevDebug_ToastCopied"], isSuccess: true);

            await Task.Delay(2500);
            BtnCopyClipboardText.Text = loc["DevDebug_Copy"];
        }
        catch (Exception ex)
        {
            ShowStatus(string.Format(loc["DevDebug_ToastCopyError"], ex.Message), isSuccess: false);
        }
    }

    private async void BtnClearLog_Click(object sender, RoutedEventArgs e)
    {
        var loc = LocalizationService.Instance;
        try
        {
            if (File.Exists(_logFilePath))
            {
                using var fs = new FileStream(_logFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                // Truncate to zero bytes
            }

            App.Log("Логи сессии были очищены через панель отладки разработчика.");
            await LoadLogsAsync();
            ShowStatus(loc["DevDebug_ToastCleared"], isSuccess: true);
        }
        catch (Exception ex)
        {
            ShowStatus(string.Format(loc["DevDebug_ToastClearError"], ex.Message), isSuccess: false);
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
