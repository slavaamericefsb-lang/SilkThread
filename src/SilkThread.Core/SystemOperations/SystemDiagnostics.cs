using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace SilkThread.Core.SystemOperations;

public sealed record CpuSetInfo(uint Id, ushort Group, byte LogicalIndex, byte CoreIndex, byte EfficiencyClass, bool Parked);
public sealed record ProcessorGroupInfo(ushort Group, uint ActiveProcessors);
public sealed record CpuTopology(IReadOnlyList<CpuSetInfo> CpuSets, IReadOnlyList<ProcessorGroupInfo> Groups)
{
    public int VisibleCores => CpuSets.Select(c => (c.Group, c.CoreIndex)).Distinct().Count();
    public bool IsComplete => Groups.Sum(g => (long)g.ActiveProcessors) == CpuSets.Count;
    public bool? SmtVisible => CpuSets.GroupBy(c => (c.Group, c.CoreIndex)).Any(g => g.Count() > 1) ? true : IsComplete ? false : null;
}
public sealed record DeviceDriverInfo(string Name, string Class, string DeviceId, string? DriverVersion, string? Provider, bool? Signed);
public sealed record DeviceDiagnostics(IReadOnlyList<DeviceDriverInfo> Drivers, uint? ConfiguredHagsMode,
    MemoryReadError? DriverError = null, MemoryReadError? HagsError = null);
public sealed record PowerSupplyInfo(bool? OnAc, bool? HasBattery, byte? BatteryPercent, bool? Charging);
public sealed record BcdDiagnostics(int ExitCode, string Output, string Error, IReadOnlyDictionary<string, string?> ExplicitOverrides);
public sealed record SystemDiagnostics(MemoryRead<CpuTopology> Cpu, MemoryRead<DeviceDiagnostics> Devices,
    MemoryRead<PowerSupplyInfo> Power, BcdDiagnostics Bcd, bool IsAdministrator, DateTimeOffset CheckedAtUtc);
public interface ISystemDiagnosticsService { Task<SystemDiagnostics> ReadAsync(CancellationToken token = default); }

