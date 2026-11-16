using SilkThread.Core.SystemOperations;

namespace SilkThread.App.ViewModels;

public partial class SystemSettingsViewModel
{
    private readonly ISystemDiagnosticsService _diagnostics;
    private SystemDiagnostics? _diagnosticSnapshot;
    private string _cpuReport = "", _deviceReport = "", _bcdReport = "", _supplyReport = "", _accessReport = "";
    private bool _diagnosticsBusy;
    public string CpuReport { get => _cpuReport; private set => SetProperty(ref _cpuReport, value); }
    public string DeviceReport { get => _deviceReport; private set => SetProperty(ref _deviceReport, value); }
    public string BcdReport { get => _bcdReport; private set => SetProperty(ref _bcdReport, value); }
    public string SupplyReport { get => _supplyReport; private set => SetProperty(ref _supplyReport, value); }
    public string AccessReport { get => _accessReport; private set => SetProperty(ref _accessReport, value); }
    public string CpuTitle => _loc["System_Cpu"];
    public string DevicesTitle => _loc["System_Devices"];
    public string TimersTitle => _loc["System_Timers"];
    public string ExportLabel => _loc["System_Copy"];
    public string GraphicsSettingsLabel => _loc["System_GraphicsSettings"];
    public string HagsTweakLabel => _loc["System_HagsTweak"];
    public void OpenHagsTweak() => _nav.Navigate(typeof(SilkThread.App.Views.TweaksPage), "gpu_hags_enable");
    public string DevicesNotice => _loc["System_DevicesNotice"];
    public string TimersNotice => _loc["System_TimersNotice"];
    public async Task RefreshDiagnosticsAsync()
    {
        if (_diagnosticsBusy) return; _diagnosticsBusy = true; OnPropertyChanged(nameof(IsBusy)); AccessReport = _loc["System_Checking"];
        try { _diagnosticSnapshot = await _diagnostics.ReadAsync(); RenderDiagnostics(); }
        catch (Exception ex) { App.Log($"[System diagnostics]: {ex}"); AccessReport = CpuReport = DeviceReport = BcdReport = _loc["Memory_Error_Failed"]; }
        finally { _diagnosticsBusy = false; OnPropertyChanged(nameof(IsBusy)); }
    }
    private void NotifySystemLabels()
    {
        OnPropertyChanged(nameof(PowerTabTitle)); OnPropertyChanged(nameof(MemoryTabTitle));
        OnPropertyChanged(nameof(EnabledLabel)); OnPropertyChanged(nameof(DisabledLabel));
        OnPropertyChanged(nameof(HagsTweakLabel));
        foreach (var property in new[] { nameof(InspectorTitle), nameof(DraftTitle), nameof(DraftSummary), nameof(InspectorText), nameof(ReviewLabel), nameof(DiscardLabel), nameof(CpuTitle), nameof(DevicesTitle), nameof(TimersTitle), nameof(ExportLabel), nameof(GraphicsSettingsLabel), nameof(DevicesNotice), nameof(TimersNotice), nameof(AutomaticLabel), nameof(RemovalLabel), nameof(PageReviewLabel), nameof(PageRestoreLabel), nameof(PageRiskLabel) }) OnPropertyChanged(property);
        RenderDiagnostics();
        if (!HasMemoryDraft) LoadMemoryEditor();
        foreach (var editor in PageFileEditors) editor.NotifyLanguage();
        if (_pagePending != null) PageFileStatus = _loc["Page_Result_" + (_pagePending.Detail == "PostBootVerificationPending" ? _pagePending.Detail : _pagePending.State)];
    }
    private void RenderDiagnostics()
    {
        var s = _diagnosticSnapshot; if (s == null) return;
        string Unknown = _loc["Memory_Unknown"];
        string Bool(bool? value) => !value.HasValue ? Unknown : _loc[value.Value ? "Memory_On" : "Memory_Off"];
        string Checked = _loc.Format("System_Checked", s.CheckedAtUtc.ToLocalTime().ToString("g"));
        AccessReport = _loc[s.IsAdministrator ? "System_Admin" : "System_Standard"];
        if (s.Cpu.Value is { } cpu)
        {
            CpuReport = Checked + "\nGetSystemCpuSetInformation\n" + _loc.Format("System_CpuCounts", cpu.VisibleCores, cpu.CpuSets.Count, cpu.Groups.Count)
                + "\nSMT: " + Bool(cpu.SmtVisible) + "\n" + _loc[cpu.IsComplete ? "System_TopologyComplete" : "System_TopologyPartial"]
                + "\n" + _loc["System_EfficiencyNotice"] + "\n" + string.Join("\n", cpu.CpuSets.GroupBy(c => (c.Group, c.CoreIndex, c.EfficiencyClass))
                    .Select(g => $"Group {g.Key.Group} • Core {g.Key.CoreIndex} • Class {g.Key.EfficiencyClass}: " + string.Join(", ", g.Select(c => c.LogicalIndex))));
        }
        else CpuReport = _loc["Memory_Error_" + (s.Cpu.Error?.Kind ?? MemoryReadFailure.Failed)];
        DeviceReport = s.Devices.Value is { } devices ? Checked + "\nWin32_PnPSignedDriver\nHAGS (HwSchMode): " + (devices.ConfiguredHagsMode?.ToString() ?? Unknown)
            + "\n" + (devices.HagsError == null ? "" : _loc["Memory_Error_" + devices.HagsError.Kind])
            + "\n" + (devices.DriverError == null ? "" : _loc["Memory_Error_" + devices.DriverError.Kind])
            + "\n" + DevicesNotice + "\n\n" + string.Join("\n\n", devices.Drivers.Select(d => $"{d.Class} • {d.Name}\n{d.DeviceId}\n{d.DriverVersion ?? Unknown} • {d.Provider ?? Unknown}\n" + _loc["System_Signed"] + ": " + Bool(d.Signed)))
            : _loc["Memory_Error_" + (s.Devices.Error?.Kind ?? MemoryReadFailure.Failed)];
        SupplyReport = s.Power.Value is { } power ? _loc.Format("System_Supply", Bool(power.OnAc), Bool(power.HasBattery), power.BatteryPercent.HasValue ? power.BatteryPercent.Value + "%" : Unknown, Bool(power.Charging))
            : _loc["Memory_Error_" + (s.Power.Error?.Kind ?? MemoryReadFailure.Failed)];
        BcdReport = Checked + "\nBCDEdit /enum {current} /v\n" + TimersNotice + "\n\n"
            + (s.Bcd.ExitCode == 0 ? string.Join("\n", s.Bcd.ExplicitOverrides.Select(pair => pair.Key + ": " + (pair.Value ?? _loc["System_NotExplicit"])))
                : _loc["System_BcdUnavailable"] + $" ({s.Bcd.ExitCode})\n{s.Bcd.Error}\n{s.Bcd.Output}");
    }
    public string GetSystemReport() => string.Join("\n\n", AccessReport, ActivePowerScheme, SupplyReport, MemoryReport, CpuReport, DeviceReport, BcdReport);
}
