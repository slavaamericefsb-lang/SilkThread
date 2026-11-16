using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SilkThread.App.Services;
using SilkThread.App.Views;
using SilkThread.Core.SystemOperations;

namespace SilkThread.App.ViewModels;

public partial class SystemSettingsViewModel : ObservableObject
{
    private readonly ILocalizationService _loc;
    private readonly INavigationService _nav;
    private readonly IPowerConfigurationReader _powerReader;
    private readonly IPowerSchemeService _powerService;
    public ObservableCollection<PowerSchemeInfo> PowerSchemes { get; } = new();
    private PowerSchemeInfo? _selectedPowerScheme;
    private bool _isPowerBusy;
    private string _powerStatus = "";
    private string _activePowerScheme = "";
    private string _powerCheckedAt = "";
    private Guid? _activePowerId;
    public PowerSchemeInfo? SelectedPowerScheme { get => _selectedPowerScheme; set { if (SetProperty(ref _selectedPowerScheme, value)) { NotifyDraftState(); OnPropertyChanged(nameof(InspectorText)); } } }
    private int _activeTab;
    public int ActiveTab { get => _activeTab; set { if (SetProperty(ref _activeTab, value)) { NotifyDraftState(); OnPropertyChanged(nameof(InspectorText)); } } }
    public bool HasPowerDraft => SelectedPowerScheme != null && SelectedPowerScheme.Id != _activePowerId;
    public bool HasSelectedChanges => ActiveTab == 1 && HasMemoryDraft || ActiveTab == 0 && HasPowerDraft;
    public bool HasPendingDrafts => HasMemoryDraft || HasPowerDraft;
    public bool IsBusy => IsPowerBusy || IsMemoryBusy || _diagnosticsBusy;
    public bool HasOperationStatus => !string.IsNullOrEmpty(OperationStatus);
    public string InspectorTitle => _loc["System_InspectorTitle"];
    public string DraftTitle => _loc["System_DraftTitle"];
    public string DraftSummary => string.Join(" вЂў ", new[] { HasPowerDraft ? PowerTabTitle : null, HasMemoryDraft ? MemoryTabTitle : null }.Where(s => s != null));
    private void NotifyDraftState()
    {
        foreach (var name in new[] { nameof(HasSelectedChanges), nameof(HasPowerDraft), nameof(HasPendingDrafts), nameof(DraftSummary) }) OnPropertyChanged(name);
    }
    public void SelectDraftForReview()
    {
        if (!HasSelectedChanges) ActiveTab = HasPowerDraft ? 0 : 1;
    }
    public void DiscardPowerDraft() => SelectedPowerScheme = System.Linq.Enumerable.FirstOrDefault(PowerSchemes, s => s.Id == _activePowerId);
    public bool IsPowerBusy { get => _isPowerBusy; private set { if (SetProperty(ref _isPowerBusy, value)) OnPropertyChanged(nameof(IsBusy)); } }
    public string PowerStatus { get => _powerStatus; private set { if (SetProperty(ref _powerStatus, value)) { OnPropertyChanged(nameof(OperationStatus)); OnPropertyChanged(nameof(HasOperationStatus)); } } }
    private string _actionStatus = "";
    public string ActionStatus { get => _actionStatus; set { if (SetProperty(ref _actionStatus, value)) { OnPropertyChanged(nameof(OperationStatus)); OnPropertyChanged(nameof(HasOperationStatus)); } } }
    public string OperationStatus => !string.IsNullOrEmpty(ActionStatus) ? ActionStatus : string.IsNullOrEmpty(PageFileStatus) ? PowerStatus : PageFileStatus;
    public string ActivePowerSchemeName => ActivePowerScheme.Split('\n')[0];
    public string ActivePowerSchemeId => _activePowerId?.ToString() ?? "";
    public string ActivePowerScheme { get => _activePowerScheme; private set { SetProperty(ref _activePowerScheme, value); OnPropertyChanged(nameof(ActivePowerSchemeName)); OnPropertyChanged(nameof(ActivePowerSchemeId)); } }
    public string PowerCheckedAt { get => _powerCheckedAt; private set => SetProperty(ref _powerCheckedAt, value); }
    public string PowerTitle => _loc["Power_Title"];
    public string PowerTabTitle => _loc["System_Power"];
    public string MemoryTabTitle => _loc["System_Memory"];
    public string PowerReview => _loc["Power_Review"];
    public string PowerRefresh => _loc["Power_Refresh"];
    public string PowerRestore => _loc["Power_Restore"];
    public string PowerNotice => _loc["Power_Notice"];

