using System.Text.Json;
using Microsoft.Data.Sqlite;
using SilkThread.Core.BackupManager;
using SilkThread.Core.SystemOperations;
using SilkThread.Core.Models;
using Xunit;

namespace SilkThread.Tests;

public class PageFileServiceTests
{
    private const ulong GiB = 1073741824;
    private static readonly PageFilePlan OriginalPlan = new(false, new[] { new PageFileSetting(@"C:\pagefile.sys", 1024, 2048) });
    private static readonly PageFilePlan NewPlan = new(false, new[] { new PageFileSetting(@"C:\pagefile.sys", 2048, 4096), new PageFileSetting(@"D:\pagefile.sys", 0, 0) });
    private static PageFileSnapshot Snapshot(PageFilePlan plan) => new(plan.Automatic, PageFileService.DesiredPagingFiles(plan), plan.Files);

    private sealed class Platform : IPageFilePlatform
    {
        public bool IsAdministrator { get; set; } = true;
        public PageFileSnapshot Current = Snapshot(OriginalPlan);
        public string Boot = "boot-1";
        public int Writes;
        public bool FailWrite;
        public bool LieAboutWrite;
        public Func<Task>? BeforeWrite;
        public Task<PageFileSnapshot> ReadAsync() => Task.FromResult(JsonSerializer.Deserialize<PageFileSnapshot>(JsonSerializer.Serialize(Current))!);
        public Task<string> BootIdAsync() => Task.FromResult(Boot);
        public async Task SetAutomaticAsync(bool value)
        {
            if (BeforeWrite != null) await BeforeWrite(); Writes++;
            Current = Current with { Automatic = value };
            // Simulate Windows changing the raw registry representation for automatic mode.
            if (value) Current = new(true, PageFileService.DesiredPagingFiles(new(true, Array.Empty<PageFileSetting>())), Array.Empty<PageFileSetting>());
        }
        public async Task WritePagingFilesAsync(RegistryValueSnapshot value)
        {
            if (BeforeWrite != null) await BeforeWrite(); Writes++;
            if (FailWrite) throw new IOException("partial write");
            if (LieAboutWrite) return;
            var entries = value.Exists ? JsonSerializer.Deserialize<string[]>(value.RawValue!)! : Array.Empty<string>();
            var files = entries.Select(entry =>
            {
                var split = entry.Split(' '); return new PageFileSetting(split[0], uint.Parse(split[1]), uint.Parse(split[2]));
            }).ToArray();
            Current = new(Current.Automatic, JsonSerializer.Deserialize<RegistryValueSnapshot>(value.ToJson())!, files);
        }
    }
    private sealed class Reader(Platform platform) : IMemoryConfigurationReader
    {
        public IReadOnlyList<PageFileUsage> Active = new[] { new PageFileUsage(@"C:\pagefile.sys", 1024, 10, 20, false) };
        public ulong? Free = 100 * GiB;
        public bool MissingDump;
        public Task<MemoryConfiguration> ReadAsync(CancellationToken token = default) => Task.FromResult(new MemoryConfiguration(
            new(new(8 * GiB, 4 * GiB, 3 * GiB, 12 * GiB, 5 * GiB), null), new(new(platform.Current.Automatic), null),
            new(platform.Current.Settings, null), new(Active, null),
            new(new[] { new PageFileVolume("C:", 500 * GiB, Free), new PageFileVolume("D:", 200 * GiB, Free) }, null),
            new(MissingDump ? null : new(7, null, null, null, null), MissingDump ? new(MemoryReadFailure.Failed, 1, "dump") : null),
            new(DumpRequirementKind.WorkloadDependent, null), PageFilePathComparison.Unknown, DateTimeOffset.UtcNow));
    }
    private sealed class Journal(IConfigurationJournal inner) : IConfigurationJournal
    {
        public bool FailPrepare, FailUpdate;
        public Action? AfterPrepare;
        public Task<ConfigurationChange?> GetOpenChangeAsync(string resource) => inner.GetOpenChangeAsync(resource);
        public Task<ConfigurationChange?> GetChangeAsync(int id) => inner.GetChangeAsync(id);
        public async Task<ConfigurationChange> PrepareChangeAsync(string resource, string title, string category, string original, string target, string boot)
        {
            if (FailPrepare) throw new IOException("snapshot failed");
            var result = await inner.PrepareChangeAsync(resource, title, category, original, target, boot); AfterPrepare?.Invoke(); return result;
        }
        public Task UpdateChangeAsync(int id, string state, string? observed, string? detail, string? boot = null)
            => FailUpdate ? throw new IOException("journal failed") : inner.UpdateChangeAsync(id, state, observed, detail, boot);
    }
    private sealed class Fixture : IAsyncDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "SilkThread-PageTests-" + Guid.NewGuid().ToString("N"));
        public string PathToDb => Path.Combine(_dir, "test.db");
        public BackupManager Backup { get; }
        public Platform Platform { get; } = new();
        public Reader Reader { get; }
        public Journal Journal { get; }
        public PageFileService Service { get; }
        public Fixture()
        {
            Backup = new(PathToDb); Journal = new(Backup); Reader = new(Platform); Service = new(Journal, Reader, Platform);
            Platform.BeforeWrite = async () => Assert.NotNull(await Backup.GetOpenChangeAsync(PageFileService.Resource));
        }
        public async Task<PageFileResult> ApplyAsync(PageFilePlan? plan = null)
        {
            await Backup.InitializeAsync();
            return await Service.ApplyAsync(await Service.PreviewAsync(plan ?? NewPlan));
        }
        public ValueTask DisposeAsync()
        {
            using var conn = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = PathToDb, Mode = SqliteOpenMode.ReadWriteCreate, DefaultTimeout = 30, ForeignKeys = true }.ToString());
            SqliteConnection.ClearPool(conn); if (Directory.Exists(_dir)) Directory.Delete(_dir, true); return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task ApplySavesBeforeWriting_VerifiesStoredState_AndWaitsForRestart()
    {
        await using var f = new Fixture(); var original = JsonSerializer.Serialize(f.Platform.Current);
        var result = await f.ApplyAsync(); Assert.Equal("SavedRestartRequired", result.Status);
        var change = await f.Backup.GetOpenChangeAsync(PageFileService.Resource);
        Assert.Equal(original, change?.OriginalJson); Assert.Equal(2, f.Platform.Writes);
        Assert.Equal("SavedRestartRequired", Assert.Single(await f.Backup.GetAuditHistoryAsync()).Status);
        Assert.Equal("SavedRestartRequired", (await f.Service.CheckPendingAsync())?.State);
        f.Platform.Boot = "boot-2"; f.Reader.Active = new[] { new PageFileUsage(@"C:\pagefile.sys", 2048, 1, 5, false), new PageFileUsage(@"D:\pagefile.sys", 512, 0, 2, false) };
        Assert.Equal("Confirmed", (await f.Service.CheckPendingAsync())?.State);
    }

    [Fact]
    public async Task RestoreIsExactAndWaitsForItsOwnRestart()
    {
        await using var f = new Fixture(); var original = await f.Platform.ReadAsync(); await f.ApplyAsync();
        f.Platform.Boot = "boot-2";
        var review = (await f.Service.PreviewRestoreAsync())!; Assert.False(review.Conflict);
        Assert.Equal("RestoreSavedRestartRequired", (await f.Service.RestoreAsync(review)).Status);
        Assert.True(PageFileService.Same(original, f.Platform.Current));
        Assert.Equal("RestoreSavedRestartRequired", (await f.Service.CheckPendingAsync())?.State);
        f.Platform.Boot = "boot-3";
        Assert.Null(await f.Service.CheckPendingAsync());
        Assert.True(Assert.Single(await f.Backup.GetAuditHistoryAsync()).IsRolledBack);
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public async Task AutomaticAndManualModesRoundTripWithoutLosingRawSnapshot(bool originalAuto)
    {
        await using var f = new Fixture(); f.Platform.Current = f.Platform.Current with { Automatic = originalAuto };
        var original = await f.Platform.ReadAsync();
        var result = await f.ApplyAsync(originalAuto ? NewPlan : new PageFilePlan(true, Array.Empty<PageFileSetting>()));
        Assert.Equal("SavedRestartRequired", result.Status);
        await f.Service.RestoreAsync((await f.Service.PreviewRestoreAsync())!);
        Assert.True(PageFileService.Same(original, f.Platform.Current));
    }

    [Fact]
    public async Task OriginallyAbsentRegistryValueIsRestoredAsAbsent()
    {
        await using var f = new Fixture(); var raw = f.Platform.Current.PagingFiles;
        raw.Exists = false; raw.ValueKind = null; raw.RawValue = null;
        f.Platform.Current = new(false, raw, Array.Empty<PageFileSetting>());
        await f.ApplyAsync(); await f.Service.RestoreAsync((await f.Service.PreviewRestoreAsync())!);
        Assert.False(f.Platform.Current.PagingFiles.Exists); Assert.Null(f.Platform.Current.PagingFiles.ValueKind);
    }

    [Fact]
    public async Task NonAdministratorNeverWritesOrCreatesAnAuditEntry()
    {
        await using var f = new Fixture(); f.Platform.IsAdministrator = false;
        Assert.Equal("NeedsRights", (await f.ApplyAsync()).Status); Assert.Equal(0, f.Platform.Writes);
        Assert.Empty(await f.Backup.GetAuditHistoryAsync());
    }

    [Fact]
    public async Task SnapshotFailureStopsBeforeFirstWrite()
    {
        await using var f = new Fixture(); f.Journal.FailPrepare = true;
        Assert.Equal("Failed", (await f.ApplyAsync()).Status); Assert.Equal(0, f.Platform.Writes);
    }

    [Fact]
    public async Task PartialWriteIsRetainedAndRecoverableAfterReopeningDatabase()
    {
        await using var f = new Fixture(); f.Platform.FailWrite = true;
        Assert.Equal("Unverified", (await f.ApplyAsync()).Status);
        f.Platform.FailWrite = false;
        var reopened = new BackupManager(f.PathToDb); var service = new PageFileService(reopened, f.Reader, f.Platform);
        Assert.NotNull(await reopened.GetOpenChangeAsync(PageFileService.Resource));
        Assert.Equal("RestoreSavedRestartRequired", (await service.RestoreAsync((await service.PreviewRestoreAsync())!)).Status);
        Assert.True(PageFileService.Same(Snapshot(OriginalPlan), f.Platform.Current));
    }

    [Fact]
    public async Task SuccessfulWriteWithoutMatchingReadbackIsNotApplied()
    {
        await using var f = new Fixture(); f.Platform.LieAboutWrite = true;
        Assert.Equal("Unverified", (await f.ApplyAsync()).Status);
        Assert.False(Assert.Single(await f.Backup.GetAuditHistoryAsync()).VerificationPassed);
    }

    [Fact]
    public async Task FinalJournalFailureLeavesDurablePreparedEntryForExplicitRecovery()
    {
        await using var f = new Fixture(); f.Journal.FailUpdate = true;
        Assert.Equal("RecoveryRequired", (await f.ApplyAsync()).Status);
        var reopened = new BackupManager(f.PathToDb); var service = new PageFileService(reopened, f.Reader, f.Platform);
        var review = (await service.PreviewRestoreAsync())!; Assert.True(review.Conflict);
        Assert.Equal("Conflict", (await service.RestoreAsync(review)).Status);
        Assert.Equal("RestoreSavedRestartRequired", (await service.RestoreAsync(review, true)).Status);
    }

    [Fact]
    public async Task StaleReviewAndExternalChangeDuringPrepareStopWrites()
    {
        await using var f = new Fixture(); var review = await f.Service.PreviewAsync(NewPlan);
        f.Platform.Current = f.Platform.Current with { Automatic = true };
        Assert.Equal("Conflict", (await f.Service.ApplyAsync(review)).Status); Assert.Equal(0, f.Platform.Writes);
        f.Platform.Current = Snapshot(OriginalPlan);
        f.Journal.AfterPrepare = () => f.Platform.Current = f.Platform.Current with { Automatic = true };
        Assert.Equal("Conflict", (await f.ApplyAsync()).Status); Assert.Equal(0, f.Platform.Writes);
        Assert.NotNull(await f.Backup.GetOpenChangeAsync(PageFileService.Resource));
    }

    [Fact]
    public async Task ExternalRestoreRequiresSeparateAcknowledgementAndFreshComparison()
    {
        await using var f = new Fixture(); await f.ApplyAsync();
        f.Platform.Current = Snapshot(new(false, new[] { new PageFileSetting(@"D:\pagefile.sys", 16, 64) }));
        var review = (await f.Service.PreviewRestoreAsync())!; Assert.True(review.Conflict);
        int writes = f.Platform.Writes;
        Assert.Equal("Conflict", (await f.Service.RestoreAsync(review)).Status); Assert.Equal(writes, f.Platform.Writes);
        f.Platform.Current = Snapshot(OriginalPlan);
        Assert.Equal("Conflict", (await f.Service.RestoreAsync(review, true)).Status); Assert.Equal(writes, f.Platform.Writes);
    }

    [Fact]
    public async Task RepeatApplyAndHistoryClearingCannotLoseOpenSnapshot()
    {
        await using var f = new Fixture(); await f.ApplyAsync();
        Assert.Equal("RecoveryRequired", (await f.ApplyAsync()).Status);
        await f.Backup.ClearHistoryAsync(); Assert.Single(await f.Backup.GetAuditHistoryAsync());
        Assert.NotNull(await f.Backup.GetOpenChangeAsync(PageFileService.Resource));
    }

    [Fact]
    public async Task MismatchedPostBootStateRemainsUnconfirmed()
    {
        await using var f = new Fixture(); await f.ApplyAsync(); f.Platform.Boot = "boot-2";
        var pending = await f.Service.CheckPendingAsync(); Assert.Equal("SavedRestartRequired", pending?.State);
        Assert.Equal("PostBootVerificationPending", pending?.Detail);
    }

    [Theory]
    [InlineData(0u, 16u)] [InlineData(15u, 16u)] [InlineData(32u, 16u)] [InlineData(16u, 1048577u)]
    public async Task InvalidSizeRangesNeverCreateSnapshot(uint initial, uint maximum)
    {
        await using var f = new Fixture();
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service.PreviewAsync(new(false, new[] { new PageFileSetting(@"C:\pagefile.sys", initial, maximum) })));
        Assert.Equal(0, f.Platform.Writes);
    }

    [Theory]
    [InlineData(@"C:\other.sys")] [InlineData(@"Z:\pagefile.sys")] [InlineData(@"\\server\share\pagefile.sys")]
    public async Task UnsupportedPathsAndVolumesAreRejected(string path)
    {
        await using var f = new Fixture();
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service.PreviewAsync(new(false, new[] { new PageFileSetting(path, 16, 64) }, true)));
    }

    [Fact]
    public async Task MissingFreeSpaceAndInsufficientSpaceDoNotBecomeSuccess()
    {
        await using var f = new Fixture(); f.Reader.Free = null;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service.PreviewAsync(NewPlan));
        f.Reader.Free = 1;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service.PreviewAsync(NewPlan));
        f.Reader.Free = 100 * GiB; f.Reader.MissingDump = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service.PreviewAsync(NewPlan));
    }

    [Fact]
    public async Task DisablingAllRequiresExplicitRiskAcknowledgement()
    {
        await using var f = new Fixture();
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service.PreviewAsync(new(false, Array.Empty<PageFileSetting>())));
        var review = await f.Service.PreviewAsync(new(false, Array.Empty<PageFileSetting>(), true));
        Assert.Contains("CommitAndDump", review.Warnings);
        Assert.Equal("SavedRestartRequired", (await f.Service.ApplyAsync(review)).Status);
    }

    [Fact]
    public async Task RamTweaksAreBlockedWhilePageFileChangeIsPending()
    {
        await using var f = new Fixture(); await f.ApplyAsync(); int writes = 0;
        var tweak = new TweakDefinition
        {
            Id = "ram-test", Title = "Test", Description = "Test", HowItWorks = "Test", ProblemSolved = "Test", ExpectedResult = "Test",
            Category = TweakCategory.RamOptimization, Risk = RiskLevel.Low, Recommendation = RecommendationLevel.Optional,
            CheckCurrentStateAsync = () => Task.FromResult(TweakStatus.NotApplied),
            ApplyAsync = () => { writes++; return Task.FromResult(TweakOperationResult.Ok("test")); },
            RollbackAsync = _ => { writes++; return Task.FromResult(TweakOperationResult.Ok("test")); }
        };
        var engine = new SilkThread.Core.TweakEngine.TweakEngine(new[] { tweak }, f.Backup,
            new SilkThread.Core.RestorePointManager.RestorePointManager(), new SilkThread.Core.CompatibilityEngine.CompatibilityEngine());
        Assert.False((await engine.ApplyTweakAsync(tweak.Id)).Success);
        Assert.False((await engine.RollbackTweakAsync(tweak.Id)).Success); Assert.Equal(0, writes);
    }

    [Fact]
    public async Task UnchangedConfigurationDoesNotCreateJournalOrWrite()
    {
        await using var f = new Fixture(); Assert.Equal("NoChange", (await f.ApplyAsync(OriginalPlan)).Status);
        Assert.Equal(0, f.Platform.Writes); Assert.Empty(await f.Backup.GetAuditHistoryAsync());
    }

    [Fact]
    public async Task ConcurrentApplyCannotReplaceFirstOriginalSnapshot()
    {
        await using var f = new Fixture(); var review = await f.Service.PreviewAsync(NewPlan);
        var results = await Task.WhenAll(f.Service.ApplyAsync(review), f.Service.ApplyAsync(review));
        Assert.Single(results, r => r.Status == "SavedRestartRequired");
        Assert.Single(results, r => r.Status == "RecoveryRequired");
        Assert.Equal(2, f.Platform.Writes); Assert.Single(await f.Backup.GetAuditHistoryAsync());
    }

    [Fact]
    public async Task InvalidStoredSnapshotIsRejectedBeforeRestorationWrites()
    {
        await using var f = new Fixture(); var result = await f.ApplyAsync(); var id = result.HistoryId!.Value;
        var change = (await f.Backup.GetChangeAsync(id))!;
        var original = JsonSerializer.Deserialize<PageFileSnapshot>(change.OriginalJson)!;
        original.PagingFiles.SubKey = "WrongKey";
        var invalid = JsonSerializer.Serialize(original);
        using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = f.PathToDb }.ToString()))
        {
            await connection.OpenAsync(); using var cmd = connection.CreateCommand();
            cmd.CommandText = "UPDATE ConfigurationChanges SET OriginalJson=@Json WHERE HistoryId=@Id;";
            cmd.Parameters.AddWithValue("@Json", invalid); cmd.Parameters.AddWithValue("@Id", id); await cmd.ExecuteNonQueryAsync();
            SqliteConnection.ClearPool(connection);
        }
        int before = f.Platform.Writes;
        Assert.Equal("RecoveryRequired", (await f.Service.RestoreAsync(new(id, JsonSerializer.Serialize(f.Platform.Current), invalid, false))).Status);
        Assert.Equal(before, f.Platform.Writes);
    }
}
