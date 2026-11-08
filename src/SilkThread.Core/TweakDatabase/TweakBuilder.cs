using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SilkThread.Core.Models;
using SilkThread.Core.SystemOperations;

namespace SilkThread.Core.TweakDatabase;

public static class TweakBuilder
{
    public static TweakDefinition CreateRegistryDword(
        string id,
        string title,
        string description,
        string howItWorks,
        string problemSolved,
        string expectedResult,
        TweakCategory category,
        RiskLevel risk,
        RecommendationLevel recommendation,
        string hive,
        string subKey,
        string valueName,
        int appliedValue,
        int? fallbackDefaultValue = null,
        bool requiresReboot = false,
        string[]? conflictsWith = null,
        bool requiresNvidiaGpu = false,
        bool requiresAmdGpu = false,
        bool requiresIntelGpu = false,
        bool requiresIntelCpu = false,
        bool requiresAmdCpu = false,
        bool requiresSsdOrNvme = false,
        bool desktopOnly = false,
        bool laptopOnly = false,
        int minWindowsBuild = 0,
        Func<HardwareProfile, CompatibilityResult>? customCompatibility = null)
    {
        return new TweakDefinition
        {
            Id = id,
            Title = title,
            Description = description,
            HowItWorks = howItWorks,
            ProblemSolved = problemSolved,
            ExpectedResult = expectedResult,
            Category = category,
            Risk = risk,
            Recommendation = recommendation,
            RequiresReboot = requiresReboot,
            ConflictsWith = conflictsWith ?? Array.Empty<string>(),
            RequiresNvidiaGpu = requiresNvidiaGpu,
            RequiresAmdGpu = requiresAmdGpu,
            RequiresIntelGpu = requiresIntelGpu,
            RequiresIntelCpu = requiresIntelCpu,
            RequiresAmdCpu = requiresAmdCpu,
            RequiresSsdOrNvme = requiresSsdOrNvme,
            DesktopOnly = desktopOnly,
            LaptopOnly = laptopOnly,
            MinWindowsBuild = minWindowsBuild,
            CustomCompatibilityCheck = customCompatibility,

            CheckCurrentStateAsync = () =>
            {
                var current = RegistryOperations.GetDword(hive, subKey, valueName);
                if (current.HasValue && current.Value == appliedValue)
                {
                    return Task.FromResult(TweakStatus.Applied);
                }
                return Task.FromResult(TweakStatus.NotApplied);
            },

            ApplyAsync = () =>
            {
                var snapshot = RegistryOperations.SnapshotValue(hive, subKey, valueName);
                bool ok = RegistryOperations.SetDword(hive, subKey, valueName, appliedValue);
                if (ok)
                {
                    return Task.FromResult(TweakOperationResult.Ok(
                        $"Successfully applied {title}.",
                        snapshot.ToJson(),
                        appliedValue.ToString(),
                        requiresReboot
                    ));
                }
                return Task.FromResult(TweakOperationResult.Fail($"Failed to set registry value {subKey}\\{valueName}. Ensure SilkThread runs with Administrator rights."));
            },

            RollbackAsync = (savedStateJson) =>
            {
                if (!string.IsNullOrEmpty(savedStateJson))
                {
                    var snapshot = RegistryValueSnapshot.FromJson(savedStateJson);
                    if (snapshot != null)
                    {
                        bool restored = RegistryOperations.RestoreFromSnapshot(snapshot);
                        if (restored)
                        {
                            return Task.FromResult(TweakOperationResult.Ok($"Successfully restored original state for {title}.", null, null, requiresReboot));
                        }
                    }
                }

                // Fallback rollback: if fallbackDefaultValue specified, write it; otherwise delete value
                if (fallbackDefaultValue.HasValue)
                {
                    RegistryOperations.SetDword(hive, subKey, valueName, fallbackDefaultValue.Value);
                }
                else
                {
                    RegistryOperations.DeleteValue(hive, subKey, valueName);
                }

                return Task.FromResult(TweakOperationResult.Ok($"Reverted {title} to default.", null, null, requiresReboot));
            }
        };
    }

