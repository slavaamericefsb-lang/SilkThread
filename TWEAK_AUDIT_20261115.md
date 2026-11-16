# Аудит исходного каталога SilkThread — 15 ноября 2026

## Область и пределы проверки

Проверены статические определения во всех 15 фабриках исходного каталога, до интеграции нового пакета: **254 записи**, уникальность ID, адреса и значения записей реестра, пересечения одиночных и составных операций, заявленные эффекты, основные ограничения применимости. Это аудит реализации и документации, а не испытание FPS на всех сборках Windows 10/11. Запись значения реестра и успешное последующее чтение доказывают только запись, а не использование параметра Windows. Отсутствие первичного источника не доказывает неработоспособность; такие настройки требуют карантина/экспериментального статуса до проверки. Команды применения и отката не исполнялись.

**Статус отчёта:** зафиксированные ниже дефекты относятся к исходным определениям. Факт исправления следует устанавливать по итоговым тестам/коду, этот документ сам по себе не объявляет их исправленными. Новые50 записей интегрируются другим этапом и должны проходить те же проверки отдельно.

## Подтверждённые пересечения и ошибки

1. `gaming_game_bar_presence_writer_disable` повторяет `AppCaptureEnabled=0` из `gaming_game_dvr_disable`.
2. `gaming_game_dvr_dxgi_compatibility` повторяет `GameDVR_DXGIHonorFSEWindowsCompatible=1` из `gaming_fullscreen_exclusive_fse`.
3. `win_start_bing_search_disable` повторяет `DisableSearchBoxSuggestions=1` из `win_start_menu_web_search_disable`, дополнительно пишет устаревший BingSearchEnabled. Рекомендуется оставить один канонический ID; старые ID должны оставаться разрешимыми для журнала/отката.
4. `maint_update_peer_caching_disable` пишет `DODownloadMode=1`, который **включает** LAN-peering, тогда как `win_delivery_optimization_p2p_disable` пишет0. Это противоречие, а не два независимых улучшения. [Документация Delivery Optimization](https://learn.microsoft.com/en-us/windows/deployment/do/waas-delivery-optimization-reference).
5. `ram_system_responsiveness_zero` не обеспечивает0%: MMCSS интерпретирует значения ниже10 как20. `GPU Priority` и `SFIO Priority` не используются; SFIO должен быть строкой, а не DWORD1. Касается `cpu_mmcss_game_tasks`, `gpu_dwm_high_priority`, `adv_dwm_thread_priority_realtime`, `adv_mmcss_audio_realtime_priority`. [Документация MMCSS](https://learn.microsoft.com/en-us/windows/win32/procthread/multimedia-class-scheduler-service).
6. `gpu_tdr_delay_extend`: изменение TDR служит отладке драйверов и не является ускорением. Microsoft прямо исключает обычное изменение таких ключей конечными пользователями. [TDR](https://learn.microsoft.com/en-us/windows-hardware/drivers/display/tdr-registry-keys).
7. `win_search_indexer_backoff_disable`: значение1 заставляет индексирование продолжаться с полной скоростью при высокой активности. Заявлять уменьшение фоновой нагрузки нельзя. [Search/DisableBackoff](https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-search#disablebackoff).
8. `net_disable_netbios_overhead`: глобальная запись `NetBT\Parameters\NetbiosOptions` не заменяет настройку конкретного адаптера. Нужен поддерживаемый метод с чтением/снимком каждого адаптера. [Win32_NetworkAdapterConfiguration](https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-networkadapterconfiguration).
9. `net_tcp_window_auto_tuning_normal`, `net_rsc_receive_segment_coalescing_disable`, `net_ecn_explicit_congestion_notification`: существующие произвольные DWORD не доказывают настройку TCP. Для этих возможностей документированы netsh/NetTCPSetting. Нужен переход на поддерживаемый интерфейс с реальной проверкой активного состояния, а не один успешный registry-readback. [netsh interface](https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/netsh-interface).
10. `bg_edge_prelaunch_disable`: `AllowPrelaunch` помещён в Chromium Edge policy-path, хотя этот параметр документирован для legacy Edge в `MicrosoftEdge\Main`. Вторая операция BackgroundModeEnabled имеет другую область действия. [Browser CSP](https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-browser#allowprelaunch).
11. `storage_disable_link_tracking_ntfs`: `NtfsDisableCompression` выключает сжатие NTFS, не tracking ссылок. `storage_ntfs_memory_usage_increase` может уменьшать доступную память другим процессам; это сценарная настройка, не универсальная рекомендация. [fsutil behavior](https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/fsutil-behavior).

## Типы реестра: локальная read-only проверка

Проверка GetValueKind на данной машине без записи подтвердила: `WindowMetrics\MinAnimate`, `Mouse\MouseTrails`, `StickyKeys/ToggleKeys/Keyboard Response\Flags` — String; `Explorer\link` — Binary; `ConsentStore\appDiagnostics\Value` — String. Исходные определения пишут DWORD. Для ConsentStore требуется семантический Allow/Deny, а не число0. Это существенный дефект даже если чтение собственного DWORD потом возвращает Applied.

`MenuShowDelay`, `MouseHoverTime`, `DoubleClickSpeed` уже имеют DWORD на этой машине: это **не доказательство корректного системного типа**, поскольку прежние твики могли переписать их. Для установления контракта требуется Windows API SystemParametersInfo/SetDoubleClickTime или чистый профиль Windows. До проверки не сертифицировать эти DWORD как рабочие.

## Совместимость и честное описание эффекта

* `Attributes=0` у PowerSettings только раскрывает настройку в UI. Не меняет активную AC/DC политику и само по себе не ускоряет ПК; учитывать отдельно от настроек производительности. Создание отсутствующего GUID-ключа не доказывает поддержку.
* AMD display adapter путь с фиксированным `0000` не гарантирует попадание в AMD-адаптер. Требуется динамическое сопоставление PNP/driver instance; наличия AMD GPU в профиле мало.
* Отключение SmartScreen, VBS/HVCI, mitigations, Defender/cloud submission не должно попадать в автоматическую безопасную рекомендацию для всех ПК. Это изменение защитных свойств, а не гарантированный прирост FPS.
* Наличие минимального build недостаточно для некоторых policy: учитывать редакцию Windows, удалённые компоненты и максимальную версию поддержки. В текущем CompatibilityEngine проверки редакции/максимального build нет.
* Строки “instant”, “eliminates”, “1–5ms”, гарантии throughput/отсутствия сбоев не подкреплены измерениями. Заменять на конкретное изменение поведения и условия применимости. Тест registry-write нельзя выдавать за benchmark.
* `storage_disable_hibernation_file` меняет HibernateEnabled напрямую, но это не доказывает освобождение hiberfil.sys. Поддерживаемый жизненный цикл — powercfg /hibernate и соответствующий снимок состояния.
* Условия к SSD должны отклонять неизвестный тип системного диска, либо требовать явной проверки; текущая проверка исключает лишь буквальный HDD, поэтому unknown проходит.

## Полная статическая инвентаризация исходных254 записей

“Статическая проверка” означает отсутствие подтверждённого дефекта в этом проходе; **не** означает доказанную эффективность или сертификацию на всех ОС. Записи с неизвестным контрактом вынесены в явную очередь проверки. Координаты соответствуют файлам до интеграции правок.

| ID | Определение | Результат аудита |
|---|---|---|
| `adv_dwm_thread_priority_realtime` | `AdvancedTweaks.cs:10` (CreateMultiRegistryMixed) | GPU Priority не используется; нет доказательства подписки DWM на созданный профиль |
| `adv_spectre_meltdown_override_experimental` | `AdvancedTweaks.cs:28` (CreateMultiRegistry) | Снижает защиту от аппаратных side-channel атак; только явный эксперимент |
| `adv_protection_mode_object_directories` | `AdvancedTweaks.cs:46` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `adv_global_flag_page_heap_disable` | `AdvancedTweaks.cs:63` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `adv_kernel_sehop_enable` | `AdvancedTweaks.cs:80` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `adv_mmcss_audio_realtime_priority` | `AdvancedTweaks.cs:97` (CreateMultiRegistryMixed) | SFIO Priority не используется и должен быть REG_SZ; гарантии отсутствия треска недоказаны |
| `adv_kernel_pool_tagging_gaming` | `AdvancedTweaks.cs:115` (CreateRegistryDword) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `adv_kernel_dpc_timeout_polynomial` | `AdvancedTweaks.cs:132` (CreateRegistryDword) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `bg_diagtrack_service_disable` | `BackgroundProcessesTweaks.cs:10` (CreateServiceTweak) | Статическая проверка; runtime/benchmark не выполнены |
| `bg_edge_prelaunch_disable` | `BackgroundProcessesTweaks.cs:24` (CreateMultiRegistry) | Частичная неработоспособность: AllowPrelaunch относится к старому MicrosoftEdge\Main |
| `bg_wer_service_disable` | `BackgroundProcessesTweaks.cs:41` (CreateServiceTweak) | Статическая проверка; runtime/benchmark не выполнены |
| `bg_widgets_news_interests_disable` | `BackgroundProcessesTweaks.cs:55` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `bg_content_delivery_suggestions_disable` | `BackgroundProcessesTweaks.cs:72` (CreateMultiRegistry) | Статическая проверка; runtime/benchmark не выполнены |
| `bg_customer_experience_ceip_disable` | `BackgroundProcessesTweaks.cs:91` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `bg_tailored_experiences_disable` | `BackgroundProcessesTweaks.cs:108` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `bg_windows_spotlight_disable` | `BackgroundProcessesTweaks.cs:125` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `bg_compatibility_telemetry_disable` | `BackgroundProcessesTweaks.cs:142` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `bg_smartscreen_prompt_latency` | `BackgroundProcessesTweaks.cs:159` (CreateRegistryDword) | Отключает защиту SmartScreen; не считать низкорисковой оптимизацией |
| `bg_device_census_disable` | `BackgroundProcessesTweaks.cs:176` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `bg_copilot_runtime_disable` | `BackgroundProcessesTweaks.cs:193` (CreateMultiRegistry) | Статическая проверка; runtime/benchmark не выполнены |
| `bg_onedrive_sync_startup_disable` | `BackgroundProcessesTweaks.cs:211` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `bg_feedback_hub_telemetry_disable` | `BackgroundProcessesTweaks.cs:228` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `bg_appx_deployment_optimization` | `BackgroundProcessesTweaks.cs:245` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `bg_cloud_clipboard_sync_disable` | `BackgroundProcessesTweaks.cs:262` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `bg_phone_link_background_disable` | `BackgroundProcessesTweaks.cs:279` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `bg_windows_tips_suggestions_disable` | `BackgroundProcessesTweaks.cs:296` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `bg_recall_disable_24h2` | `BackgroundProcessesTweaks.cs:313` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `bg_edge_startup_boost_disable` | `BackgroundProcessesTweaks.cs:331` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `cpu_power_throttling_disable` | `CpuTweaks.cs:10` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `cpu_core_parking_unhide` | `CpuTweaks.cs:27` (CreateRegistryDword) | Только раскрывает настройку питания; не меняет AC/DC |
| `cpu_win32_priority_separation` | `CpuTweaks.cs:44` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `cpu_mmcss_game_tasks` | `CpuTweaks.cs:61` (CreateMultiRegistryMixed) | GPU/SFIO Priority не используются; SFIO имеет неверный тип, не обещать эффект GPU |
| `cpu_additional_worker_threads` | `CpuTweaks.cs:80` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `cpu_intel_speedshift_unhide` | `CpuTweaks.cs:97` (CreateRegistryDword) | Только раскрывает настройку питания; не меняет AC/DC |
| `cpu_amd_thread_dpc_affinity` | `CpuTweaks.cs:115` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `cpu_idle_resiliency_disable` | `CpuTweaks.cs:133` (CreateRegistryDword) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `cpu_dxgkrnl_latency_tolerance` | `CpuTweaks.cs:150` (CreateRegistryDword) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `cpu_processor_boost_mode_unhide` | `CpuTweaks.cs:167` (CreateRegistryDword) | Только раскрывает настройку питания; не меняет AC/DC |
| `cpu_affinity_prioritization_foreground` | `CpuTweaks.cs:184` (CreateRegistryDword) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `cpu_thread_scheduling_model_unhide` | `CpuTweaks.cs:201` (CreateRegistryDword) | Только раскрывает настройку питания; не меняет AC/DC |
| `cpu_distribute_timers` | `CpuTweaks.cs:218` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `cpu_intel_ppm_epb_performance` | `CpuTweaks.cs:236` (CreateRegistryDword) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `cpu_amd_ppm_perf_boost` | `CpuTweaks.cs:254` (CreateRegistryDword) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `cpu_svm_hyperv_interrupt_steering` | `CpuTweaks.cs:272` (CreateRegistryDword) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `cpu_hetero_short_thread_runtime` | `CpuTweaks.cs:289` (CreateRegistryDword) | Только раскрывает настройку питания; не меняет AC/DC |
| `cpu_kernel_dpc_watchdog_profile` | `CpuTweaks.cs:306` (CreateRegistryDword) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `cpu_disable_vbs_hvci` | `CpuTweaks.cs:324` (CreateMultiRegistry) | Работающая, но снижает защиту; высокорисковая, нельзя автоматически рекомендовать всем |
| `gaming_game_mode_enable` | `GamingPerformanceTweaks.cs:10` (CreateMultiRegistry) | Статическая проверка; runtime/benchmark не выполнены |
| `gaming_game_dvr_disable` | `GamingPerformanceTweaks.cs:27` (CreateMultiRegistry) | Статическая проверка; runtime/benchmark не выполнены |
| `gaming_game_bar_presence_writer_disable` | `GamingPerformanceTweaks.cs:46` (CreateRegistryDword) | Дубликат/перекрытие записи; объединить операции и сохранить миграцию старого ID |
| `gaming_fullscreen_exclusive_fse` | `GamingPerformanceTweaks.cs:63` (CreateMultiRegistry) | Статическая проверка; runtime/benchmark не выполнены |
| `gaming_game_bar_nexus_disable` | `GamingPerformanceTweaks.cs:81` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `gaming_audio_ducker_disable` | `GamingPerformanceTweaks.cs:98` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `gaming_directx_gpu_energy_driver` | `GamingPerformanceTweaks.cs:115` (CreateRegistryDword) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `gaming_game_dvr_dxgi_compatibility` | `GamingPerformanceTweaks.cs:132` (CreateRegistryDword) | Дубликат/перекрытие записи; объединить операции и сохранить миграцию старого ID |
| `gaming_fullscreen_optimizations_global` | `GamingPerformanceTweaks.cs:149` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `gaming_auto_hdr_notification_disable` | `GamingPerformanceTweaks.cs:166` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `gaming_game_config_store_flags` | `GamingPerformanceTweaks.cs:183` (CreateRegistryDword) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `gaming_optimizations_for_windowed_games_enable` | `GamingPerformanceTweaks.cs:200` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `gaming_xbox_live_auth_manager_disable` | `GamingPerformanceTweaks.cs:218` (CreateServiceStartup) | Статическая проверка; runtime/benchmark не выполнены |
| `gaming_spatial_audio_overhead_disable` | `GamingPerformanceTweaks.cs:232` (CreateRegistryDword) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `gaming_discord_overlay_hook_mitigation` | `GamingPerformanceTweaks.cs:249` (CreateRegistryDword) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `gaming_directstorage_bypass_io` | `GamingPerformanceTweaks.cs:266` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `gaming_disable_game_bar_tips` | `GamingPerformanceTweaks.cs:284` (CreateMultiRegistry) | Статическая проверка; runtime/benchmark не выполнены |
| `gaming_steam_client_gpu_accel_priority` | `GamingPerformanceTweaks.cs:301` (CreateRegistryDword) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `gpu_hags_enable` | `GpuTweaks.cs:10` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `gpu_variable_refresh_rate_global` | `GpuTweaks.cs:28` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `gpu_nvidia_power_mizer_max` | `GpuTweaks.cs:45` (CreateMultiRegistry) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `gpu_nvidia_low_latency_mode` | `GpuTweaks.cs:65` (CreateRegistryDword) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `gpu_amd_anti_lag_profile` | `GpuTweaks.cs:83` (CreateRegistryDword) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `gpu_amd_shader_cache_limit` | `GpuTweaks.cs:101` (CreateRegistryDword) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `gpu_tdr_delay_extend` | `GpuTweaks.cs:119` (CreateMultiRegistry) | Карантин: TDR ключи предназначены для разработки драйверов, не ускоряют GPU |
| `gpu_dwm_high_priority` | `GpuTweaks.cs:137` (CreateRegistryDword) | GPU Priority не используется согласно документации MMCSS |
| `gpu_dx_prefer_high_performance` | `GpuTweaks.cs:154` (CreateRegistryString) | Статическая проверка; runtime/benchmark не выполнены |
| `gpu_preemption_scheduler_enable` | `GpuTweaks.cs:171` (CreateRegistryDword) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `gpu_borderless_direct_flip_optimization` | `GpuTweaks.cs:188` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `gpu_nvidia_telemetry_container_disable` | `GpuTweaks.cs:205` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `gpu_dxgi_swapchain_waitable_object` | `GpuTweaks.cs:223` (CreateRegistryDword) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `gpu_amd_ulps_disable` | `GpuTweaks.cs:240` (CreateRegistryDword) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `gpu_intel_arc_adaptive_sync` | `GpuTweaks.cs:259` (CreateRegistryDword) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `gpu_disable_mpo` | `GpuTweaks.cs:277` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `gpu_vram_allocation_priority` | `GpuTweaks.cs:295` (CreateRegistryDword) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `gpu_nvidia_ansel_overlay_disable` | `GpuTweaks.cs:313` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `input_mouse_acceleration_disable` | `InputLatencyTweaks.cs:10` (CreateMultiRegistryMixed) | Статическая проверка; runtime/benchmark не выполнены |
| `input_keyboard_repeat_rate_max` | `InputLatencyTweaks.cs:28` (CreateRegistryString) | Статическая проверка; runtime/benchmark не выполнены |
| `input_keyboard_repeat_delay_min` | `InputLatencyTweaks.cs:45` (CreateRegistryString) | Статическая проверка; runtime/benchmark не выполнены |
| `input_sticky_keys_prompt_disable` | `InputLatencyTweaks.cs:62` (CreateRegistryDword) | Ошибка типа данных: DWORD вместо строкового/бинарного значения; исправить до применения |
| `input_toggle_keys_prompt_disable` | `InputLatencyTweaks.cs:79` (CreateRegistryDword) | Ошибка типа данных: DWORD вместо строкового/бинарного значения; исправить до применения |
| `input_filter_keys_disable` | `InputLatencyTweaks.cs:96` (CreateRegistryDword) | Ошибка типа данных: DWORD вместо строкового/бинарного значения; исправить до применения |
| `input_usb_selective_suspend_disable` | `InputLatencyTweaks.cs:113` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `input_cursor_shadow_disable` | `InputLatencyTweaks.cs:130` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `input_touch_prediction_latency` | `InputLatencyTweaks.cs:147` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `input_mouse_hover_time_decrease` | `InputLatencyTweaks.cs:164` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `input_double_click_time_standard` | `InputLatencyTweaks.cs:181` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `input_hid_driver_polling_priority` | `InputLatencyTweaks.cs:198` (CreateRegistryDword) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `input_keyboard_data_queue_size` | `InputLatencyTweaks.cs:215` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `input_mouse_data_queue_size` | `InputLatencyTweaks.cs:233` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `input_csgo_raw_input_buffer_coalescing` | `InputLatencyTweaks.cs:251` (CreateRegistryDword) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `input_gamepad_xinput_polling_latency` | `InputLatencyTweaks.cs:268` (CreateRegistryDword) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `input_pointer_trails_force_disable` | `InputLatencyTweaks.cs:285` (CreateMultiRegistry) | Ошибка типа данных: DWORD вместо строкового/бинарного значения; исправить до применения |
| `input_background_raw_input_lock` | `InputLatencyTweaks.cs:302` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `net_network_throttling_disable` | `NetworkOptimizationTweaks.cs:15` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `net_tcp_nagle_algorithm_disable` | `NetworkOptimizationTweaks.cs:32` (Custom) | Статическая проверка; runtime/benchmark не выполнены |
| `net_qos_reserved_bandwidth_zero` | `NetworkOptimizationTweaks.cs:158` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `net_dns_negative_cache_disable` | `NetworkOptimizationTweaks.cs:175` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `net_default_ttl_standard` | `NetworkOptimizationTweaks.cs:192` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `net_tcp_max_data_retransmissions` | `NetworkOptimizationTweaks.cs:209` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `net_enable_pmtu_discovery` | `NetworkOptimizationTweaks.cs:226` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `net_enable_pmt_black_hole_detect` | `NetworkOptimizationTweaks.cs:243` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `net_syn_attack_protect_tune` | `NetworkOptimizationTweaks.cs:260` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `net_disable_netbios_overhead` | `NetworkOptimizationTweaks.cs:277` (CreateRegistryDword) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `net_wpad_autoproxy_disable` | `NetworkOptimizationTweaks.cs:294` (CreateRegistryDword) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `net_rss_receive_side_scaling_enable` | `NetworkOptimizationTweaks.cs:311` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `net_rsc_receive_segment_coalescing_disable` | `NetworkOptimizationTweaks.cs:328` (CreateRegistryDword) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `net_tcp_window_auto_tuning_normal` | `NetworkOptimizationTweaks.cs:345` (CreateRegistryDword) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `net_ecn_explicit_congestion_notification` | `NetworkOptimizationTweaks.cs:362` (CreateRegistryDword) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `net_tcp_timestamps_disable` | `NetworkOptimizationTweaks.cs:379` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `net_teredo_tunneling_disable` | `NetworkOptimizationTweaks.cs:396` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `net_arp_cache_expiration_tuning` | `NetworkOptimizationTweaks.cs:413` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `net_disable_hardware_rsc` | `NetworkOptimizationTweaks.cs:430` (CreateCommandAction) | Статическая проверка; runtime/benchmark не выполнены |
| `net_disable_interrupt_moderation` | `NetworkOptimizationTweaks.cs:455` (CreateCommandAction) | Статическая проверка; runtime/benchmark не выполнены |
| `power_fast_startup_disable` | `PowerManagementTweaks.cs:10` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `power_pcie_aspm_unhide` | `PowerManagementTweaks.cs:27` (CreateRegistryDword) | Только раскрывает настройку питания; не меняет AC/DC |
| `power_usb_selective_suspend_unhide` | `PowerManagementTweaks.cs:45` (CreateRegistryDword) | Только раскрывает настройку питания; не меняет AC/DC |
| `power_processor_min_state_unhide` | `PowerManagementTweaks.cs:62` (CreateRegistryDword) | Только раскрывает настройку питания; не меняет AC/DC |
| `power_modern_standby_disable` | `PowerManagementTweaks.cs:79` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `power_energy_saver_gaming_override` | `PowerManagementTweaks.cs:97` (CreateRegistryDword) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `power_processor_idle_promote_unhide` | `PowerManagementTweaks.cs:114` (CreateRegistryDword) | Только раскрывает настройку питания; не меняет AC/DC |
| `power_system_cooling_policy_unhide` | `PowerManagementTweaks.cs:131` (CreateRegistryDword) | Только раскрывает настройку питания; не меняет AC/DC |
| `power_sleep_unattended_timeout_unhide` | `PowerManagementTweaks.cs:148` (CreateRegistryDword) | Только раскрывает настройку питания; не меняет AC/DC |
| `power_ahci_hipm_dipm_unhide` | `PowerManagementTweaks.cs:165` (CreateRegistryDword) | Только раскрывает настройку питания; не меняет AC/DC |
| `power_gpu_dynamic_pstate_unhide` | `PowerManagementTweaks.cs:182` (CreateRegistryDword) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `power_processor_performance_time_check_unhide` | `PowerManagementTweaks.cs:199` (CreateRegistryDword) | Только раскрывает настройку питания; не меняет AC/DC |
| `power_processor_idle_demote_unhide` | `PowerManagementTweaks.cs:216` (CreateRegistryDword) | Только раскрывает настройку питания; не меняет AC/DC |
| `power_away_mode_disable` | `PowerManagementTweaks.cs:233` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `power_hub_power_saving_disable` | `PowerManagementTweaks.cs:250` (CreateRegistryDword) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `power_allow_throttling_disable` | `PowerManagementTweaks.cs:267` (CreateRegistryDword) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `power_latency_sensitivity_hint_unhide` | `PowerManagementTweaks.cs:284` (CreateRegistryDword) | Только раскрывает настройку питания; не меняет AC/DC |
| `ram_disable_paging_executive` | `RamTweaks.cs:10` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `ram_clear_pagefile_shutdown_disable` | `RamTweaks.cs:28` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `ram_system_responsiveness_zero` | `RamTweaks.cs:45` (CreateRegistryDword) | Не выполняет заявленную цель: значение0 интерпретируется MMCSS как20 |
| `ram_large_system_cache_gaming` | `RamTweaks.cs:62` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `ram_svchost_split_threshold` | `RamTweaks.cs:79` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `ram_pool_usage_maximum` | `RamTweaks.cs:97` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `ram_session_view_size` | `RamTweaks.cs:114` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `ram_paging_file_encryption_disable` | `RamTweaks.cs:132` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `ram_prefetch_ssd_gaming_tune` | `RamTweaks.cs:149` (CreateMultiRegistry) | Статическая проверка; runtime/benchmark не выполнены |
| `ram_system_cache_dirty_page_limit` | `RamTweaks.cs:167` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `ram_paged_pool_quota_size` | `RamTweaks.cs:185` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `ram_worker_thread_stack_commit` | `RamTweaks.cs:203` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `sec_advertising_id_disable` | `SecurityPrivacyTweaks.cs:10` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `sec_activity_history_disable` | `SecurityPrivacyTweaks.cs:27` (CreateMultiRegistry) | Статическая проверка; runtime/benchmark не выполнены |
| `sec_inking_typing_personalization_disable` | `SecurityPrivacyTweaks.cs:45` (CreateMultiRegistry) | Статическая проверка; runtime/benchmark не выполнены |
| `sec_location_tracking_disable` | `SecurityPrivacyTweaks.cs:63` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `sec_telemetry_data_collection_min` | `SecurityPrivacyTweaks.cs:80` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `sec_wifi_sense_hotspot_reporting_disable` | `SecurityPrivacyTweaks.cs:97` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `sec_cloud_settings_sync_disable` | `SecurityPrivacyTweaks.cs:114` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `sec_biometrics_facial_recognition_disable` | `SecurityPrivacyTweaks.cs:131` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `sec_handwriting_error_reporting_disable` | `SecurityPrivacyTweaks.cs:149` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `sec_windows_customer_experience_optout` | `SecurityPrivacyTweaks.cs:166` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `sec_app_diagnostic_info_deny` | `SecurityPrivacyTweaks.cs:183` (CreateRegistryDword) | Ошибка типа данных: DWORD вместо строкового/бинарного значения; исправить до применения |
| `sec_remote_assistance_disable` | `SecurityPrivacyTweaks.cs:200` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `sec_defender_sample_submission_disable` | `SecurityPrivacyTweaks.cs:217` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `sec_defender_cloud_protection_reporting` | `SecurityPrivacyTweaks.cs:234` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `sec_microphone_background_access_apps` | `SecurityPrivacyTweaks.cs:251` (CreateRegistryString) | Статическая проверка; runtime/benchmark не выполнены |
| `sec_camera_background_access_deny` | `SecurityPrivacyTweaks.cs:268` (CreateRegistryString) | Статическая проверка; runtime/benchmark не выполнены |
| `sec_smartscreen_for_apps_disable` | `SecurityPrivacyTweaks.cs:285` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `sec_voice_activation_above_lock_disable` | `SecurityPrivacyTweaks.cs:302` (CreateMultiRegistry) | Статическая проверка; runtime/benchmark не выполнены |
| `storage_ntfs_83_name_disable` | `StorageOptimizationTweaks.cs:10` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `storage_ntfs_last_access_update_disable` | `StorageOptimizationTweaks.cs:28` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `storage_trim_enable_notification` | `StorageOptimizationTweaks.cs:45` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `storage_ntfs_memory_usage_increase` | `StorageOptimizationTweaks.cs:63` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `storage_disk_sleep_timeout_unhide` | `StorageOptimizationTweaks.cs:81` (CreateRegistryDword) | Только раскрывает настройку питания; не меняет AC/DC |
| `storage_crash_dump_minidump_only` | `StorageOptimizationTweaks.cs:98` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `storage_disable_low_disk_space_warning` | `StorageOptimizationTweaks.cs:115` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `storage_disable_link_tracking_ntfs` | `StorageOptimizationTweaks.cs:132` (CreateRegistryDword) | Несоответствие: NtfsDisableCompression управляет сжатием NTFS, не tracking |
| `storage_nvme_power_state_transition` | `StorageOptimizationTweaks.cs:149` (CreateRegistryDword) | Только раскрывает настройку питания; не меняет AC/DC |
| `storage_long_path_support_enable` | `StorageOptimizationTweaks.cs:167` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `storage_storage_sense_user_temp_cleanup` | `StorageOptimizationTweaks.cs:184` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `storage_disable_hibernation_file` | `StorageOptimizationTweaks.cs:201` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `storage_write_cache_buffer_flushing_tune` | `StorageOptimizationTweaks.cs:218` (CreateRegistryDword) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `storage_disable_chkdsk_scan_timeout` | `StorageOptimizationTweaks.cs:236` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `storage_ntfs_mft_zone_reservation` | `StorageOptimizationTweaks.cs:253` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `storage_smart_polling_interval_tune` | `StorageOptimizationTweaks.cs:271` (CreateRegistryDword) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `maint_auto_reboot_update_disable` | `SystemMaintenanceTweaks.cs:10` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `maint_driver_overwriting_disable` | `SystemMaintenanceTweaks.cs:27` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `maint_maintenance_wake_disable` | `SystemMaintenanceTweaks.cs:44` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `maint_update_peer_caching_disable` | `SystemMaintenanceTweaks.cs:61` (CreateRegistryDword) | Ошибка значения: DODownloadMode=1 включает LAN P2P; перекрывает win_delivery_optimization_p2p_disable |
| `maint_event_log_system_size` | `SystemMaintenanceTweaks.cs:78` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `maint_event_log_application_size` | `SystemMaintenanceTweaks.cs:95` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `maint_auto_update_download_notification` | `SystemMaintenanceTweaks.cs:112` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `maint_smart_card_removal_policy_disable` | `SystemMaintenanceTweaks.cs:129` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `maint_disk_cleanup_old_logs` | `SystemMaintenanceTweaks.cs:146` (CreateRegistryDword) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `maint_component_store_cleanup_threshold` | `SystemMaintenanceTweaks.cs:163` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `maint_corrupt_icon_cache_auto_refresh` | `SystemMaintenanceTweaks.cs:180` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `maint_windows_update_active_hours_expand` | `SystemMaintenanceTweaks.cs:197` (CreateMultiRegistry) | Статическая проверка; runtime/benchmark не выполнены |
| `maint_disk_defrag_ssd_disable` | `SystemMaintenanceTweaks.cs:215` (CreateRegistryDword) | Карантин: заявленный механизм/адрес не подтверждён; проверить поддерживаемый API и runtime-состояние |
| `maint_system_restore_frequency_limit` | `SystemMaintenanceTweaks.cs:233` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `maint_delivery_optimization_cache_clean` | `SystemMaintenanceTweaks.cs:250` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `maint_wer_queue_size_limit` | `SystemMaintenanceTweaks.cs:267` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `maint_bsod_auto_restart_enable` | `SystemMaintenanceTweaks.cs:284` (CreateRegistryDword) | Несоответствие ID: AutoReboot=0 выключает автоперезапуск; проверить отображаемый текст |
| `ui_show_file_extensions` | `WindowsExplorerUiTweaks.cs:10` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `ui_show_hidden_files` | `WindowsExplorerUiTweaks.cs:27` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `ui_separate_process_folders` | `WindowsExplorerUiTweaks.cs:44` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `ui_open_explorer_to_this_pc` | `WindowsExplorerUiTweaks.cs:61` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `ui_compact_view_explorer` | `WindowsExplorerUiTweaks.cs:78` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `ui_classic_context_menu` | `WindowsExplorerUiTweaks.cs:95` (CreateRegistryString) | Статическая проверка; runtime/benchmark не выполнены |
| `ui_disable_recent_files_quick_access` | `WindowsExplorerUiTweaks.cs:113` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `ui_disable_thumbnail_cache_clearing` | `WindowsExplorerUiTweaks.cs:130` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `ui_show_super_hidden_system_files` | `WindowsExplorerUiTweaks.cs:147` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `ui_show_drive_letters_first` | `WindowsExplorerUiTweaks.cs:164` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `ui_disable_sharing_wizard` | `WindowsExplorerUiTweaks.cs:181` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `ui_disable_folder_type_auto_discovery` | `WindowsExplorerUiTweaks.cs:198` (CreateRegistryString) | Статическая проверка; runtime/benchmark не выполнены |
| `ui_taskbar_search_icon_only` | `WindowsExplorerUiTweaks.cs:214` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `ui_disable_shortcut_suffix` | `WindowsExplorerUiTweaks.cs:231` (CreateRegistryDword) | Ошибка типа данных: DWORD вместо строкового/бинарного значения; исправить до применения |
| `ui_taskbar_combine_behavior` | `WindowsExplorerUiTweaks.cs:248` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `ui_disable_navigation_pane_expand_open` | `WindowsExplorerUiTweaks.cs:265` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `ui_restore_classic_ribbon_file_transfer` | `WindowsExplorerUiTweaks.cs:282` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `win_menu_show_delay` | `WindowsPerformanceTweaks.cs:10` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `win_transparency_disable` | `WindowsPerformanceTweaks.cs:27` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `win_window_animations_disable` | `WindowsPerformanceTweaks.cs:44` (CreateMultiRegistry) | Ошибка типа данных: DWORD вместо строкового/бинарного значения; исправить до применения |
| `win_background_apps_global_disable` | `WindowsPerformanceTweaks.cs:61` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `win_delivery_optimization_p2p_disable` | `WindowsPerformanceTweaks.cs:78` (CreateMultiRegistry) | Статическая проверка; runtime/benchmark не выполнены |
| `win_start_menu_web_search_disable` | `WindowsPerformanceTweaks.cs:95` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `win_feedback_frequency_never` | `WindowsPerformanceTweaks.cs:112` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `win_cortana_disable` | `WindowsPerformanceTweaks.cs:129` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `win_auto_play_disable` | `WindowsPerformanceTweaks.cs:146` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `win_aero_shake_disable` | `WindowsPerformanceTweaks.cs:163` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `win_snap_assist_disable` | `WindowsPerformanceTweaks.cs:180` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `win_visual_effects_best_performance` | `WindowsPerformanceTweaks.cs:197` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `win_lockscreen_slideshow_disable` | `WindowsPerformanceTweaks.cs:214` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `win_shell_shortcuts_hotkeys_disable` | `WindowsPerformanceTweaks.cs:231` (CreateRegistryString) | Статическая проверка; runtime/benchmark не выполнены |
| `win_search_indexer_backoff_disable` | `WindowsPerformanceTweaks.cs:247` (CreateRegistryDword) | Неверная цель: DisableBackoff=1 увеличивает нагрузку индексатора во время активности |
| `win_app_suggestions_start_menu_disable` | `WindowsPerformanceTweaks.cs:264` (CreateMultiRegistry) | Статическая проверка; runtime/benchmark не выполнены |
| `win_toast_notifications_quiet_hours` | `WindowsPerformanceTweaks.cs:281` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `win_print_screen_snipping_tool_disable` | `WindowsPerformanceTweaks.cs:298` (CreateRegistryDword) | Статическая проверка; runtime/benchmark не выполнены |
| `win_start_bing_search_disable` | `WindowsPerformanceTweaks.cs:315` (CreateMultiRegistry) | Дубликат/перекрытие записи; объединить операции и сохранить миграцию старого ID |
| `svc_diagnostic_policy_disable` | `WindowsServicesTweaks.cs:10` (CreateServiceTweak) | Статическая проверка; runtime/benchmark не выполнены |
| `svc_distributed_link_tracking_disable` | `WindowsServicesTweaks.cs:24` (CreateServiceTweak) | Статическая проверка; runtime/benchmark не выполнены |
| `svc_remote_registry_disable` | `WindowsServicesTweaks.cs:38` (CreateServiceTweak) | Статическая проверка; runtime/benchmark не выполнены |
| `svc_downloaded_maps_manager_disable` | `WindowsServicesTweaks.cs:52` (CreateServiceTweak) | Статическая проверка; runtime/benchmark не выполнены |
| `svc_fax_service_disable` | `WindowsServicesTweaks.cs:66` (CreateServiceTweak) | Статическая проверка; runtime/benchmark не выполнены |
| `svc_touch_keyboard_disable` | `WindowsServicesTweaks.cs:80` (CreateServiceTweak) | Статическая проверка; runtime/benchmark не выполнены |
| `svc_retail_demo_disable` | `WindowsServicesTweaks.cs:94` (CreateServiceTweak) | Статическая проверка; runtime/benchmark не выполнены |
| `svc_wallet_service_disable` | `WindowsServicesTweaks.cs:108` (CreateServiceTweak) | Статическая проверка; runtime/benchmark не выполнены |
| `svc_phone_service_disable` | `WindowsServicesTweaks.cs:122` (CreateServiceTweak) | Статическая проверка; runtime/benchmark не выполнены |
| `svc_pca_service_disable` | `WindowsServicesTweaks.cs:136` (CreateServiceTweak) | Статическая проверка; runtime/benchmark не выполнены |
| `svc_windows_search_manual` | `WindowsServicesTweaks.cs:150` (CreateServiceTweak) | Статическая проверка; runtime/benchmark не выполнены |
| `svc_sysmain_manual` | `WindowsServicesTweaks.cs:165` (CreateServiceStartup) | Статическая проверка; runtime/benchmark не выполнены |
| `svc_biometric_service_disable` | `WindowsServicesTweaks.cs:180` (CreateServiceStartup) | Статическая проверка; runtime/benchmark не выполнены |
| `svc_print_spooler_manual` | `WindowsServicesTweaks.cs:195` (CreateServiceStartup) | Статическая проверка; runtime/benchmark не выполнены |
| `svc_alljoyn_router_disable` | `WindowsServicesTweaks.cs:210` (CreateServiceStartup) | Статическая проверка; runtime/benchmark не выполнены |
| `svc_geolocation_service_disable` | `WindowsServicesTweaks.cs:225` (CreateServiceStartup) | Статическая проверка; runtime/benchmark не выполнены |
| `svc_offline_files_disable` | `WindowsServicesTweaks.cs:240` (CreateServiceStartup) | Статическая проверка; runtime/benchmark не выполнены |

## Политика расширения до500+

Считать уникальные эффективные настройки, а не синонимы и повторные пресеты. Для каждой новой записи требовать первичный URL, минимум build/редакцию/аппаратные условия, поддерживаемый механизм, независимый readback, точный snapshot/rollback, тест дубликатов и явное описание компромиссов. Целевой размер каталога не должен достигаться добавлением неиспользуемых DWORD или дроблением одного параметра на несколько ID.

## Внесённые узкие исправления этого аудита

В InputLatencyTweaks.cs исправлены типы `input_sticky_keys_prompt_disable`, `input_toggle_keys_prompt_disable`, `input_filter_keys_disable`, `input_mouse_hover_time_decrease`, `input_double_click_time_standard` на REG_SZ с теми же десятичными значениями и строковыми fallback; составной `input_pointer_trails_force_disable` использует mixed snapshot, MouseTrails записывается REG_SZ. MouseSonar оставлен DWORD согласно локальному чтению; чистый профиль Windows ещё требует проверки. Убраны абсолютные гарантии регистрации каждого нажатия, влияния Windows double-click на стрельбу и нулевой задержки курсора. Операции применения не запускались.

Остальные устранения/карантин выполняются интеграционным этапом; таблица выше сохраняет исходные обнаруженные дефекты, чтобы результат был проверяемым.

## Проверка интеграции исходного каталога

Проверено53 элемента TweakAuditPolicy.Withdrawals: каждый ID существует, повторных ID нет. После этой стадии исходный каталог содержит201 доступную для нового применения запись и53 сохранённые для истории/отката записи. Проверен guard нового применения в TweakEngine и блокировка совместимости. Наличие RollbackAsync сохранено.

RegistryWrites заполнен в четырёх registry-builders: DWORD, string, multi-DWORD, mixed. CPU MMCSS Games очищен от неиспользуемых GPU Priority/SFIO Priority и переведён в Optional/Medium; описание указывает необходимость регистрации потока приложением. WindowsPerformance исправляет MenuShowDelay и MinAnimate на строковые значения.

Найдены дополнительные интеграционные замечания, переданные основному агенту: GetString/GetDword допускают преобразование неверного registry-kind и могут ошибочно помечать прежний DWORD как Applied после миграции в REG_SZ; тест дубликатов по(Target,Value) пропускает конфликт разных значений одной цели; SmartScreen по-прежнему назван настройкой API timeout хотя выключает фильтр. Итоговое устранение этих замечаний проверяется отдельно.

## Итоговый повторный аудит — 16 ноября 2026

После добавления50 новых логических записей каталог содержит304 определения, в том числе54 отозванных для нового применения. Доступных определений250:200 исходных и50 новых. Отозванные ID остаются для исторического отката. SmartScreen-определение добавлено в Withdrawals, все54 ID разрешаются. Это число каталога, а не число совместимых/полезных настроек для конкретного ПК.

Повторно проверены устранения: registry-builders теперь запрашивают requireExactKind при чтении DWORD/строк, поэтому старый неправильный тип не маскируется как Applied. Тест конфликтов учитывает target независимо от значения. CPU MMCSS описание ограничено регистрируемыми мультимедийными потоками.

Новые50 записей проверены по исходному коду, первичным страницам Microsoft Learn и административным шаблонам C:\Windows\PolicyDefinitions: Search.admx, SettingSync.admx, CloudContent.admx, DeliveryOptimization.admx, WindowsExplorer.admx, AppPrivacy.admx, ErrorReporting.admx, Globalization.admx, OSPolicy.admx и Thumbnails.admx. Для всех50 найдены соответствующие scope/key/value и числовые типы/значения. Консервативная граница19044 и Enterprise/Education-ограничения CloudContent соответствуют выбранной области; три новые Windows11-функции ограничены22000+, Search Highlights22621+. Новые записи не дублируют доступные исходные registry-targets по пройденным интеграционным тестам.

Уточнение DO: DOMaxBackgroundDownloadBandwidth поддерживается начиная19041; это не устаревший DOMaxDownloadBandwidth. DOMinRAMAllowedToPeer измеряется вГБ; DOMonthlyUploadDataCap ограничивает отдачу интернет-пирам, не всю LAN-отдачу. [DeliveryOptimization CSP](https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-deliveryoptimization).

Оставшееся замечание передано интегратору: шесть SettingSync-политик с основным значением2 должны учитывать парный *UserOverride. В ADMX разрешающий пользователю override соответствует0, запрещающий1. Существующее0 может сохраняться при записи только основного DWORD. Для строгого запрета требуется snapshot/apply/readback обоих значений либо описание режима “выключено по умолчанию с возможностью включения”. [SettingSync CSP](https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-settingsync).

Локальные извлечения первичных источников и schema-selfcheck находятся в docs/audit-sources/new-tweaks/. Схема и источники не доказывают наличие каждой компоненты или прирост FPS. Применение политик/изменение системных настроек в ходе аудита не выполнялись.


### Итоговое устранение замечаний — 16 ноября 2026

Парные SettingSync UserOverride=1 включены в те же шесть составных операций: readback, snapshot и rollback охватывают обе записи. CreateMultiRegistry сохраняет customCompatibility. Подпись DOMonthlyUploadDataCap исправлена на интернет-пиры. Итог:304 определения,54 отозваны для нового применения,250 не отозваны (фактическая совместимость зависит от ПК/редакции/build).50 новых настроек содержат56 уникальных registry-targets, шесть пар являются едиными политиками и не считаются дополнительными твиками.355/355 тестов пройдены; ошибки типа проверены на изолированном временном ключе теста, системные твики не применялись.
