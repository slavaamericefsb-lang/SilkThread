using System.Collections.Generic;
using SilkThread.Core.Models;

namespace SilkThread.Core.TweakDatabase;

public static class PowerManagementTweaks
{
    public static IEnumerable<TweakDefinition> GetTweaks()
    {
        yield return TweakBuilder.CreateRegistryDword(
            id: "power_fast_startup_disable",
            title: "Disable Windows Fast Startup (Hybrid Hibernation)",
            description: "Disables hybrid hibernation boot, ensuring Windows loads fresh kernel drivers on every boot cycle.",
            howItWorks: "Sets 'HiberbootEnabled' to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Power.",
            problemSolved: "Fast Startup saves cached kernel memory and corrupted driver states across boots, causing driver crashes and input lag.",
            expectedResult: "Clean system initialization, zero driver memory leaks, and stable hardware operation.",
            category: TweakCategory.PowerManagement,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Session Manager\Power",
            valueName: "HiberbootEnabled",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "power_pcie_aspm_unhide",
            title: "Unhide PCI Express Link State Power Management (ASPM)",
            description: "Exposes PCIe ASPM power configuration, allowing PCIe graphics cards and NVMe SSDs to run without bus sleep latency.",
            howItWorks: "Sets Attributes to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\501a4d13-42af-4429-9e56-4784f8e32f75\\ee12f906-d277-404b-b6da-e5fa1a576df5.",
            problemSolved: "ASPM powers down the PCIe lanes when GPU or NVMe is briefly idle, adding 10-30 ms bus re-initialization lag.",
            expectedResult: "Full PCIe bus link speed locked at all times on AC power.",
            category: TweakCategory.PowerManagement,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\501a4d13-42af-4429-9e56-4784f8e32f75\ee12f906-d277-404b-b6da-e5fa1a576df5",
            valueName: "Attributes",
            appliedValue: 0,
            fallbackDefaultValue: 1,
            desktopOnly: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "power_usb_selective_suspend_unhide",
            title: "Unhide USB 3.x Power Management Settings",
            description: "Exposes the USB Selective Suspend policy in advanced power plan settings.",
            howItWorks: "Sets Attributes to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\2a737441-1930-4402-8d77-b2bebba308a3\\48e6b7a6-50f5-4782-a5d4-53bb8f07e226.",
            problemSolved: "Enables users to verify USB controller hubs are permanently powered on AC power.",
            expectedResult: "Full visibility and control over USB device power state policies.",
            category: TweakCategory.PowerManagement,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\2a737441-1930-4402-8d77-b2bebba308a3\48e6b7a6-50f5-4782-a5d4-53bb8f07e226",
            valueName: "Attributes",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "power_processor_min_state_unhide",
            title: "Unhide Minimum Processor State Policy",
            description: "Exposes the Minimum Processor State setting in the Windows Power Management panel.",
            howItWorks: "Sets Attributes to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\89ba4784-4ba4-4878-804e-4f0e929c69c3.",
            problemSolved: "Allows locking CPU baseline frequency to 100% on AC power to prevent clock drops.",
            expectedResult: "Direct access to CPU baseline clock throttling controls.",
            category: TweakCategory.PowerManagement,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\89ba4784-4ba4-4878-804e-4f0e929c69c3",
            valueName: "Attributes",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "power_modern_standby_disable",
            title: "Disable Modern Standby (S0 Low Power Idle) on Desktops",
            description: "Forces traditional S3 sleep states instead of connected modern standby on desktop workstations.",
            howItWorks: "Sets 'PlatformAoAcOverride' to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power.",
            problemSolved: "Modern Standby keeps network and background apps executing while the system is supposedly sleeping, causing high temps and crashes.",
            expectedResult: "Stable sleep behavior and cold hardware shutdown without phantom power drain.",
            category: TweakCategory.PowerManagement,
            risk: RiskLevel.Medium,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Power",
            valueName: "PlatformAoAcOverride",
            appliedValue: 0,
            desktopOnly: true,
            requiresReboot: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "power_energy_saver_gaming_override",
            title: "Prevent Energy Saver from Dimming Displays during Gaming",
            description: "Stops Windows 11 Energy Saver from kicking in during low-battery or idle scenarios if a game is active.",
            howItWorks: "Sets 'EnergySaverStatus' override in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power.",
            problemSolved: "Unintended brightness dimming and frame rate throttling when gaming on laptops.",
            expectedResult: "Preserves full screen brightness and performance.",
            category: TweakCategory.PowerManagement,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Power",
            valueName: "EnergySaverStatus",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "power_processor_idle_promote_unhide",
            title: "Unhide Processor Idle Promote Threshold",
            description: "Exposes the threshold that determines how quickly CPU cores transition into low power C-states.",
            howItWorks: "Sets Attributes to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\7b223f10-c343-4e40-ac28-017c0f550da4.",
            problemSolved: "Prevents CPU from prematurely falling into deep sleep states during short frame intervals.",
            expectedResult: "Immediate core execution without C-state exit latency.",
            category: TweakCategory.PowerManagement,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\7b223f10-c343-4e40-ac28-017c0f550da4",
            valueName: "Attributes",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "power_system_cooling_policy_unhide",
            title: "Unhide System Cooling Policy in Power Plans",
            description: "Exposes Active vs Passive Cooling policy for fans and thermal management.",
            howItWorks: "Sets Attributes to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\94d3a615-a899-4ac5-ae2b-e4d8f634367f.",
            problemSolved: "Allows ensuring fans spin up before the CPU throttles frequency down (Active Cooling).",
            expectedResult: "Prevents CPU thermal down-clocking during gaming loads.",
            category: TweakCategory.PowerManagement,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\94d3a615-a899-4ac5-ae2b-e4d8f634367f",
            valueName: "Attributes",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "power_sleep_unattended_timeout_unhide",
            title: "Unhide System Unattended Sleep Timeout Policy",
            description: "Exposes the hidden 2-minute timer that puts Windows to sleep if awakened by background events.",
            howItWorks: "Sets Attributes to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\238c9fa8-0aad-41ed-83f4-97be242c8f20\\7bc4a2f9-d8fc-4469-b07b-33eb785aaca0.",
            problemSolved: "Windows mysteriously falls asleep after 2 minutes when using game controllers that don't reset standard keyboard/mouse idle counters.",
            expectedResult: "Prevents unwanted sleep timeouts while playing games on gamepads.",
            category: TweakCategory.PowerManagement,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\238c9fa8-0aad-41ed-83f4-97be242c8f20\7bc4a2f9-d8fc-4469-b07b-33eb785aaca0",
            valueName: "Attributes",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "power_ahci_hipm_dipm_unhide",
            title: "Unhide SATA AHCI Link Power Management (HIPM/DIPM)",
            description: "Exposes SATA Link Power Management policy to eliminate SATA drive sleep hitches.",
            howItWorks: "Sets Attributes to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\0012ee47-9041-4b5d-9b77-535fba8b1442\\0b2d69d7-a2a1-449c-9680-f91c70521c60.",
            problemSolved: "HIPM/DIPM powers off the SATA link, resulting in 1-second system freezes when secondary SSDs are accessed.",
            expectedResult: "Smooth uninterrupted access to secondary game libraries.",
            category: TweakCategory.PowerManagement,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\0012ee47-9041-4b5d-9b77-535fba8b1442\0b2d69d7-a2a1-449c-9680-f91c70521c60",
            valueName: "Attributes",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "power_gpu_dynamic_pstate_unhide",
            title: "Unhide Heterogeneous GPU Scheduling Power Policy",
            description: "Exposes GPU power management policy attributes in the power engine.",
            howItWorks: "Sets Attributes to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00.",
            problemSolved: "Allows customizing graphics adapter power states alongside CPU configurations.",
            expectedResult: "Synchronized GPU and CPU high-performance power states.",
            category: TweakCategory.PowerManagement,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00",
            valueName: "Attributes",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "power_processor_performance_time_check_unhide",
            title: "Unhide Processor Performance Time Check Interval",
            description: "Exposes the time check interval policy that dictates how often Windows recalculates CPU frequencies.",
            howItWorks: "Sets Attributes to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\4d2bbaab-45ab-4842-bc37-217e2e012030.",
            problemSolved: "Allows lowering the sample interval so the CPU adjusts frequency faster in response to gaming burst loads.",
            expectedResult: "Faster frequency ramp-up during sudden CPU-intensive game scenes.",
            category: TweakCategory.PowerManagement,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\4d2bbaab-45ab-4842-bc37-217e2e012030",
            valueName: "Attributes",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "power_processor_idle_demote_unhide",
            title: "Unhide Processor Idle Demote Threshold Policy",
            description: "Exposes processor idle demote threshold setting to configure core de-escalation from shallow idle states.",
            howItWorks: "Sets Attributes to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\4bdaf4e9-d103-46d7-a5f0-6280744297b6.",
            problemSolved: "Enables configuring the CPU to stay in high-readiness C0/C1 states instead of falling into deep C6/C7 sleep.",
            expectedResult: "Eliminates CPU core wake-up micro-latencies during rapid gaming frame transitions.",
            category: TweakCategory.PowerManagement,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\4bdaf4e9-d103-46d7-a5f0-6280744297b6",
            valueName: "Attributes",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "power_away_mode_disable",
            title: "Disable Windows Away Mode Power Policy",
            description: "Disables Away Mode, which keeps the system partially powered to handle media streaming when sleeping.",
            howItWorks: "Sets 'AwayModeEnabled' to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power.",
            problemSolved: "Prevents PC from entering fake pseudo-sleep states where fans spin but screen is black.",
            expectedResult: "Predictable, true sleep behavior without unwanted background wakes.",
            category: TweakCategory.PowerManagement,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Power",
            valueName: "AwayModeEnabled",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "power_hub_power_saving_disable",
            title: "Disable Selective Power Down for Root USB Hubs",
            description: "Configures root USB hub driver entries to prevent power state transitions to low-power states.",
            howItWorks: "Sets 'EnhancedPowerManagementEnabled' to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Services\\USBSTOR\\Parameters.",
            problemSolved: "Stops external gaming drives, audio interfaces, and DACs from dropping out or crackling due to power cuts.",
            expectedResult: "Consistent 5V rail delivery to high-end audio DACs and external NVMe drives.",
            category: TweakCategory.PowerManagement,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Services\USBSTOR\Parameters",
            valueName: "EnhancedPowerManagementEnabled",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "power_allow_throttling_disable",
            title: "Enforce Hardware P-State Autonomy Without OS Throttling",
            description: "Allows the hardware CPU autonomous power management (HWP/Speed Shift) to operate without OS down-throttling.",
            howItWorks: "Sets 'AllowThrottling' to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power.",
            problemSolved: "Overrides Windows scheduler power cap policies during thermal or load transitions.",
            expectedResult: "Maintains maximum sustained Turbo boost frequencies without OS interference.",
            category: TweakCategory.PowerManagement,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Power",
            valueName: "AllowThrottling",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "power_latency_sensitivity_hint_unhide",
            title: "Unhide Processor Latency Sensitivity Hint Policy",
            description: "Exposes Latency Sensitivity Hint processor power setting in advanced power plan options.",
            howItWorks: "Sets Attributes to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\619b0176-958d-4296-ac52-bc63cf516a0d.",
            problemSolved: "Allows tuning CPU frequency scaling to respond immediately to latency-sensitive thread activity.",
            expectedResult: "Immediate boost response for competitive game loops.",
            category: TweakCategory.PowerManagement,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\619b0176-958d-4296-ac52-bc63cf516a0d",
            valueName: "Attributes",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );
    }
}
