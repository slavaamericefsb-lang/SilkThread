using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SilkThread.Core.CompatibilityEngine;
using SilkThread.Core.HardwareDetection;
using SilkThread.Core.Models;
using SilkThread.Core.PresetManager;
using SilkThread.Core.TweakDatabase;
using SilkThread.Core.TweakEngine;
using Xunit;

namespace SilkThread.Tests;

public class PresetManagerTests
{
    private readonly IPresetManager _presetManager;
    private readonly ITweakEngine _tweakEngine;

    public PresetManagerTests()
    {
        var tweaks = TweakCatalog.GetAllTweaks();
        var backup = new SilkThread.Core.BackupManager.BackupManager(":memory:");
        var restore = new SilkThread.Core.RestorePointManager.RestorePointManager();
        var compat = new CompatibilityEngine();
        _tweakEngine = new TweakEngine(tweaks, backup, restore, compat);
        var scanner = new HardwareScanner();
        _presetManager = new PresetManager(_tweakEngine, compat, scanner);
    }

    [Fact]
    public async Task ExportAndImport_ShouldPreservePresetData()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_preset_{Guid.NewGuid():N}.silkpreset");
        try
        {
            var original = new TweakPreset
            {
                Version = "1.0",
                Name = "Ultra Gaming Profile",
                Description = "Disables telemetry, tunes timers and MMCSS",
                Author = "SilkTester",
                CreatedAtUtc = DateTime.UtcNow,
                TweakIds = new List<string> { "cpu_mmcss_network_throttling", "gaming_game_bar_disable", "latency_hpet_disable" }
            };

            await _presetManager.ExportPresetAsync(original, tempFile);
            Assert.True(File.Exists(tempFile));

            var imported = await _presetManager.ImportPresetAsync(tempFile);
            Assert.NotNull(imported);
            Assert.Equal(original.Name, imported.Name);
            Assert.Equal(original.Description, imported.Description);
            Assert.Equal(original.Author, imported.Author);
            Assert.Equal(original.TweakIds.Count, imported.TweakIds.Count);
            Assert.Equal(original.TweakIds, imported.TweakIds);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task EvaluatePreset_IncompatibleTweaks_ShouldBeSortedToVeryBeginning()
    {
        // Create synthetic hardware profile that has Intel CPU, Intel GPU, Laptop, HDD
        var cpu = new CpuInfo("Intel Core i7", "Intel", 8, 16, "x64", 3.8, IsIntel: true, IsAmd: false, true, true);
        var gpu = new GpuInfo("Intel Arc A770", "Intel", 16.0, "31.0.101.4575", "12.2", false, false, true, true, true);
        var ram = new RamInfo(32.0, 24.0, 2, 3200, "DDR4", new List<RamModuleInfo>());
        var storage = new List<DiskDriveInfo>
        {
            new("Drive0", "SATA", "HDD", 1000, 500, "C:", true, false)
        };
        var os = new OsInfo("Windows 11", "24H2", 26100, "Pro", "x64", "Balanced", "GUID", true, true, true);
        var power = new PowerAndChassisInfo(true, true, 80, false, true); // IsLaptop = true
        var processes = new ProcessDiagnosticInfo(100, new List<string>(), new List<string>());
        var restore = new RestorePointStatus(true, true, "Active");

        var hardwareProfile = new HardwareProfile(cpu, new List<GpuInfo> { gpu }, gpu, ram, storage, os, power, processes, restore);

        // Include an NVIDIA-specific tweak (e.g. gpu_nvidia_reflex or similar, or DesktopOnly)
        // Let's create a preset with known tweaks:
        // 1. One that is desktop-only or requires NVIDIA GPU (which our laptop Intel config doesn't have)
        // 2. Compatible tweaks (like windows services / general tweaks)
        var allTweaks = _tweakEngine.GetAllTweaks();
        var nvidiaTweak = allTweaks.FirstOrDefault(t => t.RequiresNvidiaGpu) ?? allTweaks.First(t => t.DesktopOnly);
        var compatibleTweak = allTweaks.FirstOrDefault(t => !t.RequiresNvidiaGpu && !t.RequiresAmdGpu && !t.DesktopOnly && !t.RequiresSsdOrNvme);

        Assert.NotNull(nvidiaTweak);
        Assert.NotNull(compatibleTweak);

        var preset = new TweakPreset
        {
            Name = "Mixed Compatibility Preset",
            // In the preset, we put compatible first, incompatible second:
            TweakIds = new List<string> { compatibleTweak.Id, nvidiaTweak.Id }
        };

        var evaluated = await _presetManager.EvaluatePresetAsync(preset, hardwareProfile);

        Assert.NotNull(evaluated);
        Assert.Equal(2, evaluated.TotalCount);
        Assert.True(evaluated.HasIncompatible);
        Assert.Equal(1, evaluated.IncompatibleCount);
        Assert.Equal(1, evaluated.CompatibleCount);

        // Requirement: "несовместимые с текущим ПК твики сортируются В САМОЕ НАЧАЛО СПИСКА"
        Assert.False(evaluated.Tweaks[0].IsCompatible, "First tweak MUST be the incompatible one!");
        Assert.Equal(nvidiaTweak.Id, evaluated.Tweaks[0].TweakId);
        Assert.False(evaluated.Tweaks[0].IsSelected, "Incompatible tweak must be unselected by default!");
        Assert.False(string.IsNullOrWhiteSpace(evaluated.Tweaks[0].IncompatibilityReason), "Incompatibility reason must be populated!");

        // Second tweak must be compatible and selected by default
        Assert.True(evaluated.Tweaks[1].IsCompatible, "Second tweak MUST be the compatible one!");
        Assert.Equal(compatibleTweak.Id, evaluated.Tweaks[1].TweakId);
        Assert.True(evaluated.Tweaks[1].IsSelected, "Compatible tweak must be selected by default!");
    }
}
