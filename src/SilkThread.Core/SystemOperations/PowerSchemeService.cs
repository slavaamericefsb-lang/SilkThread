using System.Runtime.InteropServices;

namespace SilkThread.Core.SystemOperations;

public sealed class PowerSchemeService : IPowerSchemeService
{
    // Shared across service instances: Windows has one active scheme for this user.
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private readonly IPowerConfigurationReader _reader;
    private readonly IPowerSchemeJournal _journal;
    private readonly Func<Guid, Task<uint>> _setActive;

    public PowerSchemeService(IPowerConfigurationReader reader, IPowerSchemeJournal journal)
        : this(reader, journal, id => Task.Run(() => PowerSetActiveScheme(IntPtr.Zero, ref id))) { }

    internal PowerSchemeService(IPowerConfigurationReader reader, IPowerSchemeJournal journal, Func<Guid, Task<uint>> setActive)
        => (_reader, _journal, _setActive) = (reader, journal, setActive);

    public Task<PowerSchemeChange?> GetPendingAsync() => _journal.GetOpenPowerSchemeChangeAsync();

    public async Task<PowerSchemePreview> PreviewAsync(Guid targetId)
    {
        if (targetId == Guid.Empty) throw new ArgumentException("An explicit target GUID is required.", nameof(targetId));
        var catalog = await ReadCatalogAsync();
        var target = catalog.Schemes.SingleOrDefault(s => s.Id == targetId)
            ?? throw new InvalidOperationException("The selected power scheme is no longer available.");
        var original = catalog.Schemes.SingleOrDefault(s => s.Id == catalog.ActiveSchemeId)
            ?? throw new InvalidOperationException("The active power scheme is missing from the catalog.");
        return new PowerSchemePreview(original.Id, target.Id, original.Name ?? original.Id.ToString(), target.Name ?? target.Id.ToString());
    }

