using System;
using System.Threading.Tasks;

namespace SilkThread.Core.Models;

public class TweakDefinition
{
    /// <summary>Blocked for new application after audit; retained for historical rollback.</summary>
    public string? AuditBlockReason { get; internal set; }
    public IReadOnlyList<RegistryWrite> RegistryWrites { get; init; } = Array.Empty<RegistryWrite>();
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string HowItWorks { get; init; }
    public required string ProblemSolved { get; init; }
    public required string ExpectedResult { get; init; }
    public required TweakCategory Category { get; init; }
    public required RiskLevel Risk { get; init; }
    public required RecommendationLevel Recommendation { get; init; }

    public bool RequiresReboot { get; init; }
    public string[] ConflictsWith { get; init; } = Array.Empty<string>();

    // Compatibility Flags
    public bool RequiresNvidiaGpu { get; init; }
    public bool RequiresAmdGpu { get; init; }
    public bool RequiresIntelGpu { get; init; }
    public bool RequiresIntelCpu { get; init; }
    public bool RequiresAmdCpu { get; init; }
    public bool RequiresSsdOrNvme { get; init; }
    public bool DesktopOnly { get; init; }
    public bool LaptopOnly { get; init; }
    public int MinWindowsBuild { get; init; }

    public Func<HardwareProfile, CompatibilityResult>? CustomCompatibilityCheck { get; init; }

    // Actions
    public required Func<Task<TweakStatus>> CheckCurrentStateAsync { get; init; }
    public required Func<Task<TweakOperationResult>> ApplyAsync { get; init; }
    public required Func<string?, Task<TweakOperationResult>> RollbackAsync { get; init; }
}

public record RegistryWrite(string Hive, string SubKey, string ValueName, object Value)
{
    public string Target => $"{Hive}\\{SubKey.TrimEnd('\\')}\\{ValueName}".ToUpperInvariant();
}
