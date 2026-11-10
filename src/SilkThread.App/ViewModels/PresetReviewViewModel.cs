using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using SilkThread.App.Services;
using SilkThread.Core.Models;
using SilkThread.Core.PresetManager;
using SilkThread.Core.TweakEngine;
using Windows.UI;

namespace SilkThread.App.ViewModels;

public partial class PresetTweakItemViewModel : ObservableObject
{
    private readonly Action? _onSelectionChanged;

    public string Id { get; }
    public string Title { get; }
    public string Description { get; }
    public TweakCategory Category { get; }
    public string CategoryName => LocalizationService.Instance.GetCategoryName(Category);
    public RiskLevel Risk { get; }
    public string LocalizedRisk => LocalizationService.Instance.GetRiskName(Risk);
    public bool RequiresReboot { get; }
    public bool IsCompatible { get; }
    public bool IsIncompatible => !IsCompatible;
    public string IncompatibilityReason { get; }
    public string? AiRationale { get; }
    public bool HasAiRationale => !string.IsNullOrWhiteSpace(AiRationale);

    public string IncompatibilityReasonHeader => LocalizationService.Instance.CurrentLanguage == "ru"
        ? "Причина несовместимости:"
        : "Incompatibility Reason:";

    public string AiRationaleHeader => LocalizationService.Instance.CurrentLanguage == "ru"
        ? "Обоснование ИИ-ассистента:"
        : "AI Assistant Rationale:";

    [ObservableProperty]
    private bool _isSelected;

    partial void OnIsSelectedChanged(bool value)
    {
        _onSelectionChanged?.Invoke();
    }

    // Dynamic UI Brushes
    private static readonly SolidColorBrush s_incompatibleBorder = new(Color.FromArgb(180, 244, 63, 94)); // Rose #F43F5E
    private static readonly SolidColorBrush s_compatibleBorder = new(Color.FromArgb(255, 30, 38, 61));    // SilkStrokeDark #1E263D
    private static readonly SolidColorBrush s_incompatibleBg = new(Color.FromArgb(255, 41, 18, 25));      // Dark Red Tint
    private static readonly SolidColorBrush s_compatibleBg = new(Color.FromArgb(255, 17, 22, 37));        // SilkCardDark #111625

    private static readonly SolidColorBrush s_roseBrush = new(Color.FromArgb(255, 239, 68, 68));
    private static readonly SolidColorBrush s_emeraldBrush = new(Color.FromArgb(255, 16, 185, 129));
    private static readonly SolidColorBrush s_roseBadgeBg = new(Color.FromArgb(40, 239, 68, 68));
    private static readonly SolidColorBrush s_emeraldBadgeBg = new(Color.FromArgb(40, 16, 185, 129));

    private static readonly SolidColorBrush s_riskLowBg = new(Color.FromArgb(38, 34, 197, 94));     // 15% #22C55E
    private static readonly SolidColorBrush s_riskLowBorder = new(Color.FromArgb(255, 34, 197, 94));
    private static readonly SolidColorBrush s_riskLowFg = new(Color.FromArgb(255, 74, 222, 128));

    private static readonly SolidColorBrush s_riskMedBg = new(Color.FromArgb(38, 245, 158, 11));    // 15% #F59E0B
    private static readonly SolidColorBrush s_riskMedBorder = new(Color.FromArgb(255, 245, 158, 11));
    private static readonly SolidColorBrush s_riskMedFg = new(Color.FromArgb(255, 251, 191, 36));

    private static readonly SolidColorBrush s_riskHighBg = new(Color.FromArgb(38, 239, 68, 68));    // 15% #EF4444
    private static readonly SolidColorBrush s_riskHighBorder = new(Color.FromArgb(255, 239, 68, 68));
    private static readonly SolidColorBrush s_riskHighFg = new(Color.FromArgb(255, 248, 113, 113));

    public SolidColorBrush RiskBackground => Risk switch
    {
        RiskLevel.Low => s_riskLowBg,
        RiskLevel.Medium => s_riskMedBg,
        RiskLevel.High or RiskLevel.Critical => s_riskHighBg,
        _ => s_riskLowBg
    };

