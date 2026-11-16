using SilkThread.Core.SystemOperations;
using Xunit;

namespace SilkThread.Tests;

public class SystemDiagnosticsTests
{
    private static byte[] Cpu(uint id, ushort group, byte logical, byte core, byte efficiency, int size = 32)
    {
        var bytes = new byte[size]; BitConverter.GetBytes(size).CopyTo(bytes, 0); BitConverter.GetBytes(id).CopyTo(bytes, 8);
        BitConverter.GetBytes(group).CopyTo(bytes, 12); bytes[14] = logical; bytes[15] = core; bytes[18] = efficiency; return bytes;
    }
    [Fact]
    public void TopologyPreservesGroupRelativeCoreIdsAndEfficiencyClasses()
    {
        var buffer = Cpu(1, 0, 0, 0, 8).Concat(Cpu(2, 0, 1, 0, 8, 40)).Concat(Cpu(3, 1, 0, 0, 0)).ToArray();
        var topology = new CpuTopology(SystemDiagnosticsService.ParseCpuSets(buffer), new[] { new ProcessorGroupInfo(0, 2), new ProcessorGroupInfo(1, 1) });
        Assert.Equal(2, topology.VisibleCores); Assert.True(topology.SmtVisible); Assert.True(topology.IsComplete);
        Assert.Equal(new byte[] { 8, 8, 0 }, topology.CpuSets.Select(c => c.EfficiencyClass));
    }
    [Fact]
    public void IncompleteTopologyDoesNotClaimSmtDisabled()
    {
        var topology = new CpuTopology(SystemDiagnosticsService.ParseCpuSets(Cpu(1, 0, 0, 0, 0)), new[] { new ProcessorGroupInfo(0, 4) });
        Assert.False(topology.IsComplete); Assert.Null(topology.SmtVisible);
    }
    [Theory]
    [InlineData(0)] [InlineData(4)] [InlineData(33)]
    public void MalformedVariableRecordSizesAreRejected(int size)
    {
        var buffer = Cpu(1, 0, 0, 0, 0); BitConverter.GetBytes(size).CopyTo(buffer, 0);
        Assert.Throws<InvalidDataException>(() => SystemDiagnosticsService.ParseCpuSets(buffer));
    }
    [Fact]
    public void UnknownRecordTypesAreSkippedButDuplicateProcessorsAreRejected()
    {
        var unknown = new byte[8]; BitConverter.GetBytes(8).CopyTo(unknown, 0); BitConverter.GetBytes(99).CopyTo(unknown, 4);
        Assert.Single(SystemDiagnosticsService.ParseCpuSets(unknown.Concat(Cpu(1, 0, 0, 0, 0)).ToArray()));
        Assert.Throws<InvalidDataException>(() => SystemDiagnosticsService.ParseCpuSets(Cpu(1, 0, 0, 0, 0).Concat(Cpu(2, 0, 0, 0, 0)).ToArray()));
    }
    [Fact]
    public void BcdParserDistinguishesExplicitFalseFromAbsentAndIgnoresDescriptions()
    {
        var options = SystemDiagnosticsService.ParseBcd("identifier {id}\r\nuseplatformclock No\r\nDescription useplatformtick Yes\r\ndisabledynamictick Yes\r\ntscsyncpolicy Enhanced\r\n");
        Assert.Equal("No", options["useplatformclock"]); Assert.Null(options["useplatformtick"]);
        Assert.Equal("Yes", options["disabledynamictick"]); Assert.Equal("Enhanced", options["tscsyncpolicy"]);
    }
    [Fact]
    public void BcdParserDoesNotInventDefaultsOrAcceptDuplicateEntries()
    {
        Assert.All(SystemDiagnosticsService.ParseBcd("").Values, value => Assert.Null(value));
        Assert.Throws<InvalidDataException>(() => SystemDiagnosticsService.ParseBcd("useplatformclock Yes\nuseplatformclock No"));
    }
    [Fact]
    public void UnknownPowerDataStaysUnknown()
    {
        var power = SystemDiagnosticsService.DecodePower(255, 255, 255);
        Assert.Null(power.OnAc); Assert.Null(power.HasBattery); Assert.Null(power.BatteryPercent); Assert.Null(power.Charging);
    }
    [Fact]
    public void LaptopAndDesktopPowerAreDifferent()
    {
        var laptop = SystemDiagnosticsService.DecodePower(0, 8, 44);
        Assert.False(laptop.OnAc); Assert.True(laptop.HasBattery); Assert.Equal((byte)44, laptop.BatteryPercent); Assert.True(laptop.Charging);
        var desktop = SystemDiagnosticsService.DecodePower(1, 128, 255);
        Assert.True(desktop.OnAc); Assert.False(desktop.HasBattery); Assert.Null(desktop.BatteryPercent); Assert.Null(desktop.Charging);
    }
    [Fact]
    public void ActivePageFileVerificationChecksSizesPathsAndTemporaryFiles()
    {
        var plan = new PageFilePlan(false, new[] { new PageFileSetting(@"C:\pagefile.sys", 1024, 2048) });
        Assert.True(PageFileService.ActiveMatches(plan, new[] { new PageFileUsage(@"c:\PAGEFILE.sys", 1536, 0, 0, false) }));
        Assert.False(PageFileService.ActiveMatches(plan, new[] { new PageFileUsage(@"C:\pagefile.sys", 512, 0, 0, false) }));
        Assert.False(PageFileService.ActiveMatches(plan, new[] { new PageFileUsage(@"C:\pagefile.sys", 1536, 0, 0, true) }));
        Assert.False(PageFileService.ActiveMatches(plan, null));
    }
}