    public string PageTitle => _loc["SysSet_Title"];
    public string PageSubtitle => _loc["SysSet_Subtitle"];
    public string BadgeDev => _loc["SysSet_Badge_Dev"];
    public string HeroTitle => _loc["SysSet_Hero_Title"];
    public string LowLevelHeader => _loc["SysSet_LowLevel_Header"];
    public string HeroDescription => _loc["SysSet_Hero_Description"];
    public string PlannedSectionHeader => _loc["SysSet_Planned_Section"];

    public string Feature1Title => _loc["SysSet_Feature1_Title"];
    public string Feature1Desc => _loc["SysSet_Feature1_Desc"];
    public string Feature1Badge => _loc["SysSet_Feature1_Badge"];

    public string Feature2Title => _loc["SysSet_Feature2_Title"];
    public string Feature2Desc => _loc["SysSet_Feature2_Desc"];
    public string Feature2Badge => _loc["SysSet_Feature2_Badge"];

    public string Feature3Title => _loc["SysSet_Feature3_Title"];
    public string Feature3Desc => _loc["SysSet_Feature3_Desc"];
    public string Feature3Badge => _loc["SysSet_Feature3_Badge"];

    public string BannerNotice => _loc["SysSet_Banner_Notice"];
    public string BannerBtn => _loc["SysSet_Banner_Btn"];

