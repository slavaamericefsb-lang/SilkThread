using System.Collections.Generic;

namespace SilkThread.Core.AiOptimizer;

/// <summary>
/// Final result of AI-assisted system analysis and tweak recommendations.
/// </summary>
/// <param name="AnalysisSummary">Technical analysis of system bottlenecks and tailored optimization strategy.</param>
/// <param name="RecommendedTweakIds">List of tweak IDs recommended for application.</param>
/// <param name="TweakJustifications">Dictionary mapping tweak ID to specific rationale for this hardware and scenario.</param>
/// <param name="EstimatedImpact">Quantified impact forecast (latency reduction, FPS gains, RAM debloat).</param>
/// <param name="HardwareSummary">Summary of the scanned hardware configuration used during analysis.</param>
public record AiTuningRecommendation(
    string AnalysisSummary,
    List<string> RecommendedTweakIds,
    Dictionary<string, string> TweakJustifications,
    string EstimatedImpact,
    string HardwareSummary
);
