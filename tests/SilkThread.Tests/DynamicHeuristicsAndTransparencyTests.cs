using System;
using System.Collections.Generic;
using System.Linq;
using SilkThread.App.Services;
using SilkThread.App.ViewModels;
using SilkThread.Core.HardwareDetection;
using SilkThread.Core.Models;
using SilkThread.Core.PresetManager;
using SilkThread.Core.TweakDatabase;
using Xunit;

namespace SilkThread.Tests;

public class DynamicHeuristicsAndTransparencyTests
{
    private readonly HardwareProfile _sampleHardware;
    private readonly IReadOnlyList<TweakDefinition> _allTweaks;

    public DynamicHeuristicsAndTransparencyTests()
    {
        _allTweaks = TweakCatalog.GetAllTweaks();

        var cpu = new CpuInfo(
            Name: "AMD Ryzen 7 7800X3D 8-Core Processor",
            Manufacturer: "Advanced Micro Devices",
            CoreCount: 8,
            LogicalProcessorCount: 16,
            Architecture: "x64",
            MaxClockSpeedGhz: 5.05,
            IsIntel: false,
            IsAmd: true,
            SupportsVirtualization: true,
            SupportsVbs: true
        );

        var gpu = new GpuInfo(
            Name: "NVIDIA GeForce RTX 4080",
            Manufacturer: "NVIDIA",
            VramGb: 16.0,
            DriverVersion: "560.94",
            DirectXVersion: "12.2",
            IsNvidia: true,
            IsAmd: false,
            IsIntel: false,
            SupportsHags: true,
            HagsEnabled: true
        );

        var ram = new RamInfo(
            TotalGb: 32.0,
            AvailableGb: 24.5,
            ModuleCount: 2,
            SpeedMhz: 6000,
            MemoryType: "DDR5",
            Modules: new List<RamModuleInfo>()
        );

        var storage = new List<DiskDriveInfo>
        {
            new("Samsung 990 PRO 2TB", "NVMe", "NVMe SSD", 2000, 1200, "C:", true, true)
        };

        var os = new OsInfo(
            ProductName: "Windows 11 Pro",
            DisplayVersion: "24H2",
            BuildNumber: 26100,
            Edition: "Pro",
            Architecture: "x64",
            ActivePowerPlanName: "Ultimate Performance",
            ActivePowerPlanGuid: "{e9a42b02-d5df-448d-aa00-03f14749eb61}",
            GameModeEnabled: true,
            CoreIsolationEnabled: false,
            IsWindows11: true
        );

        var power = new PowerAndChassisInfo(
            IsLaptop: false,
            HasBattery: false,
            BatteryPercent: 100,
            IsCharging: false,
            IsAcConnected: true
        );

        _sampleHardware = new HardwareProfile(
            cpu,
            new List<GpuInfo> { gpu },
            gpu,
            ram,
            storage,
            os,
            power,
            new ProcessDiagnosticInfo(100, new List<string>(), new List<string>()),
            new RestorePointStatus(true, true, "Ready")
        );
    }

    [Fact]
    public void GenerateExpertProfile_ProducesVaryingTweakCounts_AcrossScenariosAndPriorities()
    {
        var service = new LiquidAiOptimizerService();

        // Scenario 0: Esports, Priority 0: Lowest Latency
        var esportsLowestLatency = service.GenerateExpertProfile(
            _sampleHardware, 0, 1, 0, "Esports", "Safe", "Lowest Latency", _allTweaks, isRu: true);

        // Scenario 1: AAA Games, Priority 1: Maximum FPS
        var aaaMaxFps = service.GenerateExpertProfile(
            _sampleHardware, 1, 1, 1, "AAA", "Safe", "Max FPS", _allTweaks, isRu: true);

        // Scenario 2: Streaming & OBS, Priority 2: Smooth Frametime
        var streamFrametime = service.GenerateExpertProfile(
            _sampleHardware, 2, 1, 2, "Streaming", "Safe", "Smooth Frametime", _allTweaks, isRu: true);

        // Scenario 3: Balanced, Priority 3: RAM Clean
        var balancedRamClean = service.GenerateExpertProfile(
            _sampleHardware, 3, 1, 3, "Balanced", "Safe", "Clean RAM", _allTweaks, isRu: true);

        // Assert counts are dynamic and not a static fixed number
        var counts = new HashSet<int>
        {
            esportsLowestLatency.Selections.Count,
            aaaMaxFps.Selections.Count,
            streamFrametime.Selections.Count,
            balancedRamClean.Selections.Count
        };

        // There must be multiple distinct counts across the 4 distinct scenarios/priorities
        Assert.True(counts.Count >= 3, $"Expected at least 3 distinct counts across scenarios, but got: {string.Join(", ", counts)}");
    }

    [Fact]
    public void GenerateExpertProfile_SetsTransparencyMetadata_ForLocalIOSTExpert()
    {
        var service = new LiquidAiOptimizerService();

        var result = service.GenerateExpertProfile(
            _sampleHardware, 0, 1, 0, "Киберспорт", "Сбалансированный", "Минимальная задержка", _allTweaks, isRu: true);

        Assert.Equal("Локальный эксперт IOST", result.GenerationSource);
        Assert.False(result.UsedOnlineModel);
        Assert.False(result.IsOnlineAiGenerated);
        Assert.Contains("IOST", result.ModelName);
        Assert.NotEmpty(result.Selections);
    }

