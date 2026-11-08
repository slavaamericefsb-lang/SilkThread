using System.Collections.Generic;
using SilkThread.Core.Models;

namespace SilkThread.Core.TweakDatabase;

public static class WindowsPerformanceTweaks
{
    public static IEnumerable<TweakDefinition> GetTweaks()
    {
        yield return TweakBuilder.CreateRegistryDword(
            id: "win_menu_show_delay",
            title: "Reduce Menu Show Delay to 10ms",
            description: "Reduces the deliberate 400ms delay Windows inserts before opening submenus and contextual lists to 10ms.",
            howItWorks: "Sets 'MenuShowDelay' to 10 in HKCU\\Control Panel\\Desktop.",
            problemSolved: "Removes annoying sluggishness when navigating context menus, taskbar items, and system trays.",
            expectedResult: "Instant, crisp UI response throughout Windows.",
            category: TweakCategory.WindowsPerformance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Control Panel\Desktop",
            valueName: "MenuShowDelay",
            appliedValue: 10,
            fallbackDefaultValue: 400
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "win_transparency_disable",
            title: "Disable Desktop Transparency & Acrylic Blur",
            description: "Turns off real-time acrylic and mica transparency blur effects across taskbar, start menu, and app windows.",
            howItWorks: "Sets 'EnableTransparency' to 0 in HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize.",
            problemSolved: "Real-time Gaussian blur shaders consume dedicated GPU memory and compositor render passes continuously.",
            expectedResult: "Frees GPU fillrate, reduces VRAM usage, and speeds up window switching.",
            category: TweakCategory.WindowsPerformance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
            valueName: "EnableTransparency",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateMultiRegistry(
            id: "win_window_animations_disable",
            title: "Disable Window Animations & Transitions",
            description: "Disables window opening, closing, minimize, and maximize animations.",
            howItWorks: "Sets MinAnimate = 0 in HKCU\\Control Panel\\Desktop\\WindowMetrics and TaskbarAnimations = 0 in Explorer\\Advanced.",
            problemSolved: "Eliminates visual frame lag and GPU composition hitching when Alt-Tabbing or switching windows.",
            expectedResult: "Instantaneous window opening and zero animation stutter.",
            category: TweakCategory.WindowsPerformance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            dwords: new[]
            {
                ("HKCU", @"Control Panel\Desktop\WindowMetrics", "MinAnimate", 0),
                ("HKCU", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAnimations", 0)
            }
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "win_background_apps_global_disable",
            title: "Disable Background UWP Apps Execution",
            description: "Stops packaged Windows Universal apps (News, Weather, Maps, Solitaire) from running silently in the background.",
            howItWorks: "Sets 'GlobalUserDisabled' to 1 in HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\BackgroundAccessApplications.",
            problemSolved: "Dozens of background UWP apps consume CPU cycles, wake up cores, and poll network servers.",
            expectedResult: "Frees CPU cycles and memory, eliminating background network polling.",
            category: TweakCategory.WindowsPerformance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications",
            valueName: "GlobalUserDisabled",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateMultiRegistry(
            id: "win_delivery_optimization_p2p_disable",
            title: "Disable Delivery Optimization P2P Bandwidth Sharing",
            description: "Stops Windows from using your PC as a peer-to-peer torrent seeder for other computers on the Internet.",
            howItWorks: "Sets DODownloadMode = 0 in HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DeliveryOptimization.",
            problemSolved: "Delivery Optimization saturates outbound Internet upload bandwidth, causing game ping spikes and packet loss.",
            expectedResult: "Guaranteed stable ping and zero hidden bandwidth uploads.",
            category: TweakCategory.WindowsPerformance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            dwords: new[]
            {
                ("HKLM", @"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization", "DODownloadMode", 0),
                ("HKCU", @"Software\Microsoft\Windows\CurrentVersion\DeliveryOptimization", "SystemSettingsDownloadMode", 0)
            }
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "win_start_menu_web_search_disable",
            title: "Disable Bing Web Search in Start Menu",
            description: "Prevents Windows Start menu search from querying Bing and MSN web endpoints for every keystroke.",
            howItWorks: "Sets 'DisableSearchBoxSuggestions' to 1 in HKCU\\Software\\Policies\\Microsoft\\Windows\\Explorer.",
            problemSolved: "Start menu searches take 1-3 seconds to query remote web APIs, delaying local app launching.",
            expectedResult: "Instant local application and file search with zero telemetry queries sent to Microsoft Bing.",
            category: TweakCategory.WindowsPerformance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Policies\Microsoft\Windows\Explorer",
            valueName: "DisableSearchBoxSuggestions",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "win_feedback_frequency_never",
            title: "Disable Windows Feedback Surveys & Notifications",
            description: "Sets Windows Feedback prompt frequency to Never, suppressing background feedback triggers.",
            howItWorks: "Sets 'NumberOfSIUFInPeriod' to 0 in HKCU\\Software\\Microsoft\\Siuf\\Rules.",
            problemSolved: "Prevents intrusive feedback notification toasts from interrupting gaming sessions.",
            expectedResult: "Zero feedback prompt interrupts.",
            category: TweakCategory.WindowsPerformance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Siuf\Rules",
            valueName: "NumberOfSIUFInPeriod",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "win_cortana_disable",
            title: "Disable Cortana Voice Assistant",
            description: "Disables Cortana voice indexing and autostart in Windows policies.",
            howItWorks: "Sets 'AllowCortana' to 0 in HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search.",
            problemSolved: "Shuts down background Cortana memory footprint and microphone monitoring threads.",
            expectedResult: "Reclaims RAM and reduces background voice service polling.",
            category: TweakCategory.WindowsPerformance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
            valueName: "AllowCortana",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "win_auto_play_disable",
            title: "Disable AutoPlay for External Media",
            description: "Stops Windows from immediately scanning and executing media queries when USB drives or peripherals are plugged in.",
            howItWorks: "Sets 'NoDriveTypeAutoRun' to 255 in HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer.",
            problemSolved: "Prevents system freezing and security vulnerabilities when inserting gaming flash drives or USB accessories.",
            expectedResult: "Safer and faster USB peripheral connection.",
            category: TweakCategory.WindowsPerformance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer",
            valueName: "NoDriveTypeAutoRun",
            appliedValue: 255,
            fallbackDefaultValue: 145
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "win_aero_shake_disable",
            title: "Disable Aero Shake (Disappearing Windows)",
            description: "Disables the gesture where shaking a window minimizes all other background applications.",
            howItWorks: "Sets 'DisallowShaking' to 1 in HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced.",
            problemSolved: "Rapid mouse movement in windowed gaming or desktop work accidentally triggers mass minimization.",
            expectedResult: "Prevents accidental window minimization during frantic mouse swipes.",
            category: TweakCategory.WindowsPerformance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
            valueName: "DisallowShaking",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "win_snap_assist_disable",
            title: "Disable Snap Assist Auto-Suggestions",
            description: "Stops Windows 11 from showing large popups suggesting other windows to snap alongside the active one.",
            howItWorks: "Sets 'SnapAssist' to 0 in HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced.",
            problemSolved: "Snap suggestions freeze UI responsiveness when resizing or dragging windows near screen edges.",
            expectedResult: "Smooth, unobtrusive window docking without popup delays.",
            category: TweakCategory.WindowsPerformance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
            valueName: "SnapAssist",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "win_visual_effects_best_performance",
            title: "Optimize System Visual Effects for Best Performance",
            description: "Disables smooth scrolling, list box animations, and drop shadows while preserving font smoothing.",
            howItWorks: "Sets VisualFXSetting to 2 (Custom/Performance) in HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects.",
            problemSolved: "Eliminates DWM rendering passes for subtle UI drop shadows and sluggish scroll easing.",
            expectedResult: "Snappier Explorer navigation and reduced compositor GPU memory consumption.",
            category: TweakCategory.WindowsPerformance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects",
            valueName: "VisualFXSetting",
            appliedValue: 2,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "win_lockscreen_slideshow_disable",
            title: "Disable Lock Screen Slideshow and Network Spotlight",
            description: "Disables background image network fetching and rotation for the Windows lock screen.",
            howItWorks: "Sets 'NoLockScreenSlideshow' to 1 in HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Personalization.",
            problemSolved: "Stops background network polling and GPU JPEG decoding tasks during lock/wake cycles.",
            expectedResult: "Immediate wake from sleep and zero background network consumption.",
            category: TweakCategory.WindowsPerformance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Policies\Microsoft\Windows\Personalization",
            valueName: "NoLockScreenSlideshow",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryString(
            id: "win_shell_shortcuts_hotkeys_disable",
            title: "Disable Windows Hotkey Conflict Combinations (Win+W, Win+C)",
            description: "Disables Windows 11 Copilot (Win+C) and Widgets (Win+W) global keyboard hooks.",
            howItWorks: "Sets 'DisabledHotkeys' to 'CW' in HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced.",
            problemSolved: "Prevents accidental activation of heavy web-based AI/widget sidebars when pressing key combos in games.",
            expectedResult: "Eliminates intrusive sidebar popups and removes background hotkey intercept overhead.",
            category: TweakCategory.WindowsPerformance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
            valueName: "DisabledHotkeys",
            appliedValue: "CW"
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "win_search_indexer_backoff_disable",
            title: "Disable Windows Search Indexer CPU Backoff Throttling",
            description: "Prevents the Windows search indexing service from stalling file operations when activity is detected.",
            howItWorks: "Sets 'DisableBackoff' to 1 in HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search.",
            problemSolved: "Stops search indexer from lingering in a high-priority wait state during disk activity.",
            expectedResult: "Faster index completion and lower idle disk thrashing.",
            category: TweakCategory.WindowsPerformance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
            valueName: "DisableBackoff",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateMultiRegistry(
            id: "win_app_suggestions_start_menu_disable",
            title: "Disable Recommended Apps & Tips in Windows 11 Start Menu",
            description: "Disables promoted Microsoft Store apps, recommendations, and recent file suggestions in the Windows 11 Start Menu.",
            howItWorks: "Sets Start_IrisRecommendations = 0 and ShowOrHideMostUsedApps = 0 in HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced.",
            problemSolved: "Removes web-based cloud query delays when opening the Start menu in Windows 11.",
            expectedResult: "Clean, instantaneous Start Menu opening without telemetry feeds.",
            category: TweakCategory.WindowsPerformance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            dwords: new[]
            {
                ("HKCU", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Start_IrisRecommendations", 0),
                ("HKCU", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowOrHideMostUsedApps", 0)
            }
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "win_toast_notifications_quiet_hours",
            title: "Suppress Windows Toast Notifications During Fullscreen Applications",
            description: "Enforces Focus Assist automatic rules to silence all notifications and banners while playing games or running fullscreen apps.",
            howItWorks: "Sets 'FocusAssist' to 2 (Priority only) in HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Notifications\\Settings.",
            problemSolved: "Prevents Windows system notifications from minimizing or desyncing full-screen gaming sessions.",
            expectedResult: "Completely distraction-free fullscreen gaming without unexpected Alt+Tab focus loss.",
            category: TweakCategory.WindowsPerformance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Windows\CurrentVersion\Notifications\Settings",
            valueName: "NOC_GLOBAL_SETTING_ALLOW_CRITICAL_TOASTS_ABOVE_LOCK",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "win_print_screen_snipping_tool_disable",
            title: "Restore Classic Instant PrintScreen Key Behavior",
            description: "Prevents the PrintScreen key from launching the modern heavy Snipping Tool overlay, restoring immediate clipboard capture.",
            howItWorks: "Sets 'PrintScreenSnippetForScreenCaptureEnabled' to 0 in HKCU\\Control Panel\\Keyboard.",
            problemSolved: "Modern Snipping Tool freezes the screen for 500-1000ms while loading its UWP sandbox upon pressing PrintScreen.",
            expectedResult: "Immediate zero-latency clipboard screenshot capture.",
            category: TweakCategory.WindowsPerformance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Control Panel\Keyboard",
            valueName: "PrintScreenSnippetForScreenCaptureEnabled",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );
    }
}
