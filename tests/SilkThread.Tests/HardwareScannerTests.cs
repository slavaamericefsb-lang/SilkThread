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
}
