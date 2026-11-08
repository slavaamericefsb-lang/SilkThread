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

public partial class CategoryTabItem : ObservableObject
{
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "\uE713";
    public TweakCategory? Category { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BadgeText))]
    private int _count;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BadgeText))]
    private int _appliedCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BackgroundBrush))]
    [NotifyPropertyChangedFor(nameof(BorderBrush))]
    [NotifyPropertyChangedFor(nameof(ForegroundBrush))]
    [NotifyPropertyChangedFor(nameof(BadgeBackgroundBrush))]
    [NotifyPropertyChangedFor(nameof(BadgeForegroundBrush))]
    private bool _isSelected;

    public string BadgeText => AppliedCount > 0 ? $"{AppliedCount}/{Count}" : $"{Count}";

    // Minimal Dark UI Design Tokens: Accent #806BFF, Surface #202431, Border #2B3040
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_catSelectedBg = new(Windows.UI.Color.FromArgb(255, 128, 107, 255)); // #806BFF
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_catDefaultBg = new(Windows.UI.Color.FromArgb(255, 32, 36, 49)); // #202431
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_catSelectedBorder = new(Windows.UI.Color.FromArgb(255, 128, 107, 255)); // #806BFF
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_catDefaultBorder = new(Windows.UI.Color.FromArgb(255, 43, 48, 64)); // #2B3040
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_catSelectedFg = new(Windows.UI.Color.FromArgb(255, 255, 255, 255));
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_catDefaultFg = new(Windows.UI.Color.FromArgb(255, 243, 244, 248)); // #F3F4F8
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_catSelectedBadgeBg = new(Windows.UI.Color.FromArgb(50, 255, 255, 255));
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_catDefaultBadgeBg = new(Windows.UI.Color.FromArgb(255, 24, 27, 37)); // #181B25
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_catSelectedBadgeFg = new(Windows.UI.Color.FromArgb(255, 255, 255, 255));
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_catDefaultBadgeFg = new(Windows.UI.Color.FromArgb(255, 150, 156, 171)); // #969CAB

    public Microsoft.UI.Xaml.Media.Brush BackgroundBrush => IsSelected ? s_catSelectedBg : s_catDefaultBg;
    public Microsoft.UI.Xaml.Media.Brush BorderBrush => IsSelected ? s_catSelectedBorder : s_catDefaultBorder;
    public Microsoft.UI.Xaml.Media.Brush ForegroundBrush => IsSelected ? s_catSelectedFg : s_catDefaultFg;
    public Microsoft.UI.Xaml.Media.Brush BadgeBackgroundBrush => IsSelected ? s_catSelectedBadgeBg : s_catDefaultBadgeBg;
    public Microsoft.UI.Xaml.Media.Brush BadgeForegroundBrush => IsSelected ? s_catSelectedBadgeFg : s_catDefaultBadgeFg;
}

public partial class TweaksViewModel : ObservableObject
{
    private readonly ITweakEngine _tweakEngine;
    private readonly ICompatibilityEngine _compatibilityEngine;
    private readonly IHardwareScanner _hardwareScanner;
    private readonly ISettingsService _settingsService;
    private readonly ILocalizationService _loc;
    private readonly IPresetManager _presetManager;

    private List<TweakItemViewModel> _allTweaks = new();

    public ObservableCollection<CategoryTabItem> Categories { get; } = new();

    [ObservableProperty]
    private CategoryTabItem? _selectedCategory;

    [ObservableProperty]
    private string _searchQuery = "";

    [ObservableProperty]
    private string _selectedFilterType = "All"; // All, Recommended, Applied, Safe

