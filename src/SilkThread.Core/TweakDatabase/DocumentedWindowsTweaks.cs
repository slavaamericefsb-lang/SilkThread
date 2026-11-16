using SilkThread.Core.Models;

namespace SilkThread.Core.TweakDatabase;

/// <summary>Opt-in documented feature/resource policies. No measured FPS claims.</summary>
public static class DocumentedWindowsTweaks
{
    private const string Docs = "https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-";
    private const string Search = @"SOFTWARE\Policies\Microsoft\Windows\Windows Search";
    private const string Cloud = @"Software\Policies\Microsoft\Windows\CloudContent";
    private const string Sync = @"Software\Policies\Microsoft\Windows\SettingSync";
    private const string Delivery = @"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization";
    private const string Explorer = @"Software\Policies\Microsoft\Windows\Explorer";
    private const string Privacy = @"Software\Policies\Microsoft\Windows\AppPrivacy";
    private const string Wer = @"SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting";

    public static IEnumerable<TweakDefinition> GetTweaks()
    {
        yield return Make("search_cloud", Search, "AllowCloudSearch", 0, "Поиск: исключить облачные источники", "Документы OneDrive/SharePoint исчезнут из результатов Windows Search.", "search#allowcloudsearch");
        yield return Make("search_encrypted", Search, "AllowIndexingEncryptedStoresOrItems", 0, "Поиск: не индексировать зашифрованные файлы", "Содержимое зашифрованных файлов не будет находиться через индекс.", "search#allowindexingencryptedstoresoritems");
        yield return Make("search_highlights", Search, "EnableDynamicContentInWSB", 0, "Поиск: отключить динамические карточки", "Search Highlights исчезнет из поиска.", "search#allowsearchhighlights", minimumBuild: 22621);
        yield return Make("search_location", Search, "AllowSearchToUseLocation", 0, "Поиск: не использовать местоположение", "Локальные подсказки могут стать менее релевантными.", "search#allowsearchtouselocation");
        yield return Make("search_removable", Search, "DisableRemovableDriveIndexing", 1, "Поиск: не индексировать съёмные диски", "Поиск на внешних дисках может стать медленнее.", "search#disableremovabledriveindexing");
        yield return Make("search_remote", Search, "PreventRemoteQueries", 1, "Поиск: запретить удалённые запросы к индексу", "Другие компьютеры не смогут выполнять поиск через локальный индекс.", "search#preventremotequeries");
        // Additional Search.admx policies: exact DWORD/scope verified against the Windows ADMX.
        yield return Make("search_battery", Search, "PreventIndexOnBattery", 1, "Поиск: приостанавливать индексацию от батареи", "Индекс отстаёт от изменений во время работы от батареи.", "search", template: "Search.admx:PreventIndexOnBattery");
        yield return Make("search_offline", Search, "PreventIndexingOfflineFiles", 1, "Поиск: не индексировать автономные сетевые файлы", "Содержимое Offline Files не будет находиться через индекс.", "search", template: "Search.admx:PreventIndexingOfflineFiles");
        yield return Make("search_exchange", Search, "PreventIndexingUncachedExchangeFolders", 1, "Поиск: исключить некэшированные папки Exchange", "Только классический Outlook с Exchange: поиск некэшированной почты ограничивается. Не влияет на новый Outlook.", "search", template: "Search.admx:PreventIndexingUncachedExchangeFolders");
        yield return Make("search_delegate", Search, "EnableIndexingDelegateMailboxes", 0, "Поиск: не индексировать делегированные ящики", "Только классический Outlook: поиск в делегированных ящиках ограничивается.", "search", template: "Search.admx:EnableIndexingDelegateMailboxes");
        yield return Make("search_attachments", Search, "PreventIndexingEmailAttachments", 1, "Поиск: не индексировать почтовые вложения", "Требуется индексируемый почтовый провайдер; поиск внутри вложений станет недоступен.", "search", template: "Search.admx:PreventIndexingEmailAttachments");
        yield return Make("search_public", Search, "PreventIndexingPublicFolders", 1, "Поиск: не индексировать общие папки Exchange", "Только классический Outlook: общие папки Exchange не будут индексироваться.", "search", template: "Search.admx:PreventIndexingPublicFolders");

        yield return Make("cloud_desktop", Cloud, "DisableSpotlightCollectionOnDesktop", 1, "Отключить коллекцию Spotlight рабочего стола", "Фоновые изображения Spotlight станут недоступны.", "experience#allowspotlightcollection", user: true, minimumBuild: 22000, enterpriseOnly: true);
        yield return Make("cloud_thirdparty", Cloud, "DisableThirdPartySuggestions", 1, "Отключить сторонние рекомендации Spotlight", "Сторонние рекомендации исчезнут; сам Spotlight сохраняется.", "experience#allowthirdpartysuggestionsinwindowsspotlight", user: true, enterpriseOnly: true);
        yield return Make("cloud_consumer", Cloud, "DisableWindowsConsumerFeatures", 1, "Отключить предложения потребительских приложений", "Windows перестанет предлагать потребительские приложения через этот механизм.", "experience#allowwindowsconsumerfeatures", enterpriseOnly: true);
        yield return Make("cloud_action", Cloud, "DisableWindowsSpotlightOnActionCenter", 1, "Отключить Spotlight в центре уведомлений", "Подсказки Spotlight в центре уведомлений исчезнут.", "experience#allowwindowsspotlightonactioncenter", user: true, enterpriseOnly: true);
        yield return Make("cloud_settings", Cloud, "DisableWindowsSpotlightOnSettings", 1, "Отключить Spotlight в параметрах", "Рекомендованное содержимое параметров исчезнет.", "experience#allowwindowsspotlightonsettings", user: true, enterpriseOnly: true);
        yield return Make("cloud_welcome", Cloud, "DisableWindowsSpotlightWindowsWelcomeExperience", 1, "Отключить приветственные рекомендации Windows", "После обновлений не показывается приветственный экран рекомендаций.", "experience#allowwindowsspotlightwindowswelcomeexperience", user: true, enterpriseOnly: true);
        yield return Make("cloud_optimized", Cloud, "DisableCloudOptimizedContent", 1, "Не получать облачно оптимизированное содержимое", "Используется стандартное содержимое вместо облачной персонализации.", "experience#disablecloudoptimizedcontent", minimumBuild: 22000, enterpriseOnly: true);
        yield return Make("cloud_account", Cloud, "DisableConsumerAccountStateContent", 1, "Не получать предложения по состоянию аккаунта", "Исчезают предложения по состоянию потребительского аккаунта.", "experience#disableconsumeraccountstatecontent", minimumBuild: 22000, enterpriseOnly: true);

        yield return Make("sync_appsettings", Sync, "DisableApplicationSettingSync", 2, "Не синхронизировать параметры приложений", "Параметры приложений не переносятся между устройствами.", "admx-settingsync#disableapplicationsettingsync");
        yield return Make("sync_apps", Sync, "DisableAppSyncSettingSync", 2, "Не синхронизировать список приложений", "Список приложений не переносится между устройствами.", "admx-settingsync#disableappsyncsettingsync");
        yield return Make("sync_theme", Sync, "DisableDesktopThemeSettingSync", 2, "Не синхронизировать тему рабочего стола", "Тема рабочего стола не переносится между устройствами.", "admx-settingsync#disabledesktopthemesettingsync");
        yield return Make("sync_personal", Sync, "DisablePersonalizationSettingSync", 2, "Не синхронизировать персонализацию Windows", "Персонализация не переносится между устройствами.", "admx-settingsync#disablepersonalizationsettingsync");
        yield return Make("sync_windows", Sync, "DisableWindowsSettingSync", 2, "Не синхронизировать прочие параметры Windows", "Прочие параметры Windows не переносятся между устройствами.", "admx-settingsync#disablewindowssettingsync");
        yield return Make("sync_metered", Sync, "DisableSyncOnPaidNetwork", 1, "Не синхронизировать настройки в тарифицируемой сети", "Синхронизация откладывается до нетарифицируемого подключения.", "admx-settingsync", template: "SettingSync.admx:DisableSyncOnPaidNetwork");
        yield return Make("sync_start", Sync, "DisableStartLayoutSettingSync", 2, "Не синхронизировать раскладку Start", "Раскладка Start не переносится там, где такая синхронизация поддерживается.", "admx-settingsync", template: "SettingSync.admx:DisableStartLayoutSettingSync");

        yield return Make("do_cache", Delivery, "DOAbsoluteMaxCacheSize", 5, "Ограничить кэш DO до 5 ГБ", "Меньше занятое место; повторные загрузки могут возрасти. Приоритет над процентным размером кэша.", "deliveryoptimization#doabsolutemaxcachesize");
        yield return Make("do_background", Delivery, "DOMaxBackgroundDownloadBandwidth", 1024, "Ограничить фоновую загрузку DO до 1024 КБ/с", "Обновления и содержимое DO загружаются дольше. Обновления не отключаются.", "deliveryoptimization#domaxbackgrounddownloadbandwidth");
        yield return Make("do_upload", Delivery, "DOMonthlyUploadDataCap", 1, "Ограничить отдачу DO до 1 ГБ в месяц", "Лимит относится к интернет-пирам, не к LAN. Уменьшается отдача обновлений через интернет.", "deliveryoptimization#domonthlyuploaddatacap");
        yield return Make("do_ram", Delivery, "DOMinRAMAllowedToPeer", 8, "Не участвовать в P2P DO при RAM менее 8 ГБ", "На ПК с малым объёмом RAM кэш P2P не используется; интернет-загрузки могут возрасти.", "deliveryoptimization#dominramallowedtopeer");

        yield return Make("explorer_icons", @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "DisableThumbnails", 1, "Показывать значки вместо миниатюр", "Превью изображений и видео в Проводнике исчезнут; меньше работы декодеров.", "admx-thumbnails#disablethumbnails", user: true);
        yield return Make("explorer_remoteicons", Explorer, "EnableShellShortcutIconRemotePath", 0, "Не получать значки ярлыков с удалённых путей", "Удалённые значки ярлыков заменяются стандартными.", "admx-windowsexplorer#enableshellshortcuticonremotepath");
        yield return Make("explorer_snippets", Explorer, "HideContentViewModeSnippets", 1, "Не показывать фрагменты текста в поиске Проводника", "В представлении содержимого исчезнут текстовые фрагменты результатов.", "admx-windowsexplorer#hidecontentviewmodesnippets", user: true);
        yield return Make("explorer_linktracking", @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "LinkResolveIgnoreLinkInfo", 1, "Не отслеживать сетевые перемещения целей ярлыков", "Перемещённые цели ярлыков могут не находиться автоматически.", "admx-windowsexplorer#linkresolveignorelinkinfo", user: true);

        yield return AppPermission("account", "данные аккаунта", "LetAppsAccessAccountInfo");
        yield return AppPermission("calendar", "календарь", "LetAppsAccessCalendar");
        yield return AppPermission("calls", "историю звонков", "LetAppsAccessCallHistory");
        yield return AppPermission("contacts", "контакты", "LetAppsAccessContacts");
        yield return AppPermission("email", "электронную почту", "LetAppsAccessEmail");
        yield return AppPermission("messages", "сообщения", "LetAppsAccessMessaging");
        yield return AppPermission("motion", "датчики движения", "LetAppsAccessMotion");
        yield return AppPermission("notifications", "уведомления других приложений", "LetAppsAccessNotifications");
        yield return AppPermission("tasks", "задачи", "LetAppsAccessTasks");
        yield return AppPermission("radios", "управление радиомодулями", "LetAppsAccessRadios");

        yield return Make("wer_extra", Wer, "DontSendAdditionalData", 1, "Не отправлять дополнительные данные WER", "Меньше данных при сбоях; анализ причин может стать менее точным.", "errorreporting", template: "ErrorReporting.admx:WerNoSecondLevelData_2");
        yield return Make("wer_power", Wer, "BypassPowerThrottling", 0, "Соблюдать ограничения питания при отправке WER", "Отправка откладывается от батареи. Обычно уже является настройкой по умолчанию.", "errorreporting", template: "ErrorReporting.admx:WerBypassPowerThrottling_2");
        yield return Make("wer_network", Wer, "BypassNetworkCostThrottling", 0, "Соблюдать тарификацию сети при отправке WER", "Отправка откладывается в тарифицируемой сети. Обычно уже является настройкой по умолчанию.", "errorreporting", template: "ErrorReporting.admx:WerBypassNetworkCostThrottling_2");
        yield return Make("speech_online", @"Software\Policies\Microsoft\InputPersonalization", "AllowInputPersonalization", 0, "Отключить облачное распознавание речи", "Облачная диктовка и зависящие от неё приложения перестанут работать.", "privacy#allowinputpersonalization");
        yield return Make("clipboard_history", @"Software\Policies\Microsoft\Windows\System", "AllowClipboardHistory", 0, "Не сохранять историю буфера обмена", "История Win+V недоступна. Обычные копирование и вставка сохраняются.", "experience#allowclipboardhistory");
    }

