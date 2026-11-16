using System.ComponentModel;
using SilkThread.Core.SystemOperations;
using Xunit;

namespace SilkThread.Tests;

public class MemoryConfigurationReaderTests
{
    private const ulong GiB = 1024UL * 1024 * 1024;
    private sealed class Source : IMemoryReadSource
    {
        public MemoryCounters Counters = new(32 * GiB, 8 * GiB, 40 * GiB, 64 * GiB, 48 * GiB);
        public bool Automatic;
        public IReadOnlyList<PageFileSetting> Configured = new PageFileSetting[] { new(@"C:\pagefile.sys", 1024, 4096), new(@"D:\pagefile.sys", 0, 0) };
        public IReadOnlyList<PageFileUsage> Active = new PageFileUsage[] { new(@"c:\PAGEFILE.sys", 2048, 0, 15, false), new(@"D:\pagefile.sys", 1024, 1, 10, false) };
        public CrashDumpConfiguration Dump = new(1, null, null, null, @"%SystemRoot%\MEMORY.DMP");
        public Dictionary<string, Exception> Errors { get; } = new();
        public int Calls;
        private T Read<T>(string source, T value)
        {
            Interlocked.Increment(ref Calls);
            if (Errors.TryGetValue(source, out var error)) throw error;
            return value;
        }
        public MemoryCounters ReadCounters() => Read("counters", Counters);
        public AutomaticPageFileMode ReadAutomaticMode() => Read("automatic", new AutomaticPageFileMode(Automatic));
        public IReadOnlyList<PageFileSetting> ReadConfiguredFiles() => Read("configured", Configured);
        public IReadOnlyList<PageFileUsage> ReadActiveFiles() => Read("active", Active);
        public IReadOnlyList<PageFileVolume> ReadVolumes() => Read<IReadOnlyList<PageFileVolume>>("volumes", new[]
            { new PageFileVolume("C:", 512 * GiB, 100 * GiB), new PageFileVolume("D:", null, null) });
        public CrashDumpConfiguration ReadCrashDump() => Read("dump", Dump);
    }

    [Fact]
    public async Task IndependentSources_PreserveMultipleVolumesZeroUsageAndCommitAboveRam()
    {
        var source = new Source(); var before = DateTimeOffset.UtcNow;
        var result = await new MemoryConfigurationReader(source).ReadAsync();
        Assert.Equal(40 * GiB, result.Counters.Value?.CommitBytes);
        Assert.Equal(32 * GiB, result.Counters.Value?.PhysicalTotalBytes);
        Assert.Equal(48 * GiB, result.Counters.Value?.CommitPeakSinceBootBytes);
        Assert.False(result.AutomaticMode.Value?.Enabled);
        Assert.Equal(2, result.ConfiguredFiles.Value?.Count);
        Assert.Equal(0u, result.ConfiguredFiles.Value?[1].InitialMb);
        Assert.Equal(0u, result.ActiveFiles.Value?[0].UsedMb);
        Assert.Null(result.Volumes.Value?[1].FreeBytes);
        Assert.Equal(PageFilePathComparison.PathsMatch, result.PathComparison);
        Assert.Equal(6, source.Calls);
        Assert.InRange(result.CheckedAtUtc, before, DateTimeOffset.UtcNow);
    }

    [Theory]
    [InlineData("counters")]
    [InlineData("automatic")]
    [InlineData("configured")]
    [InlineData("active")]
    [InlineData("volumes")]
    [InlineData("dump")]
    public async Task OneFailedSource_DoesNotDiscardOtherSources(string failed)
    {
        var source = new Source(); source.Errors[failed] = new UnauthorizedAccessException();
        var result = await new MemoryConfigurationReader(source).ReadAsync();
        var errors = new[] { result.Counters.Error, result.AutomaticMode.Error, result.ConfiguredFiles.Error,
            result.ActiveFiles.Error, result.Volumes.Error, result.CrashDump.Error };
        Assert.Equal(MemoryReadFailure.AccessDenied, Assert.Single(errors.OfType<MemoryReadError>()).Kind);
        Assert.Equal(6, source.Calls);
        if (failed != "counters") Assert.NotNull(result.Counters.Value);
        if (failed != "active") Assert.Equal(2, result.ActiveFiles.Value?.Count);
        if (failed is "configured" or "active" or "automatic") Assert.Equal(PageFilePathComparison.Unknown, result.PathComparison);
    }

    [Theory]
    [InlineData("timeout", MemoryReadFailure.TimedOut)]
    [InlineData("invalid", MemoryReadFailure.InvalidData)]
    [InlineData("overflow", MemoryReadFailure.InvalidData)]
    [InlineData("denied", MemoryReadFailure.AccessDenied)]
    [InlineData("other", MemoryReadFailure.Failed)]
    public async Task FailuresRetainKindAndSourceWithoutReturningEmptySuccess(string failure, MemoryReadFailure expected)
    {
        var source = new Source(); source.Errors["configured"] = failure switch
        {
            "timeout" => new TimeoutException(), "invalid" => new InvalidDataException(), "overflow" => new OverflowException(),
            "denied" => new Win32Exception(5), _ => new IOException()
        };
        var result = await new MemoryConfigurationReader(source).ReadAsync();
        Assert.Null(result.ConfiguredFiles.Value);
        Assert.Equal(expected, result.ConfiguredFiles.Error?.Kind);
        Assert.Equal("Win32_PageFileSetting", result.ConfiguredFiles.Error?.Source);
    }

