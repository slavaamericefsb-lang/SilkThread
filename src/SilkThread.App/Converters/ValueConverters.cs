using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using SilkThread.App.Services;
using SilkThread.Core.Models;
using Windows.UI;

namespace SilkThread.App.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool b = value is bool flag && flag;
        if (Invert) b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Visibility v)
        {
            bool b = v == Visibility.Visible;
            return Invert ? !b : b;
        }
        return false;
    }
}

public class RiskToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is RiskLevel risk)
        {
            return risk switch
            {
                RiskLevel.Low => new SolidColorBrush(Color.FromArgb(255, 34, 197, 94)),       // Success #22C55E
                RiskLevel.Medium => new SolidColorBrush(Color.FromArgb(255, 245, 158, 11)),   // Warning #F59E0B
                RiskLevel.High => new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)),     // Error #EF4444
                RiskLevel.Critical => new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)), // Error #EF4444
                _ => new SolidColorBrush(Colors.Gray)
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class RecommendationToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is RecommendationLevel rec)
        {
            return rec switch
            {
                RecommendationLevel.Recommended => new SolidColorBrush(Color.FromArgb(255, 128, 107, 255)),  // Accent #806BFF
                RecommendationLevel.Optional => new SolidColorBrush(Color.FromArgb(255, 150, 156, 171)),     // Secondary #969CAB
                RecommendationLevel.Experimental => new SolidColorBrush(Color.FromArgb(255, 245, 158, 11)), // Warning #F59E0B
                RecommendationLevel.NotRecommended => new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)),// Error #EF4444
                _ => new SolidColorBrush(Colors.Gray)
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is TweakStatus status)
        {
            return status switch
            {
                TweakStatus.Applied => new SolidColorBrush(Color.FromArgb(255, 34, 197, 94)),        // Success #22C55E
                TweakStatus.NotApplied => new SolidColorBrush(Color.FromArgb(255, 150, 156, 171)),   // Secondary #969CAB
                TweakStatus.Failed => new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)),         // Error #EF4444
                TweakStatus.RequiresVerification => new SolidColorBrush(Color.FromArgb(255, 128, 107, 255)), // Accent #806BFF
                TweakStatus.Incompatible => new SolidColorBrush(Color.FromArgb(255, 245, 158, 11)),  // Warning #F59E0B
                _ => new SolidColorBrush(Color.FromArgb(255, 150, 156, 171))
            };
        }
        if (value is string s)
        {
            return s switch
            {
                "Applied" => new SolidColorBrush(Color.FromArgb(255, 34, 197, 94)),
                "RolledBack" => new SolidColorBrush(Color.FromArgb(255, 150, 156, 171)),
                "Failed" => new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)),
                _ => new SolidColorBrush(Color.FromArgb(255, 150, 156, 171))
            };
        }
        return new SolidColorBrush(Color.FromArgb(255, 150, 156, 171));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class StatusToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is TweakStatus status)
        {
            return status switch
            {
                TweakStatus.Applied => new SolidColorBrush(Color.FromArgb(30, 34, 197, 94)),       // #1E22C55E
                TweakStatus.NotApplied => new SolidColorBrush(Color.FromArgb(255, 32, 36, 49)),   // #202431
                TweakStatus.Failed => new SolidColorBrush(Color.FromArgb(30, 239, 68, 68)),        // #1EEF4444
                TweakStatus.RequiresVerification => new SolidColorBrush(Color.FromArgb(30, 128, 107, 255)),
                TweakStatus.Incompatible => new SolidColorBrush(Color.FromArgb(30, 245, 158, 11)),
                _ => new SolidColorBrush(Color.FromArgb(255, 32, 36, 49))
            };
        }
        if (value is string s)
        {
            return s switch
            {
                "Applied" => new SolidColorBrush(Color.FromArgb(30, 34, 197, 94)),
                "RolledBack" => new SolidColorBrush(Color.FromArgb(255, 32, 36, 49)),
                "Failed" => new SolidColorBrush(Color.FromArgb(30, 239, 68, 68)),
                _ => new SolidColorBrush(Color.FromArgb(255, 32, 36, 49))
            };
        }
        return new SolidColorBrush(Color.FromArgb(255, 32, 36, 49));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class StatusToBorderConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is TweakStatus status)
        {
            return status switch
            {
                TweakStatus.Applied => new SolidColorBrush(Color.FromArgb(60, 34, 197, 94)),
                TweakStatus.NotApplied => new SolidColorBrush(Color.FromArgb(255, 43, 48, 64)),   // #2B3040
                TweakStatus.Failed => new SolidColorBrush(Color.FromArgb(60, 239, 68, 68)),
                TweakStatus.RequiresVerification => new SolidColorBrush(Color.FromArgb(60, 128, 107, 255)),
                TweakStatus.Incompatible => new SolidColorBrush(Color.FromArgb(60, 245, 158, 11)),
                _ => new SolidColorBrush(Color.FromArgb(255, 43, 48, 64))
            };
        }
        if (value is string s)
        {
            return s switch
            {
                "Applied" => new SolidColorBrush(Color.FromArgb(60, 34, 197, 94)),
                "RolledBack" => new SolidColorBrush(Color.FromArgb(255, 43, 48, 64)),
                "Failed" => new SolidColorBrush(Color.FromArgb(60, 239, 68, 68)),
                _ => new SolidColorBrush(Color.FromArgb(255, 43, 48, 64))
            };
        }
        return new SolidColorBrush(Color.FromArgb(255, 43, 48, 64));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class StatusToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is TweakStatus status)
        {
            return status switch
            {
                TweakStatus.Applied => "\uE73E",
                TweakStatus.NotApplied => "\uE7A7",
                TweakStatus.Failed => "\uE783",
                TweakStatus.RequiresVerification => "\uE945",
                TweakStatus.Incompatible => "\uE7BA",
                _ => "\uE73E"
            };
        }
        if (value is string s)
        {
            return s switch
            {
                "Applied" => "\uE73E",
                "RolledBack" => "\uE7A7",
                "Failed" => "\uE783",
                _ => "\uE73E"
            };
        }
        return "\uE73E";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class LocalizedCategoryConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is TweakCategory cat || (value is string s && Enum.TryParse<TweakCategory>(s, out cat)))
        {
            return LocalizationService.Instance.GetCategoryName(cat);
        }
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class CategoryToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is TweakCategory cat || (value is string s && Enum.TryParse<TweakCategory>(s, out cat)))
        {
            return cat switch
            {
                TweakCategory.CpuOptimization => "\uE9D9",        // Processor
                TweakCategory.GpuOptimization => "\uE790",        // GPU / Display
                TweakCategory.RamOptimization => "\uE7C5",        // RAM / Memory chip
                TweakCategory.GamingPerformance => "\uE7FC",      // Game controller
                TweakCategory.WindowsPerformance => "\uE945",     // Speedometer
                TweakCategory.InputAndLatency => "\uE964",        // Mouse / Input
                TweakCategory.NetworkOptimization => "\uE839",    // World / Network
                TweakCategory.StorageOptimization => "\uEDA2",    // Hard disk drive
                TweakCategory.BackgroundProcesses => "\uE9F5",    // Running tasks
                TweakCategory.WindowsServices => "\uE895",        // Services / Gear
                TweakCategory.PowerManagement => "\uE7E8",        // Energy / Lightning
                TweakCategory.WindowsExplorerAndUi => "\uE8B7",   // Folder / File Explorer
                TweakCategory.SecurityAndPrivacy => "\uE72E",     // Shield / Lock
                TweakCategory.SystemMaintenance => "\uE90F",      // Wrench / Maintenance
                TweakCategory.AdvancedTweaks => "\uE7BE",         // Code / Terminal
                _ => "\uE713"                                     // Settings
            };
        }
        return "\uE713";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class LocalizedRiskConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is RiskLevel risk)
        {
            return LocalizationService.Instance.GetRiskName(risk);
        }
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class LocalizedRecommendationConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is RecommendationLevel rec)
        {
            return LocalizationService.Instance.GetRecommendationName(rec);
        }
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class StringNotEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return !string.IsNullOrWhiteSpace(value as string) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is bool b && !b;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is bool b && !b;
    }
}

