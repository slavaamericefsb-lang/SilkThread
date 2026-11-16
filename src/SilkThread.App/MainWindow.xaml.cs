using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using SilkThread.App.Services;
using SilkThread.App.Views;
using SilkThread.App.ViewModels;
using SilkThread.Core.SystemOperations;
using Windows.UI;

namespace SilkThread.App;

public sealed partial class MainWindow : Window
{
    private readonly Helpers.WindowSizeConstraint _sizeConstraint;
    private readonly INavigationService _navigationService;
    private readonly ISettingsService _settingsService;
    private readonly ILocalizationService _loc;

    public MainWindow()
    {
        App.Log("MainWindow constructor started");
        InitializeComponent();
        App.Log("MainWindow InitializeComponent finished");
        _sizeConstraint = new Helpers.WindowSizeConstraint(this, 1280, 720);

        _navigationService = App.Services.GetRequiredService<INavigationService>();
        _settingsService = App.Services.GetRequiredService<ISettingsService>();
        _loc = App.Services.GetRequiredService<ILocalizationService>();

        _navigationService.Initialize(ContentFrame);

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleBarDragRegion);

        string initialTitle = _loc["Main_WindowTitle"];
        Title = initialTitle;
        if (AppWindow != null)
        {
            AppWindow.Title = initialTitle;
            try
            {
                string iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    AppWindow.SetIcon(iconPath);
                }

                AppWindow.Resize(new Windows.Graphics.SizeInt32(1280, 720));
                var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(AppWindow.Id, Microsoft.UI.Windowing.DisplayAreaFallback.Primary);
                if (displayArea != null)
                {
                    var centeredPosition = new Windows.Graphics.PointInt32(
                        displayArea.WorkArea.X + Math.Max(0, (displayArea.WorkArea.Width - 1280) / 2),
                        displayArea.WorkArea.Y + Math.Max(0, (displayArea.WorkArea.Height - 720) / 2)
                    );
                    AppWindow.Move(centeredPosition);
                }
            }
            catch (Exception ex)
            {
                App.Log($"[Window setup warning]: {ex.Message}");
            }
        }

        // Synchronize NavView selection with frame navigation
        ContentFrame.Navigated += (s, e) =>
        {
            if (e.SourcePageType == typeof(DashboardPage)) NavView.SelectedItem = NavDashboard;
            else if (e.SourcePageType == typeof(TweaksPage)) NavView.SelectedItem = NavTweaks;
            else if (e.SourcePageType == typeof(PcScannerPage)) NavView.SelectedItem = NavScanner;
            else if (e.SourcePageType == typeof(OptimizationPage)) NavView.SelectedItem = NavOptimization;
            else if (e.SourcePageType == typeof(GamesPage)) NavView.SelectedItem = NavGames;
            else if (e.SourcePageType == typeof(SystemSettingsPage)) NavView.SelectedItem = NavSystemSettings;
            else if (e.SourcePageType == typeof(HistoryPage)) NavView.SelectedItem = NavHistory;
            else if (e.SourcePageType == typeof(RestoreBackupPage)) NavView.SelectedItem = NavRestore;
            else if (e.SourcePageType == typeof(SettingsPage)) NavView.SelectedItem = NavSettings;
        };

        // Apply saved theme
        ApplyTheme(_settingsService.Settings.AppTheme);
        _settingsService.SettingsChanged += (s, e) => ApplyTheme(_settingsService.Settings.AppTheme);

        // Setup Localization
        UpdateNavLabels();
        UpdateLanguageButtonHighlight();
        _loc.LanguageChanged += (s, e) =>
        {
            UpdateNavLabels();
            UpdateLanguageButtonHighlight();
        };

        // Admin badge check
        UpdateAdminStatus();

        // 5-Click Developer Mode Trigger on Settings Gear
        SetupDeveloperTrigger();

        App.Log("MainWindow constructor finished");
    }

    private readonly System.Collections.Generic.Queue<DateTime> _devClickTimestamps = new();
    private bool _isDevWorkflowRunning = false;

    private void SetupDeveloperTrigger()
    {
        NavSettings.AddHandler(
            UIElement.PointerPressedEvent,
            new Microsoft.UI.Xaml.Input.PointerEventHandler(NavSettings_PointerPressed),
            handledEventsToo: true);
    }

    private void NavSettings_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(sender as UIElement);
        if (!point.Properties.IsLeftButtonPressed) return;

        if (_isDevWorkflowRunning) return;

        var now = DateTime.UtcNow;
        _devClickTimestamps.Enqueue(now);

        // Keep only clicks within 2.5 seconds
        while (_devClickTimestamps.Count > 0 && (now - _devClickTimestamps.Peek()).TotalSeconds > 2.5)
        {
            _devClickTimestamps.Dequeue();
        }

        if (_devClickTimestamps.Count >= 5)
        {
            _devClickTimestamps.Clear();
            _ = OpenDeveloperWorkflowAsync();
        }
    }

    private async System.Threading.Tasks.Task OpenDeveloperWorkflowAsync()
    {
        if (_isDevWorkflowRunning) return;
        _isDevWorkflowRunning = true;

        try
        {
            var xamlRoot = Content?.XamlRoot ?? App.MainWindowInstance?.Content?.XamlRoot;
            if (xamlRoot == null) return;

            var authDialog = new DeveloperAuthDialog
            {
                XamlRoot = xamlRoot
            };

            await authDialog.ShowAsync();

            if (authDialog.IsAuthenticated)
            {
                var debugDialog = new DeveloperDebugDialog
                {
                    XamlRoot = xamlRoot
                };
                await debugDialog.ShowAsync();
            }
        }
        catch (Exception ex)
        {
            App.Log($"[Developer Workflow Error]: {ex.Message}");
        }
        finally
        {
            _isDevWorkflowRunning = false;
        }
    }

    private void UpdateAdminStatus()
    {
        bool isAdmin = ElevationHelper.IsAdministrator();
        AdminStatusText.Text = isAdmin 
            ? _loc["Main_AdminBadge"]
            : _loc["Main_UserBadge"];
        var adminBrush = new SolidColorBrush(isAdmin 
            ? Color.FromArgb(255, 34, 197, 94)   // #22C55E
            : Color.FromArgb(255, 150, 156, 171)); // #969CAB
        AdminStatusText.Foreground = adminBrush;
        AdminStatusIcon.Foreground = adminBrush;
        AdminStatusDot.Background = adminBrush;
        AdminModeButton.IsEnabled = !isAdmin;
        ToolTipService.SetToolTip(AdminModeButton, _loc[isAdmin ? "Main_AdminActive" : "Main_AdminSwitch"]);
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(AdminModeButton,
            _loc[isAdmin ? "Main_AdminActive" : "Main_AdminSwitch"]);
    }

    private bool _elevationInProgress;

    private bool HasRunningOperations()
    {
        var services = App.Services;
        return services.GetRequiredService<DashboardViewModel>().IsOptimizing
            || services.GetRequiredService<DashboardViewModel>().IsAiGenerating
            || services.GetRequiredService<OptimizationViewModel>().IsExecuting
            || services.GetRequiredService<TweaksViewModel>().HasRunningOperations
            || services.GetRequiredService<GamesViewModel>().IsApplyingProfile
            || services.GetRequiredService<RestoreBackupViewModel>().IsBusy
            || services.GetRequiredService<HistoryViewModel>().IsLoading
            || services.GetRequiredService<SystemSettingsViewModel>().IsBusy;
    }

    private async void AdminModeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_elevationInProgress || ElevationHelper.IsAdministrator()) return;
        _elevationInProgress = true;
        try
        {
            if (HasRunningOperations())
            {
                ShowElevationFeedback("Main_AdminBusy");
                return;
            }
            var dialog = new ContentDialog
            {
                XamlRoot = Content.XamlRoot,
                Title = _loc["Main_AdminSwitch"],
                Content = new TextBlock { Text = _loc["Main_AdminRestartNotice"], TextWrapping = TextWrapping.Wrap, MaxWidth = 440 },
                PrimaryButtonText = _loc["Main_AdminContinue"],
                CloseButtonText = _loc["Hist_Dialog_Cancel"],
                DefaultButton = ContentDialogButton.Close
            };
            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
            if (HasRunningOperations()) { ShowElevationFeedback("Main_AdminBusy"); return; }

            // Prevent a new operation while Windows is requesting elevation.
            ShellRoot.IsHitTestVisible = false;
            string route = (NavView.SelectedItem as NavigationViewItem)?.Tag as string ?? "Dashboard";
            var result = ElevationRestart.Start(route);
            if (result == ElevationRestartResult.Started) Close();
            else ShowElevationFeedback(result == ElevationRestartResult.Cancelled ? "Main_AdminCancelled" : "Main_AdminFailed");
        }
        catch (Exception ex)
        {
            App.Log($"[Admin mode button]: {ex}");
            ShowElevationFeedback("Main_AdminFailed");
        }
        finally { ShellRoot.IsHitTestVisible = true; _elevationInProgress = false; }
    }

    private void ShowElevationFeedback(string key)
    {
        ElevationFeedback.Message = _loc[key];
        ElevationFeedback.IsOpen = true;
    }

    private void UpdateNavLabels()
    {
        SetNavItem(NavDashboard, _loc["Nav_Dashboard"]);
        SetNavItem(NavTweaks, _loc["Nav_Tweaks"]);
        SetNavItem(NavScanner, _loc["Nav_Scanner"]);
        SetNavItem(NavOptimization, _loc["Nav_Optimization"]);
        SetNavItem(NavGames, _loc["Nav_Games"]);
        SetNavItem(NavSystemSettings, _loc["Nav_SystemSettings"]);
        SetNavItem(NavHistory, _loc["Nav_History"]);
        SetNavItem(NavRestore, _loc["Nav_Restore"]);
        SetNavItem(NavSettings, _loc["Nav_Settings"]);

        if (BrandSubtitleText != null)
        {
            BrandSubtitleText.Text = _loc["Main_BrandBadge"];
        }

        string windowTitle = _loc["Main_WindowTitle"];
        Title = windowTitle;
        if (AppWindow != null)
        {
            AppWindow.Title = windowTitle;
        }

        UpdateAdminStatus();
    }

    private void SetNavItem(NavigationViewItem item, string text)
    {
        if (item == null) return;
        item.Content = text;
        ToolTipService.SetToolTip(item, text);
        ToolTipService.SetPlacement(item, Microsoft.UI.Xaml.Controls.Primitives.PlacementMode.Right);
    }

    private void UpdateLanguageButtonHighlight()
    {
        bool isRu = _loc.CurrentLanguage == "ru";
        var activeBrush = new SolidColorBrush(Color.FromArgb(255, 128, 107, 255)); // Minimal Dark Accent #806BFF
        var inactiveBrush = new SolidColorBrush(Colors.Transparent);
        var activeText = new SolidColorBrush(Color.FromArgb(255, 243, 244, 248));   // #F3F4F8
        var inactiveText = new SolidColorBrush(Color.FromArgb(255, 150, 156, 171)); // #969CAB

        BtnLangRu.Background = isRu ? activeBrush : inactiveBrush;
        if (BtnLangRu.Content is TextBlock tbRu) tbRu.Foreground = isRu ? activeText : inactiveText;

        BtnLangEn.Background = !isRu ? activeBrush : inactiveBrush;
        if (BtnLangEn.Content is TextBlock tbEn) tbEn.Foreground = !isRu ? activeText : inactiveText;
    }

    private void BtnLangRu_Click(object sender, RoutedEventArgs e)
    {
        _loc.SetLanguage("ru");
    }

    private void BtnLangEn_Click(object sender, RoutedEventArgs e)
    {
        _loc.SetLanguage("en");
    }

    private void ApplyTheme(ElementTheme theme)
    {
        if (Content is FrameworkElement root)
        {
            root.RequestedTheme = theme;
        }
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        NavigateTo(App.StartupRoute);
        StartBackgroundUpdateCheck();
    }

    private void StartBackgroundUpdateCheck()
    {
        _ = System.Threading.Tasks.Task.Run(async () =>
        {
            // Delay 3 seconds for smooth, non-blocking startup
            await System.Threading.Tasks.Task.Delay(3000);
            try
            {
                var updateService = App.Services.GetRequiredService<IUpdateService>();
                var update = await updateService.CheckForUpdatesAsync(isManualCheck: false);
                if (update != null)
                {
                    DispatcherQueue.TryEnqueue(async () =>
                    {
                        try
                        {
                            var xamlRoot = App.MainWindowInstance?.Content?.XamlRoot ?? Content?.XamlRoot;
                            if (xamlRoot != null)
                            {
                                var dialog = new UpdateDialog(update, updateService)
                                {
                                    XamlRoot = xamlRoot
                                };
                                await dialog.ShowAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            App.Log($"[ShowUpdateDialog error]: {ex.Message}");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                App.Log($"[BackgroundUpdateCheck error]: {ex.Message}");
            }
        });
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            NavigateTo("Settings");
        }
        else if (args.SelectedItemContainer is NavigationViewItem item && item.Tag is string tag)
        {
            NavigateTo(tag);
        }
    }

    private void NavigateTo(string tag)
    {
        Type? pageType = tag switch
        {
            "Dashboard" => typeof(DashboardPage),
            "Tweaks" => typeof(TweaksPage),
            "Scanner" => typeof(PcScannerPage),
            "Optimization" => typeof(OptimizationPage),
            "Games" => typeof(GamesPage),
            "SystemSettings" => typeof(SystemSettingsPage),
            "History" => typeof(HistoryPage),
            "Restore" => typeof(RestoreBackupPage),
            "Settings" => typeof(SettingsPage),
            _ => null
        };

        if (pageType != null && ContentFrame.CurrentSourcePageType != pageType)
        {
            _navigationService.Navigate(pageType);
        }
    }
}
