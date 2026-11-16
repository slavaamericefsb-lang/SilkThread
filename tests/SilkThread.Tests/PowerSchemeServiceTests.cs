using Microsoft.Data.Sqlite;
using SilkThread.Core.BackupManager;
using SilkThread.Core.SystemOperations;
using Xunit;

namespace SilkThread.Tests;

public class PowerSchemeServiceTests
{
    private static readonly Guid Original = Guid.Parse(PowerOperations.BalancedGuid);
    private static readonly Guid Target = Guid.Parse(PowerOperations.HighPerformanceGuid);
    private static readonly Guid External = Guid.Parse(PowerOperations.UltimatePerformanceGuid);

    private sealed class Reader : IPowerConfigurationReader
    {
        public Guid Active = Original;
        public bool Fail;
        public HashSet<Guid> Plans = [Original, Target, External];
        public Task<PowerSchemeCatalog> ReadSchemesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PowerSchemeCatalog(Plans.Select(id => new PowerSchemeInfo(id, id.ToString(), null)).ToArray(),
                Fail ? null : Active, null, Fail ? new PowerReadError(PowerReadFailure.AccessDenied, 5, "read") : null, DateTimeOffset.UtcNow));
        public Task<PowerSettingValues> ReadSettingAsync(Guid schemeId, Guid subgroupId, Guid settingId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class Journal(IPowerSchemeJournal inner) : IPowerSchemeJournal
    {
        public bool FailPrepare;
        public bool FailUpdate;
        public Action? AfterPrepare;
        public Task<PowerSchemeChange?> GetOpenPowerSchemeChangeAsync() => inner.GetOpenPowerSchemeChangeAsync();
        public Task<PowerSchemeChange?> GetPowerSchemeChangeAsync(int id) => inner.GetPowerSchemeChangeAsync(id);
        public async Task<PowerSchemeChange> PreparePowerSchemeChangeAsync(Guid original, Guid target)
        {
            if (FailPrepare) throw new IOException("snapshot unavailable");
            var change = await inner.PreparePowerSchemeChangeAsync(original, target);
            AfterPrepare?.Invoke();
            return change;
        }
        public Task UpdatePowerSchemeChangeAsync(int id, PowerSchemeChangeState state, Guid? observed, string? detail)
            => FailUpdate ? throw new IOException("journal unavailable") : inner.UpdatePowerSchemeChangeAsync(id, state, observed, detail);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "SilkThread-PowerTests-" + Guid.NewGuid().ToString("N"));
        public string DbPath => Path.Combine(_directory, "test.db");
        public BackupManager Backup { get; }
        public Reader Reader { get; } = new();
        public Journal Journal { get; }
        public PowerSchemeService Service { get; }
        public List<Guid> Writes { get; } = [];
        public uint WriteCode;
        public bool Mutate = true;
        public bool FailVerification;
        public Fixture()
        {
            Backup = new BackupManager(DbPath);
            Journal = new Journal(Backup);
            Service = NewService(Journal);
        }
        public PowerSchemeService NewService(IPowerSchemeJournal journal) => new(Reader, journal, async id =>
        {
            var pending = await journal.GetOpenPowerSchemeChangeAsync();
            Assert.NotNull(pending); // Durable entry must exist BEFORE every native write.
            Assert.Equal(Original, pending.OriginalId);
            Writes.Add(id);
            if (Mutate) Reader.Active = id;
            if (FailVerification) Reader.Fail = true;
            return WriteCode;
        });
        public Task InitializeAsync() => Backup.InitializeAsync();
        public async Task<PowerSchemeResult> ApplyAsync() => await Service.ApplyAsync(await Service.PreviewAsync(Target));
        public ValueTask DisposeAsync()
        {
            using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = DbPath, Mode = SqliteOpenMode.ReadWriteCreate, DefaultTimeout = 30, ForeignKeys = true
            }.ToString());
            SqliteConnection.ClearPool(connection);
            Directory.Delete(_directory, true);
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task ApplyAndRestore_AreVerifiedAndAppearInCommonHistory()
    {
        await using var f = new Fixture(); await f.InitializeAsync();
        var applied = await f.ApplyAsync();
        Assert.Equal(PowerSchemeOutcome.Applied, applied.Outcome);
        Assert.Equal(Target, f.Reader.Active);
        var record = Assert.Single(await f.Backup.GetAuditHistoryAsync());
        Assert.Equal(applied.HistoryId, record.Id);
        Assert.True(record.Success); Assert.True(record.VerificationPassed);
        Assert.Contains(Original.ToString(), record.OriginalStateJson);
        Assert.Equal("Applied", record.Status);
        var restored = await f.Service.RestoreAsync(record.Id);
        Assert.Equal(PowerSchemeOutcome.Restored, restored.Outcome);
        Assert.Equal(Original, f.Reader.Active);
        Assert.True(Assert.Single(await f.Backup.GetAuditHistoryAsync()).IsRolledBack);
        Assert.Null(await f.Service.GetPendingAsync());
        Assert.Equal(new[] { Target, Original }, f.Writes);
    }