public class StringNotEmptyToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return !string.IsNullOrWhiteSpace(value as string);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class LocalizedStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is TweakStatus status)
        {
            return LocalizationService.Instance.GetStatusName(status);
        }
        if (value is string s)
        {
            bool isRu = LocalizationService.Instance.CurrentLanguage == "ru";
            return s switch
            {
                "Applied" => isRu ? "Применен" : "Applied",
                "RolledBack" => isRu ? "Откачен" : "Rolled back",
                "Failed" => isRu ? "Ошибка" : "Failed",
                _ => s
            };
        }
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class ActionTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (parameter is string key)
        {
            return LocalizationService.Instance[key];
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class BoolToFilterBgConverter : IValueConverter
{
    private static readonly SolidColorBrush ActiveBrush = new(Color.FromArgb(255, 128, 107, 255)); // #806BFF
    private static readonly SolidColorBrush InactiveBrush = new(Colors.Transparent);

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (value is bool b && b) ? ActiveBrush : InactiveBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class BoolToFilterFgConverter : IValueConverter
{
    private static readonly SolidColorBrush ActiveBrush = new(Color.FromArgb(255, 255, 255, 255));
    private static readonly SolidColorBrush InactiveBrush = new(Color.FromArgb(255, 150, 156, 171)); // #969CAB

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (value is bool b && b) ? ActiveBrush : InactiveBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class BoolToCardBgConverter : IValueConverter
{
    private static readonly SolidColorBrush SelectedBrush = new(Color.FromArgb(255, 29, 28, 43)); // #1D1C2B
    private static readonly SolidColorBrush NormalBrush = new(Color.FromArgb(255, 24, 27, 37));    // #181B25

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (value is bool b && b) ? SelectedBrush : NormalBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class BoolToCardBorderConverter : IValueConverter
{
    private static readonly SolidColorBrush SelectedBrush = new(Color.FromArgb(255, 128, 107, 255)); // #806BFF
    private static readonly SolidColorBrush NormalBrush = new(Color.FromArgb(255, 43, 48, 64));      // #2B3040

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (value is bool b && b) ? SelectedBrush : NormalBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class BoolToStatusBgConverter : IValueConverter
{
    private static readonly SolidColorBrush AppliedBrush = new(Color.FromArgb(30, 34, 197, 94));  // #1E22C55E
    private static readonly SolidColorBrush DefaultBrush = new(Color.FromArgb(255, 32, 36, 49));   // #202431

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (value is bool b && b) ? AppliedBrush : DefaultBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class BoolToStatusBorderConverter : IValueConverter
{
    private static readonly SolidColorBrush AppliedBrush = new(Color.FromArgb(255, 34, 197, 94));  // #22C55E
    private static readonly SolidColorBrush DefaultBrush = new(Color.FromArgb(255, 43, 48, 64));   // #2B3040

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (value is bool b && b) ? AppliedBrush : DefaultBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class BoolToStatusFgConverter : IValueConverter
{
    private static readonly SolidColorBrush AppliedBrush = new(Color.FromArgb(255, 34, 197, 94));  // #22C55E
    private static readonly SolidColorBrush DefaultBrush = new(Color.FromArgb(255, 150, 156, 171)); // #969CAB

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (value is bool b && b) ? AppliedBrush : DefaultBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class ZeroToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        int count = value is int i ? i : 0;
        return count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class NotNullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value != null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class BoolToCopyBtnTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool isCopied = value is bool b && b;
        return isCopied 
            ? LocalizationService.Instance["Games_Copied_Badge"]
            : LocalizationService.Instance["Games_Copy_Btn"];
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class BoolToElevationConverter : IValueConverter
{
    public double ElevationOffset { get; set; } = -4.0;

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (value is bool b && b) ? ElevationOffset : 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class BoolToGlowOpacityConverter : IValueConverter
{
    public double ActiveOpacity { get; set; } = 0.95;
    public double InactiveOpacity { get; set; } = 0.0;

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (value is bool b && b) ? ActiveOpacity : InactiveOpacity;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
