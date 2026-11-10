using System;
using System.Diagnostics;
using System.Threading.Tasks;
using SilkThread.Core.HardwareDetection;
using Xunit;

namespace SilkThread.Tests;

public class HardwareScannerTests
{
    [Fact]
    public async Task ScanAsync_ReturnsValidHardwareProfile()
    {
        var scanner = new HardwareScanner();
        var sw = Stopwatch.StartNew();
        var profile = await scanner.ScanAsync();
        sw.Stop();

        Assert.NotNull(profile);
        Assert.NotNull(profile.Cpu);
        Assert.False(string.IsNullOrWhiteSpace(profile.Cpu.Name));
        Assert.True(profile.Cpu.CoreCount > 0);
        Assert.True(profile.Cpu.MaxClockSpeedGhz > 0);

        Assert.NotNull(profile.Ram);
        Assert.True(profile.Ram.TotalGb > 0);
        Assert.True(profile.Ram.AvailableGb > 0);
        Assert.True(profile.Ram.SpeedMhz > 0);

        Assert.NotNull(profile.Gpus);
        Assert.NotEmpty(profile.Gpus);

        Assert.NotNull(profile.Storage);
        Assert.NotEmpty(profile.Storage);

        Assert.NotNull(profile.Os);
        Assert.False(string.IsNullOrWhiteSpace(profile.Os.ProductName));

        Assert.True(sw.ElapsedMilliseconds < 15000, $"Scan took too long: {sw.ElapsedMilliseconds} ms");
    }

    [Fact]
    public async Task ScanComponentsAsync_ReturnValidDataIndividually()
    {
        var scanner = new HardwareScanner();

        var cpu = await scanner.ScanCpuAsync();
        Assert.NotNull(cpu);
        Assert.False(string.IsNullOrWhiteSpace(cpu.Name));
        Assert.True(cpu.MaxClockSpeedGhz > 0);

        var ram = await scanner.ScanRamAsync();
        Assert.NotNull(ram);
        Assert.True(ram.TotalGb > 0);
        Assert.True(ram.AvailableGb > 0);

        var gpus = await scanner.ScanGpusAsync();
        Assert.NotNull(gpus);
        Assert.NotEmpty(gpus);

        var storage = await scanner.ScanStorageAsync();
        Assert.NotNull(storage);
        Assert.NotEmpty(storage);

        var os = await scanner.ScanOsAsync();
        Assert.NotNull(os);

        var power = await scanner.ScanPowerAsync();
        Assert.NotNull(power);

        var procs = await scanner.ScanProcessesAsync();
        Assert.NotNull(procs);
    }

    [Theory]
    [InlineData("XPG GAMMIX S11 Pro", 17u, (ushort)4, 0ul, "M2 nvme")]
    [InlineData("Samsung SSD 990 PRO 2TB", 0u, (ushort)4, 0ul, "M2 nvme")]
    [InlineData("WD_BLACK SN850X 1TB", 0u, (ushort)0, 0ul, "M2 nvme")]
    [InlineData("Kingston NV2 1TB", 0u, (ushort)0, 0ul, "M2 nvme")]
    [InlineData("PCIe NVMe SSD", 0u, (ushort)0, 0ul, "M2 nvme")]
    [InlineData("KINGSTON SA400S37480G", 11u, (ushort)4, 0ul, "SSD")]
    [InlineData("Crucial CT1000MX500SSD1", 11u, (ushort)4, 0ul, "SSD")]
    [InlineData("Generic SSD Drive", 11u, (ushort)0, 0ul, "SSD")]
    [InlineData("ST500DM002-1BD142", 11u, (ushort)3, 4294967295ul, "HDD")]
    [InlineData("WDC WD10EZEX-00WN4A0", 11u, (ushort)0, 7200ul, "HDD")]
    [InlineData("Toshiba DT01ACA100", 11u, (ushort)0, 0ul, "HDD")]
    public void DetermineDriveFormat_ClassifiesCorrectly(string model, uint busType, ushort mediaType, ulong spindleSpeed, string expected)
    {
        string actual = HardwareScanner.DetermineDriveFormat(model, busType, mediaType, spindleSpeed);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DiskDriveInfo_DriveFormat_ReturnsExpectedValues()
    {
        var nvme = new SilkThread.Core.Models.DiskDriveInfo("XPG GAMMIX S11 Pro", "PCIe / NVMe", "NVMe", 1000, 500, "C:", true, true);
        Assert.Equal("M2 nvme", nvme.DriveFormat);

        var ssd = new SilkThread.Core.Models.DiskDriveInfo("KINGSTON SA400S37480G", "SATA", "SSD", 480, 200, "D:", false, true);
        Assert.Equal("SSD", ssd.DriveFormat);

        var hdd = new SilkThread.Core.Models.DiskDriveInfo("ST500DM002-1BD142", "SATA", "HDD", 500, 300, "E:", false, false);
        Assert.Equal("HDD", hdd.DriveFormat);

        var overridden = new SilkThread.Core.Models.DiskDriveInfo("Custom Drive", "SATA", "Unknown", 500, 300, "F:", false, true, "M2 nvme");
        Assert.Equal("M2 nvme", overridden.DriveFormat);
    }

    [Theory]
    [InlineData("32.0.15.6094", true, "560.94")]
    [InlineData("32.0.16.1714", true, "617.14")]
    [InlineData("31.0.15.3623", true, "536.23")]
    [InlineData("30.0.14.7212", true, "472.12")]
    [InlineData("560.94", true, "560.94")]
    [InlineData("24.10.1", false, "24.10.1")]
    [InlineData("31.0.101.5333", false, "31.0.101.5333")]
    public void ParseDriverVersion_FormatsNicely(string raw, bool isNvidia, string expected)
    {
        string parsed = HardwareScanner.ParseDriverVersion(raw, isNvidia);
        Assert.Equal(expected, parsed);
    }

    [Fact]
    public void GpuInfo_FormattedDriverVersion_FormatsNicely()
    {
        var gpu = new SilkThread.Core.Models.GpuInfo(
            "NVIDIA GeForce RTX 4060 Ti",
            "NVIDIA",
            16.0,
            "560.94",
            "DirectX 12 Ultimate",
            true, false, false, true, true
        );

        Assert.Equal("560.94", gpu.DriverVersion);
        Assert.Equal("Драйвер: 560.94", gpu.FormattedDriverVersion);
    }

    [Fact]
    public async Task ScanStorageAsync_DetectsSystemDriveFormatAccurately()
    {
        var scanner = new HardwareScanner();
        var drives = await scanner.ScanStorageAsync();

        Assert.NotEmpty(drives);
        var sysDrive = drives.FirstOrDefault(d => d.IsSystemDrive);
        Assert.NotNull(sysDrive);
        Assert.False(string.IsNullOrWhiteSpace(sysDrive.DriveFormat));
        Assert.Contains(sysDrive.DriveFormat, new[] { "M2 nvme", "SSD", "HDD" });
    }
}
