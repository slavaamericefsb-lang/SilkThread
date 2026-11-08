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

    public ObservableCollection<PresetTweakItemViewModel> Tweaks { get; } = new();

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

        UpdateSelectedCount();
    }

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