    public SolidColorBrush RiskBorder => Risk switch
    {
        RiskLevel.Low => s_riskLowBorder,
        RiskLevel.Medium => s_riskMedBorder,
        RiskLevel.High or RiskLevel.Critical => s_riskHighBorder,
        _ => s_riskLowBorder
    };

    public SolidColorBrush RiskForeground => Risk switch
    {
        RiskLevel.Low => s_riskLowFg,
        RiskLevel.Medium => s_riskMedFg,
        RiskLevel.High or RiskLevel.Critical => s_riskHighFg,
        _ => s_riskLowFg
    };

    public Brush CardBorderBrush => IsIncompatible ? s_incompatibleBorder : s_compatibleBorder;
    public Brush CardBackgroundBrush => IsIncompatible ? s_incompatibleBg : s_compatibleBg;
    public Brush BadgeBackgroundBrush => IsIncompatible ? s_roseBadgeBg : s_emeraldBadgeBg;
    public Brush BadgeForegroundBrush => IsIncompatible ? s_roseBrush : s_emeraldBrush;
    public Brush StatusBrush => IsIncompatible ? s_roseBrush : s_emeraldBrush;

    public string StatusIcon => IsIncompatible ? "\uE7BA" : "\uE73E";
    public string CompatibilityBadgeText => IsIncompatible
        ? (LocalizationService.Instance.CurrentLanguage == "ru" ? "Не совместим" : "Incompatible")
        : (LocalizationService.Instance.CurrentLanguage == "ru" ? "Совместим" : "Compatible");

    public PresetTweakItemViewModel(PresetTweakEvaluation evaluation, Action? onSelectionChanged = null)
    {
        _onSelectionChanged = onSelectionChanged;
        Id = evaluation.TweakId;

        if (evaluation.Tweak != null)
        {
            var tweak = evaluation.Tweak;
            Title = LocalizationService.Instance.CurrentLanguage == "ru"
                ? TweakLocalization.GetTitle(tweak.Id, tweak.Title)
                : tweak.Title;
            Description = LocalizationService.Instance.CurrentLanguage == "ru"
                ? TweakLocalization.GetDescription(tweak.Id, tweak.Description)
                : tweak.Description;
            Category = tweak.Category;
            Risk = tweak.Risk;
            RequiresReboot = tweak.RequiresReboot;
        }
        else
        {
            Title = evaluation.TweakId;
            Description = LocalizationService.Instance.CurrentLanguage == "ru"
                ? "Пользовательский или неизвестный твик."
                : "Custom or unrecognized tweak.";
            Category = TweakCategory.AdvancedTweaks;
            Risk = RiskLevel.Medium;
            RequiresReboot = false;
        }

        IsCompatible = evaluation.IsCompatible;
        IncompatibilityReason = evaluation.IncompatibilityReason;
        AiRationale = SanitizeAiText(evaluation.AiRationale);
        _isSelected = evaluation.IsSelected;
    }

    private static string? SanitizeAiText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;

        var cleaned = text.Trim();
        // Strip common model prefixes so text reads seamlessly under the header
        if (cleaned.StartsWith("LFM-3:", StringComparison.OrdinalIgnoreCase))
            cleaned = cleaned.Substring(6).Trim();
        else if (cleaned.StartsWith("Liquid LFM-3:", StringComparison.OrdinalIgnoreCase))
            cleaned = cleaned.Substring(13).Trim();
        else if (cleaned.StartsWith("Liquid LFM-3 1.7B:", StringComparison.OrdinalIgnoreCase))
            cleaned = cleaned.Substring(18).Trim();
        else if (cleaned.StartsWith("LFM-3 1.7B:", StringComparison.OrdinalIgnoreCase))
            cleaned = cleaned.Substring(11).Trim();
        else if (cleaned.StartsWith("ИИ:", StringComparison.OrdinalIgnoreCase))
            cleaned = cleaned.Substring(3).Trim();
        else if (cleaned.StartsWith("AI:", StringComparison.OrdinalIgnoreCase))
            cleaned = cleaned.Substring(3).Trim();

        return cleaned
            .Replace("Liquid LFM-3 1.7B", "IOST")
            .Replace("Liquid LFM-3", "IOST")
            .Replace("LFM-3 1.7B", "IOST")
            .Replace("LFM-3", "IOST")
            .Replace("ИИ-Оптимизатор", "IOST")
            .Replace("Liquid AI", "IOST");
    }
}

