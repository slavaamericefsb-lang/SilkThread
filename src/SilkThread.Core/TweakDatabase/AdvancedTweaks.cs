using System.Collections.Generic;
using SilkThread.Core.Models;

namespace SilkThread.Core.TweakDatabase;

public static class AdvancedTweaks
{
    public static IEnumerable<TweakDefinition> GetTweaks()
    {
        yield return TweakBuilder.CreateMultiRegistryMixed(
            id: "adv_dwm_thread_priority_realtime",
            title: "Desktop Window Manager High Scheduling Category",
            description: "Elevates DWM compositor tasks to High scheduling category and maximum GPU priority.",
            howItWorks: "Sets Scheduling Category = 'High', Priority = 8, and GPU Priority = 8 in HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Window Manager.",
            problemSolved: "Prevents desktop composition pipeline from dropping frames when background streaming or browser playback is running.",
            expectedResult: "Tear-free presentation and zero frame drops on multi-monitor high-refresh displays.",
            category: TweakCategory.AdvancedTweaks,
            risk: RiskLevel.Medium,
            recommendation: RecommendationLevel.Recommended,
            entries: new (string Hive, string SubKey, string ValueName, object AppliedValue)[]
            {
                ("HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Window Manager", "Scheduling Category", "High"),
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
            subKey: @"SYSTEM\CurrentControlSet\Control\Session Manager\\kernel",
            valueName: "DisableExceptionChainValidation",
            appliedValue: 0,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateMultiRegistryMixed(
            id: "adv_mmcss_audio_realtime_priority",
            title: "MMCSS Audio Engine Real-Time Buffering Priority",
            description: "Configures Multimedia Class Scheduler Service (MMCSS) to treat audio mixing threads with high scheduling priority and zero glitch buffer.",
            howItWorks: "Sets Scheduling Category = 'High' and Priority = 6 in HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Audio.",
            problemSolved: "Prevents USB DAC sound crackle, pops, and audio buffer underruns during 100% CPU spikes in demanding game scenes.",
            expectedResult: "Pristine real-time game audio and spatial positioning without crackling.",
            category: TweakCategory.AdvancedTweaks,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            entries: new (string Hive, string SubKey, string ValueName, object AppliedValue)[]
            {
                ("HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Audio", "Scheduling Category", "High"),
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
            subKey: @"SYSTEM\CurrentControlSet\Control\Session Manager\\Memory Management",
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
            subKey: @"SYSTEM\CurrentControlSet\Control\Session Manager\\kernel",
            valueName: "DpcTimeout",
            appliedValue: 0,
            fallbackDefaultValue: 0,
            requiresReboot: true
        );
    }
}
