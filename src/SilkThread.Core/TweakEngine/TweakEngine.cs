using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SilkThread.Core.BackupManager;
using SilkThread.Core.CompatibilityEngine;
using SilkThread.Core.Models;
using SilkThread.Core.RestorePointManager;
using SilkThread.Core.SystemOperations;

namespace SilkThread.Core.TweakEngine;

public interface ITweakEngine
{
    IReadOnlyList<TweakDefinition> GetAllTweaks();
    TweakDefinition? GetTweakById(string id);
    Task<TweakStatus> CheckTweakStatusAsync(string tweakId);
    Task<TweakOperationResult> ApplyTweakAsync(string tweakId);
    Task<TweakOperationResult> RollbackTweakAsync(string tweakId);
    Task<List<TweakOperationResult>> ApplyBatchAsync(
        IEnumerable<string> tweakIds,
        bool createRestorePoint = false,
        IProgress<(int Current, int Total, string Status)>? progress = null);
    Task<List<TweakOperationResult>> RollbackBatchAsync(
        IEnumerable<string> tweakIds,
        IProgress<(int Current, int Total, string Status)>? progress = null);
}

public class TweakEngine : ITweakEngine
{
    private readonly SemaphoreSlim _statusWorkers = new(4, 4);
    private readonly IReadOnlyList<TweakDefinition> _tweaks;
    private readonly Dictionary<string, TweakDefinition> _tweakLookup;
    private readonly IBackupManager _backupManager;
    private readonly IRestorePointManager _restorePointManager;
    private readonly ICompatibilityEngine _compatibilityEngine;
    private readonly ILogger<TweakEngine>? _logger;

