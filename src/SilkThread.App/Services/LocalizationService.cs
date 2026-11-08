using System;
using System.Collections.Generic;

namespace SilkThread.App.Services;

public interface ILocalizationService
{
    string CurrentLanguage { get; }
    void SetLanguage(string lang);
    event EventHandler? LanguageChanged;

    string this[string key] { get; }
    string GetString(string key);

    string GetCategoryName(SilkThread.Core.Models.TweakCategory category);
    string GetCategoryDescription(SilkThread.Core.Models.TweakCategory category);
    string GetRiskName(SilkThread.Core.Models.RiskLevel risk);
    string GetRecommendationName(SilkThread.Core.Models.RecommendationLevel rec);
    string GetStatusName(SilkThread.Core.Models.TweakStatus status);
}

public class LocalizationService : ILocalizationService
{
    private static LocalizationService? _instance;
    public static LocalizationService Instance => _instance ??= new LocalizationService(null);

    private readonly ISettingsService? _settingsService;
    private string _currentLanguage = "ru";

    public string CurrentLanguage => _currentLanguage;

    public event EventHandler? LanguageChanged;

    public LocalizationService(ISettingsService? settingsService)
    {
        _instance = this;
        _settingsService = settingsService;

        if (_settingsService?.Settings.Language is string saved && !string.IsNullOrWhiteSpace(saved))
        {
            _currentLanguage = saved;
        }
    }

