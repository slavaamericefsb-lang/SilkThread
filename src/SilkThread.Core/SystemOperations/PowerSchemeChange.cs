namespace SilkThread.Core.SystemOperations;

public enum PowerSchemeChangeState { Prepared, Applied, Unverified, Restoring, Restored }
public sealed record PowerSchemeChange(int HistoryId, Guid OriginalId, Guid TargetId,
    PowerSchemeChangeState State, Guid? ObservedId, string? Detail);
public sealed record PowerSchemePreview(Guid OriginalId, Guid TargetId, string OriginalName, string TargetName);
public enum PowerSchemeOutcome { Applied, Restored, NoChange, Conflict, ReadFailed, NeedsRights, WriteFailed, JournalFailed, RecoveryRequired, NotFound }
public sealed record PowerSchemeResult(PowerSchemeOutcome Outcome, int? HistoryId = null, Guid? ObservedId = null,
    uint? NativeError = null, string? Detail = null);

public interface IPowerSchemeJournal
{
    Task<PowerSchemeChange?> GetOpenPowerSchemeChangeAsync();
    Task<PowerSchemeChange?> GetPowerSchemeChangeAsync(int historyId);
    Task<PowerSchemeChange> PreparePowerSchemeChangeAsync(Guid originalId, Guid targetId);
    Task UpdatePowerSchemeChangeAsync(int historyId, PowerSchemeChangeState state, Guid? observedId, string? detail);
}

public interface IPowerSchemeService
{
    Task<PowerSchemePreview> PreviewAsync(Guid targetId);
    Task<PowerSchemeChange?> GetPendingAsync();
    Task<PowerSchemeResult> ApplyAsync(PowerSchemePreview preview);
    Task<PowerSchemeResult> RestoreAsync(int historyId);
}
