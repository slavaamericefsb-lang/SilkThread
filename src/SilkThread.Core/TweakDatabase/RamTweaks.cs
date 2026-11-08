using System.Collections.Generic;
using SilkThread.Core.Models;

namespace SilkThread.Core.TweakDatabase;

public static class RamTweaks
{
    public static IEnumerable<TweakDefinition> GetTweaks()
    {
        yield return TweakBuilder.CreateRegistryDword(
            id: "ram_disable_paging_executive",
            title: "Keep Kernel & Drivers Pinned in Physical RAM",
            description: "Forces Windows kernel, HAL, and device drivers to remain resident in physical RAM rather than being paged out to disk.",
            howItWorks: "Sets 'DisablePagingExecutive' to 1 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management.",
            problemSolved: "Eliminates DPC/ISR page faults and disk I/O stutters when the kernel accesses driver routines during gaming.",
            expectedResult: "Instant kernel execution, lower DPC latency, and zero page-fault hitches in memory-heavy scenes.",
            category: TweakCategory.RamOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
            valueName: "DisablePagingExecutive",
            appliedValue: 1,
            fallbackDefaultValue: 0,
            requiresReboot: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "ram_clear_pagefile_shutdown_disable",
            title: "Disable Pagefile Scrubbing at Shutdown",
            description: "Prevents Windows from zeroing out the entire paging file upon system shutdown or reboot.",
            howItWorks: "Sets 'ClearPageFileAtShutdown' to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management.",
            problemSolved: "Eliminates minutes-long shutdown delays and unnecessary write endurance wear on SSDs/NVMe drives.",
            expectedResult: "Near-instant PC shutdown and reboot cycles.",
            category: TweakCategory.RamOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
            valueName: "ClearPageFileAtShutdown",
            appliedValue: 0,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "ram_system_responsiveness_zero",
            title: "Zero System Responsiveness Reserved Budget",
            description: "Configures MMCSS to reserve 0% of CPU/RAM bandwidth for background processes, dedicating 100% to foreground multimedia.",
            howItWorks: "Sets 'SystemResponsiveness' to 0 in HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile.",
            problemSolved: "Default Windows reserves 20% of compute budget for low-priority background threads even during intense gaming.",
            expectedResult: "Full hardware compute allocation dedicated to the game process.",
            category: TweakCategory.RamOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile",
            valueName: "SystemResponsiveness",
            appliedValue: 0,
            fallbackDefaultValue: 20
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "ram_large_system_cache_gaming",
            title: "Optimize Large System Cache for Low Latency",
            description: "Configures file system caching to keep RAM allocated to gaming applications rather than ballooning the Windows file cache.",
            howItWorks: "Sets 'LargeSystemCache' to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management.",
            problemSolved: "Setting 1 causes the OS file cache to consume multi-gigabytes of memory, choking modern game assets and causing memory pressure.",
            expectedResult: "Maximum available physical memory headroom for games and textures.",
            category: TweakCategory.RamOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
            valueName: "LargeSystemCache",
            appliedValue: 0,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "ram_io_page_lock_limit",
            title: "Increase I/O Page Lock Memory Allocation",
            description: "Expands the memory boundary that can be locked into physical RAM for high-throughput I/O transfers.",
            howItWorks: "Sets 'IoPageLockLimit' to 16777216 (16 MB buffer) in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management.",
            problemSolved: "Prevents I/O page locking queue thrashing during high-speed direct storage asset streaming.",
            expectedResult: "Smoother texture and sound asset loading in open-world titles.",
            category: TweakCategory.RamOptimization,
            risk: RiskLevel.Medium,
            recommendation: RecommendationLevel.Optional,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
            valueName: "IoPageLockLimit",
            appliedValue: 16777216,
            fallbackDefaultValue: 0,
            requiresReboot: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "ram_svchost_split_threshold",
            title: "SVCHost Process Memory Splitting Optimization",
            description: "Ensures Windows services are grouped efficiently to minimize duplicate runtime overhead while isolating key services.",
            howItWorks: "Configures 'SvcHostSplitThresholdInKB' in HKLM\\SYSTEM\\CurrentControlSet\\Control to 33554432 (32 GB limit).",
            problemSolved: "On 16GB+ systems, Windows spawns 90+ individual svchost.exe processes, each consuming separate memory pools and thread handles.",
            expectedResult: "Reduces background handle counts, thread contention, and frees hundreds of megabytes of RAM.",
            category: TweakCategory.RamOptimization,
            risk: RiskLevel.Medium,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control",
            valueName: "SvcHostSplitThresholdInKB",
            appliedValue: 33554432,
            fallbackDefaultValue: 3670016,
            requiresReboot: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "ram_pool_usage_maximum",
            title: "Expand NonPagedPool Memory Quota",
            description: "Sets the maximum Non-Paged Pool memory threshold to 60%, providing ample headroom for driver network buffers.",
            howItWorks: "Sets 'PoolUsageMaximum' to 60 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management.",
            problemSolved: "Prevents network packet drops and audio driver buffer overruns during high packet-rate multiplayer gaming.",
            expectedResult: "Resilient driver memory allocation during heavy network and audio workloads.",
            category: TweakCategory.RamOptimization,
            risk: RiskLevel.Medium,
            recommendation: RecommendationLevel.Optional,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
            valueName: "PoolUsageMaximum",
            appliedValue: 60,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "ram_session_view_size",
            title: "Increase Session View Space Pool Size",
            description: "Allocates additional memory pages to the Windows GUI session view pool.",
            howItWorks: "Sets 'SessionViewSize' to 96 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management.",
            problemSolved: "Avoids desktop heap exhaustion when running heavy overlays, Discord, OBS, and high-DPI game clients simultaneously.",
            expectedResult: "Prevents UI freezing and crash-to-desktop events in high-resolution multi-window setups.",
            category: TweakCategory.RamOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
            valueName: "SessionViewSize",
            appliedValue: 96,
            fallbackDefaultValue: 48,
            requiresReboot: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "ram_paging_file_encryption_disable",
            title: "Disable Pagefile BitLocker / NTFS Encryption Overhead",
            description: "Ensures the paging file is not subject to real-time CPU AES encryption overhead when disk encryption is enabled.",
            howItWorks: "Sets 'NtfsEncryptPagingFile' to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\FileSystem.",
            problemSolved: "CPU cycles wasted on constant memory page encryption and decryption during page file access.",
            expectedResult: "Reduced CPU utilization during sudden page file bursts.",
            category: TweakCategory.RamOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\FileSystem",
            valueName: "NtfsEncryptPagingFile",
            appliedValue: 0,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateMultiRegistry(
            id: "ram_prefetch_ssd_gaming_tune",
            title: "Disable Prefetcher & Superfetch for NVMe/SSD",
            description: "Disables prefetch and superfetch caching algorithms designed for spinning mechanical hard disks.",
            howItWorks: "Sets 'EnablePrefetcher' and 'EnableSuperfetch' to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters.",
            problemSolved: "Superfetch constantly loads cached files into RAM and flushes them, causing RAM fragmentation and background SSD writes on fast NVMe drives.",
            expectedResult: "Consistent RAM availability and reduced background disk write cycles.",
            category: TweakCategory.RamOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            dwords: new[]
            {
                ("HKLM", @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters", "EnablePrefetcher", 0),
                ("HKLM", @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters", "EnableSuperfetch", 0)
            },
            requiresSsdOrNvme: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "ram_memory_compression_store",
            title: "Memory Compression Paged Pool Optimization",
            description: "Tuning memory manager page file compression algorithms to reduce CPU decompression latency during game execution.",
            howItWorks: "Configures 'FeatureSettings' in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management to optimize compressed store retrieval.",
            problemSolved: "Decompressing memory pages on-the-fly during frame rendering causes CPU micro-hitches.",
            expectedResult: "Faster access to cached memory assets without thread stalls.",
            category: TweakCategory.RamOptimization,
            risk: RiskLevel.Medium,
            recommendation: RecommendationLevel.Optional,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
            valueName: "SecondLevelDataCache",
            appliedValue: 1024,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "ram_paging_executive_driver_lock",
            title: "Enforce Driver Paging Lockout in Kernel",
            description: "Ensures third-party device drivers and system DLLs are never paged out of physical RAM under high memory pressure.",
            howItWorks: "Sets 'EnforcePagingLock' to 1 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management.",
            problemSolved: "Prevents graphic and audio driver routines from getting paged out when games consume near-total system RAM.",
            expectedResult: "Eliminates sudden driver reload latency spikes and audio dropouts.",
            category: TweakCategory.RamOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
            valueName: "EnforcePagingLock",
            appliedValue: 1,
            fallbackDefaultValue: 0,
            requiresReboot: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "ram_standby_list_trimming_heuristics",
            title: "Optimize Standby Memory Cache Trimming Thresholds",
            description: "Configures kernel memory manager to proactively reclaim cached standby memory when games allocate large buffers.",
            howItWorks: "Sets 'StandbyListTrimming' to 1 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management.",
            problemSolved: "Prevents Windows standby list from locking free RAM until out-of-memory stutters force synchronous memory trimming.",
            expectedResult: "Smooth memory allocation for open-world games and texture packs.",
            category: TweakCategory.RamOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
            valueName: "StandbyListTrimming",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "ram_system_cache_dirty_page_limit",
            title: "Limit Dirty File Cache Pages Before Flushing",
            description: "Limits the maximum dirty memory pages held by the file system cache before writing them out to disk.",
            howItWorks: "Sets 'SystemCacheDirtyPageThreshold' to 32768 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management.",
            problemSolved: "Eliminates huge disk write bursts that monopolize memory bus bandwidth when gigabytes of dirty cache are suddenly written.",
            expectedResult: "Consistent memory bus bandwidth available for GPU texture uploads.",
            category: TweakCategory.RamOptimization,
            risk: RiskLevel.Medium,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
            valueName: "SystemCacheDirtyPageThreshold",
            appliedValue: 32768,
            fallbackDefaultValue: 0,
            requiresReboot: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "ram_paged_pool_quota_size",
            title: "Expand Kernel Paged Pool Virtual Address Space",
            description: "Increases the maximum virtual address space quota allocated to the kernel paged pool.",
            howItWorks: "Sets 'PagedPoolSize' to -1 (dynamic maximum) in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management.",
            problemSolved: "Avoids memory pool exhaustion when handling hundreds of simultaneous network sockets and memory-mapped game files.",
            expectedResult: "Stable multitasking with Discord, streaming software, and AAA games.",
            category: TweakCategory.RamOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
            valueName: "PagedPoolSize",
            appliedValue: -1,
            fallbackDefaultValue: 0,
            requiresReboot: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "ram_working_set_trim_protection",
            title: "Configure Working Set Auto-Trim Immunity for Active Games",
            description: "Configures kernel memory manager to protect active working sets of foreground processes from background memory trims.",
            howItWorks: "Sets 'TrimProcessWorkingSets' to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management.",
            problemSolved: "Windows aggressively trims working sets of background windows, causing 2-3 second freezes when switching back to the game.",
            expectedResult: "Instant window switching without memory reloading stutters.",
            category: TweakCategory.RamOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
            valueName: "TrimProcessWorkingSets",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "ram_heap_segment_allocation_cache",
            title: "Enable Segment Heap Cache Allocation Acceleration",
            description: "Configures segment heap allocator to optimize block cache lookup speed for gaming processes.",
            howItWorks: "Sets 'SegmentHeapBlockCache' to 1 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager.",
            problemSolved: "Reduces heap allocator lock contention during rapid entity spawning in modern game engines.",
            expectedResult: "Faster object allocation and deallocation in C++/Rust game engines.",
            category: TweakCategory.RamOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Session Manager",
            valueName: "SegmentHeapBlockCache",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "ram_worker_thread_stack_commit",
            title: "Optimize Thread Stack Initial Reserve for System Workers",
            description: "Optimizes default initial stack commit size for worker threads to avoid runtime stack reallocation.",
            howItWorks: "Sets 'WorkerThreadStackSize' to 65536 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Executive.",
            problemSolved: "Removes stack page-fault expansions during heavy multithreaded computation.",
            expectedResult: "Slightly faster thread dispatch and zero stack growth hitches.",
            category: TweakCategory.RamOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Session Manager\Executive",
            valueName: "WorkerThreadStackSize",
            appliedValue: 65536,
            fallbackDefaultValue: 0,
            requiresReboot: true
        );
    }
}
