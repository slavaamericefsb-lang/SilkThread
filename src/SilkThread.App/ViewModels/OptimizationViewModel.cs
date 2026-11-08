using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SilkThread.App.Services;
using SilkThread.App.Views;
using SilkThread.Core.CompatibilityEngine;
using SilkThread.Core.HardwareDetection;
using SilkThread.Core.Models;
using SilkThread.Core.PresetManager;
using SilkThread.Core.TweakEngine;

namespace SilkThread.App.ViewModels;

public partial class OptimizationPreset : ObservableObject
{
    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public string Icon { get; }
    public string Badge { get; }
    public string AccentColorHex { get; }
    public Func<TweakDefinition, HardwareProfile, bool> Selector { get; }

    [ObservableProperty]
    private bool _isSelected;

    public OptimizationPreset(string id, string name, string description, string icon, string badge, string accentColorHex, Func<TweakDefinition, HardwareProfile, bool> selector)
    {
        Id = id;
        Name = name;
        Description = description;
        Icon = icon;
        Badge = badge;
        AccentColorHex = accentColorHex;
        Selector = selector;
    }
}

public partial class OptimizationViewModel : ObservableObject
{
    private readonly ITweakEngine _tweakEngine;
    private readonly IHardwareScanner _hardwareScanner;
    private readonly ICompatibilityEngine _compatibilityEngine;
    private readonly ISettingsService _settingsService;
    private readonly ILocalizationService _loc;
    private readonly SilkThread.Core.RestorePointManager.IRestorePointManager _restorePointManager;
    private readonly IPresetManager _presetManager;

    public ObservableCollection<OptimizationPreset> Presets { get; } = new();

    [ObservableProperty]
    private OptimizationPreset? _selectedPreset;

    [ObservableProperty]
    private bool _createRestorePoint = true;

    [ObservableProperty]
    private bool _isExecuting;

    [ObservableProperty]
    private int _progressCurrent;

    [ObservableProperty]
    private int _progressTotal;

    [ObservableProperty]
    private double _progressPercent;

    [ObservableProperty]
    private string _currentStatusText = "Готов к запуску оптимизации.";

    public ObservableCollection<TweakItemViewModel> TweaksToApply { get; } = new();
    public ObservableCollection<string> ExecutionLog { get; } = new();

    // Localized properties
    public string PageTitle => _loc["Opt_Title"];
    public string PageSubtitle => _loc["Opt_Subtitle"];
    public string CreateRestorePointLabel => _loc["Opt_CreateRestorePoint"];
    public string StartBtnLabel => _loc["Opt_Start_Btn"];
    public string ExecutionLogTitle => _loc.CurrentLanguage == "ru" ? "ЖУРНАЛ ОПЕРАЦИЙ В РЕАЛЬНОМ ВРЕМЕНИ" : "REAL-TIME EXECUTION LOG";
    public string CreatePresetLabel => _loc["Tweaks_CreatePreset"];
    public string ActivatePresetLabel => _loc["Tweaks_ActivatePreset"];
    public string CustomPresetsSectionTitle => _loc.CurrentLanguage == "ru" ? "ПОЛЬЗОВАТЕЛЬСКИЕ ПРЕСЕТЫ" : "CUSTOM PRESETS";
    public string CustomPresetsDesc => _loc.CurrentLanguage == "ru" ? "Создание резервных профилей и импорт внешних конфигураций .silkpreset" : "Create backup profiles and import external .silkpreset files";

