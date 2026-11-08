using System.Collections.Generic;
using SilkThread.Core.Models;

namespace SilkThread.Core.TweakDatabase;

public static class AdvancedTweaks
{
    public static IEnumerable<TweakDefinition> GetTweaks()
    {
        yield return TweakBuilder.CreateRegistryDword(
            id: "adv_win32_priority_quanta_28",
            title: "Win32PrioritySeparation 0x28 (Competitive Gaming Focus)",
            description: "Configures process priority quantum to 0x28 (40 decimal): 3:1 foreground boost with fixed long timeslices.",
            howItWorks: "Sets 'Win32PrioritySeparation' to 40 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl.",
            problemSolved: "Stabilizes thread execution by giving the focused game process uninterrupted, generous CPU time quanta.",
            expectedResult: "Highest average frame rate and minimal frame pacing variance in competitive eSports titles.",
            category: TweakCategory.AdvancedTweaks,
            risk: RiskLevel.Medium,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\PriorityControl",
            valueName: "Win32PrioritySeparation",
            appliedValue: 40,
            fallbackDefaultValue: 2
        );

        yield return TweakBuilder.CreateMultiRegistry(
            id: "adv_dwm_thread_priority_realtime",
            title: "Desktop Window Manager High Scheduling Category",
            description: "Elevates DWM compositor tasks to High scheduling category and maximum GPU priority.",
            howItWorks: "Sets Scheduling Category = 2, Priority = 8, and GPU Priority = 8 in HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Window Manager.",
            problemSolved: "Prevents desktop composition pipeline from dropping frames when background streaming or browser playback is running.",
            expectedResult: "Tear-free presentation and zero frame drops on multi-monitor high-refresh displays.",
            category: TweakCategory.AdvancedTweaks,
            risk: RiskLevel.Medium,
            recommendation: RecommendationLevel.Recommended,
            dwords: new[]
            {
                ("HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Window Manager", "Scheduling Category", 2),
                ("HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Window Manager", "Priority", 8),
                ("HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Window Manager", "GPU Priority", 8)
            }
        );

        yield return TweakBuilder.CreateMultiRegistry(
            id: "adv_spectre_meltdown_override_experimental",
            title: "Speculative Execution Side-Channel Mitigation Bypass (Experimental)",
            description: "Disables kernel Spectre/Meltdown software branch prediction barriers for raw CPU performance on pure offline/gaming machines.",
            howItWorks: "Sets FeatureSettingsOverride = 3 and FeatureSettingsOverrideMask = 3 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management.",
            problemSolved: "Speculative execution mitigations impose a 3-10% CPU instruction throughput penalty on older 8th-11th gen CPUs.",
            expectedResult: "Recovers raw IPC execution speed. WARNING: Reduces hardware side-channel attack protection. Use only on dedicated gaming rigs.",
            category: TweakCategory.AdvancedTweaks,
            risk: RiskLevel.High,
            recommendation: RecommendationLevel.Experimental,
            dwords: new[]
            {
                ("HKLM", @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettingsOverride", 3),
                ("HKLM", @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettingsOverrideMask", 3)
            },
            requiresReboot: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "adv_system_pages_unlimited",
            title: "Maximize Dynamically Calculated Kernel SystemPages",
            description: "Sets SystemPages to 0, instructing Windows to calculate the maximum possible page table entries based on physical RAM.",
            howItWorks: "Sets 'SystemPages' to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management.",
            problemSolved: "Hardcoded page limits constrain kernel memory mapping on 32GB+ gaming rigs.",
            expectedResult: "Optimal memory page translation throughput across all physical RAM.",
            category: TweakCategory.AdvancedTweaks,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
            valueName: "SystemPages",
            appliedValue: 0,
            fallbackDefaultValue: 0,
            requiresReboot: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "adv_heap_decommit_threshold",
            title: "Optimize Heap Decommit Free Block Threshold",
            description: "Increases the threshold before the process heap decommits memory blocks back to the OS.",
            howItWorks: "Sets 'HeapDeCommitFreeBlockThreshold' to 262144 (256KB) in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager.",
            problemSolved: "Rapidly allocating and freeing memory structures causes constant kernel context switches and page table invalidation.",
            expectedResult: "Smoother memory allocation in complex simulation and physics game engines.",
            category: TweakCategory.AdvancedTweaks,
            risk: RiskLevel.Medium,
            recommendation: RecommendationLevel.Optional,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Session Manager",
            valueName: "HeapDeCommitFreeBlockThreshold",
            appliedValue: 262144,
            fallbackDefaultValue: 0,
            requiresReboot: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "adv_critical_section_timeout",
            title: "Kernel Critical Section Deadlock Timeout Tuning",
            description: "Calibrates the kernel wait timeout for deadlocked thread synchronization primitives.",
            howItWorks: "Sets 'CriticalSectionTimeout' to 2592000 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager.",
            problemSolved: "Prevents false-positive deadlock aborts when heavy shaders hold lock primitives during loading.",
            expectedResult: "Enhanced stability during massive shader compilation passes.",
            category: TweakCategory.AdvancedTweaks,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Session Manager",
            valueName: "CriticalSectionTimeout",
            appliedValue: 2592000,
            fallbackDefaultValue: 2592000
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "adv_session_pool_size_128",
            title: "Expand Session Pool Size to 128MB",
            description: "Allocates 128 megabytes to the kernel session pool for handling complex graphics rendering heaps.",
            howItWorks: "Sets 'SessionPoolSize' to 128 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management.",
            problemSolved: "Session pool exhaustion causes DirectX device-removed (DXGI_ERROR_DEVICE_REMOVED) crashes in multi-monitor 4K setups.",
            expectedResult: "Rock-solid graphics stability during 4K ultra-wide and VR gaming.",
            category: TweakCategory.AdvancedTweaks,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
            valueName: "SessionPoolSize",
            appliedValue: 128,
            fallbackDefaultValue: 48,
            requiresReboot: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "adv_paging_executive_strict_lock",
            title: "Lock Kernel Stack and Non-Paged Pools into RAM",
            description: "Enforces strict physical memory residency for all executive subsystem thread stacks.",
            howItWorks: "Sets 'DisablePagingExecutive' to 1 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management.",
            problemSolved: "Guarantees zero page-fault wait states during audio engine and mouse interrupt routines.",
            expectedResult: "Consistent low DPC latency and audio crackle prevention.",
            category: TweakCategory.AdvancedTweaks,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
            valueName: "DisablePagingExecutive",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "adv_protection_mode_object_directories",
            title: "Enforce ProtectionMode for Kernel Object Directories",
            description: "Enforces standard root object directory security to prevent unauthorized symbol shadowing.",
            howItWorks: "Sets 'ProtectionMode' to 1 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager.",
            problemSolved: "Protects kernel object naming spaces from collision and unauthorized injection.",
            expectedResult: "Maintains kernel naming space security integrity.",
            category: TweakCategory.AdvancedTweaks,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Session Manager",
            valueName: "ProtectionMode",
            appliedValue: 1,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "adv_global_flag_page_heap_disable",
            title: "Ensure Page Heap Debug Flag is Inactive",
            description: "Ensures developer page heap debugging flags (GlobalFlag) are not active in release runtime.",
            howItWorks: "Sets 'GlobalFlag' to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager.",
            problemSolved: "Leftover debugging page heap flags multiply memory consumption by 10x and severely slow down allocations.",
            expectedResult: "Pure release memory speed without diagnostic tracking overhead.",
            category: TweakCategory.AdvancedTweaks,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Session Manager",
            valueName: "GlobalFlag",
            appliedValue: 0,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "adv_kernel_sehop_enable",
            title: "Ensure Structured Exception Handler Overwrite Protection (SEHOP)",
            description: "Maintains SEHOP kernel integrity to prevent game exploit crashes and buffer overflow attacks.",
            howItWorks: "Sets 'SEHOP' policy in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel.",
            problemSolved: "Protects against malicious multi-player server packet injection exploiting stack exception chains.",
            expectedResult: "Solid game process exception resilience in online gaming.",
            category: TweakCategory.AdvancedTweaks,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Session Manager\kernel",
            valueName: "DisableExceptionChainValidation",
            appliedValue: 0,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "adv_hpet_synthetic_timer_tune",
            title: "Disable Synthetic Timer Periodic Expiration Overhead",
            description: "Configures Windows kernel timer subsystem to use direct hardware TSC timers instead of synthetic timer ticks.",
            howItWorks: "Sets 'SyntheticTimers' to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel.",
            problemSolved: "Synthetic timers emulate timer hardware via software virtualization, adding tick jitter and CPU overhead.",
            expectedResult: "Jitter-free high-resolution hardware timestamps for game physics and frame time counters.",
            category: TweakCategory.AdvancedTweaks,
            risk: RiskLevel.Medium,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Session Manager\kernel",
            valueName: "SyntheticTimers",
            appliedValue: 0,
            fallbackDefaultValue: 1,
            requiresReboot: true
        );

        yield return TweakBuilder.CreateMultiRegistry(
            id: "adv_mmcss_audio_realtime_priority",
            title: "MMCSS Audio Engine Real-Time Buffering Priority",
            description: "Configures Multimedia Class Scheduler Service (MMCSS) to treat audio mixing threads with high scheduling priority and zero glitch buffer.",
            howItWorks: "Sets Scheduling Category = 2 (High) and Priority = 6 in HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Audio.",
            problemSolved: "Prevents USB DAC sound crackle, pops, and audio buffer underruns during 100% CPU spikes in demanding game scenes.",
            expectedResult: "Pristine real-time game audio and spatial positioning without crackling.",
            category: TweakCategory.AdvancedTweaks,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            dwords: new[]
            {
                ("HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Audio", "Scheduling Category", 2),
                ("HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Audio", "Priority", 6),
                ("HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Audio", "SFIO Priority", 1)
            }
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "adv_kernel_pool_tagging_gaming",
            title: "Enable Kernel Pool Tagging Allocation Optimization",
            description: "Ensures pool tagging is enabled in the memory manager for efficient slab allocation and memory tracking.",
            howItWorks: "Sets 'PoolTagging' to 1 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management.",
            problemSolved: "Maintains fast kernel memory allocation paths and prevents pool fragmentation.",
            expectedResult: "Predictable driver memory allocation performance.",
            category: TweakCategory.AdvancedTweaks,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
            valueName: "PoolTagging",
            appliedValue: 1,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "adv_kernel_dpc_timeout_polynomial",
            title: "Tune Kernel DPC Timeout Watchdog Execution Interval",
            description: "Adjusts the kernel DPC watchdog timer interval to prevent premature DPC watchdog violation bluescreens during heavy GPU shader compilation.",
            howItWorks: "Sets 'DpcTimeout' to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel.",
            problemSolved: "Long shader compile bursts and asset decompression routines can trigger false-positive DPC_WATCHDOG_VIOLATION errors.",
            expectedResult: "Prevents watchdog crashes during intense loading and shader compiling.",
            category: TweakCategory.AdvancedTweaks,
            risk: RiskLevel.Medium,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Session Manager\kernel",
            valueName: "DpcTimeout",
            appliedValue: 0,
            fallbackDefaultValue: 0,
            requiresReboot: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "adv_executive_fast_fail_strict_lock",
            title: "Optimize FastFail Exception Dispatch Routing",
            description: "Ensures security __fastfail exceptions bypass slow nested SEH frames and terminate cleanly.",
            howItWorks: "Sets 'FastFailRouting' to 1 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel.",
            problemSolved: "Prevents hung process states when a worker thread faults, allowing immediate clean process cleanup.",
            expectedResult: "Immediate process termination without hanging in the taskbar or locking game files.",
            category: TweakCategory.AdvancedTweaks,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Session Manager\kernel",
            valueName: "FastFailRouting",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "adv_device_latency_interrupt_affinity",
            title: "Enable Message Signaled Interrupts (MSI) Priority Allocation",
            description: "Configures interrupt arbitration in the kernel HAL to prioritize device message signaled interrupts over legacy line interrupts.",
            howItWorks: "Sets 'MsiSupported' to 1 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl.",
            problemSolved: "Legacy pin-based IRQs share interrupt lines with other hardware devices, causing interrupt storms and input lag.",
            expectedResult: "Direct vector interrupt delivery, lowering hardware DPC latency.",
            category: TweakCategory.AdvancedTweaks,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\PriorityControl",
            valueName: "MsiSupported",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );
    }
}
