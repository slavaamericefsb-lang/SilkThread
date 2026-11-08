using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using SilkThread.App.Services;

namespace SilkThread.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly ILocalizationService _loc;

    [ObservableProperty]
    private int _languageIndex; // 0 = Русский, 1 = English

    [ObservableProperty]
    private bool _confirmDangerousTweaks;

    [ObservableProperty]
    private bool _autoCreateRestorePoint;

    [ObservableProperty]
    private bool _hideIncompatibleTweaks;

    [ObservableProperty]
    private string _openRouterApiKey = "";

    [ObservableProperty]
    private string _openRouterModel = "liquid/lfm-3-1.7b:free";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LockIcon))]
    [NotifyPropertyChangedFor(nameof(LockTooltipText))]
    [NotifyPropertyChangedFor(nameof(ApiKeyBoxOpacity))]
    private bool _isApiKeyUnlocked = false;

    public string LockIcon => IsApiKeyUnlocked ? "\uE785" : "\uE72E";

    public string LockTooltipText => IsApiKeyUnlocked
        ? (_loc.CurrentLanguage == "ru" ? "Сохранить и заблокировать" : "Save and lock")
        : (_loc.CurrentLanguage == "ru" ? "Разблокировать для редактирования" : "Unlock to edit");

    public double ApiKeyBoxOpacity => IsApiKeyUnlocked ? 1.0 : 0.65;

    public string ActiveModelDisplayName => _loc.CurrentLanguage == "ru"
        ? "Нейросетевой агент оптимизации"
        : "AI Tuning Engine";

    public string PageTitle => _loc["Set_Title"];
    public string PageSubtitle => _loc["Set_Subtitle"];
    public string LangLabel => _loc["Set_Lang_Label"];
    public string ConfirmDangerousLabel => _loc["Set_Confirm_Dangerous"];
    public string AutoRestoreLabel => _loc["Set_Auto_Restore"];
    public string HideIncompatibleLabel => _loc["Set_Hide_Incompatible"];
    public string RebootNotifyLabel => _loc["Set_Reboot_Notify"];
    public string AboutTitle => _loc["Set_About_Title"];
    public string AboutDesc => _loc["Set_About_Desc"];
    public string VersionLabel => _loc["Set_Version"];
    public string UpdateHistoryButtonText => _loc["UpdHist_Btn"];
    public string CheckUpdatesButtonText => _loc["Update_CheckButton"];

    public string SectionLangTitle => _loc.CurrentLanguage == "ru" ? "Язык интерфейса" : "Interface Language";
    public string SectionLangDesc => _loc.CurrentLanguage == "ru" ? "Выберите основной язык интерфейса SilkThread." : "Select your preferred application language.";
    public string SectionSafetyTitle => _loc.CurrentLanguage == "ru" ? "Безопасность и откат" : "Safety & Safeguards";
    public string SectionAboutTitle => _loc.CurrentLanguage == "ru" ? "О программе" : "About SilkThread";

    public string SectionAiTitle => _loc.CurrentLanguage == "ru" ? "Параметры OpenRouter AI" : "OpenRouter AI Settings";
    public string SectionAiDesc => _loc.CurrentLanguage == "ru"
        ? "Конфигурация нейросетевого ассистента для персональной генерации пресетов."
        : "Configuration for neural tuning copilot and preset generation.";
    public string OpenRouterApiKeyLabel => _loc.CurrentLanguage == "ru" ? "API-ключ OpenRouter" : "OpenRouter API Key";
    public string OpenRouterApiKeyDesc => _loc.CurrentLanguage == "ru"
        ? "Ключ доступа к API OpenRouter для отправки запросов к нейросетевой модели."
        : "OpenRouter access key used for neural engine inference queries.";
    public string ActiveModelLabel => _loc.CurrentLanguage == "ru" ? "Активная нейросетевая модель" : "Active Neural Model";
    public string ActiveModelDesc => _loc.CurrentLanguage == "ru"
        ? "Нейросетевая архитектура — адаптивное рассуждение о твиках Windows."
        : "Neural architecture — adaptive reasoning over Windows tweaks.";
    public string ModelTierBadge => _loc.CurrentLanguage == "ru" ? "FREE TIER АКТИВЕН" : "FREE TIER ACTIVE";
    public string ModelStatusText => _loc.CurrentLanguage == "ru" ? "Готов к работе (OpenRouter Cloud)" : "Ready (OpenRouter Cloud)";

    public string ConfirmDangerousDesc => _loc.CurrentLanguage == "ru" 
        ? "Перед применением критических настроек ядра или служб запрашивать дополнительное подтверждение." 
        : "Prompt for confirmation before applying critical core system or service tweaks.";
    public string AutoRestoreDesc => _loc.CurrentLanguage == "ru" 
        ? "Создавать точку восстановления Windows перед массовым применением оптимизаций." 
        : "Automatically create a Windows restore point before running batch optimizations.";
    public string HideIncompatibleDesc => _loc.CurrentLanguage == "ru" 
        ? "Автоматически скрывать твики для других архитектур GPU/CPU или несовместимых накопителей." 
        : "Automatically hide tweaks intended for other CPU/GPU architectures or incompatible drives.";

    public string AppVersion => "SilkThread 2.0 (Windows 11 x64)";
    public string RuntimeEnvironment => $".NET {Environment.Version} (WinUI 3 / Windows App SDK)";

    public string SubSafetyLabel => _loc["Set_Sub_Safety"];
    public string SubAiLabel => _loc["Set_Sub_Ai"];
    public string SubLangLabel => _loc["Set_Sub_Lang"];
    public string SubLangSwitchLabel => _loc["Set_Sub_LangSwitch"];
    public string SubAboutLabel => _loc["Set_Sub_About"];

    public SettingsViewModel(ISettingsService settingsService, ILocalizationService localizationService)
    {
        _settingsService = settingsService;
        _loc = localizationService;

        _languageIndex = _loc.CurrentLanguage == "en" ? 1 : 0;
        _confirmDangerousTweaks = _settingsService.Settings.ConfirmDangerousTweaks;
        _autoCreateRestorePoint = _settingsService.Settings.AutoCreateRestorePoint;
        _hideIncompatibleTweaks = _settingsService.Settings.HideIncompatibleTweaks;
        _openRouterApiKey = _settingsService.Settings.OpenRouterApiKey;
        _openRouterModel = _settingsService.Settings.OpenRouterModel;

        // Force Dark Theme (Mica Dark)
        _settingsService.Settings.AppTheme = ElementTheme.Dark;

        _loc.LanguageChanged += (s, e) =>
        {
            _languageIndex = _loc.CurrentLanguage == "en" ? 1 : 0;
            OnPropertyChanged(nameof(LanguageIndex));
            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(PageSubtitle));
            OnPropertyChanged(nameof(LangLabel));
            OnPropertyChanged(nameof(ConfirmDangerousLabel));
            OnPropertyChanged(nameof(AutoRestoreLabel));
            OnPropertyChanged(nameof(HideIncompatibleLabel));
            OnPropertyChanged(nameof(RebootNotifyLabel));
            OnPropertyChanged(nameof(AboutTitle));
            OnPropertyChanged(nameof(AboutDesc));
            OnPropertyChanged(nameof(VersionLabel));
            OnPropertyChanged(nameof(UpdateHistoryButtonText));
            OnPropertyChanged(nameof(CheckUpdatesButtonText));
            OnPropertyChanged(nameof(SectionLangTitle));
            OnPropertyChanged(nameof(SectionLangDesc));
            OnPropertyChanged(nameof(SectionSafetyTitle));
            OnPropertyChanged(nameof(SectionAboutTitle));
            OnPropertyChanged(nameof(SectionAiTitle));
            OnPropertyChanged(nameof(SectionAiDesc));
            OnPropertyChanged(nameof(OpenRouterApiKeyLabel));
            OnPropertyChanged(nameof(OpenRouterApiKeyDesc));
            OnPropertyChanged(nameof(ActiveModelLabel));
            OnPropertyChanged(nameof(ActiveModelDesc));
            OnPropertyChanged(nameof(ModelTierBadge));
            OnPropertyChanged(nameof(ModelStatusText));
            OnPropertyChanged(nameof(ConfirmDangerousDesc));
            OnPropertyChanged(nameof(AutoRestoreDesc));
            OnPropertyChanged(nameof(HideIncompatibleDesc));
            OnPropertyChanged(nameof(ActiveModelDisplayName));
            OnPropertyChanged(nameof(SubSafetyLabel));
            OnPropertyChanged(nameof(SubAiLabel));
            OnPropertyChanged(nameof(SubLangLabel));
            OnPropertyChanged(nameof(SubLangSwitchLabel));
            OnPropertyChanged(nameof(SubAboutLabel));
            OnPropertyChanged(nameof(LockTooltipText));
        };
    }

    [RelayCommand]
    private void ToggleApiKeyLock()
    {
        if (IsApiKeyUnlocked)
        {
            _settingsService.Settings.OpenRouterApiKey = OpenRouterApiKey;
            _settingsService.SaveSettings();
            IsApiKeyUnlocked = false;
        }
        else
        {
            IsApiKeyUnlocked = true;
        }
    }

    partial void OnLanguageIndexChanged(int value)
    {
        string target = value == 1 ? "en" : "ru";
        _loc.SetLanguage(target);
    }

    partial void OnConfirmDangerousTweaksChanged(bool value)
    {
        _settingsService.Settings.ConfirmDangerousTweaks = value;
        _settingsService.Save();
    }

    partial void OnAutoCreateRestorePointChanged(bool value)
    {
        _settingsService.Settings.AutoCreateRestorePoint = value;
        _settingsService.Save();
    }

    partial void OnHideIncompatibleTweaksChanged(bool value)
    {
        _settingsService.Settings.HideIncompatibleTweaks = value;
        _settingsService.Save();
    }

    partial void OnOpenRouterApiKeyChanged(string value)
    {
        _settingsService.Settings.OpenRouterApiKey = value;
        _settingsService.Save();
    }
}

public partial class ShellViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private string _title = "SilkThread — Windows Gaming Optimizer";

    public ShellViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }
}
