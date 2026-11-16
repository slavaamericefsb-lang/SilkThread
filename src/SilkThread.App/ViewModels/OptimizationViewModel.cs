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
using SilkThread.Core.Execution;
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
    private int _selectedProfileIndex;

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
    private string _currentStatusText = "";

    public ObservableCollection<TweakItemViewModel> TweaksToApply { get; } = new();
    public ObservableCollection<string> ExecutionLog { get; } = new();

    // Localized properties
    public string PageTitle => _loc["Opt_Title"];
    public string PageSubtitle => _loc["Opt_Subtitle"];
    public string CreateRestorePointLabel => _loc["Opt_CreateRestorePoint"];
    public string StartBtnLabel => _loc["Opt_Start_Btn"];
    public string ExecutionLogTitle => _loc["Opt_ExecutionLogTitle"];
    public string CreatePresetLabel => _loc["Tweaks_CreatePreset"];
    public string ActivatePresetLabel => _loc["Tweaks_ActivatePreset"];
    public string CustomPresetsSectionTitle => _loc["Opt_CustomPresetsSectionTitle"];
    public string CustomPresetsDesc => _loc["Opt_CustomPresetsDesc"];

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
        _currentStatusText = _loc["Opt_Status_Ready"];

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
            if (!IsExecuting)
            {
                CurrentStatusText = _loc["Opt_Status_Ready"];
            }
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
            "LOW RISK",
            "#10B981",
            (t, h) => t.Risk == RiskLevel.Low && t.Recommendation == RecommendationLevel.Recommended
        ));

        Presets.Add(new OptimizationPreset(
            "gaming",
            _loc["Opt_Preset_Gaming"],
            _loc["Opt_Preset_Gaming_Desc"],
            "\uE7FC", // Controller
            "GAMING",
            "#6366F1",
            (t, h) => (t.Category == TweakCategory.CpuOptimization ||
                       t.Category == TweakCategory.GpuOptimization ||
                       t.Category == TweakCategory.GamingPerformance ||
                       t.Category == TweakCategory.InputAndLatency) &&
                      t.Risk <= RiskLevel.Medium && t.Recommendation <= RecommendationLevel.Optional
        ));

        Presets.Add(new OptimizationPreset(
            "network",
            _loc["Opt_Preset_Network"],
            _loc["Opt_Preset_Network_Desc"],
            "\uE839", // Globe
            "NETWORK",
            "#38BDF8",
            (t, h) => t.Category == TweakCategory.NetworkOptimization && t.Risk <= RiskLevel.Medium && t.Recommendation <= RecommendationLevel.Optional
        ));

        Presets.Add(new OptimizationPreset(
            "minimal",
            _loc["Opt_Preset_Minimal"],
            _loc["Opt_Preset_Minimal_Desc"],
            "\uE9F5", // Clean / tasks
            "BACKGROUND",
            "#F59E0B",
            (t, h) => (t.Category == TweakCategory.BackgroundProcesses ||
                       t.Category == TweakCategory.WindowsServices ||
                       t.Category == TweakCategory.SecurityAndPrivacy) &&
                      t.Risk <= RiskLevel.Medium && t.Recommendation <= RecommendationLevel.Optional
        ));

        int targetIndex = _settingsService.LastOptimizationProfileIndex;
        if (targetIndex < 0 || targetIndex >= Presets.Count)
        {
            targetIndex = 0;
        }

        SelectedProfileIndex = targetIndex;
        SelectedPreset = Presets[targetIndex];
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

        if (value != null)
        {
            int index = Presets.IndexOf(value);
            if (index >= 0)
            {
                if (SelectedProfileIndex != index)
                {
                    SelectedProfileIndex = index;
                }
                if (_settingsService.LastOptimizationProfileIndex != index)
                {
                    _settingsService.LastOptimizationProfileIndex = index;
                    _settingsService.Save();
                }
            }
        }

        _ = RefreshTweaksToApplyAsync();
    }

    partial void OnSelectedProfileIndexChanged(int value)
    {
        if (value >= 0 && value < Presets.Count && SelectedPreset != Presets[value])
        {
            SelectedPreset = Presets[value];
        }
    }

    [RelayCommand]
    public void SelectPreset(OptimizationPreset preset)
    {
        SelectedPreset = preset;
        int index = Presets.IndexOf(preset);
        if (index >= 0)
        {
            SelectedProfileIndex = index;
            _settingsService.LastOptimizationProfileIndex = index;
            _settingsService.Save();
        }
    }

    [RelayCommand]
    public void ClearLogs()
    {
        ExecutionLog.Clear();
    }

    private Task<HardwareProfile>? _previewHardware;
    private DateTimeOffset _previewHardwareExpires;
    private int _previewRequest;

    public async Task RefreshTweaksToApplyAsync()
    {
        int request = ++_previewRequest;
        var preset = SelectedPreset;
        if (preset == null) return;
        try
        {
            // Share a pending observation when the user clicks several presets quickly.
            // Only previews reuse this short-lived snapshot; execution scans fresh below.
            if (_previewHardware == null || _previewHardware.IsFaulted || _previewHardware.IsCanceled
                || (_previewHardware.IsCompleted && DateTimeOffset.UtcNow >= _previewHardwareExpires))
            {
                _previewHardware = _hardwareScanner.ScanAsync();
                _previewHardwareExpires = DateTimeOffset.UtcNow.AddSeconds(30);
            }
            var hw = await _previewHardware;
            if (request != _previewRequest) return;
            var matches = _tweakEngine.GetAllTweaks()
                .Where(t => t.AuditBlockReason == null && preset.Selector(t, hw))
                .Where(t => _compatibilityEngine.EvaluateCompatibility(t, hw).IsCompatible)
                .Select(t => new TweakItemViewModel(t, _tweakEngine)).ToList();
            TweaksToApply.Clear();
            foreach (var item in matches) TweaksToApply.Add(item);
            ProgressTotal = matches.Count;
            CurrentStatusText = string.Format(_loc["Opt_Status_SelectedFormat"], matches.Count, preset.Name);
            await Task.WhenAll(matches.Select(item => item.RefreshStatusAsync()));
        }
        catch (Exception ex)
        {
            App.Log($"[Optimization preview]: {ex}");
            if (request == _previewRequest)
                CurrentStatusText = _loc.CurrentLanguage == "ru" ? "Не удалось обновить список. Выберите профиль повторно." : "Unable to refresh. Select the preset again.";
        }
    }

    [RelayCommand]
    public async Task StartOptimizationAsync()
    {
        if (IsExecuting || SelectedPreset == null) return;

        IsExecuting = true;
        List<TweakItemViewModel> selectedTweaks;
        var selectedPreset = SelectedPreset;
        try
        {
            var hardware = await _hardwareScanner.ScanAsync();
            var preset = await _presetManager.CreatePresetFromTweakIdsAsync(
                selectedPreset.Name, selectedPreset.Description,
                _tweakEngine.GetAllTweaks().Where(t => t.AuditBlockReason == null && selectedPreset.Selector(t, hardware)).Select(t => t.Id), "SilkThread");
            var evaluated = await _presetManager.EvaluatePresetAsync(preset, hardware);
            var review = new PresetReviewDialog(evaluated, _tweakEngine, selectionOnly: true)
            {
                XamlRoot = App.MainWindowInstance?.Content?.XamlRoot
            };
            await review.ShowAsync();
            if (!review.WasConfirmed) { IsExecuting = false; return; }
            selectedTweaks = review.SelectedTweakIds.Select(id => new TweakItemViewModel(_tweakEngine.GetTweakById(id)!, _tweakEngine)).ToList();
        }
        catch (Exception ex)
        {
            App.Log($"[Optimization review]: {ex}");
            CurrentStatusText = ex.Message;
            IsExecuting = false;
            return;
        }
        ProgressTotal = selectedTweaks.Count;
        ProgressCurrent = 0;
        ProgressPercent = 0;
        ExecutionLog.Clear();

        CurrentStatusText = _loc["Opt_Executing"];

        Action<string> logAction = msg =>
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var line = $"[{timestamp}] {msg}";
            var dq = App.MainDispatcherQueue ?? App.MainWindowInstance?.DispatcherQueue;
            if (dq != null)
            {
                if (!dq.HasThreadAccess)
                {
                    dq.TryEnqueue(() =>
                    {
                        try { ExecutionLog.Add(line); } catch { }
                    });
                }
                else
                {
                    try { ExecutionLog.Add(line); } catch { }
                }
            }
        };

        TweakExecutionService.Instance.OperationLogged += logAction;

        try
        {
            logAction($"Запуск пакетной оптимизации: «{selectedPreset.Name}»");

            if (CreateRestorePoint)
            {
                CurrentStatusText = _loc["Opt_Status_CreatingRestore"];
                logAction($"[RESTORE] {_loc["Opt_CreateRestorePoint"]}...");

                var rpResult = await _restorePointManager.CreateRestorePointAsync(
                    $"SilkThread Batch Optimization ({selectedPreset.Name ?? "Preset"})");

                if (rpResult.Success)
                {
                    logAction($"[RESTORE] ✓ Точка восстановления создана: {rpResult.Message}");
                }
                else
                {
                    logAction($"[RESTORE] ⚠ Точка восстановления не создана: {rpResult.Message}");
                }
            }

            var hw = await _hardwareScanner.ScanAsync();
            int count = 0;
            int appliedCount = 0;
            int skippedCount = 0;
            int failedCount = 0;

            foreach (var tweak in selectedTweaks)
            {
                count++;
                ProgressCurrent = count;
                ProgressPercent = (double)count / selectedTweaks.Count * 100.0;

                CurrentStatusText = $"[{count}/{selectedTweaks.Count}] {tweak.Title}";

                var def = _tweakEngine.GetTweakById(tweak.Id);
                if (def != null)
                {
                    var compat = _compatibilityEngine.EvaluateCompatibility(def, hw);
                    if (!compat.IsCompatible)
                    {
                        skippedCount++;
                        logAction($"[SKIPPED] ⚠ {tweak.Title} -> {compat.Reason}");
                        continue;
                    }
                }

                logAction($"[TWEAK] >>> [{count}/{selectedTweaks.Count}] {tweak.Title}");

                try
                {
                    var res = await _tweakEngine.ApplyTweakAsync(tweak.Id);
                    if (res.Success)
                    {
                        appliedCount++;
                        tweak.Status = TweakStatus.Applied;
                        logAction($"[STATUS] ✓ {tweak.Title}: {res.Message}");
                    }
                    else
                    {
                        failedCount++;
                        logAction($"[ERROR] ✗ {tweak.Title}: {res.Message}");
                    }
                }
                catch (Exception ex)
                {
                    failedCount++;
                    logAction($"[ERROR] ✗ Исключение при выполнении {tweak.Title}: {ex.Message}");
                }
            }

            CurrentStatusText = string.Format(_loc["Opt_Status_CompletedFormat"], appliedCount, failedCount, skippedCount);

            logAction($"Все операции завершены. Успешно: {appliedCount}, Сбоев: {failedCount}, Пропущено: {skippedCount}.");
        }
        finally
        {
            TweakExecutionService.Instance.OperationLogged -= logAction;
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
