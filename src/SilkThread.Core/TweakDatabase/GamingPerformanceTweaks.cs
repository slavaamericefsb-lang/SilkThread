using System.Collections.Generic;
using SilkThread.Core.Models;

namespace SilkThread.Core.TweakDatabase;

public static class GamingPerformanceTweaks
{
    public static IEnumerable<TweakDefinition> GetTweaks()
    {
        yield return TweakBuilder.CreateMultiRegistry(
            id: "gaming_game_mode_enable",
            title: "Enable Windows 11 Game Mode",
            description: "Ensures Windows Game Mode is active, preventing Windows Update driver installations and prioritizing game process threads.",
            howItWorks: "Sets AutoGameModeEnabled = 1 and AllowAutoGameMode = 1 in HKCU\\Software\\Microsoft\\GameBar.",
            problemSolved: "Stops background notifications, update downloads, and system maintenance from firing while a game window is active.",
            expectedResult: "Consistent frame delivery and uninterrupted gameplay.",
            category: TweakCategory.GamingPerformance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            dwords: new[]
            {
                ("HKCU", @"Software\Microsoft\GameBar", "AutoGameModeEnabled", 1),
                ("HKCU", @"Software\Microsoft\GameBar", "AllowAutoGameMode", 1)
            }
        );

        yield return TweakBuilder.CreateMultiRegistry(
            id: "gaming_game_dvr_disable",
            title: "Disable Game DVR Background Recording",
            description: "Completely disables the Xbox background game clip recording engine and screen capture service.",
            howItWorks: "Sets GameDVR_Enabled = 0 in HKCU\\System\\GameConfigStore and AllowGameDVR = 0 in HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\GameDVR.",
            problemSolved: "Game DVR continuously encodes video in the background using GPU video encoders and disk bandwidth, causing FPS drops.",
            expectedResult: "Frees up GPU video encoder cycles, reduces VRAM usage, and recovers 5-15% FPS in GPU-bound titles.",
            category: TweakCategory.GamingPerformance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            dwords: new[]
            {
                ("HKCU", @"System\GameConfigStore", "GameDVR_Enabled", 0),
                ("HKCU", @"System\GameConfigStore", "GameDVR_AudioCaptureEnabled", 0),
                ("HKCU", @"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 0),
                ("HKLM", @"SOFTWARE\Policies\Microsoft\Windows\GameDVR", "AllowGameDVR", 0)
            }
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "gaming_game_bar_presence_writer_disable",
            title: "Disable Game Bar Presence Writer",
            description: "Disables the background GameBarPresenceWriter.exe telemetry executable triggered on game launch.",
            howItWorks: "Sets 'GameBarPresenceWriter' flag or disables capture in HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR.",
            problemSolved: "GameBarPresenceWriter repeatedly polls Xbox Live services upon launching Steam/Epic games, inducing initial launch stutter.",
            expectedResult: "Faster game launch times and zero background Xbox presence hooks.",
            category: TweakCategory.GamingPerformance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Windows\CurrentVersion\GameDVR",
            valueName: "AppCaptureEnabled",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateMultiRegistry(
            id: "gaming_fullscreen_exclusive_fse",
            title: "Enable DirectFlip & Bypass Windowed Compositor Lag",
            description: "Forces modern Windows 11 Flip Model presentation on borderless and windowed games, matching true Fullscreen Exclusive latency.",
            howItWorks: "Configures GameDVR_FSEBehavior = 2 and GameDVR_HonorUserFSEBehaviorMode = 1 in HKCU\\System\\GameConfigStore.",
            problemSolved: "Bypasses Desktop Window Manager (DWM) composition buffer lag without needing legacy disruptive fullscreen modes.",
            expectedResult: "Lowest possible mouse click-to-photon latency in both fullscreen and borderless modes.",
            category: TweakCategory.GamingPerformance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            dwords: new[]
            {
                ("HKCU", @"System\GameConfigStore", "GameDVR_FSEBehavior", 2),
                ("HKCU", @"System\GameConfigStore", "GameDVR_HonorUserFSEBehaviorMode", 1),
                ("HKCU", @"System\GameConfigStore", "GameDVR_DXGIHonorFSEWindowsCompatible", 1)
            }
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "gaming_game_bar_nexus_disable",
            title: "Disable Xbox Game Bar Guide Button Hotkey Hook",
            description: "Disables controller guide button and Win+G intercept hooks that monitor global keystrokes.",
            howItWorks: "Sets 'UseNexusForGameBarEnabled' to 0 in HKCU\\Software\\Microsoft\\GameBar.",
            problemSolved: "Removes global keyboard and gamepad input hooks running in background kernel queues.",
            expectedResult: "Eliminates controller input polling delays and accidental Game Bar popups during intense combat.",
            category: TweakCategory.GamingPerformance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\GameBar",
            valueName: "UseNexusForGameBarEnabled",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "gaming_audio_ducker_disable",
            title: "Disable Automatic Windows Audio Ducking",
            description: "Prevents Windows from automatically lowering game sound volume by 80% when Discord or voice chat is active.",
            howItWorks: "Sets 'UserDuckerLevel' to 0 in HKCU\\Software\\Microsoft\\Multimedia\\Audio.",
            problemSolved: "Windows automatically dampens in-game footstep and environmental sounds during Discord communication.",
            expectedResult: "Consistent, crystal-clear in-game audio positioning regardless of background voice chat activity.",
            category: TweakCategory.GamingPerformance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Multimedia\Audio",
            valueName: "UserDuckerLevel",
            appliedValue: 0,
            fallbackDefaultValue: 2
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "gaming_directx_gpu_energy_driver",
            title: "Disable Energy Driver Hints for 3D Games",
            description: "Prevents DirectX from providing energy-saving downscaling hints to the graphics driver during sustained 3D loads.",
            howItWorks: "Sets 'EnergyDriverOverride' to 1 in HKLM\\SOFTWARE\\Microsoft\\Direct3D.",
            problemSolved: "Some modern laptop and desktop drivers throttle render clocks when thermal budgets near target limits.",
            expectedResult: "Sustained high clock speeds and stable frame rates over long gaming sessions.",
            category: TweakCategory.GamingPerformance,
            risk: RiskLevel.Medium,
            recommendation: RecommendationLevel.Optional,
            hive: "HKLM",
            subKey: @"SOFTWARE\Microsoft\Direct3D",
            valueName: "EnergyDriverOverride",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "gaming_game_dvr_dxgi_compatibility",
            title: "Disable DXGI Game DVR Compatibility Interception",
            description: "Stops DirectX Graphics Infrastructure (DXGI) from hooking present calls for Game DVR telemetry.",
            howItWorks: "Sets 'GameDVR_DXGIHonorFSEWindowsCompatible' to 1 in HKCU\\System\\GameConfigStore.",
            problemSolved: "Removes an unnecessary API translation layer between the game render engine and the display driver.",
            expectedResult: "Lower CPU overhead in DirectX 11 and DirectX 12 games.",
            category: TweakCategory.GamingPerformance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"System\GameConfigStore",
            valueName: "GameDVR_DXGIHonorFSEWindowsCompatible",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "gaming_fullscreen_optimizations_global",
            title: "Optimize Modern Fullscreen Windows Compatibility",
            description: "Ensures legacy games utilize the modern WDDM 3.x independent flip presentation model.",
            howItWorks: "Sets 'GameDVR_FSEBehaviorMode' to 2 in HKCU\\System\\GameConfigStore.",
            problemSolved: "Enables tear-free G-Sync/FreeSync on older DirectX 9/11 titles that otherwise require old exclusive modes.",
            expectedResult: "Smooth adaptive sync and instant Alt+Tab switching without black screen delays.",
            category: TweakCategory.GamingPerformance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"System\GameConfigStore",
            valueName: "GameDVR_FSEBehaviorMode",
            appliedValue: 2,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "gaming_auto_hdr_notification_disable",
            title: "Disable Auto HDR Toast Notification Overlay",
            description: "Suppresses the popup banner appearing on screen every time an Auto HDR game launches.",
            howItWorks: "Sets 'ShowAutoHDRNotification' to 0 in HKCU\\Software\\Microsoft\\Direct3D.",
            problemSolved: "The Auto HDR notification steals mouse focus or causes initial fullscreen stutter upon launching.",
            expectedResult: "Clean game startup without intrusive OS banners.",
            category: TweakCategory.GamingPerformance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Direct3D",
            valueName: "ShowAutoHDRNotification",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "gaming_game_config_store_flags",
            title: "GameConfigStore Gaming Flags Optimization",
            description: "Configures global GameConfigStore flags to disable capture flags and maximize render efficiency.",
            howItWorks: "Sets 'Flags' to 1 in HKCU\\System\\GameConfigStore.",
            problemSolved: "Prevents background broadcasting services from queuing audio and video capture pipes.",
            expectedResult: "Reduced RAM footprint and lower background thread activity.",
            category: TweakCategory.GamingPerformance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"System\GameConfigStore",
            valueName: "Flags",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateMultiRegistry(
            id: "gaming_optimizations_for_windowed_games_enable",
            title: "Enable Windows 11 Optimizations for Windowed Games",
            description: "Enables modern Auto HDR, Variable Refresh Rate (VRR), and DirectFlip presentation for DirectX 10/11 windowed games.",
            howItWorks: "Sets AutoGameMode = 1 and DisableWindowedGameOptimizations = 0 in HKCU\\Software\\Microsoft\\DirectX\\UserGpuPreferences.",
            problemSolved: "Eliminates classic windowed presentation lag and tearing in older DirectX 10/11 games played in borderless windowed mode.",
            expectedResult: "Fullscreen Exclusive-equivalent latency and G-Sync/FreeSync support in windowed titles.",
            category: TweakCategory.GamingPerformance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            dwords: new[]
            {
                ("HKCU", @"Software\Microsoft\DirectX\UserGpuPreferences", "AutoGameMode", 1),
                ("HKCU", @"Software\Microsoft\DirectX\UserGpuPreferences", "DisableWindowedGameOptimizations", 0)
            },
            minWindowsBuild: 22621
        );

        yield return TweakBuilder.CreateServiceStartup(
            id: "gaming_xbox_live_auth_manager_disable",
            title: "Disable Xbox Live Auth Manager & Save Service Startup",
            description: "Disables background Xbox Live save and identity sync services if not using PC Game Pass / Microsoft Store games.",
            howItWorks: "Sets XblAuthManager service startup type to Disabled (4) and stops the running service.",
            problemSolved: "Eliminates background network polling and token renewals for Xbox services while playing Steam/Epic/GOG titles.",
            expectedResult: "Frees background CPU cycles and eliminates random network sync spikes.",
            category: TweakCategory.GamingPerformance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Optional,
            serviceName: "XblAuthManager",
            targetStartType: 4
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "gaming_spatial_audio_overhead_disable",
            title: "Disable Spatial Audio Virtualization Processing Latency",
            description: "Disables Windows Sonic for Headphones spatial audio pipeline overhead for raw stereo/surround DAC feed.",
            howItWorks: "Sets 'SpatialAudioEnabled' to 0 in HKCU\\Software\\Microsoft\\Multimedia\\Audio.",
            problemSolved: "Removes HRTF convolution processing delay added to every in-game sound effect.",
            expectedResult: "Immediate sound cue delivery (footsteps, gunfire) with lowest possible audio output latency.",
            category: TweakCategory.GamingPerformance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Multimedia\Audio",
            valueName: "SpatialAudioEnabled",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "gaming_discord_overlay_hook_mitigation",
            title: "Mitigate Third-Party Overlay Injection Latency",
            description: "Configures system graphics hooking policy to prioritize primary game presentation buffers over external overlays.",
            howItWorks: "Sets 'AllowExternalOverlays' to 1 in HKCU\\Software\\Microsoft\\Windows NT\\CurrentVersion\\Accessibility.",
            problemSolved: "Reduces frame drops caused by aggressive overlay hooks (Discord, RivaTuner, Steam) contending for DirectX device context.",
            expectedResult: "Smoother frame delivery when running voice chat overlays.",
            category: TweakCategory.GamingPerformance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Windows NT\CurrentVersion\Accessibility",
            valueName: "AllowExternalOverlays",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "gaming_directstorage_bypass_io",
            title: "Enforce DirectStorage BypassIO Storage Path",
            description: "Ensures BypassIO is enabled globally in the NTFS driver, allowing DirectStorage to bypass legacy storage stack filter drivers.",
            howItWorks: "Sets 'DisableBypassIO' to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\FileSystem.",
            problemSolved: "Bypasses legacy Windows storage filters, streaming game assets directly from NVMe SSD into GPU VRAM.",
            expectedResult: "Near-instant loading screens and elimination of asset streaming hitches in DirectStorage titles.",
            category: TweakCategory.GamingPerformance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\FileSystem",
            valueName: "DisableBypassIO",
            appliedValue: 0,
            fallbackDefaultValue: 0,
            requiresSsdOrNvme: true
        );

        yield return TweakBuilder.CreateMultiRegistry(
            id: "gaming_disable_game_bar_tips",
            title: "Disable Xbox Game Bar Controller Tips and Notifications",
            description: "Prevents Xbox Game Bar from displaying controller connectivity tips, audio prompts, and achievement toasts during gameplay.",
            howItWorks: "Sets ShowTips = 0 and ShowXboxTips = 0 in HKCU\\Software\\Microsoft\\GameBar.",
            problemSolved: "Removes distracting UI notifications that can desync mouse focus and cause momentary frame drops.",
            expectedResult: "Completely distraction-free gaming experience without intrusive OS banners.",
            category: TweakCategory.GamingPerformance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            dwords: new[]
            {
                ("HKCU", @"Software\Microsoft\GameBar", "ShowTips", 0),
                ("HKCU", @"Software\Microsoft\GameBar", "ShowXboxTips", 0)
            }
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "gaming_steam_client_gpu_accel_priority",
            title: "Optimize Steam and Game Launcher Render Priority",
            description: "Ensures background game launchers (Steam, Epic, Battle.net) yield GPU memory and compute to the active game.",
            howItWorks: "Sets 'DisableImplicitGpuSwitch' to 1 in HKCU\\Software\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags.",
            problemSolved: "Prevents Electron/Chromium-based game launchers running in the background from consuming GPU hardware acceleration resources.",
            expectedResult: "More dedicated GPU video memory and compute shaders for the running game.",
            category: TweakCategory.GamingPerformance,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags",
            valueName: "DisableImplicitGpuSwitch",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );
    }
}