    public static TweakDefinition CreateMultiRegistry(
        string id,
        string title,
        string description,
        string howItWorks,
        string problemSolved,
        string expectedResult,
        TweakCategory category,
        RiskLevel risk,
        RecommendationLevel recommendation,
        (string Hive, string SubKey, string ValueName, int AppliedValue)[] dwords,
        bool requiresReboot = false,
        string[]? conflictsWith = null,
        bool requiresNvidiaGpu = false,
        bool requiresAmdGpu = false,
        bool requiresIntelGpu = false,
        bool requiresIntelCpu = false,
        bool requiresAmdCpu = false,
        bool requiresSsdOrNvme = false,
        bool desktopOnly = false,
        bool laptopOnly = false,
        int minWindowsBuild = 0)
    {
        return new TweakDefinition
        {
            Id = id,
            Title = title,
            Description = description,
            HowItWorks = howItWorks,
            ProblemSolved = problemSolved,
            ExpectedResult = expectedResult,
            Category = category,
            Risk = risk,
            Recommendation = recommendation,
            RequiresReboot = requiresReboot,
            ConflictsWith = conflictsWith ?? Array.Empty<string>(),
            RequiresNvidiaGpu = requiresNvidiaGpu,
            RequiresAmdGpu = requiresAmdGpu,
            RequiresIntelGpu = requiresIntelGpu,
            RequiresIntelCpu = requiresIntelCpu,
            RequiresAmdCpu = requiresAmdCpu,
            RequiresSsdOrNvme = requiresSsdOrNvme,
            DesktopOnly = desktopOnly,
            LaptopOnly = laptopOnly,
            MinWindowsBuild = minWindowsBuild,

            CheckCurrentStateAsync = () =>
            {
                foreach (var item in dwords)
                {
                    var cur = RegistryOperations.GetDword(item.Hive, item.SubKey, item.ValueName);
                    if (!cur.HasValue || cur.Value != item.AppliedValue)
                    {
                        return Task.FromResult(TweakStatus.NotApplied);
                    }
                }
                return Task.FromResult(TweakStatus.Applied);
            },

            ApplyAsync = () =>
            {
                var multi = new RegistryMultiSnapshot();
                bool allOk = true;

                foreach (var item in dwords)
                {
                    multi.Snapshots.Add(RegistryOperations.SnapshotValue(item.Hive, item.SubKey, item.ValueName));
                    if (!RegistryOperations.SetDword(item.Hive, item.SubKey, item.ValueName, item.AppliedValue))
                    {
                        allOk = false;
                    }
                }

                if (allOk)
                {
                    return Task.FromResult(TweakOperationResult.Ok($"Successfully applied {title}.", multi.ToJson(), "applied", requiresReboot));
                }
                return Task.FromResult(TweakOperationResult.Fail($"Failed to apply all registry settings for {title}."));
            },

            RollbackAsync = (savedStateJson) =>
            {
                if (!string.IsNullOrEmpty(savedStateJson))
                {
                    var multi = RegistryMultiSnapshot.FromJson(savedStateJson);
                    if (multi != null)
                    {
                        RegistryOperations.RestoreFromMultiSnapshot(multi);
                        return Task.FromResult(TweakOperationResult.Ok($"Successfully restored original state for {title}.", null, null, requiresReboot));
                    }
                }

                foreach (var item in dwords)
                {
                    RegistryOperations.DeleteValue(item.Hive, item.SubKey, item.ValueName);
                }
                return Task.FromResult(TweakOperationResult.Ok($"Reverted {title}.", null, null, requiresReboot));
            }
        };
    }

    public static TweakDefinition CreateServiceTweak(
        string id,
        string title,
        string description,
        string howItWorks,
        string problemSolved,
        string expectedResult,
        TweakCategory category,
        RiskLevel risk,
        RecommendationLevel recommendation,
        string serviceName,
        int targetStartType = 4, // 4 = Disabled, 3 = Manual
        bool stopServiceImmediately = true,
        bool requiresReboot = false,
        string[]? conflictsWith = null)
    {
        return new TweakDefinition
        {
            Id = id,
            Title = title,
            Description = description,
            HowItWorks = howItWorks,
            ProblemSolved = problemSolved,
            ExpectedResult = expectedResult,
            Category = category,
            Risk = risk,
            Recommendation = recommendation,
            RequiresReboot = requiresReboot,
            ConflictsWith = conflictsWith ?? Array.Empty<string>(),

            CheckCurrentStateAsync = () =>
            {
                var current = ServiceOperations.GetServiceStartType(serviceName);
                if (current.HasValue && current.Value == targetStartType)
                {
                    return Task.FromResult(TweakStatus.Applied);
                }
                return Task.FromResult(TweakStatus.NotApplied);
            },

            ApplyAsync = () =>
            {
                var snapshot = ServiceOperations.SnapshotService(serviceName);
                if (!snapshot.Exists)
                {
                    return Task.FromResult(TweakOperationResult.Fail($"Service '{serviceName}' does not exist on this Windows installation."));
                }

                bool ok = ServiceOperations.SetServiceStartType(serviceName, targetStartType);
                if (stopServiceImmediately)
                {
                    ServiceOperations.StopServiceIfRunning(serviceName);
                }

                if (ok)
                {
                    return Task.FromResult(TweakOperationResult.Ok(
                        $"Service '{serviceName}' set to {(targetStartType == 4 ? "Disabled" : "Manual")}.",
                        snapshot.ToJson(),
                        targetStartType.ToString(),
                        requiresReboot
                    ));
                }
                return Task.FromResult(TweakOperationResult.Fail($"Failed to modify service '{serviceName}'."));
            },

            RollbackAsync = (savedStateJson) =>
            {
                if (!string.IsNullOrEmpty(savedStateJson))
                {
                    var snapshot = ServiceStateSnapshot.FromJson(savedStateJson);
                    if (snapshot != null)
                    {
                        ServiceOperations.RestoreFromSnapshot(snapshot);
                        return Task.FromResult(TweakOperationResult.Ok($"Restored original configuration for service '{serviceName}'.", null, null, requiresReboot));
                    }
                }

                // Fallback: restore to Automatic (2) or Manual (3)
                ServiceOperations.SetServiceStartType(serviceName, 3);
                return Task.FromResult(TweakOperationResult.Ok($"Restored service '{serviceName}' to Manual.", null, null, requiresReboot));
            }
        };
    }

