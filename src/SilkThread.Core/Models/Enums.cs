namespace SilkThread.Core.Models;

public enum RiskLevel
{
    Low,
    Medium,
    High,
    Critical
}

public enum RecommendationLevel
{
    Recommended,
    Optional,
    NotRecommended,
    Experimental
}

public enum TweakCategory
{
    CpuOptimization,
    GpuOptimization,
    RamOptimization,
    GamingPerformance,
    WindowsPerformance,
    InputAndLatency,
    NetworkOptimization,
    StorageOptimization,
    BackgroundProcesses,
    WindowsServices,
    PowerManagement,
    WindowsExplorerAndUi,
    SecurityAndPrivacy,
    SystemMaintenance,
    AdvancedTweaks
}

public enum TweakStatus
{
    Unknown,
    NotApplied,
    Applied,
    Incompatible,
    RequiresVerification,
    Failed
}
