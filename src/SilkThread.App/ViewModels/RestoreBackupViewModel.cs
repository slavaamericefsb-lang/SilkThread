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
    public bool IsProtectionActive => Status?.SystemDriveProtected == true;
    public bool IsProtectionInactive => !IsProtectionActive;
    public string ProtectionStatusBadge => Status?.SystemDriveProtected == true 
        ? (_loc.CurrentLanguage == "ru" ? "ВКЛЮЧЕНА" : "ACTIVE") 
        : (_loc.CurrentLanguage == "ru" ? "ВЫКЛЮЧЕНА" : "DISABLED");
    public int RestorePointsCount => RestorePoints.Count;

    [ObservableProperty]
    private string _newRestorePointName = "SilkThread Резервная копия";

    [ObservableProperty]
    private bool _isBusy;

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
    public bool HasNoPoints => RestorePoints.Count == 0;
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

    public string EmptyTitleLabel => Status?.SystemDriveProtected == false
        ? (_loc.CurrentLanguage == "ru" ? "Защита системы выключена" : "System Protection is Disabled")
        : (_loc.CurrentLanguage == "ru" ? "Точки восстановления не найдены" : "No Restore Points Found");

    public string EmptyDescriptionLabel => Status?.SystemDriveProtected == false
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

        _loc.LanguageChanged += (s, e) =>
        {
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

    public async Task InitializeAsync()
    {
        await RefreshStatusAsync();
        await LoadRestorePointsAsync();
    }

    [RelayCommand]
    public async Task RefreshStatusAsync()
    {
        Status = await _restorePointManager.CheckStatusAsync();
    }

    [RelayCommand]
    public async Task LoadRestorePointsAsync()
    {
        IsBusy = true;
        try
        {
            var points = await _restorePointManager.GetRestorePointsAsync();
            RestorePoints.Clear();
            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];
                RestorePoints.Add(p with { IsNewest = (i == 0) });
            }
            OnPropertyChanged(nameof(HasNoPoints));
            OnPropertyChanged(nameof(RestorePointsCount));
        }
        finally
        {
            IsBusy = false;
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
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void OpenWindowsRestoreUi()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "rstrui.exe",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            OperationMessage = $"Не удалось открыть rstrui.exe: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task RollbackRestorePointAsync(RestorePointItem point)
    {
        OpenWindowsRestoreUi();
        OperationMessage = _loc.CurrentLanguage == "ru"
            ? $"Запущен мастер отката Windows для контрольной точки #{point.SequenceNumber} ({point.Description})."
            : $"Launched Windows System Restore wizard for point #{point.SequenceNumber} ({point.Description}).";
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
        finally
        {
            IsBusy = false;
        }
    }
}
