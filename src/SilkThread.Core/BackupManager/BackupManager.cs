using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using SilkThread.Core.Models;

namespace SilkThread.Core.BackupManager;

public interface IBackupManager
{
    Task InitializeAsync();
    Task<int> RecordAppliedTweakAsync(string tweakId, string tweakTitle, string category, string? originalStateJson, string? appliedStateJson, bool success, bool verificationPassed, string? notes = null);
    Task<bool> MarkTweakRolledBackAsync(int historyId);
    Task<TweakAuditRecord?> GetLatestTweakRecordAsync(string tweakId);
    Task<List<TweakAuditRecord>> GetAuditHistoryAsync(int limit = 100);
    Task<bool> ClearHistoryAsync();
    Task<int> SaveSystemSnapshotAsync(string name, string jsonData);
    Task<string?> GetSystemSnapshotAsync(int snapshotId);
}

public partial class BackupManager : IBackupManager, SilkThread.Core.SystemOperations.IPowerSchemeJournal
{
    private readonly string _connectionString;
    private readonly string _dbPath;
    private readonly SemaphoreSlim _dbLock = new(1, 1);

    public BackupManager(string? customDbPath = null)
    {
        if (string.IsNullOrEmpty(customDbPath))
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dir = Path.Combine(appData, "SilkThread");
            Directory.CreateDirectory(dir);
            _dbPath = Path.Combine(dir, "silkthread.db");
        }
        else
        {
            _dbPath = customDbPath;
            var dir = Path.GetDirectoryName(_dbPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            DefaultTimeout = 30,
            ForeignKeys = true
        }.ToString();
    }

