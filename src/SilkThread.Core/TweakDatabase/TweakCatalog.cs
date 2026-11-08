using System;
using System.Collections.Generic;
using System.Linq;
using SilkThread.Core.Models;

namespace SilkThread.Core.TweakDatabase;

public static class TweakCatalog
{
    public static List<TweakDefinition> GetAllTweaks()
    {
        var all = new List<TweakDefinition>();

        all.AddRange(CpuTweaks.GetTweaks());
        all.AddRange(GpuTweaks.GetTweaks());
        all.AddRange(RamTweaks.GetTweaks());
        all.AddRange(GamingPerformanceTweaks.GetTweaks());
        all.AddRange(WindowsPerformanceTweaks.GetTweaks());
        all.AddRange(InputLatencyTweaks.GetTweaks());
        all.AddRange(NetworkOptimizationTweaks.GetTweaks());
        all.AddRange(StorageOptimizationTweaks.GetTweaks());
        all.AddRange(BackgroundProcessesTweaks.GetTweaks());
        all.AddRange(WindowsServicesTweaks.GetTweaks());
        all.AddRange(PowerManagementTweaks.GetTweaks());
        all.AddRange(WindowsExplorerUiTweaks.GetTweaks());
        all.AddRange(SecurityPrivacyTweaks.GetTweaks());
        all.AddRange(SystemMaintenanceTweaks.GetTweaks());
        all.AddRange(AdvancedTweaks.GetTweaks());

        return all;
    }

    public static Dictionary<TweakCategory, List<TweakDefinition>> GetTweaksByCategory()
    {
        return GetAllTweaks()
            .GroupBy(t => t.Category)
            .ToDictionary(g => g.Key, g => g.ToList());
    }
}