public partial class PresetReviewViewModel : ObservableObject
{
    private readonly EvaluatedPreset _evaluated;
    private readonly ITweakEngine _tweakEngine;

    public string PresetName => SanitizeText(_evaluated.Preset.Name);

    public string PresetDescription => string.IsNullOrWhiteSpace(_evaluated.Preset.Description)
        ? (LocalizationService.Instance.CurrentLanguage == "ru" ? "Без описания" : "No description")
        : SanitizeText(_evaluated.Preset.Description);

    public string PresetAuthor
    {
        get
        {
            var author = string.IsNullOrWhiteSpace(_evaluated.Preset.Author) ? "Unknown" : _evaluated.Preset.Author;
            return SanitizeText(author);
        }
    }

    public string CreatedAtText => _evaluated.Preset.FormattedDate;

    public int TotalCount => _evaluated.TotalCount;
    public int IncompatibleCount => _evaluated.IncompatibleCount;
    public int CompatibleCount => _evaluated.CompatibleCount;
    public bool HasIncompatible => _evaluated.HasIncompatible;

    private static SolidColorBrush? s_onlineBadgeBorder;
    private static SolidColorBrush? s_onlineBadgeBg;
    private static SolidColorBrush? s_onlineBadgeFg;

    private static SolidColorBrush? s_offlineBadgeBorder;
    private static SolidColorBrush? s_offlineBadgeBg;
    private static SolidColorBrush? s_offlineBadgeFg;

    private static SolidColorBrush OnlineBadgeBorder => s_onlineBadgeBorder ??= new(Color.FromArgb(255, 128, 107, 255));
    private static SolidColorBrush OnlineBadgeBg => s_onlineBadgeBg ??= new(Color.FromArgb(255, 32, 36, 49));
    private static SolidColorBrush OnlineBadgeFg => s_onlineBadgeFg ??= new(Color.FromArgb(255, 128, 107, 255));

    private static SolidColorBrush OfflineBadgeBorder => s_offlineBadgeBorder ??= new(Color.FromArgb(160, 150, 156, 171));
    private static SolidColorBrush OfflineBadgeBg => s_offlineBadgeBg ??= new(Color.FromArgb(255, 32, 36, 49));
    private static SolidColorBrush OfflineBadgeFg => s_offlineBadgeFg ??= new(Color.FromArgb(255, 150, 156, 171));

    public string GenerationSource => _evaluated.Preset?.GenerationSource 
        ?? (_evaluated.Preset?.Author == "SilkThread IOST" ? "Локальный эксперт IOST" : "");

    public bool IsOnlineAiGenerated => (_evaluated.Preset?.IsOnlineAiGenerated ?? false) || (_evaluated.Preset?.UsedOnlineModel ?? false);
    public string? AiModelUsed => _evaluated.Preset?.AiModelUsed;

    public bool UsedOnlineModel => IsOnlineAiGenerated || 
        (!string.IsNullOrEmpty(_evaluated.Preset?.GenerationSource) && _evaluated.Preset.GenerationSource.Contains("OpenRouter", StringComparison.OrdinalIgnoreCase));

    public bool HasSourceBadge => !string.IsNullOrEmpty(GenerationSource) || 
                                  _evaluated.Preset?.Author == "SilkThread IOST" || 
                                  _evaluated.Preset?.Name?.StartsWith("AI:") == true;

    public string SourceBadgeText
    {
        get
        {
            if (UsedOnlineModel)
            {
                var src = !string.IsNullOrWhiteSpace(_evaluated.Preset?.GenerationSource)
                    ? _evaluated.Preset.GenerationSource
                    : (!string.IsNullOrWhiteSpace(_evaluated.Preset?.Author) && _evaluated.Preset.Author.StartsWith("AI (") 
                        ? _evaluated.Preset.Author 
                        : "OpenRouter");

                if (src.StartsWith("Нейросеть OpenRouter (", StringComparison.OrdinalIgnoreCase) && src.EndsWith(")"))
                {
                    var inner = src.Substring("Нейросеть OpenRouter (".Length);
                    inner = inner.Substring(0, inner.Length - 1);
                    return $"Нейросеть: {inner}";
                }
                if (src.StartsWith("AI (") && src.EndsWith(")"))
                {
                    var inner = src.Substring(4, src.Length - 5);
                    return $"Нейросеть: {inner}";
                }
                if (src.StartsWith("Нейросеть:", StringComparison.OrdinalIgnoreCase))
                    return src;

                return $"Нейросеть: {src}";
            }

            return "Локальный эксперт IOST";
        }
    }