    [Fact]
    public void GenerateExpertProfile_Scenario0_PrioritizesInputNetworkAndLatency()
    {
        var service = new LiquidAiOptimizerService();

        var result = service.GenerateExpertProfile(
            _sampleHardware, 0, 1, 0, "Esports", "Medium", "Lowest Latency", _allTweaks, isRu: true);

        var selectedIds = result.Selections.Select(s => s.TweakId).ToHashSet();

        // Must include key esports input & network tweaks
        Assert.Contains("input_mouse_acceleration_disable", selectedIds);
        Assert.Contains("net_tcp_nagle_algorithm_disable", selectedIds);
        Assert.Contains("net_rsc_receive_segment_coalescing_disable", selectedIds);
    }

    [Fact]
    public void GenerateExpertProfile_Scenario1_PrioritizesGpuHagsAndCoreUnparking()
    {
        var service = new LiquidAiOptimizerService();

        var result = service.GenerateExpertProfile(
            _sampleHardware, 1, 1, 1, "AAA", "Medium", "Max FPS", _allTweaks, isRu: true);

        var selectedIds = result.Selections.Select(s => s.TweakId).ToHashSet();

        // Must include key AAA / FPS tweaks
        Assert.Contains("gpu_hags_enable", selectedIds);
        Assert.Contains("cpu_core_parking_unhide", selectedIds);
        Assert.Contains("gaming_game_mode_enable", selectedIds);
    }

    [Fact]
    public void GenerateExpertProfile_Scenario2_PrioritizesAudioLatencyAndGameDVR()
    {
        var service = new LiquidAiOptimizerService();

        var result = service.GenerateExpertProfile(
            _sampleHardware, 2, 1, 2, "Streaming", "Medium", "Smooth Frametime", _allTweaks, isRu: true);

        var selectedIds = result.Selections.Select(s => s.TweakId).ToHashSet();

        // Must include key OBS / streaming / audio tweaks
        Assert.Contains("cpu_mmcss_game_tasks", selectedIds);
        Assert.Contains("gaming_game_dvr_disable", selectedIds);
        Assert.Contains("gaming_audio_ducker_disable", selectedIds);
    }

    [Fact]
    public void GenerateExpertProfile_Priority3_PrioritizesRamCleanAndDebloat()
    {
        var service = new LiquidAiOptimizerService();

        var result = service.GenerateExpertProfile(
            _sampleHardware, 3, 1, 3, "Balanced", "Medium", "RAM Clean", _allTweaks, isRu: true);

        var selectedIds = result.Selections.Select(s => s.TweakId).ToHashSet();

        // Must include memory clean tweaks
        Assert.Contains("bg_edge_prelaunch_disable", selectedIds);
        Assert.Contains("bg_copilot_runtime_disable", selectedIds);
        Assert.Contains("bg_widgets_news_interests_disable", selectedIds);
    }

    [Fact]
    public void GenerateExpertProfile_ScalesByRiskIndex_AcrossAllModes()
    {
        var service = new LiquidAiOptimizerService();

        // Safe mode (Risk 0): ~60–80 tweaks
        var safeProfile = service.GenerateExpertProfile(
            _sampleHardware, 0, 0, 0, "Esports", "Safe", "Lowest Latency", _allTweaks, isRu: true);

        // Balanced mode (Risk 1): ~85–120 tweaks
        var balancedProfile = service.GenerateExpertProfile(
            _sampleHardware, 1, 1, 1, "AAA", "Balanced", "Max FPS", _allTweaks, isRu: true);

        // Maximum mode (Risk 2): ~120–160+ tweaks
        var maxProfile = service.GenerateExpertProfile(
            _sampleHardware, 1, 2, 1, "AAA", "Maximum", "Max FPS", _allTweaks, isRu: true);

        Assert.InRange(safeProfile.Selections.Count, 60, 80);
        Assert.InRange(balancedProfile.Selections.Count, 85, 120);
        Assert.True(maxProfile.Selections.Count >= 120 && maxProfile.Selections.Count <= 165,
            $"Expected 120–165 tweaks in Maximum mode, got {maxProfile.Selections.Count}");

        // In Safe mode, all selected tweaks must be Low risk
        var tweakMap = _allTweaks.ToDictionary(t => t.Id);
        foreach (var sel in safeProfile.Selections)
        {
            Assert.Equal(RiskLevel.Low, tweakMap[sel.TweakId].Risk);
        }
    }

