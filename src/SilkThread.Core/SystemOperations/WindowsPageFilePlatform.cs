using System.Management;

namespace SilkThread.Core.SystemOperations;

internal sealed class WindowsPageFilePlatform : IPageFilePlatform
{
    internal const string MemoryKey = @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management";
    public bool IsAdministrator => ElevationHelper.IsAdministrator();
    public Task<PageFileSnapshot> ReadAsync() => Task.Run(() =>
    {
        var source = new WindowsMemoryReadSource();
        var raw = RegistryOperations.SnapshotValue("HKLM", MemoryKey, "PagingFiles");
        if (!raw.ReadSucceeded) throw new UnauthorizedAccessException("PagingFiles snapshot failed.");
        if (raw.Exists && raw.ValueKind != "MultiString") throw new InvalidDataException("Unexpected PagingFiles type.");
        return new PageFileSnapshot(source.ReadAutomaticMode().Enabled, raw, source.ReadConfiguredFiles().ToArray());
    });
    public Task<string> BootIdAsync() => Task.Run(() =>
        WindowsMemoryReadSource.Query("SELECT LastBootUpTime FROM Win32_OperatingSystem", r => (string)r["LastBootUpTime"]).Single());
    public Task SetAutomaticAsync(bool value) => Task.Run(() =>
    {
        var scope = new ManagementScope(@"\\.\root\cimv2", new ConnectionOptions { EnablePrivileges = true, Timeout = TimeSpan.FromSeconds(5) });
        using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT * FROM Win32_ComputerSystem"),
            new System.Management.EnumerationOptions { Timeout = TimeSpan.FromSeconds(5), ReturnImmediately = true });
        using var rows = searcher.Get(); int count = 0;
        foreach (ManagementObject row in rows) using (row)
        {
            count++; if ((bool)row["AutomaticManagedPagefile"] != value)
            {
                row["AutomaticManagedPagefile"] = value;
                row.Put(new PutOptions { Type = PutType.UpdateOnly, Timeout = TimeSpan.FromSeconds(5) });
            }
        }
        if (count != 1) throw new InvalidDataException("Computer system unavailable.");
    });
    public Task WritePagingFilesAsync(RegistryValueSnapshot value) => Task.Run(() =>
    {
        if (!value.ReadSucceeded || value.Hive != "HKLM" || value.SubKey != MemoryKey || value.ValueName != "PagingFiles"
            || value.Exists && value.ValueKind != "MultiString") throw new InvalidDataException("Invalid page-file snapshot.");
        if (!RegistryOperations.RestoreFromSnapshot(value)) throw new IOException("PagingFiles write failed.");
    });
}
