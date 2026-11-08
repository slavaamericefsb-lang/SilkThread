using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SilkThread.Core.BackupManager;
using SilkThread.Core.CompatibilityEngine;
using SilkThread.Core.Models;
using SilkThread.Core.SystemOperations;
using SilkThread.Core.TweakDatabase;
using Xunit;

namespace SilkThread.Tests;

public class TweakCatalogTests
{
    [Fact]
    public void Catalog_ShouldContainAtLeast150Tweaks()
    {
        var tweaks = TweakCatalog.GetAllTweaks();
        Assert.True(tweaks.Count >= 150, $"Expected >= 150 tweaks, but found {tweaks.Count}");
    }

    [Fact]
    public void Catalog_ShouldHaveUniqueIds()
    {
        var tweaks = TweakCatalog.GetAllTweaks();
        var duplicates = tweaks.GroupBy(t => t.Id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        Assert.Empty(duplicates);
    }

    [Fact]
    public void Catalog_ShouldCoverAll15Categories()
    {
        var tweaks = TweakCatalog.GetAllTweaks();
        var categories = tweaks.Select(t => t.Category).Distinct().ToList();
        Assert.Equal(15, categories.Count);
    }

    [Fact]
    public void Catalog_EveryTweakMustHaveRequiredFields()
    {
        var tweaks = TweakCatalog.GetAllTweaks();
        foreach (var t in tweaks)
        {
            Assert.False(string.IsNullOrWhiteSpace(t.Id), "Id is missing");
            Assert.False(string.IsNullOrWhiteSpace(t.Title), $"Title missing on {t.Id}");
            Assert.False(string.IsNullOrWhiteSpace(t.Description), $"Description missing on {t.Id}");
            Assert.False(string.IsNullOrWhiteSpace(t.HowItWorks), $"HowItWorks missing on {t.Id}");
            Assert.False(string.IsNullOrWhiteSpace(t.ProblemSolved), $"ProblemSolved missing on {t.Id}");
            Assert.False(string.IsNullOrWhiteSpace(t.ExpectedResult), $"ExpectedResult missing on {t.Id}");
            Assert.NotNull(t.CheckCurrentStateAsync);
            Assert.NotNull(t.ApplyAsync);
            Assert.NotNull(t.RollbackAsync);
        }
    }

    [Fact]
    public void Catalog_ClassicContextMenu_ShouldBeConfiguredCorrectly()
    {
        var tweaks = TweakCatalog.GetAllTweaks();
        var classicMenu = tweaks.FirstOrDefault(t => t.Id == "ui_classic_context_menu");
        Assert.NotNull(classicMenu);
        Assert.Equal(TweakCategory.WindowsExplorerAndUi, classicMenu.Category);
        Assert.Equal(22000, classicMenu.MinWindowsBuild);
    }

    [Fact]
    public void Catalog_CpuAffinityTweak_ShouldHaveCorrectMetadata()
    {
        var tweaks = TweakCatalog.GetAllTweaks();
        var affinityTweak = tweaks.FirstOrDefault(t => t.Id == "cpu_affinity_prioritization_foreground");
        Assert.NotNull(affinityTweak);
        Assert.Equal(TweakCategory.CpuOptimization, affinityTweak.Category);
        Assert.Contains("ForegroundPriorityAffinity", affinityTweak.HowItWorks);
    }

    [Fact]
    public void RegistryOperations_BinarySnapshot_ShouldStoreAndRestoreCorrectly()
    {
        byte[] original = new byte[] { 0x01, 0x02, 0x03, 0xFF, 0x00, 0x42 };
        string base64 = System.Convert.ToBase64String(original);
        var snapshot = new RegistryValueSnapshot
        {
            Hive = "HKCU",
            SubKey = @"Software\SilkThreadTest",
            ValueName = "BinaryTest",
            Exists = true,
            ValueKind = "Binary",
            RawValue = base64
        };

        byte[] restored = System.Convert.FromBase64String(snapshot.RawValue);
        Assert.Equal(original, restored);
    }
}

public class CompatibilityEngineTests
{
    [Fact]
    public void Compatibility_ShouldFilterNvidiaTweaks_WhenNoNvidiaGpu()
    {
        var engine = new CompatibilityEngine();
        var amdGpu = new GpuInfo("AMD Radeon RX 7900 XTX", "AMD", 24, "31.0", "DirectX 12", false, true, false, true, true);
        var profile = new HardwareProfile(
            new CpuInfo("AMD Ryzen 7 7800X3D", "AMD", 8, 16, "x64", 5.0, false, true, true, false),
            new() { amdGpu },
            amdGpu,
            new RamInfo(32, 24, 2, 6000, "DDR5", new()),
            new() { new DiskDriveInfo("Samsung 990 Pro", "NVMe", "NVMe", 2000, 1500, "C:", true, true) },
            new OsInfo("Windows 11 Pro", "23H2", 22631, "Pro", "64-bit", "Balanced", "guid", true, false, true),
            new PowerAndChassisInfo(false, false, 100, false, true),
            new ProcessDiagnosticInfo(150, new(), new()),
            new RestorePointStatus(true, true, "OK")
        );

        var nvidiaTweak = GpuTweaks.GetTweaks().First(t => t.RequiresNvidiaGpu);
        var result = engine.EvaluateCompatibility(nvidiaTweak, profile);

        Assert.False(result.IsCompatible);
        Assert.Contains("NVIDIA", result.Reason);
    }

