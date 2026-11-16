namespace SilkThread.Core.SystemOperations;

public sealed class PowerConfigurationReader : IPowerConfigurationReader
{
    private readonly IPowerReadApi _api;

    public PowerConfigurationReader() : this(new NativePowerReadApi()) { }
    internal PowerConfigurationReader(IPowerReadApi api) => _api = api;

    public Task<PowerSchemeCatalog> ReadSchemesAsync(CancellationToken cancellationToken = default)
        => Task.Run(() => ReadSchemes(cancellationToken), cancellationToken);

    public Task<PowerSettingValues> ReadSettingAsync(Guid schemeId, Guid subgroupId, Guid settingId,
        CancellationToken cancellationToken = default)
    {
        if (schemeId == Guid.Empty) throw new ArgumentException("An explicit scheme GUID is required.", nameof(schemeId));
        if (subgroupId == Guid.Empty) throw new ArgumentException("A subgroup GUID is required.", nameof(subgroupId));
        if (settingId == Guid.Empty) throw new ArgumentException("A setting GUID is required.", nameof(settingId));
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var acCode = _api.ReadValueIndex(schemeId, subgroupId, settingId, true, out var ac);
            cancellationToken.ThrowIfCancellationRequested();
            var dcCode = _api.ReadValueIndex(schemeId, subgroupId, settingId, false, out var dc);
            cancellationToken.ThrowIfCancellationRequested();
            return new PowerSettingValues(schemeId, subgroupId, settingId,
                acCode == 0 ? ac : null, dcCode == 0 ? dc : null,
                Error(acCode, "PowerReadACValueIndex"), Error(dcCode, "PowerReadDCValueIndex"), DateTimeOffset.UtcNow);
        }, cancellationToken);
    }

    private PowerSchemeCatalog ReadSchemes(CancellationToken cancellationToken)
    {
        var schemes = new List<PowerSchemeInfo>();
        var seen = new HashSet<Guid>();
        PowerReadError? enumerationError = null;
        // Bound the observation in case the catalog changes continuously during enumeration.
        for (uint index = 0; ; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (index == 4096)
            {
                enumerationError = Error(13, "PowerEnumerate");
                break;
            }
            var code = _api.EnumerateScheme(index, out var id);
            if (code == 259) break; // ERROR_NO_MORE_ITEMS: an empty catalog is valid.
            if (code != 0)
            {
                enumerationError = Error(code, "PowerEnumerate");
                break;
            }
            if (id == Guid.Empty || !seen.Add(id))
            {
                enumerationError = Error(13, "PowerEnumerate");
                break;
            }
            cancellationToken.ThrowIfCancellationRequested();
            var nameCode = _api.ReadSchemeName(id, out var name);
            if (nameCode == 0 && string.IsNullOrWhiteSpace(name)) nameCode = 13;
            schemes.Add(new PowerSchemeInfo(id, nameCode == 0 ? name : null, Error(nameCode, "PowerReadFriendlyName")));
        }

        cancellationToken.ThrowIfCancellationRequested();
        var activeCode = _api.ReadActiveScheme(out var active);
        if (activeCode == 0 && active == Guid.Empty) activeCode = 13;
        cancellationToken.ThrowIfCancellationRequested();
        return new PowerSchemeCatalog(schemes.AsReadOnly(), activeCode == 0 ? active : null,
            enumerationError, Error(activeCode, "PowerGetActiveScheme"), DateTimeOffset.UtcNow);
    }

    private static PowerReadError? Error(uint code, string operation) => code == 0 ? null : new(code switch
    {
        5 => PowerReadFailure.AccessDenied,
        2 or 3 => PowerReadFailure.NotFound,
        50 or 120 => PowerReadFailure.NotSupported,
        _ => PowerReadFailure.Failed
    }, code, operation);
}

// Only read operations are exposed to this service.
internal interface IPowerReadApi
{
    uint EnumerateScheme(uint index, out Guid schemeId);
    uint ReadActiveScheme(out Guid schemeId);
    uint ReadSchemeName(Guid schemeId, out string? name);
    uint ReadValueIndex(Guid schemeId, Guid subgroupId, Guid settingId, bool ac, out uint value);
}
