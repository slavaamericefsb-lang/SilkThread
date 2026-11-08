using System;
using System.Collections.Generic;

namespace SilkThread.App.Services;

/// <summary>
/// Model representing a release history milestone.
/// </summary>
public record UpdateReleaseEntry(
    string Version,
    string Date,
    string Title,
    IReadOnlyList<string> Highlights,
    bool IsLatest = false
);

/// <summary>
/// Service providing version release history with full localization support.
/// </summary>
public interface IUpdateHistoryService
{
    IReadOnlyList<UpdateReleaseEntry> GetReleaseHistory();
}

public sealed class UpdateHistoryService : IUpdateHistoryService
{
    private readonly ILocalizationService _loc;

    public UpdateHistoryService(ILocalizationService loc)
    {
        _loc = loc;
    }

    public IReadOnlyList<UpdateReleaseEntry> GetReleaseHistory()
    {
        return new List<UpdateReleaseEntry>
        {
            new(
                Version: "v1.1.0",
                Date: _loc["UpdHist_v11_Date"],
                Title: _loc["UpdHist_v11_Title"],
                Highlights: new[]
                {
                    _loc["UpdHist_v11_Item1"],
                    _loc["UpdHist_v11_Item2"],
                    _loc["UpdHist_v11_Item3"],
                    _loc["UpdHist_v11_Item4"],
                    _loc["UpdHist_v11_Item5"],
                    _loc["UpdHist_v11_Item6"]
                },
                IsLatest: true
            ),
            new(
                Version: "v1.0.5",
                Date: _loc["UpdHist_v15_Date"],
                Title: _loc["UpdHist_v15_Title"],
                Highlights: new[]
                {
                    _loc["UpdHist_v15_Item1"],
                    _loc["UpdHist_v15_Item2"],
                    _loc["UpdHist_v15_Item3"]
                },
                IsLatest: false
            ),
            new(
                Version: "v1.0.0",
                Date: _loc["UpdHist_v10_Date"],
                Title: _loc["UpdHist_v10_Title"],
                Highlights: new[]
                {
                    _loc["UpdHist_v10_Item1"],
                    _loc["UpdHist_v10_Item2"],
                    _loc["UpdHist_v10_Item3"]
                },
                IsLatest: false
            )
        };
    }
}
