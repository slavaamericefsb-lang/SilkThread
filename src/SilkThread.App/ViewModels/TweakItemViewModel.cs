using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SilkThread.App.Services;
using SilkThread.Core.Models;
using SilkThread.Core.TweakEngine;

namespace SilkThread.App.ViewModels;

public partial class TweakItemViewModel : ObservableObject
{
    private readonly ITweakEngine _tweakEngine;

    public TweakDefinition Definition { get; }

    public string Id => Definition.Id;

    public string Title => LocalizationService.Instance.CurrentLanguage == "ru"
        ? TweakLocalization.GetTitle(Definition.Id, Definition.Title)
        : Definition.Title;

    public string Description => LocalizationService.Instance.CurrentLanguage == "ru"
        ? TweakLocalization.GetDescription(Definition.Id, Definition.Description)
        : Definition.Description;

    public string HowItWorks => LocalizationService.Instance.CurrentLanguage == "ru"
        ? TweakLocalization.GetHowItWorks(Definition.Id, Definition.HowItWorks)
        : Definition.HowItWorks;

    public string ProblemSolved => LocalizationService.Instance.CurrentLanguage == "ru"
        ? TweakLocalization.GetProblemSolved(Definition.Id, Definition.ProblemSolved)
        : Definition.ProblemSolved;

    public string ExpectedResult => LocalizationService.Instance.CurrentLanguage == "ru"
        ? TweakLocalization.GetExpectedResult(Definition.Id, Definition.ExpectedResult)
        : Definition.ExpectedResult;

    public TweakCategory Category => Definition.Category;
    public string CategoryName => LocalizationService.Instance.GetCategoryName(Category);

    public RiskLevel Risk => Definition.Risk;
    public string LocalizedRisk => LocalizationService.Instance.GetRiskName(Risk);

    public RecommendationLevel Recommendation => Definition.Recommendation;
    public string LocalizedRecommendation => LocalizationService.Instance.GetRecommendationName(Recommendation);

