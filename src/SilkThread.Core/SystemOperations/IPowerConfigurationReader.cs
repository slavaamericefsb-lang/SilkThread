namespace SilkThread.Core.SystemOperations;

public enum PowerReadFailure
{
    AccessDenied,
    NotFound,
    NotSupported,
    Failed
}

public sealed record PowerReadError(PowerReadFailure Kind, uint NativeCode, string Operation);

public sealed record PowerSchemeInfo(Guid Id, string? Name, PowerReadError? NameError)
{
    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? Id.ToString("D") : Name;
}

// Observations are sequential, not an atomic snapshot or proof of permission to write.
public sealed record PowerSchemeCatalog(
    IReadOnlyList<PowerSchemeInfo> Schemes,
    Guid? ActiveSchemeId,
    PowerReadError? EnumerationError,
    PowerReadError? ActiveSchemeError,
    DateTimeOffset CheckedAtUtc);

// Stored indices for an explicit scheme, not defaults or effective hardware behavior.
// Null means unavailable; zero is a valid value. AC and DC may fail independently.
public sealed record PowerSettingValues(
    Guid SchemeId,
    Guid SubgroupId,
    Guid SettingId,
    uint? AcIndex,
    uint? DcIndex,
    PowerReadError? AcError,
    PowerReadError? DcError,
    DateTimeOffset CheckedAtUtc)
{
    public bool IsComplete => AcIndex.HasValue && DcIndex.HasValue && AcError is null && DcError is null;
}

public interface IPowerConfigurationReader
{
    Task<PowerSchemeCatalog> ReadSchemesAsync(CancellationToken cancellationToken = default);
    Task<PowerSettingValues> ReadSettingAsync(Guid schemeId, Guid subgroupId, Guid settingId,
        CancellationToken cancellationToken = default);
}
