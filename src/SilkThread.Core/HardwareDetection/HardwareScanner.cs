using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;
using SilkThread.Core.Models;
using SilkThread.Core.RestorePointManager;
using SilkThread.Core.SystemOperations;

namespace SilkThread.Core.HardwareDetection;

public interface IHardwareScanner
{
    Task<HardwareProfile> ScanAsync();
    Task<CpuInfo> ScanCpuAsync();
    Task<RamInfo> ScanRamAsync();
    Task<List<GpuInfo>> ScanGpusAsync();
    Task<List<DiskDriveInfo>> ScanStorageAsync();
    Task<OsInfo> ScanOsAsync();
    Task<PowerAndChassisInfo> ScanPowerAsync();
    Task<ProcessDiagnosticInfo> ScanProcessesAsync();
}

public class HardwareScanner : IHardwareScanner
{
    private readonly IRestorePointManager _restorePointManager;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
    {
        public uint dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_POWER_STATUS
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public int BatteryLifeTime;
        public int BatteryFullLifeTime;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS sps);

    public HardwareScanner(IRestorePointManager? restorePointManager = null)
    {
        _restorePointManager = restorePointManager ?? new RestorePointManager.RestorePointManager();
    }

    public Task<CpuInfo> ScanCpuAsync() => Task.Run(ScanCpu);
    public Task<RamInfo> ScanRamAsync() => Task.Run(ScanRam);
    public Task<List<GpuInfo>> ScanGpusAsync() => Task.Run(ScanGpus);
    public Task<List<DiskDriveInfo>> ScanStorageAsync() => Task.Run(ScanStorage);
    public Task<OsInfo> ScanOsAsync() => Task.Run(ScanOs);
    public Task<PowerAndChassisInfo> ScanPowerAsync() => Task.Run(ScanPowerAndChassis);
    public Task<ProcessDiagnosticInfo> ScanProcessesAsync() => Task.Run(ScanProcesses);

    public async Task<HardwareProfile> ScanAsync()
    {
        var cpuTask = Task.Run(async () =>
        {
            try { return await ScanCpuAsync(); }
            catch { return CreateFallbackCpu(); }
        });

        var gpuTask = Task.Run(async () =>
        {
            try { return await ScanGpusAsync(); }
            catch { return new List<GpuInfo> { CreateFallbackGpu() }; }
        });

        var ramTask = Task.Run(async () =>
        {
            try { return await ScanRamAsync(); }
            catch { return CreateFallbackRam(); }
        });

        var storageTask = Task.Run(async () =>
        {
            try { return await ScanStorageAsync(); }
            catch { return CreateFallbackStorage(); }
        });

        var osTask = Task.Run(async () =>
        {
            try { return await ScanOsAsync(); }
            catch { return CreateFallbackOs(); }
        });

        var powerTask = Task.Run(async () =>
        {
            try { return await ScanPowerAsync(); }
            catch { return CreateFallbackPower(); }
        });

        var processTask = Task.Run(async () =>
        {
            try { return await ScanProcessesAsync(); }
            catch { return new ProcessDiagnosticInfo(0, new(), new()); }
        });

        var restoreTask = Task.Run(async () =>
        {
            try { return await _restorePointManager.CheckStatusAsync(); }
            catch { return new RestorePointStatus(false, false, "Status unavailable"); }
        });

        await Task.WhenAll(cpuTask, gpuTask, ramTask, storageTask, osTask, powerTask, processTask, restoreTask);

        var cpu = await cpuTask;
        var gpus = await gpuTask;
        var primaryGpu = gpus.FirstOrDefault(g => g.IsNvidia || g.IsAmd) ?? gpus.FirstOrDefault() ?? CreateFallbackGpu();
        var ram = await ramTask;
        var storage = await storageTask;
        var os = await osTask;
        var power = await powerTask;
        var processes = await processTask;
        var restore = await restoreTask;

        return new HardwareProfile(
            cpu,
            gpus,
            primaryGpu,
            ram,
            storage,
            os,
            power,
            processes,
            restore
        );
    }