    [Fact]
    public async Task SnapshotFailure_StopsAllWrites()
    {
        await using var f = new Fixture(); await f.InitializeAsync(); f.Journal.FailPrepare = true;
        Assert.Equal(PowerSchemeOutcome.JournalFailed, (await f.ApplyAsync()).Outcome);
        Assert.Empty(f.Writes); Assert.Empty(await f.Backup.GetAuditHistoryAsync());
    }

    [Fact]
    public async Task StalePreview_StopsAllWrites()
    {
        await using var f = new Fixture(); await f.InitializeAsync();
        var preview = await f.Service.PreviewAsync(Target); f.Reader.Active = External;
        Assert.Equal(PowerSchemeOutcome.Conflict, (await f.Service.ApplyAsync(preview)).Outcome);
        Assert.Empty(f.Writes); Assert.Null(await f.Service.GetPendingAsync());
    }

    [Fact]
    public async Task ExternalSwitchDuringSnapshot_StopsWriteAndRetainsRecoveryEntry()
    {
        await using var f = new Fixture(); await f.InitializeAsync();
        f.Journal.AfterPrepare = () => f.Reader.Active = External;
        Assert.Equal(PowerSchemeOutcome.Conflict, (await f.ApplyAsync()).Outcome);
        Assert.Empty(f.Writes); Assert.NotNull(await f.Service.GetPendingAsync());
    }

    [Fact]
    public async Task ApplyingActiveScheme_IsNoOp()
    {
        await using var f = new Fixture(); await f.InitializeAsync();
        Assert.Equal(PowerSchemeOutcome.NoChange, (await f.Service.ApplyAsync(await f.Service.PreviewAsync(Original))).Outcome);
        Assert.Empty(f.Writes); Assert.Empty(await f.Backup.GetAuditHistoryAsync());
    }

    [Fact]
    public async Task RepeatAndConcurrentApply_DoNotReplaceOriginalSnapshot()
    {
        await using var f = new Fixture(); await f.InitializeAsync();
        var preview = await f.Service.PreviewAsync(Target);
        var results = await Task.WhenAll(f.Service.ApplyAsync(preview), f.NewService(f.Journal).ApplyAsync(preview));
        Assert.Single(results, r => r.Outcome == PowerSchemeOutcome.Applied);
        Assert.Single(results, r => r.Outcome == PowerSchemeOutcome.RecoveryRequired);
        Assert.Single(f.Writes); Assert.Single(await f.Backup.GetAuditHistoryAsync());
        Assert.Equal(Original, (await f.Service.GetPendingAsync())?.OriginalId);
    }

    [Theory]
    [InlineData(5u, PowerSchemeOutcome.NeedsRights)]
    [InlineData(87u, PowerSchemeOutcome.WriteFailed)]
    public async Task WriteFailure_IsNotReportedAsApplied(uint code, PowerSchemeOutcome outcome)
    {
        await using var f = new Fixture(); await f.InitializeAsync(); f.WriteCode = code; f.Mutate = false;
        var result = await f.ApplyAsync();
        Assert.Equal(outcome, result.Outcome); Assert.Equal(code, result.NativeError);
        Assert.Equal(PowerSchemeChangeState.Unverified, (await f.Service.GetPendingAsync())?.State);
        Assert.Equal("Unverified", Assert.Single(await f.Backup.GetAuditHistoryAsync()).Status);
        Assert.Equal(PowerSchemeOutcome.Restored, (await f.Service.RestoreAsync(result.HistoryId!.Value)).Outcome);
        Assert.Single(f.Writes); // Already at original: only close the journal.
    }

    [Fact]
    public async Task SuccessfulCommandWithWrongState_IsUnverified()
    {
        await using var f = new Fixture(); await f.InitializeAsync(); f.Mutate = false;
        Assert.Equal(PowerSchemeOutcome.WriteFailed, (await f.ApplyAsync()).Outcome);
        Assert.False(Assert.Single(await f.Backup.GetAuditHistoryAsync()).VerificationPassed);
    }

