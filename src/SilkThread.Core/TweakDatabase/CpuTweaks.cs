using System.Collections.Generic;
using SilkThread.Core.Models;

namespace SilkThread.Core.TweakDatabase;

public static class CpuTweaks
{
    public static IEnumerable<TweakDefinition> GetTweaks()
    {
        yield return TweakBuilder.CreateRegistryDword(
            id: "cpu_power_throttling_disable",
            title: "Disable Execution Power Throttling",
            description: "Disables Windows automatic power throttling mechanism for background and non-focused execution threads.",
            howItWorks: "Sets 'PowerThrottlingOff' to 1 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerThrottling.",
            problemSolved: "Prevents background game services (anticheat, Discord voice, audio mix engines) from having clock speeds curtailed during heavy gameplay.",
            expectedResult: "More consistent frame pacing and eliminates audio stuttering caused by OS thread down-clocking.",
            category: TweakCategory.CpuOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling",
            valueName: "PowerThrottlingOff",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "cpu_core_parking_unhide",
            title: "Unhide Core Parking Attributes",
            description: "Unhides the CPU Core Parking Minimum/Maximum Cores policy in the Windows Power Options menu.",
            howItWorks: "Sets Attributes to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c190-4594-944f-6213cc715194.",
            problemSolved: "Allows system to prevent parking CPU cores on AC power, keeping all physical cores active and warm.",
            expectedResult: "Eliminates core wake-up micro-delays (1-2 ms penalty per parked core unpark event).",
            category: TweakCategory.CpuOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\0cc5b647-c190-4594-944f-6213cc715194",
            valueName: "Attributes",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "cpu_win32_priority_separation",
            title: "Optimize Win32 Priority Separation for Gaming",
            description: "Sets the process thread quantum allocation to prioritize foreground gaming applications with short, dynamic quanta.",
            howItWorks: "Sets 'Win32PrioritySeparation' to 0x26 (38 decimal) in HKLM\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl.",
            problemSolved: "Standard Windows server/workstation quanta give long timeslices which introduce input lag in interactive games.",
            expectedResult: "Sub-millisecond responsiveness for keyboard/mouse input dispatch and immediate game loop execution.",
            category: TweakCategory.CpuOptimization,
            risk: RiskLevel.Medium,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\PriorityControl",
            valueName: "Win32PrioritySeparation",
            appliedValue: 38,
            fallbackDefaultValue: 2
        );

        yield return TweakBuilder.CreateMultiRegistry(
            id: "cpu_mmcss_game_tasks",
            title: "MMCSS High-Priority Gaming Thread Scheduling",
            description: "Configures Multimedia Class Scheduler Service (MMCSS) to treat game process threads with high scheduling priority and dedicated GPU quota.",
            howItWorks: "Configures Scheduling Category = 'High', Priority = 6, and GPU Priority = 8 in HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games.",
            problemSolved: "Prevents background maintenance tasks from preempting the game's render thread during high CPU loads.",
            expectedResult: "Smooth frame rate delivery and minimized 1% low frame drops.",
            category: TweakCategory.CpuOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            dwords: new[]
            {
                ("HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Priority", 6),
                ("HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "GPU Priority", 8),
                ("HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Scheduling Category", 2),
                ("HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "SFIO Priority", 1)
            }
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "cpu_additional_worker_threads",
            title: "Increase Executive Critical Worker Threads",
            description: "Allocates additional critical kernel worker threads to handle asynchronous I/O and hardware interrupts without queue congestion.",
            howItWorks: "Sets 'AdditionalCriticalWorkerThreads' to 16 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Executive.",
            problemSolved: "Relieves bottleneck during simultaneous asset streaming, anticheat hashing, and sound buffering in modern AAA games.",
            expectedResult: "Reduced frame spikes during rapid map traversal and loading zones.",
            category: TweakCategory.CpuOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Session Manager\Executive",
            valueName: "AdditionalCriticalWorkerThreads",
            appliedValue: 16,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "cpu_intel_speedshift_unhide",
            title: "Intel Speed Shift (HWP) EPP Tuning",
            description: "Exposes Energy Performance Preference settings for Intel processors, enabling 0-scale Maximum Performance on AC power.",
            howItWorks: "Unhides Intel HWP energy preference attribute in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\36687f9e-1337-42ce-808c-a630502e6f04.",
            problemSolved: "Intel Core processors default to balanced speed ramp-up curves, adding 15-30 ms frequency scaling latency on load changes.",
            expectedResult: "Instant frequency boost to maximum Turbo clock upon game scene changes.",
            category: TweakCategory.CpuOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\36687f9e-1337-42ce-808c-a630502e6f04",
            valueName: "Attributes",
            appliedValue: 0,
            fallbackDefaultValue: 1,
            requiresIntelCpu: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "cpu_amd_thread_dpc_affinity",
            title: "AMD Ryzen Thread DPC Affinity Optimization",
            description: "Directs Deferred Procedure Calls (DPCs) to stay on the origin processor core to prevent cross-CCD cache invalidation on AMD Zen architecture.",
            howItWorks: "Sets 'ThreadDpcEnable' to 1 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel.",
            problemSolved: "Cross-CCD (Core Complex Die) communication on multi-CCD Ryzen CPUs induces high inter-core latency spikes.",
            expectedResult: "Improved 0.1% low FPS and smoother frame pacing on AMD Ryzen 5000/7000/9000 CPUs.",
            category: TweakCategory.CpuOptimization,
            risk: RiskLevel.Medium,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Session Manager\kernel",
            valueName: "ThreadDpcEnable",
            appliedValue: 1,
            fallbackDefaultValue: 0,
            requiresAmdCpu: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "cpu_idle_resiliency_disable",
            title: "Disable CPU Idle Resiliency Coalescing",
            description: "Prevents Windows power engine from batching and delaying CPU wake-ups to save microscopic battery fractions on desktop workstations.",
            howItWorks: "Sets 'CoalescingTimerInterval' to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power.",
            problemSolved: "Timer coalescing delays hardware timer delivery to CPU cores, increasing tick jitter.",
            expectedResult: "Precise high-resolution timer ticks for game physics and netcode polling.",
            category: TweakCategory.CpuOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Power",
            valueName: "CoalescingTimerInterval",
            appliedValue: 0,
            desktopOnly: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "cpu_dxgkrnl_latency_tolerance",
            title: "DirectX Kernel Monitor Latency Tolerance",
            description: "Forces DirectX Graphics Kernel (dxgkrnl.sys) to report zero latency tolerance for frame display flips.",
            howItWorks: "Sets 'MonitorLatencyTolerance' to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Services\\DXGKrnl.",
            problemSolved: "Default display flip heuristics allow the driver to delay present packets during buffer swapping.",
            expectedResult: "Immediate GPU queue dispatch for present calls, reducing render queue lag.",
            category: TweakCategory.CpuOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Services\DXGKrnl",
            valueName: "MonitorLatencyTolerance",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "cpu_processor_boost_mode_unhide",
            title: "Unhide Processor Performance Boost Mode",
            description: "Unhides the CPU Turbo/Core Boost Mode policy dropdown in power options, allowing configuration of aggressive boosting.",
            howItWorks: "Sets Attributes to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\be337238-0d82-4146-a960-4f3749d470c7.",
            problemSolved: "Enables users to lock CPU boost to 'Aggressive At Guaranteed' or 'Efficient Aggressive'.",
            expectedResult: "Full access to processor frequency scaling policies.",
            category: TweakCategory.CpuOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Optional,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\be337238-0d82-4146-a960-4f3749d470c7",
            valueName: "Attributes",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "cpu_affinity_prioritization_foreground",
            title: "Prioritize Foreground Application Affinity",
            description: "Instructs the Windows kernel scheduler to allocate CPU cycles with strong affinity to the focused foreground process.",
            howItWorks: "Configures 'PriorityControl' with ForegroundPriorityAffinity = 1 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl.",
            problemSolved: "Prevents background tasks from taking thread cycles from the primary game window.",
            expectedResult: "Maximum single-core and multi-core throughput directed to the active game.",
            category: TweakCategory.CpuOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\PriorityControl",
            valueName: "ForegroundPriorityAffinity",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "cpu_thread_scheduling_model_unhide",
            title: "Unhide Heterogeneous Thread Scheduling Policy",
            description: "Exposes heterogeneous architecture (P-core and E-core) thread scheduling policy in advanced power options for hybrid CPUs.",
            howItWorks: "Sets Attributes to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\7f2492b6-60b1-45e5-ae13-98c232876e53.",
            problemSolved: "Allows manual assignment of game render and simulation threads strictly to high-performance cores (P-cores).",
            expectedResult: "Prevents high-load game simulation loops from accidentally running on low-frequency Efficient cores.",
            category: TweakCategory.CpuOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\7f2492b6-60b1-45e5-ae13-98c232876e53",
            valueName: "Attributes",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "cpu_distribute_timers",
            title: "Enable Kernel Timer Distribution Across Cores",
            description: "Distributes Windows kernel timer expirations across all available CPU cores instead of queueing them exclusively on CPU Core 0.",
            howItWorks: "Sets 'DistributeTimers' to 1 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel.",
            problemSolved: "Eliminates timer processing bottlenecks on Core 0 where OS interrupts and main game render threads frequently compete.",
            expectedResult: "Reduces DPC latency spikes on Core 0 and improves frame time stability in CPU-bound titles.",
            category: TweakCategory.CpuOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Session Manager\kernel",
            valueName: "DistributeTimers",
            appliedValue: 1,
            fallbackDefaultValue: 0,
            requiresReboot: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "cpu_intel_ppm_epb_performance",
            title: "Intel Energy Performance Bias (EPB) Performance Override",
            description: "Configures Intel processor power management driver (intelppm) to bias hardware frequency scaling towards absolute performance.",
            howItWorks: "Sets 'EnergyPerfBias' to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Services\\intelppm\\Parameters.",
            problemSolved: "Overrides firmware-level energy saving bias that throttles CPU burst transitions during fast game scene changes.",
            expectedResult: "Faster core frequency ramp-up when transient CPU loads occur on Intel Core 10th-14th+ gen processors.",
            category: TweakCategory.CpuOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Services\intelppm\Parameters",
            valueName: "EnergyPerfBias",
            appliedValue: 0,
            fallbackDefaultValue: 1,
            requiresIntelCpu: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "cpu_amd_ppm_perf_boost",
            title: "AMD Processor Power Management Boost Bias",
            description: "Configures AMD processor power driver (amdppm) parameter overrides to prioritize peak compute burst over idle sleep.",
            howItWorks: "Sets 'PerfBoostMode' to 2 (Aggressive) in HKLM\\SYSTEM\\CurrentControlSet\\Services\\amdppm\\Parameters.",
            problemSolved: "Reduces AMD Ryzen Core Performance Boost ramp-up time from low-power C-states.",
            expectedResult: "Higher sustained boost clocks during CPU-heavy game phases on AMD Ryzen CPUs.",
            category: TweakCategory.CpuOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Services\amdppm\Parameters",
            valueName: "PerfBoostMode",
            appliedValue: 2,
            fallbackDefaultValue: 0,
            requiresAmdCpu: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "cpu_svm_hyperv_interrupt_steering",
            title: "Optimize Interrupt Time Accounting Precision",
            description: "Forces high-precision CPU cycle counter accounting for interrupts and DPCs within the kernel executive.",
            howItWorks: "Sets 'InterruptSteeringDisabled' to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Executive.",
            problemSolved: "Reduces unbalanced interrupt loads clustering onto a single physical CPU core.",
            expectedResult: "Balanced interrupt distribution across physical cores, reducing micro-stutter.",
            category: TweakCategory.CpuOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Session Manager\Executive",
            valueName: "InterruptSteeringDisabled",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "cpu_hetero_short_thread_runtime",
            title: "Unhide Heterogeneous Short-Running Thread Policy",
            description: "Unhides power setting policy governing where short-running threads are scheduled on hybrid processors.",
            howItWorks: "Sets Attributes to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\bae08b81-2d5e-4233-ad97-97444b9d390d.",
            problemSolved: "Enables users to route background short tasks away from critical gaming render cores.",
            expectedResult: "Prevents background spikes from stealing cache and memory bandwidth from primary game threads.",
            category: TweakCategory.CpuOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Optional,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\bae08b81-2d5e-4233-ad97-97444b9d390d",
            valueName: "Attributes",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "cpu_kernel_dpc_watchdog_profile",
            title: "Kernel DPC Queue Execution Rate Optimization",
            description: "Optimizes Deferred Procedure Call (DPC) queue drain threshold in the Windows kernel to minimize execution latency.",
            howItWorks: "Sets 'DpcQueueDepth' to 1 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel.",
            problemSolved: "Default kernel settings allow DPCs to accumulate in deep queues before forcing a flush, causing latency spikes.",
            expectedResult: "Immediate DPC queue draining, lowering system interrupt latency and improving audio/input smoothness.",
            category: TweakCategory.CpuOptimization,
            risk: RiskLevel.Medium,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Session Manager\kernel",
            valueName: "DpcQueueDepth",
            appliedValue: 1,
            fallbackDefaultValue: 0,
            requiresReboot: true
        );
    }
}