    public string ProfilesCardTitle => _loc["Opt_Profiles_Title"];
    public string ProfilesCardSubtitle => _loc["Opt_Profiles_Subtitle"];
    public string ProfilesCountBadge => _loc["Opt_Profiles_Badge"];
    public string ControlCardTitle => _loc["Opt_Control_Title"];
    public string ControlCardSubtitle => _loc["Opt_Control_Subtitle"];
    public string ProtectionActiveBadge => _loc["Opt_Safety_Active"];
    public string RestorePointSubtitle => _loc["Opt_Safety_Desc"];
    public string SelectedProfilePrefix => _loc["Opt_Selected_Profile"];
    public string TweaksCountSuffix => _loc["Opt_Tweaks_Suffix"];
    public string AuditSubtitle => _loc["Opt_Audit_Subtitle"];
    public string ClearLogsLabel => _loc["Opt_Clear_Logs"];
    public string ProgressHeader => _loc["Opt_Progress_Label"];
    public string SystemStatusHeader => _loc["Opt_System_Status_Label"];

    public OptimizationViewModel(
        ITweakEngine tweakEngine,
        IHardwareScanner hardwareScanner,
        ICompatibilityEngine compatibilityEngine,
        ISettingsService settingsService,
        ILocalizationService localizationService,
        SilkThread.Core.RestorePointManager.IRestorePointManager restorePointManager,
        IPresetManager presetManager)
    {
        _tweakEngine = tweakEngine;
        _hardwareScanner = hardwareScanner;
        _compatibilityEngine = compatibilityEngine;
        _settingsService = settingsService;
        _loc = localizationService;
        _restorePointManager = restorePointManager;
        _presetManager = presetManager;

        _createRestorePoint = _settingsService.Settings.AutoCreateRestorePoint;

        BuildPresets();

        _loc.LanguageChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(PageSubtitle));
            OnPropertyChanged(nameof(CreateRestorePointLabel));
            OnPropertyChanged(nameof(StartBtnLabel));
            OnPropertyChanged(nameof(ExecutionLogTitle));
            OnPropertyChanged(nameof(CreatePresetLabel));
            OnPropertyChanged(nameof(ActivatePresetLabel));
            OnPropertyChanged(nameof(CustomPresetsSectionTitle));
            OnPropertyChanged(nameof(CustomPresetsDesc));
            OnPropertyChanged(nameof(ProfilesCardTitle));
            OnPropertyChanged(nameof(ProfilesCardSubtitle));
            OnPropertyChanged(nameof(ProfilesCountBadge));
            OnPropertyChanged(nameof(ControlCardTitle));
            OnPropertyChanged(nameof(ControlCardSubtitle));
            OnPropertyChanged(nameof(ProtectionActiveBadge));
            OnPropertyChanged(nameof(RestorePointSubtitle));
            OnPropertyChanged(nameof(SelectedProfilePrefix));
            OnPropertyChanged(nameof(TweaksCountSuffix));
            OnPropertyChanged(nameof(AuditSubtitle));
            OnPropertyChanged(nameof(ClearLogsLabel));
            OnPropertyChanged(nameof(ProgressHeader));
            OnPropertyChanged(nameof(SystemStatusHeader));
            BuildPresets();
        };
    }

    private void BuildPresets()
    {
        var prevId = SelectedPreset?.Id;
        Presets.Clear();

        Presets.Add(new OptimizationPreset(
            "safe",
            _loc["Opt_Preset_Safe"],
            _loc["Opt_Preset_Safe_Desc"],
            "\uE72E", // Shield
            "100% SAFE",
            "#10B981",
            (t, h) => t.Risk == RiskLevel.Low && t.Recommendation == RecommendationLevel.Recommended
        ));

        Presets.Add(new OptimizationPreset(
            "gaming",
            _loc["Opt_Preset_Gaming"],
            _loc["Opt_Preset_Gaming_Desc"],
            "\uE7FC", // Controller
            "MAX FPS",
            "#6366F1",
            (t, h) => (t.Category == TweakCategory.CpuOptimization ||
                       t.Category == TweakCategory.GpuOptimization ||
                       t.Category == TweakCategory.GamingPerformance ||
                       t.Category == TweakCategory.InputAndLatency) &&
                      t.Risk <= RiskLevel.Medium
        ));

        Presets.Add(new OptimizationPreset(
            "network",
            _loc["Opt_Preset_Network"],
            _loc["Opt_Preset_Network_Desc"],
            "\uE839", // Globe
            "LOW LATENCY",
            "#38BDF8",
            (t, h) => t.Category == TweakCategory.NetworkOptimization && t.Risk <= RiskLevel.Medium
        ));

        Presets.Add(new OptimizationPreset(
            "minimal",
            _loc["Opt_Preset_Minimal"],
            _loc["Opt_Preset_Minimal_Desc"],
            "\uE9F5", // Clean / tasks
            "DEBLOAT",
            "#F59E0B",
            (t, h) => (t.Category == TweakCategory.BackgroundProcesses ||
                       t.Category == TweakCategory.WindowsServices ||
                       t.Category == TweakCategory.SecurityAndPrivacy) &&
                      t.Risk <= RiskLevel.Medium
        ));

        SelectedPreset = Presets.FirstOrDefault(p => p.Id == prevId) ?? Presets.FirstOrDefault();
        if (SelectedPreset != null)
        {
            SelectedPreset.IsSelected = true;
        }
    }

    public async Task InitializeAsync()
    {
        await RefreshTweaksToApplyAsync();
    }

    partial void OnSelectedPresetChanged(OptimizationPreset? value)
    {
        foreach (var p in Presets)
        {
            p.IsSelected = (p == value);
        }
        _ = RefreshTweaksToApplyAsync();
    }

    [RelayCommand]
    public void SelectPreset(OptimizationPreset preset)
    {
        SelectedPreset = preset;
    }

    [RelayCommand]
    public void ClearLogs()
    {
        ExecutionLog.Clear();
    }

    public async Task RefreshTweaksToApplyAsync()
    {
        if (SelectedPreset == null) return;

        var hw = await _hardwareScanner.ScanAsync();
        TweaksToApply.Clear();

        var matches = _tweakEngine.GetAllTweaks()
            .Where(t => SelectedPreset.Selector(t, hw))
            .Where(t => _compatibilityEngine.EvaluateCompatibility(t, hw).IsCompatible)
            .ToList();

        foreach (var def in matches)
        {
            var item = new TweakItemViewModel(def, _tweakEngine);
            _ = item.RefreshStatusAsync();
            TweaksToApply.Add(item);
        }

        ProgressTotal = TweaksToApply.Count;
        CurrentStatusText = _loc.CurrentLanguage == "ru"
            ? $"Отобрано {TweaksToApply.Count} твиков для профиля «{SelectedPreset.Name}»"
            : $"Selected {TweaksToApply.Count} tweaks for «{SelectedPreset.Name}»";
    }

    [RelayCommand]
    public async Task StartOptimizationAsync()
    {
        if (IsExecuting || TweaksToApply.Count == 0) return;

        IsExecuting = true;
        ProgressCurrent = 0;
        ProgressPercent = 0;
        ExecutionLog.Clear();

        CurrentStatusText = _loc["Opt_Executing"];

        try
        {
            var targetIds = TweaksToApply.Select(t => t.Id).ToList();

            ExecutionLog.Add($"[{DateTime.Now:HH:mm:ss}] Запуск пакетной оптимизации: {SelectedPreset?.Name}");

            if (CreateRestorePoint)
            {
                CurrentStatusText = _loc.CurrentLanguage == "ru"
                    ? "Создание системной точки восстановления..."
                    : "Creating system restore point...";
                ExecutionLog.Add($"[{DateTime.Now:HH:mm:ss}] {_loc["Opt_CreateRestorePoint"]}...");

                var rpResult = await _restorePointManager.CreateRestorePointAsync(
                    $"SilkThread Batch Optimization ({SelectedPreset?.Name ?? "Preset"})");

                if (rpResult.Success)
                {
                    ExecutionLog.Add($"[{DateTime.Now:HH:mm:ss}] ✓ Точка восстановления создана: {rpResult.Message}");
                }
                else
                {
                    ExecutionLog.Add($"[{DateTime.Now:HH:mm:ss}] ⚠ Точка восстановления не создана: {rpResult.Message}");
                }
            }

            int count = 0;
            foreach (var tweak in TweaksToApply)
            {
                count++;
                ProgressCurrent = count;
                ProgressPercent = (double)count / TweaksToApply.Count * 100.0;

                CurrentStatusText = $"[{count}/{TweaksToApply.Count}] {tweak.Title}";

                var res = await _tweakEngine.ApplyTweakAsync(tweak.Id);
                ExecutionLog.Add($"[{DateTime.Now:HH:mm:ss}] {(res.Success ? "✓" : "✗")} {tweak.Title}: {res.Message}");

                if (res.Success)
                {
                    tweak.Status = TweakStatus.Applied;
                }
            }

            CurrentStatusText = _loc.CurrentLanguage == "ru"
                ? $"✓ Пакетная оптимизация успешно завершена! Применено {count} параметров."
                : $"✓ Batch optimization completed! Applied {count} settings.";

            ExecutionLog.Add($"[{DateTime.Now:HH:mm:ss}] Все операции успешно выполнены.");
        }
        finally
        {
            IsExecuting = false;
        }
    }

    [RelayCommand]
    public async Task CreatePresetAsync()
    {
        var tweaks = _tweakEngine.GetAllTweaks();
        var appliedTweaks = new List<string>();
        foreach (var t in tweaks)
        {
            var status = await _tweakEngine.CheckTweakStatusAsync(t.Id);
            if (status == TweakStatus.Applied)
            {
                appliedTweaks.Add(t.Id);
            }
        }

        if (appliedTweaks.Count == 0)
        {
            CurrentStatusText = _loc["Tweaks_NoAppliedForPreset"];
            ExecutionLog.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {_loc["Tweaks_NoAppliedForPreset"]}");
            return;
        }

        var dialog = new CreatePresetDialog(appliedTweaks.Count);
        dialog.XamlRoot = App.MainWindowInstance?.Content?.XamlRoot;
        await dialog.ShowAsync();

        if (dialog.IsConfirmed)
        {
            var preset = await _presetManager.CreatePresetFromTweakIdsAsync(
                dialog.PresetName,
                dialog.PresetDescription,
                appliedTweaks,
                dialog.PresetAuthor
            );

            string defaultFileName = $"{SanitizeFileName(dialog.PresetName)}.silkpreset";
            var savePath = await FilePickerHelper.PickSavePresetFileAsync(defaultFileName);
            if (!string.IsNullOrEmpty(savePath))
            {
                await _presetManager.ExportPresetAsync(preset, savePath);
                var msg = string.Format(_loc["Tweaks_PresetSaved"], preset.Name);
                CurrentStatusText = msg;
                ExecutionLog.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {msg}");
            }
        }
    }

    [RelayCommand]
    public async Task ActivatePresetAsync()
    {
        var filePath = await FilePickerHelper.PickOpenPresetFileAsync();
        if (string.IsNullOrEmpty(filePath)) return;

        try
        {
            var evaluated = await _presetManager.LoadAndEvaluatePresetAsync(filePath);
            var dialog = new PresetReviewDialog(evaluated, _tweakEngine);
            dialog.XamlRoot = App.MainWindowInstance?.Content?.XamlRoot;
            await dialog.ShowAsync();

            if (dialog.WasApplied && dialog.AppliedCount > 0)
            {
                var msg = string.Format(_loc["Tweaks_PresetApplied"], evaluated.Preset.Name, dialog.AppliedCount);
                CurrentStatusText = msg;
                ExecutionLog.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {msg}");
            }
        }
        catch (Exception ex)
        {
            var err = $"Ошибка загрузки пресета: {ex.Message}";
            CurrentStatusText = err;
            ExecutionLog.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {err}");
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        var clean = new string(name.Where(c => !invalid.Contains(c)).ToArray());
        return string.IsNullOrWhiteSpace(clean) ? "SilkPreset" : clean;
    }
}