    [Fact]
    public void GenerateExpertProfile_DeepCategoryCoverage_InMaximumMode()
    {
        var service = new LiquidAiOptimizerService();
        var maxProfile = service.GenerateExpertProfile(
            _sampleHardware, 1, 2, 1, "AAA", "Maximum", "Max FPS", _allTweaks, isRu: true);

        var tweakMap = _allTweaks.ToDictionary(t => t.Id);
        var selectedTweaks = maxProfile.Selections.Select(s => tweakMap[s.TweakId]).ToList();

        int cpuTweaks = selectedTweaks.Count(t => t.Category == TweakCategory.CpuOptimization || t.Category == TweakCategory.AdvancedTweaks && t.Id.StartsWith("adv_"));
        int gpuTweaks = selectedTweaks.Count(t => t.Category == TweakCategory.GpuOptimization);
        int ramTweaks = selectedTweaks.Count(t => t.Category == TweakCategory.RamOptimization);
        int netTweaks = selectedTweaks.Count(t => t.Category == TweakCategory.NetworkOptimization);
        int inputTweaks = selectedTweaks.Count(t => t.Category == TweakCategory.InputAndLatency);
        int gamingTweaks = selectedTweaks.Count(t => t.Category == TweakCategory.GamingPerformance);
        int servicesTweaks = selectedTweaks.Count(t => t.Category == TweakCategory.WindowsServices);
        int telemetryTweaks = selectedTweaks.Count(t => t.Category == TweakCategory.SecurityAndPrivacy || t.Category == TweakCategory.BackgroundProcesses);
        int storageTweaks = selectedTweaks.Count(t => t.Category == TweakCategory.StorageOptimization);
        int powerTweaks = selectedTweaks.Count(t => t.Category == TweakCategory.PowerManagement || t.Category == TweakCategory.SystemMaintenance);

        Assert.True(cpuTweaks >= 10, $"Expected >= 10 CPU tweaks, got {cpuTweaks}");
        Assert.True(gpuTweaks >= 10, $"Expected >= 10 GPU tweaks, got {gpuTweaks}");
        Assert.True(ramTweaks >= 8, $"Expected >= 8 RAM tweaks, got {ramTweaks}");
        Assert.True(netTweaks >= 12, $"Expected >= 12 Net tweaks, got {netTweaks}");
        Assert.True(inputTweaks >= 10, $"Expected >= 10 Input tweaks, got {inputTweaks}");
        Assert.True(gamingTweaks >= 12, $"Expected >= 12 Gaming tweaks, got {gamingTweaks}");
        Assert.True(servicesTweaks >= 10, $"Expected >= 10 Services tweaks, got {servicesTweaks}");
        Assert.True(telemetryTweaks >= 15, $"Expected >= 15 Telemetry tweaks, got {telemetryTweaks}");
        Assert.True(storageTweaks >= 8, $"Expected >= 8 Storage tweaks, got {storageTweaks}");
        Assert.True(powerTweaks >= 8, $"Expected >= 8 Power/Maint tweaks, got {powerTweaks}");
    }

    [Fact]
    public void PresetReviewViewModel_OnlineModelBadge_ConfiguredCorrectly()
    {
        var preset = new TweakPreset
        {
            Name = "AI: Esports (Lowest Latency)",
            Author = "SilkThread IOST",
            GenerationSource = "Нейросеть OpenRouter (google/gemini-2.5-flash)",
            UsedOnlineModel = true,
            IsOnlineAiGenerated = true,
            TweakIds = new List<string> { "input_mouse_acceleration_disable" }
        };

        var evaluated = new EvaluatedPreset
        {
            Preset = preset,
            Tweaks = new List<PresetTweakEvaluation>()
        };

        var vm = new PresetReviewViewModel(evaluated, null!);

        Assert.True(vm.HasSourceBadge);
        Assert.True(vm.UsedOnlineModel);
        Assert.Equal("Нейросеть: google/gemini-2.5-flash", vm.SourceBadgeText);
        Assert.Equal("#806BFF", vm.SourceBadgeBorderHex);
        Assert.Equal("#202431", vm.SourceBadgeBgHex);
        Assert.Equal("#806BFF", vm.SourceBadgeFgHex);
        Assert.Equal("\uE945", vm.SourceBadgeIcon);
    }

    [Fact]
    public void PresetReviewViewModel_OfflineExpertBadge_ConfiguredCorrectly()
    {
        var preset = new TweakPreset
        {
            Name = "AI: Heavy AAA Games",
            Author = "SilkThread IOST",
            GenerationSource = "Локальный эксперт IOST",
            UsedOnlineModel = false,
            IsOnlineAiGenerated = false,
            TweakIds = new List<string> { "gpu_hags_enable" }
        };

        var evaluated = new EvaluatedPreset
        {
            Preset = preset,
            Tweaks = new List<PresetTweakEvaluation>()
        };

        var vm = new PresetReviewViewModel(evaluated, null!);

        Assert.True(vm.HasSourceBadge);
        Assert.False(vm.UsedOnlineModel);
        Assert.Equal("Локальный эксперт IOST", vm.SourceBadgeText);
        Assert.Equal("#969CAB", vm.SourceBadgeBorderHex);
        Assert.Equal("#202431", vm.SourceBadgeBgHex);
        Assert.Equal("#969CAB", vm.SourceBadgeFgHex);
        Assert.Equal("\uE9D9", vm.SourceBadgeIcon);
    }
}
