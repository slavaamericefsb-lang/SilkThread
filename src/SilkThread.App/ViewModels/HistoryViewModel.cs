using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SilkThread.App.Services;
using SilkThread.Core.BackupManager;
using SilkThread.Core.Models;
using SilkThread.Core.TweakEngine;
using SilkThread.Core.SystemOperations;

namespace SilkThread.App.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly IBackupManager _backupManager;
    private readonly ITweakEngine _tweakEngine;
    private readonly ILocalizationService _loc;
    private readonly IPowerSchemeService? _powerSchemes;

    public ObservableCollection<TweakAuditRecord> HistoryRecords { get; } = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _statusMessage;

    // Localized properties
    public string PageTitle => _loc["Hist_Title"];
    public string PageSubtitle => _loc["Hist_Subtitle"];
    public string RollbackBtnLabel => _loc["Hist_Rollback_Btn"];
    public string ClearBtnLabel => _loc["Hist_Clear_Btn"];
    public string ColTime => _loc["Hist_Col_Time"];
    public string ColTweak => _loc["Hist_Col_Tweak"];
    public string ColCategory => _loc["Hist_Col_Category"];
    public string ColStatus => _loc["Hist_Col_Status"];
    public string ColAction => _loc["Hist_Col_Action"];
    public string EmptyLabel => _loc["Hist_Empty"];
    public string RefreshBtnLabel => _loc["Hist_Refresh_Btn"];
    public string EmptyTitleLabel => _loc["Hist_Empty_Title"];
    public bool HasNoRecords => HistoryRecords.Count == 0;

    public HistoryViewModel(
        IBackupManager backupManager,
        ITweakEngine tweakEngine,
        ILocalizationService localizationService,
        IPowerSchemeService? powerSchemes = null)
    {
        _backupManager = backupManager;
        _tweakEngine = tweakEngine;
        _loc = localizationService;
        _powerSchemes = powerSchemes;

        _loc.LanguageChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(PageSubtitle));
            OnPropertyChanged(nameof(RollbackBtnLabel));
            OnPropertyChanged(nameof(ClearBtnLabel));
            OnPropertyChanged(nameof(ColTime));
            OnPropertyChanged(nameof(ColTweak));
            OnPropertyChanged(nameof(ColCategory));
            OnPropertyChanged(nameof(ColStatus));
            OnPropertyChanged(nameof(ColAction));
            OnPropertyChanged(nameof(EmptyLabel));
            OnPropertyChanged(nameof(RefreshBtnLabel));
            OnPropertyChanged(nameof(EmptyTitleLabel));
            _ = Task.Run(async () =>
            {
                try { await LoadHistoryAsync(); }
                catch (Exception ex) { App.Log($"[HistoryViewModel LangChanged Load error]: {ex.Message}"); }
            });
        };
    }

    private void RunOnUI(Action action)
    {
        var dq = App.MainDispatcherQueue ?? App.MainWindowInstance?.DispatcherQueue;
        if (dq != null && !dq.HasThreadAccess)
        {
            dq.TryEnqueue(() =>
            {
                try { action(); }
                catch (Exception ex) { App.Log($"[HistoryViewModel RunOnUI Exception]: {ex}"); }
            });
        }
        else
        {
            try { action(); }
            catch (Exception ex) { App.Log($"[HistoryViewModel RunOnUI Exception]: {ex}"); }
        }
    }

    public async Task InitializeAsync()
    {
        await LoadHistoryAsync();
    }

    [RelayCommand]
    public async Task LoadHistoryAsync()
    {
        RunOnUI(() => IsLoading = true);
        try
        {
            var records = await _backupManager.GetAuditHistoryAsync(150);
            RunOnUI(() =>
            {
                HistoryRecords.Clear();
                foreach (var r in records)
                {
                    HistoryRecords.Add(r);
                }
                OnPropertyChanged(nameof(HasNoRecords));
            });
        }
        catch (Exception ex)
        {
            App.Log($"[HistoryViewModel LoadHistoryAsync error]: {ex.Message}");
        }
        finally
        {
            RunOnUI(() => IsLoading = false);
        }
    }

    [RelayCommand]
    public async Task RollbackRecordAsync(TweakAuditRecord record)
    {
        if (record.IsRolledBack) return;

        RunOnUI(() => IsLoading = true);
        try
        {
            if (record.TweakId == PageFileService.Resource)
            {
                RunOnUI(() => StatusMessage = _loc["Page_ReviewRequired"]);
            }
            else if (record.TweakId == "system_power_scheme" && _powerSchemes != null)
            {
                var res = await _powerSchemes.RestoreAsync(record.Id);
                RunOnUI(() => StatusMessage = _loc["Power_Result_" + res.Outcome]);
            }
            else
            {
                var res = await _tweakEngine.RollbackTweakAsync(record.TweakId);
                RunOnUI(() => StatusMessage = res.Message);
            }
            await LoadHistoryAsync();
        }
        catch (Exception ex)
        {
            App.Log($"[HistoryViewModel RollbackRecordAsync error]: {ex.Message}");
        }
        finally
        {
            RunOnUI(() => IsLoading = false);
        }
    }

    [RelayCommand]
    public async Task ClearHistoryAsync()
    {
        try
        {
            await _backupManager.ClearHistoryAsync();
            await LoadHistoryAsync();
            RunOnUI(() => StatusMessage = _loc["Hist_Cleared_Status"]);
        }
        catch (Exception ex)
        {
            App.Log($"[HistoryViewModel ClearHistoryAsync error]: {ex.Message}");
        }
    }
}