public sealed class SystemDiagnosticsService : ISystemDiagnosticsService
{
    public async Task<SystemDiagnostics> ReadAsync(CancellationToken token = default)
    {
        var cpu = MemoryConfigurationReader.CaptureAsync(ReadCpu, "GetSystemCpuSetInformation", token);
        var devices = MemoryConfigurationReader.CaptureAsync(ReadDevices, "Win32_PnPSignedDriver / HwSchMode", token);
        var power = MemoryConfigurationReader.CaptureAsync(ReadPower, "GetSystemPowerStatus", token);
        bool admin = ElevationHelper.IsAdministrator();
        var result = admin ? await CommandOperations.RunAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "bcdedit.exe"),
            "/enum {current} /v", outputEncoding: System.Text.CodePagesEncodingProvider.Instance.GetEncoding((int)GetOEMCP()))
            : (ExitCode: 5, Output: "", Error: "");
        await Task.WhenAll(cpu, devices, power); token.ThrowIfCancellationRequested();
        BcdDiagnostics bcd;
        try { bcd = new(result.ExitCode, result.Output, result.Error, ParseBcd(result.ExitCode == 0 ? result.Output : "")); }
        catch (InvalidDataException ex) { bcd = new(-2, result.Output, ex.Message, ParseBcd("")); }
        return new(cpu.Result, devices.Result, power.Result, bcd, admin, DateTimeOffset.UtcNow);
    }

    internal static IReadOnlyDictionary<string, string?> ParseBcd(string output)
    {
        var keys = new[] { "useplatformclock", "useplatformtick", "disabledynamictick", "tscsyncpolicy", "numproc", "onecpu", "groupsize", "maxgroup" };
        return keys.ToDictionary(key => key, key =>
        {
            var matches = Regex.Matches(output, @"^[ \t]*" + Regex.Escape(key) + @"[ \t]+([^\r\n]+)\r?$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (matches.Count > 1) throw new InvalidDataException("Duplicate BCD option.");
            return matches.Count == 0 ? null : matches[0].Groups[1].Value.Trim();
        });
    }

    private static CpuTopology ReadCpu()
    {
        uint size = 0;
        _ = GetSystemCpuSetInformation(null, 0, out size, IntPtr.Zero, 0);
        for (var attempt = 0; attempt < 3; attempt++)
        {
            if (size == 0 || size > 16 * 1024 * 1024) throw new InvalidDataException("Invalid CPU topology buffer.");
            var buffer = new byte[size];
            if (!GetSystemCpuSetInformation(buffer, size, out var actual, IntPtr.Zero, 0))
            {
                var error = Marshal.GetLastWin32Error(); if (error == 122) { size = actual; continue; }
                throw new Win32Exception(error);
            }
            var sets = ParseCpuSets(buffer.AsSpan(0, checked((int)actual)));
            var groups = Enumerable.Range(0, GetActiveProcessorGroupCount()).Select(index =>
            {
                var count = GetActiveProcessorCount((ushort)index);
                if (count == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
                return new ProcessorGroupInfo((ushort)index, count);
            }).ToArray();
            return new(sets, groups);
        }
        throw new InvalidDataException("CPU topology changed while reading.");
    }

    internal static IReadOnlyList<CpuSetInfo> ParseCpuSets(ReadOnlySpan<byte> buffer)
    {
        var result = new List<CpuSetInfo>(); var ids = new HashSet<uint>(); var processors = new HashSet<(ushort, byte)>();
        for (int offset = 0; offset < buffer.Length;)
        {
            if (buffer.Length - offset < 8) throw new InvalidDataException("Truncated CPU topology.");
            var record = buffer[offset..]; uint size = BitConverter.ToUInt32(record[..4]);
            if (size < 8 || size > record.Length) throw new InvalidDataException("Invalid CPU topology record size.");
            if (BitConverter.ToUInt32(record.Slice(4, 4)) == 0)
            {
                if (size < 32) throw new InvalidDataException("Truncated CPU set.");
                var cpu = new CpuSetInfo(BitConverter.ToUInt32(record.Slice(8, 4)), BitConverter.ToUInt16(record.Slice(12, 2)), record[14], record[15], record[18], (record[19] & 1) != 0);
                if (!ids.Add(cpu.Id) || !processors.Add((cpu.Group, cpu.LogicalIndex))) throw new InvalidDataException("Duplicate CPU set.");
                result.Add(cpu);
            }
            offset += checked((int)size);
        }
        if (result.Count == 0) throw new InvalidDataException("No CPU sets were returned.");
        return result.AsReadOnly();
    }

    private static DeviceDiagnostics ReadDevices()
    {
        IReadOnlyList<DeviceDriverInfo> drivers = Array.Empty<DeviceDriverInfo>();
        MemoryReadError? driverError = null, hagsError = null;
        try { drivers = WindowsMemoryReadSource.Query("SELECT DeviceName,DeviceClass,DeviceID,DriverVersion,DriverProviderName,IsSigned FROM Win32_PnPSignedDriver WHERE DeviceClass='DISPLAY' OR DeviceClass='NET' OR DeviceClass='DISKDRIVE' OR DeviceClass='SCSIADAPTER'", row =>
            new DeviceDriverInfo(row["DeviceName"] as string ?? row["DeviceID"] as string ?? "N/A", row["DeviceClass"] as string ?? "N/A",
                row["DeviceID"] as string ?? "N/A", row["DriverVersion"] as string, row["DriverProviderName"] as string, row["IsSigned"] as bool?)); }
        catch (Exception ex) { driverError = new(ex is UnauthorizedAccessException ? MemoryReadFailure.AccessDenied : MemoryReadFailure.Failed, ex.HResult, "Win32_PnPSignedDriver"); }
        uint? hags = null;
        try
        {
            using var hive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var key = hive.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", false);
            var raw = key?.GetValue("HwSchMode");
            if (raw != null && raw is not int) throw new InvalidDataException("Unexpected HAGS value type.");
            hags = raw is int value ? unchecked((uint)value) : null;
        }
        catch (Exception ex) { hagsError = new(ex is UnauthorizedAccessException ? MemoryReadFailure.AccessDenied : MemoryReadFailure.Failed, ex.HResult, "HwSchMode"); }
        return new(drivers, hags, driverError, hagsError);
    }
    private static PowerSupplyInfo ReadPower()
    {
        if (!GetSystemPowerStatus(out var status)) throw new Win32Exception(Marshal.GetLastWin32Error());
        return DecodePower(status.Ac, status.BatteryFlag, status.BatteryPercent);
    }
    internal static PowerSupplyInfo DecodePower(byte ac, byte flags, byte percent)
    {
        bool? hasBattery = flags == 255 ? null : (flags & 128) == 0;
        return new(ac is 0 or 1 ? ac == 1 : null, hasBattery, hasBattery == true && percent <= 100 ? percent : null,
            hasBattery == true ? (flags & 8) != 0 : null);
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct PowerStatus { public byte Ac, BatteryFlag, BatteryPercent, Reserved; public uint Life, FullLife; }
    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemCpuSetInformation([Out] byte[]? info, uint size, out uint returned, IntPtr process, uint flags);
    [DllImport("kernel32.dll", ExactSpelling = true)] private static extern ushort GetActiveProcessorGroupCount();
    [DllImport("kernel32.dll", ExactSpelling = true)] private static extern uint GetOEMCP();
    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)] private static extern uint GetActiveProcessorCount(ushort group);
    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetSystemPowerStatus(out PowerStatus status);
}
