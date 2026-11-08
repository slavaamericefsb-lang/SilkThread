namespace SilkThread.Core.AiOptimizer;

/// <summary>
/// User-selected preferences and requirements for AI-driven system optimization.
/// </summary>
/// <param name="GamingScenario">Gaming scenario, e.g. "Esports" (CS2/Valorant), "AAA_Visuals", "Streaming", "Balanced".</param>
/// <param name="RiskTolerance">Risk tolerance level: "SafeOnly", "Moderate", "Experimental".</param>
/// <param name="TargetPriority">Primary optimization target: "LowestLatency", "MaxFps", "SmoothFrametime", "MemoryDebloat".</param>
/// <param name="DisplayRefreshRate">Display refresh rate tier: "240Hz+", "144Hz", "60Hz".</param>
/// <param name="MousePollingRate">Mouse USB polling rate: "1000Hz+", "500Hz", "Default".</param>
public record UserOptimizationPreferences(
    string GamingScenario,
    string RiskTolerance,
    string TargetPriority,
    string DisplayRefreshRate,
    string MousePollingRate
);