    [Fact]
    public async Task NoPageFiles_IsEmptyDataNotReadFailure()
    {
        var result = await new MemoryConfigurationReader(new Source { Configured = [], Active = [] }).ReadAsync();
        Assert.Empty(result.ActiveFiles.Value!); Assert.Empty(result.ConfiguredFiles.Value!);
        Assert.Null(result.ActiveFiles.Error); Assert.Null(result.ConfiguredFiles.Error);
        Assert.Equal(PageFilePathComparison.PathsMatch, result.PathComparison);
    }

    [Fact]
    public async Task SavedAndActivePathsDiffer_WithoutClaimingRestartIsConfirmed()
    {
        var result = await new MemoryConfigurationReader(new Source { Active = new PageFileUsage[] { new(@"E:\pagefile.sys", 4096, 20, 30, true) } }).ReadAsync();
        Assert.Equal(PageFilePathComparison.PathsDiffer, result.PathComparison);
        Assert.True(result.ActiveFiles.Value![0].Temporary);
        Assert.Equal(2, result.ConfiguredFiles.Value?.Count);
    }

    [Fact]
    public async Task AutomaticManagement_DoesNotTreatEmptySavedListAsDisabledOrPendingRestart()
    {
        var result = await new MemoryConfigurationReader(new Source { Automatic = true, Configured = [] }).ReadAsync();
        Assert.True(result.AutomaticMode.Value?.Enabled);
        Assert.Equal(PageFilePathComparison.Unknown, result.PathComparison);
        Assert.NotEmpty(result.ActiveFiles.Value!);
    }

    [Theory]
    [InlineData(0u, DumpRequirementKind.Disabled)]
    [InlineData(2u, DumpRequirementKind.WorkloadDependent)]
    [InlineData(7u, DumpRequirementKind.WorkloadDependent)]
    [InlineData(99u, DumpRequirementKind.Unknown)]
    public async Task DumpRequirements_DoNotInventExactSizes(uint type, DumpRequirementKind expected)
    {
        var result = await new MemoryConfigurationReader(new Source { Dump = new(type, null, null, null, null) }).ReadAsync();
        Assert.Equal(expected, result.DumpRequirement.Kind); Assert.Null(result.DumpRequirement.MinimumBytes);
    }

    [Fact]
    public async Task CompleteDumpUsesRamPlusOverhead_SmallDumpHasSeparateMinimum()
    {
        var source = new Source(); var reader = new MemoryConfigurationReader(source);
        Assert.Equal(32 * GiB + 257UL * 1024 * 1024, (await reader.ReadAsync()).DumpRequirement.MinimumBytes);
        source.Dump = new(3, null, null, null, null);
        Assert.Equal(1024UL * 1024, (await reader.ReadAsync()).DumpRequirement.MinimumBytes);
    }

    [Fact]
    public async Task MissingRamPreventsCompleteDumpEstimate_ActiveDumpRemainsDynamic()
    {
        var source = new Source(); source.Errors["counters"] = new IOException();
        var reader = new MemoryConfigurationReader(source);
        Assert.Equal(DumpRequirementKind.Unknown, (await reader.ReadAsync()).DumpRequirement.Kind);
        source.Dump = new(1, 1, null, null, null);
        Assert.Equal(DumpRequirementKind.WorkloadDependent, (await reader.ReadAsync()).DumpRequirement.Kind);
    }

    [Fact]
    public async Task AbsentDumpValuesRemainUnknown_DedicatedFileDoesNotImplyReadiness()
    {
        var source = new Source { Dump = new(null, null, null, null, null) };
        var reader = new MemoryConfigurationReader(source);
        var result = await reader.ReadAsync();
        Assert.Null(result.CrashDump.Value?.CrashDumpEnabled);
        Assert.Equal(DumpRequirementKind.Unknown, result.DumpRequirement.Kind);
        source.Dump = new(1, null, @"D:\dedicated.sys", 10, @"%SystemRoot%\MEMORY.DMP");
        result = await reader.ReadAsync();
        Assert.Equal(@"D:\dedicated.sys", result.CrashDump.Value?.DedicatedDumpFile);
        Assert.Equal(32 * GiB + 257UL * 1024 * 1024, result.DumpRequirement.MinimumBytes);
    }

    [Fact]
    public void NativeCountersConvertPageCountsToBytesWithout32BitTruncation()
    {
        var counters = WindowsMemoryReadSource.ConvertCounters(4096, 8_388_608, 1_048_576, 10_485_760, 16_777_216, 12_582_912);
        Assert.Equal(32 * GiB, counters.PhysicalTotalBytes);
        Assert.Equal(40 * GiB, counters.CommitBytes);
        Assert.Equal(48 * GiB, counters.CommitPeakSinceBootBytes);
    }

    [Fact]
    public void InvalidNativeCountersNeverProducePlausibleMemoryData()
    {
        Assert.Throws<InvalidDataException>(() => WindowsMemoryReadSource.ConvertCounters(0, 8, 4, 10, 16, 12));
        Assert.Throws<InvalidDataException>(() => WindowsMemoryReadSource.ConvertCounters(4096, 8, 9, 10, 16, 12));
        Assert.Throws<InvalidDataException>(() => WindowsMemoryReadSource.ConvertCounters(4096, 8, 4, 17, 16, 18));
        Assert.Throws<OverflowException>(() => WindowsMemoryReadSource.ConvertCounters(4096, ulong.MaxValue, 4, 10, 16, 12));
    }

    [Fact]
    public async Task CancelledReadDoesNotAccessSystemSources()
    {
        var source = new Source(); using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new MemoryConfigurationReader(source).ReadAsync(cts.Token));
        Assert.Equal(0, source.Calls);
    }
}
