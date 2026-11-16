using SilkThread.Core.SystemOperations;

namespace SilkThread.App.ViewModels;

public partial class SystemSettingsViewModel
{
    private readonly IMemoryConfigurationReader _memoryReader;
    private MemoryConfiguration? _memorySnapshot;
    private bool _isMemoryBusy;
    private string _memoryReport = "";
    public bool IsMemoryBusy { get => _isMemoryBusy; private set { if (SetProperty(ref _isMemoryBusy, value)) { OnPropertyChanged(nameof(IsBusy)); OnPropertyChanged(nameof(CanEditMemory)); OnPropertyChanged(nameof(CanEditManual)); } } }
    public string MemoryReport { get => _memoryReport; private set => SetProperty(ref _memoryReport, value); }
    public string MemoryTitle => _loc["Memory_Title"];
    public string MemoryRefresh => _loc["Power_Refresh"];
    public string MemoryNotice => _loc["Memory_Notice"];

    public async Task RefreshMemoryAsync()
    {
        if (IsMemoryBusy) return;
        IsMemoryBusy = true;
        try
        {
            _pagePending = await _pageFiles.CheckPendingAsync();
            _memorySnapshot = await _memoryReader.ReadAsync(); RenderMemory();
            if (!HasMemoryDraft) LoadMemoryEditor();
            PageFileStatus = _pagePending == null ? "" : _loc["Page_Result_" + (_pagePending.Detail == "PostBootVerificationPending" ? _pagePending.Detail : _pagePending.State)];
            OnPropertyChanged(nameof(IsBusy)); OnPropertyChanged(nameof(CanEditMemory)); OnPropertyChanged(nameof(CanEditManual));
        }
        catch (Exception ex) { App.Log($"[Memory refresh]: {ex}"); MemoryReport = _loc["Memory_Error_Failed"]; }
        finally { IsMemoryBusy = false; OnPropertyChanged(nameof(IsBusy)); OnPropertyChanged(nameof(CanEditMemory)); OnPropertyChanged(nameof(CanEditManual)); }
    }

    private void RenderMemory()
    {
        OnPropertyChanged(nameof(MemoryTitle));
        OnPropertyChanged(nameof(MemoryRefresh));
        OnPropertyChanged(nameof(MemoryNotice));
        var snapshot = _memorySnapshot;
        if (snapshot == null) return;
        string Bytes(ulong? value) => !value.HasValue ? _loc["Memory_Unknown"]
            : value.Value < 1073741824 ? (value.Value / 1048576d).ToString("0.##") + " MiB"
            : (value.Value / 1073741824d).ToString("0.##") + " GiB";
        string Show<T>(MemoryRead<T> read, Func<T, string> format) where T : class
            => read.Value != null ? format(read.Value) : _loc["Memory_Error_" + (read.Error?.Kind ?? MemoryReadFailure.Failed)];
        string Rows(IEnumerable<string> rows) => string.Join("\n", rows.DefaultIfEmpty(_loc["Memory_NoRows"]));
        var report = new System.Text.StringBuilder();
        report.AppendLine(_loc.Format("Memory_Checked", snapshot.CheckedAtUtc.ToLocalTime().ToString("g")));
        report.AppendLine(Show(snapshot.Counters, c => _loc.Format("Memory_Counters", Bytes(c.PhysicalTotalBytes),
            Bytes(c.PhysicalAvailableBytes), Bytes(c.CommitBytes), Bytes(c.CommitLimitBytes), Bytes(c.CommitPeakSinceBootBytes))));
        report.AppendLine();
        report.AppendLine(_loc["Memory_Automatic"] + ": " + Show(snapshot.AutomaticMode, m => _loc[m.Enabled ? "Memory_On" : "Memory_Off"]));
        report.AppendLine(_loc["Memory_Configured"]);
        report.AppendLine(Show(snapshot.ConfiguredFiles, files => Rows(files.Select(f =>
            $"{f.Path}: " + (f.InitialMb == 0 && f.MaximumMb == 0 ? _loc["Memory_SystemSize"]
                : _loc.Format("Memory_SizeRange", f.InitialMb, f.MaximumMb))))));
        report.AppendLine();
        report.AppendLine(_loc["Memory_Active"]);
        report.AppendLine(Show(snapshot.ActiveFiles, files => Rows(files.Select(f =>
            _loc.Format("Memory_Usage", f.Path, f.AllocatedMb, f.UsedMb, f.PeakUsedMb) + (f.Temporary ? " • " + _loc["Memory_Temporary"] : "")))));
        report.AppendLine(_loc["Memory_Paths_" + snapshot.PathComparison]);
        report.AppendLine();
        report.AppendLine(_loc["Memory_Volumes"]);
        report.AppendLine(Show(snapshot.Volumes, volumes => Rows(volumes.Select(v => _loc.Format("Memory_Volume", v.Root, Bytes(v.FreeBytes), Bytes(v.SizeBytes))))));
        report.AppendLine();
        report.AppendLine(_loc["Memory_Dump"]);
        report.AppendLine(Show(snapshot.CrashDump, d =>
        {
            string type = d.CrashDumpEnabled == 1 && d.FilterPages == 1 ? "Active" : d.CrashDumpEnabled?.ToString() ?? "Unknown";
            var text = _loc["Memory_DumpType_" + (type is "0" or "1" or "2" or "3" or "7" or "Active" ? type : "Unknown")];
            if (!string.IsNullOrWhiteSpace(d.DumpFile)) text += "\n" + _loc["Memory_DumpPath"] + ": " + d.DumpFile;
            if (!string.IsNullOrWhiteSpace(d.DedicatedDumpFile)) text += "\n" + _loc["Memory_Dedicated"] + ": " + d.DedicatedDumpFile;
            if (d.DedicatedDumpSizeMb.HasValue) text += "\n" + _loc.Format("Memory_DumpSize", d.DedicatedDumpSizeMb.Value);
            return text;
        }));
        report.AppendLine(snapshot.DumpRequirement.Kind == DumpRequirementKind.FixedMinimum
            ? _loc.Format("Memory_DumpMinimum", snapshot.DumpRequirement.MinimumBytes!.Value.ToString("N0") + " B")
            : _loc["Memory_DumpRequirement_" + snapshot.DumpRequirement.Kind]);
        report.AppendLine(_loc["Memory_DumpCaveat"]);
        MemoryReport = report.ToString().TrimEnd();
    }
}
