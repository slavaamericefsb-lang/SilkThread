using SilkThread.Core.SystemOperations;

namespace SilkThread.Core.GameProfiles;

public class GameProfileService : IGameProfileService
{
    // All instances reload durable state inside the same gate before touching the registry.
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private readonly IGameProfileRegistry _registry;
    private readonly IGameProfileStateStore _store;
    private const string GpuPrefKey = @"Software\Microsoft\DirectX\UserGpuPreferences";
    private const string LayersKey = @"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers";
    private const string IfeoKeyPrefix = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options";
    private const string GameConfigStoreKey = @"System\GameConfigStore";

    public GameProfileService() : this(new WindowsGameProfileRegistry(), new FileGameProfileStateStore(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SilkThread", "game-profile-snapshots.json"))) { }
    public GameProfileService(IGameProfileRegistry registry, IGameProfileStateStore store)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }
    // Compatibility seams are fully isolated: no production registry access or persistence.
    public GameProfileService(Func<string, string, string, bool> deleteValue)
        : this(deleteValue, (h, k, n, v) => true, (h, k, n, v) => true) { }
    public GameProfileService(Func<string, string, string, bool> deleteValue,
        Func<string, string, string, string, bool> setString, Func<string, string, string, int, bool> setDword)
        : this(new DelegateGameProfileRegistry(deleteValue, setString, setDword), new MemoryGameProfileStateStore()) { }

    public IReadOnlyList<GameProfile> GetAllProfiles() => GameProfileCatalog.GetAllProfiles();
    public GameProfile? GetProfileById(string id) => GameProfileCatalog.GetProfileById(id);
    public bool IsProfileApplied(string id)
    {
        Gate.Wait();
        try { return WithProcessLock(() => _store.Load().Profiles.TryGetValue(id, out var snapshot) && snapshot.Applied); }
        catch { return false; }
        finally { Gate.Release(); }
    }

    public Task<bool> ApplyProfileAsync(string id) => RunLocked(() =>
    {
        var profile = GetProfileById(id);
        if (profile == null) return false;
        var state = _store.Load();
        if (state.Profiles.TryGetValue(profile.Id, out var existing)) return existing.Applied;
        // An interrupted transaction must be recovered before adding another shared owner.
        if (state.Profiles.Values.Any(value => !value.Applied)) return false;
        var snapshot = new GameProfileSnapshot();
        foreach (var exe in profile.ExecutableNames)
        {
            snapshot.Values.Add(Capture("HKCU", GpuPrefKey, exe));
            snapshot.Values.Add(Capture("HKCU", LayersKey, exe));
            var key = $@"{IfeoKeyPrefix}\{exe}\PerfOptions";
            snapshot.Values.Add(Capture("HKLM", key, "CpuPriorityClass"));
            snapshot.Values.Add(Capture("HKLM", key, "IoPriority"));
        }
        bool firstOwner = state.Profiles.Count == 0;
        if (firstOwner)
            state.SharedValues = SharedNames.Select(name => Capture("HKCU", GameConfigStoreKey, name)).ToList();
        state.Profiles.Add(profile.Id, snapshot);
        _store.Save(state); // No registry mutation until rollback data is durable.
        try
        {
            foreach (var exe in profile.ExecutableNames)
            {
                Require(_registry.SetString("HKCU", GpuPrefKey, exe, "GpuPreference=2;"));
                Require(_registry.SetString("HKCU", LayersKey, exe, "~ DISABLEDXMAXIMIZEDWINDOWEDMODE HIGHDPIAWARE"));
                var key = $@"{IfeoKeyPrefix}\{exe}\PerfOptions";
                Require(_registry.SetDword("HKLM", key, "CpuPriorityClass", 3));
                Require(_registry.SetDword("HKLM", key, "IoPriority", 3));
            }
            if (firstOwner)
                for (int i = 0; i < SharedNames.Length; i++)
                    Require(_registry.SetDword("HKCU", GameConfigStoreKey, SharedNames[i], i == 0 ? 2 : 1));
            snapshot.Applied = true;
            _store.Save(state);
            return true;
        }
        catch
        {
            snapshot.Applied = false;
            RestoreAndRemove(state, profile.Id);
            return false;
        }
    });

    public Task<bool> RevertProfileAsync(string id) => RunLocked(() =>
    {
        var profile = GetProfileById(id);
        if (profile == null) return false;
        var state = _store.Load();
        // Legacy profiles have no original values. Never guess or delete their settings.
        if (!state.Profiles.ContainsKey(profile.Id)) return false;
        return RestoreAndRemove(state, profile.Id);
    });

    private bool RestoreAndRemove(GameProfileState state, string id)
    {
        var snapshot = state.Profiles[id];
        snapshot.Applied = false;
        _store.Save(state);
        bool success = Restore(snapshot.Values);
        if (state.Profiles.Count == 1) success &= Restore(state.SharedValues);
        if (!success) return false; // Keep durable originals for a retry after failure/restart.
        state.Profiles.Remove(id);
        if (state.Profiles.Count == 0) state.SharedValues.Clear();
        _store.Save(state);
        return true;
    }
    private bool Restore(IEnumerable<RegistryValueSnapshot> snapshots)
    {
        bool success = true;
        foreach (var snapshot in snapshots)
        {
            try { success &= _registry.Restore(snapshot); }
            catch { success = false; }
        }
        return success;
    }
    private RegistryValueSnapshot Capture(string hive, string key, string name)
    {
        var snapshot = _registry.Capture(hive, key, name);
        if (!snapshot.ReadSucceeded) throw new IOException("Cannot capture original registry value.");
        return snapshot;
    }
    private static void Require(bool success)
    {
        if (!success) throw new IOException("Registry write failed.");
    }
    private static readonly string[] SharedNames = { "GameDVR_FSEBehaviorMode", "GameDVR_HonorUserFSEBehaviorMode", "GameDVR_DXGIHonorFSEWindowsCompatible" };
    private static bool WithProcessLock(Func<bool> operation)
    {
        using var mutex = new Mutex(false, @"Local\SilkThread.GameProfileRegistry");
        bool acquired = false;
        try
        {
            try { acquired = mutex.WaitOne(TimeSpan.FromSeconds(30)); }
            catch (AbandonedMutexException) { acquired = true; }
            return acquired && operation();
        }
        finally { if (acquired) mutex.ReleaseMutex(); }
    }
    private static async Task<bool> RunLocked(Func<bool> operation)
    {
        await Gate.WaitAsync().ConfigureAwait(false);
        try { return await Task.Run(() => WithProcessLock(operation)).ConfigureAwait(false); }
        catch { return false; }
        finally { Gate.Release(); }
    }
}