    private static TweakDefinition AppPermission(string id, string label, string value) => Make(
        "app_" + id, Privacy, value, 2, "Запретить UWP-приложениям: " + label,
        "Ограничивает соответствующую функцию UWP-приложений; пользователь не сможет разрешить доступ. Не ограничивает все Win32-программы. Снижение нагрузки возможно только у приложений, использующих эту возможность.",
        "privacy#" + value.ToLowerInvariant());

    private static TweakDefinition Make(string id, string key, string value, int desired,
        string title, string tradeoff, string source, bool user = false, int minimumBuild = 19044,
        bool enterpriseOnly = false, string? template = null)
    {
        var tweak = TweakBuilder.CreateRegistryDword(
        id: "doc_" + id, title: title, description: tradeoff,
        howItWorks: $"Документированная политика REG_DWORD: {(user ? "HKCU" : "HKLM")}\\{key}\\{value} = {desired}. Источник: {Docs}{source}. "
            + (template == null ? "" : $"Дополнительно проверен административный шаблон Windows: {template}. "),
        problemSolved: "Условное уменьшение работы конкретной необязательной функции, только если она используется. Не универсальное ускорение компьютера.",
        expectedResult: "Возможно снижение фоновой нагрузки; измеренный прирост FPS не заявляется. Политики домена/MDM могут иметь приоритет. После применения может требоваться повторный вход или перезагрузка. Откат восстанавливает сохранённое исходное значение и тип.",
        category: key == Search ? TweakCategory.WindowsPerformance : key == Delivery ? TweakCategory.NetworkOptimization
            : key == Wer ? TweakCategory.SystemMaintenance : key == Privacy ? TweakCategory.SecurityAndPrivacy
            : id.StartsWith("explorer_") ? TweakCategory.WindowsExplorerAndUi : TweakCategory.BackgroundProcesses,
        risk: RiskLevel.Medium, recommendation: RecommendationLevel.Optional,
        hive: user ? "HKCU" : "HKLM", subKey: key, valueName: value, appliedValue: desired,
        requiresReboot: true, minWindowsBuild: minimumBuild,
        customCompatibility: profile =>
        {
            string edition = profile.Os.Edition;
            bool enterprise = edition.Contains("Enterprise", StringComparison.OrdinalIgnoreCase)
                || edition.Contains("Education", StringComparison.OrdinalIgnoreCase);
            bool pro = edition.StartsWith("Professional", StringComparison.OrdinalIgnoreCase)
                || edition.Equals("Pro", StringComparison.OrdinalIgnoreCase);
            if (profile.Os.BuildNumber < minimumBuild)
                return CompatibilityResult.Incompatible($"Requires Windows build {minimumBuild} or later.");
            if (!enterprise && (enterpriseOnly || !pro))
                return CompatibilityResult.Incompatible(enterpriseOnly
                    ? "Conservatively limited to Enterprise/Education, including IoT Enterprise."
                    : "Documented managed edition required: Pro, Enterprise or Education. Home is not supported.");
            return CompatibilityResult.Compatible;
        });
        if (key == Sync && desired == 2)
            return TweakBuilder.CreateMultiRegistry(tweak.Id, tweak.Title, tweak.Description,
                tweak.HowItWorks + $" Парная политика {value}UserOverride=1 запрещает пользователю отменять ограничение.",
                tweak.ProblemSolved, tweak.ExpectedResult, tweak.Category, tweak.Risk, tweak.Recommendation,
                new[] { ("HKLM", key, value, desired), ("HKLM", key, value + "UserOverride", 1) },
                requiresReboot: true, minWindowsBuild: minimumBuild,
                customCompatibility: tweak.CustomCompatibilityCheck);
        return tweak;
    }
}