    public static TweakDefinition CreateRegistryString(
        string id,
        string title,
        string description,
        string howItWorks,
        string problemSolved,
        string expectedResult,
        TweakCategory category,
        RiskLevel risk,
        RecommendationLevel recommendation,
        string hive,
        string subKey,
        string valueName,
        string appliedValue,
        string? fallbackDefaultValue = null,
        bool requiresReboot = false,
        string[]? conflictsWith = null,
        bool requiresNvidiaGpu = false,
        bool requiresAmdGpu = false,
        bool requiresIntelGpu = false,
        bool requiresIntelCpu = false,
        bool requiresAmdCpu = false,
        bool requiresSsdOrNvme = false,
        bool desktopOnly = false,
        bool laptopOnly = false,
        int minWindowsBuild = 0,
        Func<HardwareProfile, CompatibilityResult>? customCompatibility = null)
    {
        return new TweakDefinition
        {
            Id = id,
            Title = title,
            Description = description,
            HowItWorks = howItWorks,
            ProblemSolved = problemSolved,
            ExpectedResult = expectedResult,
            Category = category,
            Risk = risk,
            Recommendation = recommendation,
            RequiresReboot = requiresReboot,
            ConflictsWith = conflictsWith ?? Array.Empty<string>(),
            RequiresNvidiaGpu = requiresNvidiaGpu,
            RequiresAmdGpu = requiresAmdGpu,
            RequiresIntelGpu = requiresIntelGpu,
            RequiresIntelCpu = requiresIntelCpu,
            RequiresAmdCpu = requiresAmdCpu,
            RequiresSsdOrNvme = requiresSsdOrNvme,
            DesktopOnly = desktopOnly,
            LaptopOnly = laptopOnly,
            MinWindowsBuild = minWindowsBuild,
            CustomCompatibilityCheck = customCompatibility,

            CheckCurrentStateAsync = () =>
            {
                var current = RegistryOperations.GetString(hive, subKey, valueName);
                if (current != null && string.Equals(current, appliedValue, StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(TweakStatus.Applied);
                }
                return Task.FromResult(TweakStatus.NotApplied);
            },

            ApplyAsync = () =>
            {
                var snapshot = RegistryOperations.SnapshotValue(hive, subKey, valueName);
                bool ok = RegistryOperations.SetString(hive, subKey, valueName, appliedValue);
                if (ok)
                {
                    return Task.FromResult(TweakOperationResult.Ok(
                        $"Successfully applied {title}.",
                        snapshot.ToJson(),
                        appliedValue,
                        requiresReboot
                    ));
                }
                return Task.FromResult(TweakOperationResult.Fail($"Failed to set registry value {subKey}\\{valueName}. Ensure SilkThread runs with Administrator rights."));
            },

            RollbackAsync = (savedStateJson) =>
            {
                if (!string.IsNullOrEmpty(savedStateJson))
                {
                    var snapshot = RegistryValueSnapshot.FromJson(savedStateJson);
                    if (snapshot != null)
                    {
                        bool restored = RegistryOperations.RestoreFromSnapshot(snapshot);
                        if (restored)
                        {
                            return Task.FromResult(TweakOperationResult.Ok($"Successfully restored original state for {title}.", null, null, requiresReboot));
                        }
                    }
                }

                if (fallbackDefaultValue != null)
                {
                    RegistryOperations.SetString(hive, subKey, valueName, fallbackDefaultValue);
                }
                else
                {
                    RegistryOperations.DeleteValue(hive, subKey, valueName);
                }

                return Task.FromResult(TweakOperationResult.Ok($"Reverted {title} to default.", null, null, requiresReboot));
            }
        };
    }

    public static TweakDefinition CreateServiceStartup(
        string id,
        string title,
        string description,
        string howItWorks,
        string problemSolved,
        string expectedResult,
        TweakCategory category,
        RiskLevel risk,
        RecommendationLevel recommendation,
        string serviceName,
        int targetStartType = 4, // 4 = Disabled, 3 = Manual, 2 = Automatic
        bool stopServiceImmediately = true,
        bool requiresReboot = false,
        string[]? conflictsWith = null)
        => CreateServiceTweak(id, title, description, howItWorks, problemSolved, expectedResult, category, risk, recommendation, serviceName, targetStartType, stopServiceImmediately, requiresReboot, conflictsWith);

    public static TweakDefinition CreateCommandAction(
        string id,
        string title,
        string description,
        string howItWorks,
        string problemSolved,
        string expectedResult,
        TweakCategory category,
        RiskLevel risk,
        RecommendationLevel recommendation,
        string applyFileName,
        string applyArguments,
        string rollbackFileName,
        string rollbackArguments,
        Func<Task<TweakStatus>>? checkStatus = null,
        string? checkFileName = null,
        string? checkArguments = null,
        string? checkExpectedOutputSubstring = null,
        bool requiresReboot = false,
        string[]? conflictsWith = null,
        bool requiresNvidiaGpu = false,
        bool requiresAmdGpu = false,
        bool requiresIntelGpu = false,
        bool requiresIntelCpu = false,
        bool requiresAmdCpu = false,
        bool requiresSsdOrNvme = false,
        bool desktopOnly = false,
        bool laptopOnly = false,
        int minWindowsBuild = 0)
    {
        return new TweakDefinition
        {
            Id = id,
            Title = title,
            Description = description,
            HowItWorks = howItWorks,
            ProblemSolved = problemSolved,
            ExpectedResult = expectedResult,
            Category = category,
            Risk = risk,
            Recommendation = recommendation,
            RequiresReboot = requiresReboot,
            ConflictsWith = conflictsWith ?? Array.Empty<string>(),
            RequiresNvidiaGpu = requiresNvidiaGpu,
            RequiresAmdGpu = requiresAmdGpu,
            RequiresIntelGpu = requiresIntelGpu,
            RequiresIntelCpu = requiresIntelCpu,
            RequiresAmdCpu = requiresAmdCpu,
            RequiresSsdOrNvme = requiresSsdOrNvme,
            DesktopOnly = desktopOnly,
            LaptopOnly = laptopOnly,
            MinWindowsBuild = minWindowsBuild,

            CheckCurrentStateAsync = async () =>
            {
                if (checkStatus != null)
                {
                    return await checkStatus();
                }

                if (!string.IsNullOrEmpty(checkFileName) && !string.IsNullOrEmpty(checkExpectedOutputSubstring))
                {
                    var res = await CommandOperations.RunAsync(checkFileName, checkArguments ?? string.Empty);
                    if (res.ExitCode == 0 && res.Output.Contains(checkExpectedOutputSubstring, StringComparison.OrdinalIgnoreCase))
                    {
                        return TweakStatus.Applied;
                    }
                    return TweakStatus.NotApplied;
                }

                return TweakStatus.RequiresVerification;
            },

            ApplyAsync = async () =>
            {
                var res = await CommandOperations.RunAsync(applyFileName, applyArguments);
                if (res.ExitCode == 0)
                {
                    return TweakOperationResult.Ok($"Successfully executed {title}.", null, res.Output, requiresReboot);
                }
                return TweakOperationResult.Fail($"Failed to execute {title}: {res.Error} (Code {res.ExitCode})");
            },

            RollbackAsync = async (savedStateJson) =>
            {
                var res = await CommandOperations.RunAsync(rollbackFileName, rollbackArguments);
                if (res.ExitCode == 0)
                {
                    return TweakOperationResult.Ok($"Successfully reverted {title}.", null, res.Output, requiresReboot);
                }
                return TweakOperationResult.Fail($"Failed to revert {title}: {res.Error} (Code {res.ExitCode})");
            }
        };
    }
}