    public async Task<PowerSchemeResult> ApplyAsync(PowerSchemePreview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);
        await Gate.WaitAsync();
        try
        {
            PowerSchemeChange? pending;
            try { pending = await GetPendingAsync(); }
            catch (Exception ex) { return new(PowerSchemeOutcome.JournalFailed, Detail: ex.Message); }
            if (pending != null) return new(PowerSchemeOutcome.RecoveryRequired, pending.HistoryId);

            PowerSchemePreview current;
            try { current = await PreviewAsync(preview.TargetId); }
            catch (Exception ex) { return new(PowerSchemeOutcome.ReadFailed, Detail: ex.Message); }
            if (current.OriginalId != preview.OriginalId) return new(PowerSchemeOutcome.Conflict, ObservedId: current.OriginalId);
            if (current.OriginalId == current.TargetId) return new(PowerSchemeOutcome.NoChange, ObservedId: current.OriginalId);

            PowerSchemeChange change;
            try { change = await _journal.PreparePowerSchemeChangeAsync(current.OriginalId, current.TargetId); }
            catch (Exception ex) { return new(PowerSchemeOutcome.JournalFailed, Detail: ex.Message); }
            return await ExecuteAsync(change, restoring: false);
        }
        finally { Gate.Release(); }
    }

    public async Task<PowerSchemeResult> RestoreAsync(int historyId)
    {
        await Gate.WaitAsync();
        try
        {
            PowerSchemeChange? change;
            try { change = await _journal.GetPowerSchemeChangeAsync(historyId); }
            catch (Exception ex) { return new(PowerSchemeOutcome.JournalFailed, historyId, Detail: ex.Message); }
            if (change == null) return new(PowerSchemeOutcome.NotFound, historyId);
            // An old completed history row must never change a subsequent session.
            if (change.State == PowerSchemeChangeState.Restored) return new(PowerSchemeOutcome.Restored, historyId);
            return await ExecuteAsync(change, restoring: true);
        }
        finally { Gate.Release(); }
    }

    private async Task<PowerSchemeResult> ExecuteAsync(PowerSchemeChange change, bool restoring)
    {
        PowerSchemeCatalog catalog;
        try { catalog = await ReadCatalogAsync(); }
        catch (Exception ex) { return new(PowerSchemeOutcome.ReadFailed, change.HistoryId, Detail: ex.Message); }
        Guid desired = restoring ? change.OriginalId : change.TargetId;
        if (restoring && catalog.ActiveSchemeId == desired)
            return await FinishAsync(change, PowerSchemeChangeState.Restored, new(PowerSchemeOutcome.Restored, change.HistoryId, desired));

        Guid expected = restoring ? change.TargetId : change.OriginalId;
        if (catalog.ActiveSchemeId != expected)
            return new(PowerSchemeOutcome.Conflict, change.HistoryId, catalog.ActiveSchemeId);
        if (!catalog.Schemes.Any(s => s.Id == desired))
            return new(PowerSchemeOutcome.NotFound, change.HistoryId, catalog.ActiveSchemeId);

        if (restoring)
        {
            try { await _journal.UpdatePowerSchemeChangeAsync(change.HistoryId, PowerSchemeChangeState.Restoring, catalog.ActiveSchemeId, null); }
            catch (Exception ex) { return new(PowerSchemeOutcome.JournalFailed, change.HistoryId, Detail: ex.Message); }
            // Detect a switch while the journal was being saved. Windows offers no compare-and-set here.
            try { catalog = await ReadCatalogAsync(); }
            catch (Exception ex) { return new(PowerSchemeOutcome.ReadFailed, change.HistoryId, Detail: ex.Message); }
            if (catalog.ActiveSchemeId != expected)
                return new(PowerSchemeOutcome.Conflict, change.HistoryId, catalog.ActiveSchemeId);
        }

        uint? code = null;
        string? detail = null;
        try { code = await _setActive(desired); }
        catch (Exception ex) { detail = ex.Message; }
        Guid? observed = null;
        try { observed = (await ReadCatalogAsync()).ActiveSchemeId; }
        catch (Exception ex) { detail = $"{detail} Verification: {ex.Message}".Trim(); }

        bool verified = code == 0 && observed == desired;
        var outcome = verified ? (restoring ? PowerSchemeOutcome.Restored : PowerSchemeOutcome.Applied)
            : code == 5 ? PowerSchemeOutcome.NeedsRights : PowerSchemeOutcome.WriteFailed;
        var state = verified ? (restoring ? PowerSchemeChangeState.Restored : PowerSchemeChangeState.Applied)
            : PowerSchemeChangeState.Unverified;
        return await FinishAsync(change, state, new(outcome, change.HistoryId, observed, code, detail));
    }

    private async Task<PowerSchemeResult> FinishAsync(PowerSchemeChange change, PowerSchemeChangeState state, PowerSchemeResult result)
    {
        try
        {
            await _journal.UpdatePowerSchemeChangeAsync(change.HistoryId, state, result.ObservedId,
                $"{result.Outcome}; Win32={result.NativeError}; {result.Detail}");
            return result;
        }
        catch (Exception ex)
        {
            // The pre-write entry remains discoverable after restart even if this final update fails.
            return new(PowerSchemeOutcome.JournalFailed, change.HistoryId, result.ObservedId, result.NativeError, ex.Message);
        }
    }

    private async Task<PowerSchemeCatalog> ReadCatalogAsync()
    {
        var catalog = await _reader.ReadSchemesAsync();
        if (catalog.EnumerationError != null || catalog.ActiveSchemeError != null || !catalog.ActiveSchemeId.HasValue)
            throw new InvalidOperationException($"Power configuration unavailable: {catalog.EnumerationError ?? catalog.ActiveSchemeError}");
        return catalog;
    }

    [DllImport("powrprof.dll", ExactSpelling = true)]
    private static extern uint PowerSetActiveScheme(IntPtr root, ref Guid scheme);
}
