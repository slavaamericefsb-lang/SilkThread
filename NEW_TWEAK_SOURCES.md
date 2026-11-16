# Источники 50 новых условных настроек

Добавлены 50 разных адресов политик, а не 50 обещаний прироста FPS. Все записи имеют уровень Optional/Medium, описание потери функций, источник и обратимое сохранение исходного реестра через TweakBuilder. Вызовы применения во время этой проверки не выполнялись. Замеры нагрузки и матрица реальных Windows 10/11 не проводились.

| Группа | Количество | Первичный источник |
|---|---:|---|
| Индексация и поиск | 12 | [Search Policy CSP](https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-search), локальный административный шаблон Windows Search.admx |
| Облачное содержимое Windows | 8 | [Experience Policy CSP](https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-experience), CloudContent.admx |
| Синхронизация групп настроек | 7 | [SettingSync Policy CSP](https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-settingsync), SettingSync.admx |
| Лимиты ресурсов Delivery Optimization | 4 | [DeliveryOptimization Policy CSP](https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-deliveryoptimization), DeliveryOptimization.admx |
| Работа Проводника | 4 | [WindowsExplorer Policy CSP](https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-windowsexplorer), [Thumbnails Policy CSP](https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-thumbnails) |
| Разрешения UWP-приложений | 10 | [Privacy Policy CSP](https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-privacy), AppPrivacy.admx |
| Отправка отчётов об ошибках | 3 | [WER Settings](https://learn.microsoft.com/en-us/windows/win32/wer/wer-settings), ErrorReporting.admx |
| Облачное распознавание речи | 1 | [AllowInputPersonalization](https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-privacy#allowinputpersonalization) |
| История буфера обмена | 1 | [AllowClipboardHistory](https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-experience#allowclipboardhistory) |

Точные адреса HKLM/HKCU, DWORD-значения и ссылки расположены в DocumentedWindowsTweaks.cs и HowItWorks каждой записи. Для старых параметров, отсутствующих на странице CSP, указан конкретный policy ID из поставленного с Windows ADMX. Это отдельное доказательство схемы политики, а не утверждение, что страница CSP описывает отсутствующий там параметр.

Консервативная совместимость: managed-редакции Pro, Enterprise, Education (включая IoT Enterprise), Windows build19044+. Home исключена. Облачные политики ограничены Enterprise/Education; специфичные для Windows11 параметры требуют build22000+, Search Highlights —22621+. Проверка редакции намеренно ограничивает предложение настройки; запись DWORD сама по себе не доказывает, что текущая служба уже перечитала политику. Доменный GPO/MDM может иметь приоритет.

Ограничения: почтовые параметры полезны только при соответствующем индексируемом провайдере классического Outlook/Exchange. Они не относятся к новому Outlook. Ограничения разрешений UWP не равны отключению всех Win32-процессов. Ограничения синхронизации действуют только на поддерживаемую Windows группу настроек. Некоторые записи задают поведение, уже являющееся стандартным; выигрыша тогда нет. Delivery Optimization продолжает получать обновления, но ограничение полосы замедляет их загрузку. Defender, безопасность обновлений и механизмы системной защиты новыми записями не отключаются.
