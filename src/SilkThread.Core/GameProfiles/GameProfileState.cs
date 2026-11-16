using System.Text.Json;
using SilkThread.Core.SystemOperations;

namespace SilkThread.Core.GameProfiles;

public interface IGameProfileRegistry
{
    RegistryValueSnapshot Capture(string hive, string key, string name);
    bool Restore(RegistryValueSnapshot snapshot);
    bool SetString(string hive, string key, string name, string value);
    bool SetDword(string hive, string key, string name, int value);
}

public interface IGameProfileStateStore
{
    GameProfileState Load();
    void Save(GameProfileState state);
}

public sealed class GameProfileState
{
    public Dictionary<string, GameProfileSnapshot> Profiles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<RegistryValueSnapshot> SharedValues { get; set; } = new();
}

public sealed class GameProfileSnapshot
{
    public bool Applied { get; set; }
    public List<RegistryValueSnapshot> Values { get; set; } = new();
}

public sealed class FileGameProfileStateStore : IGameProfileStateStore
{
    private readonly string _path;
    public FileGameProfileStateStore(string path) => _path = path;
    public GameProfileState Load()
    {
        if (!File.Exists(_path)) return new();
        var state = JsonSerializer.Deserialize<GameProfileState>(File.ReadAllText(_path))
            ?? throw new InvalidDataException("Invalid game profile snapshot file.");
        state.Profiles = new(state.Profiles, StringComparer.OrdinalIgnoreCase);
        return state;
    }
    public void Save(GameProfileState state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path))!);
        var temp = _path + ".tmp";
        using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(stream, state);
            stream.Flush(flushToDisk: true);
        }
        File.Move(temp, _path, overwrite: true);
    }
}

internal sealed class WindowsGameProfileRegistry : IGameProfileRegistry
{
    public RegistryValueSnapshot Capture(string hive, string key, string name) => RegistryOperations.SnapshotValue(hive, key, name);
    public bool Restore(RegistryValueSnapshot snapshot) => RegistryOperations.RestoreFromSnapshot(snapshot);
    public bool SetString(string hive, string key, string name, string value) => RegistryOperations.SetString(hive, key, name, value);
    public bool SetDword(string hive, string key, string name, int value) => RegistryOperations.SetDword(hive, key, name, value);
}

internal sealed class MemoryGameProfileStateStore : IGameProfileStateStore
{
    private GameProfileState _state = new();
    public GameProfileState Load() => JsonSerializer.Deserialize<GameProfileState>(JsonSerializer.Serialize(_state))!;
    public void Save(GameProfileState state) => _state = JsonSerializer.Deserialize<GameProfileState>(JsonSerializer.Serialize(state))!;
}

internal sealed class DelegateGameProfileRegistry(
    Func<string, string, string, bool> delete,
    Func<string, string, string, string, bool> setString,
    Func<string, string, string, int, bool> setDword) : IGameProfileRegistry
{
    public RegistryValueSnapshot Capture(string hive, string key, string name) => new() { Hive = hive, SubKey = key, ValueName = name };
    public bool Restore(RegistryValueSnapshot snapshot) => delete(snapshot.Hive, snapshot.SubKey, snapshot.ValueName);
    public bool SetString(string hive, string key, string name, string value) => setString(hive, key, name, value);
    public bool SetDword(string hive, string key, string name, int value) => setDword(hive, key, name, value);
}
