using SilkThread.Core.SystemOperations;
using System.Text.Json;

namespace SilkThread.App.Services;

public static class PageFileReviewText
{
    public static string Snapshot(string json, ILocalizationService loc)
    {
        var snapshot = JsonSerializer.Deserialize<PageFileSnapshot>(json) ?? throw new InvalidDataException();
        var raw = snapshot.PagingFiles;
        return loc["Memory_Automatic"] + ": " + loc[snapshot.Automatic ? "Memory_On" : "Memory_Off"] + "\n"
            + (raw.Exists ? string.Join("\n", JsonSerializer.Deserialize<string[]>(raw.RawValue ?? "[]") ?? Array.Empty<string>()) : loc["Page_ValueAbsent"]);
    }
    public static string Apply(PageFileReview review, ILocalizationService loc)
        => Snapshot(review.OriginalJson, loc) + "\n\n↓\n\n" + loc["Memory_Automatic"] + ": " + loc[review.Plan.Automatic ? "Memory_On" : "Memory_Off"]
            + "\n" + string.Join("\n", review.Plan.Files.Select(f => $"{f.Path}: {f.InitialMb} / {f.MaximumMb} MiB"))
            + "\n\n" + loc["Page_Risk"];
    public static string Restore(PageFileRestoreReview review, ILocalizationService loc)
        => (review.Conflict ? loc["Page_ExternalConflict"] + "\n\n" : "") + Snapshot(review.CurrentJson, loc)
            + "\n\n↓\n\n" + Snapshot(review.OriginalJson, loc) + "\n\n" + loc["Page_Risk"];
}
