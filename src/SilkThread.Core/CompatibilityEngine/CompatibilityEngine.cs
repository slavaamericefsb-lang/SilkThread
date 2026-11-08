using System;
using SilkThread.Core.Models;

namespace SilkThread.Core.CompatibilityEngine;

public interface ICompatibilityEngine
{
    CompatibilityResult EvaluateCompatibility(TweakDefinition tweak, HardwareProfile profile);
}

public class CompatibilityEngine : ICompatibilityEngine
{
    public CompatibilityResult EvaluateCompatibility(TweakDefinition tweak, HardwareProfile profile)
    {
        // 1. Hardware-level GPU checks
        if (tweak.RequiresNvidiaGpu)
        {
            bool hasNvidia = profile.Gpus.Any(g => g.IsNvidia);
            if (!hasNvidia)
            {
                return CompatibilityResult.Incompatible("Requires NVIDIA GeForce / RTX graphics card. Current GPU: " + profile.PrimaryGpu.Name);
            }
        }

        if (tweak.RequiresAmdGpu)
        {
            bool hasAmd = profile.Gpus.Any(g => g.IsAmd);
            if (!hasAmd)
            {
                return CompatibilityResult.Incompatible("Requires AMD Radeon graphics card. Current GPU: " + profile.PrimaryGpu.Name);
            }
        }

        if (tweak.RequiresIntelGpu)
        {
            bool hasIntel = profile.Gpus.Any(g => g.IsIntel);
            if (!hasIntel)
            {
                return CompatibilityResult.Incompatible("Requires Intel Arc / Iris graphics card. Current GPU: " + profile.PrimaryGpu.Name);
            }
        }

        // 2. CPU vendor checks
        if (tweak.RequiresIntelCpu && !profile.Cpu.IsIntel)
        {
            return CompatibilityResult.Incompatible("Optimized exclusively for Intel Core processors. Current CPU: " + profile.Cpu.Name);
        }

        if (tweak.RequiresAmdCpu && !profile.Cpu.IsAmd)
        {
            return CompatibilityResult.Incompatible("Optimized exclusively for AMD Ryzen processors. Current CPU: " + profile.Cpu.Name);
        }

        // 3. Storage checks (SSD vs HDD)
        if (tweak.RequiresSsdOrNvme)
        {
            var systemDrive = profile.Storage.FirstOrDefault(d => d.IsSystemDrive) ?? profile.Storage.FirstOrDefault();
            if (systemDrive != null && systemDrive.MediaType == "HDD")
            {
                return CompatibilityResult.Incompatible("Tweak is designed specifically for Solid State Drives (SSD/NVMe). Current drive: " + systemDrive.Model);
            }
        }

        // 4. Laptop / Battery checks
        if (tweak.DesktopOnly && profile.Power.IsLaptop)
        {
            return CompatibilityResult.Incompatible("Desktop-only optimization. Bypasses power saving limits not recommended on battery-powered laptops.");
        }

        if (tweak.LaptopOnly && !profile.Power.IsLaptop)
        {
            return CompatibilityResult.Incompatible("Laptop-only power management tuning. Current system is a desktop workstation.");
        }

        // 5. Windows OS version / build checks
        if (tweak.MinWindowsBuild > 0 && profile.Os.BuildNumber < tweak.MinWindowsBuild)
        {
            return CompatibilityResult.Incompatible($"Requires Windows 11 Build {tweak.MinWindowsBuild} or higher. Current Build: {profile.Os.BuildNumber}");
        }

        // 6. Custom tweak-specific predicate check
        if (tweak.CustomCompatibilityCheck != null)
        {
            try
            {
                return tweak.CustomCompatibilityCheck(profile);
            }
            catch (Exception ex)
            {
                return CompatibilityResult.VerificationNeeded("Unable to evaluate dynamic compatibility rule: " + ex.Message);
            }
        }

        return CompatibilityResult.Compatible;
    }
}