    public bool RequiresReboot => Definition.RequiresReboot;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsApplied))]
    [NotifyPropertyChangedFor(nameof(IsNotApplied))]
    [NotifyPropertyChangedFor(nameof(LocalizedStatus))]
    [NotifyPropertyChangedFor(nameof(StatusBadgeBackground))]
    [NotifyPropertyChangedFor(nameof(StatusBadgeBorder))]
    [NotifyPropertyChangedFor(nameof(StatusBadgeForeground))]
    [NotifyPropertyChangedFor(nameof(StatusBadgeDot))]
    private TweakStatus _status = TweakStatus.Unknown;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCompatible))]
    [NotifyPropertyChangedFor(nameof(RequiresVerification))]
    [NotifyPropertyChangedFor(nameof(CompatibilityReason))]
    [NotifyPropertyChangedFor(nameof(CanApply))]
    private CompatibilityResult _compatibility = CompatibilityResult.Compatible;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CardBackgroundBrush))]
    [NotifyPropertyChangedFor(nameof(CardBorderBrush))]
    [NotifyPropertyChangedFor(nameof(SelectedIndicatorVisibility))]
    private bool _isSelected;

    // Minimal Dark UI design tokens
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_selectedBg = new(Windows.UI.Color.FromArgb(255, 32, 36, 49)); // #202431
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_defaultBg = new(Windows.UI.Color.FromArgb(255, 24, 27, 37)); // #181B25
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_selectedBorder = new(Windows.UI.Color.FromArgb(255, 128, 107, 255)); // #806BFF
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_defaultBorder = new(Windows.UI.Color.FromArgb(255, 43, 48, 64)); // #2B3040

    public Microsoft.UI.Xaml.Media.Brush CardBackgroundBrush => IsSelected ? s_selectedBg : s_defaultBg;
    public Microsoft.UI.Xaml.Media.Brush CardBorderBrush => IsSelected ? s_selectedBorder : s_defaultBorder;
    public Microsoft.UI.Xaml.Visibility SelectedIndicatorVisibility => IsSelected ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

    // Risk Badges (Success: #22C55E, Warning: #F59E0B, Error: #EF4444)
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_riskLowBg = new(Windows.UI.Color.FromArgb(38, 34, 197, 94)); // 15% #22C55E
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_riskLowBorder = new(Windows.UI.Color.FromArgb(255, 34, 197, 94));
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_riskLowFg = new(Windows.UI.Color.FromArgb(255, 34, 197, 94));

    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_riskMedBg = new(Windows.UI.Color.FromArgb(38, 245, 158, 11)); // 15% #F59E0B
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_riskMedBorder = new(Windows.UI.Color.FromArgb(255, 245, 158, 11));
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_riskMedFg = new(Windows.UI.Color.FromArgb(255, 245, 158, 11));

    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_riskHighBg = new(Windows.UI.Color.FromArgb(38, 239, 68, 68)); // 15% #EF4444
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_riskHighBorder = new(Windows.UI.Color.FromArgb(255, 239, 68, 68));
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_riskHighFg = new(Windows.UI.Color.FromArgb(255, 239, 68, 68));

    public Microsoft.UI.Xaml.Media.SolidColorBrush RiskBackground => Risk switch
    {
        RiskLevel.Low => s_riskLowBg,
        RiskLevel.Medium => s_riskMedBg,
        RiskLevel.High or RiskLevel.Critical => s_riskHighBg,
        _ => s_riskLowBg
    };

    public Microsoft.UI.Xaml.Media.SolidColorBrush RiskBorder => Risk switch
    {
        RiskLevel.Low => s_riskLowBorder,
        RiskLevel.Medium => s_riskMedBorder,
        RiskLevel.High or RiskLevel.Critical => s_riskHighBorder,
        _ => s_riskLowBorder
    };

    public Microsoft.UI.Xaml.Media.SolidColorBrush RiskForeground => Risk switch
    {
        RiskLevel.Low => s_riskLowFg,
        RiskLevel.Medium => s_riskMedFg,
        RiskLevel.High or RiskLevel.Critical => s_riskHighFg,
        _ => s_riskLowFg
    };

    // Recommendation Badges (Accent: #806BFF, Muted: #969CAB, Warning: #F59E0B, Error: #EF4444)
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_recRecBg = new(Windows.UI.Color.FromArgb(38, 128, 107, 255)); // 15% #806BFF
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_recRecBorder = new(Windows.UI.Color.FromArgb(255, 128, 107, 255));
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_recRecFg = new(Windows.UI.Color.FromArgb(255, 196, 181, 253));

    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_recOptBg = new(Windows.UI.Color.FromArgb(255, 32, 36, 49)); // #202431
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_recOptBorder = new(Windows.UI.Color.FromArgb(255, 43, 48, 64)); // #2B3040
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_recOptFg = new(Windows.UI.Color.FromArgb(255, 150, 156, 171)); // #969CAB

    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_recExpBg = new(Windows.UI.Color.FromArgb(38, 245, 158, 11)); // 15% #F59E0B
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_recExpBorder = new(Windows.UI.Color.FromArgb(255, 245, 158, 11));
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_recExpFg = new(Windows.UI.Color.FromArgb(255, 245, 158, 11));

    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_recNotRecBg = new(Windows.UI.Color.FromArgb(38, 239, 68, 68)); // 15% #EF4444
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_recNotRecBorder = new(Windows.UI.Color.FromArgb(255, 239, 68, 68));
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_recNotRecFg = new(Windows.UI.Color.FromArgb(255, 239, 68, 68));

    public Microsoft.UI.Xaml.Media.SolidColorBrush RecommendationBackground => Recommendation switch
    {
        RecommendationLevel.Recommended => s_recRecBg,
        RecommendationLevel.Optional => s_recOptBg,
        RecommendationLevel.Experimental => s_recExpBg,
        RecommendationLevel.NotRecommended => s_recNotRecBg,
        _ => s_recOptBg
    };

    public Microsoft.UI.Xaml.Media.SolidColorBrush RecommendationBorder => Recommendation switch
    {
        RecommendationLevel.Recommended => s_recRecBorder,
        RecommendationLevel.Optional => s_recOptBorder,
        RecommendationLevel.Experimental => s_recExpBorder,
        RecommendationLevel.NotRecommended => s_recNotRecBorder,
        _ => s_recOptBorder
    };

    public Microsoft.UI.Xaml.Media.SolidColorBrush RecommendationForeground => Recommendation switch
    {
        RecommendationLevel.Recommended => s_recRecFg,
        RecommendationLevel.Optional => s_recOptFg,
        RecommendationLevel.Experimental => s_recExpFg,
        RecommendationLevel.NotRecommended => s_recNotRecFg,
        _ => s_recOptFg
    };

    // Status Badges (Applied: #22C55E, NotApplied: #969CAB, Failed: #EF4444)
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_statAppliedBg = new(Windows.UI.Color.FromArgb(38, 34, 197, 94)); // #22C55E
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_statAppliedBorder = new(Windows.UI.Color.FromArgb(255, 34, 197, 94));
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_statAppliedFg = new(Windows.UI.Color.FromArgb(255, 34, 197, 94));

    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_statNotAppliedBg = new(Windows.UI.Color.FromArgb(255, 32, 36, 49)); // #202431
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_statNotAppliedBorder = new(Windows.UI.Color.FromArgb(255, 43, 48, 64)); // #2B3040
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_statNotAppliedFg = new(Windows.UI.Color.FromArgb(255, 150, 156, 171)); // #969CAB
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_statNotAppliedDot = new(Windows.UI.Color.FromArgb(255, 100, 116, 139));

    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_statFailedBg = new(Windows.UI.Color.FromArgb(38, 239, 68, 68)); // #EF4444
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_statFailedBorder = new(Windows.UI.Color.FromArgb(255, 239, 68, 68));
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_statFailedFg = new(Windows.UI.Color.FromArgb(255, 239, 68, 68));

    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_statRecBg = new(Windows.UI.Color.FromArgb(38, 128, 107, 255)); // #806BFF
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_statRecBorder = new(Windows.UI.Color.FromArgb(255, 128, 107, 255));
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush s_statRecFg = new(Windows.UI.Color.FromArgb(255, 196, 181, 253));

    public Microsoft.UI.Xaml.Media.SolidColorBrush StatusBadgeBackground => Status switch
    {
        TweakStatus.Applied => s_statAppliedBg,
        TweakStatus.NotApplied => s_statNotAppliedBg,
        TweakStatus.Failed => s_statFailedBg,
        TweakStatus.RequiresVerification => s_statRecBg,
        _ => s_statNotAppliedBg
    };

    public Microsoft.UI.Xaml.Media.SolidColorBrush StatusBadgeBorder => Status switch
    {
        TweakStatus.Applied => s_statAppliedBorder,
        TweakStatus.NotApplied => s_statNotAppliedBorder,
        TweakStatus.Failed => s_statFailedBorder,
        TweakStatus.RequiresVerification => s_statRecBorder,
        _ => s_statNotAppliedBorder
    };

    public Microsoft.UI.Xaml.Media.SolidColorBrush StatusBadgeForeground => Status switch
    {
        TweakStatus.Applied => s_statAppliedFg,
        TweakStatus.NotApplied => s_statNotAppliedFg,
        TweakStatus.Failed => s_statFailedFg,
        TweakStatus.RequiresVerification => s_statRecFg,
        _ => s_statNotAppliedFg
    };

    public Microsoft.UI.Xaml.Media.SolidColorBrush StatusBadgeDot => Status switch
    {
        TweakStatus.Applied => s_statAppliedFg,
        TweakStatus.NotApplied => s_statNotAppliedDot,
        TweakStatus.Failed => s_statFailedFg,
        TweakStatus.RequiresVerification => s_statRecFg,
        _ => s_statNotAppliedDot
    };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanApply))]
    [NotifyPropertyChangedFor(nameof(CanRollback))]
    private bool _isBusy;

    [ObservableProperty]
    private string? _lastOperationMessage;

    public bool IsApplied => Status == TweakStatus.Applied;
    public bool IsNotApplied => Status != TweakStatus.Applied;
    public bool IsCompatible => Compatibility.IsCompatible;
    public bool CanApply => IsCompatible && !IsBusy;
    public bool CanRollback => !IsBusy;
    public bool RequiresVerification => Compatibility.RequiresVerification;
    public string CompatibilityReason => Compatibility?.Reason ?? string.Empty;
    public string LocalizedStatus => LocalizationService.Instance.GetStatusName(Status);

    public TweakItemViewModel(TweakDefinition definition, ITweakEngine tweakEngine)
    {
        Definition = definition;
        _tweakEngine = tweakEngine;
    }

    public void RefreshLocalization()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(HowItWorks));
        OnPropertyChanged(nameof(ProblemSolved));
        OnPropertyChanged(nameof(ExpectedResult));
        OnPropertyChanged(nameof(CategoryName));
        OnPropertyChanged(nameof(LocalizedRisk));
        OnPropertyChanged(nameof(LocalizedRecommendation));
        OnPropertyChanged(nameof(LocalizedStatus));
    }

    public async Task RefreshStatusAsync()
    {
        IsBusy = true;
        try
        {
            Status = await _tweakEngine.CheckTweakStatusAsync(Id);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task ToggleAsync()
    {
        if (IsApplied)
        {
            await RollbackAsync();
        }
        else
        {
            await ApplyAsync();
        }
    }

    [RelayCommand]
    public async Task ApplyAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var res = await _tweakEngine.ApplyTweakAsync(Id);
            LastOperationMessage = res.Message;
            if (res.Success)
            {
                Status = TweakStatus.Applied;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task RollbackAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var res = await _tweakEngine.RollbackTweakAsync(Id);
            LastOperationMessage = res.Message;
            if (res.Success)
            {
                Status = TweakStatus.NotApplied;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
