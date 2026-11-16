using SilkThread.Core.GameProfiles;
using SilkThread.Core.SystemOperations;
using Xunit;

namespace SilkThread.Tests;

public class GameProfileRollbackRegressionTests
{
    [Fact]
    public async Task RestartAndRepeatedApply_PreserveOriginals()
    {
        await WithStore(async (path, registry) =>
        {
            registry.Values["HKCU|Software\\Microsoft\\DirectX\\UserGpuPreferences|cs2.exe"] = "original";
            var first = Service(path, registry);
            Assert.True(await first.ApplyProfileAsync("cs2"));
            int writes = registry.Writes;
            var restarted = Service(path, registry);
            Assert.True(await restarted.ApplyProfileAsync("cs2"));
            Assert.Equal(writes, registry.Writes);
            Assert.True(await restarted.RevertProfileAsync("cs2"));
            Assert.Equal("original", registry.Values["HKCU|Software\\Microsoft\\DirectX\\UserGpuPreferences|cs2.exe"]);
            Assert.Single(registry.Values);
        });
    }

    [Fact]
    public async Task TwoProfiles_KeepSharedFlagsUntilLastRevert()
    {
        await WithStore(async (path, registry) =>
        {
            var service = Service(path, registry);
            Assert.True(await service.ApplyProfileAsync("cs2"));
            Assert.True(await service.ApplyProfileAsync("dota2"));
            Assert.True(await service.RevertProfileAsync("cs2"));
            Assert.Equal(3, registry.Values.Keys.Count(k => k.Contains("GameDVR_")));
            Assert.True(await Service(path, registry).RevertProfileAsync("dota2"));
            Assert.Empty(registry.Values);
        });
    }

    [Fact]
    public async Task ApplyFailure_RestoresOriginalsAndRemovesJournal()
    {
        await WithStore(async (path, registry) =>
        {
            registry.FailWriteAt = 3;
            Assert.False(await Service(path, registry).ApplyProfileAsync("cs2"));
            Assert.Empty(registry.Values);
            Assert.Empty(new FileGameProfileStateStore(path).Load().Profiles);
        });
    }

    [Fact]
    public async Task RollbackFailure_KeepsOriginalsForRetryAfterRestart()
    {
        await WithStore(async (path, registry) =>
        {
            var service = Service(path, registry);
            Assert.True(await service.ApplyProfileAsync("cs2"));
            registry.FailRestore = true;
            Assert.False(await service.RevertProfileAsync("cs2"));
            Assert.Single(new FileGameProfileStateStore(path).Load().Profiles);
            Assert.False(service.IsProfileApplied("cs2"));
            registry.FailRestore = false;
            Assert.True(await Service(path, registry).RevertProfileAsync("cs2"));
            Assert.Empty(registry.Values);
        });
    }

    [Fact]
    public async Task LegacyWithoutSnapshot_DoesNotDeleteAnything()
    {
        await WithStore(async (path, registry) =>
        {
            Assert.False(await Service(path, registry).RevertProfileAsync("cs2"));
            Assert.Equal(0, registry.Restores);
        });
    }

    [Fact]
    public async Task FailedCapture_PreventsEveryWrite()
    {
        await WithStore(async (path, registry) =>
        {
            registry.FailCapture = true;
            Assert.False(await Service(path, registry).ApplyProfileAsync("cs2"));
            Assert.Equal(0, registry.Writes);
        });
    }

    [Fact]
    public async Task FailedPersistence_PreventsEveryWrite()
    {
        var registry = new FakeRegistry();
        var service = new GameProfileService(registry, new FailingStore());
        Assert.False(await service.ApplyProfileAsync("cs2"));
        Assert.Equal(0, registry.Writes);
    }

    [Fact]
    public async Task ConcurrentApply_SerializesAndCapturesOnce()
    {
        await WithStore(async (path, registry) =>
        {
            Assert.All(await Task.WhenAll(Service(path, registry).ApplyProfileAsync("cs2"),
                Service(path, registry).ApplyProfileAsync("cs2")), result => Assert.True(result));
            Assert.Equal(7, registry.Writes);
            Assert.True(await Service(path, registry).RevertProfileAsync("cs2"));
        });
    }

    [Fact]
    public async Task FailedApplyAndRecovery_RetainDurableOriginalsUntilRetry()
    {
        await WithStore(async (path, registry) =>
        {
            registry.FailWriteAt = 3;
            registry.FailRestore = true;
            Assert.False(await Service(path, registry).ApplyProfileAsync("cs2"));
            Assert.Single(new FileGameProfileStateStore(path).Load().Profiles);
            Assert.False(await Service(path, registry).ApplyProfileAsync("dota2"));
            registry.FailRestore = false;
            Assert.True(await Service(path, registry).RevertProfileAsync("cs2"));
            Assert.Empty(registry.Values);
        });
    }

    private sealed class FailingStore : IGameProfileStateStore
    {
        public GameProfileState Load() => new();
        public void Save(GameProfileState state) => throw new IOException("Disk full");
    }
    private static GameProfileService Service(string path, FakeRegistry registry) => new(registry, new FileGameProfileStateStore(path));
    private static async Task WithStore(Func<string, FakeRegistry, Task> test)
    {
        string dir = Path.Combine(Path.GetTempPath(), "SilkThread-profile-" + Guid.NewGuid());
        try { await test(Path.Combine(dir, "state.json"), new FakeRegistry()); }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }
    private sealed class FakeRegistry : IGameProfileRegistry
    {
        public Dictionary<string, string> Values = new();
        public int Writes, Restores, FailWriteAt;
        public bool FailRestore, FailCapture;
        private static string Key(string h, string k, string n) => h + "|" + k + "|" + n;
        public RegistryValueSnapshot Capture(string h, string k, string n)
        {
            bool exists = Values.TryGetValue(Key(h, k, n), out var value);
            return new() { Hive = h, SubKey = k, ValueName = n, Exists = exists,
                RawValue = value, ValueKind = "String", ReadSucceeded = !FailCapture };
        }
        public bool Restore(RegistryValueSnapshot snapshot)
        {
            Restores++;
            if (FailRestore) return false;
            string key = Key(snapshot.Hive, snapshot.SubKey, snapshot.ValueName);
            if (snapshot.Exists) Values[key] = snapshot.RawValue!;
            else Values.Remove(key);
            return true;
        }
        public bool SetString(string h, string k, string n, string value)
        {
            Writes++;
            if (Writes == FailWriteAt) return false;
            Values[Key(h, k, n)] = value;
            return true;
        }
        public bool SetDword(string h, string k, string n, int value) => SetString(h, k, n, value.ToString());
    }
}
