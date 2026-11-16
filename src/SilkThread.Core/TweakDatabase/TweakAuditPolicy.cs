using SilkThread.Core.Models;

namespace SilkThread.Core.TweakDatabase;

/// <summary>Audit withdrawals preserve IDs and rollback snapshots without allowing new writes.</summary>
public static class TweakAuditPolicy
{
    public static IReadOnlyDictionary<string, string> Withdrawals { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["bg_smartscreen_prompt_latency"] = "Аудит: твик отключает SmartScreen вопреки названию об ускорении API. Не используется в пакете производительности.",
        ["cpu_idle_resiliency_disable"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["cpu_dxgkrnl_latency_tolerance"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["cpu_affinity_prioritization_foreground"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["cpu_intel_ppm_epb_performance"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["cpu_amd_ppm_perf_boost"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["cpu_svm_hyperv_interrupt_steering"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["cpu_kernel_dpc_watchdog_profile"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["gaming_directx_gpu_energy_driver"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["gaming_game_config_store_flags"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["gaming_discord_overlay_hook_mitigation"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["gaming_steam_client_gpu_accel_priority"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["gaming_spatial_audio_overhead_disable"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["gpu_nvidia_low_latency_mode"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["gpu_nvidia_power_mizer_max"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["gpu_amd_anti_lag_profile"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["gpu_amd_shader_cache_limit"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["gpu_amd_ulps_disable"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["gpu_intel_arc_adaptive_sync"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["gpu_dxgi_swapchain_waitable_object"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["gpu_vram_allocation_priority"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["gpu_preemption_scheduler_enable"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["input_hid_driver_polling_priority"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["input_csgo_raw_input_buffer_coalescing"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["input_gamepad_xinput_polling_latency"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["power_energy_saver_gaming_override"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["power_gpu_dynamic_pstate_unhide"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["power_hub_power_saving_disable"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["power_allow_throttling_disable"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["storage_write_cache_buffer_flushing_tune"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["storage_smart_polling_interval_tune"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["maint_disk_cleanup_old_logs"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["maint_disk_defrag_ssd_disable"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["adv_kernel_pool_tagging_gaming"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["adv_kernel_dpc_timeout_polynomial"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["net_tcp_window_auto_tuning_normal"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["net_rsc_receive_segment_coalescing_disable"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["net_ecn_explicit_congestion_notification"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["net_disable_netbios_overhead"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["net_wpad_autoproxy_disable"] = "Аудит: поддерживаемый механизм этой настройки Windows 10/11 не подтверждён. Новое применение заблокировано; исторический откат сохранён.",
        ["ram_system_responsiveness_zero"] = "Аудит MMCSS: используются игнорируемые значения или неподтверждённый профиль. Источник: https://learn.microsoft.com/en-us/windows/win32/procthread/multimedia-class-scheduler-service",
        ["gpu_dwm_high_priority"] = "Аудит MMCSS: используются игнорируемые значения или неподтверждённый профиль. Источник: https://learn.microsoft.com/en-us/windows/win32/procthread/multimedia-class-scheduler-service",
        ["adv_dwm_thread_priority_realtime"] = "Аудит MMCSS: используются игнорируемые значения или неподтверждённый профиль. Источник: https://learn.microsoft.com/en-us/windows/win32/procthread/multimedia-class-scheduler-service",
        ["adv_mmcss_audio_realtime_priority"] = "Аудит MMCSS: используются игнорируемые значения или неподтверждённый профиль. Источник: https://learn.microsoft.com/en-us/windows/win32/procthread/multimedia-class-scheduler-service",
        ["gpu_tdr_delay_extend"] = "TDR — параметры отладки драйвера, не оптимизация FPS. https://learn.microsoft.com/en-us/windows-hardware/drivers/display/tdr-registry-keys",
        ["ui_disable_shortcut_suffix"] = "Аудит: Explorer\\link должен иметь тип REG_BINARY; прежнее определение записывало DWORD.",
        ["sec_app_diagnostic_info_deny"] = "Аудит: ConsentStore\\Value должен иметь тип REG_SZ; прежнее определение записывало DWORD.",
        ["gaming_game_bar_presence_writer_disable"] = "Аудит: дублирующая или противоречащая запись. Используйте gaming_game_dvr_disable. Старый ID сохранён для отката.",
        ["gaming_game_dvr_dxgi_compatibility"] = "Аудит: дублирующая или противоречащая запись. Используйте gaming_fullscreen_exclusive_fse. Старый ID сохранён для отката.",
        ["win_start_bing_search_disable"] = "Аудит: дублирующая или противоречащая запись. Используйте win_start_menu_web_search_disable. Старый ID сохранён для отката.",
        ["maint_update_peer_caching_disable"] = "Аудит: дублирующая или противоречащая запись. Используйте win_delivery_optimization_p2p_disable. Старый ID сохранён для отката.",
        ["bg_edge_prelaunch_disable"] = "Аудит: смешаны пути политик Legacy Edge и Chromium Edge. Применение не гарантирует заявленный эффект.",
        ["win_search_indexer_backoff_disable"] = "Аудит: отключение backoff ускоряет индексирование под нагрузкой, а не снижает фоновую нагрузку в играх.",
        ["storage_disable_link_tracking_ntfs"] = "Аудит: NtfsDisableCompression управляет сжатием NTFS, а не Distributed Link Tracking.",
    };
    public static void Annotate(IEnumerable<TweakDefinition> definitions)
    {
        foreach (var tweak in definitions)
            if (Withdrawals.TryGetValue(tweak.Id, out var reason)) tweak.AuditBlockReason = reason;
    }
}
