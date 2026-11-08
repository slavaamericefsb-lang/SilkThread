using System.Collections.Generic;

namespace SilkThread.Core.Models;

public record CpuInfo(
    string Name,
    string Manufacturer,
    int CoreCount,
    int LogicalProcessorCount,
    string Architecture,
    double MaxClockSpeedGhz,
    bool IsIntel,
    bool IsAmd,
    bool SupportsVirtualization,
    bool SupportsVbs,
    int L3CacheMb = 0
);

public record GpuInfo(
    string Name,
    string Manufacturer,
    double VramGb,
    string DriverVersion,
    string DirectXVersion,
    bool IsNvidia,
    bool IsAmd,
    bool IsIntel,
    bool SupportsHags,
    bool HagsEnabled
)
{
    public string Vendor => Manufacturer;
    public string FormattedVram => $"{VramGb:F1} GB";
    public string HagsStatusString => SupportsHags ? (HagsEnabled ? "Active" : "Disabled") : "Not Supported";
};

public record RamModuleInfo(
    string BankLabel,
    double CapacityGb,
    int SpeedMhz,
    string Manufacturer,
    string PartNumber
);

public record RamInfo(
    double TotalGb,
    double AvailableGb,
    int ModuleCount,
    int SpeedMhz,
    string MemoryType,
    List<RamModuleInfo> Modules
);

public record DiskDriveInfo(
    string Model,
    string InterfaceType,
    string MediaType, // NVMe, SSD, HDD
    double TotalSizeGb,
    double FreeSpaceGb,
    string DriveLetter,
    bool IsSystemDrive,
    bool SupportsTrim
)
{
    public string FormattedSize => $"{TotalSizeGb:F0} GB";
    public string FormattedFreeSpace => $"{FreeSpaceGb:F0} GB free";
    public string IsTrimSupportedString => SupportsTrim ? "Enabled" : "Disabled";
    public double UsedPercent => TotalSizeGb > 0 ? Math.Clamp((TotalSizeGb - FreeSpaceGb) / TotalSizeGb * 100.0, 0, 100) : 0;
    public string FormattedUsedSpace => $"{(TotalSizeGb - FreeSpaceGb):F0} GB";
    public bool IsNvme => MediaType.Contains("NVMe", System.StringComparison.OrdinalIgnoreCase);
    public bool IsSsd => MediaType.Contains("SSD", System.StringComparison.OrdinalIgnoreCase) || IsNvme;
};

public record OsInfo(
    string ProductName,
    string DisplayVersion,
    int BuildNumber,
    string Edition,
    string Architecture,
    string ActivePowerPlanName,
    string ActivePowerPlanGuid,
    bool GameModeEnabled,
    bool CoreIsolationEnabled,
    bool IsWindows11
);

public record PowerAndChassisInfo(
    bool IsLaptop,
    bool HasBattery,
    int BatteryPercent,
    bool IsCharging,
    bool IsAcConnected
);

public record ProcessDiagnosticInfo(
    int TotalRunningProcesses,
    List<string> RunningGamingProcesses,
    List<string> RunningHeavyBackgroundApps
);

public record RestorePointStatus(
    bool SystemRestoreAvailable,
    bool SystemDriveProtected,
    string StatusDescription
);

public record HardwareProfile(
    CpuInfo Cpu,
    List<GpuInfo> Gpus,
    GpuInfo PrimaryGpu,
    RamInfo Ram,
    List<DiskDriveInfo> Storage,
    OsInfo Os,
    PowerAndChassisInfo Power,
    ProcessDiagnosticInfo Processes,
    RestorePointStatus RestorePoint
);
