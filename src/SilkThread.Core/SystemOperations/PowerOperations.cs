using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SilkThread.Core.SystemOperations;

public static class PowerOperations
{
    public const string UltimatePerformanceGuid = "e9a42b02-d5df-448d-aa00-03f14749eb61";
    public const string HighPerformanceGuid = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
    public const string BalancedGuid = "381b4222-f694-41f0-9685-ff5bb260df2e";

    public static async Task<(string Guid, string Name)> GetActiveSchemeAsync()
    {
        var res = await CommandOperations.RunAsync("powercfg", "/getactivescheme");
        if (res.ExitCode == 0 && !string.IsNullOrWhiteSpace(res.Output))
        {
            // Output format: Power Scheme GUID: 381b4222-f694-41f0-9685-ff5bb260df2e  (Balanced)
            var match = Regex.Match(res.Output, @"([a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12})\s+\(([^)]+)\)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return (match.Groups[1].Value, match.Groups[2].Value);
            }
        }
        return (BalancedGuid, "Balanced");
    }

    public static async Task<bool> SetActiveSchemeAsync(string guid)
    {
        var res = await CommandOperations.RunAsync("powercfg", $"/setactive {guid}");
        return res.ExitCode == 0;
    }

    public static async Task<string?> DuplicateSchemeAsync(string sourceGuid)
    {
        var res = await CommandOperations.RunAsync("powercfg", $"-duplicatescheme {sourceGuid}");
        if (res.ExitCode == 0 && !string.IsNullOrWhiteSpace(res.Output))
        {
            var match = Regex.Match(res.Output, @"([a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12})", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }
        return null;
    }

    public static async Task<bool> SetPowerValueIndexAsync(string subGroupGuid, string settingGuid, int acIndex, int? dcIndex = null)
    {
        var active = await GetActiveSchemeAsync();
        var resAc = await CommandOperations.RunAsync("powercfg", $"/setacvalueindex {active.Guid} {subGroupGuid} {settingGuid} {acIndex}");
        if (dcIndex.HasValue)
        {
            await CommandOperations.RunAsync("powercfg", $"/setdcvalueindex {active.Guid} {subGroupGuid} {settingGuid} {dcIndex.Value}");
        }
        await CommandOperations.RunAsync("powercfg", $"/setactive {active.Guid}");
        return resAc.ExitCode == 0;
    }
}
