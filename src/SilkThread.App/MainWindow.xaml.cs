using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SilkThread.App.Services;
using SilkThread.App.Views;
using SilkThread.Core.SystemOperations;
using Windows.UI;

namespace SilkThread.App;

public sealed partial class MainWindow : Window
{
    private readonly INavigationService _navigationService;
    private readonly ISettingsService _settingsService;
    private readonly ILocalizationService _loc;

    public MainWindow()
    {
        App.Log("MainWindow constructor started");
        InitializeComponent();
        App.Log("MainWindow InitializeComponent finished");

        _navigationService = App.Services.GetRequiredService<INavigationService>();
        _settingsService = App.Services.GetRequiredService<ISettingsService>();
        _loc = App.Services.GetRequiredService<ILocalizationService>();

        _navigationService.Initialize(ContentFrame);

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        Title = "SilkThread — Windows Gaming Optimizer";
        if (AppWindow != null)
        {
            AppWindow.Title = "SilkThread — Windows Gaming Optimizer";
            try
            {
                string iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    AppWindow.SetIcon(iconPath);
                }

                AppWindow.Resize(new Windows.Graphics.SizeInt32(1240, 820));
                var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(AppWindow.Id, Microsoft.UI.Windowing.DisplayAreaFallback.Primary);
                if (displayArea != null)
                {
                    var centeredPosition = new Windows.Graphics.PointInt32(
                        (displayArea.WorkArea.Width - 1240) / 2,
                        (displayArea.WorkArea.Height - 820) / 2
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

        App.Log("MainWindow constructor finished");
    }

    private void UpdateAdminStatus()
    {
        bool isAdmin = ElevationHelper.IsAdministrator();
        AdminStatusText.Text = isAdmin 
            ? (_loc.CurrentLanguage == "ru" ? "Администратор ✓" : "Admin ✓")
            : (_loc.CurrentLanguage == "ru" ? "Пользователь" : "Standard User");
        var adminBrush = new SolidColorBrush(isAdmin 
            ? Color.FromArgb(255, 34, 197, 94)   // #22C55E
            : Color.FromArgb(255, 150, 156, 171)); // #969CAB
        AdminStatusText.Foreground = adminBrush;
    }

    private void UpdateNavLabels()
    {
        SetNavItem(NavDashboard, _loc["Nav_Dashboard"]);
        SetNavItem(NavTweaks, _loc["Nav_Tweaks"]);
        SetNavItem(NavScanner, _loc["Nav_Scanner"]);
        SetNavItem(NavOptimization, _loc["Nav_Optimization"]);
        SetNavItem(NavHistory, _loc["Nav_History"]);
        SetNavItem(NavRestore, _loc["Nav_Restore"]);
        SetNavItem(NavSettings, _loc["Nav_Settings"]);

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
        NavView.SelectedItem = NavDashboard;
        NavigateTo("Dashboard");
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
                            if (Content?.XamlRoot != null)
                            {
                                var dialog = new UpdateDialog(update, updateService)
                                {
                                    XamlRoot = Content.XamlRoot
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