    [ObservableProperty]
    private bool _hideIncompatible = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedTweak))]
    private TweakItemViewModel? _selectedTweak;

    public bool HasSelectedTweak => SelectedTweak != null;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _totalCount = 265;

    [ObservableProperty]
    private int _appliedCount;

    [ObservableProperty]
    private int _compatibleCount;

    [ObservableProperty]
    private string _filteredCountText = "";

    [ObservableProperty]
    private string _batchStatusMessage = "";

    public ObservableCollection<TweakItemViewModel> FilteredTweaks { get; } = new();

    // Localized UI strings for Tweaks Page
    public string PageTitle => _loc["Tweaks_Title"];
    public string PageSubtitle => _loc["Tweaks_Subtitle"];
    public string SearchPlaceholder => _loc.CurrentLanguage == "ru" ? "Поиск среди 265 твиков..." : "Search among 265 tweaks...";
    public string FilterAllLabel => _loc["Tweaks_Filter_All"];
    public string FilterRecommendedLabel => _loc["Tweaks_Filter_Recommended"];
    public string FilterAppliedLabel => _loc["Tweaks_Filter_Applied"];
    public string FilterNotAppliedLabel => _loc["Tweaks_Filter_NotApplied"];
    public string FilterSafeLabel => _loc["Tweaks_Filter_Safe"];
    public string CompatibleOnlyLabel => _loc.CurrentLanguage == "ru" ? "Скрыть несовместимые" : "Hide incompatible";
    public string BatchApplyRecLabel => _loc["Tweaks_Batch_ApplyRec"];
    public string BatchRevertLabel => _loc["Tweaks_Batch_Revert"];
    public string CreatePresetLabel => _loc["Tweaks_CreatePreset"];
    public string ActivatePresetLabel => _loc["Tweaks_ActivatePreset"];
    public string InspectorEmptyLabel => _loc["Tweaks_Inspector_Empty"];
    public string ApplyLabel => _loc["Tweaks_Apply"];
    public string RevertLabel => _loc["Tweaks_Revert"];
    public string SectionOverviewLabel => _loc["Tweaks_Section_Overview"];
    public string SectionMechanismLabel => _loc["Tweaks_Section_Mechanism"];
    public string SectionProblemLabel => _loc["Tweaks_Section_Problem"];
    public string SectionResultLabel => _loc["Tweaks_Section_Result"];
    public string SectionRiskLabel => _loc["Tweaks_Section_Risk"];
    public string IncompatibleWarningLabel => _loc["Tweaks_Incompatible_Warning"];
    public string RequiresRebootLabel => _loc["Tweaks_RequiresReboot"];
    public string TotalBadgeLabel => _loc["Tweaks_TotalBadge"];
    public string AppliedBadgeLabel => _loc["Tweaks_AppliedBadge"];
    public string InspectorDetailsLabel => _loc["Tweaks_Inspector_Details"];
    public string InspectorIdLabel => _loc["Tweaks_Inspector_Id"];
    public string InspectorCategoryLabel => _loc["Tweaks_Inspector_Category"];
    public string InspectorRiskLabel => _loc["Tweaks_Inspector_Risk"];
    public string InspectorRecLabel => _loc["Tweaks_Inspector_Rec"];
    public string InspectorRebootLabel => _loc["Tweaks_Inspector_Reboot"];
    public string RebootRequiredLabel => _loc["Tweaks_RebootRequired"];
    public string RebootNotRequiredLabel => _loc["Tweaks_RebootNotRequired"];
    public string RebootBadgeLabel => _loc["Tweaks_RebootBadge"];
    public string AllPcsLabel => _loc["Tweaks_AllPcs"];
    public string TweaksSuffixLabel => _loc.CurrentLanguage == "ru" ? "твиков" : "tweaks";
    public string TotalPrefixLabel => _loc["Tweaks_Stat_Total"];
    public string AppliedPrefixLabel => _loc["Tweaks_Stat_Applied"];
    public string ScrollLeftTooltip => _loc.CurrentLanguage == "ru" ? "Назад" : "Back";
    public string ScrollRightTooltip => _loc.CurrentLanguage == "ru" ? "Вперед" : "Forward";
    public string InspectorEmptySubLabel => _loc["Tweaks_Inspector_EmptySub"];
    public string SectionCompatLabel => _loc["Tweaks_Section_Compat"];
    public string FullCompatTitle => _loc["Tweaks_FullCompat_Title"];
    public string FullCompatDesc => _loc["Tweaks_FullCompat_Desc"];

    // Filter Button Visual Properties (Minimal Dark UI tokens)
    public bool IsFilterAll => SelectedFilterType == "All";
    public bool IsFilterRecommended => SelectedFilterType == "Recommended";
    public bool IsFilterApplied => SelectedFilterType == "Applied";
    public bool IsFilterNotApplied => SelectedFilterType == "NotApplied";

    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_filterActiveBg = new(Windows.UI.Color.FromArgb(255, 43, 48, 64)); // #2B3040
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_filterInactiveBg = new(Windows.UI.Color.FromArgb(255, 32, 36, 49)); // #202431
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_filterActiveBorder = new(Windows.UI.Color.FromArgb(255, 128, 107, 255)); // #806BFF
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_filterAppliedActiveBorder = new(Windows.UI.Color.FromArgb(255, 34, 197, 94)); // #22C55E
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_filterNotAppliedActiveBorder = new(Windows.UI.Color.FromArgb(255, 128, 107, 255)); // #806BFF
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_filterInactiveBorder = new(Windows.UI.Color.FromArgb(255, 43, 48, 64)); // #2B3040

    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_filterActiveFg = new(Windows.UI.Color.FromArgb(255, 243, 244, 248)); // #F3F4F8
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_filterRecFg = new(Windows.UI.Color.FromArgb(255, 128, 107, 255)); // #806BFF
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_filterAppliedFg = new(Windows.UI.Color.FromArgb(255, 34, 197, 94)); // #22C55E
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_filterNotAppliedFg = new(Windows.UI.Color.FromArgb(255, 243, 244, 248)); // #F3F4F8
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_filterInactiveFg = new(Windows.UI.Color.FromArgb(255, 150, 156, 171)); // #969CAB

    public Microsoft.UI.Xaml.Media.SolidColorBrush FilterAllBg => IsFilterAll ? s_filterActiveBg : s_filterInactiveBg;
    public Microsoft.UI.Xaml.Media.SolidColorBrush FilterAllBorder => IsFilterAll ? s_filterActiveBorder : s_filterInactiveBorder;
    public Microsoft.UI.Xaml.Media.SolidColorBrush FilterAllFg => IsFilterAll ? s_filterActiveFg : s_filterInactiveFg;

    public Microsoft.UI.Xaml.Media.SolidColorBrush FilterRecBg => IsFilterRecommended ? s_filterActiveBg : s_filterInactiveBg;
    public Microsoft.UI.Xaml.Media.SolidColorBrush FilterRecBorder => IsFilterRecommended ? s_filterActiveBorder : s_filterInactiveBorder;
    public Microsoft.UI.Xaml.Media.SolidColorBrush FilterRecFg => IsFilterRecommended ? s_filterRecFg : s_filterInactiveFg;

    public Microsoft.UI.Xaml.Media.SolidColorBrush FilterAppliedBg => IsFilterApplied ? s_filterActiveBg : s_filterInactiveBg;
    public Microsoft.UI.Xaml.Media.SolidColorBrush FilterAppliedBorder => IsFilterApplied ? s_filterAppliedActiveBorder : s_filterInactiveBorder;
    public Microsoft.UI.Xaml.Media.SolidColorBrush FilterAppliedFg => IsFilterApplied ? s_filterAppliedFg : s_filterInactiveFg;

    public Microsoft.UI.Xaml.Media.SolidColorBrush FilterNotAppliedBg => IsFilterNotApplied ? s_filterActiveBg : s_filterInactiveBg;
    public Microsoft.UI.Xaml.Media.SolidColorBrush FilterNotAppliedBorder => IsFilterNotApplied ? s_filterNotAppliedActiveBorder : s_filterInactiveBorder;
    public Microsoft.UI.Xaml.Media.SolidColorBrush FilterNotAppliedFg => IsFilterNotApplied ? s_filterNotAppliedFg : s_filterInactiveFg;

    public TweaksViewModel(
        ITweakEngine tweakEngine,
        ICompatibilityEngine compatibilityEngine,
        IHardwareScanner hardwareScanner,
        ISettingsService settingsService,
        ILocalizationService localizationService,
        IPresetManager presetManager)
    {
        _tweakEngine = tweakEngine;
        _compatibilityEngine = compatibilityEngine;
        _hardwareScanner = hardwareScanner;
        _settingsService = settingsService;
        _loc = localizationService;
        _presetManager = presetManager;

        HideIncompatible = _settingsService.Settings.HideIncompatibleTweaks;

        _loc.LanguageChanged += (s, e) =>
        {
            RefreshLocalizedStrings();
            foreach (var tweak in _allTweaks)
            {
                tweak.RefreshLocalization();
            }
            BuildCategoryTabs();
            ApplyFilters();
        };

        BuildCategoryTabs();
    }

    private void RefreshLocalizedStrings()
    {
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(PageSubtitle));
        OnPropertyChanged(nameof(SearchPlaceholder));
        OnPropertyChanged(nameof(FilterAllLabel));
        OnPropertyChanged(nameof(FilterRecommendedLabel));
        OnPropertyChanged(nameof(FilterAppliedLabel));
        OnPropertyChanged(nameof(FilterNotAppliedLabel));
        OnPropertyChanged(nameof(FilterSafeLabel));
        OnPropertyChanged(nameof(CompatibleOnlyLabel));
        OnPropertyChanged(nameof(BatchApplyRecLabel));
        OnPropertyChanged(nameof(BatchRevertLabel));
        OnPropertyChanged(nameof(CreatePresetLabel));
        OnPropertyChanged(nameof(ActivatePresetLabel));
        OnPropertyChanged(nameof(InspectorEmptyLabel));
        OnPropertyChanged(nameof(ApplyLabel));
        OnPropertyChanged(nameof(RevertLabel));
        OnPropertyChanged(nameof(SectionOverviewLabel));
        OnPropertyChanged(nameof(SectionMechanismLabel));
        OnPropertyChanged(nameof(SectionProblemLabel));
        OnPropertyChanged(nameof(SectionResultLabel));
        OnPropertyChanged(nameof(SectionRiskLabel));
        OnPropertyChanged(nameof(IncompatibleWarningLabel));
        OnPropertyChanged(nameof(RequiresRebootLabel));
        OnPropertyChanged(nameof(TotalBadgeLabel));
        OnPropertyChanged(nameof(AppliedBadgeLabel));
        OnPropertyChanged(nameof(InspectorDetailsLabel));
        OnPropertyChanged(nameof(InspectorIdLabel));
        OnPropertyChanged(nameof(InspectorCategoryLabel));
        OnPropertyChanged(nameof(InspectorRiskLabel));
        OnPropertyChanged(nameof(InspectorRecLabel));
        OnPropertyChanged(nameof(InspectorRebootLabel));
        OnPropertyChanged(nameof(RebootRequiredLabel));
        OnPropertyChanged(nameof(RebootNotRequiredLabel));
        OnPropertyChanged(nameof(RebootBadgeLabel));
        OnPropertyChanged(nameof(AllPcsLabel));
        OnPropertyChanged(nameof(TweaksSuffixLabel));
        OnPropertyChanged(nameof(TotalPrefixLabel));
        OnPropertyChanged(nameof(AppliedPrefixLabel));
        OnPropertyChanged(nameof(ScrollLeftTooltip));
        OnPropertyChanged(nameof(ScrollRightTooltip));
        OnPropertyChanged(nameof(InspectorEmptySubLabel));
        OnPropertyChanged(nameof(SectionCompatLabel));
        OnPropertyChanged(nameof(FullCompatTitle));
        OnPropertyChanged(nameof(FullCompatDesc));
    }

    private static string GetShortCategoryName(TweakCategory cat, bool isRu) => cat switch
    {
        TweakCategory.CpuOptimization => isRu ? "ЦП" : "CPU",
        TweakCategory.GpuOptimization => isRu ? "Видеокарта" : "GPU",
        TweakCategory.RamOptimization => isRu ? "Память" : "RAM",
        TweakCategory.GamingPerformance => isRu ? "Игры" : "Gaming",
        TweakCategory.WindowsPerformance => isRu ? "Windows" : "Windows",
        TweakCategory.InputAndLatency => isRu ? "Задержка ввода" : "Input Latency",
        TweakCategory.NetworkOptimization => isRu ? "Сеть" : "Network",
        TweakCategory.StorageOptimization => isRu ? "Хранилище" : "Storage",
        TweakCategory.BackgroundProcesses => isRu ? "Процессы" : "Processes",
        TweakCategory.WindowsServices => isRu ? "Службы" : "Services",
        TweakCategory.PowerManagement => isRu ? "Питание" : "Power",
        TweakCategory.WindowsExplorerAndUi => isRu ? "Проводник" : "Explorer",
        TweakCategory.SecurityAndPrivacy => isRu ? "Приватность" : "Privacy",
        TweakCategory.SystemMaintenance => isRu ? "Обслуживание" : "Maintenance",
        TweakCategory.AdvancedTweaks => isRu ? "Тонкая настройка" : "Advanced",
        _ => cat.ToString()
    };

    private void BuildCategoryTabs()
    {
        var prevSelectedCategory = SelectedCategory?.Category;
        Categories.Clear();

        bool isRu = _loc.CurrentLanguage == "ru";

        Categories.Add(new CategoryTabItem
        {
            Name = isRu ? "Все" : "All",
            Icon = "\uE71D", // All apps
            Category = null,
            Count = _allTweaks.Count > 0 ? _allTweaks.Count : 265
        });

        foreach (TweakCategory cat in Enum.GetValues<TweakCategory>())
        {
            Categories.Add(new CategoryTabItem
            {
                Name = GetShortCategoryName(cat, isRu),
                Icon = GetCategoryIcon(cat),
                Category = cat,
                Count = 0
            });
        }

        if (prevSelectedCategory.HasValue)
        {
            SelectedCategory = Categories.FirstOrDefault(c => c.Category == prevSelectedCategory.Value) ?? Categories[0];
        }
        else
        {
            SelectedCategory = Categories[0];
        }
    }

    private string GetCategoryIcon(TweakCategory cat) => cat switch
    {
        TweakCategory.CpuOptimization => "\uE9D9",
        TweakCategory.GpuOptimization => "\uE790",
        TweakCategory.RamOptimization => "\uE7C5",
        TweakCategory.GamingPerformance => "\uE7FC",
        TweakCategory.WindowsPerformance => "\uE945",
        TweakCategory.InputAndLatency => "\uE964",
        TweakCategory.NetworkOptimization => "\uE839",
        TweakCategory.StorageOptimization => "\uEDA2",
        TweakCategory.BackgroundProcesses => "\uE9F5",
        TweakCategory.WindowsServices => "\uE895",
        TweakCategory.PowerManagement => "\uE7E8",
        TweakCategory.WindowsExplorerAndUi => "\uE8B7",
        TweakCategory.SecurityAndPrivacy => "\uE72E",
        TweakCategory.SystemMaintenance => "\uE90F",
        TweakCategory.AdvancedTweaks => "\uE7BE",
        _ => "\uE713"
    };

    public async Task InitializeAsync()
    {
        if (_allTweaks.Count > 0) return;

        IsLoading = true;
        try
        {
            var hardware = await _hardwareScanner.ScanAsync();
            var defs = _tweakEngine.GetAllTweaks();

            _allTweaks = defs.Select(d =>
            {
                var vm = new TweakItemViewModel(d, _tweakEngine);
                vm.Compatibility = _compatibilityEngine.EvaluateCompatibility(d, hardware);
                vm.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(TweakItemViewModel.Status))
                    {
                        UpdateCounts();
                        if (SelectedFilterType == "Applied" || SelectedFilterType == "NotApplied")
                        {
                            ApplyFilters();
                        }
                    }
                };
                return vm;
            }).ToList();

            TotalCount = _allTweaks.Count;
            CompatibleCount = _allTweaks.Count(t => t.IsCompatible);

            // Check current statuses concurrently
            await Task.WhenAll(_allTweaks.Select(t => t.RefreshStatusAsync()));
            UpdateCounts();

            UpdateCategoryCounts();
            ApplyFilters();

            if (FilteredTweaks.Count > 0)
            {
                SelectedTweak = FilteredTweaks[0];
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateCounts()
    {
        AppliedCount = _allTweaks.Count(t => t.IsApplied);
        UpdateCategoryCounts();
    }

    private void UpdateCategoryCounts()
    {
        if (Categories.Count == 0) return;

        Categories[0].Count = _allTweaks.Count;
        Categories[0].AppliedCount = _allTweaks.Count(t => t.IsApplied);

        for (int i = 1; i < Categories.Count; i++)
        {
            var tab = Categories[i];
            if (tab.Category.HasValue)
            {
                var inCat = _allTweaks.Where(t => t.Category == tab.Category.Value).ToList();
                tab.Count = inCat.Count;
                tab.AppliedCount = inCat.Count(t => t.IsApplied);
            }
        }
    }

    partial void OnSelectedTweakChanged(TweakItemViewModel? oldValue, TweakItemViewModel? newValue)
    {
        if (oldValue != null) oldValue.IsSelected = false;
        if (newValue != null) newValue.IsSelected = true;
    }

    partial void OnSelectedCategoryChanged(CategoryTabItem? value)
    {
        foreach (var c in Categories) c.IsSelected = (c == value);
        ApplyFilters();
    }

    partial void OnSearchQueryChanged(string value) => ApplyFilters();
    partial void OnSelectedFilterTypeChanged(string value)
    {
        OnPropertyChanged(nameof(IsFilterAll));
        OnPropertyChanged(nameof(IsFilterRecommended));
        OnPropertyChanged(nameof(IsFilterApplied));
        OnPropertyChanged(nameof(IsFilterNotApplied));
        OnPropertyChanged(nameof(FilterAllBg));
        OnPropertyChanged(nameof(FilterAllBorder));
        OnPropertyChanged(nameof(FilterAllFg));
        OnPropertyChanged(nameof(FilterRecBg));
        OnPropertyChanged(nameof(FilterRecBorder));
        OnPropertyChanged(nameof(FilterRecFg));
        OnPropertyChanged(nameof(FilterAppliedBg));
        OnPropertyChanged(nameof(FilterAppliedBorder));
        OnPropertyChanged(nameof(FilterAppliedFg));
        OnPropertyChanged(nameof(FilterNotAppliedBg));
        OnPropertyChanged(nameof(FilterNotAppliedBorder));
        OnPropertyChanged(nameof(FilterNotAppliedFg));
        ApplyFilters();
    }
    partial void OnHideIncompatibleChanged(bool value)
    {
        _settingsService.Settings.HideIncompatibleTweaks = value;
        _settingsService.Save();
        ApplyFilters();
    }

    [RelayCommand]
    public void SetFilter(string filterType)
    {
        SelectedFilterType = filterType;
    }

    [RelayCommand]
    public async Task ApplyRecommendedAsync()
    {
        var targetTweaks = FilteredTweaks
            .Where(t => t.IsCompatible && !t.IsApplied && t.Recommendation == RecommendationLevel.Recommended)
            .ToList();

        if (targetTweaks.Count == 0)
        {
            BatchStatusMessage = _loc.CurrentLanguage == "ru"
                ? "Все рекомендуемые твики уже применены!"
                : "All recommended tweaks are already applied!";
            return;
        }

        IsLoading = true;
        try
        {
            int applied = 0;
            foreach (var tweak in targetTweaks)
            {
                await tweak.ApplyAsync();
                if (tweak.IsApplied) applied++;
            }

            UpdateCounts();
            BatchStatusMessage = _loc.CurrentLanguage == "ru"
                ? $"✓ Успешно применено {applied} рекомендуемых твиков!"
                : $"✓ Successfully applied {applied} recommended tweaks!";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task RevertCategoryAsync()
    {
        var targetTweaks = FilteredTweaks.Where(t => t.IsApplied).ToList();
        if (targetTweaks.Count == 0)
        {
            BatchStatusMessage = _loc.CurrentLanguage == "ru"
                ? "В этой категории нет применённых твиков."
                : "No applied tweaks in this category.";
            return;
        }

        IsLoading = true;
        try
        {
            int rolled = 0;
            foreach (var tweak in targetTweaks)
            {
                await tweak.RollbackAsync();
                if (!tweak.IsApplied) rolled++;
            }

            UpdateCounts();
            BatchStatusMessage = _loc.CurrentLanguage == "ru"
                ? $"✓ Откачено {rolled} твиков в категории."
                : $"✓ Rolled back {rolled} tweaks in category.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task CreatePresetAsync()
    {
        var activeIds = _allTweaks.Where(t => t.IsApplied).Select(t => t.Id).ToList();
        if (activeIds.Count == 0)
        {
            BatchStatusMessage = _loc["Tweaks_NoAppliedForPreset"];
            return;
        }

        var dialog = new CreatePresetDialog(activeIds.Count);
        dialog.XamlRoot = App.MainWindowInstance?.Content?.XamlRoot;
        await dialog.ShowAsync();

        if (dialog.IsConfirmed)
        {
            var preset = await _presetManager.CreatePresetFromTweakIdsAsync(
                dialog.PresetName,
                dialog.PresetDescription,
                activeIds,
                dialog.PresetAuthor
            );

            string defaultFileName = $"{SanitizeFileName(dialog.PresetName)}.silkpreset";
            var savePath = await FilePickerHelper.PickSavePresetFileAsync(defaultFileName);
            if (!string.IsNullOrEmpty(savePath))
            {
                await _presetManager.ExportPresetAsync(preset, savePath);
                BatchStatusMessage = string.Format(_loc["Tweaks_PresetSaved"], preset.Name);
            }
        }
    }

    [RelayCommand]
    public async Task ActivatePresetAsync()
    {
        var filePath = await FilePickerHelper.PickOpenPresetFileAsync();
        if (string.IsNullOrEmpty(filePath)) return;

        IsLoading = true;
        try
        {
            var evaluated = await _presetManager.LoadAndEvaluatePresetAsync(filePath);
            var dialog = new PresetReviewDialog(evaluated, _tweakEngine);
            dialog.XamlRoot = App.MainWindowInstance?.Content?.XamlRoot;
            await dialog.ShowAsync();

            if (dialog.WasApplied && dialog.AppliedCount > 0)
            {
                await Task.WhenAll(_allTweaks.Select(t => t.RefreshStatusAsync()));
                UpdateCounts();
                ApplyFilters();

                BatchStatusMessage = string.Format(_loc["Tweaks_PresetApplied"], evaluated.Preset.Name, dialog.AppliedCount);
            }
        }
        catch (Exception ex)
        {
            BatchStatusMessage = _loc.CurrentLanguage == "ru"
                ? $"Ошибка загрузки пресета: {ex.Message}"
                : $"Failed to load preset: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        var clean = new string(name.Where(c => !invalid.Contains(c)).ToArray());
        return string.IsNullOrWhiteSpace(clean) ? "SilkPreset" : clean;
    }

    private void ApplyFilters()
    {
        if (_allTweaks.Count == 0) return;

        var query = _allTweaks.AsEnumerable();

        // 1. Category Filter
        if (SelectedCategory?.Category != null)
        {
            query = query.Where(t => t.Category == SelectedCategory.Category.Value);
        }

        // 2. Hide Incompatible
        if (HideIncompatible)
        {
            query = query.Where(t => t.IsCompatible);
        }

        // 3. Quick Filter Tabs
        query = SelectedFilterType switch
        {
            "Recommended" => query.Where(t => t.Recommendation == RecommendationLevel.Recommended),
            "Applied" => query.Where(t => t.IsApplied),
            "NotApplied" => query.Where(t => !t.IsApplied),
            "Safe" => query.Where(t => t.Risk == RiskLevel.Low),
            _ => query
        };

        // 4. Search Filter
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var q = SearchQuery.Trim().ToLowerInvariant();
            query = query.Where(t =>
                t.Title.ToLowerInvariant().Contains(q) ||
                t.Description.ToLowerInvariant().Contains(q) ||
                t.ProblemSolved.ToLowerInvariant().Contains(q) ||
                t.HowItWorks.ToLowerInvariant().Contains(q) ||
                t.Id.ToLowerInvariant().Contains(q));
        }

        FilteredTweaks.Clear();
        foreach (var item in query)
        {
            FilteredTweaks.Add(item);
        }

        FilteredCountText = string.Format(
            _loc["Tweaks_FilteredCount"],
            FilteredTweaks.Count,
            TotalCount
        );

        if (SelectedTweak == null || !FilteredTweaks.Contains(SelectedTweak))
        {
            SelectedTweak = FilteredTweaks.FirstOrDefault();
        }
    }
}