    public async Task InitializeAsync()
    {
        await _dbLock.WaitAsync();
        try
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            string pragmaSql = "PRAGMA journal_mode = WAL; PRAGMA synchronous = NORMAL;";
            using var pragmaCmd = new SqliteCommand(pragmaSql, conn);
            await pragmaCmd.ExecuteNonQueryAsync();

            string sql = @"
                CREATE TABLE IF NOT EXISTS TweakHistory (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TweakId TEXT NOT NULL,
                    TweakTitle TEXT NOT NULL,
                    Category TEXT NOT NULL,
                    AppliedAtUtc TEXT NOT NULL,
                    OriginalStateJson TEXT,
                    AppliedStateJson TEXT,
                    Success INTEGER NOT NULL,
                    VerificationPassed INTEGER NOT NULL,
                    IsRolledBack INTEGER NOT NULL DEFAULT 0,
                    RolledBackAtUtc TEXT,
                    Notes TEXT
                );

                CREATE TABLE IF NOT EXISTS SystemSnapshots (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    CreatedAtUtc TEXT NOT NULL,
                    SnapshotName TEXT NOT NULL,
                    SnapshotDataJson TEXT NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_tweak_history_id ON TweakHistory(TweakId);
                CREATE TABLE IF NOT EXISTS PowerSchemeChanges (
                    HistoryId INTEGER PRIMARY KEY REFERENCES TweakHistory(Id) ON DELETE CASCADE,
                    OriginalId TEXT NOT NULL,
                    TargetId TEXT NOT NULL,
                    State TEXT NOT NULL,
                    ObservedId TEXT,
                    Detail TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS idx_power_open ON PowerSchemeChanges((1)) WHERE State <> 'Restored';
                CREATE TABLE IF NOT EXISTS ConfigurationChanges (
                    HistoryId INTEGER PRIMARY KEY REFERENCES TweakHistory(Id) ON DELETE CASCADE,
                    Resource TEXT NOT NULL, OriginalJson TEXT NOT NULL, TargetJson TEXT NOT NULL,
                    ObservedJson TEXT, State TEXT NOT NULL, BootId TEXT NOT NULL, Detail TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS idx_configuration_open ON ConfigurationChanges(Resource) WHERE State <> 'Restored';
            ";

            using var cmd = new SqliteCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task<int> RecordAppliedTweakAsync(
        string tweakId,
        string tweakTitle,
        string category,
        string? originalStateJson,
        string? appliedStateJson,
        bool success,
        bool verificationPassed,
        string? notes = null)
    {
        await _dbLock.WaitAsync();
        try
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            string sql = @"
                INSERT INTO TweakHistory (
                    TweakId, TweakTitle, Category, AppliedAtUtc, 
                    OriginalStateJson, AppliedStateJson, Success, 
                    VerificationPassed, IsRolledBack, Notes
                )
                VALUES (
                    @TweakId, @TweakTitle, @Category, @AppliedAtUtc,
                    @OriginalStateJson, @AppliedStateJson, @Success,
                    @VerificationPassed, 0, @Notes
                );
                SELECT last_insert_rowid();
            ";

            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TweakId", tweakId);
            cmd.Parameters.AddWithValue("@TweakTitle", tweakTitle);
            cmd.Parameters.AddWithValue("@Category", category);
            cmd.Parameters.AddWithValue("@AppliedAtUtc", DateTime.UtcNow.ToString("O"));
            cmd.Parameters.AddWithValue("@OriginalStateJson", (object?)originalStateJson ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@AppliedStateJson", (object?)appliedStateJson ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Success", success ? 1 : 0);
            cmd.Parameters.AddWithValue("@VerificationPassed", verificationPassed ? 1 : 0);
            cmd.Parameters.AddWithValue("@Notes", (object?)notes ?? DBNull.Value);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task<bool> MarkTweakRolledBackAsync(int historyId)
    {
        await _dbLock.WaitAsync();
        try
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            string sql = @"
                UPDATE TweakHistory 
                SET IsRolledBack = 1, RolledBackAtUtc = @RolledBackAtUtc 
                WHERE Id = @Id;
            ";

            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", historyId);
            cmd.Parameters.AddWithValue("@RolledBackAtUtc", DateTime.UtcNow.ToString("O"));

            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task<TweakAuditRecord?> GetLatestTweakRecordAsync(string tweakId)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        string sql = @"
            SELECT Id, TweakId, TweakTitle, Category, AppliedAtUtc, 
                   OriginalStateJson, AppliedStateJson, Success, 
                   VerificationPassed, IsRolledBack, RolledBackAtUtc, Notes
            FROM TweakHistory
            WHERE TweakId = @TweakId AND IsRolledBack = 0 AND Success = 1
            ORDER BY Id DESC
            LIMIT 1;
        ";

        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@TweakId", tweakId);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new TweakAuditRecord
            {
                Id = reader.GetInt32(0),
                TweakId = reader.GetString(1),
                TweakTitle = reader.GetString(2),
                Category = reader.GetString(3),
                AppliedAtUtc = DateTime.Parse(reader.GetString(4), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                OriginalStateJson = reader.IsDBNull(5) ? null : reader.GetString(5),
                AppliedStateJson = reader.IsDBNull(6) ? null : reader.GetString(6),
                Success = reader.GetInt32(7) == 1,
                VerificationPassed = reader.GetInt32(8) == 1,
                IsRolledBack = reader.GetInt32(9) == 1,
                RolledBackAtUtc = reader.IsDBNull(10) ? null : DateTime.Parse(reader.GetString(10), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                Notes = reader.IsDBNull(11) ? null : reader.GetString(11)
            };
        }

        return null;
    }

    public async Task<List<TweakAuditRecord>> GetAuditHistoryAsync(int limit = 100)
    {
        var list = new List<TweakAuditRecord>();

        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        string sql = @"
            SELECT Id, TweakId, TweakTitle, Category, AppliedAtUtc, 
                   OriginalStateJson, AppliedStateJson, Success, 
                   VerificationPassed, IsRolledBack, RolledBackAtUtc, Notes,
                   COALESCE((SELECT State FROM PowerSchemeChanges WHERE HistoryId = TweakHistory.Id),
                       (SELECT State FROM ConfigurationChanges WHERE HistoryId = TweakHistory.Id))
            FROM TweakHistory
            ORDER BY Id DESC
            LIMIT @Limit;
        ";

        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Limit", limit);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new TweakAuditRecord
            {
                Id = reader.GetInt32(0),
                TweakId = reader.GetString(1),
                TweakTitle = reader.GetString(2),
                Category = reader.GetString(3),
                AppliedAtUtc = DateTime.Parse(reader.GetString(4), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                OriginalStateJson = reader.IsDBNull(5) ? null : reader.GetString(5),
                AppliedStateJson = reader.IsDBNull(6) ? null : reader.GetString(6),
                Success = reader.GetInt32(7) == 1,
                VerificationPassed = reader.GetInt32(8) == 1,
                IsRolledBack = reader.GetInt32(9) == 1,
                RolledBackAtUtc = reader.IsDBNull(10) ? null : DateTime.Parse(reader.GetString(10), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                Notes = reader.IsDBNull(11) ? null : reader.GetString(11),
                PowerSchemeState = reader.IsDBNull(12) ? null : reader.GetString(12)
            });
        }

        return list;
    }

    public async Task<bool> ClearHistoryAsync()
    {
        await _dbLock.WaitAsync();
        try
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            string sql = "DELETE FROM TweakHistory WHERE Id NOT IN (SELECT HistoryId FROM PowerSchemeChanges WHERE State <> 'Restored' UNION SELECT HistoryId FROM ConfigurationChanges WHERE State <> 'Restored');";
            using var cmd = new SqliteCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
            return true;
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task<int> SaveSystemSnapshotAsync(string name, string jsonData)
    {
        await _dbLock.WaitAsync();
        try
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            string sql = @"
                INSERT INTO SystemSnapshots (CreatedAtUtc, SnapshotName, SnapshotDataJson)
                VALUES (@CreatedAtUtc, @SnapshotName, @SnapshotDataJson);
                SELECT last_insert_rowid();
            ";

            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@CreatedAtUtc", DateTime.UtcNow.ToString("O"));
            cmd.Parameters.AddWithValue("@SnapshotName", name);
            cmd.Parameters.AddWithValue("@SnapshotDataJson", jsonData);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task<string?> GetSystemSnapshotAsync(int snapshotId)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        string sql = "SELECT SnapshotDataJson FROM SystemSnapshots WHERE Id = @Id;";
        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", snapshotId);

        var result = await cmd.ExecuteScalarAsync();
        return result?.ToString();
    }
}
