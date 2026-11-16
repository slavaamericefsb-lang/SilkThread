using System.Text.Json;

namespace SilkThread.Core.SystemOperations;

public sealed record PageFilePlan(bool Automatic, PageFileSetting[] Files, bool AllowRemoval = false);
public sealed record PageFileSnapshot(bool Automatic, RegistryValueSnapshot PagingFiles, PageFileSetting[] Settings);
public sealed record PageFileReview(PageFilePlan Plan, string OriginalJson, string BootId, string[] Warnings);
public sealed record PageFileRestoreReview(int HistoryId, string CurrentJson, string OriginalJson, bool Conflict);
public sealed record PageFileResult(string Status, int? HistoryId = null, string? Detail = null);
public interface IPageFileService
{
    Task<PageFileReview> PreviewAsync(PageFilePlan plan);
    Task<PageFileResult> ApplyAsync(PageFileReview review);
    Task<PageFileRestoreReview?> PreviewRestoreAsync(int? historyId = null);
    Task<PageFileResult> RestoreAsync(PageFileRestoreReview review, bool acknowledgeConflict = false);
    Task<ConfigurationChange?> CheckPendingAsync();
}
internal interface IPageFilePlatform
{
    bool IsAdministrator { get; }
    Task<PageFileSnapshot> ReadAsync();
    Task<string> BootIdAsync();
    Task SetAutomaticAsync(bool value);
    Task WritePagingFilesAsync(RegistryValueSnapshot value);
}

public sealed class PageFileService : IPageFileService
{
    public const string Resource = "system_pagefile";
    private readonly IConfigurationJournal _journal;
    private readonly IMemoryConfigurationReader _reader;
    private readonly IPageFilePlatform _platform;
    public PageFileService(IConfigurationJournal journal, IMemoryConfigurationReader reader)
        : this(journal, reader, new WindowsPageFilePlatform()) { }
    internal PageFileService(IConfigurationJournal journal, IMemoryConfigurationReader reader, IPageFilePlatform platform)
        => (_journal, _reader, _platform) = (journal, reader, platform);

    public async Task<PageFileReview> PreviewAsync(PageFilePlan plan)
    {
        var memory = await _reader.ReadAsync();
        var original = await _platform.ReadAsync();
        ValidateSnapshot(original);
        Validate(plan, memory, original);
        return new(plan, JsonSerializer.Serialize(original), await _platform.BootIdAsync(),
            new[] { "CommitAndDump", "RestartRequired", "MaximumNotReserved" });
    }