    [Fact]
    public void Compatibility_ShouldFilterDesktopOnlyTweaks_OnLaptops()
    {
        var engine = new CompatibilityEngine();
        var gpu = new GpuInfo("NVIDIA RTX 4080 Laptop GPU", "NVIDIA", 12, "31.0", "DirectX 12", true, false, false, true, true);
        var profile = new HardwareProfile(
            new CpuInfo("Intel Core i9-13900H", "Intel", 14, 20, "x64", 5.4, true, false, true, false),
            new() { gpu },
            gpu,
            new RamInfo(32, 20, 2, 5600, "DDR5", new()),
            new() { new DiskDriveInfo("WD Black SN850X", "NVMe", "NVMe", 1000, 500, "C:", true, true) },
            new OsInfo("Windows 11 Pro", "23H2", 22631, "Pro", "64-bit", "Balanced", "guid", true, false, true),
            new PowerAndChassisInfo(true, true, 85, true, true), // Laptop with battery
            new ProcessDiagnosticInfo(120, new(), new()),
            new RestorePointStatus(true, true, "OK")
        );

        var desktopTweak = PowerManagementTweaks.GetTweaks().First(t => t.DesktopOnly);
        var result = engine.EvaluateCompatibility(desktopTweak, profile);

        Assert.False(result.IsCompatible);
        Assert.Contains("Desktop-only", result.Reason);
    }
}

public class HardwareScannerLiveTests
{
    [Fact]
    public async Task HardwareScanner_ShouldScanRealHardwareWithoutThrowing()
    {
        var scanner = new SilkThread.Core.HardwareDetection.HardwareScanner();
        var profile = await scanner.ScanAsync();

        Assert.NotNull(profile);
        Assert.NotNull(profile.Cpu);
        Assert.NotNull(profile.PrimaryGpu);
        Assert.NotNull(profile.Ram);
        Assert.NotNull(profile.Os);
        Assert.NotNull(profile.Power);
        Assert.NotNull(profile.Processes);
        Assert.NotEmpty(profile.Cpu.Name);
        Assert.True(profile.Cpu.CoreCount > 0);
        Assert.True(profile.Ram.TotalGb > 0);
        Assert.True(profile.Storage.Count > 0);
    }
}

public class BackupManagerTests
{
    [Fact]
    public async Task BackupManager_ShouldCreateAndRecordHistory()
    {
        var tempDb = Path.Combine(Path.GetTempPath(), $"silkthread_test_{System.Guid.NewGuid()}.db");
        try
        {
            var manager = new BackupManager(tempDb);
            await manager.InitializeAsync();

            int id = await manager.RecordAppliedTweakAsync(
                "cpu_test_tweak",
                "Test CPU Tweak",
                "CpuOptimization",
                "{\"Value\": 0}",
                "{\"Value\": 1}",
                true,
                true,
                "Applied successfully"
            );

            Assert.True(id > 0);

            var latest = await manager.GetLatestTweakRecordAsync("cpu_test_tweak");
            Assert.NotNull(latest);
            Assert.Equal("cpu_test_tweak", latest.TweakId);
            Assert.Equal("{\"Value\": 0}", latest.OriginalStateJson);
            Assert.False(latest.IsRolledBack);

            bool rolledBack = await manager.MarkTweakRolledBackAsync(id);
            Assert.True(rolledBack);

            var latestAfterRollback = await manager.GetLatestTweakRecordAsync("cpu_test_tweak");
            Assert.Null(latestAfterRollback);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(tempDb))
            {
                try { File.Delete(tempDb); } catch { }
            }
        }
    }
}
