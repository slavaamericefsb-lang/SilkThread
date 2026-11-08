using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace SilkThread.Core.Models;

public record CompatibilityResult(
    bool IsCompatible,
    string Reason,
    bool RequiresVerification = false
)
{
    public static CompatibilityResult Compatible => new(true, "Fully compatible with current system hardware and OS.");
    public static CompatibilityResult Incompatible(string reason) => new(false, reason);
    public static CompatibilityResult VerificationNeeded(string reason) => new(true, reason, true);
}

public record TweakOperationResult(
    bool Success,
    string Message,
    string? OriginalStateJson = null,
    string? NewStateJson = null,
    bool RequiresReboot = false
)
{
    public static TweakOperationResult Ok(string message, string? originalStateJson = null, string? newStateJson = null, bool requiresReboot = false)
        => new(true, message, originalStateJson, newStateJson, requiresReboot);

    public static TweakOperationResult Fail(string error)
        => new(false, error);
}

public class TweakAuditRecord
{
    public int Id { get; set; }
    public string TweakId { get; set; } = string.Empty;
    public string TweakTitle { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime AppliedAtUtc { get; set; }
    public string? OriginalStateJson { get; set; }
    public string? AppliedStateJson { get; set; }
    public bool Success { get; set; }
    public bool VerificationPassed { get; set; }
    public bool IsRolledBack { get; set; }
    public DateTime? RolledBackAtUtc { get; set; }
    public string? Notes { get; set; }

    public string FormattedDate => AppliedAtUtc.ToLocalTime().ToString("dd.MM.yyyy");
    public string FormattedTime => AppliedAtUtc.ToLocalTime().ToString("HH:mm:ss");
    public string FormattedTimestamp => $"{FormattedDate} {FormattedTime}";
    public string TweakName => TweakTitle;
    public string Status => IsRolledBack ? "RolledBack" : (Success ? "Applied" : "Failed");
}

public record RestorePointItem(
    long SequenceNumber,
    string Description,
    DateTime CreatedAt,
    string RestorePointType
)
{
    public string FormattedDate => CreatedAt.ToLocalTime().ToString("dd.MM.yyyy");
    public string FormattedTime => CreatedAt.ToLocalTime().ToString("HH:mm:ss");
    public string FormattedCreationTime => $"{FormattedDate} {FormattedTime}";
    public string EventType => RestorePointType;
    public string EventTypeString => EventType;
}