    public TweakEngine(
        IEnumerable<TweakDefinition> tweaks,
        IBackupManager backupManager,
        IRestorePointManager restorePointManager,
        ICompatibilityEngine compatibilityEngine,
        ILogger<TweakEngine>? logger = null)
    {
        _tweaks = tweaks.ToList();
        _tweakLookup = _tweaks.ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);
        _backupManager = backupManager;
        _restorePointManager = restorePointManager;
        _compatibilityEngine = compatibilityEngine;
        _logger = logger;
    }

    public IReadOnlyList<TweakDefinition> GetAllTweaks() => _tweaks;

    public TweakDefinition? GetTweakById(string id)
    {
        _tweakLookup.TryGetValue(id, out var tweak);
        return tweak;
    }

    public async Task<TweakStatus> CheckTweakStatusAsync(string tweakId)
    {
        var tweak = GetTweakById(tweakId);
        if (tweak == null) return TweakStatus.Unknown;

        await _statusWorkers.WaitAsync().ConfigureAwait(false);
        try
        {
            // Registry/service delegates often complete synchronously before their first await.
            // Never execute those reads on a caller's UI thread or start hundreds at once.
            return await Task.Run(tweak.CheckCurrentStateAsync).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to check status for tweak {TweakId}", tweakId);
            return TweakStatus.Failed;
        }
        finally { _statusWorkers.Release(); }
    }

    public async Task<TweakOperationResult> ApplyTweakAsync(string tweakId)
        => await WithMemoryGuardAsync(tweakId, () => ApplyTweakCoreAsync(tweakId));

    private async Task<TweakOperationResult> WithMemoryGuardAsync(string tweakId, Func<Task<TweakOperationResult>> operation)
    {
        if (GetTweakById(tweakId)?.Category != TweakCategory.RamOptimization) return await operation();
        await MemoryOperationCoordinator.Gate.WaitAsync();
        try
        {
            if (_backupManager is IConfigurationJournal journal && await journal.GetOpenChangeAsync(PageFileService.Resource) is { State: not "Confirmed" })
                return TweakOperationResult.Fail("Page-file configuration is awaiting recovery or restart verification. Finish it in System Settings first.");
            return await operation();
        }
        catch (Exception ex) { return TweakOperationResult.Fail(ex.Message); }
        finally { MemoryOperationCoordinator.Gate.Release(); }
    }

    private async Task<TweakOperationResult> ApplyTweakCoreAsync(string tweakId)
    {
        var tweak = GetTweakById(tweakId);
        if (tweak == null)
        {
            return TweakOperationResult.Fail($"Tweak '{tweakId}' not found in registry.");
        }
        if (!string.IsNullOrWhiteSpace(tweak.AuditBlockReason))
            return TweakOperationResult.Fail(tweak.AuditBlockReason);

        try
        {
            // Execute apply
            var result = await tweak.ApplyAsync();

            // Record in audit log database
            bool verified = false;
            if (result.Success)
            {
                try
                {
                    var newStatus = await tweak.CheckCurrentStateAsync();
                    verified = (newStatus == TweakStatus.Applied);
                }
                catch (Exception ex)
                {
                    // Verification failure must not discard the successful operation's rollback snapshot.
                    _logger?.LogWarning(ex, "Could not verify applied tweak {TweakId}", tweakId);
                }
            }

            await _backupManager.RecordAppliedTweakAsync(
                tweak.Id,
                tweak.Title,
                tweak.Category.ToString(),
                result.OriginalStateJson,
                result.NewStateJson,
                result.Success,
                verified,
                result.Message
            );

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error applying tweak {TweakId}", tweakId);
            try
            {
                await _backupManager.RecordAppliedTweakAsync(
                    tweak.Id,
                    tweak.Title,
                    tweak.Category.ToString(),
                    null,
                    null,
                    false,
                    false,
                    ex.Message
                );
            }
            catch { }
            return TweakOperationResult.Fail($"Exception while applying tweak: {ex.Message}");
        }
    }

    public async Task<TweakOperationResult> RollbackTweakAsync(string tweakId)
        => await WithMemoryGuardAsync(tweakId, () => RollbackTweakCoreAsync(tweakId));

    private async Task<TweakOperationResult> RollbackTweakCoreAsync(string tweakId)
    {
        var tweak = GetTweakById(tweakId);
        if (tweak == null)
        {
            return TweakOperationResult.Fail($"Tweak '{tweakId}' not found in registry.");
        }

        try
        {
            // Lookup original state from SQLite
            var auditRecord = await _backupManager.GetLatestTweakRecordAsync(tweakId);
            string? originalStateJson = auditRecord?.OriginalStateJson;

            // Execute rollback with saved state
            var result = await tweak.RollbackAsync(originalStateJson);

            if (result.Success && auditRecord != null)
            {
                await _backupManager.MarkTweakRolledBackAsync(auditRecord.Id);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error rolling back tweak {TweakId}", tweakId);
            return TweakOperationResult.Fail($"Exception while rolling back tweak: {ex.Message}");
        }
    }

    public async Task<List<TweakOperationResult>> ApplyBatchAsync(
        IEnumerable<string> tweakIds,
        bool createRestorePoint = false,
        IProgress<(int Current, int Total, string Status)>? progress = null)
    {
        var idList = tweakIds.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var results = new List<TweakOperationResult>();

        if (createRestorePoint)
        {
            progress?.Report((0, idList.Count, "Creating Windows System Restore point..."));
            try
            {
                await _restorePointManager.CreateRestorePointAsync("SilkThread Batch Optimization Point");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Could not create restore point before batch");
            }
        }

        for (int i = 0; i < idList.Count; i++)
        {
            var id = idList[i];
            var tweak = GetTweakById(id);
            string title = tweak?.Title ?? id;

            progress?.Report((i + 1, idList.Count, $"Applying: {title}"));
            var res = await ApplyTweakAsync(id);
            results.Add(res);
        }

        progress?.Report((idList.Count, idList.Count, "Optimization batch completed."));
        return results;
    }

    public async Task<List<TweakOperationResult>> RollbackBatchAsync(
        IEnumerable<string> tweakIds,
        IProgress<(int Current, int Total, string Status)>? progress = null)
    {
        var idList = tweakIds.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var results = new List<TweakOperationResult>();

        for (int i = 0; i < idList.Count; i++)
        {
            var id = idList[i];
            var tweak = GetTweakById(id);
            string title = tweak?.Title ?? id;

            progress?.Report((i + 1, idList.Count, $"Reverting: {title}"));
            var res = await RollbackTweakAsync(id);
            results.Add(res);
        }

        progress?.Report((idList.Count, idList.Count, "Rollback batch completed."));
        return results;
    }
}
