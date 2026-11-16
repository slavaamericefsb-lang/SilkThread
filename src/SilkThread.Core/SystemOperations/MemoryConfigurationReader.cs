using System.ComponentModel;
using System.Management;
using System.Security;

namespace SilkThread.Core.SystemOperations;

public sealed class MemoryConfigurationReader : IMemoryConfigurationReader
{
    private readonly IMemoryReadSource _source;
    public MemoryConfigurationReader() : this(new WindowsMemoryReadSource()) { }
    internal MemoryConfigurationReader(IMemoryReadSource source) => _source = source;

    public async Task<MemoryConfiguration> ReadAsync(CancellationToken cancellationToken = default)
    {
        var counters = CaptureAsync(_source.ReadCounters, "GetPerformanceInfo", cancellationToken);
        var automatic = CaptureAsync(_source.ReadAutomaticMode, "Win32_ComputerSystem.AutomaticManagedPagefile", cancellationToken);
        var configured = CaptureAsync(_source.ReadConfiguredFiles, "Win32_PageFileSetting", cancellationToken);
        var active = CaptureAsync(_source.ReadActiveFiles, "Win32_PageFileUsage", cancellationToken);
        var volumes = CaptureAsync(_source.ReadVolumes, "Win32_LogicalDisk", cancellationToken);
        var dump = CaptureAsync(_source.ReadCrashDump, "HKLM/CrashControl", cancellationToken);
        await Task.WhenAll(counters, automatic, configured, active, volumes, dump);
        cancellationToken.ThrowIfCancellationRequested();
        var comparison = PageFilePathComparison.Unknown;
        if (automatic.Result.Value?.Enabled == false && configured.Result.Value is { } settings && active.Result.Value is { } usage)
        {
            var paths = settings.Select(s => s.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
            comparison = paths.SetEquals(usage.Select(s => s.Path)) ? PageFilePathComparison.PathsMatch : PageFilePathComparison.PathsDiffer;
        }
        return new(counters.Result, automatic.Result, configured.Result, active.Result, volumes.Result, dump.Result,
            GetDumpRequirement(dump.Result.Value, counters.Result.Value), comparison, DateTimeOffset.UtcNow);
    }

    internal static Task<MemoryRead<T>> CaptureAsync<T>(Func<T> read, string source, CancellationToken token) where T : class
        => Task.Run(() =>
        {
            token.ThrowIfCancellationRequested();
            try { return new MemoryRead<T>(read(), null); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                var kind = ex switch
                {
                    UnauthorizedAccessException or SecurityException => MemoryReadFailure.AccessDenied,
                    Win32Exception { NativeErrorCode: 5 } => MemoryReadFailure.AccessDenied,
                    ManagementException { ErrorCode: ManagementStatus.AccessDenied } => MemoryReadFailure.AccessDenied,
                    ManagementException { ErrorCode: ManagementStatus.InvalidClass or ManagementStatus.NotSupported } => MemoryReadFailure.NotSupported,
                    TimeoutException or ManagementException { ErrorCode: ManagementStatus.Timedout } => MemoryReadFailure.TimedOut,
                    EntryPointNotFoundException or DllNotFoundException or PlatformNotSupportedException => MemoryReadFailure.NotSupported,
                    InvalidDataException or OverflowException => MemoryReadFailure.InvalidData,
                    _ => MemoryReadFailure.Failed
                };
                return new MemoryRead<T>(null, new(kind, ex is Win32Exception win32 ? win32.NativeErrorCode : ex.HResult, source));
            }
        }, token);

    internal static DumpRequirement GetDumpRequirement(CrashDumpConfiguration? dump, MemoryCounters? counters)
        => dump?.CrashDumpEnabled switch
        {
            0 => new(DumpRequirementKind.Disabled, null),
            3 => new(DumpRequirementKind.FixedMinimum, 1024UL * 1024),
            1 when dump.FilterPages == 1 => new(DumpRequirementKind.WorkloadDependent, null),
            1 when counters != null => new(DumpRequirementKind.FixedMinimum, checked(counters.PhysicalTotalBytes + 257UL * 1024 * 1024)),
            1 => new(DumpRequirementKind.Unknown, null),
            2 or 7 => new(DumpRequirementKind.WorkloadDependent, null),
            _ => new(DumpRequirementKind.Unknown, null)
        };
}
