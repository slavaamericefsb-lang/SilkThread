using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using SilkThread.App.Services;
using SilkThread.App.ViewModels;
using SilkThread.Core.AiOptimizer;
using SilkThread.Core.BackupManager;
using SilkThread.Core.CompatibilityEngine;
using SilkThread.Core.HardwareDetection;
using SilkThread.Core.PresetManager;
using SilkThread.Core.RestorePointManager;
using SilkThread.Core.TweakDatabase;
using SilkThread.Core.TweakEngine;

namespace SilkThread.App;

public partial class App : Application
{
    private Window? _window;
    public static Window? MainWindowInstance { get; private set; }
    public static nint MainWindowHandle { get; private set; }
    private static readonly string LogFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SilkThread", "startup_debug.log");

    public static IServiceProvider Services { get; private set; } = null!;

    public static void Log(string message)
    {
        try
        {
            var dir = Path.GetDirectoryName(LogFile);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.AppendAllText(LogFile, $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] {message}\n");
        }
        catch { }
    }

    public App()
    {
        Log("App() constructor started");

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            Log($"[FATAL] AppDomain Unhandled: {e.ExceptionObject}");
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            Log($"[FATAL] UnobservedTaskException: {e.Exception}");
            e.SetObserved();
        };

        UnhandledException += (s, e) =>
        {
            Log($"[FATAL] Xaml Unhandled: {e.Message} | {e.Exception}");
            e.Handled = true;
        };

        try
        {
            InitializeComponent();
            Log("App.InitializeComponent completed");

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            Services = serviceCollection.BuildServiceProvider();
            Log("Services built successfully");
        }
        catch (Exception ex)
        {
            Log($"[EXCEPTION in App constructor]: {ex}");
            throw;
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Core Services
        services.AddSingleton<IHardwareScanner, HardwareScanner>();
        services.AddSingleton<IRestorePointManager, RestorePointManager>();
        services.AddSingleton<IBackupManager, BackupManager>();
        services.AddSingleton<ICompatibilityEngine, CompatibilityEngine>();
        services.AddSingleton<IPresetManager, PresetManager>();
        services.AddSingleton<IAiOptimizerService, OpenRouterAiService>();

        // Tweak Engine initialized with 165 tweaks catalog
        services.AddSingleton<ITweakEngine>(sp =>
        {
            var tweaks = TweakCatalog.GetAllTweaks();
            var backup = sp.GetRequiredService<IBackupManager>();
            var restore = sp.GetRequiredService<IRestorePointManager>();
            var compat = sp.GetRequiredService<ICompatibilityEngine>();
            return new TweakEngine(tweaks, backup, restore, compat);
        });

        // App Services
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<ILiquidAiOptimizerService, LiquidAiOptimizerService>();
        services.AddSingleton<IUpdateService, UpdateService>();

        // ViewModels
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<TweaksViewModel>();
        services.AddSingleton<PcScannerViewModel>();
        services.AddSingleton<OptimizationViewModel>();
        services.AddSingleton<HistoryViewModel>();
        services.AddSingleton<RestoreBackupViewModel>();
        services.AddSingleton<SettingsViewModel>();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Log("OnLaunched started");
        try
        {
            _window = new MainWindow();
            MainWindowInstance = _window;
            MainWindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(_window);
            Log("MainWindow instantiated");
            _window.Activate();
            Log("MainWindow.Activate() called");

            // Initialize SQLite in background without blocking window presentation
            Task.Run(async () =>
            {
                try
                {
                    Log("Background SQLite initialization starting");
                    var backupManager = Services.GetRequiredService<IBackupManager>();
                    await backupManager.InitializeAsync();
                    Log("Background SQLite initialization completed");
                }
                catch (Exception ex)
                {
                    Log($"[EXCEPTION in SQLite init]: {ex}");
                }
            });
        }
        catch (Exception ex)
        {
            Log($"[EXCEPTION in OnLaunched]: {ex}");
            throw;
        }
    }
}
