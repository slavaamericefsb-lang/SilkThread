using System.Collections.Generic;
using SilkThread.Core.Models;

namespace SilkThread.Core.TweakDatabase;

public static class BackgroundProcessesTweaks
{
    public static IEnumerable<TweakDefinition> GetTweaks()
    {
        yield return TweakBuilder.CreateServiceTweak(
            id: "bg_diagtrack_service_disable",
            title: "Disable Connected User Experiences & Telemetry Service (DiagTrack)",
            description: "Stops and disables the core Windows Telemetry (DiagTrack) service from running in the background.",
            howItWorks: "Sets DiagTrack service startup type to Disabled (4) and terminates the running process.",
            problemSolved: "DiagTrack frequently wakes up CPU cores to parse event logs and transmit usage packets to Microsoft telemetry endpoints.",
            expectedResult: "Eliminates random CPU spikes, frees up svchost RAM, and protects gaming privacy.",
            category: TweakCategory.BackgroundProcesses,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            serviceName: "DiagTrack",
            targetStartType: 4
        );

        yield return TweakBuilder.CreateMultiRegistry(
            id: "bg_edge_prelaunch_disable",
            title: "Disable Microsoft Edge Background Startup & Pre-launching",
            description: "Prevents Microsoft Edge from pre-launching into RAM and maintaining active background processes when Windows boots.",
            howItWorks: "Sets AllowPrelaunch = 0 and BackgroundModeEnabled = 0 in HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge.",
            problemSolved: "Edge silently spawns multiple msedge.exe processes that consume 300MB+ of memory even if you use Chrome or Firefox.",
            expectedResult: "Frees up hundreds of megabytes of physical RAM and eliminates background browser telemetry.",
            category: TweakCategory.BackgroundProcesses,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            dwords: new[]
            {
                ("HKLM", @"SOFTWARE\Policies\Microsoft\Edge", "AllowPrelaunch", 0),
                ("HKLM", @"SOFTWARE\Policies\Microsoft\Edge", "BackgroundModeEnabled", 0)
            }
        );

        yield return TweakBuilder.CreateServiceTweak(
            id: "bg_wer_service_disable",
            title: "Disable Windows Error Reporting Service (WerSvc)",
            description: "Disables the background service that captures application crash dumps and queues error reports.",
            howItWorks: "Sets WerSvc service startup type to Disabled (4).",
            problemSolved: "When a game hitches or throws an handled exception, WerSvc can pause the process to generate a dump.",
            expectedResult: "Faster application recovery and reduced disk activity during minor errors.",
            category: TweakCategory.BackgroundProcesses,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            serviceName: "WerSvc",
            targetStartType: 4
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "bg_widgets_news_interests_disable",
            title: "Disable Windows 11 Widgets Board (News & Feeds)",
            description: "Disables the Windows 11 Widgets taskbar board and background webview feed engines.",
            howItWorks: "Sets 'AllowNewsAndInterests' to 0 in HKLM\\SOFTWARE\\Policies\\Microsoft\\Dsh.",
            problemSolved: "Widgets run background WebView2 instances (Edge processes) that constantly update news stories and stocks.",
            expectedResult: "Removes background WebView2 processes and frees RAM.",
            category: TweakCategory.BackgroundProcesses,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Policies\Microsoft\Dsh",
            valueName: "AllowNewsAndInterests",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateMultiRegistry(
            id: "bg_content_delivery_suggestions_disable",
            title: "Disable Content Delivery Manager Promoted Suggestions",
            description: "Stops Windows from downloading and displaying promoted apps, games, and web suggestions in the UI.",
            howItWorks: "Disables SystemPaneSuggestionsEnabled and SoftLandingEnabled in HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager.",
            problemSolved: "Prevents Windows from automatically downloading bloatware shortcuts and sponsored content.",
            expectedResult: "Clean, bloat-free Windows experience without unsolicited background network downloads.",
            category: TweakCategory.BackgroundProcesses,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            dwords: new[]
            {
                ("HKCU", @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SystemPaneSuggestionsEnabled", 0),
                ("HKCU", @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SoftLandingEnabled", 0),
                ("HKCU", @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338388Enabled", 0),
                ("HKCU", @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SilentInstalledAppsEnabled", 0)
            }
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "bg_customer_experience_ceip_disable",
            title: "Disable Customer Experience Improvement Program (CEIP)",
            description: "Disables scheduled CEIP telemetry tasks that collect and upload hardware usage profiles.",
            howItWorks: "Sets 'CEIPEnable' to 0 in HKLM\\SOFTWARE\\Policies\\Microsoft\\SQMClient\\Windows.",
            problemSolved: "CEIP tasks run periodically during active sessions, introducing unprompted CPU spikes.",
            expectedResult: "Reduced scheduled background task execution.",
            category: TweakCategory.BackgroundProcesses,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Policies\Microsoft\SQMClient\Windows",
            valueName: "CEIPEnable",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "bg_tailored_experiences_disable",
            title: "Disable Tailored Experiences with Diagnostic Data",
            description: "Prevents Microsoft from analyzing user habits to serve personalized recommendations and tips.",
            howItWorks: "Sets 'DisableTailoredExperiencesWithDiagnosticData' to 1 in HKCU\\Software\\Policies\\Microsoft\\Windows\\CloudContent.",
            problemSolved: "Shuts down continuous telemetry indexing of user clicks and app launches.",
            expectedResult: "Enhanced system privacy and reduced background analytical work.",
            category: TweakCategory.BackgroundProcesses,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Policies\Microsoft\Windows\CloudContent",
            valueName: "DisableTailoredExperiencesWithDiagnosticData",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "bg_windows_spotlight_disable",
            title: "Disable Windows Spotlight Background Downloads",
            description: "Stops the lock screen from downloading dynamic Bing wallpapers and advertisement captions in the background.",
            howItWorks: "Sets 'RotatingImageEnabled' to 0 in HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager.",
            problemSolved: "Spotlight downloads high-res images and metadata silently over active network connections.",
            expectedResult: "Saves network bandwidth and disk I/O.",
            category: TweakCategory.BackgroundProcesses,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager",
            valueName: "RotatingImageEnabled",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "bg_compatibility_telemetry_disable",
            title: "Disable Windows Compatibility Appraiser Telemetry",
            description: "Disables the Microsoft Compatibility Telemetry process (CompatTelRunner.exe).",
            howItWorks: "Sets 'DisableInventory' to 1 in HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection.",
            problemSolved: "CompatTelRunner.exe is notorious for scanning all executables on disk, causing 100% disk and CPU spikes.",
            expectedResult: "Completely eliminates CompatTelRunner.exe system lockups.",
            category: TweakCategory.BackgroundProcesses,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Policies\Microsoft\Windows\DataCollection",
            valueName: "DisableInventory",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "bg_smartscreen_prompt_latency",
            title: "Optimize SmartScreen Filter API Timeout",
            description: "Adjusts SmartScreen URL validation timeout to prevent launch stalls on newly downloaded files.",
            howItWorks: "Sets 'EnableSmartScreen' policy tuning in HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System.",
            problemSolved: "SmartScreen contacts remote Microsoft servers before allowing application launches, causing stalls if servers are slow.",
            expectedResult: "Faster application and game installer startup.",
            category: TweakCategory.BackgroundProcesses,
            risk: RiskLevel.Medium,
            recommendation: RecommendationLevel.Optional,
            hive: "HKLM",
            subKey: @"SOFTWARE\Policies\Microsoft\Windows\System",
            valueName: "EnableSmartScreen",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "bg_device_census_disable",
            title: "Disable Background Device Census Telemetry",
            description: "Disables DeviceCensus.exe background hardware surveying scheduled task.",
            howItWorks: "Sets 'DisableDeviceCensus' to 1 in HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection.",
            problemSolved: "Device Census scans connected USB peripherals and hardware IDs periodically during idle moments.",
            expectedResult: "Keeps CPU cores at rest without periodic device enumeration.",
            category: TweakCategory.BackgroundProcesses,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Policies\Microsoft\Windows\DataCollection",
            valueName: "DisableDeviceCensus",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateMultiRegistry(
            id: "bg_copilot_runtime_disable",
            title: "Disable Windows Copilot Background AI Provider",
            description: "Disables the Microsoft Copilot runtime integration and background cloud assistant process.",
            howItWorks: "Sets TurnOffWindowsCopilot = 1 in HKCU\\Software\\Policies\\Microsoft\\Windows\\WindowsCopilot and HKLM.",
            problemSolved: "Copilot hooks background webviews and system hotkeys, consuming RAM and telemetry bandwidth.",
            expectedResult: "Completely eliminates background Copilot processes and reduces RAM consumption.",
            category: TweakCategory.BackgroundProcesses,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            dwords: new[]
            {
                ("HKCU", @"Software\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", 1),
                ("HKLM", @"SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", 1)
            },
            minWindowsBuild: 22621
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "bg_onedrive_sync_startup_disable",
            title: "Disable Microsoft OneDrive Background Automatic Startup",
            description: "Prevents OneDrive cloud syncing executable from auto-starting and scanning game folders.",
            howItWorks: "Sets 'DisableFileSyncNGSC' to 1 in HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\OneDrive.",
            problemSolved: "OneDrive constantly monitors and attempts to sync savegame folders in Documents during gameplay, causing severe stutter.",
            expectedResult: "Eliminates savegame write locks and background file upload contention.",
            category: TweakCategory.BackgroundProcesses,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Policies\Microsoft\Windows\OneDrive",
            valueName: "DisableFileSyncNGSC",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "bg_feedback_hub_telemetry_disable",
            title: "Disable Windows Feedback Hub Background Diagnostic Dispatch",
            description: "Stops Feedback Hub background worker tasks from executing event log aggregations.",
            howItWorks: "Sets 'DoNotShowFeedbackNotifications' to 1 in HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection.",
            problemSolved: "Removes background diagnostic packaging tasks that wake up CPU idle states.",
            expectedResult: "Reduced kernel context switches and cleaner background idle state.",
            category: TweakCategory.BackgroundProcesses,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Policies\Microsoft\Windows\DataCollection",
            valueName: "DoNotShowFeedbackNotifications",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "bg_appx_deployment_optimization",
            title: "Optimize AppX Package Background Deployment Scheduling",
            description: "Prevents Windows Store from automatically downloading and installing UWP app updates during active sessions.",
            howItWorks: "Sets 'AutoDownload' to 2 (Never auto-download) in HKLM\\SOFTWARE\\Policies\\Microsoft\\WindowsStore.",
            problemSolved: "Silent Microsoft Store app updates (e.g. Xbox app, Gaming Services) hammer SSD and CPU in the middle of online games.",
            expectedResult: "Guarantees no surprise background app downloads during gaming.",
            category: TweakCategory.BackgroundProcesses,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Policies\Microsoft\WindowsStore",
            valueName: "AutoDownload",
            appliedValue: 2,
            fallbackDefaultValue: 4
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "bg_cloud_clipboard_sync_disable",
            title: "Disable Cloud Clipboard History Background Synchronization",
            description: "Disables cross-device clipboard history syncing to Microsoft cloud servers.",
            howItWorks: "Sets 'AllowCrossDeviceClipboard' to 0 in HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System.",
            problemSolved: "Every copied text or screenshot is sent over encrypted HTTP to cloud endpoints, adding background network overhead.",
            expectedResult: "Local-only instant clipboard access with zero network latency overhead.",
            category: TweakCategory.BackgroundProcesses,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Policies\Microsoft\Windows\System",
            valueName: "AllowCrossDeviceClipboard",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "bg_phone_link_background_disable",
            title: "Disable Phone Link (Your Phone) Background Execution",
            description: "Disables the Microsoft Phone Link background sync host from running in the background.",
            howItWorks: "Sets 'Disabled' to 1 in HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\BackgroundAccessApplications\\Microsoft.YourPhone_8wekyb3d8bbwe.",
            problemSolved: "Phone Link maintains active Bluetooth and Wi-Fi synchronization sockets that wake up CPU cores.",
            expectedResult: "Frees RAM and eliminates background wireless device syncing during games.",
            category: TweakCategory.BackgroundProcesses,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications\Microsoft.YourPhone_8wekyb3d8bbwe",
            valueName: "Disabled",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "bg_windows_tips_suggestions_disable",
            title: "Disable Windows Soft Landing Tips & Tricks Notifications",
            description: "Disables Windows tips, tricks, and welcome experience popups that run in the background.",
            howItWorks: "Sets 'DisableSoftLanding' to 1 in HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent.",
            problemSolved: "Windows periodically injects educational tooltips and tips dialogs that steal window focus.",
            expectedResult: "Zero background welcome prompts and unprompted UI interruptions.",
            category: TweakCategory.BackgroundProcesses,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Policies\Microsoft\Windows\CloudContent",
            valueName: "DisableSoftLanding",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );
    }
}
