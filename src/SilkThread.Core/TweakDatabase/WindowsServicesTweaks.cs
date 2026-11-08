using System.Collections.Generic;
using SilkThread.Core.Models;

namespace SilkThread.Core.TweakDatabase;

public static class WindowsServicesTweaks
{
    public static IEnumerable<TweakDefinition> GetTweaks()
    {
        yield return TweakBuilder.CreateServiceTweak(
            id: "svc_diagnostic_policy_disable",
            title: "Disable Diagnostic Policy Service (DPS)",
            description: "Stops and disables the Windows Diagnostic Policy Service that detects and logs troubleshooting events.",
            howItWorks: "Sets DPS service startup type to Disabled (4) and terminates the service.",
            problemSolved: "DPS continuously queries event logs, analyzes crashes, and writes diagnostic reports to disk during gaming.",
            expectedResult: "Eliminates diagnostic disk I/O and frees up system memory.",
            category: TweakCategory.WindowsServices,
            risk: RiskLevel.Medium,
            recommendation: RecommendationLevel.Recommended,
            serviceName: "DPS",
            targetStartType: 4
        );

        yield return TweakBuilder.CreateServiceTweak(
            id: "svc_distributed_link_tracking_disable",
            title: "Disable Distributed Link Tracking Client (TrkWks)",
            description: "Disables the legacy service that tracks shortcuts moved between NTFS volumes in local network domains.",
            howItWorks: "Sets TrkWks service startup type to Disabled (4).",
            problemSolved: "Unnecessary on gaming PCs; wastes background resources monitoring NTFS shortcut handles.",
            expectedResult: "Removes unnecessary background link tracking overhead.",
            category: TweakCategory.WindowsServices,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            serviceName: "TrkWks",
            targetStartType: 4
        );

        yield return TweakBuilder.CreateServiceTweak(
            id: "svc_remote_registry_disable",
            title: "Disable Remote Registry Service",
            description: "Ensures the Remote Registry service is permanently disabled to close network registry modification ports.",
            howItWorks: "Sets RemoteRegistry service startup type to Disabled (4).",
            problemSolved: "Leaves a network attack surface open for unauthorized remote registry queries.",
            expectedResult: "Improved network security and eliminated service polling.",
            category: TweakCategory.WindowsServices,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            serviceName: "RemoteRegistry",
            targetStartType: 4
        );

        yield return TweakBuilder.CreateServiceTweak(
            id: "svc_downloaded_maps_manager_disable",
            title: "Disable Downloaded Maps Manager (MapsBroker)",
            description: "Disables the background service that manages offline maps for Windows Maps app.",
            howItWorks: "Sets MapsBroker service startup type to Disabled (4).",
            problemSolved: "MapsBroker wakes up periodically to verify offline map updates over active internet connections.",
            expectedResult: "Prevents unsolicited background map update checks.",
            category: TweakCategory.WindowsServices,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            serviceName: "MapsBroker",
            targetStartType: 4
        );

        yield return TweakBuilder.CreateServiceTweak(
            id: "svc_fax_service_disable",
            title: "Disable Fax Service",
            description: "Disables the legacy Windows Fax service.",
            howItWorks: "Sets Fax service startup type to Disabled (4).",
            problemSolved: "Completely obsolete on gaming PCs; consumes resources in the service control manager.",
            expectedResult: "Removes unused legacy component overhead.",
            category: TweakCategory.WindowsServices,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            serviceName: "Fax",
            targetStartType: 4
        );

        yield return TweakBuilder.CreateServiceTweak(
            id: "svc_touch_keyboard_disable",
            title: "Disable Touch Keyboard & Handwriting Service (TabletInputService)",
            description: "Disables on-screen touch keyboard and pen digitizer handwriting conversion on non-touch desktop systems.",
            howItWorks: "Sets TabletInputService startup type to Disabled (4).",
            problemSolved: "Consistently runs text input framework listeners in background even when only a physical keyboard is attached.",
            expectedResult: "Removes touch input framework hooks from the Windows message pump.",
            category: TweakCategory.WindowsServices,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            serviceName: "TabletInputService",
            targetStartType: 4
        );

        yield return TweakBuilder.CreateServiceTweak(
            id: "svc_retail_demo_disable",
            title: "Disable Retail Demo Service",
            description: "Disables the store retail demonstration mode service.",
            howItWorks: "Sets RetailDemo service startup type to Disabled (4).",
            problemSolved: "Prevents retail kiosk demonstration loops from ever activating.",
            expectedResult: "Zero retail demo activity.",
            category: TweakCategory.WindowsServices,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            serviceName: "RetailDemo",
            targetStartType: 4
        );

        yield return TweakBuilder.CreateServiceTweak(
            id: "svc_wallet_service_disable",
            title: "Disable Windows Wallet Service (WalletService)",
            description: "Disables the Windows Wallet and NFC payment host card emulation service.",
            howItWorks: "Sets WalletService service startup type to Disabled (4).",
            problemSolved: "Desktop gaming PCs do not use NFC point-of-sale payments; service runs redundantly in svchost.",
            expectedResult: "Frees memory and removes unused payment framework.",
            category: TweakCategory.WindowsServices,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            serviceName: "WalletService",
            targetStartType: 4
        );

        yield return TweakBuilder.CreateServiceTweak(
            id: "svc_phone_service_disable",
            title: "Disable Phone / Cellular Telephony Service (PhoneSvc)",
            description: "Disables the cellular phone and SIM calling service on systems without cellular modems.",
            howItWorks: "Sets PhoneSvc service startup type to Disabled (4).",
            problemSolved: "Redundant cellular telephony monitoring on desktop and non-cellular PCs.",
            expectedResult: "Removes unneeded telephony service thread.",
            category: TweakCategory.WindowsServices,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            serviceName: "PhoneSvc",
            targetStartType: 4
        );

        yield return TweakBuilder.CreateServiceTweak(
            id: "svc_pca_service_disable",
            title: "Disable Program Compatibility Assistant (PcaSvc)",
            description: "Disables the Program Compatibility Assistant service that detects and prompts compatibility modes for older games.",
            howItWorks: "Sets PcaSvc service startup type to Disabled (4).",
            problemSolved: "PcaSvc intercepts game process exits and pops up 'This program might not have operated correctly' dialogs.",
            expectedResult: "Clean game process shutdown without unwanted compatibility prompts.",
            category: TweakCategory.WindowsServices,
            risk: RiskLevel.Medium,
            recommendation: RecommendationLevel.Recommended,
            serviceName: "PcaSvc",
            targetStartType: 4
        );

        yield return TweakBuilder.CreateServiceTweak(
            id: "svc_windows_search_manual",
            title: "Set Windows Search Indexer to Manual / Low Overhead",
            description: "Changes Windows Search (WSearch) service startup to Manual (Demand), preventing constant background indexing.",
            howItWorks: "Sets WSearch service startup type to Manual (3).",
            problemSolved: "Search indexer scans game directory files and shaders in real-time, causing disk bottlenecks.",
            expectedResult: "Eliminates background disk indexing during games while preserving manual search capability.",
            category: TweakCategory.WindowsServices,
            risk: RiskLevel.Medium,
            recommendation: RecommendationLevel.Optional,
            serviceName: "WSearch",
            targetStartType: 3,
            stopServiceImmediately: false
        );

        yield return TweakBuilder.CreateServiceStartup(
            id: "svc_sysmain_manual",
            title: "Set SysMain (Superfetch) Service to Manual / Low Overhead",
            description: "Configures SysMain service to Manual start, preventing aggressive RAM caching cycles on modern SSD systems.",
            howItWorks: "Sets SysMain service startup type to Manual (3).",
            problemSolved: "SysMain continuously reads thousands of application binaries into RAM standby list, causing background SSD read queues.",
            expectedResult: "Frees RAM and removes continuous background storage scanning on NVMe/SSD systems.",
            category: TweakCategory.WindowsServices,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            serviceName: "SysMain",
            targetStartType: 3,
            stopServiceImmediately: true
        );

        yield return TweakBuilder.CreateServiceStartup(
            id: "svc_biometric_service_disable",
            title: "Disable Windows Biometric Service (WbioSrvc)",
            description: "Disables the Windows Biometric fingerprint and facial recognition service on systems without biometric sensors.",
            howItWorks: "Sets WbioSrvc service startup type to Disabled (4).",
            problemSolved: "Continuously monitors biometric hardware abstraction layer even when no sensors are present.",
            expectedResult: "Removes unused biometric authentication loop from memory.",
            category: TweakCategory.WindowsServices,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            serviceName: "WbioSrvc",
            targetStartType: 4,
            stopServiceImmediately: true
        );

        yield return TweakBuilder.CreateServiceStartup(
            id: "svc_print_spooler_manual",
            title: "Set Print Spooler (Spooler) to Manual for Non-Printing PCs",
            description: "Sets Print Spooler service to Manual instead of Automatic on gaming PCs without physical printers.",
            howItWorks: "Sets Spooler service startup type to Manual (3).",
            problemSolved: "Print Spooler maintains an active RPC listener and memory buffers even if the user never prints documents.",
            expectedResult: "Closes an RPC attack surface and frees background memory footprint.",
            category: TweakCategory.WindowsServices,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Optional,
            serviceName: "Spooler",
            targetStartType: 3,
            stopServiceImmediately: false
        );

        yield return TweakBuilder.CreateServiceStartup(
            id: "svc_alljoyn_router_disable",
            title: "Disable AllJoyn Router Service (AJRouter)",
            description: "Disables the IoT device routing service used for smart home appliances (AllJoyn framework).",
            howItWorks: "Sets AJRouter service startup type to Disabled (4).",
            problemSolved: "Completely unnecessary on gaming workstations; consumes resources in the service dispatcher.",
            expectedResult: "Removes obsolete IoT routing service.",
            category: TweakCategory.WindowsServices,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            serviceName: "AJRouter",
            targetStartType: 4,
            stopServiceImmediately: true
        );

        yield return TweakBuilder.CreateServiceStartup(
            id: "svc_geolocation_service_disable",
            title: "Disable Geolocation Service (lfsvc)",
            description: "Disables the Windows Geolocation Service that tracks physical device position.",
            howItWorks: "Sets lfsvc service startup type to Disabled (4).",
            problemSolved: "Stops background location event notifications and Wi-Fi scanning routines.",
            expectedResult: "Prevents Wi-Fi scanning latency spikes and protects location privacy.",
            category: TweakCategory.WindowsServices,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            serviceName: "lfsvc",
            targetStartType: 4,
            stopServiceImmediately: true
        );

        yield return TweakBuilder.CreateServiceStartup(
            id: "svc_offline_files_disable",
            title: "Disable Offline Files Service (CscService)",
            description: "Disables the Client Side Caching (CSC) service that syncs network shares for offline access.",
            howItWorks: "Sets CscService service startup type to Disabled (4).",
            problemSolved: "Offline Files periodically scans network shares and locks file handles, causing file access delays.",
            expectedResult: "Eliminates network file synchronization overhead on home gaming setups.",
            category: TweakCategory.WindowsServices,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            serviceName: "CscService",
            targetStartType: 4,
            stopServiceImmediately: true
        );
    }
}
