namespace SilkThread.Core.SystemOperations;

public sealed record ConfigurationChange(int HistoryId, string Resource, string OriginalJson, string TargetJson,
    string? ObservedJson, string State, string BootId, string? Detail);
public interface IConfigurationJournal
{
    Task<ConfigurationChange?> GetOpenChangeAsync(string resource);
    Task<ConfigurationChange?> GetChangeAsync(int historyId);
    Task<ConfigurationChange> PrepareChangeAsync(string resource, string title, string category,
        string originalJson, string targetJson, string bootId);
    Task UpdateChangeAsync(int historyId, string state, string? observedJson, string? detail, string? bootId = null);
}

// All RAM tweaks and page-file changes share this in-process execution boundary.
public static class MemoryOperationCoordinator
{
    public static SemaphoreSlim Gate { get; } = new(1, 1);
}
