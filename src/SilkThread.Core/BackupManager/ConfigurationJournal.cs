using Microsoft.Data.Sqlite;
using SilkThread.Core.SystemOperations;

namespace SilkThread.Core.BackupManager;

public partial class BackupManager : IConfigurationJournal
{
    public Task<ConfigurationChange?> GetOpenChangeAsync(string resource) => ReadChangeAsync(resource, null);
    public Task<ConfigurationChange?> GetChangeAsync(int historyId) => ReadChangeAsync(null, historyId);
    private async Task<ConfigurationChange?> ReadChangeAsync(string? resource, int? id)
    {
        await InitializeAsync();
        using var conn = new SqliteConnection(_connectionString); await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT HistoryId,Resource,OriginalJson,TargetJson,ObservedJson,State,BootId,Detail FROM ConfigurationChanges WHERE "
            + (id.HasValue ? "HistoryId=@Id" : "Resource=@Resource AND State<>'Restored'");
        if (id.HasValue) cmd.Parameters.AddWithValue("@Id", id.Value); else cmd.Parameters.AddWithValue("@Resource", resource!);
        using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? new(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.IsDBNull(7) ? null : reader.GetString(7)) : null;
    }

    public async Task<ConfigurationChange> PrepareChangeAsync(string resource, string title, string category,
        string originalJson, string targetJson, string bootId)
    {
        await InitializeAsync(); await _dbLock.WaitAsync();
        try
        {
            using var conn = new SqliteConnection(_connectionString); await conn.OpenAsync();
            using (var sync = conn.CreateCommand()) { sync.CommandText = "PRAGMA synchronous=FULL;"; await sync.ExecuteNonQueryAsync(); }
            using var tx = conn.BeginTransaction();
            using var cmd = conn.CreateCommand(); cmd.Transaction = tx;
            cmd.CommandText = @"INSERT INTO TweakHistory(TweakId,TweakTitle,Category,AppliedAtUtc,OriginalStateJson,AppliedStateJson,Success,VerificationPassed,Notes)
                VALUES(@Resource,@Title,@Category,@Time,@Original,@Target,0,0,'Prepared'); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@Resource", resource); cmd.Parameters.AddWithValue("@Title", title);
            cmd.Parameters.AddWithValue("@Category", category); cmd.Parameters.AddWithValue("@Time", DateTime.UtcNow.ToString("O"));
            cmd.Parameters.AddWithValue("@Original", originalJson); cmd.Parameters.AddWithValue("@Target", targetJson);
            int id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            cmd.CommandText = @"INSERT INTO ConfigurationChanges(HistoryId,Resource,OriginalJson,TargetJson,State,BootId)
                VALUES(@Id,@Resource,@Original,@Target,'Prepared',@Boot);";
            cmd.Parameters.AddWithValue("@Id", id); cmd.Parameters.AddWithValue("@Boot", bootId);
            await cmd.ExecuteNonQueryAsync(); await tx.CommitAsync();
            return new(id, resource, originalJson, targetJson, null, "Prepared", bootId, null);
        }
        finally { _dbLock.Release(); }
    }

    public async Task UpdateChangeAsync(int historyId, string state, string? observedJson, string? detail, string? bootId = null)
    {
        if (state is not ("Prepared" or "SavedRestartRequired" or "RestoreSavedRestartRequired" or "Confirmed" or "Unverified" or "Restoring" or "Restored"))
            throw new ArgumentException("Unknown configuration state.", nameof(state));
        await _dbLock.WaitAsync();
        try
        {
            using var conn = new SqliteConnection(_connectionString); await conn.OpenAsync();
            using (var sync = conn.CreateCommand()) { sync.CommandText = "PRAGMA synchronous=FULL;"; await sync.ExecuteNonQueryAsync(); }
            using var tx = conn.BeginTransaction(); using var cmd = conn.CreateCommand(); cmd.Transaction = tx;
            cmd.CommandText = @"UPDATE ConfigurationChanges SET State=@State,ObservedJson=COALESCE(@Observed,ObservedJson),Detail=@Detail,BootId=COALESCE(@Boot,BootId) WHERE HistoryId=@Id;
                UPDATE TweakHistory SET Success=@Success,VerificationPassed=@Verified,IsRolledBack=@Restored,RolledBackAtUtc=@Time,Notes=@Detail WHERE Id=@Id;";
            cmd.Parameters.AddWithValue("@Id", historyId); cmd.Parameters.AddWithValue("@State", state);
            cmd.Parameters.AddWithValue("@Observed", (object?)observedJson ?? DBNull.Value); cmd.Parameters.AddWithValue("@Detail", (object?)detail ?? state);
            cmd.Parameters.AddWithValue("@Boot", (object?)bootId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Success", state is "SavedRestartRequired" or "Confirmed" ? 1 : 0);
            cmd.Parameters.AddWithValue("@Verified", state == "Confirmed" ? 1 : 0); cmd.Parameters.AddWithValue("@Restored", state == "Restored" ? 1 : 0);
            cmd.Parameters.AddWithValue("@Time", state == "Restored" ? DateTime.UtcNow.ToString("O") : (object)DBNull.Value);
            if (await cmd.ExecuteNonQueryAsync() != 2) throw new InvalidOperationException("Configuration journal entry is missing.");
            await tx.CommitAsync();
        }
        finally { _dbLock.Release(); }
    }
}