    public void SetLanguage(string lang)
    {
        if (_currentLanguage == lang) return;
        _currentLanguage = lang;

        if (_settingsService != null)
        {
            _settingsService.Settings.Language = lang;
            _settingsService.Save();
        }

        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public string this[string key] => GetString(key);

    public string GetString(string key)
    {
        bool isRu = _currentLanguage == "ru";
        if (isRu && RuStrings.TryGetValue(key, out var ruVal)) return ruVal;
        if (EnStrings.TryGetValue(key, out var enVal)) return enVal;
        return key;
    }

    public string GetCategoryName(SilkThread.Core.Models.TweakCategory category)
    {
        bool isRu = _currentLanguage == "ru";
        return category switch
        {
            SilkThread.Core.Models.TweakCategory.CpuOptimization => isRu ? "Оптимизация CPU" : "CPU Optimization",
            SilkThread.Core.Models.TweakCategory.GpuOptimization => isRu ? "Оптимизация GPU" : "GPU Optimization",
            SilkThread.Core.Models.TweakCategory.RamOptimization => isRu ? "Оптимизация RAM" : "RAM Optimization",
            SilkThread.Core.Models.TweakCategory.GamingPerformance => isRu ? "Игровая производительность" : "Gaming Performance",
            SilkThread.Core.Models.TweakCategory.WindowsPerformance => isRu ? "Производительность Windows" : "Windows Performance",
            SilkThread.Core.Models.TweakCategory.InputAndLatency => isRu ? "Ввод и задержки" : "Input & Latency",
            SilkThread.Core.Models.TweakCategory.NetworkOptimization => isRu ? "Оптимизация сети" : "Network Optimization",
            SilkThread.Core.Models.TweakCategory.StorageOptimization => isRu ? "Оптимизация накопителей" : "Storage Optimization",
            SilkThread.Core.Models.TweakCategory.BackgroundProcesses => isRu ? "Фоновые процессы" : "Background Processes",
            SilkThread.Core.Models.TweakCategory.WindowsServices => isRu ? "Службы Windows" : "Windows Services",
            SilkThread.Core.Models.TweakCategory.PowerManagement => isRu ? "Управление питанием" : "Power Management",
            SilkThread.Core.Models.TweakCategory.WindowsExplorerAndUi => isRu ? "Проводник и интерфейс" : "Windows Explorer & UI",
            SilkThread.Core.Models.TweakCategory.SecurityAndPrivacy => isRu ? "Безопасность и приватность" : "Security & Privacy",
            SilkThread.Core.Models.TweakCategory.SystemMaintenance => isRu ? "Обслуживание системы" : "System Maintenance",
            SilkThread.Core.Models.TweakCategory.AdvancedTweaks => isRu ? "Тонкая настройка" : "Advanced Tweaks",
            _ => category.ToString()
        };
    }

    public string GetCategoryDescription(SilkThread.Core.Models.TweakCategory category)
    {
        bool isRu = _currentLanguage == "ru";
        return category switch
        {
            SilkThread.Core.Models.TweakCategory.CpuOptimization => isRu 
                ? "Разблокировка парковки ядер, квантование времени потоков, приоритет MMCSS и отключение Power Throttling." 
                : "Core unparking, thread quantum allocation, MMCSS prioritization, and disabling Power Throttling.",
            SilkThread.Core.Models.TweakCategory.GpuOptimization => isRu 
                ? "Аппаратное планирование HAGS, переменная частота VRR, профили задержек NVIDIA/AMD и тайминги DWM." 
                : "Hardware-accelerated GPU scheduling (HAGS), VRR, NVIDIA/AMD latency profiles, and DWM timing optimizations.",
            SilkThread.Core.Models.TweakCategory.RamOptimization => isRu 
                ? "Закрепление ядра ОС в ОЗУ, очистка рабочих наборов, оптимизация Prefetcher и лимитов Svchost." 
                : "Locking Windows kernel in RAM, working set trimming, NVMe Prefetcher tuning, and Svchost memory limits.",
            SilkThread.Core.Models.TweakCategory.GamingPerformance => isRu 
                ? "Игровой режим Windows, полноэкранная оптимизация FSE/DirectFlip, отключение Game DVR и автоприглушения звука." 
                : "Windows Game Mode, FSE/DirectFlip optimizations, disabling Game DVR, and audio ducking prevention.",
            SilkThread.Core.Models.TweakCategory.WindowsPerformance => isRu 
                ? "Снижение задержки контекстных меню, отключение эффектов прозрачности и фоновых UWP-задач." 
                : "Context menu latency reduction, disabling transparency effects, and terminating background UWP tasks.",
            SilkThread.Core.Models.TweakCategory.InputAndLatency => isRu 
                ? "Прямой ввод мыши (Raw Input 1:1), ускорение опроса клавиатуры и запрет отключения USB-портов." 
                : "1:1 Raw mouse input, keyboard repeat rate acceleration, and disabling USB selective suspend.",
            SilkThread.Core.Models.TweakCategory.NetworkOptimization => isRu 
                ? "Отключение алгоритма Нагла, ускоренная отправка пакетов TCP ACK, снятие сетевого троттлинга и 20% резерва QoS." 
                : "Disabling Nagle's algorithm, instant TCP ACK frequency, network throttling removal, and unlocking QoS reserve.",
            SilkThread.Core.Models.TweakCategory.StorageOptimization => isRu 
                ? "Отключение коротких имён 8.3, запрет меток Last Access, активация TRIM для SSD и оптимизация дампов." 
                : "Disabling NTFS 8.3 names, disabling Last Access timestamps, TRIM activation, and crash dump limits.",
            SilkThread.Core.Models.TweakCategory.BackgroundProcesses => isRu 
                ? "Отключение телеметрии DiagTrack, виджетов Windows, предзапуска Edge и отчётов об ошибках." 
                : "Disabling DiagTrack telemetry, Windows Widgets, Microsoft Edge prelaunch, and Windows Error Reporting (WER).",
            SilkThread.Core.Models.TweakCategory.WindowsServices => isRu 
                ? "Отключение неиспользуемых служб для снижения фоновой нагрузки на процессор и задержек DPC." 
                : "Disabling unused Windows services to minimize CPU overhead and DPC execution latency.",
            SilkThread.Core.Models.TweakCategory.PowerManagement => isRu 
                ? "Активация схемы «Максимальная производительность», разблокировка параметров ASPM и отключение быстрого запуска." 
                : "Ultimate Performance power plan, unlocking PCIe ASPM states, and disabling Fast Startup.",
            SilkThread.Core.Models.TweakCategory.WindowsExplorerAndUi => isRu 
                ? "Компактный вид папок, показ расширений файлов, классическое контекстное меню и мгновенный отклик Проводника." 
                : "Compact folder view, file extension display, classic context menu, and instant Explorer responsiveness.",
            SilkThread.Core.Models.TweakCategory.SecurityAndPrivacy => isRu 
                ? "Отключение рекламного идентификатора, служб геолокации, телеметрии клавиатуры и журнала активности." 
                : "Disabling advertising ID, location services, typing telemetry, and Windows activity history.",
            SilkThread.Core.Models.TweakCategory.SystemMaintenance => isRu 
                ? "Запрет принудительных перезагрузок и защита видеодрайверов от перезаписи Центром обновления Windows." 
                : "Preventing automatic reboots and preventing Windows Update from overwriting GPU display drivers.",
            SilkThread.Core.Models.TweakCategory.AdvancedTweaks => isRu 
                ? "Приоритет реального времени для DWM, калибровка пула сессий и оптимизация освобождения памяти кучи." 
                : "Realtime priority for DWM, session pool tuning, and heap memory decommit optimization.",
            _ => ""
        };
    }

    public string GetRiskName(SilkThread.Core.Models.RiskLevel risk)
    {
        bool isRu = _currentLanguage == "ru";
        return risk switch
        {
            SilkThread.Core.Models.RiskLevel.Low => isRu ? "Низкий риск" : "Low Risk",
            SilkThread.Core.Models.RiskLevel.Medium => isRu ? "Умеренный риск" : "Medium Risk",
            SilkThread.Core.Models.RiskLevel.High => isRu ? "Высокий риск" : "High Risk",
            SilkThread.Core.Models.RiskLevel.Critical => isRu ? "Критический риск" : "Critical Risk",
            _ => risk.ToString()
        };
    }

    public string GetRecommendationName(SilkThread.Core.Models.RecommendationLevel rec)
    {
        bool isRu = _currentLanguage == "ru";
        return rec switch
        {
            SilkThread.Core.Models.RecommendationLevel.Recommended => isRu ? "Рекомендуется" : "Recommended",
            SilkThread.Core.Models.RecommendationLevel.Optional => isRu ? "Опционально" : "Optional",
            SilkThread.Core.Models.RecommendationLevel.NotRecommended => isRu ? "Не рекомендуется" : "Not Recommended",
            SilkThread.Core.Models.RecommendationLevel.Experimental => isRu ? "Экспериментально" : "Experimental",
            _ => rec.ToString()
        };
    }

    public string GetStatusName(SilkThread.Core.Models.TweakStatus status)
    {
        bool isRu = _currentLanguage == "ru";
        return status switch
        {
            SilkThread.Core.Models.TweakStatus.Applied => isRu ? "Применён" : "Applied",
            SilkThread.Core.Models.TweakStatus.NotApplied => isRu ? "Не применён" : "Not Applied",
            SilkThread.Core.Models.TweakStatus.Failed => isRu ? "Ошибка" : "Failed",
            SilkThread.Core.Models.TweakStatus.RequiresVerification => isRu ? "Требует проверки" : "Requires Verification",
            SilkThread.Core.Models.TweakStatus.Incompatible => isRu ? "Не поддерживается" : "Incompatible",
            _ => isRu ? "Не проверен" : "Unknown"
        };
    }

    private static readonly Dictionary<string, string> RuStrings = new()
    {
        // Navigation
        ["Nav_Dashboard"] = "Главная",
        ["Nav_Tweaks"] = "Библиотека твиков",
        ["Nav_Scanner"] = "Диагностика ПК",
        ["Nav_Optimization"] = "Мастер оптимизации",
        ["Nav_History"] = "История и откат",
        ["Nav_Restore"] = "Точки восстановления",
        ["Nav_Settings"] = "Параметры",

        // Tweaks Page
        ["Tweaks_Title"] = "Библиотека твиков",
        ["Tweaks_Subtitle"] = "165 проверенных оптимизаций Windows 11 с возможностью безопасного отката каждого параметра",
        ["Tweaks_SearchPlaceholder"] = "Поиск по названию, описанию или ветке реестра...",
        ["Tweaks_AllCategories"] = "Все категории",
        ["Tweaks_Filter_All"] = "Все твики",
        ["Tweaks_Filter_Applied"] = "Применен",
        ["Tweaks_Filter_NotApplied"] = "Не применен",
        ["Tweaks_Filter_Recommended"] = "Рекомендуемые ⭐",
        ["Tweaks_Filter_Safe"] = "Безопасные 🛡️",
        ["Tweaks_CompatibleOnly"] = "Только совместимые",
        ["Tweaks_Batch_ApplyRec"] = "Применить рекомендуемые",
        ["Tweaks_Batch_Revert"] = "Откатить категорию",
        ["Tweaks_CreatePreset"] = "Создать пресет",
        ["Tweaks_ActivatePreset"] = "Активировать пресет",
        ["Tweaks_PresetSaved"] = "✓ Пресет '{0}' успешно сохранён!",
        ["Tweaks_PresetApplied"] = "✓ Пресет '{0}' успешно применён! Активировано {1} твиков.",
        ["Tweaks_NoAppliedForPreset"] = "⚠ В системе нет активных твиков для создания пресета.",
        ["Tweaks_Inspector_Empty"] = "Выберите оптимизацию в списке для просмотра подробных сведений",
        ["Tweaks_Apply"] = "Применить твик",
        ["Tweaks_Revert"] = "Откатить изменения",
        ["Tweaks_Section_Overview"] = "ОПИСАНИЕ ОПТИМИЗАЦИИ",
        ["Tweaks_Section_Mechanism"] = "ПРИНЦИП РАБОТЫ (WINDOWS API / РЕЕСТР)",
        ["Tweaks_Section_Problem"] = "РЕШАЕМАЯ ПРОБЛЕМА",
        ["Tweaks_Section_Result"] = "ОЖИДАЕМЫЙ ЭФФЕКТ",
        ["Tweaks_Section_Risk"] = "ОЦЕНКА БЕЗОПАСНОСТИ",
        ["Tweaks_Incompatible_Warning"] = "Не поддерживается текущим оборудованием: ",
        ["Tweaks_RequiresReboot"] = "Для вступления изменений в силу требуется перезагрузка",
        ["Tweaks_TotalBadge"] = "Всего: ",
        ["Tweaks_AppliedBadge"] = "Применено: ",
        ["Tweaks_FilteredCount"] = "Показано {0} из {1} твиков",
        ["Tweaks_Inspector_Details"] = "Сведения об оптимизации",
        ["Tweaks_Inspector_Id"] = "Идентификатор:",
        ["Tweaks_Inspector_Category"] = "Категория:",
        ["Tweaks_Inspector_Risk"] = "Уровень риска:",
        ["Tweaks_Inspector_Rec"] = "Рекомендация:",
        ["Tweaks_Inspector_Reboot"] = "Перезагрузка:",
        ["Tweaks_RebootRequired"] = "Требуется",
        ["Tweaks_RebootNotRequired"] = "Не требуется",
        ["Tweaks_RebootBadge"] = "Перезагрузка",
        ["Tweaks_AllPcs"] = "Все конфигурации ПК",

        // Dashboard Bento Grid
        ["Dash_Hero_Title"] = "Центр управления производительностью",
        ["Dash_Hero_Subtitle"] = "Интеллектуальная оптимизация Windows 11 для максимального FPS и мгновенной скорости системы",
        ["Dash_Scan_Btn"] = "Диагностика системы",
        ["Dash_Quick_Optimize"] = "Быстрая оптимизация в 1 клик",
        ["Dash_Status_Active"] = "Игровой профиль активен",
        ["Dash_Stat_Applied"] = "Применено твиков",
        ["Dash_Stat_Safe"] = "Безопасных рекомендаций",
        ["Dash_Stat_Score"] = "Индекс оптимизации",
        ["Dash_Index"] = "Оптимизировано",
        ["Dash_GameReady"] = "Готов к киберспорту ✓",
        ["Dash_OptimizingTitle"] = "Оптимизация системы",
        ["Dash_AllTweaksBtn"] = "Библиотека твиков",
        ["Dash_TotalBadge"] = "Всего:",
        ["Dash_AppliedBadge"] = "Применено:",
        ["Dash_Hw_Title"] = "Аппаратная платформа",
        ["Dash_Hw_Cpu"] = "Процессор (CPU)",
        ["Dash_Hw_Gpu"] = "Видеокарта (GPU)",
        ["Dash_Hw_Ram"] = "Оперативная память (RAM)",
        ["Dash_Hw_Storage"] = "Система и хранилище",
        ["Dash_Hw_Os"] = "Система и питание",
        ["Dash_Hw_Battery"] = "Режим питания",
        ["Dash_Hw_DiagBtn"] = "Подробная диагностика",

        ["Dash_Latency_Title"] = "Игровая матрица задержек",
        ["Dash_Latency_Subtitle"] = "Снижение задержек ввода и ускорение отклика системы",
        ["Dash_Latency_InputTitle"] = "Прямой ввод 1:1 (Raw Input)",
        ["Dash_Latency_InputDesc"] = "Отключение сглаживания курсора, ускорение USB polling и повтора клавиш",
        ["Dash_Latency_InputBadge"] = "Активно",
        ["Dash_Latency_HagsTitle"] = "Планировщик HAGS & DirectFlip",
        ["Dash_Latency_HagsDesc"] = "Аппаратное планирование GPU, обход накладных расходов DWM",
        ["Dash_Latency_HagsBadge"] = "Активно",
        ["Dash_Latency_NetworkTitle"] = "Сетевой стек TCP NoDelay",
        ["Dash_Latency_NetworkDesc"] = "Отключение алгоритма Нагла, мгновенный ACK без сетевого троттлинга",
        ["Dash_Latency_NetworkBadge"] = "Активно",
        ["Dash_Latency_MmcssTitle"] = "Приоритет MMCSS в шедхулер",
        ["Dash_Latency_MmcssDesc"] = "Максимальные кванты потоков для игр, отключение Power Throttling",
        ["Dash_Latency_MmcssBadge"] = "Активно",
        ["Dash_Latency_Btn"] = "Настроить отклик",

        ["Dash_Security_Title"] = "Безопасность и точки отката",
        ["Dash_Security_Desc"] = "Создаётся точка восстановления перед внесением изменений.",
        ["Dash_Security_Protected"] = "✓ Защита системы активна",
        ["Dash_Security_Btn"] = "Точка восстановления",

        ["Dash_Presets_Title"] = "Быстрые сценарии",
        ["Dash_Preset_EsportsTitle"] = "Киберспорт (Ultra Latency)",
        ["Dash_Preset_EsportsDesc"] = "Минимальный инпутлаг, максимальная стабильность 1% Low FPS",
        ["Dash_Preset_BalancedTitle"] = "Сбалансированный игровой профиль",
        ["Dash_Preset_BalancedDesc"] = "Оптимальный отклик для игр, стриминга через OBS и каждый день",
        ["Dash_Preset_OfficeTitle"] = "Офисная и повседневная работа",
        ["Dash_Preset_HistoryTitle"] = "Журнал аудита и откат",
        ["Dash_Preset_HistoryDesc"] = "Мгновенный просмотр истории и откат любого изменения",
        ["Dash_Presets_WizardBtn"] = "Мастер оптимизации",

        // Categorical Health Indicators
        ["Dash_Cat_SectionTitle"] = "КАТЕГОРИЙНЫЙ ИНДЕКС ГОТОВНОСТИ К КИБЕРСПОРТУ",
        ["Dash_Cat_Latency"] = "Загрузка ввода и HDD",
        ["Dash_Cat_Gpu"] = "Графика и HAGS",
        ["Dash_Cat_Ram"] = "Память и MMCSS",
        ["Dash_Cat_Network"] = "Пинг и TCP NoDelay",

        // AI Copilot HUD & Console
        ["Dash_Ai_Hud_Title"] = "МОНИТОР НЕЙРОСЕТЕВОГО ТЮНИНГА",
        ["Dash_Ai_Hud_Latency"] = "Целевой отклик ядра",
        ["Dash_Ai_Hud_LatencyVal"] = "< 0.8 мс",
        ["Dash_Ai_Hud_Confidence"] = "Точность модели",
        ["Dash_Ai_Hud_ConfidenceVal"] = "99.4%",
        ["Dash_Ai_Hud_Profile"] = "Профиль эвристики",
        ["Dash_Ai_Hud_ProfileVal"] = "D3D / MMCSS Ultra",

        // Diagnostics / Scanner
        ["Scan_Title"] = "Аппаратная диагностика ПК",
        ["Scan_Subtitle"] = "Глубокий анализ аппаратной платформы, архитектуры процессора, памяти, драйверов и системного окружения",
        ["Scan_Run_Btn"] = "Повторить сканирование",
        ["Scan_Copy_Btn"] = "Скопировать отчёт",
        ["Scan_Copied_Toast"] = "✓ Полный аппаратный отчёт скопирован в буфер обмена!",
        ["Scan_Card_Cpu"] = "Процессор (CPU)",
        ["Scan_Card_Gpu"] = "Видеокарта (GPU)",
        ["Scan_Card_Ram"] = "Оперативная память (RAM)",
        ["Scan_Card_Storage"] = "Дисковые накопители (SSD / HDD)",
        ["Scan_Card_Os"] = "Операционная система и платформа",
        ["Scan_Card_Gaming"] = "Игровое окружение и задержки",

        // Optimization Wizard
        ["Opt_Title"] = "Мастер комплексной оптимизации",
        ["Opt_Subtitle"] = "Автоматический подбор и применение готовых профилей оптимизации для вашей системы",
        ["Opt_Preset_Safe"] = "Безопасный и рекомендуемый (Базовый)",
        ["Opt_Preset_Safe_Desc"] = "Только проверенные оптимизации с минимальным риском. Высокая стабильность для повседневной работы и игр.",
        ["Opt_Preset_Gaming"] = "Максимальный FPS и минимальная задержка",
        ["Opt_Preset_Gaming_Desc"] = "Глубокая оптимизация планировщика MMCSS, видеоподсистемы HAGS/DirectFlip и Игрового режима Windows.",
        ["Opt_Preset_Network"] = "Киберспортивная сеть и низкий пинг",
        ["Opt_Preset_Network_Desc"] = "Отключение алгоритма Нагла, ускоренная отправка пакетов TCP ACK, снятие 20% резерва QoS и оптимизация сетевых очередей.",
        ["Opt_Preset_Minimal"] = "Чистая и легкая Windows",
        ["Opt_Preset_Minimal_Desc"] = "Отключение служб телеметрии DiagTrack, виджетов, фонового предварительного запуска Edge и неиспользуемых фоновых служб.",
        ["Opt_CreateRestorePoint"] = "Создать точку восстановления Windows перед применением",
        ["Opt_Start_Btn"] = "Запустить оптимизацию",
        ["Opt_Executing"] = "Выполняется оптимизация системы...",

        // History / Rollback
        ["Hist_Title"] = "Журнал аудита и откат изменений",
        ["Hist_Subtitle"] = "Локальная база данных со всеми применёнными оптимизациями и их точным исходным состоянием",
        ["Hist_Rollback_Btn"] = "Откатить твик",
        ["Hist_Clear_Btn"] = "Очистить журнал",
        ["Hist_Col_Time"] = "Дата и время",
        ["Hist_Col_Tweak"] = "Оптимизация",
        ["Hist_Col_Category"] = "Категория",
        ["Hist_Col_Status"] = "Статус",
        ["Hist_Col_Action"] = "Действие",
        ["Hist_Empty"] = "Журнал изменений пуст. Все применённые оптимизации будут сохранены здесь.",

        // Restore Points
        ["Rest_Title"] = "Точки восстановления Windows",
        ["Rest_Subtitle"] = "Прямое управление встроенной службой защиты системы Windows (srclient.dll)",
        ["Rest_Create_Btn"] = "Создать точку восстановления",
        ["Rest_Open_SysRestore"] = "Открыть окно «Восстановление системы» Windows",
        ["Rest_Name_Placeholder"] = "Название новой точки восстановления...",
        ["Rest_Status_Enabled"] = "Защита системы включена",
        ["Rest_Status_Disabled"] = "Защита системы отключена",
        ["Rest_List_Title"] = "Доступные точки восстановления на ПК",
        ["Rest_Empty"] = "Точки восстановления не найдены или защита отключена на системном диске.",

        // Settings
        ["Set_Title"] = "Параметры программы",
        ["Set_Subtitle"] = "Настройка интерфейса, языка и параметров безопасности SilkThread",
        ["Set_Lang_Label"] = "Язык интерфейса / Language",
        ["Set_Theme_Label"] = "Тема оформления",
        ["Set_Theme_Dark"] = "Тёмная (Mica Dark)",
        ["Set_Theme_Light"] = "Светлая",
        ["Set_Confirm_Dangerous"] = "Запрашивать подтверждение перед применением твиков высокого риска",
        ["Set_Auto_Restore"] = "Создавать точку восстановления Windows перед пакетным применением",
        ["Set_Hide_Incompatible"] = "Скрывать твики, не поддерживаемые текущим оборудованием",
        ["Set_Reboot_Notify"] = "Уведомлять о необходимости перезагрузки системы",
        ["Set_About_Title"] = "О программе SilkThread",
        ["Set_About_Desc"] = "Высокопроизводительный нативный оптимизатор Windows 11 x64 нового поколения на базе .NET 10 и WinUI 3.",
        ["Set_Version"] = "Версия 2.0 (Windows 11 Edition)",

        // Update History Dialog
        ["UpdHist_Btn"] = "История обновлений",
        ["UpdHist_DialogTitle"] = "История версий SilkThread",
        ["UpdHist_DialogSubtitle"] = "Хронология релизов, новых модулей и оптимизаций",
        ["UpdHist_Close"] = "Закрыть",
        ["UpdHist_Badge_Latest"] = "АКТУАЛЬНАЯ ВЕРСИЯ",

        ["UpdHist_v20_Title"] = "Bento Grid & Community Presets",
        ["UpdHist_v20_Date"] = "8 ноября 2026 г.",
        ["UpdHist_v20_Item1"] = "Новый адаптивный интерфейс в стиле Bento Grid с поддержкой динамического масштабирования.",
        ["UpdHist_v20_Item2"] = "Скрытая компактная навигация: иконки разделов с интерактивными всплывающими подсказками.",
        ["UpdHist_v20_Item3"] = "Система пользовательских пресетов (.silkpreset) с проверкой совместимости и защитой от сбоев.",
        ["UpdHist_v20_Item4"] = "Расширение библиотеки до 265 проверенных твиков Windows 11 с подробными техническими описаниями.",
        ["UpdHist_v20_Item5"] = "Полный аудит кодовой базы и исправление 12 системных дефектов и необработанных исключений.",

        ["UpdHist_v15_Title"] = "Deep Hardware Diagnostics & Safety",
        ["UpdHist_v15_Date"] = "1 ноября 2026 г.",
        ["UpdHist_v15_Item1"] = "Интеллектуальный сканер оборудования (CPU, GPU VRAM, RAM, NVMe/SSD, VBS, HAGS, TRIM).",
        ["UpdHist_v15_Item2"] = "Локальная база SQLite для аудита истории изменений с возможностью индивидуального отката.",
        ["UpdHist_v15_Item3"] = "Модуль создания точек восстановления Windows и проверки службы System Restore (srclient.dll).",

        ["UpdHist_v10_Title"] = "Initial Release",
        ["UpdHist_v10_Date"] = "28 октября 2026 г.",
        ["UpdHist_v10_Item1"] = "Базовый каталог из 165 проверенных оптимизаций Windows 11.",
        ["UpdHist_v10_Item2"] = "Глубокая тёмная тема оформления Mica Dark в стиле Windows 11 Fluent 2.",
        ["UpdHist_v10_Item3"] = "Полноценная двуязычная локализация (Русский / English).",

        // Remote Update System
        ["Update_CheckButton"] = "Проверить обновления",
        ["Update_Checking"] = "Проверка обновлений...",
        ["Update_UpToDate"] = "У вас установлена последняя версия SilkThread ✓",
        ["Update_Available"] = "Вышло новое обновление!",
        ["Update_Error"] = "Не удалось связаться с сервером обновлений",

        // IOST (Intelligent Optimization & System Tuning)
        ["Dash_Ai_Title"] = "IOST — Интеллектуальная оптимизация",
        ["Dash_Ai_Badge"] = "IOST активен",
        ["Dash_Ai_AgentActive"] = "IOST АКТИВЕН",
        ["Dash_Ai_Subtitle"] = "Интеллектуальная настройка параметров под Windows 11 на основе анализа системы и сценариев игр",
        ["Dash_Ai_ScenarioHeader"] = "Сценарий использования",
        ["Dash_Ai_RiskHeader"] = "Допустимый риск",
        ["Dash_Ai_PriorityHeader"] = "Ключевой приоритет",
        ["Dash_Ai_GenerateBtn"] = "Сгенерировать персональный пресет (IOST)",
        ["Dash_Ai_Scenario_Esports"] = "Киберспорт",
        ["Dash_Ai_Scenario_HeavyAaa"] = "Тяжёлые AAA-игры",
        ["Dash_Ai_Scenario_Streaming"] = "Стриминг и OBS",
        ["Dash_Ai_Scenario_Balanced"] = "Базовое",
        ["Dash_Ai_Risk_Safe"] = "Безопасный",
        ["Dash_Ai_Risk_Balanced"] = "Сбалансированный",
        ["Dash_Ai_Risk_Maximum"] = "Максимальный",
        ["Dash_Ai_Priority_InputLag"] = "Минимальная задержка",
        ["Dash_Ai_Priority_MaxFps"] = "Максимальный FPS",
        ["Dash_Ai_Priority_Frametime"] = "Плавный фреймтайм",
        ["Dash_Ai_Priority_Memory"] = "Очистка памяти",
        ["Dash_Ai_Status_Sending"] = "Анализ аппаратного профиля...",
        ["Dash_Ai_Status_Reasoning"] = "Рассуждение и отбор твиков...",
        ["Dash_Ai_Status_Ready"] = "✓ Персональный пресет успешно сгенерирован!",

        // Settings OpenRouter AI
        ["Set_Ai_Title"] = "Параметры OpenRouter AI",
        ["Set_Ai_Subtitle"] = "Конфигурация нейросетевого ассистента для генерации пресетов",
        ["Set_Ai_ApiKey"] = "API-ключ OpenRouter",
        ["Set_Ai_ApiKey_Desc"] = "Ключ доступа к API OpenRouter для отправки запросов к нейросетевой модели",
        ["Set_Ai_Model"] = "Активная нейросетевая модель",
        ["Set_Ai_Model_Desc"] = "Нейросетевая архитектура — адаптивное рассуждение о твиках Windows",
        ["Set_Ai_Badge"] = "FREE TIER АКТИВЕН",
        ["Set_Ai_Status"] = "Готов к работе (OpenRouter Cloud)",

        // Tweaks Page Extra
        ["Tweaks_TweaksCountSuffix"] = "твиков",
        ["Tweaks_ScrollPrev"] = "Назад",
        ["Tweaks_ScrollNext"] = "Вперед",
        ["Tweaks_Inspector_EmptyHint"] = "Выберите твик в левой панели для просмотра его назначения, механизма работы и эффекта.",
        ["Tweaks_Section_Compatibility"] = "СОВМЕСТИМОСТЬ С ВАШЕЙ СИСТЕМОЙ",
        ["Tweaks_Compatible_Title"] = "Полная совместимость",
        ["Tweaks_Compatible_Desc"] = "Твик полностью совместим и оптимизирован для вашей конфигурации Windows и оборудования.",

        // Dashboard Extra
        ["Dash_Score_HeroTitle"] = "Отличная производительность!",
        ["Dash_Score_Desc"] = "Система оптимизирована на {0}%. Можно улучшить ещё больше.",
        ["Dash_Stat_Processes"] = "процессов в фоне",
        ["Dash_Stat_SafeRecs"] = "безопасных рекомендаций",
        ["Dash_Stat_Services"] = "служб",
        ["Dash_Ai_Forecast_Fps"] = "Прирост FPS",
        ["Dash_Ai_Forecast_Latency"] = "Задержка ввода",
        ["Dash_Ai_Forecast_Ram"] = "Очистка RAM",
        ["Dash_Latency_Active"] = "Активно",

        // Diagnostics / Scanner Extra
        ["Scan_Sub_CpuArch"] = "АРХИТЕКТУРА И ВЫЧИСЛИТЕЛЬНЫЕ ЯДРА",
        ["Scan_Sub_RamSpeed"] = "СКОРОСТЬ И ЗАПОЛНЕНИЕ ПАМЯТИ",
        ["Scan_Sub_GpuDriver"] = "ГРАФИЧЕСКИЙ АДАПТЕР И ДРАЙВЕР",
        ["Scan_Lbl_Vram"] = "ОБЪЕМ ВИДЕОПАМЯТИ (VRAM)",
        ["Scan_Lbl_DriverVersion"] = "ВЕРСИЯ ДРАЙВЕРА",
        ["Scan_Lbl_HagsScheduling"] = "HAGS ПЛАНИРОВАНИЕ",
        ["Scan_Sub_OsServices"] = "ОПЕРАЦИОННАЯ СИСТЕМА И СЛУЖБЫ",
        ["Scan_Lbl_OsEdition"] = "РЕДАКЦИЯ WINDOWS 11",
        ["Scan_Lbl_CoreIsolation_Desc"] = "Изоляция ядра (HVCI / VBS)",
        ["Scan_Lbl_UsedOf"] = "занято из",

        // Optimization Wizard Extra
        ["Opt_Profiles_Title"] = "ПРОФИЛИ ОПТИМИЗАЦИИ",
        ["Opt_Profiles_Subtitle"] = "ВЫБЕРИТЕ СЦЕНАРИЙ ДЛЯ СИСТЕМЫ",
        ["Opt_Profiles_Badge"] = "4 ПРОФИЛЯ",
        ["Opt_Control_Title"] = "УПРАВЛЕНИЕ ОПТИМИЗАЦИЕЙ",
        ["Opt_Control_Subtitle"] = "ПАРАМЕТРЫ ЗАЩИТЫ И СИСТЕМНЫЙ СНИМОК",
        ["Opt_Safety_Active"] = "ЗАЩИТА АКТИВНА",
        ["Opt_Safety_Desc"] = "Создает контрольный снимок реестра и драйверов перед применением твиков",
        ["Opt_Selected_Profile"] = "Выбран профиль:",
        ["Opt_Tweaks_Suffix"] = "твиков",
        ["Opt_Audit_Subtitle"] = "МОНИТОРИНГ И АУДИТ ОПЕРАЦИЙ",
        ["Opt_Clear_Logs"] = "Очистить",
        ["Opt_Progress_Label"] = "ПРОГРЕСС ВЫПОЛНЕНИЯ",
        ["Opt_System_Status_Label"] = "СОСТОЯНИЕ СИСТЕМЫ",
        ["Opt_Status_Ready"] = "Готов к запуску оптимизации.",

        // History Extra
        ["Hist_Refresh_Btn"] = "Обновить",
        ["Hist_Rollback_Action"] = "Откатить",
        ["Hist_Empty_Title"] = "История операций пуста",
        ["Hist_Rollback_Dialog_Title"] = "Подтверждение отката твика",
        ["Hist_Rollback_Dialog_Content"] = "Вы действительно хотите откатить твик «{0}»?\nПараметры реестра и системы будут возвращены в состояние до его применения.",
        ["Hist_Clear_Dialog_Title"] = "Очистка журнала аудита",
        ["Hist_Clear_Dialog_Content"] = "Вы уверены, что хотите полностью очистить историю применённых операций?\nЗаписи об исходных состояниях будут удалены. Это действие необратимо.",
        ["Hist_Dialog_Cancel"] = "Отмена",

        // Restore Points Extra
        ["Rest_Sub_Protection"] = "СЛУЖБА ЗАЩИТЫ СИСТЕМЫ И ДИСКОВ WINDOWS (SRCLIENT.DLL)",
        ["Rest_Sub_Status"] = "СОСТОЯНИЕ СЛУЖБЫ ВОССТАНОВЛЕНИЯ",
        ["Rest_Sub_Snapshot"] = "МОМЕНТАЛЬНЫЙ СНИМОК РЕЕСТРА И КОНФИГУРАЦИИ СИСТЕМЫ",
        ["Rest_Sub_Timeline"] = "ХРОНОЛОГИЯ СОХРАНЁННЫХ ТОЧЕК ВОССТАНОВЛЕНИЯ",
        ["Rest_Sub_PointsCount"] = "ТОЧЕК В БАЗЕ",
        ["Rest_Refresh_Btn"] = "Обновить список",
        ["Rest_Rollback_Btn"] = "Откатить",
        ["Rest_Delete_Btn"] = "Удалить",
        ["Rest_Empty_Title"] = "Точки восстановления не найдены",
        ["Rest_Rstrui_Tooltip"] = "Запуск официальной системной утилиты Windows",
        ["Rest_Rollback_Dialog_Title"] = "Подтверждение отката системы",
        ["Rest_Rollback_Dialog_Content"] = "Вы действительно хотите восстановить конфигурацию Windows к контрольной точке «{0}» (#{1})?\n\nБудет запущен официальный системный мастер восстановления Windows (rstrui.exe). Для применения изменений потребуется перезагрузка компьютера.",
        ["Rest_Rollback_Dialog_Btn"] = "Запустить восстановление",
        ["Rest_Delete_Dialog_Title"] = "Удаление контрольной точки",
        ["Rest_Delete_Dialog_Content"] = "Вы действительно хотите удалить точку восстановления «{0}» (#{1})?\n\nТеневая копия состояния системы будет удалена. Это действие необратимо.",
        ["Rest_Delete_Dialog_Btn"] = "Удалить",
        ["Rest_Dialog_Cancel"] = "Отмена",

        // Settings Extra
        ["Set_Sub_Safety"] = "СИСТЕМНЫЕ ОГРАНИЧЕНИЯ И ЗАЩИТА ОТ СБОЕВ",
        ["Set_Sub_Ai"] = "НЕЙРОСЕТЕВОЙ АГЕНТ ОПТИМИЗАЦИИ",
        ["Set_Sub_Lang"] = "ЛОКАЛИЗАЦИЯ И РЕГИОНАЛЬНЫЕ СТАНДАРТЫ",
        ["Set_Sub_LangSwitch"] = "Мгновенное переключение языка",
        ["Set_Sub_About"] = "СВЕДЕНИЯ О ПЛАТФОРМЕ И РАЗРАБОТЧИКЕ",

        // Create Preset Dialog
        ["PresetCreate_Title"] = "Создание пресета",
        ["PresetCreate_Subtitle"] = "Экспорт текущих параметров системы в файл .silkpreset",
        ["PresetCreate_ConfigHeader"] = "Конфигурация активных параметров",
        ["PresetCreate_ActiveCount"] = "Будет сохранено активных твиков:",
        ["PresetCreate_NameLabel"] = "Название пресета",
        ["PresetCreate_NamePlaceholder"] = "Например: Gaming Pro Profile",
        ["PresetCreate_DescLabel"] = "Описание",
        ["PresetCreate_DescPlaceholder"] = "Например: Оптимизация сети, задержек и фоновых служб",
        ["PresetCreate_AuthorLabel"] = "Автор",
        ["PresetCreate_CancelBtn"] = "Отмена",
        ["PresetCreate_SaveBtn"] = "Сохранить",
        ["PresetCreate_DefaultDesc"] = "Пользовательский пресет оптимизации SilkThread"
    };

    private static readonly Dictionary<string, string> EnStrings = new()
    {
        // Navigation
        ["Nav_Dashboard"] = "Dashboard",
        ["Nav_Tweaks"] = "Tweaks Library",
        ["Nav_Scanner"] = "PC Diagnostics",
        ["Nav_Optimization"] = "Optimization Wizard",
        ["Nav_History"] = "History & Rollback",
        ["Nav_Restore"] = "Restore Points",
        ["Nav_Settings"] = "Settings",

        // Tweaks Page
        ["Tweaks_Title"] = "Tweaks Library",
        ["Tweaks_Subtitle"] = "165 verified Windows 11 optimizations with safe individual rollback for each tweak",
        ["Tweaks_SearchPlaceholder"] = "Search by title, description or registry key...",
        ["Tweaks_AllCategories"] = "All Categories",
        ["Tweaks_Filter_All"] = "All Tweaks",
        ["Tweaks_Filter_Applied"] = "Applied",
        ["Tweaks_Filter_NotApplied"] = "Not Applied",
        ["Tweaks_Filter_Recommended"] = "Recommended ⭐",
        ["Tweaks_Filter_Safe"] = "Safe 🛡️",
        ["Tweaks_CompatibleOnly"] = "Compatible Only",
        ["Tweaks_Batch_ApplyRec"] = "Apply Recommended",
        ["Tweaks_Batch_Revert"] = "Revert Category",
        ["Tweaks_CreatePreset"] = "Create Preset",
        ["Tweaks_ActivatePreset"] = "Activate Preset",
        ["Tweaks_PresetSaved"] = "✓ Preset '{0}' successfully saved!",
        ["Tweaks_PresetApplied"] = "✓ Preset '{0}' successfully applied! Activated {1} tweaks.",
        ["Tweaks_NoAppliedForPreset"] = "⚠ There are no active tweaks to create a preset.",
        ["Tweaks_Inspector_Empty"] = "Select a tweak from the list to view its details",
        ["Tweaks_Apply"] = "Apply Tweak",
        ["Tweaks_Revert"] = "Revert Changes",
        ["Tweaks_Section_Overview"] = "OVERVIEW",
        ["Tweaks_Section_Mechanism"] = "TECHNICAL MECHANISM (WINDOWS API / REGISTRY)",
        ["Tweaks_Section_Problem"] = "PROBLEM SOLVED",
        ["Tweaks_Section_Result"] = "EXPECTED RESULT",
        ["Tweaks_Section_Risk"] = "RISK ASSESSMENT",
        ["Tweaks_Incompatible_Warning"] = "Incompatible with current hardware: ",
        ["Tweaks_RequiresReboot"] = "System restart required for changes to take effect",
        ["Tweaks_TotalBadge"] = "Total: ",
        ["Tweaks_AppliedBadge"] = "Applied: ",
        ["Tweaks_FilteredCount"] = "Showing {0} of {1} tweaks",
        ["Tweaks_Inspector_Details"] = "Tweak Details",
        ["Tweaks_Inspector_Id"] = "Identifier:",
        ["Tweaks_Inspector_Category"] = "Category:",
        ["Tweaks_Inspector_Risk"] = "Risk Level:",
        ["Tweaks_Inspector_Rec"] = "Recommendation:",
        ["Tweaks_Inspector_Reboot"] = "Restart Required:",
        ["Tweaks_RebootRequired"] = "Required",
        ["Tweaks_RebootNotRequired"] = "Not Required",
        ["Tweaks_RebootBadge"] = "Restart",
        ["Tweaks_AllPcs"] = "All PC Configurations",

        // Dashboard Bento Grid
        ["Dash_Hero_Title"] = "Performance Command Center",
        ["Dash_Hero_Subtitle"] = "Intelligent Windows 11 optimization for peak frame rates and ultra-low input latency",
        ["Dash_Scan_Btn"] = "System Diagnostics",
        ["Dash_Quick_Optimize"] = "1-Click Quick Optimization",
        ["Dash_Status_Active"] = "Gaming Profile Active",
        ["Dash_Stat_Applied"] = "Applied Tweaks",
        ["Dash_Stat_Safe"] = "Safe Recommendations",
        ["Dash_Stat_Score"] = "Optimization Score",
        ["Dash_Index"] = "INDEX",
        ["Dash_GameReady"] = "ESPORTS READY ✓",
        ["Dash_OptimizingTitle"] = "System Optimization",
        ["Dash_AllTweaksBtn"] = "Tweaks Library",
        ["Dash_TotalBadge"] = "Total:",
        ["Dash_AppliedBadge"] = "Applied:",
        ["Dash_Hw_Title"] = "Hardware Platform",
        ["Dash_Hw_Cpu"] = "Processor (CPU)",
        ["Dash_Hw_Gpu"] = "Graphics Card (GPU)",
        ["Dash_Hw_Ram"] = "System Memory (RAM)",
        ["Dash_Hw_Storage"] = "Storage Drives",
        ["Dash_Hw_Os"] = "System & Power",
        ["Dash_Hw_Battery"] = "Power Plan & State",
        ["Dash_Hw_DiagBtn"] = "Full Diagnostics",

        ["Dash_Latency_Title"] = "Gaming Latency Matrix",
        ["Dash_Latency_Subtitle"] = "Input latency reduction and frame pipeline optimization",
        ["Dash_Latency_InputTitle"] = "1:1 Raw Input Architecture",
        ["Dash_Latency_InputDesc"] = "Zero cursor smoothing, accelerated USB polling & key repeat",
        ["Dash_Latency_InputBadge"] = "1:1 RAW INPUT",
        ["Dash_Latency_HagsTitle"] = "HAGS & DirectFlip Pipeline",
        ["Dash_Latency_HagsDesc"] = "Hardware GPU scheduling bypassing desktop window manager overhead",
        ["Dash_Latency_HagsBadge"] = "ACTIVE",
        ["Dash_Latency_NetworkTitle"] = "TCP NoDelay Gaming Stack",
        ["Dash_Latency_NetworkDesc"] = "Disabled Nagle algorithm, instant ACK & zero gaming packet queue",
        ["Dash_Latency_NetworkBadge"] = "0% BUFFERING",
        ["Dash_Latency_MmcssTitle"] = "MMCSS & Thread Priority",
        ["Dash_Latency_MmcssDesc"] = "Maximized thread quantum allocation, Power Throttling disabled",
        ["Dash_Latency_MmcssBadge"] = "HIGH PRIORITY",
        ["Dash_Latency_Btn"] = "Tune Latency",

        ["Dash_Security_Title"] = "Safety & Restore Protection",
        ["Dash_Security_Desc"] = "SilkThread isolates every tweak. Before making any changes, a Windows System Restore point and registry backup are created automatically.",
        ["Dash_Security_Protected"] = "SYSTEM PROTECTION ACTIVE",
        ["Dash_Security_Btn"] = "Restore Points",

        ["Dash_Presets_Title"] = "Quick Optimization Profiles",
        ["Dash_Preset_EsportsTitle"] = "Esports (Ultra Latency)",
        ["Dash_Preset_EsportsDesc"] = "Minimal input lag, highest 1% low frame consistency",
        ["Dash_Preset_BalancedTitle"] = "Balanced Gaming Profile",
        ["Dash_Preset_BalancedDesc"] = "Optimal latency for gaming, OBS streaming, and productivity",
        ["Dash_Preset_HistoryTitle"] = "Audit Log & Rollback",
        ["Dash_Preset_HistoryDesc"] = "Instant tweak history inspection and 1-click reversal",
        ["Dash_Presets_WizardBtn"] = "Optimization Wizard",

        // Categorical Health Indicators
        ["Dash_Cat_SectionTitle"] = "CATEGORICAL GAMING READINESS INDEX",
        ["Dash_Cat_Latency"] = "Input Latency & D3D",
        ["Dash_Cat_Gpu"] = "Graphics & HAGS",
        ["Dash_Cat_Ram"] = "Memory & MMCSS",
        ["Dash_Cat_Network"] = "Ping & TCP NoDelay",

        // AI Copilot HUD & Console
        ["Dash_Ai_Hud_Title"] = "NEURAL TUNING TELEMETRY HUD",
        ["Dash_Ai_Hud_Latency"] = "Target Kernel Latency",
        ["Dash_Ai_Hud_LatencyVal"] = "< 0.8 ms",
        ["Dash_Ai_Hud_Confidence"] = "Model Confidence",
        ["Dash_Ai_Hud_ConfidenceVal"] = "99.4%",
        ["Dash_Ai_Hud_Profile"] = "Heuristics Engine",
        ["Dash_Ai_Hud_ProfileVal"] = "D3D / MMCSS Ultra",

        // Diagnostics / Scanner
        ["Scan_Title"] = "Hardware PC Diagnostics",
        ["Scan_Subtitle"] = "In-depth analysis of hardware platform, CPU architecture, memory timings, drivers, and system environment",
        ["Scan_Run_Btn"] = "Re-scan Hardware",
        ["Scan_Copy_Btn"] = "Copy Report",
        ["Scan_Copied_Toast"] = "✓ Full hardware report copied to clipboard!",
        ["Scan_Card_Cpu"] = "Processor (CPU)",
        ["Scan_Card_Gpu"] = "Graphics Processing Unit (GPU)",
        ["Scan_Card_Ram"] = "Random Access Memory (RAM)",
        ["Scan_Card_Storage"] = "Storage Drives (SSD / HDD)",
        ["Scan_Card_Os"] = "Operating System & Platform",
        ["Scan_Card_Gaming"] = "Gaming Environment & Latency",

        // Optimization Wizard
        ["Opt_Title"] = "Optimization Profile Wizard",
        ["Opt_Subtitle"] = "Curated tuning profiles and intelligent batch optimization tailored to your PC",
        ["Opt_Preset_Safe"] = "Safe & Recommended (Balanced)",
        ["Opt_Preset_Safe_Desc"] = "Low-risk verified tweaks only. Maximum stability for everyday multitasking and competitive gaming.",
        ["Opt_Preset_Gaming"] = "Maximum FPS & Low Latency",
        ["Opt_Preset_Gaming_Desc"] = "In-depth tuning of MMCSS scheduler, HAGS/DirectFlip display pipeline, and Windows Game Mode.",
        ["Opt_Preset_Network"] = "Esports Network & Low Ping",
        ["Opt_Preset_Network_Desc"] = "Disabling Nagle's algorithm, instant TCP ACK frequency, unlocking the 20% QoS bandwidth reserve, and eliminating network throttling.",
        ["Opt_Preset_Minimal"] = "Clean & Lightweight Windows",
        ["Opt_Preset_Minimal_Desc"] = "Disabling telemetry, Widgets, Edge background prelaunch, and unused secondary services.",
        ["Opt_CreateRestorePoint"] = "Create Windows Restore Point before applying",
        ["Opt_Start_Btn"] = "Start Optimization",
        ["Opt_Executing"] = "Applying system optimizations...",

        // History / Rollback
        ["Hist_Title"] = "Audit History & Rollback",
        ["Hist_Subtitle"] = "Local database tracking every applied modification with its exact original state",
        ["Hist_Rollback_Btn"] = "Rollback Tweak",
        ["Hist_Clear_Btn"] = "Clear History",
        ["Hist_Col_Time"] = "Date & Time",
        ["Hist_Col_Tweak"] = "Optimization",
        ["Hist_Col_Category"] = "Category",
        ["Hist_Col_Status"] = "Status",
        ["Hist_Col_Action"] = "Action",
        ["Hist_Empty"] = "History log is empty. All applied tweaks will appear here.",

        // Restore Points
        ["Rest_Title"] = "Windows System Restore",
        ["Rest_Subtitle"] = "Direct native management of Windows System Protection API (srclient.dll)",
        ["Rest_Create_Btn"] = "Create Restore Point",
        ["Rest_Open_SysRestore"] = "Open Windows System Restore Tool",
        ["Rest_Name_Placeholder"] = "New restore point name...",
        ["Rest_Status_Enabled"] = "System Protection Enabled",
        ["Rest_Status_Disabled"] = "System Protection Disabled",
        ["Rest_List_Title"] = "Available Restore Points on PC",
        ["Rest_Empty"] = "No restore points found or system protection is disabled on the system drive.",

        // Settings
        ["Set_Title"] = "Application Settings",
        ["Set_Subtitle"] = "Customize appearance, language, and safety preferences for SilkThread",
        ["Set_Lang_Label"] = "Language / Язык интерфейса",
        ["Set_Theme_Label"] = "Theme Appearance",
        ["Set_Theme_Dark"] = "Dark (Mica Dark)",
        ["Set_Theme_Light"] = "Light",
        ["Set_Confirm_Dangerous"] = "Confirm before applying high-risk tweaks",
        ["Set_Auto_Restore"] = "Automatically create a restore point during batch optimizations",
        ["Set_Hide_Incompatible"] = "Automatically hide tweaks incompatible with current hardware",
        ["Set_Reboot_Notify"] = "Notify when system restart is required",
        ["Set_About_Title"] = "About SilkThread",
        ["Set_About_Desc"] = "Next-generation native Windows 11 x64 gaming optimizer built with .NET 10 and WinUI 3.",
        ["Set_Version"] = "Version 2.0 (Windows 11 Edition)",

        // Update History Dialog
        ["UpdHist_Btn"] = "Update History",
        ["UpdHist_DialogTitle"] = "SilkThread Release History",
        ["UpdHist_DialogSubtitle"] = "Timeline of releases, new features, and stability enhancements",
        ["UpdHist_Close"] = "Close",
        ["UpdHist_Badge_Latest"] = "CURRENT RELEASE",

        ["UpdHist_v20_Title"] = "Bento Grid & Community Presets",
        ["UpdHist_v20_Date"] = "November 8, 2026",
        ["UpdHist_v20_Item1"] = "New responsive Bento Grid interface layout with dynamic scaling support.",
        ["UpdHist_v20_Item2"] = "Collapsible compact navigation: section icons with interactive tooltips.",
        ["UpdHist_v20_Item3"] = "Community and user presets system (.silkpreset) with compatibility verification and crash safeguards.",
        ["UpdHist_v20_Item4"] = "Expanded tweak catalog up to 265 verified Windows 11 optimizations with in-depth technical documentation.",
        ["UpdHist_v20_Item5"] = "Comprehensive codebase audit and resolution of 12 system defects and unhandled exceptions.",

        ["UpdHist_v15_Title"] = "Deep Hardware Diagnostics & Safety",
        ["UpdHist_v15_Date"] = "November 1, 2026",
        ["UpdHist_v15_Item1"] = "Intelligent hardware diagnostics scanner (CPU, GPU VRAM, RAM, NVMe/SSD, VBS, HAGS, TRIM).",
        ["UpdHist_v15_Item2"] = "Embedded SQLite audit logging for configuration history with individual rollback capabilities.",
        ["UpdHist_v15_Item3"] = "Windows System Restore point creation module with native srclient.dll health checks.",

        ["UpdHist_v10_Title"] = "Initial Release",
        ["UpdHist_v10_Date"] = "October 28, 2026",
        ["UpdHist_v10_Item1"] = "Baseline catalog of 165 verified Windows 11 optimizations.",
        ["UpdHist_v10_Item2"] = "Mica Dark immersive theme styling conforming to Windows 11 Fluent 2 guidelines.",
        ["UpdHist_v10_Item3"] = "Full bilingual localization support (Russian / English).",

        // Remote Update System
        ["Update_CheckButton"] = "Check for Updates",
        ["Update_Checking"] = "Checking for updates...",
        ["Update_UpToDate"] = "SilkThread is up to date ✓",
        ["Update_Available"] = "New update available!",
        ["Update_Error"] = "Could not connect to update server",

        // IOST (Intelligent Optimization & System Tuning)
        ["Dash_Ai_Title"] = "IOST — Intelligent Optimization",
        ["Dash_Ai_Badge"] = "IOST ACTIVE",
        ["Dash_Ai_AgentActive"] = "IOST ACTIVE",
        ["Dash_Ai_Subtitle"] = "Intelligent Windows 11 kernel & pipeline tuning adapted specifically to your hardware and gaming profile",
        ["Dash_Ai_ScenarioHeader"] = "1. TARGET WORKLOAD SCENARIO",
        ["Dash_Ai_RiskHeader"] = "2. RISK TOLERANCE LEVEL",
        ["Dash_Ai_PriorityHeader"] = "3. PRIMARY OPTIMIZATION FOCUS",
        ["Dash_Ai_GenerateBtn"] = "Generate Custom Preset (IOST)",
        ["Dash_Ai_Scenario_Esports"] = "Esports (CS2/Valorant)",
        ["Dash_Ai_Scenario_HeavyAaa"] = "Heavy AAA Games",
        ["Dash_Ai_Scenario_Streaming"] = "Streaming & OBS",
        ["Dash_Ai_Scenario_Balanced"] = "Balanced",
        ["Dash_Ai_Risk_Safe"] = "Safe (100% Safe)",
        ["Dash_Ai_Risk_Balanced"] = "Balanced",
        ["Dash_Ai_Risk_Maximum"] = "Maximum",
        ["Dash_Ai_Priority_InputLag"] = "Minimal Input Lag",
        ["Dash_Ai_Priority_MaxFps"] = "Maximum FPS",
        ["Dash_Ai_Priority_Frametime"] = "Smooth Frametime",
        ["Dash_Ai_Priority_Memory"] = "Memory Cleanup",
        ["Dash_Ai_Status_Sending"] = "Analyzing hardware profile...",
        ["Dash_Ai_Status_Reasoning"] = "Reasoning and selecting tweaks...",
        ["Dash_Ai_Status_Ready"] = "✓ Custom preset generated successfully!",

        // Settings OpenRouter AI
        ["Set_Ai_Title"] = "OpenRouter AI Settings",
        ["Set_Ai_Subtitle"] = "Configuration for AI tuning copilot and preset generation",
        ["Set_Ai_ApiKey"] = "OpenRouter API Key",
        ["Set_Ai_ApiKey_Desc"] = "OpenRouter access key used for AI model inference queries",
        ["Set_Ai_Model"] = "Active Neural Model",
        ["Set_Ai_Model_Desc"] = "Neural architecture — adaptive reasoning over Windows tweaks",
        ["Set_Ai_Badge"] = "FREE TIER ACTIVE",
        ["Set_Ai_Status"] = "Ready (OpenRouter Cloud)",

        // Tweaks Page Extra
        ["Tweaks_TweaksCountSuffix"] = "tweaks",
        ["Tweaks_ScrollPrev"] = "Previous",
        ["Tweaks_ScrollNext"] = "Next",
        ["Tweaks_Inspector_EmptyHint"] = "Select a tweak in the left panel to inspect its purpose, mechanism and expected result.",
        ["Tweaks_Section_Compatibility"] = "SYSTEM COMPATIBILITY",
        ["Tweaks_Compatible_Title"] = "Fully Compatible",
        ["Tweaks_Compatible_Desc"] = "This tweak is fully compatible and optimized for your Windows and hardware configuration.",

        // Dashboard Extra
        ["Dash_Score_HeroTitle"] = "Excellent Performance!",
        ["Dash_Score_Desc"] = "System is {0}% optimized. Can be tuned further.",
        ["Dash_Stat_Processes"] = "background processes",
        ["Dash_Stat_SafeRecs"] = "safe recommendations",
        ["Dash_Stat_Services"] = "services",
        ["Dash_Ai_Forecast_Fps"] = "FPS Boost",
        ["Dash_Ai_Forecast_Latency"] = "Input Latency",
        ["Dash_Ai_Forecast_Ram"] = "RAM Freed",
        ["Dash_Latency_Active"] = "Active",

        // Diagnostics / Scanner Extra
        ["Scan_Sub_CpuArch"] = "ARCHITECTURE & COMPUTE CORES",
        ["Scan_Sub_RamSpeed"] = "SPEED & MEMORY CAPACITY",
        ["Scan_Sub_GpuDriver"] = "GRAPHICS ADAPTER & DRIVER",
        ["Scan_Lbl_Vram"] = "VIDEO MEMORY (VRAM)",
        ["Scan_Lbl_DriverVersion"] = "DRIVER VERSION",
        ["Scan_Lbl_HagsScheduling"] = "HAGS SCHEDULING",
        ["Scan_Sub_OsServices"] = "OPERATING SYSTEM & SERVICES",
        ["Scan_Lbl_OsEdition"] = "WINDOWS 11 EDITION",
        ["Scan_Lbl_CoreIsolation_Desc"] = "Core Isolation (HVCI / VBS)",
        ["Scan_Lbl_UsedOf"] = "used of",

        // Optimization Wizard Extra
        ["Opt_Profiles_Title"] = "OPTIMIZATION PROFILES",
        ["Opt_Profiles_Subtitle"] = "CHOOSE SYSTEM WORKLOAD",
        ["Opt_Profiles_Badge"] = "4 PROFILES",
        ["Opt_Control_Title"] = "OPTIMIZATION CONTROLLER",
        ["Opt_Control_Subtitle"] = "SAFETY SAFEGUARDS & RESTORE SNAPSHOT",
        ["Opt_Safety_Active"] = "PROTECTION ACTIVE",
        ["Opt_Safety_Desc"] = "Creates a backup snapshot of registry and drivers prior to applying tweaks",
        ["Opt_Selected_Profile"] = "Selected profile:",
        ["Opt_Tweaks_Suffix"] = "tweaks",
        ["Opt_Audit_Subtitle"] = "OPERATION MONITORING & AUDIT",
        ["Opt_Clear_Logs"] = "Clear",
        ["Opt_Progress_Label"] = "EXECUTION PROGRESS",
        ["Opt_System_Status_Label"] = "SYSTEM STATE",
        ["Opt_Status_Ready"] = "Ready to start optimization.",

        // History Extra
        ["Hist_Refresh_Btn"] = "Refresh",
        ["Hist_Rollback_Action"] = "Rollback",
        ["Hist_Empty_Title"] = "History is empty",
        ["Hist_Rollback_Dialog_Title"] = "Confirm Tweak Rollback",
        ["Hist_Rollback_Dialog_Content"] = "Are you sure you want to rollback tweak \"{0}\"?\nRegistry and system parameters will be restored to their state before it was applied.",
        ["Hist_Clear_Dialog_Title"] = "Clear Audit History",
        ["Hist_Clear_Dialog_Content"] = "Are you sure you want to completely clear the history of applied operations?\nOriginal state records will be deleted. This action cannot be undone.",
        ["Hist_Dialog_Cancel"] = "Cancel",

        // Restore Points Extra
        ["Rest_Sub_Protection"] = "WINDOWS SYSTEM & DRIVE PROTECTION (SRCLIENT.DLL)",
        ["Rest_Sub_Status"] = "RESTORE SERVICE STATUS",
        ["Rest_Sub_Snapshot"] = "SYSTEM REGISTRY & CONFIGURATION SNAPSHOT",
        ["Rest_Sub_Timeline"] = "SAVED RESTORE POINTS TIMELINE",
        ["Rest_Sub_PointsCount"] = "POINTS RECORDED",
        ["Rest_Refresh_Btn"] = "Refresh List",
        ["Rest_Rollback_Btn"] = "Restore",
        ["Rest_Delete_Btn"] = "Delete",
        ["Rest_Empty_Title"] = "No Restore Points Found",
        ["Rest_Rstrui_Tooltip"] = "Launch official Windows system restore tool",
        ["Rest_Rollback_Dialog_Title"] = "Confirm System Restore",
        ["Rest_Rollback_Dialog_Content"] = "Are you sure you want to restore Windows configuration to restore point \"{0}\" (#{1})?\n\nThis will launch the official Windows System Restore wizard (rstrui.exe). A computer restart is required to apply changes.",
        ["Rest_Rollback_Dialog_Btn"] = "Start Restore",
        ["Rest_Delete_Dialog_Title"] = "Delete Restore Point",
        ["Rest_Delete_Dialog_Content"] = "Are you sure you want to delete restore point \"{0}\" (#{1})?\n\nThe shadow copy of system state will be permanently removed. This action cannot be undone.",
        ["Rest_Delete_Dialog_Btn"] = "Delete",
        ["Rest_Dialog_Cancel"] = "Cancel",

        // Settings Extra
        ["Set_Sub_Safety"] = "SYSTEM RESTRICTIONS & CRASH SAFEGUARDS",
        ["Set_Sub_Ai"] = "NEURAL OPTIMIZATION AGENT",
        ["Set_Sub_Lang"] = "LOCALIZATION & REGIONAL PREFERENCES",
        ["Set_Sub_LangSwitch"] = "Instant language switching",
        ["Set_Sub_About"] = "PLATFORM & DEVELOPER INFORMATION",

        // Create Preset Dialog
        ["PresetCreate_Title"] = "Create Preset",
        ["PresetCreate_Subtitle"] = "Export current system configuration to a .silkpreset file",
        ["PresetCreate_ConfigHeader"] = "Active Configuration",
        ["PresetCreate_ActiveCount"] = "Active tweaks to save:",
        ["PresetCreate_NameLabel"] = "Preset Name",
        ["PresetCreate_NamePlaceholder"] = "E.g.: Gaming Pro Profile",
        ["PresetCreate_DescLabel"] = "Description",
        ["PresetCreate_DescPlaceholder"] = "E.g.: Network, latency, and background services tuning",
        ["PresetCreate_AuthorLabel"] = "Author",
        ["PresetCreate_CancelBtn"] = "Cancel",
        ["PresetCreate_SaveBtn"] = "Save",
        ["PresetCreate_DefaultDesc"] = "Custom SilkThread optimization preset"
    };
}
