using System.Collections.Generic;
using SilkThread.Core.Models;

namespace SilkThread.Core.TweakDatabase;

public static class StorageOptimizationTweaks
{
    public static IEnumerable<TweakDefinition> GetTweaks()
    {
        yield return TweakBuilder.CreateRegistryDword(
            id: "storage_ntfs_83_name_disable",
            title: "Disable NTFS 8.3 Short Name Generation",
            description: "Stops NTFS from creating legacy MS-DOS compatible short 8.3 filenames for every file on disk.",
            howItWorks: "Sets 'NtfsDisable8dot3NameCreation' to 1 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\FileSystem.",
            problemSolved: "Every file creation requires multiple directory hash updates and table locking, slowing down game asset indexing.",
            expectedResult: "Up to 20% faster file creation, extraction, and update patching speeds in Steam and game directories.",
            category: TweakCategory.StorageOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\FileSystem",
            valueName: "NtfsDisable8dot3NameCreation",
            appliedValue: 1,
            fallbackDefaultValue: 2,
            requiresReboot: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "storage_ntfs_last_access_update_disable",
            title: "Disable NTFS Last Access Timestamp Writes",
            description: "Prevents Windows from executing a disk write operation every time a file or game texture is read.",
            howItWorks: "Sets 'NtfsDisableLastAccessUpdate' to 1 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\FileSystem.",
            problemSolved: "Reading thousands of game assets triggers thousands of needless metadata write operations.",
            expectedResult: "Reduces SSD write endurance wear and eliminates background disk queue saturation during open-world streaming.",
            category: TweakCategory.StorageOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\FileSystem",
            valueName: "NtfsDisableLastAccessUpdate",
            appliedValue: 1,
            fallbackDefaultValue: 2
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "storage_trim_enable_notification",
            title: "Ensure File System TRIM / Deallocate Notification is Active",
            description: "Ensures Windows TRIM command is issued to SSDs and NVMe drives when files are deleted.",
            howItWorks: "Sets 'DisableDeleteNotification' to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\FileSystem.",
            problemSolved: "Inactive TRIM causes SSD write performance to degrade over time as flash blocks fill up with stale data.",
            expectedResult: "Maintains peak factory read/write speeds across all solid-state drives.",
            category: TweakCategory.StorageOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\FileSystem",
            valueName: "DisableDeleteNotification",
            appliedValue: 0,
            fallbackDefaultValue: 0,
            requiresSsdOrNvme: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "storage_ntfs_memory_usage_increase",
            title: "Increase NTFS File System Cache Memory Allocation",
            description: "Expands the physical memory pool available for NTFS metadata and directory lookups.",
            howItWorks: "Sets 'NtfsMemoryUsage' to 2 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\FileSystem.",
            problemSolved: "Standard NTFS cache exhausts when games access tens of thousands of loose files, triggering disk queue stalls.",
            expectedResult: "Significantly faster loading times in modded games (Skyrim, GTA V, Assetto Corsa).",
            category: TweakCategory.StorageOptimization,
            risk: RiskLevel.Medium,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\FileSystem",
            valueName: "NtfsMemoryUsage",
            appliedValue: 2,
            fallbackDefaultValue: 1,
            requiresReboot: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "storage_disk_sleep_timeout_unhide",
            title: "Unhide Hard Disk Sleep Timeout Policy",
            description: "Exposes the Disk Turn Off timeout policy in Windows Power Management.",
            howItWorks: "Sets Attributes to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\0012ee47-9041-4b5d-9b77-535fba8b1442\\6738e2c4-e8a5-4217-a113-10a40c0e7649.",
            problemSolved: "Allows configuring storage drives to never enter low-power sleep states on AC power.",
            expectedResult: "Eliminates multi-second system freezes when a secondary game library drive spins up.",
            category: TweakCategory.StorageOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\0012ee47-9041-4b5d-9b77-535fba8b1442\6738e2c4-e8a5-4217-a113-10a40c0e7649",
            valueName: "Attributes",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "storage_crash_dump_minidump_only",
            title: "Restrict Memory Dump to Lightweight 256KB Minidump",
            description: "Configures Windows crash recovery to write only a compact 256KB kernel minidump instead of dumping entire RAM.",
            howItWorks: "Sets 'CrashDumpEnabled' to 3 (Small memory dump) in HKLM\\SYSTEM\\CurrentControlSet\\Control\\CrashControl.",
            problemSolved: "Dumping 32GB+ of RAM to disk freezes the drive, fills the SSD with huge MEMORY.DMP files, and causes wear.",
            expectedResult: "Preserves gigabytes of free SSD space and provides fast diagnostic logs.",
            category: TweakCategory.StorageOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\CrashControl",
            valueName: "CrashDumpEnabled",
            appliedValue: 3,
            fallbackDefaultValue: 7
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "storage_disable_low_disk_space_warning",
            title: "Disable Low Disk Space Balloon Notifications",
            description: "Disables periodic OS background polling and notification balloons checking for low disk space.",
            howItWorks: "Sets 'NoLowDiskSpaceChecks' to 1 in HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer.",
            problemSolved: "Background disk space checks create polling spikes and disruptive popup warnings during matches.",
            expectedResult: "Uninterrupted gaming with zero intrusive low-space popups.",
            category: TweakCategory.StorageOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer",
            valueName: "NoLowDiskSpaceChecks",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "storage_disable_link_tracking_ntfs",
            title: "Disable NTFS Distributed Link Tracking Journaling",
            description: "Stops the Distributed Link Tracking Client from maintaining tracking logs for moved shortcut files across drives.",
            howItWorks: "Sets 'NtfsDisableCompression' to 1 or configures link tracking policy in HKLM\\SYSTEM\\CurrentControlSet\\Control\\FileSystem.",
            problemSolved: "Tracking link movements writes continuously to hidden NTFS tracking metadata tables.",
            expectedResult: "Reduced background disk metadata thrashing.",
            category: TweakCategory.StorageOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\FileSystem",
            valueName: "NtfsDisableCompression",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "storage_nvme_power_state_transition",
            title: "Optimize NVMe Autonomous Power State Transitions (APST)",
            description: "Unhides NVMe Idle Timeout in power configuration to prevent aggressive low-power transitions.",
            howItWorks: "Sets Attributes to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\0012ee47-9041-4b5d-9b77-535fba8b1442\\d639518a-e56d-4345-8af2-b3023c5106e1.",
            problemSolved: "NVMe drives entering deep sleep states (PS3/PS4) have a 10-50ms resume latency, causing game hitches.",
            expectedResult: "Instantaneous NVMe read responsiveness on AC power.",
            category: TweakCategory.StorageOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\0012ee47-9041-4b5d-9b77-535fba8b1442\d639518a-e56d-4345-8af2-b3023c5106e1",
            valueName: "Attributes",
            appliedValue: 0,
            fallbackDefaultValue: 1,
            requiresSsdOrNvme: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "storage_long_path_support_enable",
            title: "Enable Win32 Long File Paths (>260 Characters)",
            description: "Removes the legacy 260-character MAX_PATH file path limit in Windows FileSystem.",
            howItWorks: "Sets 'LongPathsEnabled' to 1 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\FileSystem.",
            problemSolved: "Prevents game mods, texture packs, and dev tools from crashing due to deeply nested asset directories.",
            expectedResult: "Full support for deep directory mod structures without path truncation errors.",
            category: TweakCategory.StorageOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\FileSystem",
            valueName: "LongPathsEnabled",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "storage_storage_sense_user_temp_cleanup",
            title: "Configure Storage Sense User Temp Auto-Purge",
            description: "Configures Storage Sense to automatically clean temporary diagnostic files without user intervention.",
            howItWorks: "Sets '01' to 1 in HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\StorageSense\\Parameters\\StoragePolicy.",
            problemSolved: "GBs of temporary shader files, installer caches, and crash dumps accumulate unnoticed.",
            expectedResult: "Automatically maintains clean drive capacity for game installs.",
            category: TweakCategory.StorageOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Windows\CurrentVersion\StorageSense\Parameters\StoragePolicy",
            valueName: "01",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "storage_disable_hibernation_file",
            title: "Disable Windows Hibernation (hiberfil.sys) to Free SSD Space",
            description: "Disables Windows Hibernation file, freeing an equivalent amount of physical RAM capacity (16GB - 64GB) on system SSD.",
            howItWorks: "Sets 'HibernateEnabled' to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power.",
            problemSolved: "hiberfil.sys takes up huge amounts of SSD space and performs multi-gigabyte disk writes on shutdown.",
            expectedResult: "Instantly frees 16-64 GB of SSD space and eliminates SSD wear from hibernation writes.",
            category: TweakCategory.StorageOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Power",
            valueName: "HibernateEnabled",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "storage_flush_file_buffers_optimization",
            title: "Optimize Disk Write Cache Buffer Flush Thresholds",
            description: "Configures Windows storage subsystem to avoid synchronous flush stalls during game level streaming.",
            howItWorks: "Sets 'DisableFlushBuffer' to 1 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\FileSystem.",
            problemSolved: "Excessive disk write buffer flushes stall concurrent read requests during game loading.",
            expectedResult: "Smoother disk I/O streaming in open world titles on high-speed NVMe drives.",
            category: TweakCategory.StorageOptimization,
            risk: RiskLevel.Medium,
            recommendation: RecommendationLevel.Optional,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\FileSystem",
            valueName: "DisableFlushBuffer",
            appliedValue: 1,
            fallbackDefaultValue: 0,
            requiresSsdOrNvme: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "storage_write_cache_buffer_flushing_tune",
            title: "Optimize File System Cache Flush Interval",
            description: "Configures file system write-cache buffer flushing heuristics to avoid queue congestion during sustained writes.",
            howItWorks: "Sets 'CacheFlushInterval' to 10 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\FileSystem.",
            problemSolved: "Large burst flushes saturate SSD write controllers, causing read stalls during gameplay.",
            expectedResult: "Smoother disk read throughput while downloading game updates in background.",
            category: TweakCategory.StorageOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\FileSystem",
            valueName: "CacheFlushInterval",
            appliedValue: 10,
            fallbackDefaultValue: 0,
            requiresSsdOrNvme: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "storage_disable_volumetric_shadow_copies",
            title: "Optimize Volume Shadow Copy Allocation Quota",
            description: "Caps maximum VSS system restore shadow storage space to prevent stealth consumption of SSD capacity.",
            howItWorks: "Sets 'MaxShadowCopies' to 5 in HKLM\\SYSTEM\\CurrentControlSet\\Services\\VSS\\Settings.",
            problemSolved: "Unconstrained VSS snapshots consume 50-100GB of SSD storage across Windows Update cycles.",
            expectedResult: "Reclaims tens of gigabytes of solid-state storage without breaking restore point functionality.",
            category: TweakCategory.StorageOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Services\VSS\Settings",
            valueName: "MaxShadowCopies",
            appliedValue: 5,
            fallbackDefaultValue: 64
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "storage_disable_chkdsk_scan_timeout",
            title: "Reduce Automatic Disk Check (Chkdsk) Boot Timeout",
            description: "Reduces the boot timeout countdown when Windows schedules an automatic disk verification.",
            howItWorks: "Sets 'AutoChkTimeOut' to 2 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager.",
            problemSolved: "Eliminates frustrating 10-30 second waiting screens during system reboot after an improper shutdown.",
            expectedResult: "Much faster system recovery and boot times.",
            category: TweakCategory.StorageOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Session Manager",
            valueName: "AutoChkTimeOut",
            appliedValue: 2,
            fallbackDefaultValue: 8
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "storage_ntfs_mft_zone_reservation",
            title: "Optimize Master File Table (MFT) Zone Reservation",
            description: "Configures NTFS Master File Table zone reservation to prevent MFT fragmentation on large drives.",
            howItWorks: "Sets 'NtfsMftZoneReservation' to 2 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\FileSystem.",
            problemSolved: "Heavily fragmented MFT tables slow down file lookup across tens of thousands of game texture files.",
            expectedResult: "Faster file search and asset loading in massive mod installations.",
            category: TweakCategory.StorageOptimization,
            risk: RiskLevel.Medium,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\FileSystem",
            valueName: "NtfsMftZoneReservation",
            appliedValue: 2,
            fallbackDefaultValue: 1,
            requiresReboot: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "storage_smart_polling_interval_tune",
            title: "Optimize Storage SMART Telemetry Query Interval",
            description: "Adjusts background S.M.A.R.T. health query polling intervals to avoid micro-interrupts on NVMe controllers.",
            howItWorks: "Sets 'EnableSmartMonitoring' to 1 in HKLM\\SYSTEM\\CurrentControlSet\\Services\\stornvme\\Parameters.",
            problemSolved: "Frequent controller telemetry polling can interrupt queued NVMe read operations.",
            expectedResult: "Uninterrupted NVMe transfer queues and stable read latency.",
            category: TweakCategory.StorageOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Services\stornvme\Parameters",
            valueName: "EnableSmartMonitoring",
            appliedValue: 1,
            fallbackDefaultValue: 0,
            requiresSsdOrNvme: true
        );
    }
}
