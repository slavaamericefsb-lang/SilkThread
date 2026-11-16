namespace SilkThread.Core.SystemOperations;

public enum MemoryReadFailure { AccessDenied, NotSupported, TimedOut, InvalidData, Failed }
public sealed record MemoryReadError(MemoryReadFailure Kind, int Code, string Source);
public sealed record MemoryRead<T>(T? Value, MemoryReadError? Error) where T : class;
public sealed record MemoryCounters(ulong PhysicalTotalBytes, ulong PhysicalAvailableBytes,
    ulong CommitBytes, ulong CommitLimitBytes, ulong CommitPeakSinceBootBytes);
public sealed record AutomaticPageFileMode(bool Enabled);
public sealed record PageFileSetting(string Path, uint InitialMb, uint MaximumMb);
public sealed record PageFileUsage(string Path, uint AllocatedMb, uint UsedMb, uint PeakUsedMb, bool Temporary);
public sealed record PageFileVolume(string Root, ulong? SizeBytes, ulong? FreeBytes);
// Null registry values mean absent, not a successful read of zero/default settings.
public sealed record CrashDumpConfiguration(uint? CrashDumpEnabled, uint? FilterPages,
    string? DedicatedDumpFile, uint? DedicatedDumpSizeMb, string? DumpFile);
public enum DumpRequirementKind { Unknown, Disabled, FixedMinimum, WorkloadDependent }
public sealed record DumpRequirement(DumpRequirementKind Kind, ulong? MinimumBytes);
public enum PageFilePathComparison { Unknown, PathsMatch, PathsDiffer }

public sealed record MemoryConfiguration(
    MemoryRead<MemoryCounters> Counters,
    MemoryRead<AutomaticPageFileMode> AutomaticMode,
    MemoryRead<IReadOnlyList<PageFileSetting>> ConfiguredFiles,
    MemoryRead<IReadOnlyList<PageFileUsage>> ActiveFiles,
    MemoryRead<IReadOnlyList<PageFileVolume>> Volumes,
    MemoryRead<CrashDumpConfiguration> CrashDump,
    DumpRequirement DumpRequirement,
    PageFilePathComparison PathComparison,
    DateTimeOffset CheckedAtUtc);

public interface IMemoryConfigurationReader
{
    Task<MemoryConfiguration> ReadAsync(CancellationToken cancellationToken = default);
}

internal interface IMemoryReadSource
{
    MemoryCounters ReadCounters();
    AutomaticPageFileMode ReadAutomaticMode();
    IReadOnlyList<PageFileSetting> ReadConfiguredFiles();
    IReadOnlyList<PageFileUsage> ReadActiveFiles();
    IReadOnlyList<PageFileVolume> ReadVolumes();
    CrashDumpConfiguration ReadCrashDump();
}
