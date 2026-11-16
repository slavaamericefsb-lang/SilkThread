using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SilkThread.App.Services;
using SilkThread.Core.HardwareDetection;
using SilkThread.Core.Models;
using Windows.ApplicationModel.DataTransfer;

namespace SilkThread.App.ViewModels;

public partial class PcScannerViewModel : ObservableObject
{
    private readonly IHardwareScanner _hardwareScanner;
    private readonly ILocalizationService _loc;

    [ObservableProperty]
    private HardwareProfile? _profile;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private string? _copyReportSuccessMessage;

    [ObservableProperty]
    private bool _hasCopyMessage;

    [ObservableProperty]
    private string _cpuName = "Сканирование CPU...";

    [ObservableProperty]
    private string _cpuCoresThreads = "Определение топологии...";

    [ObservableProperty]
    private string _cpuMaxClock = "0 ГГц";

    [ObservableProperty]
    private string _cpuVbs = "Отключено";

    [ObservableProperty]
    private bool _isCpuVbsActive;

    [ObservableProperty]
    private string _cpuArch = "x64";

    [ObservableProperty]
    private string _cpuCache = "—";

    [ObservableProperty]
    private string _ramTotalGb = "0 ГБ";

    [ObservableProperty]
    private string _ramAvailGb = "0 ГБ";

