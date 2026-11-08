using System.Collections.Generic;
using SilkThread.Core.Models;

namespace SilkThread.Core.TweakDatabase;

public static class SecurityPrivacyTweaks
{
    public static IEnumerable<TweakDefinition> GetTweaks()
    {
        yield return TweakBuilder.CreateRegistryDword(
            id: "sec_advertising_id_disable",
            title: "Disable Windows Advertising ID for User Tracking",
            description: "Stops applications from using an individualized advertising ID to track user behavior across apps.",
            howItWorks: "Sets 'Enabled' to 0 in HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\AdvertisingInfo.",
            problemSolved: "Cross-app tracking builds advertising profiles and queries remote advertising telemetry servers.",
            expectedResult: "Protects user privacy and reduces ad tracking requests.",
            category: TweakCategory.SecurityAndPrivacy,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo",
            valueName: "Enabled",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateMultiRegistry(
            id: "sec_activity_history_disable",
            title: "Disable Windows Activity History Collection",
            description: "Stops Windows from recording user actions, open tabs, files, and publishing them to cloud feeds.",
            howItWorks: "Sets EnableActivityFeed = 0 and PublishUserActivities = 0 in HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System.",
            problemSolved: "Activity history records every document and game launch, storing continuous metadata on disk.",
            expectedResult: "Eliminates activity journal overhead and maintains complete usage privacy.",
            category: TweakCategory.SecurityAndPrivacy,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            dwords: new[]
            {
                ("HKLM", @"SOFTWARE\Policies\Microsoft\Windows\System", "EnableActivityFeed", 0),
                ("HKLM", @"SOFTWARE\Policies\Microsoft\Windows\System", "PublishUserActivities", 0),
                ("HKLM", @"SOFTWARE\Policies\Microsoft\Windows\System", "UploadUserActivities", 0)
            }
        );

        yield return TweakBuilder.CreateMultiRegistry(
            id: "sec_inking_typing_personalization_disable",
            title: "Disable Inking & Typing Keystroke Personalization Telemetry",
            description: "Stops Windows from sending typing samples and pen dictionaries to Microsoft servers.",
            howItWorks: "Sets RestrictImplicitInkCollection = 1 and RestrictImplicitTextCollection = 1 in HKCU\\Software\\Microsoft\\InputPersonalization.",
            problemSolved: "Typing telemetry monitors keyboard input frequency and samples words typed in applications.",
            expectedResult: "Ensures keystroke confidentiality and terminates text collection workers.",
            category: TweakCategory.SecurityAndPrivacy,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            dwords: new[]
            {
                ("HKCU", @"Software\Microsoft\InputPersonalization", "RestrictImplicitInkCollection", 1),
                ("HKCU", @"Software\Microsoft\InputPersonalization", "RestrictImplicitTextCollection", 1),
                ("HKCU", @"Software\Microsoft\InputPersonalization\TrainedDataStore", "HarvestContacts", 0)
            }
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "sec_location_tracking_disable",
            title: "Disable Global Windows Location Tracking Sensor",
            description: "Disables the OS geolocation sensing engine for desktop gaming computers.",
            howItWorks: "Sets 'DisableLocation' to 1 in HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\LocationAndSensors.",
            problemSolved: "Desktop computers do not need GPS geolocation; background services poll Wi-Fi BSSIDs to locate the PC.",
            expectedResult: "Terminates background Wi-Fi triangulation scans and protects exact physical location.",
            category: TweakCategory.SecurityAndPrivacy,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors",
            valueName: "DisableLocation",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "sec_telemetry_data_collection_min",
            title: "Restrict Diagnostic Data Collection to Security/Required",
            description: "Caps Windows Diagnostic Data collection to the minimum technical level (Security / Required).",
            howItWorks: "Sets 'AllowTelemetry' to 0 in HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection.",
            problemSolved: "Default 'Optional' telemetry captures memory dumps, visited URLs, and detailed hardware state snapshots.",
            expectedResult: "Zero transmission of optional personal telemetry packets.",
            category: TweakCategory.SecurityAndPrivacy,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Policies\Microsoft\Windows\DataCollection",
            valueName: "AllowTelemetry",
            appliedValue: 0,
            fallbackDefaultValue: 3
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "sec_wifi_sense_hotspot_reporting_disable",
            title: "Disable Wi-Fi Sense & Hotspot Reporting",
            description: "Prevents automatic connection to open suggested hotspots and sharing network credentials.",
            howItWorks: "Sets 'value' to 0 in HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\default\\WiFi\\AllowWiFiHotSpotReporting.",
            problemSolved: "Wi-Fi Sense transmits encrypted SSID and password hashes across Microsoft accounts.",
            expectedResult: "Secure, manual Wi-Fi connection control without automated sharing.",
            category: TweakCategory.SecurityAndPrivacy,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Microsoft\PolicyManager\default\WiFi\AllowWiFiHotSpotReporting",
            valueName: "value",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "sec_cloud_settings_sync_disable",
            title: "Disable Automatic Microsoft Account Settings Sync",
            description: "Stops Windows from syncing desktop wallpaper, passwords, and explorer settings to OneDrive cloud.",
            howItWorks: "Sets 'SyncPolicy' to 5 in HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\SettingSync.",
            problemSolved: "Cloud sync constantly checks for remote changes, occasionally reverting local optimization tweaks.",
            expectedResult: "Maintains strictly local PC settings without remote overwrite.",
            category: TweakCategory.SecurityAndPrivacy,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Windows\CurrentVersion\SettingSync",
            valueName: "SyncPolicy",
            appliedValue: 5,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "sec_biometrics_facial_recognition_disable",
            title: "Disable Windows Hello Camera Sensors on Desktops",
            description: "Stops background polling for infrared facial recognition cameras on desktop systems without IR hardware.",
            howItWorks: "Sets 'Biometrics' policy in HKLM\\SOFTWARE\\Policies\\Microsoft\\Biometrics.",
            problemSolved: "Background biometric authentication services search for biometric sensor devices on boot.",
            expectedResult: "Removes biometric polling delays on standard desktop PCs.",
            category: TweakCategory.SecurityAndPrivacy,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Policies\Microsoft\Biometrics",
            valueName: "Enabled",
            appliedValue: 0,
            fallbackDefaultValue: 1,
            desktopOnly: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "sec_handwriting_error_reporting_disable",
            title: "Disable Handwriting Recognition Error Reporting",
            description: "Stops Windows from transmitting unrecognized handwriting snippets to Microsoft.",
            howItWorks: "Sets 'PreventHandwritingErrorReports' to 1 in HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\HandwritingErrorReports.",
            problemSolved: "Transmits sensitive graphical stroke data over network connections.",
            expectedResult: "Enhanced privacy and zero background reporting queues.",
            category: TweakCategory.SecurityAndPrivacy,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Policies\Microsoft\Windows\HandwritingErrorReports",
            valueName: "PreventHandwritingErrorReports",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "sec_windows_customer_experience_optout",
            title: "Opt-Out of SQM Application Telemetry Data",
            description: "Disables Software Quality Metrics (SQM) telemetry logging across all installed Windows applications.",
            howItWorks: "Sets 'OptIn' to 0 in HKLM\\SOFTWARE\\Policies\\Microsoft\\SQMClient.",
            problemSolved: "Applications generate periodic .sqm log files in user profile directories.",
            expectedResult: "Prevents disk clutter from temporary SQM metric dumps.",
            category: TweakCategory.SecurityAndPrivacy,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Policies\Microsoft\SQMClient",
            valueName: "OptIn",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "sec_app_diagnostic_info_deny",
            title: "Deny Store Apps Access to System Diagnostic Information",
            description: "Blocks downloaded Microsoft Store applications from reading hardware diagnostics and system event logs.",
            howItWorks: "Sets 'Value' to 0 in HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\appDiagnostics.",
            problemSolved: "Third-party Store apps can query CPU temperature, active process lists, and system event history.",
            expectedResult: "Strict containment of third-party UWP app permissions.",
            category: TweakCategory.SecurityAndPrivacy,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\appDiagnostics",
            valueName: "Value",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "sec_remote_assistance_disable",
            title: "Disable Remote Assistance (MSRA) Incoming Connections",
            description: "Disables Remote Assistance invitations and incoming remote control listener ports on Windows.",
            howItWorks: "Sets 'fAllowToGetHelp' to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Remote Assistance.",
            problemSolved: "Closes background listening ports and attack surface for unauthorized remote desktop assistance.",
            expectedResult: "Increased system security and reduced network listening overhead.",
            category: TweakCategory.SecurityAndPrivacy,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Remote Assistance",
            valueName: "fAllowToGetHelp",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "sec_defender_sample_submission_disable",
            title: "Disable Automatic Sample Submission to Microsoft Defender",
            description: "Prevents Microsoft Defender Antivirus from automatically uploading suspicious files and game mod binaries to cloud servers.",
            howItWorks: "Sets 'SubmitSamplesConsent' to 2 (Never send) in HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Spynet.",
            problemSolved: "Defender freezes game mod execution while uploading custom .dll or trainer files to Microsoft cloud analysis.",
            expectedResult: "Prevents game mods and trainers from being unexpectedly blocked or uploaded.",
            category: TweakCategory.SecurityAndPrivacy,
            risk: RiskLevel.Medium,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Policies\Microsoft\Windows Defender\Spynet",
            valueName: "SubmitSamplesConsent",
            appliedValue: 2,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "sec_defender_cloud_protection_reporting",
            title: "Configure Defender Cloud Extended Timeout to Fast Mode",
            description: "Prevents Microsoft Defender Cloud Protection from stalling process launch for up to 10 seconds on newly created game binaries.",
            howItWorks: "Sets 'SpynetReporting' to 1 (Basic) in HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Spynet.",
            problemSolved: "Eliminates multi-second launch stalls when opening newly downloaded games or modded files.",
            expectedResult: "Faster executable launches without long cloud validation freezes.",
            category: TweakCategory.SecurityAndPrivacy,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Policies\Microsoft\Windows Defender\Spynet",
            valueName: "SpynetReporting",
            appliedValue: 1,
            fallbackDefaultValue: 2
        );

        yield return TweakBuilder.CreateRegistryString(
            id: "sec_microphone_background_access_apps",
            title: "Restrict Background Apps Access to System Microphone",
            description: "Prevents background UWP apps from silently recording or accessing the microphone.",
            howItWorks: "Sets 'Value' to 'Deny' in HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\microphone\\NonPackaged.",
            problemSolved: "Unsolicited background microphone listening causes audio buffer jitter and privacy leaks.",
            expectedResult: "Guaranteed audio isolation with microphone access only given to active foreground apps.",
            category: TweakCategory.SecurityAndPrivacy,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\microphone\NonPackaged",
            valueName: "Value",
            appliedValue: "Deny",
            fallbackDefaultValue: "Allow"
        );

        yield return TweakBuilder.CreateRegistryString(
            id: "sec_camera_background_access_deny",
            title: "Restrict Background Apps Access to Webcam",
            description: "Blocks background apps from accessing the connected webcam without explicit user activation.",
            howItWorks: "Sets 'Value' to 'Deny' in HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\webcam\\NonPackaged.",
            problemSolved: "Stops background apps from initializing webcam USB driver endpoints and capturing video.",
            expectedResult: "Prevents webcam driver wakeups and protects video privacy.",
            category: TweakCategory.SecurityAndPrivacy,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\webcam\NonPackaged",
            valueName: "Value",
            appliedValue: "Deny",
            fallbackDefaultValue: "Allow"
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "sec_smartscreen_for_apps_disable",
            title: "Disable Windows Defender SmartScreen for Store Apps",
            description: "Disables SmartScreen filter URL inspection inside modern Windows Store applications.",
            howItWorks: "Sets 'EnableWebContentEvaluation' to 0 in HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\AppHost.",
            problemSolved: "Every web link clicked inside Store apps is inspected via Microsoft servers, delaying navigation.",
            expectedResult: "Instant in-app link opening with zero telemetry lookup.",
            category: TweakCategory.SecurityAndPrivacy,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Windows\CurrentVersion\AppHost",
            valueName: "EnableWebContentEvaluation",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateMultiRegistry(
            id: "sec_voice_activation_above_lock_disable",
            title: "Disable Voice Activation Wake Words Above Lock Screen",
            description: "Prevents voice assistants from listening to microphone while PC is locked.",
            howItWorks: "Sets AgentActivationEnabled = 0 and AgentActivationOnLockScreenEnabled = 0 in HKCU\\Software\\Microsoft\\Speech_OneCore\\Settings\\VoiceActivation\\UserPreferenceDatabase.",
            problemSolved: "Keeps microphone audio DSP active and consuming power even when workstation is locked.",
            expectedResult: "Completely mutes microphone monitoring during locked states.",
            category: TweakCategory.SecurityAndPrivacy,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            dwords: new[]
            {
                ("HKCU", @"Software\Microsoft\Speech_OneCore\Settings\VoiceActivation\UserPreferenceDatabase", "AgentActivationEnabled", 0),
                ("HKCU", @"Software\Microsoft\Speech_OneCore\Settings\VoiceActivation\UserPreferenceDatabase", "AgentActivationOnLockScreenEnabled", 0)
            }
        );
    }
}
