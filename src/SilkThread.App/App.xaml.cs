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
using SilkThread.Core.GameProfiles;
using SilkThread.Core.HardwareDetection;
using SilkThread.Core.PresetManager;
using SilkThread.Core.RestorePointManager;
using SilkThread.Core.TweakDatabase;
using SilkThread.Core.TweakEngine;
using SilkThread.Core.SystemOperations;

namespace SilkThread.App;

public partial class App : Application
{
    private Window? _window;
    private System.Threading.Mutex? _instanceMutex;
    private bool _ownsInstanceMutex;
    public static string StartupRoute { get; private set; } = "Dashboard";
    public static Window? MainWindowInstance { get; private set; }
    public static nint MainWindowHandle { get; private set; }
    public static Microsoft.UI.Dispatching.DispatcherQueue? MainDispatcherQueue { get; private set; }
    private static readonly object _logLock = new();
    private static readonly string LogFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SilkThread", "startup_debug.log");
    public static string LogFilePath => LogFile;

    public static IServiceProvider Services { get; private set; } = null!;

    public static void Log(string message)
    {
        try
        {
            lock (_logLock)
            {
                var dir = Path.GetDirectoryName(LogFile);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.AppendAllText(LogFile, $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] {message}\n");
            }
        }
        catch { }
    }

    public App()
    {
        Log("App() constructor started");

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            try
            {
                Log($"[FATAL] AppDomain Unhandled: {e.ExceptionObject}");
            }
            catch { }
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            try
            {
                Log($"[FATAL] UnobservedTaskException: {e.Exception}");
                e.SetObserved();
            }
            catch { }
        };

        UnhandledException += (s, e) =>
        {
            try
            {
                Log($"[FATAL] Xaml Unhandled: {e.Message} | {e.Exception}");
                e.Handled = true;
            }
            catch { }
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
        // Wire up core logging to App.Log
        OpenRouterAiService.ExternalLog = Log;

        // Core Services
        services.AddSingleton<IHardwareScanner, HardwareScanner>();
        services.AddSingleton<IPowerConfigurationReader, PowerConfigurationReader>();
        services.AddSingleton<IMemoryConfigurationReader, MemoryConfigurationReader>();
        services.AddSingleton<ISystemDiagnosticsService, SystemDiagnosticsService>();
        services.AddSingleton<IConfigurationJournal>(sp => sp.GetRequiredService<BackupManager>());
        services.AddSingleton<IPageFileService, PageFileService>();
        services.AddSingleton<IRestorePointManager, RestorePointManager>();
        services.AddSingleton<BackupManager>();
        services.AddSingleton<IBackupManager>(sp => sp.GetRequiredService<BackupManager>());
        services.AddSingleton<IPowerSchemeJournal>(sp => sp.GetRequiredService<BackupManager>());
        services.AddSingleton<IPowerSchemeService, PowerSchemeService>();
        services.AddSingleton<ICompatibilityEngine, CompatibilityEngine>();
        services.AddSingleton<IPresetManager, PresetManager>();
        services.AddSingleton<IAiOptimizerService, OpenRouterAiService>();
        services.AddSingleton<IGameProfileService, GameProfileService>();

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
        services.AddSingleton<IUpdateHistoryService, UpdateHistoryService>();

        // ViewModels
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<TweaksViewModel>();
        services.AddSingleton<PcScannerViewModel>();
        services.AddSingleton<OptimizationViewModel>();
        services.AddSingleton<GamesViewModel>();
        services.AddSingleton<SystemSettingsViewModel>();
        services.AddSingleton<HistoryViewModel>();
        services.AddSingleton<RestoreBackupViewModel>();
        services.AddSingleton<SettingsViewModel>();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        Log("OnLaunched started");
        var commandLine = Environment.GetCommandLineArgs().Skip(1).ToArray();
        if (commandLine.Contains(ElevationRestart.Switch))
        {
            var restart = ElevationRestart.Parse(commandLine);
            if (restart == null || !ElevationHelper.IsAdministrator() || !await ElevationRestart.WaitForParentAsync(restart))
            {
                Log("Invalid or incomplete elevation handoff.");
                Exit();
                return;
            }
            StartupRoute = restart.Route;
        }
        _instanceMutex = new System.Threading.Mutex(false, @"Local\SilkThread.App.SingleInstance");
        try { _ownsInstanceMutex = _instanceMutex.WaitOne(0); }
        catch (System.Threading.AbandonedMutexException) { _ownsInstanceMutex = true; }
        if (!_ownsInstanceMutex)
        {
            Log("Another SilkThread instance is already running.");
            _instanceMutex.Dispose();
            Exit();
            return;
        }
        try
        {
            _window = new MainWindow();
            _window.Closed += (_, _) =>
            {
                if (_ownsInstanceMutex)
                {
                    _instanceMutex.ReleaseMutex();
                    _ownsInstanceMutex = false;
                }
                _instanceMutex.Dispose();
            };
            MainWindowInstance = _window;
            MainDispatcherQueue = _window.DispatcherQueue;
            MainWindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(_window);
            Log("MainWindow instantiated");
            _window.Activate();
            Log("MainWindow.Activate() called");

            // Initialize SQLite in background without blocking window presentation
            _ = Task.Run(async () =>
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