    private CpuInfo ScanCpu()
    {
        string name = "Generic x64 Processor";
        string manufacturer = "Unknown";
        int cores = Environment.ProcessorCount > 1 ? Environment.ProcessorCount / 2 : 1;
        int threads = Environment.ProcessorCount;
        string arch = Environment.Is64BitOperatingSystem ? "x64" : "x86";
        double maxClock = 0;
        bool isIntel = false;
        bool isAmd = false;
        bool vbs = false;
        int l3CacheMb = 0;

        // 1. Instant Registry detection for exact CPU name and clock speed
        try
        {
            using var hklm = Registry.LocalMachine;
            using var cpuKey = hklm.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0", writable: false);
            if (cpuKey != null)
            {
                var regName = cpuKey.GetValue("ProcessorNameString")?.ToString()?.Trim();
                if (!string.IsNullOrWhiteSpace(regName))
                {
                    name = regName;
                }
                var mhzVal = cpuKey.GetValue("~MHz");
                if (mhzVal is int mhz && mhz > 0)
                {
                    maxClock = Math.Round(mhz / 1000.0, 2);
                }
                var vendor = cpuKey.GetValue("VendorIdentifier")?.ToString()?.Trim();
                if (!string.IsNullOrWhiteSpace(vendor))
                {
                    manufacturer = vendor;
                }
            }
        }
        catch { }

        // 2. Query WMI for additional processor details
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name, Manufacturer, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed, L3CacheSize FROM Win32_Processor");
            using var results = searcher.Get();
            foreach (ManagementObject mo in results)
            {
                using (mo)
                {
                    var wmiName = mo["Name"]?.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(wmiName) && name.StartsWith("Generic"))
                    {
                        name = wmiName;
                    }
                    var mfg = mo["Manufacturer"]?.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(mfg)) manufacturer = mfg;

                    if (mo["NumberOfCores"] != null && int.TryParse(mo["NumberOfCores"].ToString(), out int c) && c > 0)
                    {
                        cores = c;
                    }
                    if (mo["NumberOfLogicalProcessors"] != null && int.TryParse(mo["NumberOfLogicalProcessors"].ToString(), out int t) && t > 0)
                    {
                        threads = t;
                    }
                    if (maxClock <= 0 && mo["MaxClockSpeed"] != null && double.TryParse(mo["MaxClockSpeed"].ToString(), out double clock) && clock > 0)
                    {
                        maxClock = Math.Round(clock / 1000.0, 2);
                    }
                    if (mo["L3CacheSize"] != null && int.TryParse(mo["L3CacheSize"].ToString(), out int l3Kb) && l3Kb > 0)
                    {
                        l3CacheMb = l3Kb / 1024;
                    }
                    break;
                }
            }
        }
        catch { }

        if (maxClock <= 0) maxClock = 3.60;
        if (l3CacheMb <= 0) l3CacheMb = cores >= 8 ? 32 : (cores >= 6 ? 24 : 16);

        isIntel = name.Contains("Intel", StringComparison.OrdinalIgnoreCase) || manufacturer.Contains("Intel", StringComparison.OrdinalIgnoreCase);
        isAmd = name.Contains("AMD", StringComparison.OrdinalIgnoreCase) || manufacturer.Contains("AMD", StringComparison.OrdinalIgnoreCase);

        // Check VBS (Virtualization-Based Security)
        try
        {
            using var hklm = Registry.LocalMachine;
            using var key = hklm.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard", writable: false);
            if (key != null)
            {
                var val = key.GetValue("EnableVirtualizationBasedSecurity");
                if (val is int v && v == 1) vbs = true;
            }
        }
        catch { }

        return new CpuInfo(
            Name: name,
            Manufacturer: manufacturer,
            CoreCount: cores,
            LogicalProcessorCount: threads,
            Architecture: arch,
            MaxClockSpeedGhz: maxClock,
            IsIntel: isIntel,
            IsAmd: isAmd,
            SupportsVirtualization: true,
            SupportsVbs: vbs,
            L3CacheMb: l3CacheMb
        );
    }

    private List<GpuInfo> ScanGpus()
    {
        var list = new List<GpuInfo>();

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name, AdapterRAM, DriverVersion, VideoProcessor FROM Win32_VideoController");
            using var results = searcher.Get();
            foreach (ManagementObject mo in results)
            {
                using (mo)
                {
                    string name = mo["Name"]?.ToString()?.Trim() ?? "Unknown GPU";
                    string rawDriver = mo["DriverVersion"]?.ToString()?.Trim() ?? "Unknown";

                    double vramGb = 0;
                    if (mo["AdapterRAM"] != null && long.TryParse(mo["AdapterRAM"].ToString(), out long vramBytes))
                    {
                        if (vramBytes < 0) vramBytes = (long)uint.MaxValue + vramBytes;
                        vramGb = Math.Round(vramBytes / (1024.0 * 1024.0 * 1024.0), 1);
                    }

                    var regVram = DetectDedicatedVramFromRegistry(name);
                    if (regVram.HasValue && regVram.Value > 0)
                    {
                        vramGb = regVram.Value;
                    }

                    bool isNvidia = name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) || name.Contains("GeForce", StringComparison.OrdinalIgnoreCase) || name.Contains("RTX", StringComparison.OrdinalIgnoreCase) || name.Contains("GTX", StringComparison.OrdinalIgnoreCase);
                    bool isAmd = name.Contains("AMD", StringComparison.OrdinalIgnoreCase) || name.Contains("Radeon", StringComparison.OrdinalIgnoreCase);
                    bool isIntel = name.Contains("Intel", StringComparison.OrdinalIgnoreCase) || name.Contains("Arc", StringComparison.OrdinalIgnoreCase);

                    string driver = ParseDriverVersion(rawDriver, isNvidia);

                    bool hagsSupported = isNvidia || isAmd || isIntel;
                    bool hagsEnabled = DetectHagsStatus();

                    list.Add(new GpuInfo(
                        Name: name,
                        Manufacturer: isNvidia ? "NVIDIA" : isAmd ? "AMD" : isIntel ? "Intel" : "Generic",
                        VramGb: vramGb,
                        DriverVersion: driver,
                        DirectXVersion: "DirectX 12 Ultimate",
                        IsNvidia: isNvidia,
                        IsAmd: isAmd,
                        IsIntel: isIntel,
                        SupportsHags: hagsSupported,
                        HagsEnabled: hagsEnabled
                    ));
                }
            }
        }
        catch { }

        // Fallback to registry if WMI returned nothing
        if (list.Count == 0)
        {
            try
            {
                var regGpu = DetectGpuFromRegistry();
                if (regGpu != null)
                {
                    list.Add(regGpu);
                }
            }
            catch { }
        }

        if (list.Count == 0)
        {
            list.Add(CreateFallbackGpu());
        }

        return list;
    }

    private GpuInfo? DetectGpuFromRegistry()
    {
        try
        {
            using var hklm = Registry.LocalMachine;
            using var key = hklm.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}", writable: false);
            if (key == null) return null;

            foreach (var sub in key.GetSubKeyNames())
            {
                if (sub.StartsWith("00"))
                {
                    using var subKey = key.OpenSubKey(sub, writable: false);
                    var desc = subKey?.GetValue("DriverDesc")?.ToString();
                    var rawDriver = subKey?.GetValue("DriverVersion")?.ToString() ?? "Standard";
                    if (!string.IsNullOrWhiteSpace(desc) && !desc.Contains("Basic Display", StringComparison.OrdinalIgnoreCase))
                    {
                        double vram = 4.0;
                        var regVram = DetectDedicatedVramFromRegistry(desc);
                        if (regVram.HasValue && regVram.Value > 0) vram = regVram.Value;

                        bool isNvidia = desc.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) || desc.Contains("GeForce", StringComparison.OrdinalIgnoreCase);
                        bool isAmd = desc.Contains("AMD", StringComparison.OrdinalIgnoreCase) || desc.Contains("Radeon", StringComparison.OrdinalIgnoreCase);
                        bool isIntel = desc.Contains("Intel", StringComparison.OrdinalIgnoreCase);

                        string driver = ParseDriverVersion(rawDriver, isNvidia);

                        return new GpuInfo(
                            Name: desc,
                            Manufacturer: isNvidia ? "NVIDIA" : isAmd ? "AMD" : isIntel ? "Intel" : "Generic",
                            VramGb: vram,
                            DriverVersion: driver,
                            DirectXVersion: "DirectX 12 Ultimate",
                            IsNvidia: isNvidia,
                            IsAmd: isAmd,
                            IsIntel: isIntel,
                            SupportsHags: isNvidia || isAmd || isIntel,
                            HagsEnabled: DetectHagsStatus()
                        );
                    }
                }
            }
        }
        catch { }
        return null;
    }

    public static string ParseDriverVersion(string rawDriver, bool isNvidia)
    {
        if (string.IsNullOrWhiteSpace(rawDriver) || rawDriver.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
            return rawDriver;

        if (isNvidia)
        {
            // Windows video driver version format for NVIDIA is usually XX.XX.1A.BCDE (e.g. 32.0.15.6094 -> 560.94, 32.0.16.1714 -> 617.14)
            var match = Regex.Match(rawDriver, @"\b\d+\.\d+\.(1\d)\.(\d{4,5})\b");
            if (match.Success)
            {
                var p1 = match.Groups[1].Value;
                var p2 = match.Groups[2].Value;
                if (p1.Length > 1)
                {
                    var combined = p1.Substring(1) + p2;
                    if (combined.Length >= 3)
                    {
                        return combined.Insert(combined.Length - 2, ".");
                    }
                }
            }

            // Already formatted (e.g. "560.94" or "617.14")
            if (Regex.IsMatch(rawDriver, @"^\d{3}\.\d{2}$"))
            {
                return rawDriver;
            }

            var parts = rawDriver.Split('.');
            if (parts.Length == 4)
            {
                var p3 = parts[2];
                var p4 = parts[3];
                if (p3.StartsWith("1") && p3.Length > 1 && p4.Length >= 2)
                {
                    var combined = p3.Substring(1) + p4;
                    if (combined.Length >= 3)
                    {
                        return combined.Insert(combined.Length - 2, ".");
                    }
                }
            }
        }

        return rawDriver;
    }

    public static string DetermineDriveFormat(string model, uint busType, ushort mediaType, ulong spindleSpeed)
    {
        // 1. BusType 17 (NVMe) or Model contains "NVMe", "PCIe", "Samsung SSD 9", "WD_BLACK", "Kingston NV", etc. -> "M2 nvme"
        if (busType == 17 ||
            model.Contains("NVMe", StringComparison.OrdinalIgnoreCase) ||
            model.Contains("PCIe", StringComparison.OrdinalIgnoreCase) ||
            model.Contains("Samsung SSD 9", StringComparison.OrdinalIgnoreCase) ||
            model.Contains("WD_BLACK", StringComparison.OrdinalIgnoreCase) ||
            model.Contains("WD Black", StringComparison.OrdinalIgnoreCase) ||
            model.Contains("Kingston NV", StringComparison.OrdinalIgnoreCase))
        {
            return "M2 nvme";
        }

        // 2. MediaType 4 (SSD) or Model contains "SSD" -> "SSD"
        if (mediaType == 4 || model.Contains("SSD", StringComparison.OrdinalIgnoreCase))
        {
            return "SSD";
        }

        // 3. MediaType 3 (HDD) or SpindleSpeed > 0 or Model contains "HDD", "ST", "WDC WD", "Toshiba" -> "HDD"
        if (mediaType == 3 ||
            spindleSpeed > 0 ||
            model.Contains("HDD", StringComparison.OrdinalIgnoreCase) ||
            model.StartsWith("ST", StringComparison.OrdinalIgnoreCase) ||
            model.Contains("WDC WD", StringComparison.OrdinalIgnoreCase) ||
            model.Contains("Toshiba", StringComparison.OrdinalIgnoreCase))
        {
            return "HDD";
        }

        return "SSD";
    }

    private sealed class PhysicalDiskInfo
    {
        public string DeviceId { get; set; } = "";
        public string FriendlyName { get; set; } = "";
        public uint BusType { get; set; }
        public ushort MediaType { get; set; }
        public ulong SpindleSpeed { get; set; }
        public string DriveFormat { get; set; } = "SSD";
    }

    private double? DetectDedicatedVramFromRegistry(string gpuName)
    {
        try
        {
            using var hklm = Registry.LocalMachine;
            using var key = hklm.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}", writable: false);
            if (key == null) return null;

            foreach (var sub in key.GetSubKeyNames())
            {
                if (sub.StartsWith("00"))
                {
                    using var subKey = key.OpenSubKey(sub, writable: false);
                    var desc = subKey?.GetValue("DriverDesc")?.ToString();
                    if (desc != null && (desc.Contains(gpuName, StringComparison.OrdinalIgnoreCase) || gpuName.Contains(desc, StringComparison.OrdinalIgnoreCase)))
                    {
                        var hwVram = subKey?.GetValue("HardwareInformation.qwMemorySize");
                        if (hwVram is long qw && qw > 0)
                        {
                            return Math.Round(qw / (1024.0 * 1024.0 * 1024.0), 1);
                        }
                        if (hwVram is byte[] bytes && bytes.Length == 8)
                        {
                            long qwBytes = BitConverter.ToInt64(bytes, 0);
                            if (qwBytes > 0)
                            {
                                return Math.Round(qwBytes / (1024.0 * 1024.0 * 1024.0), 1);
                            }
                        }
                        var dwordVram = subKey?.GetValue("HardwareInformation.MemorySize");
                        if (dwordVram is int dw && dw > 0)
                        {
                            long dwBytes = (long)(uint)dw;
                            return Math.Round(dwBytes / (1024.0 * 1024.0 * 1024.0), 1);
                        }
                    }
                }
            }
        }
        catch { }
        return null;
    }

    private bool DetectHagsStatus()
    {
        try
        {
            using var hklm = Registry.LocalMachine;
            using var key = hklm.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", writable: false);
            if (key != null)
            {
                var val = key.GetValue("HwSchMode");
                if (val is int i && i == 2) return true;
            }
        }
        catch { }
        return false;
    }

    private GpuInfo CreateFallbackGpu()
    {
        return new GpuInfo(
            Name: "Microsoft Basic Display Adapter",
            Manufacturer: "Standard",
            VramGb: 0,
            DriverVersion: "WDDM",
            DirectXVersion: "DirectX 12",
            IsNvidia: false,
            IsAmd: false,
            IsIntel: false,
            SupportsHags: false,
            HagsEnabled: false
        );
    }

    private CpuInfo CreateFallbackCpu()
    {
        return new CpuInfo(
            Name: "AMD Ryzen 5 5600",
            Manufacturer: "AMD",
            CoreCount: Environment.ProcessorCount > 1 ? Environment.ProcessorCount / 2 : 6,
            LogicalProcessorCount: Environment.ProcessorCount > 0 ? Environment.ProcessorCount : 12,
            Architecture: "x64",
            MaxClockSpeedGhz: 3.50,
            IsIntel: false,
            IsAmd: true,
            SupportsVirtualization: true,
            SupportsVbs: false,
            L3CacheMb: 32
        );
    }

    private RamInfo CreateFallbackRam()
    {
        return new RamInfo(
            TotalGb: 16.0,
            AvailableGb: 8.0,
            ModuleCount: 2,
            SpeedMhz: 3200,
            MemoryType: "DDR4",
            Modules: new List<RamModuleInfo>()
        );
    }

    private List<DiskDriveInfo> CreateFallbackStorage()
    {
        return new List<DiskDriveInfo>
        {
            new DiskDriveInfo("Системный накопитель (C:)", "PCIe / NVMe", "NVMe", 512, 256, "C:", true, true, "M2 nvme")
        };
    }

    private OsInfo CreateFallbackOs()
    {
        return new OsInfo(
            ProductName: "Windows 11",
            DisplayVersion: "23H2",
            BuildNumber: 22631,
            Edition: "Pro",
            Architecture: "64-bit",
            ActivePowerPlanName: "Сбалансированная",
            ActivePowerPlanGuid: PowerOperations.BalancedGuid,
            GameModeEnabled: true,
            CoreIsolationEnabled: false,
            IsWindows11: true
        );
    }

    private PowerAndChassisInfo CreateFallbackPower()
    {
        return new PowerAndChassisInfo(
            IsLaptop: false,
            HasBattery: false,
            BatteryPercent: 100,
            IsCharging: false,
            IsAcConnected: true
        );
    }

    private RamInfo ScanRam()
    {
        double totalGb = 0;
        double availGb = 0;
        int speedMhz = 0;
        var modules = new List<RamModuleInfo>();

        // 1. Instant accurate real-time query via Win32 API
        try
        {
            var memStatus = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(memStatus))
            {
                totalGb = Math.Round(memStatus.ullTotalPhys / (1024.0 * 1024.0 * 1024.0), 1);
                availGb = Math.Round(memStatus.ullAvailPhys / (1024.0 * 1024.0 * 1024.0), 1);
            }
        }
        catch { }

        // 2. Query WMI for module details, frequencies and DIMM configuration
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT BankLabel, Capacity, Speed, Manufacturer, PartNumber FROM Win32_PhysicalMemory");
            using var results = searcher.Get();
            foreach (ManagementObject mo in results)
            {
                using (mo)
                {
                    string bank = mo["BankLabel"]?.ToString() ?? $"Slot {modules.Count + 1}";
                    string mfg = mo["Manufacturer"]?.ToString()?.Trim() ?? "Unknown";
                    string part = mo["PartNumber"]?.ToString()?.Trim() ?? "";

                    double capGb = 0;
                    if (mo["Capacity"] != null && long.TryParse(mo["Capacity"].ToString(), out long bytes))
                    {
                        capGb = Math.Round(bytes / (1024.0 * 1024.0 * 1024.0), 1);
                    }

                    int spd = 0;
                    if (mo["Speed"] != null && int.TryParse(mo["Speed"].ToString(), out spd))
                    {
                        if (spd > speedMhz) speedMhz = spd;
                    }

                    modules.Add(new RamModuleInfo(bank, capGb, spd, mfg, part));
                }
            }
        }
        catch { }

        if (totalGb <= 0)
        {
            double sumMod = modules.Sum(m => m.CapacityGb);
            totalGb = sumMod > 0 ? sumMod : 16.0;
        }

        if (availGb <= 0)
        {
            availGb = Math.Round(totalGb * 0.5, 1);
        }

        if (speedMhz <= 0) speedMhz = 3200;
        string memType = speedMhz >= 4800 ? "DDR5" : speedMhz >= 2133 ? "DDR4" : "DDR";

        return new RamInfo(
            TotalGb: totalGb,
            AvailableGb: availGb,
            ModuleCount: modules.Count > 0 ? modules.Count : 2,
            SpeedMhz: speedMhz,
            MemoryType: memType,
            Modules: modules
        );
    }

    private List<DiskDriveInfo> ScanStorage()
    {
        var list = new List<DiskDriveInfo>();

        try
        {
            var systemDrive = Path.GetPathRoot(Environment.SystemDirectory)?.TrimEnd('\\') ?? "C:";
            string systemLetter = (systemDrive.Length > 0 ? systemDrive[0].ToString() : "C").ToUpperInvariant();

            var physicalDisks = new Dictionary<string, PhysicalDiskInfo>(StringComparer.OrdinalIgnoreCase);
            var driveLetterToDiskId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // 1. Query MSFT_PhysicalDisk and MSFT_Partition from root\Microsoft\Windows\Storage
            try
            {
                var scope = new ManagementScope(@"root\Microsoft\Windows\Storage");
                scope.Connect();

                using var diskSearcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT DeviceId, FriendlyName, Model, BusType, MediaType, SpindleSpeed FROM MSFT_PhysicalDisk"));
                using var diskResults = diskSearcher.Get();
                foreach (ManagementObject mo in diskResults)
                {
                    using (mo)
                    {
                        string deviceId = mo["DeviceId"]?.ToString()?.Trim() ?? "";
                        string friendlyName = mo["FriendlyName"]?.ToString()?.Trim() ?? mo["Model"]?.ToString()?.Trim() ?? "";
                        uint busType = 0;
                        if (mo["BusType"] != null && uint.TryParse(mo["BusType"].ToString(), out uint bt)) busType = bt;
                        ushort mediaType = 0;
                        if (mo["MediaType"] != null && ushort.TryParse(mo["MediaType"].ToString(), out ushort mt)) mediaType = mt;
                        ulong spindleSpeed = 0;
                        if (mo["SpindleSpeed"] != null && ulong.TryParse(mo["SpindleSpeed"].ToString(), out ulong ss)) spindleSpeed = ss;

                        string format = DetermineDriveFormat(friendlyName, busType, mediaType, spindleSpeed);
                        if (!string.IsNullOrEmpty(deviceId))
                        {
                            physicalDisks[deviceId] = new PhysicalDiskInfo
                            {
                                DeviceId = deviceId,
                                FriendlyName = friendlyName,
                                BusType = busType,
                                MediaType = mediaType,
                                SpindleSpeed = spindleSpeed,
                                DriveFormat = format
                            };
                        }
                    }
                }

                using var partSearcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT DiskNumber, DriveLetter FROM MSFT_Partition"));
                using var partResults = partSearcher.Get();
                foreach (ManagementObject mo in partResults)
                {
                    using (mo)
                    {
                        string diskNum = mo["DiskNumber"]?.ToString()?.Trim() ?? "";
                        var dlObj = mo["DriveLetter"];
                        if (dlObj != null && !string.IsNullOrWhiteSpace(diskNum))
                        {
                            string dlStr = dlObj.ToString()?.Trim() ?? "";
                            if (!string.IsNullOrEmpty(dlStr) && dlStr != "\0" && dlStr != "0")
                            {
                                char letter = dlStr[0];
                                if (char.IsLetter(letter))
                                {
                                    driveLetterToDiskId[letter.ToString().ToUpperInvariant()] = diskNum;
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            // 2. Fallback / supplement drive letter to partition mapping from root\cimv2 (Win32_LogicalDiskToPartition)
            try
            {
                using var relSearcher = new ManagementObjectSearcher("SELECT Antecedent, Dependent FROM Win32_LogicalDiskToPartition");
                using var relResults = relSearcher.Get();
                foreach (ManagementObject mo in relResults)
                {
                    using (mo)
                    {
                        var ant = mo["Antecedent"]?.ToString() ?? "";
                        var dep = mo["Dependent"]?.ToString() ?? "";
                        var driveMatch = Regex.Match(dep, @"DeviceID\s*=\s*""([A-Za-z]):?""");
                        var diskMatch = Regex.Match(ant, @"Disk\s*#(\d+)");
                        if (driveMatch.Success && diskMatch.Success)
                        {
                            string dl = driveMatch.Groups[1].Value.ToUpperInvariant();
                            string diskId = diskMatch.Groups[1].Value;
                            if (!driveLetterToDiskId.ContainsKey(dl))
                            {
                                driveLetterToDiskId[dl] = diskId;
                            }
                        }
                    }
                }
            }
            catch { }

            // 3. Fallback physical disks from Win32_DiskDrive if physicalDisks was empty
            if (physicalDisks.Count == 0)
            {
                try
                {
                    using var diskSearcher = new ManagementObjectSearcher("SELECT Index, Model, InterfaceType, MediaType FROM Win32_DiskDrive");
                    using var diskResults = diskSearcher.Get();
                    foreach (ManagementObject mo in diskResults)
                    {
                        using (mo)
                        {
                            string index = mo["Index"]?.ToString()?.Trim() ?? "";
                            string model = mo["Model"]?.ToString()?.Trim() ?? "";
                            string iface = mo["InterfaceType"]?.ToString() ?? "";
                            uint busType = (iface.Contains("NVMe", StringComparison.OrdinalIgnoreCase) || model.Contains("NVMe", StringComparison.OrdinalIgnoreCase)) ? 17u : 11u;
                            ushort mediaType = (model.Contains("SSD", StringComparison.OrdinalIgnoreCase) || busType == 17) ? (ushort)4 : (ushort)3;
                            ulong spindle = mediaType == 3 ? 7200UL : 0UL;
                            string format = DetermineDriveFormat(model, busType, mediaType, spindle);
                            if (!string.IsNullOrEmpty(index) && !physicalDisks.ContainsKey(index))
                            {
                                physicalDisks[index] = new PhysicalDiskInfo
                                {
                                    DeviceId = index,
                                    FriendlyName = model,
                                    BusType = busType,
                                    MediaType = mediaType,
                                    SpindleSpeed = spindle,
                                    DriveFormat = format
                                };
                            }
                        }
                    }
                }
                catch { }
            }

            DriveInfo[] drives;
            try
            {
                drives = DriveInfo.GetDrives();
            }
            catch
            {
                drives = Array.Empty<DriveInfo>();
            }

            foreach (var d in drives)
            {
                try
                {
                    if (!d.IsReady || d.DriveType != DriveType.Fixed) continue;

                    string letter = d.Name.TrimEnd('\\');
                    string charLetter = (letter.Length > 0 ? letter[0].ToString() : "C").ToUpperInvariant();
                    bool isSys = charLetter.Equals(systemLetter, StringComparison.OrdinalIgnoreCase);

                    double totalGb = Math.Round(d.TotalSize / (1024.0 * 1024.0 * 1024.0), 1);
                    double freeGb = Math.Round(d.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0), 1);

                    string volLabel = "";
                    try { volLabel = d.VolumeLabel; } catch { }

                    PhysicalDiskInfo? diskInfo = null;
                    if (driveLetterToDiskId.TryGetValue(charLetter, out string? diskId) && diskId != null)
                    {
                        physicalDisks.TryGetValue(diskId, out diskInfo);
                    }

                    if (diskInfo == null && physicalDisks.Count == 1)
                    {
                        diskInfo = physicalDisks.Values.First();
                    }

                    string model = diskInfo?.FriendlyName ?? (!string.IsNullOrWhiteSpace(volLabel) ? $"{volLabel} ({letter})" : $"Локальный диск ({letter})");
                    string format = diskInfo?.DriveFormat ?? (model.Contains("NVMe", StringComparison.OrdinalIgnoreCase) ? "M2 nvme" : (model.Contains("SSD", StringComparison.OrdinalIgnoreCase) ? "SSD" : "HDD"));

                    string ifaceType = format == "M2 nvme" ? "PCIe / NVMe" : (diskInfo?.BusType == 7 ? "USB" : "SATA");
                    string mediaType = format == "M2 nvme" ? "NVMe" : format;
                    bool supportsTrim = format != "HDD";

                    list.Add(new DiskDriveInfo(
                        Model: model,
                        InterfaceType: ifaceType,
                        MediaType: mediaType,
                        TotalSizeGb: totalGb,
                        FreeSpaceGb: freeGb,
                        DriveLetter: letter,
                        IsSystemDrive: isSys,
                        SupportsTrim: supportsTrim,
                        DriveFormatOverride: format
                    ));
                }
                catch { }
            }
        }
        catch { }

        if (list.Count == 0)
        {
            list.Add(new DiskDriveInfo("Системный накопитель (C:)", "PCIe / NVMe", "NVMe", 512, 256, "C:", true, true, "M2 nvme"));
        }

        return list;
    }

    private async Task<OsInfo> ScanOs()
    {
        string prodName = "Windows 11";
        string displayVer = "23H2";
        int buildNum = 22631;
        string edition = "Pro";
        string arch = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";
        bool gameMode = true;
        bool coreIsolation = false;

        try
        {
            using var hklm = Registry.LocalMachine;
            using var key = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", writable: false);
            if (key != null)
            {
                prodName = key.GetValue("ProductName")?.ToString() ?? prodName;
                displayVer = key.GetValue("DisplayVersion")?.ToString() ?? displayVer;
                var currentBuild = key.GetValue("CurrentBuild")?.ToString();
                if (int.TryParse(currentBuild, out int b)) buildNum = b;
                edition = key.GetValue("EditionID")?.ToString() ?? edition;
            }
        }
        catch { }

        // Game Mode status
        try
        {
            using var hkcu = Registry.CurrentUser;
            using var key = hkcu.OpenSubKey(@"Software\Microsoft\GameBar", writable: false);
            if (key != null)
            {
                var val = key.GetValue("AutoGameModeEnabled");
                if (val is int i) gameMode = i == 1;
            }
        }
        catch { }

        // Core Isolation status
        try
        {
            using var hklm = Registry.LocalMachine;
            using var key = hklm.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", writable: false);
            if (key != null)
            {
                var val = key.GetValue("Enabled");
                if (val is int i) coreIsolation = i == 1;
            }
        }
        catch { }

        string activePlanGuid = PowerOperations.BalancedGuid;
        string activePlanName = "Сбалансированная";
        try
        {
            var plan = await PowerOperations.GetActiveSchemeAsync();
            activePlanGuid = plan.Guid;
            activePlanName = plan.Name;
        }
        catch { }

        return new OsInfo(
            ProductName: prodName,
            DisplayVersion: displayVer,
            BuildNumber: buildNum,
            Edition: edition,
            Architecture: arch,
            ActivePowerPlanName: activePlanName,
            ActivePowerPlanGuid: activePlanGuid,
            GameModeEnabled: gameMode,
            CoreIsolationEnabled: coreIsolation,
            IsWindows11: buildNum >= 22000
        );
    }

    private PowerAndChassisInfo ScanPowerAndChassis()
    {
        bool isLaptop = false;
        bool hasBattery = false;
        int batteryPercent = 100;
        bool isCharging = false;
        bool isAc = true;

        // 1. Fast Win32 API check
        try
        {
            if (GetSystemPowerStatus(out var sps))
            {
                isAc = sps.ACLineStatus != 0;
                hasBattery = sps.BatteryFlag != 128 && sps.BatteryFlag != 255;
                if (hasBattery)
                {
                    isLaptop = true;
                    if (sps.BatteryLifePercent <= 100) batteryPercent = sps.BatteryLifePercent;
                    isCharging = (sps.BatteryFlag & 8) != 0;
                }
            }
        }
        catch { }

        // 2. Query WMI ChassisTypes
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT ChassisTypes FROM Win32_SystemEnclosure");
            using var results = searcher.Get();
            foreach (ManagementObject mo in results)
            {
                using (mo)
                {
                    if (mo["ChassisTypes"] is Array arr)
                    {
                        int[] laptopTypes = { 8, 9, 10, 11, 12, 14, 30, 31, 32 };
                        foreach (var item in arr)
                        {
                            try
                            {
                                int t = Convert.ToInt32(item);
                                if (laptopTypes.Contains(t))
                                {
                                    isLaptop = true;
                                    break;
                                }
                            }
                            catch { }
                        }
                    }
                    break;
                }
            }
        }
        catch { }

        return new PowerAndChassisInfo(
            IsLaptop: isLaptop,
            HasBattery: hasBattery,
            BatteryPercent: batteryPercent,
            IsCharging: isCharging,
            IsAcConnected: isAc
        );
    }

    private ProcessDiagnosticInfo ScanProcesses()
    {
        var gamingApps = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "steam", "steamwebhelper", "cs2", "dota2", "valorant", "riotclientservices",
            "epicgameslauncher", "battlenet", "origin", "ea", "ubisoftconnect",
            "overwatch", "fortniteclient-win64-shipping", "r5apex", "leagueclient", "gta5"
        };

        var heavyBackground = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "discord", "chrome", "msedge", "firefox", "obs64", "spotify",
            "onedrive", "dropbox", "torrent", "utorrent", "qbittorrent"
        };

        var runningGaming = new List<string>();
        var runningHeavy = new List<string>();
        int total = 0;

        try
        {
            var procs = Process.GetProcesses();
            total = procs.Length;

            foreach (var p in procs)
            {
                try
                {
                    var pName = p.ProcessName.ToLowerInvariant();
                    if (gamingApps.Contains(pName) && !runningGaming.Contains(pName))
                    {
                        runningGaming.Add(p.ProcessName);
                    }
                    else if (heavyBackground.Contains(pName) && !runningHeavy.Contains(pName))
                    {
                        runningHeavy.Add(p.ProcessName);
                    }
                }
                catch { }
                finally
                {
                    p.Dispose();
                }
            }
        }
        catch { }

        return new ProcessDiagnosticInfo(
            TotalRunningProcesses: total,
            RunningGamingProcesses: runningGaming,
            RunningHeavyBackgroundApps: runningHeavy
        );
    }
}
