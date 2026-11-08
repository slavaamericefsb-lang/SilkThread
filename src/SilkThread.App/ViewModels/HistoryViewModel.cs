using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SilkThread.App.Services;
using SilkThread.Core.BackupManager;
using SilkThread.Core.Models;
using SilkThread.Core.TweakEngine;

namespace SilkThread.App.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly IBackupManager _backupManager;
    private readonly ITweakEngine _tweakEngine;
    private readonly ILocalizationService _loc;

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
        ILocalizationService localizationService)
    {
        _backupManager = backupManager;
        _tweakEngine = tweakEngine;
        _loc = localizationService;

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
            _ = LoadHistoryAsync();
        };
    }

    public async Task InitializeAsync()
    {
        await LoadHistoryAsync();
    }

    [RelayCommand]
    public async Task LoadHistoryAsync()
    {
        IsLoading = true;
        try
        {
            var records = await _backupManager.GetAuditHistoryAsync(150);
            HistoryRecords.Clear();
            foreach (var r in records)
            {
                HistoryRecords.Add(r);
            }
            OnPropertyChanged(nameof(HasNoRecords));
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task RollbackRecordAsync(TweakAuditRecord record)
    {
        if (record.IsRolledBack) return;

        IsLoading = true;
        try
        {
            var res = await _tweakEngine.RollbackTweakAsync(record.TweakId);
            StatusMessage = res.Message;
            await LoadHistoryAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task ClearHistoryAsync()
    {
        await _backupManager.ClearHistoryAsync();
        HistoryRecords.Clear();
        OnPropertyChanged(nameof(HasNoRecords));
        StatusMessage = _loc.CurrentLanguage == "ru" ? "Журнал аудита успешно очищен." : "Audit history log cleared.";
    }
}
