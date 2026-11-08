using System.Collections.Generic;
using SilkThread.Core.Models;

namespace SilkThread.Core.TweakDatabase;

public static class NetworkOptimizationTweaks
{
    public static IEnumerable<TweakDefinition> GetTweaks()
    {
        yield return TweakBuilder.CreateRegistryDword(
            id: "net_network_throttling_disable",
            title: "Disable Windows Network Throttling Index",
            description: "Completely disables the Windows network packet throttling mechanism, which caps non-multimedia packet rates.",
            howItWorks: "Sets 'NetworkThrottlingIndex' to 0xFFFFFFFF (unlimited) in HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile.",
            problemSolved: "Windows limits network throughput to 10,000 packets per second by default when media or sound is playing, causing micro-drops in multiplayer games.",
            expectedResult: "Maximized UDP/TCP packet dispatch rate and eliminated ping spikes while listening to music or voice chat.",
            category: TweakCategory.NetworkOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile",
            valueName: "NetworkThrottlingIndex",
            appliedValue: -1,
            fallbackDefaultValue: 10
        );

        yield return TweakBuilder.CreateMultiRegistry(
            id: "net_tcp_nagle_algorithm_disable",
            title: "Disable Nagle's Algorithm (TCP No Delay & Instant ACK)",
            description: "Forces TCP stack to send small packets immediately instead of buffering them to wait for larger payloads.",
            howItWorks: "Sets TcpAckFrequency = 1 and TCPNoDelay = 1 under active network interfaces in HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces.",
            problemSolved: "Nagle's algorithm bundles packets together, adding 10-40ms buffering latency to multiplayer position packets and hit registration.",
            expectedResult: "Lowest possible network packet round-trip time (RTT) and crisp hit registration.",
            category: TweakCategory.NetworkOptimization,
            risk: RiskLevel.Medium,
            recommendation: RecommendationLevel.Recommended,
            dwords: new[]
            {
                ("HKLM", @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "TcpAckFrequency", 1),
                ("HKLM", @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "TCPNoDelay", 1),
                ("HKLM", @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "TcpDelAckTicks", 0)
            }
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "net_qos_reserved_bandwidth_zero",
            title: "Disable 20% QoS Reserved Bandwidth Limit",
            description: "Removes the default 20% system bandwidth reservation enforced by Windows Packet Scheduler (QoS).",
            howItWorks: "Sets 'NonBestEffortLimit' to 0 in HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Psched.",
            problemSolved: "Restores the reserved 20% pipe to 100% available capacity for game downloads and low-latency streams.",
            expectedResult: "Full bandwidth utilization for all games and applications.",
            category: TweakCategory.NetworkOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Policies\Microsoft\Windows\Psched",
            valueName: "NonBestEffortLimit",
            appliedValue: 0,
            fallbackDefaultValue: 20
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "net_dns_negative_cache_disable",
            title: "Disable DNS Negative Cache Retention",
            description: "Prevents Windows DNS Client from caching failed host lookups, which block reconnections to game servers.",
            howItWorks: "Sets 'MaxNegativeCacheTtl' to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Services\\Dnscache\\Parameters.",
            problemSolved: "If a server briefly fails to resolve during a match reconnection, Windows locks the failure for 5-15 minutes.",
            expectedResult: "Instant recovery and reconnect capability when joining multiplayer lobbies.",
            category: TweakCategory.NetworkOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Services\Dnscache\Parameters",
            valueName: "MaxNegativeCacheTtl",
            appliedValue: 0,
            fallbackDefaultValue: 5
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "net_default_ttl_standard",
            title: "Set Default IP Packet Time To Live (TTL) to 64",
            description: "Standardizes the IP Time-To-Live header to 64 hops, optimal for modern high-speed internet routing.",
            howItWorks: "Sets 'DefaultTTL' to 64 in HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters.",
            problemSolved: "Non-standard TTL values cause router re-encapsulation overhead and routing anomalies.",
            expectedResult: "Clean IP packet routing across global gaming servers.",
            category: TweakCategory.NetworkOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters",
            valueName: "DefaultTTL",
            appliedValue: 64,
            fallbackDefaultValue: 128
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "net_tcp_max_data_retransmissions",
            title: "Reduce TCP Maximum Data Retransmissions",
            description: "Shortens the retry count for dropped TCP packets from 5 to 3 to trigger fast failover or recovery.",
            howItWorks: "Sets 'TcpMaxDataRetransmissions' to 3 in HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters.",
            problemSolved: "Excessive retransmissions freeze client socket states for up to 30 seconds during network hiccups.",
            expectedResult: "Faster socket recovery and reduced desync duration during transient packet drops.",
            category: TweakCategory.NetworkOptimization,
            risk: RiskLevel.Medium,
            recommendation: RecommendationLevel.Optional,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters",
            valueName: "TcpMaxDataRetransmissions",
            appliedValue: 3,
            fallbackDefaultValue: 5
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "net_enable_pmtu_discovery",
            title: "Enable Path MTU Discovery (PMTU)",
            description: "Enables dynamic Path MTU discovery to automatically find the largest packet size without IP fragmentation.",
            howItWorks: "Sets 'EnablePMTUDiscovery' to 1 in HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters.",
            problemSolved: "Packet fragmentation by intermediate routers doubles latency and causes packet loss.",
            expectedResult: "Optimal packet frame sizes and zero fragmentation overhead.",
            category: TweakCategory.NetworkOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters",
            valueName: "EnablePMTUDiscovery",
            appliedValue: 1,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "net_enable_pmt_black_hole_detect",
            title: "Enable PMTU Black Hole Router Detection",
            description: "Detects routers that drop packets exceeding MTU without returning an ICMP fragmentation needed response.",
            howItWorks: "Sets 'EnablePMTUBHDetect' to 1 in HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters.",
            problemSolved: "Black-hole routers cause unexplained multiplayer disconnects and stalled loading screens.",
            expectedResult: "Resilient connection across diverse international ISP routing paths.",
            category: TweakCategory.NetworkOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters",
            valueName: "EnablePMTUBHDetect",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "net_syn_attack_protect_tune",
            title: "Configure SYN Attack Protection for High PPS",
            description: "Allows higher inbound and outbound connection handshakes before activating SYN protection throttling.",
            howItWorks: "Sets 'SynAttackProtect' to 1 in HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters.",
            problemSolved: "Prevents false-positive TCP handshake throttling during server browser ping queries in Steam/Valve games.",
            expectedResult: "Fast server browser refreshes without missing community servers.",
            category: TweakCategory.NetworkOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters",
            valueName: "SynAttackProtect",
            appliedValue: 1,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "net_disable_netbios_overhead",
            title: "Disable NetBIOS over TCP/IP Overhead",
            description: "Disables legacy NetBIOS broadcasts and name resolution on network interfaces.",
            howItWorks: "Sets 'NetbiosOptions' to 2 in HKLM\\SYSTEM\\CurrentControlSet\\Services\\NetBT\\Parameters.",
            problemSolved: "Broadcast storms from outdated NetBIOS resolution introduce periodic latency spikes.",
            expectedResult: "Clean network adapter traffic without local broadcast clutter.",
            category: TweakCategory.NetworkOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Services\NetBT\Parameters",
            valueName: "NetbiosOptions",
            appliedValue: 2,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "net_wpad_autoproxy_disable",
            title: "Disable Web Proxy Auto-Discovery (WPAD)",
            description: "Stops Windows from continuously broadcasting WPAD queries searching for local network proxy scripts.",
            howItWorks: "Sets 'EnableAutoproxyResultCache' to 0 in HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\\Wpad.",
            problemSolved: "WPAD lookups cause 1-2 second delays when games establish outbound socket connections.",
            expectedResult: "Instant socket connection initiation without DNS proxy lookup pauses.",
            category: TweakCategory.NetworkOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Windows\CurrentVersion\Internet Settings\Wpad",
            valueName: "EnableAutoproxyResultCache",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "net_rss_receive_side_scaling_enable",
            title: "Enable Receive Side Scaling (RSS) for Network Adapters",
            description: "Distributes incoming network packet processing across multiple CPU cores instead of saturating a single core.",
            howItWorks: "Sets 'EnableRSS' to 1 in HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters.",
            problemSolved: "High-packet-rate multiplayer games and voice chat overwhelm CPU Core 0 with network DPCs.",
            expectedResult: "Even network CPU load distribution, lowering ping jitter under heavy traffic.",
            category: TweakCategory.NetworkOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters",
            valueName: "EnableRSS",
            appliedValue: 1,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "net_rsc_receive_segment_coalescing_disable",
            title: "Disable Receive Segment Coalescing (RSC) Latency Buffer",
            description: "Disables NIC hardware segment coalescing to deliver individual incoming network packets immediately.",
            howItWorks: "Sets 'EnableRSC' to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters.",
            problemSolved: "RSC batches incoming TCP/UDP packets inside NIC hardware, adding 1-5ms packet holding delay.",
            expectedResult: "Immediate packet delivery to game netcode, lowering tick-to-tick packet variance.",
            category: TweakCategory.NetworkOptimization,
            risk: RiskLevel.Medium,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters",
            valueName: "EnableRSC",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "net_tcp_window_auto_tuning_normal",
            title: "Optimize TCP Window Auto-Tuning Level to Normal",
            description: "Ensures TCP Receive Window Auto-Tuning is set to Normal, avoiding restricted throughput on Gigabit fiber.",
            howItWorks: "Sets 'TcpAutotuningLevel' to 1 in HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters.",
            problemSolved: "Disabled or restricted autotuning caps download speeds and causes buffer overflow in high-bandwidth games.",
            expectedResult: "Full fiber internet speeds and optimal TCP receive window scaling.",
            category: TweakCategory.NetworkOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters",
            valueName: "TcpAutotuningLevel",
            appliedValue: 1,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "net_ecn_explicit_congestion_notification",
            title: "Disable Explicit Congestion Notification (ECN) Capability",
            description: "Disables ECN negotiation in the Windows TCP/IP stack to prevent packet drops on incompatible game servers and routers.",
            howItWorks: "Sets 'EnableECN' to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters.",
            problemSolved: "Many consumer routers and game server firewalls drop packets marked with ECN flags.",
            expectedResult: "Eliminates random packet loss caused by legacy router ECN drops.",
            category: TweakCategory.NetworkOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters",
            valueName: "EnableECN",
            appliedValue: 0,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "net_tcp_timestamps_disable",
            title: "Disable TCP Timestamps Overhead",
            description: "Disables 12-byte RFC 1323 TCP timestamp options in every network packet header.",
            howItWorks: "Sets 'Tcp1323Opts' to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters.",
            problemSolved: "Removes header overhead and unnecessary CPU timestamp calculation for every transmitted packet.",
            expectedResult: "Lean packet headers and reduced network stack processing overhead.",
            category: TweakCategory.NetworkOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters",
            valueName: "Tcp1323Opts",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "net_teredo_tunneling_disable",
            title: "Disable Teredo IPv6-over-IPv4 Tunneling Interface",
            description: "Disables the Microsoft Teredo IPv6 tunneling adapter overhead when native IPv4/IPv6 is available.",
            howItWorks: "Sets 'Type' to 0 in HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip6\\Parameters.",
            problemSolved: "Teredo tunneling encapsulates packets in extra layers, adding 20-50ms latency in games.",
            expectedResult: "Direct routing over native network interfaces without tunnel overhead.",
            category: TweakCategory.NetworkOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters",
            valueName: "DisabledComponents",
            appliedValue: 8,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "net_arp_cache_expiration_tuning",
            title: "Optimize ARP Cache Retention Lifetime",
            description: "Increases the Address Resolution Protocol (ARP) cache lifetime to avoid frequent gateway re-lookups.",
            howItWorks: "Sets 'ArpCacheLife' to 120 in HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters.",
            problemSolved: "Frequent ARP cache expiration triggers synchronous gateway ARP broadcasts during competitive matches.",
            expectedResult: "Stable gateway resolution without momentary network hitches.",
            category: TweakCategory.NetworkOptimization,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters",
            valueName: "ArpCacheLife",
            appliedValue: 120,
            fallbackDefaultValue: 0
        );
    }
}
