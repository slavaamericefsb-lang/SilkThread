using System.Collections.Generic;
using SilkThread.Core.Models;

namespace SilkThread.Core.TweakDatabase;

public static class SystemMaintenanceTweaks
{
    public static IEnumerable<TweakDefinition> GetTweaks()
    {
        yield return TweakBuilder.CreateRegistryDword(
            id: "maint_auto_reboot_update_disable",
            title: "Disable Automatic Reboot for Windows Updates While Logged On",
            description: "Prevents Windows Update from forcibly restarting your PC when a user is actively logged in.",
            howItWorks: "Sets 'NoAutoRebootWithLoggedOnUsers' to 1 in HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU.",
            problemSolved: "Windows abruptly restarts the PC during competitive matches or overnight gaming sessions to apply updates.",
            expectedResult: "You control exactly when the PC restarts, eliminating surprise update reboots.",
            category: TweakCategory.SystemMaintenance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU",
            valueName: "NoAutoRebootWithLoggedOnUsers",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "maint_driver_overwriting_disable",
            title: "Prevent Windows Update from Overwriting Graphics Drivers",
            description: "Blocks Windows Update from silently replacing clean NVIDIA or AMD display drivers with generic OEM versions.",
            howItWorks: "Sets 'ExcludeWUDriversInQualityUpdate' to 1 in HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate.",
            problemSolved: "Windows Update frequently downgrades graphics drivers without warning, breaking G-Sync, DLSS, and custom profiles.",
            expectedResult: "Graphics driver versions remain completely stable and under user control.",
            category: TweakCategory.SystemMaintenance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate",
            valueName: "ExcludeWUDriversInQualityUpdate",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "maint_maintenance_wake_disable",
            title: "Disable Automatic Maintenance PC Wake Timers",
            description: "Prevents Windows Automatic Maintenance from waking up sleeping PCs in the middle of the night.",
            howItWorks: "Sets 'WakeUp' to 0 in HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Schedule\\Maintenance.",
            problemSolved: "PCs suddenly turn on fans at full blast at 2:00 AM, wasting power and heating up the room.",
            expectedResult: "PC remains peacefully in sleep mode until user manually awakens it.",
            category: TweakCategory.SystemMaintenance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\Maintenance",
            valueName: "WakeUp",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "maint_update_peer_caching_disable",
            title: "Disable LAN Peer Update Caching Overhead",
            description: "Stops Delivery Optimization from maintaining an active HTTP server on port 7680 for local peer updates.",
            howItWorks: "Sets 'DODownloadMode' to 1 (Bypass P2P / HTTP only) in HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DeliveryOptimization.",
            problemSolved: "Closes background local network listener ports that consume socket handles and firewall state tables.",
            expectedResult: "Clean network stack with zero LAN update broadcasting.",
            category: TweakCategory.SystemMaintenance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization",
            valueName: "DODownloadMode",
            appliedValue: 1,
            fallbackDefaultValue: 3
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "maint_event_log_system_size",
            title: "Expand System Event Log Retention Buffer to 64MB",
            description: "Increases the System Event Log circular buffer capacity to 64 megabytes.",
            howItWorks: "Sets 'MaxSize' to 67108864 in HKLM\\SYSTEM\\CurrentControlSet\\Services\\EventLog\\System.",
            problemSolved: "Prevents critical hardware and driver event logs from being overwritten during diagnostic troubleshooting.",
            expectedResult: "Comprehensive historical logs for analyzing rare game crashes and driver events.",
            category: TweakCategory.SystemMaintenance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Services\EventLog\System",
            valueName: "MaxSize",
            appliedValue: 67108864,
            fallbackDefaultValue: 20971520
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "maint_event_log_application_size",
            title: "Expand Application Event Log Retention Buffer to 64MB",
            description: "Increases the Application Event Log circular buffer capacity to 64 megabytes.",
            howItWorks: "Sets 'MaxSize' to 67108864 in HKLM\\SYSTEM\\CurrentControlSet\\Services\\EventLog\\Application.",
            problemSolved: "Ensures DirectX, Unreal Engine, and Unity crash callstacks are preserved without log truncation.",
            expectedResult: "Reliable crash logging for game developers and modders.",
            category: TweakCategory.SystemMaintenance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Services\EventLog\Application",
            valueName: "MaxSize",
            appliedValue: 67108864,
            fallbackDefaultValue: 20971520
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "maint_auto_update_download_notification",
            title: "Notify Before Downloading Windows Updates",
            description: "Configures Windows Update to notify you before downloading large feature or cumulative updates.",
            howItWorks: "Sets 'AUOptions' to 2 (Notify for download and auto install) in HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU.",
            problemSolved: "Large 5GB update downloads unexpectedly saturate internet bandwidth mid-game, causing 200ms+ ping spikes.",
            expectedResult: "Zero surprise background network bandwidth saturation.",
            category: TweakCategory.SystemMaintenance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU",
            valueName: "AUOptions",
            appliedValue: 2,
            fallbackDefaultValue: 4
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "maint_smart_card_removal_policy_disable",
            title: "Disable Smart Card Subsystem Checks on Desktops",
            description: "Stops the OS from querying for cryptographic smart card readers.",
            howItWorks: "Sets Scard service configuration in HKLM\\SYSTEM\\CurrentControlSet\\Services\\SCardSvr.",
            problemSolved: "Eliminates smart card reader device queries in the background on consumer PCs.",
            expectedResult: "Removes unneeded hardware querying.",
            category: TweakCategory.SystemMaintenance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Services\SCardSvr",
            valueName: "Start",
            appliedValue: 4,
            fallbackDefaultValue: 3
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "maint_disk_cleanup_old_logs",
            title: "Enable Automatic Purge of Old Windows Error Dumps",
            description: "Configures system cleanup to clear temporary crash dump artifacts older than 7 days.",
            howItWorks: "Sets 'DaysToKeep' to 7 in HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VolumeCaches\\System error memory dump files.",
            problemSolved: "Old crash dumps silently consume tens of gigabytes of valuable SSD space over time.",
            expectedResult: "Maintains optimal SSD free space automatically.",
            category: TweakCategory.SystemMaintenance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\System error memory dump files",
            valueName: "DaysToKeep",
            appliedValue: 7,
            fallbackDefaultValue: 30
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "maint_component_store_cleanup_threshold",
            title: "Enable WinSxS Component Store Scavenging",
            description: "Enables aggressive WinSxS superseded component scavenging during maintenance.",
            howItWorks: "Sets 'DisableScavenging' to 0 in HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\SideBySide\\Configuration.",
            problemSolved: "The Windows WinSxS folder balloons to 15-25 GB with superseded DLL versions.",
            expectedResult: "Recovers gigabytes of drive storage without breaking system file integrity.",
            category: TweakCategory.SystemMaintenance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Microsoft\Windows\CurrentVersion\SideBySide\Configuration",
            valueName: "DisableScavenging",
            appliedValue: 0,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "maint_corrupt_icon_cache_auto_refresh",
            title: "Expand Shell Icon Cache Size to 4096KB",
            description: "Increases the Windows Shell icon cache to 4MB to prevent blank icons and rebuilding lag.",
            howItWorks: "Sets 'Max Cached Icons' to 4096 in HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer.",
            problemSolved: "Small default icon cache frequently overflows, causing desktop icons to flash blank and reload.",
            expectedResult: "Smooth, permanent icon rendering across desktop and start menu.",
            category: TweakCategory.SystemMaintenance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer",
            valueName: "Max Cached Icons",
            appliedValue: 4096,
            fallbackDefaultValue: 500
        );

        yield return TweakBuilder.CreateMultiRegistry(
            id: "maint_windows_update_active_hours_expand",
            title: "Expand Windows Update Active Hours Window to 18 Hours",
            description: "Configures Windows Update Active Hours to maximum span (6:00 AM to 12:00 AM) to prevent daytime and evening reboots.",
            howItWorks: "Sets SetActiveHours = 1 with ActiveHoursStart = 6 and ActiveHoursEnd = 0 in HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate.",
            problemSolved: "Standard default active hours interrupt late night gaming sessions with scheduled update prompts.",
            expectedResult: "Protects your entire gaming and work window from update interruptions.",
            category: TweakCategory.SystemMaintenance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            dwords: new[]
            {
                ("HKLM", @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "SetActiveHours", 1),
                ("HKLM", @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "ActiveHoursStart", 6),
                ("HKLM", @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "ActiveHoursEnd", 0)
            }
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "maint_disk_defrag_ssd_disable",
            title: "Prevent Automatic Fragmentation Scans on Solid State Drives",
            description: "Disables scheduled disk defragmentation analysis tasks that perform redundant wear-inducing passes on SSDs.",
            howItWorks: "Sets 'DefragSched' to 0 in HKLM\\SOFTWARE\\Microsoft\\Dfrg\\BootOptimizeFunction.",
            problemSolved: "Windows defrag scheduler sometimes misidentifies NVMe drives and performs unnecessary defragmentation.",
            expectedResult: "Protects SSD write endurance while allowing standard manual TRIM operations.",
            category: TweakCategory.SystemMaintenance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Microsoft\Dfrg\BootOptimizeFunction",
            valueName: "DefragSched",
            appliedValue: 0,
            fallbackDefaultValue: 1,
            requiresSsdOrNvme: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "maint_system_restore_frequency_limit",
            title: "Set System Restore Point Creation Frequency Interval",
            description: "Ensures Windows does not create multiple duplicate restore points within a 24-hour period.",
            howItWorks: "Sets 'SystemRestorePointCreationFrequency' to 1440 in HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\SystemRestore.",
            problemSolved: "Installing multiple game redistributables (DirectX, VC++ Runtimes) triggers separate 2-4GB restore points each time.",
            expectedResult: "Saves SSD disk space and prevents installation lag when installing multiple games.",
            category: TweakCategory.SystemMaintenance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore",
            valueName: "SystemRestorePointCreationFrequency",
            appliedValue: 1440,
            fallbackDefaultValue: 1440
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "maint_delivery_optimization_cache_clean",
            title: "Limit Delivery Optimization Cache Retention to 3 Days",
            description: "Caps the maximum retention time for downloaded Windows Update and Store cache files to 3 days.",
            howItWorks: "Sets 'DOMaxCacheAge' to 259200 in HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DeliveryOptimization.",
            problemSolved: "Delivery Optimization hoards 20GB+ of old update CAB files in hidden folders indefinitely.",
            expectedResult: "Automatically purges stale update cache and frees SSD capacity.",
            category: TweakCategory.SystemMaintenance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization",
            valueName: "DOMaxCacheAge",
            appliedValue: 259200,
            fallbackDefaultValue: 2592000
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "maint_wer_queue_size_limit",
            title: "Limit Windows Error Reporting Report Queue Size",
            description: "Limits the maximum number of queued error reports stored on disk to 5.",
            howItWorks: "Sets 'MaxQueueCount' to 5 in HKLM\\SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting.",
            problemSolved: "Repeated game crashes during mod development generate hundreds of MBs of report files in the WER queue.",
            expectedResult: "Limits diagnostic disk clutter while retaining recent crash logs.",
            category: TweakCategory.SystemMaintenance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Microsoft\Windows\Windows Error Reporting",
            valueName: "MaxQueueCount",
            appliedValue: 5,
            fallbackDefaultValue: 20
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "maint_bsod_auto_restart_enable",
            title: "Enable Immediate Crash Screen Display on BugCheck (BSOD)",
            description: "Disables automatic instantaneous restart on system crash so that stop error codes and faulting drivers remain readable.",
            howItWorks: "Sets 'AutoReboot' to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\CrashControl.",
            problemSolved: "Instant reboot hides critical BSOD bugcheck codes and faulting driver names needed to diagnose unstable overclocks.",
            expectedResult: "Allows reading exact BSOD error codes and faulting .sys drivers without unexpected reboots.",
            category: TweakCategory.SystemMaintenance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\CrashControl",
            valueName: "AutoReboot",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );
    }
}
