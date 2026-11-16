using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SilkThread.Core.SystemOperations;

public static class PowerOperations
{
    public const string UltimatePerformanceGuid = "e9a42b02-d5df-448d-aa00-03f14749eb61";
    public const string HighPerformanceGuid = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
    public const string BalancedGuid = "381b4222-f694-41f0-9685-ff5bb260df2e";

    private static readonly PowerCommandClient Client = new(
        arguments => CommandOperations.RunAsync("powercfg", arguments));

    /// <exception cref="InvalidOperationException">The active scheme could not be read.</exception>
    public static Task<(string Guid, string Name)> GetActiveSchemeAsync() => Client.GetActiveSchemeAsync();
    public static Task<bool> SetActiveSchemeAsync(string guid) => Client.SetActiveSchemeAsync(guid);
    public static Task<string?> DuplicateSchemeAsync(string sourceGuid) => Client.DuplicateSchemeAsync(sourceGuid);

    // False can also mean a partial write. This helper does not provide a snapshot or rollback.
    public static Task<bool> SetPowerValueIndexAsync(string subGroupGuid, string settingGuid, int acIndex, int? dcIndex = null)
        => Client.SetPowerValueIndexAsync(subGroupGuid, settingGuid, acIndex, dcIndex);
}

// Per-instance command boundary keeps tests isolated from powercfg and from one another.
internal sealed class PowerCommandClient(Func<string, Task<(int ExitCode, string Output, string Error)>> run)
{
    private const string GuidPattern = @"[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}";

    public async Task<(string Guid, string Name)> GetActiveSchemeAsync()
    {
        var result = await run("/getactivescheme");
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"Unable to read the active power scheme ({result.ExitCode}): {result.Error}");

        // GUID is invariant; the surrounding label and friendly name are localized.
        var match = Regex.Match(result.Output, $@"(?<guid>{GuidPattern})\s+\((?<name>[^\r\n]+)\)", RegexOptions.IgnoreCase);
        if (!match.Success)
            throw new InvalidOperationException("The active power scheme response did not contain a GUID and name.");

        return (System.Guid.Parse(match.Groups["guid"].Value).ToString("D"), match.Groups["name"].Value);
    }

    public async Task<bool> SetActiveSchemeAsync(string guid)
    {
        guid = NormalizeGuid(guid);
        var result = await run($"/setactive {guid}");
        if (result.ExitCode != 0) return false;

        try
        {
            var actual = await GetActiveSchemeAsync();
            return string.Equals(actual.Guid, guid, StringComparison.OrdinalIgnoreCase);
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public async Task<string?> DuplicateSchemeAsync(string sourceGuid)
    {
        sourceGuid = NormalizeGuid(sourceGuid);
        var result = await run($"-duplicatescheme {sourceGuid}");
        if (result.ExitCode != 0) return null;
        var match = Regex.Match(result.Output, GuidPattern, RegexOptions.IgnoreCase);
        return match.Success ? System.Guid.Parse(match.Value).ToString("D") : null;
    }

    public async Task<bool> SetPowerValueIndexAsync(string subGroupGuid, string settingGuid, int acIndex, int? dcIndex = null)
    {
        subGroupGuid = NormalizeGuid(subGroupGuid);
        settingGuid = NormalizeGuid(settingGuid);
        ArgumentOutOfRangeException.ThrowIfNegative(acIndex);
        if (dcIndex.HasValue) ArgumentOutOfRangeException.ThrowIfNegative(dcIndex.Value);

        (string Guid, string Name) active;
        try
        {
            active = await GetActiveSchemeAsync();
        }
        catch (InvalidOperationException)
        {
            // Never write to an assumed default scheme when discovery fails.
            return false;
        }

        var ac = await run($"/setacvalueindex {active.Guid} {subGroupGuid} {settingGuid} {acIndex}");
        if (ac.ExitCode != 0) return false;
        if (dcIndex.HasValue)
        {
            var dc = await run($"/setdcvalueindex {active.Guid} {subGroupGuid} {settingGuid} {dcIndex.Value}");
            if (dc.ExitCode != 0) return false;
        }

        // Detect an external plan switch before reactivation (not an atomic OS operation).
        try
        {
            var current = await GetActiveSchemeAsync();
            if (current.Guid != active.Guid) return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        return await SetActiveSchemeAsync(active.Guid);
    }

    private static string NormalizeGuid(string value)
    {
        if (!System.Guid.TryParse(value, out var guid))
            throw new ArgumentException("A power scheme or setting identifier must be a GUID.", nameof(value));
        return guid.ToString("D");
    }
}