    internal static void Validate(PageFilePlan plan, MemoryConfiguration memory, PageFileSnapshot original)
    {
        if (plan.Automatic) return;
        if (memory.Volumes.Value == null || memory.Counters.Value == null || memory.CrashDump.Value == null)
            throw new InvalidOperationException("MissingValidationData");
        if (plan.Files.Length == 0 && !plan.AllowRemoval) throw new InvalidOperationException("RemovalAcknowledgement");
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in plan.Files)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(file.Path, @"^[A-Za-z]:\\pagefile\.sys$", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                || !seen.Add(file.Path)) throw new InvalidOperationException("InvalidPath");
            bool system = file.InitialMb == 0 && file.MaximumMb == 0;
            // Deliberately supported application range; not an estimate of workload needs.
            if (!system && (file.InitialMb < 16 || file.MaximumMb < file.InitialMb || file.MaximumMb > 1_048_576))
                throw new InvalidOperationException("InvalidSize");
            var volume = memory.Volumes.Value.SingleOrDefault(v => string.Equals(v.Root.TrimEnd('\\'), file.Path[..2], StringComparison.OrdinalIgnoreCase));
            if (volume?.FreeBytes == null || volume.SizeBytes == null || volume.FreeBytes > volume.SizeBytes)
                throw new InvalidOperationException("VolumeUnavailable");
            if (!system && (ulong)file.MaximumMb * 1024 * 1024 > volume.SizeBytes)
                throw new InvalidOperationException("InvalidSize");
            var existing = memory.ActiveFiles.Value?.FirstOrDefault(f => string.Equals(f.Path, file.Path, StringComparison.OrdinalIgnoreCase));
            ulong growth = system ? 256UL * 1024 * 1024 : (ulong)Math.Max(0L, (long)file.InitialMb - (existing?.AllocatedMb ?? 0)) * 1024 * 1024;
            if (volume.FreeBytes < growth + 256UL * 1024 * 1024) throw new InvalidOperationException("InsufficientSpace");
        }
        if (original.Settings.Any(f => !seen.Contains(f.Path)) && !plan.AllowRemoval)
            throw new InvalidOperationException("RemovalAcknowledgement");
        // Refuse a known fixed capacity below the current commit. System-managed growth is not predicted.
        if (plan.Files.All(f => f.MaximumMb != 0) && memory.Counters.Value.CommitBytes > memory.Counters.Value.PhysicalTotalBytes + (ulong)plan.Files.Sum(f => (long)f.MaximumMb) * 1024UL * 1024)
            throw new InvalidOperationException("CommitCapacity");
    }

    public async Task<PageFileResult> ApplyAsync(PageFileReview review)
    {
        await MemoryOperationCoordinator.Gate.WaitAsync();
        int? id = null;
        try
        {
            if (!_platform.IsAdministrator) return new("NeedsRights");
            var pending = await _journal.GetOpenChangeAsync(Resource);
            if (pending != null) return new("RecoveryRequired", pending.HistoryId);
            var current = await _platform.ReadAsync();
            if (!Same(current, Parse(review.OriginalJson))) return new("Conflict");
            Validate(review.Plan, await _reader.ReadAsync(), current);
            if (Matches(current, review.Plan)) return new("NoChange");
            var change = await _journal.PrepareChangeAsync(Resource, "Page file configuration", "RamOptimization",
                review.OriginalJson, JsonSerializer.Serialize(review.Plan), await _platform.BootIdAsync());
            id = change.HistoryId;
            if (!Same(await _platform.ReadAsync(), current)) return new("Conflict", id);
            try
            {
                await _platform.SetAutomaticAsync(review.Plan.Automatic);
                if (!review.Plan.Automatic) await _platform.WritePagingFilesAsync(DesiredPagingFiles(review.Plan));
            }
            catch (Exception ex) { return await ObserveFailureAsync(id.Value, ex); }
            var after = await _platform.ReadAsync();
            string state = Matches(after, review.Plan) ? "SavedRestartRequired" : "Unverified";
            await _journal.UpdateChangeAsync(id.Value, state, JsonSerializer.Serialize(after), null);
            return new(state, id);
        }
        catch (Exception ex) { return new(id.HasValue ? "RecoveryRequired" : "Failed", id, ex.Message); }
        finally { MemoryOperationCoordinator.Gate.Release(); }
    }

    public async Task<PageFileRestoreReview?> PreviewRestoreAsync(int? historyId = null)
    {
        var change = historyId.HasValue ? await _journal.GetChangeAsync(historyId.Value) : await _journal.GetOpenChangeAsync(Resource);
        if (change == null || change.Resource != Resource || change.State == "Restored") return null;
        var current = await _platform.ReadAsync();
        bool known = Same(current, Parse(change.OriginalJson))
            || (change.ObservedJson != null && Same(current, Parse(change.ObservedJson)));
        return new(change.HistoryId, JsonSerializer.Serialize(current), change.OriginalJson, !known);
    }

    public async Task<PageFileResult> RestoreAsync(PageFileRestoreReview review, bool acknowledgeConflict = false)
    {
        await MemoryOperationCoordinator.Gate.WaitAsync();
        try
        {
            if (!_platform.IsAdministrator) return new("NeedsRights", review.HistoryId);
            var change = await _journal.GetChangeAsync(review.HistoryId);
            if (change == null || change.Resource != Resource || change.State == "Restored") return new("NotFound", review.HistoryId);
            var current = await _platform.ReadAsync();
            bool actualConflict = !Same(current, Parse(change.OriginalJson))
                && !(change.ObservedJson != null && Same(current, Parse(change.ObservedJson)));
            if (change.OriginalJson != review.OriginalJson || !Same(current, Parse(review.CurrentJson)) || (actualConflict && !acknowledgeConflict))
                return new("Conflict", review.HistoryId);
            var original = Parse(change.OriginalJson);
            await _journal.UpdateChangeAsync(change.HistoryId, "Restoring", JsonSerializer.Serialize(current), null);
            if (!Same(await _platform.ReadAsync(), current)) return new("Conflict", change.HistoryId);
            try
            {
                await _platform.SetAutomaticAsync(original.Automatic);
                await _platform.WritePagingFilesAsync(original.PagingFiles);
            }
            catch (Exception ex) { return await ObserveFailureAsync(change.HistoryId, ex); }
            var after = await _platform.ReadAsync();
            var state = Same(after, original) ? "RestoreSavedRestartRequired" : "Unverified";
            await _journal.UpdateChangeAsync(change.HistoryId, state, JsonSerializer.Serialize(after), null, await _platform.BootIdAsync());
            return new(state, change.HistoryId);
        }
        catch (Exception ex) { return new("RecoveryRequired", review.HistoryId, ex.Message); }
        finally { MemoryOperationCoordinator.Gate.Release(); }
    }

    public async Task<ConfigurationChange?> CheckPendingAsync()
    {
        await MemoryOperationCoordinator.Gate.WaitAsync();
        try
        {
            var change = await _journal.GetOpenChangeAsync(Resource);
            if (change == null || change.BootId == await _platform.BootIdAsync()) return change;
            bool restoring = change.State == "RestoreSavedRestartRequired";
            if (change.State is not ("SavedRestartRequired" or "RestoreSavedRestartRequired" or "Confirmed")) return change;
            var original = Parse(change.OriginalJson);
            var plan = restoring ? new PageFilePlan(original.Automatic, original.Settings) : JsonSerializer.Deserialize<PageFilePlan>(change.TargetJson)!;
            var current = await _platform.ReadAsync(); var memory = await _reader.ReadAsync();
            bool stored = restoring ? Same(current, original) : Matches(current, plan);
            if (stored && ActiveMatches(plan, memory.ActiveFiles.Value))
            {
                var state = restoring ? "Restored" : "Confirmed";
                await _journal.UpdateChangeAsync(change.HistoryId, state, JsonSerializer.Serialize(current), null);
                return restoring ? null : change with { State = state };
            }
            return change with { Detail = "PostBootVerificationPending" };
        }
        finally { MemoryOperationCoordinator.Gate.Release(); }
    }

    internal static bool ActiveMatches(PageFilePlan plan, IReadOnlyList<PageFileUsage>? active)
        => active != null && !active.Any(f => f.Temporary) && (plan.Automatic ? active.Count > 0
            : active.Count == plan.Files.Length && plan.Files.All(f => active.Any(a => string.Equals(a.Path, f.Path, StringComparison.OrdinalIgnoreCase)
                && (f.MaximumMb == 0 ? a.AllocatedMb > 0 : a.AllocatedMb >= f.InitialMb && a.AllocatedMb <= f.MaximumMb))));
    private async Task<PageFileResult> ObserveFailureAsync(int id, Exception ex)
    {
        string? observed = null;
        try { observed = JsonSerializer.Serialize(await _platform.ReadAsync()); } catch { }
        await _journal.UpdateChangeAsync(id, "Unverified", observed, ex.Message);
        return new("Unverified", id, ex.Message);
    }
    internal static RegistryValueSnapshot DesiredPagingFiles(PageFilePlan plan) => new()
    {
        Exists = true, Hive = "HKLM", SubKey = WindowsPageFilePlatform.MemoryKey, ValueName = "PagingFiles", ValueKind = "MultiString",
        RawValue = JsonSerializer.Serialize(plan.Files.OrderBy(f => f.Path, StringComparer.OrdinalIgnoreCase).Select(f => $"{f.Path} {f.InitialMb} {f.MaximumMb}").ToArray())
    };
    internal static bool Same(PageFileSnapshot a, PageFileSnapshot b) => a.Automatic == b.Automatic
        && a.PagingFiles.Exists == b.PagingFiles.Exists && a.PagingFiles.ValueKind == b.PagingFiles.ValueKind && a.PagingFiles.RawValue == b.PagingFiles.RawValue;
    internal static bool Matches(PageFileSnapshot snapshot, PageFilePlan plan) => snapshot.Automatic == plan.Automatic
        && (plan.Automatic || snapshot.PagingFiles.ValueKind == "MultiString" &&
            JsonSerializer.Deserialize<string[]>(snapshot.PagingFiles.RawValue ?? "[]")!.Order(StringComparer.OrdinalIgnoreCase)
                .SequenceEqual(JsonSerializer.Deserialize<string[]>(DesiredPagingFiles(plan).RawValue!)!.Order(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase));
    private static PageFileSnapshot Parse(string json)
    {
        var snapshot = JsonSerializer.Deserialize<PageFileSnapshot>(json) ?? throw new InvalidDataException("Invalid snapshot.");
        ValidateSnapshot(snapshot); return snapshot;
    }
    internal static void ValidateSnapshot(PageFileSnapshot snapshot)
    {
        var raw = snapshot.PagingFiles;
        if (raw == null || !raw.ReadSucceeded || raw.Hive != "HKLM" || raw.SubKey != WindowsPageFilePlatform.MemoryKey
            || raw.ValueName != "PagingFiles" || snapshot.Settings == null || raw.Exists && raw.ValueKind != "MultiString")
            throw new InvalidDataException("Invalid page-file snapshot.");
        if (raw.Exists) _ = RegistryOperations.DecodeSnapshotValue(Microsoft.Win32.RegistryValueKind.MultiString, raw.RawValue);
    }
}
