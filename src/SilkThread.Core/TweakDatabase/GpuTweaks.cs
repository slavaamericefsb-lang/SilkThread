using System.Collections.Generic;
using SilkThread.Core.Models;

namespace SilkThread.Core.TweakDatabase;

public static class GpuTweaks
{
    public static IEnumerable<TweakDefinition> GetTweaks()
    {
        yield return TweakBuilder.CreateRegistryDword(
            id: "gpu_hags_enable",
            title: "Hardware-Accelerated GPU Scheduling (HAGS)",
            description: "Allows the GPU to manage its own video memory and scheduling directly, bypassing CPU dispatch overhead.",
            howItWorks: "Sets 'HwSchMode' to 2 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers.",
            problemSolved: "Reduces frame render latency and unlocks advanced features like DLSS 3 Frame Generation and AMD FSR 3.",
            expectedResult: "Lower CPU bottleneck in GPU-intensive titles and reduced frame render times.",
            category: TweakCategory.GpuOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
            valueName: "HwSchMode",
            appliedValue: 2,
            fallbackDefaultValue: 1,
            requiresReboot: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "gpu_variable_refresh_rate_global",
            title: "Enable Global Variable Refresh Rate (VRR)",
            description: "Enables Windows 11 VRR optimization for legacy and borderless windowed games that do not natively support G-Sync or FreeSync.",
            howItWorks: "Sets 'EnablePerProcessSystemVRR' to 1 in HKCU\\Control Panel\\Desktop.",
            problemSolved: "Eliminates screen tearing and stutter in borderless windowed mode when playing non-DX12 native games.",
            expectedResult: "Smooth adaptive sync across all borderless and windowed titles.",
            category: TweakCategory.GpuOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Control Panel\Desktop",
            valueName: "EnablePerProcessSystemVRR",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateMultiRegistry(
            id: "gpu_nvidia_power_mizer_max",
            title: "NVIDIA PowerMizer Prefer Maximum Performance",
            description: "Forces the NVIDIA display driver to lock core and memory clocks to highest 3D performance P-states on AC power.",
            howItWorks: "Sets PowerMizerEnable = 1 and PowerMizerLevelAC = 1 in HKLM\\SYSTEM\\CurrentControlSet\\Services\\nvlddmkm.",
            problemSolved: "Prevents GPU core clock down-clocking during low-GPU-utilization scenarios (e.g. CS2, Valorant menus, low texture scenes).",
            expectedResult: "Eliminates micro-stutter when GPU rapidly shifts between P-states.",
            category: TweakCategory.GpuOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            dwords: new[]
            {
                ("HKLM", @"SYSTEM\CurrentControlSet\Services\nvlddmkm", "PowerMizerEnable", 1),
                ("HKLM", @"SYSTEM\CurrentControlSet\Services\nvlddmkm", "PowerMizerLevel", 1),
                ("HKLM", @"SYSTEM\CurrentControlSet\Services\nvlddmkm", "PowerMizerLevelAC", 1)
            },
            requiresNvidiaGpu: true,
            desktopOnly: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "gpu_nvidia_low_latency_mode",
            title: "NVIDIA Driver Low Latency Queue Limit",
            description: "Restricts maximum pre-rendered frames in the NVIDIA driver pipeline to 1.",
            howItWorks: "Sets 'PreRenderedFrames' to 1 in HKLM\\SYSTEM\\CurrentControlSet\\Services\\nvlddmkm\\Global\\NVTweak.",
            problemSolved: "Reduces buffer queue delay between mouse movement and on-screen frame rendering.",
            expectedResult: "Significantly crisper mouse aiming in competitive first-person shooters.",
            category: TweakCategory.GpuOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Services\nvlddmkm\Global\NVTweak",
            valueName: "PreRenderedFrames",
            appliedValue: 1,
            fallbackDefaultValue: 3,
            requiresNvidiaGpu: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "gpu_amd_anti_lag_profile",
            title: "AMD Radeon Anti-Lag Driver Policy",
            description: "Enables low input lag pacing pacing profile in the AMD Radeon Adrenalin kernel stack.",
            howItWorks: "Sets 'AntiLag' to 1 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000.",
            problemSolved: "Controls CPU work pace to ensure it doesn't get too far ahead of the GPU, reducing input lag.",
            expectedResult: "Lower input-to-display latency on AMD Radeon RX 6000/7000/8000 series cards.",
            category: TweakCategory.GpuOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000",
            valueName: "AntiLag",
            appliedValue: 1,
            fallbackDefaultValue: 0,
            requiresAmdGpu: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "gpu_amd_shader_cache_limit",
            title: "AMD Radeon Unlimited Shader Cache Retention",
            description: "Enables continuous shader cache retention to eliminate real-time PSO compilation hitches.",
            howItWorks: "Sets 'ShaderCache' to 1 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000.",
            problemSolved: "Prevents AMD driver from purging compiled pipeline states, which causes shader compilation stutter in DirectX 12 games.",
            expectedResult: "Eliminates first-encounter stutter in modern PC ports.",
            category: TweakCategory.GpuOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000",
            valueName: "ShaderCache",
            appliedValue: 1,
            fallbackDefaultValue: 0,
            requiresAmdGpu: true
        );

        yield return TweakBuilder.CreateMultiRegistry(
            id: "gpu_tdr_delay_extend",
            title: "Extend GPU Timeout Detection & Recovery (TDR) Delay",
            description: "Increases the driver recovery watchdog timeout from 2 seconds to 10 seconds.",
            howItWorks: "Sets 'TdrDelay' and 'TdrDdiDelay' to 10 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers.",
            problemSolved: "Prevents premature 'Display driver stopped responding' crashes during heavy level loading and shader compilation in UE5.",
            expectedResult: "Greater stability during heavy raytracing and high-resolution texture streaming.",
            category: TweakCategory.GpuOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            dwords: new[]
            {
                ("HKLM", @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "TdrDelay", 10),
                ("HKLM", @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "TdrDdiDelay", 10)
            },
            requiresReboot: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "gpu_dwm_high_priority",
            title: "Desktop Window Manager Display Post-Processing Priority",
            description: "Prioritizes display compositor swapchain flips over mundane desktop windows.",
            howItWorks: "Sets 'GPU Priority' to 8 in HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\DisplayPostProcessing.",
            problemSolved: "Avoids desktop compositor contention when streaming on second monitor or running overlays.",
            expectedResult: "Smooth 60/120/144Hz video playback while gaming on multi-monitor setups.",
            category: TweakCategory.GpuOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\DisplayPostProcessing",
            valueName: "GPU Priority",
            appliedValue: 8,
            fallbackDefaultValue: 5
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "gpu_dx_prefer_high_performance",
            title: "Default DirectX Graphic Preference to Discrete GPU",
            description: "Instructs DirectX runtime to prefer High-Performance discrete graphics adapter for all games by default.",
            howItWorks: "Sets 'DirectXUserGlobalSettings' to 2 in HKCU\\Software\\Microsoft\\DirectX\\UserGpuPreferences.",
            problemSolved: "Prevents hybrid laptop and dual-GPU systems from accidentally launching games on weak integrated iGPUs.",
            expectedResult: "Ensures maximum rendering power is automatically assigned.",
            category: TweakCategory.GpuOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\DirectX\UserGpuPreferences",
            valueName: "DirectXUserGlobalSettings",
            appliedValue: 2,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "gpu_preemption_scheduler_enable",
            title: "Enable Fine-Grained GPU Preemption",
            description: "Enables hardware preemption in the DirectX scheduler for rapid context switching.",
            howItWorks: "Sets 'EnablePreemption' to 1 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers\\Scheduler.",
            problemSolved: "Reduces queuing delays for urgent UI and frame delivery commands.",
            expectedResult: "Faster frame turnaround and lower display latency.",
            category: TweakCategory.GpuOptimization,
            risk: RiskLevel.Medium,
            recommendation: RecommendationLevel.Optional,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Scheduler",
            valueName: "EnablePreemption",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "gpu_borderless_direct_flip_optimization",
            title: "Optimize Windowed Gaming DirectFlip Model",
            description: "Configures Windows 11 Game Config Store to use direct hardware presentation for borderless gaming windows.",
            howItWorks: "Sets 'GameDVR_DSEBehavior' to 2 in HKCU\\System\\GameConfigStore.",
            problemSolved: "Bypasses the traditional windowed presentation queue penalty, delivering Fullscreen Exclusive latency in windowed mode.",
            expectedResult: "Fullscreen-grade input latency with the alt-tab convenience of borderless windowed mode.",
            category: TweakCategory.GpuOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"System\GameConfigStore",
            valueName: "GameDVR_DSEBehavior",
            appliedValue: 2,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "gpu_nvidia_telemetry_container_disable",
            title: "Disable NVIDIA Driver Background Telemetry Reporting",
            description: "Disables telemetry data collection hooks in the NVIDIA display driver that log GPU telemetry events to disk.",
            howItWorks: "Sets 'NvTelemetryReporting' to 0 in HKLM\\SOFTWARE\\NVIDIA Corporation\\Global.",
            problemSolved: "Eliminates periodic disk writes and CPU thread wake-ups initiated by NVIDIA telemetry services while playing games.",
            expectedResult: "Reduced background disk I/O and micro-spikes during intensive gaming.",
            category: TweakCategory.GpuOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\NVIDIA Corporation\Global",
            valueName: "NvTelemetryReporting",
            appliedValue: 0,
            fallbackDefaultValue: 1,
            requiresNvidiaGpu: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "gpu_dxgi_swapchain_waitable_object",
            title: "Enforce DXGI Low-Latency Waitable Object Presentations",
            description: "Enables low-latency frame presentation queuing heuristics for DXGI swapchains across DirectX 11/12.",
            howItWorks: "Sets 'SwapEffectWaitable' to 1 in HKLM\\SOFTWARE\\Microsoft\\DirectX.",
            problemSolved: "Prevents GPU render threads from accumulating multi-frame back-buffers ahead of monitor scanout.",
            expectedResult: "Reduces render-to-photon latency by up to 1 entire frame cycle in modern titles.",
            category: TweakCategory.GpuOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Microsoft\DirectX",
            valueName: "SwapEffectWaitable",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "gpu_amd_ulps_disable",
            title: "Disable AMD Radeon Ultra Low Power State (ULPS)",
            description: "Disables Ultra Low Power State (ULPS) in the AMD Radeon driver, preventing compute engine sleep states.",
            howItWorks: "Sets 'EnableUlps' to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000.",
            problemSolved: "Eliminates severe micro-stutters and black screen delays when GPU rapidly switches between sleep and compute states.",
            expectedResult: "Rock-solid GPU clock stability and instant response in demanding titles on AMD Radeon GPUs.",
            category: TweakCategory.GpuOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000",
            valueName: "EnableUlps",
            appliedValue: 0,
            fallbackDefaultValue: 1,
            requiresAmdGpu: true,
            requiresReboot: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "gpu_intel_arc_adaptive_sync",
            title: "Enable Intel Arc GPU Adaptive Sync Global Policy",
            description: "Forces Intel Arc Graphics display driver to enable adaptive sync frame smoothing globally across all games.",
            howItWorks: "Sets 'GlobalAdaptiveSync' to 1 in HKLM\\SOFTWARE\\Intel\\IGFX\\Display.",
            problemSolved: "Eliminates stuttering and frame tearing on Intel Arc A-series and Battlemage discrete GPUs.",
            expectedResult: "Consistent frame delivery with adaptive sync displays on Intel graphics hardware.",
            category: TweakCategory.GpuOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Intel\IGFX\Display",
            valueName: "GlobalAdaptiveSync",
            appliedValue: 1,
            fallbackDefaultValue: 0,
            requiresIntelGpu: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "gpu_dwm_composition_buffer_count",
            title: "Minimize Desktop Window Manager Composition Back-Buffers",
            description: "Reduces internal DWM compositor buffering from triple buffer to double buffer for windowed games.",
            howItWorks: "Sets 'MaxFrameLatency' to 1 in HKLM\\SOFTWARE\\Microsoft\\Windows\\DWM.",
            problemSolved: "Default desktop window manager buffers 2-3 frames, adding 16-33ms latency to windowed games.",
            expectedResult: "Lower input latency when playing games in windowed or borderless mode.",
            category: TweakCategory.GpuOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Microsoft\Windows\DWM",
            valueName: "MaxFrameLatency",
            appliedValue: 1,
            fallbackDefaultValue: 2
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "gpu_vram_allocation_priority",
            title: "Prioritize Dedicated VRAM over System Shared Memory",
            description: "Configures DirectX kernel to strictly prioritize high-speed discrete GDDR/HBM video memory over slower system RAM.",
            howItWorks: "Sets 'PreferDedicatedMemory' to 1 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers.",
            problemSolved: "Prevents DirectX from early spilling textures into slow system DDR4/DDR5 memory bus before VRAM is completely full.",
            expectedResult: "Eliminates frame drops caused by PCIe texture transfers over shared system memory.",
            category: TweakCategory.GpuOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
            valueName: "PreferDedicatedMemory",
            appliedValue: 1,
            fallbackDefaultValue: 0,
            requiresReboot: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "gpu_nvidia_ansel_overlay_disable",
            title: "Disable NVIDIA Ansel In-Game Photography Hook",
            description: "Disables automatic NVIDIA Ansel camera injection hooks into game executables.",
            howItWorks: "Sets 'EnableAnsel' to 0 in HKLM\\SOFTWARE\\NVIDIA Corporation\\Global.",
            problemSolved: "Removes third-party DLL injection overhead into game main process threads.",
            expectedResult: "Improves compatibility and removes hook latency during game loading and rendering.",
            category: TweakCategory.GpuOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\NVIDIA Corporation\Global",
            valueName: "EnableAnsel",
            appliedValue: 0,
            fallbackDefaultValue: 1,
            requiresNvidiaGpu: true
        );
    }
}