    public SolidColorBrush SourceBadgeBorderBrush => UsedOnlineModel ? OnlineBadgeBorder : OfflineBadgeBorder;
    public SolidColorBrush SourceBadgeBackgroundBrush => UsedOnlineModel ? OnlineBadgeBg : OfflineBadgeBg;
    public SolidColorBrush SourceBadgeForegroundBrush => UsedOnlineModel ? OnlineBadgeFg : OfflineBadgeFg;
    public string SourceBadgeBorderHex => UsedOnlineModel ? "#806BFF" : "#969CAB";
    public string SourceBadgeBgHex => "#202431";
    public string SourceBadgeFgHex => UsedOnlineModel ? "#806BFF" : "#969CAB";
    public string SourceBadgeIcon => UsedOnlineModel ? "\uE945" : "\uE9D9";

    public ObservableCollection<PresetTweakItemViewModel> Tweaks { get; } = new();
    public ObservableCollection<PresetCategoryGroupViewModel> CategoryGroups { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanApply))]
    private int _selectedCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanApply))]
    private bool _isApplying;

    public bool CanApply => !IsApplying && SelectedCount > 0;

    [ObservableProperty]
    private string _progressText = "";

    [ObservableProperty]
    private double _progressPercent;

    [ObservableProperty]
    private bool _isCompleted;

    [ObservableProperty]
    private int _appliedCount;

    // Localized helper properties for UI
    public string TotalLabel => LocalizationService.Instance.CurrentLanguage == "ru" ? "Всего твиков:" : "Total tweaks:";
    public string CompatibleBadgeSummary => LocalizationService.Instance.CurrentLanguage == "ru" ? "совместимо" : "compatible";
    public string IncompatibleBadgeSummary => LocalizationService.Instance.CurrentLanguage == "ru" ? "не совместимо" : "incompatible";
    public string SelectedLabel => LocalizationService.Instance.CurrentLanguage == "ru" ? "Выбрано для применения:" : "Selected to apply:";
    public string QuickCompatibleOnlyText => LocalizationService.Instance.CurrentLanguage == "ru" ? "Только совместимые" : "Compatible only";
    public string QuickSelectAllText => LocalizationService.Instance.CurrentLanguage == "ru" ? "Выбрать все" : "Select all";
    public string QuickDeselectAllText => LocalizationService.Instance.CurrentLanguage == "ru" ? "Снять все" : "Deselect all";
    public string RejectBtnText => LocalizationService.Instance.CurrentLanguage == "ru" ? "Отклонить" : "Reject";
    public string ApplyBtnText => LocalizationService.Instance.CurrentLanguage == "ru" ? "Применить" : "Apply";
    public string WarningBannerBadge => LocalizationService.Instance.CurrentLanguage == "ru" ? "ВНИМАНИЕ" : "ATTENTION";
    public string WarningBannerTitle => LocalizationService.Instance.CurrentLanguage == "ru"
        ? "Некоторые параметры не совместимы с вашей системой"
        : "Some tweaks are incompatible with your system";
    public string WarningBannerDesc => LocalizationService.Instance.CurrentLanguage == "ru"
        ? "Часть твиков в пресете не поддерживается конфигурацией оборудования (GPU, CPU или типом накопителя). Они помещены в начало списка и по умолчанию отключены. Вы можете включить их принудительно на свой страх и риск."
        : "Some tweaks in this preset are unsupported by your PC hardware (GPU, CPU or storage type). They are placed at the top and disabled by default. You may manually enable them at your own risk.";

    public PresetReviewViewModel(EvaluatedPreset evaluated, ITweakEngine tweakEngine)
    {
        _evaluated = evaluated;
        _tweakEngine = tweakEngine;

        foreach (var item in _evaluated.Tweaks)
        {
            Tweaks.Add(new PresetTweakItemViewModel(item, UpdateSelectedCount));
        }

        // Group tweaks by Category for structured review
        var grouped = Tweaks
            .GroupBy(t => t.Category)
            .OrderBy(g => g.Any(t => t.IsIncompatible) ? 0 : 1)
            .ThenBy(g => g.Key.ToString());

        foreach (var g in grouped)
        {
            string catName = LocalizationService.Instance.GetCategoryName(g.Key);
            string catIcon = GetCategoryGlyph(g.Key);
            var groupVm = new PresetCategoryGroupViewModel(g.Key, catName, catIcon, g.OrderBy(t => t.IsCompatible ? 1 : 0).ThenBy(t => t.Title));
            CategoryGroups.Add(groupVm);
        }

        UpdateSelectedCount();
    }

    public static string GetCategoryGlyph(TweakCategory cat) => cat switch
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

    private void UpdateSelectedCount()
    {
        SelectedCount = Tweaks.Count(t => t.IsSelected);
    }

    [RelayCommand]
    public void SelectAll()
    {
        foreach (var t in Tweaks) t.IsSelected = true;
        UpdateSelectedCount();
    }

    [RelayCommand]
    public void SelectCompatibleOnly()
    {
        foreach (var t in Tweaks) t.IsSelected = t.IsCompatible;
        UpdateSelectedCount();
    }

    [RelayCommand]
    public void DeselectAll()
    {
        foreach (var t in Tweaks) t.IsSelected = false;
        UpdateSelectedCount();
    }

    public async Task<int> ApplySelectedTweaksAsync()
    {
        var targetTweaks = Tweaks.Where(t => t.IsSelected).ToList();
        if (targetTweaks.Count == 0) return 0;

        IsApplying = true;
        ProgressPercent = 0;
        int successful = 0;

        try
        {
            for (int i = 0; i < targetTweaks.Count; i++)
            {
                var tweak = targetTweaks[i];
                ProgressText = LocalizationService.Instance.CurrentLanguage == "ru"
                    ? $"Применение ({i + 1}/{targetTweaks.Count}): {tweak.Title}..."
                    : $"Applying ({i + 1}/{targetTweaks.Count}): {tweak.Title}...";
                ProgressPercent = (double)(i) / targetTweaks.Count * 100.0;

                var result = await _tweakEngine.ApplyTweakAsync(tweak.Id);
                if (result.Success)
                {
                    successful++;
                }

                ProgressPercent = (double)(i + 1) / targetTweaks.Count * 100.0;
            }

            AppliedCount = successful;
            IsCompleted = true;
            ProgressText = LocalizationService.Instance.CurrentLanguage == "ru"
                ? $"✓ Успешно применено {successful} из {targetTweaks.Count} выбранных твиков!"
                : $"✓ Successfully applied {successful} of {targetTweaks.Count} selected tweaks!";

            await Task.Delay(600);
            return successful;
        }
        finally
        {
            IsApplying = false;
        }
    }

    private static string SanitizeText(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;
        return input
            .Replace("Liquid LFM-3 1.7B (SilkThread AI)", "IOST (SilkThread Copilot)")
            .Replace("Liquid LFM-3 1.7B", "IOST")
            .Replace("Liquid LFM-3", "IOST")
            .Replace("LFM-3 AI:", "IOST:")
            .Replace("LFM-3 AI", "IOST")
            .Replace("LFM-3 1.7B", "IOST")
            .Replace("LFM-3", "IOST")
            .Replace("ИИ-Оптимизатор", "IOST")
            .Replace("Liquid AI", "IOST");
    }
}

public partial class PresetCategoryGroupViewModel : ObservableObject
{
    public TweakCategory Category { get; }
    public string CategoryName { get; }
    public string CategoryIcon { get; }
    public string CountBadge => $"({Items.Count})";
    public ObservableCollection<PresetTweakItemViewModel> Items { get; } = new();

    public PresetCategoryGroupViewModel(TweakCategory category, string categoryName, string categoryIcon, IEnumerable<PresetTweakItemViewModel> items)
    {
        Category = category;
        CategoryName = categoryName;
        CategoryIcon = categoryIcon;
        foreach (var item in items)
        {
            Items.Add(item);
        }
    }
}
