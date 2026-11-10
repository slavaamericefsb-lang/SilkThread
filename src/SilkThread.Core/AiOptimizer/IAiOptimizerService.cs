using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SilkThread.Core.Models;

namespace SilkThread.Core.AiOptimizer;

/// <summary>
/// Service contract for AI-driven system optimization and intelligent hardware tuning recommendations.
/// </summary>
public interface IAiOptimizerService
{
    /// <summary>
    /// Analyzes the hardware configuration and user gaming preferences, returning tailored tweak recommendations.
    /// Uses OpenRouter LLM with automated local heuristic fallback on network or API failures.
    /// </summary>
    Task<AiTuningRecommendation> GetOptimizationRecommendationsAsync(
        UserOptimizationPreferences preferences,
        HardwareProfile? hardwareProfile = null,
        IReadOnlyList<TweakDefinition>? availableTweaks = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates the system prompt embedding hardware specs, system status (VBS/HAGS/TRIM), and available tweak catalog.
    /// </summary>
    string BuildSystemPrompt(
        HardwareProfile hardwareProfile,
        IReadOnlyList<TweakDefinition> availableTweaks);

    /// <summary>
    /// Generates the user prompt reflecting scenario, risk tolerance, and latency/FPS targets.
    /// </summary>
    string BuildUserPrompt(
        UserOptimizationPreferences preferences);

    /// <summary>
    /// Parses and validates the JSON response returned by the AI model or raw OpenRouter envelope.
    /// </summary>
    AiTuningRecommendation ParseAiResponse(
        string responseContent,
        HardwareProfile hardwareProfile,
        IReadOnlyList<TweakDefinition> availableTweaks,
        UserOptimizationPreferences? preferences = null,
        string? modelName = null);

    /// <summary>
    /// Fallback rule-based heuristic optimizer executed when the AI API is unreachable or returns invalid data.
    /// </summary>
    AiTuningRecommendation GenerateHeuristicFallback(
        UserOptimizationPreferences preferences,
        HardwareProfile hardwareProfile,
        IReadOnlyList<TweakDefinition> availableTweaks,
        string? failureReason = null);
}
