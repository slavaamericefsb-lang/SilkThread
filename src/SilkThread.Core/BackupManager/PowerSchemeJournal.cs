using Microsoft.Data.Sqlite;
using SilkThread.Core.SystemOperations;
using System.Text.Json;

namespace SilkThread.Core.BackupManager;

public partial class BackupManager
{
    public Task<PowerSchemeChange?> GetOpenPowerSchemeChangeAsync()
        => ReadPowerSchemeChangeAsync(null);

    public Task<PowerSchemeChange?> GetPowerSchemeChangeAsync(int historyId)
        => ReadPowerSchemeChangeAsync(historyId);

    private async Task<PowerSchemeChange?> ReadPowerSchemeChangeAsync(int? historyId)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT HistoryId, OriginalId, TargetId, State, ObservedId, Detail FROM PowerSchemeChanges WHERE "
            + (historyId.HasValue ? "HistoryId = @Id" : "State <> 'Restored'");
        if (historyId.HasValue) cmd.Parameters.AddWithValue("@Id", historyId.Value);
        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return new PowerSchemeChange(reader.GetInt32(0), Guid.Parse(reader.GetString(1)), Guid.Parse(reader.GetString(2)),
            Enum.Parse<PowerSchemeChangeState>(reader.GetString(3)), reader.IsDBNull(4) ? null : Guid.Parse(reader.GetString(4)),
            reader.IsDBNull(5) ? null : reader.GetString(5));
    }

    public async Task<PowerSchemeChange> PreparePowerSchemeChangeAsync(Guid originalId, Guid targetId)
    {
        if (originalId == Guid.Empty || targetId == Guid.Empty || originalId == targetId)
            throw new ArgumentException("Distinct nonempty power scheme GUIDs are required.");
        await _dbLock.WaitAsync();
        try
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            using (var sync = conn.CreateCommand())
            {
                sync.CommandText = "PRAGMA synchronous=FULL;";
                await sync.ExecuteNonQueryAsync();
            }
            using var tx = conn.BeginTransaction();
            using var history = conn.CreateCommand();
            history.Transaction = tx;
            history.CommandText = @"INSERT INTO TweakHistory
                (TweakId,TweakTitle,Category,AppliedAtUtc,OriginalStateJson,AppliedStateJson,Success,VerificationPassed,Notes)
                VALUES ('system_power_scheme','Power scheme','PowerManagement',@Time,@Original,@Target,0,0,'Prepared');
                SELECT last_insert_rowid();";
            history.Parameters.AddWithValue("@Time", DateTime.UtcNow.ToString("O"));
            history.Parameters.AddWithValue("@Original", JsonSerializer.Serialize(new { SchemeId = originalId }));
            history.Parameters.AddWithValue("@Target", JsonSerializer.Serialize(new { SchemeId = targetId }));
            int id = Convert.ToInt32(await history.ExecuteScalarAsync());
            using var snapshot = conn.CreateCommand();
            snapshot.Transaction = tx;
            snapshot.CommandText = "INSERT INTO PowerSchemeChanges(HistoryId,OriginalId,TargetId,State) VALUES(@Id,@Original,@Target,'Prepared');";
            snapshot.Parameters.AddWithValue("@Id", id);
            snapshot.Parameters.AddWithValue("@Original", originalId.ToString("D"));
            snapshot.Parameters.AddWithValue("@Target", targetId.ToString("D"));
            await snapshot.ExecuteNonQueryAsync();
            await tx.CommitAsync();
            return new PowerSchemeChange(id, originalId, targetId, PowerSchemeChangeState.Prepared, null, null);
        }
        finally { _dbLock.Release(); }
    }

    public async Task UpdatePowerSchemeChangeAsync(int historyId, PowerSchemeChangeState state, Guid? observedId, string? detail)
    {
        if (!Enum.IsDefined(state)) throw new ArgumentOutOfRangeException(nameof(state));
        await _dbLock.WaitAsync();
        try
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            using var tx = conn.BeginTransaction();
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"UPDATE PowerSchemeChanges SET State=@State,ObservedId=@Observed,Detail=@Detail WHERE HistoryId=@Id;
                UPDATE TweakHistory SET Success=@Success,VerificationPassed=@Success,IsRolledBack=@Restored,
                RolledBackAtUtc=@Time,Notes=@Detail WHERE Id=@Id;";
            cmd.Parameters.AddWithValue("@Id", historyId);
            cmd.Parameters.AddWithValue("@State", state.ToString());
            cmd.Parameters.AddWithValue("@Observed", (object?)observedId?.ToString("D") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Detail", (object?)detail ?? state.ToString());
            cmd.Parameters.AddWithValue("@Success", state == PowerSchemeChangeState.Applied ? 1 : 0);
            cmd.Parameters.AddWithValue("@Restored", state == PowerSchemeChangeState.Restored ? 1 : 0);
            cmd.Parameters.AddWithValue("@Time", state == PowerSchemeChangeState.Restored ? DateTime.UtcNow.ToString("O") : (object)DBNull.Value);
            if (await cmd.ExecuteNonQueryAsync() != 2) throw new InvalidOperationException("Power scheme journal entry was not found.");
            await tx.CommitAsync();
        }
        finally { _dbLock.Release(); }
    }
}
