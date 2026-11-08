$output = @"
using System.Collections.Generic;

namespace SilkThread.App.Services;

public record TweakTranslation(
    string Title,
    string Description,
    string ProblemSolved,
    string ExpectedResult,
    string? HowItWorks = null
);

public static class TweakLocalization
{
    public static string GetTitle(string id, string fallback) =>
        Translations.TryGetValue(id, out var t) ? t.Title : fallback;

    public static string GetDescription(string id, string fallback) =>
        Translations.TryGetValue(id, out var t) ? t.Description : fallback;

    public static string GetProblemSolved(string id, string fallback) =>
        Translations.TryGetValue(id, out var t) ? t.ProblemSolved : fallback;

    public static string GetExpectedResult(string id, string fallback) =>
        Translations.TryGetValue(id, out var t) ? t.ExpectedResult : fallback;

    public static string GetHowItWorks(string id, string fallback) =>
        Translations.TryGetValue(id, out var t) && !string.IsNullOrEmpty(t.HowItWorks) ? t.HowItWorks : fallback;

    public static readonly Dictionary<string, TweakTranslation> Translations = new()
    {
"@

# Helper to escape strings
function Q($str) {
    if ([string]::IsNullOrEmpty($str)) { return '""' }
    $escaped = $str.Replace('\', '\\').Replace('"', '\"')
    return "`"$escaped`""
}

Write-Output "Helper defined"
