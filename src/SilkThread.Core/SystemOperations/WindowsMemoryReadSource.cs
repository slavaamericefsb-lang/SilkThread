using System.ComponentModel;
using System.Management;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace SilkThread.Core.SystemOperations;

internal sealed class WindowsMemoryReadSource : IMemoryReadSource
{
    public MemoryCounters ReadCounters()
    {
        var size = (uint)Marshal.SizeOf<PerformanceInformation>();
        if (!GetPerformanceInfo(out var info, size)) throw new Win32Exception(Marshal.GetLastWin32Error());
        return ConvertCounters(info.PageSize.ToUInt64(), info.PhysicalTotal.ToUInt64(), info.PhysicalAvailable.ToUInt64(),
            info.CommitTotal.ToUInt64(), info.CommitLimit.ToUInt64(), info.CommitPeak.ToUInt64());
    }

    internal static MemoryCounters ConvertCounters(ulong pageSize, ulong physical, ulong available, ulong commit, ulong limit, ulong peak)
    {
        if (pageSize == 0 || physical == 0 || available > physical || commit > limit || peak < commit)
            throw new InvalidDataException("Invalid performance counter data.");
        return new(checked(physical * pageSize), checked(available * pageSize), checked(commit * pageSize),
            checked(limit * pageSize), checked(peak * pageSize));
    }

    public AutomaticPageFileMode ReadAutomaticMode()
    {
        var values = Query("SELECT AutomaticManagedPagefile FROM Win32_ComputerSystem",
            row => new AutomaticPageFileMode(Required<bool>(row, "AutomaticManagedPagefile")));
        return values.Count == 1 ? values[0] : throw new InvalidDataException("Missing computer system data.");
    }

    public IReadOnlyList<PageFileSetting> ReadConfiguredFiles() => Query("SELECT Name,InitialSize,MaximumSize FROM Win32_PageFileSetting",
        row => new PageFileSetting(Required<string>(row, "Name"), Required<uint>(row, "InitialSize"), Required<uint>(row, "MaximumSize")));

    public IReadOnlyList<PageFileUsage> ReadActiveFiles() => Query("SELECT Name,AllocatedBaseSize,CurrentUsage,PeakUsage,TempPageFile FROM Win32_PageFileUsage",
        row => new PageFileUsage(Required<string>(row, "Name"), Required<uint>(row, "AllocatedBaseSize"),
            Required<uint>(row, "CurrentUsage"), Required<uint>(row, "PeakUsage"), Required<bool>(row, "TempPageFile")));

    public IReadOnlyList<PageFileVolume> ReadVolumes() => Query("SELECT DeviceID,Size,FreeSpace FROM Win32_LogicalDisk WHERE DriveType=3",
        row => new PageFileVolume(Required<string>(row, "DeviceID"), Optional<ulong>(row, "Size"), Optional<ulong>(row, "FreeSpace")));

    private static T Required<T>(ManagementBaseObject row, string name)
        => row[name] is T value && (value is not string text || !string.IsNullOrWhiteSpace(text))
            ? value : throw new InvalidDataException($"Missing or invalid WMI property: {name}.");
    private static T? Optional<T>(ManagementBaseObject row, string name) where T : struct
        => row[name] == null ? null : Required<T>(row, name);

    internal static IReadOnlyList<T> Query<T>(string query, Func<ManagementBaseObject, T> project)
    {
        var scope = new ManagementScope(@"\\.\root\cimv2", new ConnectionOptions { Timeout = TimeSpan.FromSeconds(5) });
        using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery(query),
            new System.Management.EnumerationOptions { ReturnImmediately = true, Timeout = TimeSpan.FromSeconds(5) });
        using var rows = searcher.Get();
        var result = new List<T>();
        foreach (ManagementBaseObject row in rows)
            using (row) result.Add(project(row));
        return result.AsReadOnly();
    }

    public CrashDumpConfiguration ReadCrashDump()
    {
        using var hive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var key = hive.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\CrashControl", false);
        if (key == null) throw new InvalidDataException("CrashControl key is absent.");
        uint? Number(string name)
        {
            var value = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            if (value == null) return null;
            if (key.GetValueKind(name) != RegistryValueKind.DWord || value is not int number)
                throw new InvalidDataException($"Unexpected registry type: {name}.");
            return unchecked((uint)number);
        }
        string? Text(string name)
        {
            var value = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            if (value == null) return null;
            if (key.GetValueKind(name) is not (RegistryValueKind.String or RegistryValueKind.ExpandString) || value is not string text)
                throw new InvalidDataException($"Unexpected registry type: {name}.");
            return text; // Preserve environment variables and absence; no file readiness is inferred.
        }
        return new(Number("CrashDumpEnabled"), Number("FilterPages"), Text("DedicatedDumpFile"), Number("DumpFileSize"), Text("DumpFile"));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PerformanceInformation
    {
        public uint Size;
        public UIntPtr CommitTotal, CommitLimit, CommitPeak, PhysicalTotal, PhysicalAvailable;
        public UIntPtr SystemCache, KernelTotal, KernelPaged, KernelNonpaged, PageSize;
        public uint HandleCount, ProcessCount, ThreadCount;
    }
    [DllImport("psapi.dll", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetPerformanceInfo(out PerformanceInformation information, uint size);
}