    public SystemSettingsViewModel(ILocalizationService localizationService, INavigationService navigationService,
        IPowerConfigurationReader powerReader, IPowerSchemeService powerService, IMemoryConfigurationReader memoryReader,
        IPageFileService pageFiles, ISystemDiagnosticsService diagnostics)
    {
        _loc = localizationService;
        _nav = navigationService;
        _powerReader = powerReader;
        _powerService = powerService;
        _memoryReader = memoryReader;
        _pageFiles = pageFiles;
        _diagnostics = diagnostics;

        _loc.LanguageChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(PageSubtitle));
            OnPropertyChanged(nameof(BadgeDev));
            OnPropertyChanged(nameof(HeroTitle));
            OnPropertyChanged(nameof(LowLevelHeader));
            OnPropertyChanged(nameof(HeroDescription));
            OnPropertyChanged(nameof(PlannedSectionHeader));
            OnPropertyChanged(nameof(Feature1Title));
            OnPropertyChanged(nameof(Feature1Desc));
            OnPropertyChanged(nameof(Feature1Badge));
            OnPropertyChanged(nameof(Feature2Title));
            OnPropertyChanged(nameof(Feature2Desc));
            OnPropertyChanged(nameof(Feature2Badge));
            OnPropertyChanged(nameof(Feature3Title));
            OnPropertyChanged(nameof(Feature3Desc));
            OnPropertyChanged(nameof(Feature3Badge));
            OnPropertyChanged(nameof(BannerNotice));
            OnPropertyChanged(nameof(BannerBtn));
            OnPropertyChanged(nameof(PowerTitle));
            OnPropertyChanged(nameof(PowerReview));
            OnPropertyChanged(nameof(PowerRefresh));
            OnPropertyChanged(nameof(PowerRestore));
            OnPropertyChanged(nameof(PowerNotice));
            _ = RefreshPowerAsync();
            RenderMemory();
            NotifySystemLabels();
        };
    }

    public async Task RefreshPowerAsync()
    {
        if (IsPowerBusy) return;
        IsPowerBusy = true;
        try { await ReadPowerAsync(); }
        catch (Exception ex) { App.Log($"[Power refresh]: {ex}"); PowerStatus = _loc["Power_Result_ReadFailed"]; }
        finally { IsPowerBusy = false; }
    }

    private async Task ReadPowerAsync()
    {
        var catalog = await _powerReader.ReadSchemesAsync();
        var pending = await _powerService.GetPendingAsync();
        var selectedId = SelectedPowerScheme?.Id;
        _activePowerId = catalog.ActiveSchemeId;
        PowerSchemes.Clear();
        foreach (var scheme in catalog.Schemes) PowerSchemes.Add(scheme);
        SelectedPowerScheme = System.Linq.Enumerable.FirstOrDefault(PowerSchemes, s => s.Id == selectedId)
            ?? System.Linq.Enumerable.FirstOrDefault(PowerSchemes, s => s.Id == catalog.ActiveSchemeId);
        var active = System.Linq.Enumerable.FirstOrDefault(PowerSchemes, s => s.Id == catalog.ActiveSchemeId);
        ActivePowerScheme = catalog.ActiveSchemeId.HasValue ? $"{active?.Name}\n{catalog.ActiveSchemeId}" : _loc["Power_Result_ReadFailed"];
        PowerCheckedAt = catalog.CheckedAtUtc.ToLocalTime().ToString("g");
        NotifyDraftState();
        PowerStatus = catalog.ActiveSchemeError != null || catalog.EnumerationError != null
            ? _loc["Power_Result_ReadFailed"] : pending == null ? "" : _loc["Power_Result_RecoveryRequired"];
    }

    public async Task<PowerSchemePreview?> ReviewPowerAsync()
    {
        if (IsPowerBusy || SelectedPowerScheme == null) return null;
        IsPowerBusy = true;
        try { return await _powerService.PreviewAsync(SelectedPowerScheme.Id); }
        catch (Exception ex) { App.Log($"[Power preview]: {ex}"); PowerStatus = _loc["Power_Result_ReadFailed"]; return null; }
        finally { IsPowerBusy = false; }
    }

    public async Task ApplyPowerAsync(PowerSchemePreview preview)
    {
        if (IsPowerBusy) return;
        IsPowerBusy = true;
        try
        {
            var result = await _powerService.ApplyAsync(preview);
            await ReadPowerAsync();
            PowerStatus = _loc["Power_Result_" + result.Outcome];
        }
        catch (Exception ex) { App.Log($"[Power apply]: {ex}"); PowerStatus = _loc["Power_Result_JournalFailed"]; }
        finally { IsPowerBusy = false; }
    }

    public string InspectorText
    {
        get
        {
            string selection = ActiveTab == 0 && SelectedPowerScheme != null ? SelectedPowerScheme.DisplayName + "\n" + SelectedPowerScheme.Id + "\n\n"
                : ActiveTab == 1 ? _loc["Memory_Automatic"] + ": " + _loc[AutomaticPageFile ? "Memory_On" : "Memory_Off"] + "\n"
                    + string.Join("\n", PageFileEditors.Select(e => e.Path + ": " + e.Modes[e.Mode] + (e.IsCustom ? $" {e.Initial} / {e.Maximum} MiB" : ""))) + "\n\n" : "";
            return selection + _loc["System_Inspector"];
        }
    }
    public string ReviewLabel => _loc["System_Review"];
    public string DiscardLabel => _loc["System_Discard"];

    public async Task RestorePowerAsync()
    {
        if (IsPowerBusy) return;
        IsPowerBusy = true;
        try
        {
            var pending = await _powerService.GetPendingAsync();
            var result = pending == null ? new PowerSchemeResult(PowerSchemeOutcome.NotFound)
                : await _powerService.RestoreAsync(pending.HistoryId);
            await ReadPowerAsync();
            PowerStatus = _loc["Power_Result_" + result.Outcome];
        }
        catch (Exception ex) { App.Log($"[Power restore]: {ex}"); PowerStatus = _loc["Power_Result_JournalFailed"]; }
        finally { IsPowerBusy = false; }
    }

    [RelayCommand]
    private void NavigateToTweaks()
    {
        _nav.Navigate(typeof(TweaksPage));
    }
}