    [ObservableProperty]
    private string _ramUsedGb = "0 ГБ";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RamUsedPercentage))]
    private double _ramUsagePercent;

    public double RamUsedPercentage => RamUsagePercent;

    [ObservableProperty]
    private string _ramSpeed = "0 МГц";

    [ObservableProperty]
    private string _ramTypeModules = "DDR4";

    [ObservableProperty]
    private string _ramChannelMode = "—";

    [ObservableProperty]
    private string _osName = "Windows 11";

    [ObservableProperty]
    private string _osEdition = "Windows 11 Pro";

    [ObservableProperty]
    private string _osBuild = "Сборка 22631 (23H2)";

    [ObservableProperty]
    private string _osGameMode = "Включен (Game Mode ON)";

    [ObservableProperty]
    private bool _isGameModeActive = true;

    [ObservableProperty]
    private string _osPowerPlan = "Сбалансированная";

    [ObservableProperty]
    private string _isLaptopText = "Настольный ПК";

    [ObservableProperty]
    private string _isAcText = "Питание от сети (220V)";

    [ObservableProperty]
    private string _osCoreIsolation = "Отключено";

    [ObservableProperty]
    private bool _isCoreIsolationActive;

    [ObservableProperty]
    private int _gamingProcessesCount;

    public ObservableCollection<DiskDriveInfo> StorageDrives { get; } = new();
    public ObservableCollection<GpuInfo> Gpus { get; } = new();
    public ObservableCollection<string> GamingProcesses { get; } = new();

    // Localized properties
    public string PageTitle => _loc["Scan_Title"];
    public string PageSubtitle => _loc["Scan_Subtitle"];
    public string ScanBtnText => _loc["Scan_Run_Btn"];
    public string CopyBtnText => _loc["Scan_Copy_Btn"];
    public string CardCpuTitle => _loc["Scan_Card_Cpu"];
    public string CardGpuTitle => _loc["Scan_Card_Gpu"];
    public string CardRamTitle => _loc["Scan_Card_Ram"];
    public string CardStorageTitle => _loc["Scan_Card_Storage"];
    public string CardOsTitle => _loc["Scan_Card_Os"];
    public string CardGamingTitle => _loc["Scan_Card_Gaming"];

    public string LblCpuModel => _loc.CurrentLanguage == "ru" ? "Модель процессора" : "Processor Model";
    public string LblTopology => _loc.CurrentLanguage == "ru" ? "Топология" : "Topology";
    public string LblMaxClock => _loc.CurrentLanguage == "ru" ? "Макс. частота" : "Max Clock Speed";
    public string LblVbsSecurity => _loc.CurrentLanguage == "ru" ? "Безопасность VBS" : "VBS Security";

    public string LblGpuVram => _loc.CurrentLanguage == "ru" ? "Видеопамять" : "Video Memory";
    public string LblDriver => _loc.CurrentLanguage == "ru" ? "Версия драйвера" : "Driver Version";
    public string LblHags => _loc.CurrentLanguage == "ru" ? "HAGS Планирование" : "HAGS Scheduling";

    public string LblRamTotal => _loc.CurrentLanguage == "ru" ? "Общий объём" : "Total Capacity";
    public string LblRamAvail => _loc.CurrentLanguage == "ru" ? "Доступно" : "Available Memory";
    public string LblRamSpeed => _loc.CurrentLanguage == "ru" ? "Частота" : "Frequency";
    public string LblRamConfig => _loc.CurrentLanguage == "ru" ? "Конфигурация" : "Configuration";

    public string LblDiskSize => _loc.CurrentLanguage == "ru" ? "Объём" : "Capacity";
    public string LblDiskFree => _loc.CurrentLanguage == "ru" ? "Свободно" : "Free Space";
    public string LblTrimSupport => _loc.CurrentLanguage == "ru" ? "Поддержка TRIM" : "TRIM Support";

    public string LblPowerPlan => _loc.CurrentLanguage == "ru" ? "Схема питания" : "Power Plan";
    public string LblDeviceForm => _loc.CurrentLanguage == "ru" ? "Форм-фактор устройства" : "Device Form Factor";
    public string LblPowerState => _loc.CurrentLanguage == "ru" ? "Состояние батареи / сети" : "Power Source State";
    public string LblCoreIsolation => _loc.CurrentLanguage == "ru" ? "Изоляция ядра (HVCI)" : "Core Isolation (HVCI)";
    public string LblChannelMode => _loc.CurrentLanguage == "ru" ? "Режим каналов RAM" : "Channel Architecture";
    public string LblCpuCache => _loc.CurrentLanguage == "ru" ? "Кэш процессора" : "Processor Cache";
    public string LblDirectX => "DirectX API";
    public string LblOsBuild => _loc.CurrentLanguage == "ru" ? "Номер сборки" : "Build Number";
    public string LblGameMode => _loc.CurrentLanguage == "ru" ? "Игровой режим" : "Windows Game Mode";
    public string LblStorageSubtitle => _loc.CurrentLanguage == "ru" ? "Накопители, емкость и статус TRIM" : "Drives, Capacity & TRIM";
    public string LblCpuSubtitle => _loc.CurrentLanguage == "ru" ? "АРХИТЕКТУРА И ВЫЧИСЛИТЕЛЬНЫЕ ЯДРА" : "ARCHITECTURE & COMPUTE CORES";
    public string LblRamSubtitle => _loc.CurrentLanguage == "ru" ? "СКОРОСТЬ И ЗАПОЛНЕНИЕ ПАМЯТИ" : "SPEED & MEMORY CAPACITY";
    public string LblGpuSubtitle => _loc.CurrentLanguage == "ru" ? "ГРАФИЧЕСКИЙ АДАПТЕР И ДРАЙВЕР" : "GRAPHICS ADAPTER & DRIVER";
    public string LblOsSubtitle => _loc.CurrentLanguage == "ru" ? "ОПЕРАЦИОННАЯ СИСТЕМА И СЛУЖБЫ" : "OPERATING SYSTEM & SERVICES";
    public string LblOsEditionHeader => _loc.CurrentLanguage == "ru" ? "РЕДАКЦИЯ WINDOWS 11" : "WINDOWS 11 EDITION";
    public string LblHvciSubtitle => _loc.CurrentLanguage == "ru" ? "Изоляция ядра (HVCI / VBS)" : "Core Isolation (HVCI / VBS)";
    public string LblGameBarGpuPriority => _loc["Scan_Lbl_GameBarGpuPriority"];
    public string UsedOfTotalLabel => _loc.CurrentLanguage == "ru" ? "занято из" : "used of";

    private CpuInfo? _lastCpu;
    private RamInfo? _lastRam;
    private OsInfo? _lastOs;
    private PowerAndChassisInfo? _lastPower;
    private List<GpuInfo>? _lastGpus;
    private List<DiskDriveInfo>? _lastStorage;

    public PcScannerViewModel(IHardwareScanner hardwareScanner, ILocalizationService localizationService)
    {
        _hardwareScanner = hardwareScanner;
        _loc = localizationService;

        _loc.LanguageChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(PageSubtitle));
            OnPropertyChanged(nameof(ScanBtnText));
            OnPropertyChanged(nameof(CopyBtnText));
            OnPropertyChanged(nameof(CardCpuTitle));
            OnPropertyChanged(nameof(CardGpuTitle));
            OnPropertyChanged(nameof(CardRamTitle));
            OnPropertyChanged(nameof(CardStorageTitle));
            OnPropertyChanged(nameof(CardOsTitle));
            OnPropertyChanged(nameof(CardGamingTitle));
            OnPropertyChanged(nameof(LblCpuModel));
            OnPropertyChanged(nameof(LblTopology));
            OnPropertyChanged(nameof(LblMaxClock));
            OnPropertyChanged(nameof(LblVbsSecurity));
            OnPropertyChanged(nameof(LblGpuVram));
            OnPropertyChanged(nameof(LblDriver));
            OnPropertyChanged(nameof(LblHags));
            OnPropertyChanged(nameof(LblRamTotal));
            OnPropertyChanged(nameof(LblRamAvail));
            OnPropertyChanged(nameof(LblRamSpeed));
            OnPropertyChanged(nameof(LblRamConfig));
            OnPropertyChanged(nameof(LblDiskSize));
            OnPropertyChanged(nameof(LblDiskFree));
            OnPropertyChanged(nameof(LblTrimSupport));
            OnPropertyChanged(nameof(LblPowerPlan));
            OnPropertyChanged(nameof(LblDeviceForm));
            OnPropertyChanged(nameof(LblPowerState));
            OnPropertyChanged(nameof(LblCoreIsolation));
            OnPropertyChanged(nameof(LblChannelMode));
            OnPropertyChanged(nameof(LblCpuCache));
            OnPropertyChanged(nameof(LblDirectX));
            OnPropertyChanged(nameof(LblOsBuild));
            OnPropertyChanged(nameof(LblGameMode));
            OnPropertyChanged(nameof(LblStorageSubtitle));
            OnPropertyChanged(nameof(LblCpuSubtitle));
            OnPropertyChanged(nameof(LblRamSubtitle));
            OnPropertyChanged(nameof(LblGpuSubtitle));
            OnPropertyChanged(nameof(LblOsSubtitle));
            OnPropertyChanged(nameof(LblOsEditionHeader));
            OnPropertyChanged(nameof(LblHvciSubtitle));
            OnPropertyChanged(nameof(LblGameBarGpuPriority));
            OnPropertyChanged(nameof(UsedOfTotalLabel));

            if (_lastCpu != null) ApplyCpuInfo(_lastCpu);
            if (_lastRam != null) ApplyRamInfo(_lastRam);
            if (_lastOs != null && _lastPower != null) ApplyOsAndPowerInfo(_lastOs, _lastPower);
            if (_lastGpus != null) ApplyGpuInfo(_lastGpus);
            if (_lastStorage != null) ApplyStorageInfo(_lastStorage);
        };
    }

    public async Task InitializeAsync()
    {
        // Retain the last displayed snapshot while navigating; the scan button always refreshes.
        if (Profile == null) await ScanAsync();
    }

    [RelayCommand]
    public async Task ScanAsync()
    {
        if (IsScanning) return;
        IsScanning = true;

        try
        {
            // HardwareScanner already scans independent subsystems concurrently. Previously
            // this page repeated that entire scan a second time just to assemble CopyReport.
            var profile = await _hardwareScanner.ScanAsync();
            Profile = profile;
            ApplyCpuInfo(profile.Cpu);
            ApplyRamInfo(profile.Ram);
            ApplyGpuInfo(profile.Gpus);
            ApplyStorageInfo(profile.Storage);
            ApplyOsAndPowerInfo(profile.Os, profile.Power);
            GamingProcesses.Clear();
            foreach (var process in profile.Processes.RunningGamingProcesses) GamingProcesses.Add(process);
            GamingProcessesCount = GamingProcesses.Count;
        }
        catch (Exception ex)
        {
            App.Log($"[PcScanner] General scan error: {ex.Message}");
        }
        finally
        {
            IsScanning = false;
        }
    }

    private void RunOnUI(Action action)
    {
        var dispatcher = App.MainDispatcherQueue ?? App.MainWindowInstance?.DispatcherQueue;
        if (dispatcher != null)
        {
            if (!dispatcher.HasThreadAccess)
            {
                dispatcher.TryEnqueue(() =>
                {
                    try { action(); }
                    catch (Exception ex) { App.Log($"[PcScanner RunOnUI Exception]: {ex}"); }
                });
            }
            else
            {
                try { action(); }
                catch (Exception ex) { App.Log($"[PcScanner RunOnUI Exception]: {ex}"); }
            }
        }
        else
        {
            App.Log("[PcScanner RunOnUI] Warning: DispatcherQueue is null, operation dropped to prevent cross-thread crash.");
        }
    }

    private void ApplyCpuInfo(CpuInfo cpu)
    {
        _lastCpu = cpu;
        RunOnUI(() =>
        {
            CpuName = cpu.Name;
            CpuCoresThreads = _loc.CurrentLanguage == "ru"
                ? $"{cpu.CoreCount} ядер / {cpu.LogicalProcessorCount} потоков"
                : $"{cpu.CoreCount} cores / {cpu.LogicalProcessorCount} threads";
            CpuMaxClock = _loc.CurrentLanguage == "ru"
                ? $"{cpu.MaxClockSpeedGhz:F2} ГГц"
                : $"{cpu.MaxClockSpeedGhz:F2} GHz";
            CpuVbs = cpu.SupportsVbs
                ? (_loc.CurrentLanguage == "ru" ? "Включено (VBS/HVCI)" : "Enabled (VBS/HVCI)")
                : (_loc.CurrentLanguage == "ru" ? "Отключено (Макс. FPS)" : "Disabled (Max FPS)");
            IsCpuVbsActive = cpu.SupportsVbs;
            CpuArch = string.IsNullOrWhiteSpace(cpu.Architecture) ? "x64" : cpu.Architecture;
            CpuCache = cpu.L3CacheMb > 0
                ? (_loc.CurrentLanguage == "ru" ? $"{cpu.L3CacheMb} МБ L3" : $"{cpu.L3CacheMb} MB L3")
                : (_loc.CurrentLanguage == "ru" ? "Нет данных" : "Unknown");

            OnPropertyChanged(nameof(CpuName));
            OnPropertyChanged(nameof(CpuCoresThreads));
            OnPropertyChanged(nameof(CpuMaxClock));
            OnPropertyChanged(nameof(CpuVbs));
            OnPropertyChanged(nameof(IsCpuVbsActive));
            OnPropertyChanged(nameof(CpuArch));
            OnPropertyChanged(nameof(CpuCache));
        });
    }

    private void ApplyRamInfo(RamInfo ram)
    {
        _lastRam = ram;
        RunOnUI(() =>
        {
            string gbUnit = _loc.CurrentLanguage == "ru" ? "ГБ" : "GB";
            string freeText = _loc.CurrentLanguage == "ru" ? "свободно" : "free";
            string usedText = _loc.CurrentLanguage == "ru" ? "занято" : "used";
            string mhzUnit = _loc.CurrentLanguage == "ru" ? "МГц" : "MHz";
            string modulesText = _loc.CurrentLanguage == "ru" ? "модулей" : "modules";

            RamTotalGb = $"{ram.TotalGb:F1} {gbUnit}";
            RamAvailGb = $"{ram.AvailableGb:F1} {gbUnit} {freeText}";
            double usedGb = Math.Max(0, ram.TotalGb - ram.AvailableGb);
            RamUsedGb = $"{usedGb:F1} {gbUnit} {usedText}";
            RamUsagePercent = ram.TotalGb > 0 ? Math.Clamp((usedGb / ram.TotalGb) * 100.0, 0, 100) : 0;
            RamSpeed = $"{ram.SpeedMhz} {mhzUnit}";
            RamTypeModules = $"{ram.MemoryType} ({ram.ModuleCount} {modulesText})";
            // DIMM count does not establish channel topology (four DIMMs can be dual-channel).
            RamChannelMode = _loc.CurrentLanguage == "ru" ? "Нет данных" : "Unknown";

            OnPropertyChanged(nameof(RamTotalGb));
            OnPropertyChanged(nameof(RamAvailGb));
            OnPropertyChanged(nameof(RamUsedGb));
            OnPropertyChanged(nameof(RamUsagePercent));
            OnPropertyChanged(nameof(RamUsedPercentage));
            OnPropertyChanged(nameof(RamSpeed));
            OnPropertyChanged(nameof(RamTypeModules));
            OnPropertyChanged(nameof(RamChannelMode));
        });
    }

    private void ApplyGpuInfo(List<GpuInfo> gpus)
    {
        _lastGpus = gpus;
        RunOnUI(() =>
        {
            Gpus.Clear();
            foreach (var g in gpus) Gpus.Add(g);
            OnPropertyChanged(nameof(Gpus));
        });
    }

    private void ApplyStorageInfo(List<DiskDriveInfo> storage)
    {
        _lastStorage = storage;
        RunOnUI(() =>
        {
            StorageDrives.Clear();
            foreach (var d in storage) StorageDrives.Add(d);
            OnPropertyChanged(nameof(StorageDrives));
        });
    }

    private void ApplyOsAndPowerInfo(OsInfo os, PowerAndChassisInfo power)
    {
        _lastOs = os;
        _lastPower = power;
        RunOnUI(() =>
        {
            OsName = $"{os.ProductName} {os.DisplayVersion}";
            OsEdition = string.IsNullOrWhiteSpace(os.Edition)
                ? os.ProductName
                : $"{os.ProductName} {os.Edition}".Trim();
            OsBuild = _loc.CurrentLanguage == "ru"
                ? $"Сборка {os.BuildNumber} ({os.DisplayVersion})"
                : $"Build {os.BuildNumber} ({os.DisplayVersion})";
            IsGameModeActive = os.GameModeEnabled;
            OsGameMode = os.GameModeEnabled
                ? (_loc.CurrentLanguage == "ru" ? "Включен (Game Mode ON)" : "Active (Game Mode ON)")
                : (_loc.CurrentLanguage == "ru" ? "Отключен" : "Disabled");
            OsPowerPlan = os.ActivePowerPlanName;
            IsLaptopText = power.IsLaptop
                ? (_loc.CurrentLanguage == "ru" ? "Ноутбук / Портативное устройство" : "Laptop / Mobile PC")
                : (_loc.CurrentLanguage == "ru" ? "Настольный компьютер (Desktop)" : "Desktop Computer");
            IsAcText = power.IsAcConnected
                ? (_loc.CurrentLanguage == "ru" ? "Питание от электросети" : "AC Line Power")
                : (_loc.CurrentLanguage == "ru" ? "Работа от аккумулятора (Battery)" : "Battery Power");
            IsCoreIsolationActive = os.CoreIsolationEnabled;
            OsCoreIsolation = os.CoreIsolationEnabled
                ? (_loc.CurrentLanguage == "ru" ? "Включено (VBS HVCI)" : "Active (VBS HVCI)")
                : (_loc.CurrentLanguage == "ru" ? "Отключено (Макс. FPS)" : "Disabled (Low Latency)");

            OnPropertyChanged(nameof(OsName));
            OnPropertyChanged(nameof(OsEdition));
            OnPropertyChanged(nameof(OsBuild));
            OnPropertyChanged(nameof(IsGameModeActive));
            OnPropertyChanged(nameof(OsGameMode));
            OnPropertyChanged(nameof(OsPowerPlan));
            OnPropertyChanged(nameof(IsLaptopText));
            OnPropertyChanged(nameof(IsAcText));
            OnPropertyChanged(nameof(IsCoreIsolationActive));
            OnPropertyChanged(nameof(OsCoreIsolation));
        });
    }

    [RelayCommand]
    public void CopyReport()
    {
        if (Profile == null) return;

        bool isRu = _loc.CurrentLanguage == "ru";
        var sb = new StringBuilder();

        if (isRu)
        {
            sb.AppendLine("==================================================");
            sb.AppendLine("SILKTHREAD — АППАРАТНЫЙ ДИАГНОСТИЧЕСКИЙ ОТЧЕТ ПК");
            sb.AppendLine("==================================================");
            sb.AppendLine($"Дата сканирования: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Платформа: {(Profile.Power.IsLaptop ? "Ноутбук" : "Настольный ПК")}, Питание: {(Profile.Power.IsAcConnected ? "Сеть" : "Батарея")}");
            sb.AppendLine();
            sb.AppendLine($"[ЦП]: {Profile.Cpu.Name}");
            sb.AppendLine($"Ядра / Потоки: {Profile.Cpu.CoreCount} / {Profile.Cpu.LogicalProcessorCount}, Частота: {Profile.Cpu.MaxClockSpeedGhz:F2} ГГц, Кэш: {CpuCache}, Архитектура: {CpuArch}, VBS: {(Profile.Cpu.SupportsVbs ? "Да" : "Нет")}");
            sb.AppendLine();
            sb.AppendLine($"[ОЗУ]: {Profile.Ram.TotalGb:F1} ГБ {Profile.Ram.MemoryType} @ {Profile.Ram.SpeedMhz} МГц ({Profile.Ram.ModuleCount} модулей, {RamChannelMode})");
            sb.AppendLine();
            sb.AppendLine("[ВИДЕОКАРТЫ]:");
            foreach (var gpu in Profile.Gpus)
            {
                sb.AppendLine($" - {gpu.Name} ({gpu.VramGb:F1} ГБ, DirectX: {gpu.DirectXVersion}, Драйвер: {gpu.DriverVersion}, HAGS: {(gpu.HagsEnabled ? "Вкл" : "Выкл")})");
            }
            sb.AppendLine();
            sb.AppendLine("[НАКОПИТЕЛИ]:");
            foreach (var drive in Profile.Storage)
            {
                sb.AppendLine($" - [{drive.DriveLetter}] {drive.Model} ({drive.DriveFormat}): {drive.FreeSpaceGb:F1} ГБ свободно из {drive.TotalSizeGb:F1} ГБ (TRIM: {(drive.SupportsTrim ? "Да" : "Нет")})");
            }
            sb.AppendLine();
            sb.AppendLine($"[ОС]: {OsEdition} (Сборка {Profile.Os.BuildNumber}), Game Mode: {(Profile.Os.GameModeEnabled ? "Вкл" : "Выкл")}, Изоляция ядра: {(Profile.Os.CoreIsolationEnabled ? "Вкл" : "Выкл")}, Схема питания: {Profile.Os.ActivePowerPlanName}");
            sb.AppendLine("==================================================");
        }
        else
        {
            sb.AppendLine("==================================================");
            sb.AppendLine("SILKTHREAD — HARDWARE DIAGNOSTIC REPORT");
            sb.AppendLine("==================================================");
            sb.AppendLine($"Scan Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Platform: {(Profile.Power.IsLaptop ? "Laptop" : "Desktop PC")}, Power Source: {(Profile.Power.IsAcConnected ? "AC Power" : "Battery")}");
            sb.AppendLine();
            sb.AppendLine($"[CPU]: {Profile.Cpu.Name}");
            sb.AppendLine($"Cores / Threads: {Profile.Cpu.CoreCount} / {Profile.Cpu.LogicalProcessorCount}, Frequency: {Profile.Cpu.MaxClockSpeedGhz:F2} GHz, Cache: {CpuCache}, Arch: {CpuArch}, VBS: {(Profile.Cpu.SupportsVbs ? "Yes" : "No")}");
            sb.AppendLine();
            sb.AppendLine($"[RAM]: {Profile.Ram.TotalGb:F1} GB {Profile.Ram.MemoryType} @ {Profile.Ram.SpeedMhz} MHz ({Profile.Ram.ModuleCount} modules, {RamChannelMode})");
            sb.AppendLine();
            sb.AppendLine("[GRAPHICS ADAPTERS]:");
            foreach (var gpu in Profile.Gpus)
            {
                sb.AppendLine($" - {gpu.Name} ({gpu.VramGb:F1} GB, DirectX: {gpu.DirectXVersion}, Driver: {gpu.DriverVersion}, HAGS: {(gpu.HagsEnabled ? "Enabled" : "Disabled")})");
            }
            sb.AppendLine();
            sb.AppendLine("[STORAGE DRIVES]:");
            foreach (var drive in Profile.Storage)
            {
                sb.AppendLine($" - [{drive.DriveLetter}] {drive.Model} ({drive.DriveFormat}): {drive.FreeSpaceGb:F1} GB free of {drive.TotalSizeGb:F1} GB (TRIM: {(drive.SupportsTrim ? "Yes" : "No")})");
            }
            sb.AppendLine();
            sb.AppendLine($"[OS]: {OsEdition} (Build {Profile.Os.BuildNumber}), Game Mode: {(Profile.Os.GameModeEnabled ? "Enabled" : "Disabled")}, Core Isolation: {(Profile.Os.CoreIsolationEnabled ? "Enabled" : "Disabled")}, Power Plan: {Profile.Os.ActivePowerPlanName}");
            sb.AppendLine("==================================================");
        }

        var package = new DataPackage();
        package.SetText(sb.ToString());
        Clipboard.SetContent(package);

        CopyReportSuccessMessage = _loc["Scan_Copied_Toast"];
        HasCopyMessage = true;
    }
}
