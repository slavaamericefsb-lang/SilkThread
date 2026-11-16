using SilkThread.Core.Models;
using Xunit;

namespace SilkThread.Tests;

public class TweakEngineRegressionTests
{
    [Fact]
    public async Task AuditBlockedTweak_CannotApplyButExistingSnapshotCanRollback()
    {
        await WithEngine(async (engine, backup, counters) =>
        {
            Assert.True((await engine.ApplyTweakAsync("test")).Success);
            engine.GetTweakById("test")!.AuditBlockReason = "Unsupported target identified in audit";
            var blocked = await engine.ApplyTweakAsync("test");
            Assert.False(blocked.Success);
            Assert.Equal("Unsupported target identified in audit", blocked.Message);
            Assert.Equal(1, counters.Applies);
            Assert.True((await engine.RollbackTweakAsync("test")).Success);
            Assert.Equal("original", counters.RolledBackState);
            Assert.Equal(1, counters.Rollbacks);
        });
    }

    [Fact]
    public async Task VerificationException_PreservesSuccessfulApplyAndRollbackSnapshot()
    {
        await WithEngine(async (engine, backup, counters) =>
        {
            var result = await engine.ApplyTweakAsync("test");
            Assert.True(result.Success);
            var record = await backup.GetLatestTweakRecordAsync("test");
            Assert.NotNull(record);
            Assert.False(record.VerificationPassed);
            Assert.Equal("original", record.OriginalStateJson);
            Assert.True((await engine.RollbackTweakAsync("test")).Success);
            Assert.Equal("original", counters.RolledBackState);
        });
    }

    [Fact]
    public async Task Batch_DeduplicatesCaseInsensitiveIdsForApplyAndRollback()
    {
        await WithEngine(async (engine, backup, counters) =>
        {
            Assert.Single(await engine.ApplyBatchAsync(new[] { "test", "TEST", "test" }));
            Assert.Equal(1, counters.Applies);
            Assert.Single(await engine.RollbackBatchAsync(new[] { "test", "TEST" }));
            Assert.Equal(1, counters.Rollbacks);
        });
    }

    private sealed class Counters
    {
        public int Applies;
        public int Rollbacks;
        public string? RolledBackState;
    }

    private static async Task WithEngine(Func<SilkThread.Core.TweakEngine.TweakEngine,
        SilkThread.Core.BackupManager.BackupManager, Counters, Task> test)
    {
        var dir = Path.Combine(Path.GetTempPath(), "SilkThread-audit-" + Guid.NewGuid());
        try
        {
            var backup = new SilkThread.Core.BackupManager.BackupManager(Path.Combine(dir, "test.db"));
            await backup.InitializeAsync();
            var counters = new Counters();
            var tweak = new TweakDefinition
            {
                Id = "test", Title = "Test", Description = "Test", HowItWorks = "Test",
                ProblemSolved = "Test", ExpectedResult = "Test", Category = TweakCategory.AdvancedTweaks,
                Risk = RiskLevel.Low, Recommendation = RecommendationLevel.Optional,
                CheckCurrentStateAsync = () => throw new InvalidOperationException("Verification unavailable"),
                ApplyAsync = () => { counters.Applies++; return Task.FromResult(TweakOperationResult.Ok("Applied", "original", "changed")); },
                RollbackAsync = state => { counters.Rollbacks++; counters.RolledBackState = state; return Task.FromResult(TweakOperationResult.Ok("Restored")); }
            };
            var engine = new SilkThread.Core.TweakEngine.TweakEngine(new[] { tweak }, backup,
                new SilkThread.Core.RestorePointManager.RestorePointManager(),
                new SilkThread.Core.CompatibilityEngine.CompatibilityEngine());
            await test(engine, backup, counters);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }
}
