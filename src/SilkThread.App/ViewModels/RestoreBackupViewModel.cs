using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SilkThread.App.Services;
using SilkThread.Core.BackupManager;
using SilkThread.Core.Models;
using SilkThread.Core.RestorePointManager;

namespace SilkThread.App.ViewModels;

public partial class RestoreBackupViewModel : ObservableObject
{
    private readonly IRestorePointManager _restorePointManager;
    private readonly IBackupManager _backupManager;
    private readonly ILocalizationService _loc;
    private readonly System.Threading.SemaphoreSlim _readGate = new(1, 1);

    public ObservableCollection<RestorePointItem> RestorePoints { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusDescription))]
    [NotifyPropertyChangedFor(nameof(IsProtectionActive))]
    [NotifyPropertyChangedFor(nameof(IsProtectionInactive))]
    [NotifyPropertyChangedFor(nameof(ProtectionStatusBadge))]
    [NotifyPropertyChangedFor(nameof(EmptyTitleLabel))]
    [NotifyPropertyChangedFor(nameof(EmptyDescriptionLabel))]
    private RestorePointStatus? _status;

    public string StatusDescription => Status?.StatusDescription ?? "Служба защиты системы готова к созданию контрольных точек.";
    public bool IsProtectionActive => Status is { ProtectionStateKnown: true, SystemDriveProtected: true };
    public bool IsProtectionInactive => Status is { ProtectionStateKnown: true, SystemDriveProtected: false };
    public string ProtectionStatusBadge => Status?.ProtectionStateKnown != true ? (_loc.CurrentLanguage == "ru" ? "НЕ ОПРЕДЕЛЕНО" : "UNKNOWN") : Status?.SystemDriveProtected == true 
        ? (_loc.CurrentLanguage == "ru" ? "ВКЛЮЧЕНА" : "ACTIVE") 
        : (_loc.CurrentLanguage == "ru" ? "ВЫКЛЮЧЕНА" : "DISABLED");
    public int RestorePointsCount => RestorePoints.Count;
    public string RestorePointsCountDisplay => !_hasSuccessfulRead || HasListReadError ? "—" : RestorePointsCount.ToString();

    [ObservableProperty]
    private string _newRestorePointName = "SilkThread Резервная копия";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoPoints))]
    private bool _isBusy;

    private bool _hasSuccessfulRead;
    private string? _listReadError;
    public bool HasListReadError => _listReadError != null;
    public string? ListReadError => _listReadError;

    [ObservableProperty]
    private string? _operationMessage;

    // Localized properties
    public string PageTitle => _loc["Rest_Title"];
    public string PageSubtitle => _loc["Rest_Subtitle"];
    public string CreateBtnLabel => _loc["Rest_Create_Btn"];
    public string OpenSysRestoreLabel => _loc["Rest_Open_SysRestore"];
    public string PlaceholderText => _loc["Rest_Name_Placeholder"];
    public string ListTitle => _loc["Rest_List_Title"];
    public string EmptyLabel => _loc["Rest_Empty"];
    public bool HasNoPoints => _hasSuccessfulRead && !HasListReadError && !IsBusy && RestorePoints.Count == 0;
    public string ProtectionCardTitle => _loc.CurrentLanguage == "ru" ? "Защита системы Windows (System Restore)" : "Windows System Protection";
    public string CheckBtnLabel => _loc.CurrentLanguage == "ru" ? "Проверить" : "Check Status";
    public string CreateCardTitle => _loc.CurrentLanguage == "ru" ? "Создать контрольную точку восстановления" : "Create System Restore Point";
    public string CreateCardDesc => _loc.CurrentLanguage == "ru" 
        ? "Создает моментальный снимок реестра, системных драйверов и настроек безопасности Windows." 
        : "Creates an instant snapshot of Windows registry, drivers, and security settings.";

    public string SubProtectionLabel => _loc["Rest_Sub_Protection"];
    public string SubStatusLabel => _loc["Rest_Sub_Status"];
    public string RstruiTooltip => _loc["Rest_Rstrui_Tooltip"];
    public string SubSnapshotLabel => _loc["Rest_Sub_Snapshot"];
    public string SubTimelineLabel => _loc["Rest_Sub_Timeline"];
    public string SubPointsCountLabel => _loc["Rest_Sub_PointsCount"];
    public string RefreshListLabel => _loc["Rest_Refresh_Btn"];
    public string RollbackBtnLabel => _loc["Rest_Rollback_Btn"];
    public string DeleteBtnLabel => _loc["Rest_Delete_Btn"];

    public string EmptyTitleLabel => Status is { ProtectionStateKnown: true, SystemDriveProtected: false }
        ? (_loc.CurrentLanguage == "ru" ? "Защита системы выключена" : "System Protection is Disabled")
        : (_loc.CurrentLanguage == "ru" ? "Точки восстановления не найдены" : "No Restore Points Found");

    public string EmptyDescriptionLabel => Status is { ProtectionStateKnown: true, SystemDriveProtected: false }
        ? (_loc.CurrentLanguage == "ru"
            ? "Защита системного диска Windows отключена. Включите защиту системы для возможности создания контрольных точек."
            : "System drive protection is disabled in Windows settings. Enable protection or create the first restore point.")
        : (_loc.CurrentLanguage == "ru"
            ? "В системе пока нет контрольных точек восстановления. Нажмите кнопку ниже, чтобы создать первую контрольную точку перед оптимизацией."
            : "No system restore points found. Click below to create your first restore point before optimization.");

    public string CreateFirstPointLabel => _loc.CurrentLanguage == "ru" ? "Создать первую точку восстановления" : "Create First Restore Point";
    public string EnableProtectionLabel => _loc.CurrentLanguage == "ru" ? "Включить защиту диска C:" : "Enable System Protection (C:)";

    public RestoreBackupViewModel(
        IRestorePointManager restorePointManager,
        IBackupManager backupManager,
        ILocalizationService localizationService)
    {
        _restorePointManager = restorePointManager;
        _backupManager = backupManager;
        _loc = localizationService;
        _newRestorePointName = _loc["Rest_DefaultPointName"];

        _loc.LanguageChanged += (s, e) =>
        {
            if (NewRestorePointName == "SilkThread Резервная копия" || NewRestorePointName == "SilkThread Backup Point")
            {
                NewRestorePointName = _loc["Rest_DefaultPointName"];
            }

            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(PageSubtitle));
            OnPropertyChanged(nameof(CreateBtnLabel));
            OnPropertyChanged(nameof(OpenSysRestoreLabel));
            OnPropertyChanged(nameof(PlaceholderText));
            OnPropertyChanged(nameof(ListTitle));
            OnPropertyChanged(nameof(EmptyLabel));
            OnPropertyChanged(nameof(ProtectionCardTitle));
            OnPropertyChanged(nameof(CheckBtnLabel));
            OnPropertyChanged(nameof(CreateCardTitle));
            OnPropertyChanged(nameof(CreateCardDesc));
            OnPropertyChanged(nameof(StatusDescription));
            OnPropertyChanged(nameof(ProtectionStatusBadge));
            OnPropertyChanged(nameof(SubProtectionLabel));
            OnPropertyChanged(nameof(SubStatusLabel));
            OnPropertyChanged(nameof(RstruiTooltip));
            OnPropertyChanged(nameof(SubSnapshotLabel));
            OnPropertyChanged(nameof(SubTimelineLabel));
            OnPropertyChanged(nameof(SubPointsCountLabel));
            OnPropertyChanged(nameof(RefreshListLabel));
            OnPropertyChanged(nameof(RollbackBtnLabel));
            OnPropertyChanged(nameof(DeleteBtnLabel));
            OnPropertyChanged(nameof(EmptyTitleLabel));
            OnPropertyChanged(nameof(EmptyDescriptionLabel));
            OnPropertyChanged(nameof(CreateFirstPointLabel));
            OnPropertyChanged(nameof(EnableProtectionLabel));
            _ = LoadRestorePointsAsync();
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
                catch (Exception ex) { App.Log($"[RestoreBackupViewModel RunOnUI Exception]: {ex}"); }
            });
        }
        else
        {
            try { action(); }
            catch (Exception ex) { App.Log($"[RestoreBackupViewModel RunOnUI Exception]: {ex}"); }
        }
    }

    public async Task InitializeAsync()
    {
        await RefreshStatusAsync();
        await LoadRestorePointsAsync();
    }

    [RelayCommand]
    public async Task RefreshStatusAsync()
    {
        var status = await _restorePointManager.CheckStatusAsync();
        RunOnUI(() => Status = status);
    }

    [RelayCommand]
    public async Task LoadRestorePointsAsync()
    {
        await _readGate.WaitAsync();
        RunOnUI(() => IsBusy = true);
        try
        {
            var points = await _restorePointManager.GetRestorePointsAsync();
            RunOnUI(() =>
            {
                if (OperationMessage == _listReadError) OperationMessage = null;
                _listReadError = null;
                _hasSuccessfulRead = true;
                OnPropertyChanged(nameof(HasListReadError));
                OnPropertyChanged(nameof(ListReadError)); OnPropertyChanged(nameof(RestorePointsCountDisplay));
                RestorePoints.Clear();
                for (int i = 0; i < points.Count; i++)
                {
                    var p = points[i];
                    RestorePoints.Add(p with { IsNewest = (i == 0) });
                }
                OnPropertyChanged(nameof(HasNoPoints));
                OnPropertyChanged(nameof(RestorePointsCount)); OnPropertyChanged(nameof(RestorePointsCountDisplay));
            });
        }
        catch (Exception ex)
        {
            App.Log($"[RestoreBackupViewModel LoadRestorePointsAsync error]: {ex.Message}");
            RunOnUI(() =>
            {
                _listReadError = ex is UnauthorizedAccessException
                    ? (_loc.CurrentLanguage == "ru" ? "Windows не разрешила чтение точек восстановления. Нажмите «Пользователь» вверху окна, перезапустите от администратора и обновите список. Это не означает, что точек нет." : "Windows denied access to restore points. Use the account button above to restart as administrator, then refresh. This does not mean that no points exist.")
                    : (_loc.CurrentLanguage == "ru" ? "Не удалось прочитать список. Ранее загруженные записи сохранены. Причина: " : "Unable to read the list. Previously loaded entries are retained. Reason: ") + ex.Message;
                OperationMessage = _listReadError;
                OnPropertyChanged(nameof(HasListReadError)); OnPropertyChanged(nameof(ListReadError)); OnPropertyChanged(nameof(RestorePointsCountDisplay)); OnPropertyChanged(nameof(HasNoPoints));
            });
        }
        finally
        {
            RunOnUI(() => IsBusy = false);
            _readGate.Release();
        }
    }

    [RelayCommand]
    public async Task CreateRestorePointAsync()
    {
        if (string.IsNullOrWhiteSpace(NewRestorePointName)) return;

        IsBusy = true;
        OperationMessage = _loc.CurrentLanguage == "ru" ? "Создание точки восстановления..." : "Creating system restore point...";

        try
        {
            var res = await _restorePointManager.CreateRestorePointAsync(NewRestorePointName);
            OperationMessage = res.Message;
            await LoadRestorePointsAsync();
        }
        catch (Exception ex)
        {
            App.Log($"[RestoreBackup operation error]: {ex.Message}");
            OperationMessage = _loc.CurrentLanguage == "ru"
                ? $"Операция не выполнена: {ex.Message}"
                : $"Operation failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void OpenWindowsRestoreUi() => TryOpenWindowsRestoreUi();

    private bool TryOpenWindowsRestoreUi()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "rstrui.exe",
                UseShellExecute = true
            });
            return true;
        }
        catch (Exception ex)
        {
            OperationMessage = _loc.CurrentLanguage == "ru"
                ? $"Не удалось открыть rstrui.exe: {ex.Message}"
                : $"Could not open rstrui.exe: {ex.Message}";
            return false;
        }
    }

    [RelayCommand]
    public async Task RollbackRestorePointAsync(RestorePointItem point)
    {
        if (!TryOpenWindowsRestoreUi()) return;
        OperationMessage = _loc.CurrentLanguage == "ru"
            ? $"Открыт мастер восстановления Windows. Выберите точку #{point.SequenceNumber} ({point.Description}) в мастере."
            : $"Opened Windows System Restore. Select point #{point.SequenceNumber} ({point.Description}) in the wizard.";
        await Task.CompletedTask;
    }

    [RelayCommand]
    public async Task DeleteRestorePointAsync(RestorePointItem point)
    {
        IsBusy = true;
        try
        {
            var res = await _restorePointManager.DeleteRestorePointAsync(point.SequenceNumber);
            OperationMessage = res.Message;
            await LoadRestorePointsAsync();
        }
        catch (Exception ex)
        {
            App.Log($"[RestoreBackup operation error]: {ex.Message}");
            OperationMessage = _loc.CurrentLanguage == "ru"
                ? $"Операция не выполнена: {ex.Message}"
                : $"Operation failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task EnableProtectionAsync()
    {
        IsBusy = true;
        OperationMessage = _loc.CurrentLanguage == "ru" ? "Включение защиты системы на диске C:..." : "Enabling system protection on drive C:...";
        try
        {
            var res = await _restorePointManager.EnableProtectionAsync("C:\\");
            OperationMessage = res.Message;
            await RefreshStatusAsync();
            await LoadRestorePointsAsync();
        }
        catch (Exception ex)
        {
            App.Log($"[RestoreBackup operation error]: {ex.Message}");
            OperationMessage = _loc.CurrentLanguage == "ru"
                ? $"Операция не выполнена: {ex.Message}"
                : $"Operation failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