    [Fact]
    public async Task FailedVerification_RetainsSnapshotAndRestoresAfterRestart()
    {
        await using var f = new Fixture(); await f.InitializeAsync(); f.FailVerification = true;
        var result = await f.ApplyAsync(); Assert.Equal(PowerSchemeOutcome.WriteFailed, result.Outcome);
        f.FailVerification = false; f.Reader.Fail = false;
        var reopened = new BackupManager(f.DbPath); await reopened.InitializeAsync();
        var restarted = f.NewService(reopened);
        Assert.Equal(result.HistoryId, (await restarted.GetPendingAsync())?.HistoryId);
        Assert.Equal(PowerSchemeOutcome.Restored, (await restarted.RestoreAsync(result.HistoryId!.Value)).Outcome);
        Assert.Equal(Original, f.Reader.Active);
    }

    [Fact]
    public async Task FinalJournalFailure_RetainsPreparedSnapshotAfterSuccessfulWrite()
    {
        await using var f = new Fixture(); await f.InitializeAsync(); f.Journal.FailUpdate = true;
        var result = await f.ApplyAsync(); Assert.Equal(PowerSchemeOutcome.JournalFailed, result.Outcome);
        Assert.Equal(Target, f.Reader.Active);
        var reopened = new BackupManager(f.DbPath); await reopened.InitializeAsync();
        Assert.Equal(PowerSchemeChangeState.Prepared, (await reopened.GetOpenPowerSchemeChangeAsync())?.State);
        Assert.Equal(PowerSchemeOutcome.Restored, (await f.NewService(reopened).RestoreAsync(result.HistoryId!.Value)).Outcome);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CrashBeforeOrAfterWrite_RestoresFromPreparedEntry(bool wrote)
    {
        await using var f = new Fixture(); await f.InitializeAsync();
        var entry = await f.Backup.PreparePowerSchemeChangeAsync(Original, Target);
        if (wrote) f.Reader.Active = Target;
        var reopened = new BackupManager(f.DbPath); await reopened.InitializeAsync();
        Assert.Equal(PowerSchemeOutcome.Restored, (await f.NewService(reopened).RestoreAsync(entry.HistoryId)).Outcome);
        Assert.Equal(wrote ? 1 : 0, f.Writes.Count);
        Assert.Equal(Original, f.Reader.Active);
    }

    [Fact]
    public async Task Restore_RefusesExternalStateAndDeletedOriginal()
    {
        await using var f = new Fixture(); await f.InitializeAsync(); var applied = await f.ApplyAsync();
        f.Reader.Active = External;
        Assert.Equal(PowerSchemeOutcome.Conflict, (await f.Service.RestoreAsync(applied.HistoryId!.Value)).Outcome);
        f.Reader.Active = Target; f.Reader.Plans.Remove(Original);
        Assert.Equal(PowerSchemeOutcome.NotFound, (await f.Service.RestoreAsync(applied.HistoryId.Value)).Outcome);
        Assert.Single(f.Writes); Assert.NotNull(await f.Service.GetPendingAsync());
    }

    [Fact]
    public async Task RestoreJournalFailure_StopsWriteAndKeepsEntry()
    {
        await using var f = new Fixture(); await f.InitializeAsync(); var applied = await f.ApplyAsync();
        f.Journal.FailUpdate = true;
        Assert.Equal(PowerSchemeOutcome.JournalFailed, (await f.Service.RestoreAsync(applied.HistoryId!.Value)).Outcome);
        Assert.Single(f.Writes); Assert.NotNull(await f.Service.GetPendingAsync());
    }

    [Fact]
    public async Task RestoreWriteFailure_IsRetryableAndDoesNotMarkRollback()
    {
        await using var f = new Fixture(); await f.InitializeAsync(); var applied = await f.ApplyAsync();
        f.WriteCode = 5; f.Mutate = false;
        Assert.Equal(PowerSchemeOutcome.NeedsRights, (await f.Service.RestoreAsync(applied.HistoryId!.Value)).Outcome);
        Assert.False(Assert.Single(await f.Backup.GetAuditHistoryAsync()).IsRolledBack);
        f.WriteCode = 0; f.Mutate = true;
        Assert.Equal(PowerSchemeOutcome.Restored, (await f.Service.RestoreAsync(applied.HistoryId.Value)).Outcome);
    }

    [Fact]
    public async Task ClearingHistory_PreservesOpenSnapshotAndHistoryButAllowsClearingAfterRestore()
    {
        await using var f = new Fixture(); await f.InitializeAsync(); var applied = await f.ApplyAsync();
        await f.Backup.ClearHistoryAsync();
        Assert.Single(await f.Backup.GetAuditHistoryAsync()); Assert.NotNull(await f.Service.GetPendingAsync());
        await f.Service.RestoreAsync(applied.HistoryId!.Value); await f.Backup.ClearHistoryAsync();
        Assert.Empty(await f.Backup.GetAuditHistoryAsync());
        Assert.Null(await f.Backup.GetPowerSchemeChangeAsync(applied.HistoryId.Value));
    }

    [Fact]
    public async Task DatabaseRejectsSecondOpenEntryWithoutOrphanAuditRow()
    {
        await using var f = new Fixture(); await f.InitializeAsync();
        await f.Backup.PreparePowerSchemeChangeAsync(Original, Target);
        var second = new BackupManager(f.DbPath); await second.InitializeAsync();
        await Assert.ThrowsAsync<SqliteException>(() => second.PreparePowerSchemeChangeAsync(Original, External));
        Assert.Single(await f.Backup.GetAuditHistoryAsync());
    }

    [Fact]
    public async Task RestoredHistoricalEntry_NeverChangesLaterSession()
    {
        await using var f = new Fixture(); await f.InitializeAsync(); var first = await f.ApplyAsync();
        await f.Service.RestoreAsync(first.HistoryId!.Value);
        var second = await f.ApplyAsync();
        Assert.Equal(PowerSchemeOutcome.Restored, (await f.Service.RestoreAsync(first.HistoryId.Value)).Outcome);
        Assert.Equal(Target, f.Reader.Active); Assert.Equal(3, f.Writes.Count);
        Assert.Equal(second.HistoryId, (await f.Service.GetPendingAsync())?.HistoryId);
    }

    [Fact]
    public async Task ReadFailureAndMissingTarget_StopBeforeSnapshot()
    {
        await using var f = new Fixture(); await f.InitializeAsync();
        var preview = await f.Service.PreviewAsync(Target); f.Reader.Fail = true;
        Assert.Equal(PowerSchemeOutcome.ReadFailed, (await f.Service.ApplyAsync(preview)).Outcome);
        f.Reader.Fail = false; f.Reader.Plans.Remove(Target);
        Assert.Equal(PowerSchemeOutcome.ReadFailed, (await f.Service.ApplyAsync(preview)).Outcome);
        Assert.Empty(f.Writes); Assert.Empty(await f.Backup.GetAuditHistoryAsync());
    }

    [Fact]
    public async Task Initialize_UpgradesOldDatabaseWithoutLosingExistingHistory()
    {
        await using var f = new Fixture(); await f.InitializeAsync();
        await f.Backup.RecordAppliedTweakAsync("existing", "Existing tweak", "Cpu", "original", "applied", true, true);
        using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = f.DbPath }.ToString()))
        {
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = "DROP TABLE PowerSchemeChanges;";
            await command.ExecuteNonQueryAsync();
            SqliteConnection.ClearPool(connection);
        }
        var upgraded = new BackupManager(f.DbPath); await upgraded.InitializeAsync(); await upgraded.InitializeAsync();
        Assert.Equal("existing", Assert.Single(await upgraded.GetAuditHistoryAsync()).TweakId);
        var pending = await upgraded.PreparePowerSchemeChangeAsync(Original, Target);
        Assert.Equal(pending.HistoryId, (await upgraded.GetOpenPowerSchemeChangeAsync())?.HistoryId);
        Assert.Equal(2, (await upgraded.GetAuditHistoryAsync()).Count);
    }

    [Fact]
    public async Task HistoryRollback_RoutesPowerRecordToExactJournalEntry()
    {
        await using var f = new Fixture(); await f.InitializeAsync(); await f.ApplyAsync();
        var vm = new SilkThread.App.ViewModels.HistoryViewModel(f.Backup, null!,
            SilkThread.App.Services.LocalizationService.Instance, f.Service);
        await vm.RollbackRecordAsync(Assert.Single(await f.Backup.GetAuditHistoryAsync()));
        Assert.Equal(Original, f.Reader.Active);
        Assert.True(Assert.Single(vm.HistoryRecords).IsRolledBack);
        Assert.False(vm.IsLoading);
    }
}
