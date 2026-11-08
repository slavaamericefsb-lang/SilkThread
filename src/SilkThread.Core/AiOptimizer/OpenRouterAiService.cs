using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SilkThread.Core.CompatibilityEngine;
using SilkThread.Core.HardwareDetection;
using SilkThread.Core.Models;
using SilkThread.Core.TweakDatabase;

namespace SilkThread.Core.AiOptimizer;

/// <summary>
/// AI-powered system optimization service connecting to OpenRouter API (Liquid LFM-3 1.7B)
/// with intelligent local heuristic fallback on network or API failures.
/// </summary>
public class OpenRouterAiService : IAiOptimizerService, IDisposable
{
    public const string DefaultModel = "liquid/lfm-3-1.7b:free";
    public const string DefaultEndpoint = "https://openrouter.ai/api/v1/chat/completions";
    public const string DefaultApiKey = "";
    public const string RefererHeader = "https://github.com/ColdLuv/SilkThread";
    public const string AppTitleHeader = "SilkThread Gaming Optimizer";

    private readonly HttpClient _httpClient;
    private readonly bool _disposeClient;
    private readonly IHardwareScanner? _hardwareScanner;
    private readonly ICompatibilityEngine? _compatibilityEngine;
    private readonly ILogger<OpenRouterAiService>? _logger;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly string _endpoint;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public string Model => _model;
    public string Endpoint => _endpoint;

    public OpenRouterAiService()
        : this(null, null, null, null, null, null, null)
    {
    }

    public OpenRouterAiService(
        IHardwareScanner hardwareScanner,
        ICompatibilityEngine compatibilityEngine,
        ILogger<OpenRouterAiService>? logger = null)
        : this(hardwareScanner, compatibilityEngine, logger, null, null, null, null)
    {
    }

    public OpenRouterAiService(
        IHardwareScanner? hardwareScanner = null,
        ICompatibilityEngine? compatibilityEngine = null,
        ILogger<OpenRouterAiService>? logger = null,
        HttpClient? httpClient = null,
        string? apiKey = null,
        string? model = null,
        string? endpoint = null)
    {
        _hardwareScanner = hardwareScanner;
        _compatibilityEngine = compatibilityEngine;
        _logger = logger;
        _apiKey = !string.IsNullOrWhiteSpace(apiKey) ? apiKey : DefaultApiKey;
        _model = !string.IsNullOrWhiteSpace(model) ? model : DefaultModel;
        _endpoint = !string.IsNullOrWhiteSpace(endpoint) ? endpoint : DefaultEndpoint;

        if (httpClient != null)
        {
            _httpClient = httpClient;
            _disposeClient = false;
        }
        else
        {
            _httpClient = CreateDefaultHttpClient(_apiKey);
            _disposeClient = true;
        }
    }

    private static HttpClient CreateDefaultHttpClient(string apiKey)
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");
        }
        client.DefaultRequestHeaders.TryAddWithoutValidation("HTTP-Referer", RefererHeader);
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Title", AppTitleHeader);
        return client;
    }

    /// <inheritdoc />
    public async Task<AiTuningRecommendation> GetOptimizationRecommendationsAsync(
        UserOptimizationPreferences preferences,
        HardwareProfile? hardwareProfile = null,
        IReadOnlyList<TweakDefinition>? availableTweaks = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Resolve Hardware Profile
        if (hardwareProfile == null)
        {
            if (_hardwareScanner != null)
            {
                try
                {
                    hardwareProfile = await _hardwareScanner.ScanAsync();
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to scan hardware profile via IHardwareScanner, using fallback hardware specs.");
                }
            }
            hardwareProfile ??= CreateFallbackHardwareProfile();
        }

        // 2. Resolve Available Tweaks
        if (availableTweaks == null || availableTweaks.Count == 0)
        {
            availableTweaks = TweakCatalog.GetAllTweaks();
        }

        // 3. Pre-filter candidate tweaks for prompt relevance (compatible & within risk bounds)
        var candidateTweaks = PreFilterCandidateTweaks(preferences, hardwareProfile, availableTweaks);

        // 4. Build Prompts
        var systemPrompt = BuildSystemPrompt(hardwareProfile, candidateTweaks);
        var userPrompt = BuildUserPrompt(preferences);

        // 5. Build OpenRouter Request Body
        var requestPayload = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            response_format = new { type = "json_object" },
            temperature = 0.2
        };

        var requestJson = JsonSerializer.Serialize(requestPayload, JsonOptions);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
            };

            // Ensure headers are populated even if custom HttpClient without default headers was passed
            if (!string.IsNullOrWhiteSpace(_apiKey) && !request.Headers.Contains("Authorization") && !_httpClient.DefaultRequestHeaders.Contains("Authorization"))
            {
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_apiKey}");
            }
            if (!request.Headers.Contains("HTTP-Referer") && !_httpClient.DefaultRequestHeaders.Contains("HTTP-Referer"))
            {
                request.Headers.TryAddWithoutValidation("HTTP-Referer", RefererHeader);
            }
            if (!request.Headers.Contains("X-Title") && !_httpClient.DefaultRequestHeaders.Contains("X-Title"))
            {
                request.Headers.TryAddWithoutValidation("X-Title", AppTitleHeader);
            }

            _logger?.LogInformation("Sending optimization request to OpenRouter ({Model})...", _model);

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger?.LogWarning("OpenRouter API returned HTTP {StatusCode}: {ErrorBody}. Triggering heuristic fallback.",
                    (int)response.StatusCode, errorBody);

                return GenerateHeuristicFallback(
                    preferences,
                    hardwareProfile,
                    availableTweaks,
                    $"OpenRouter API HTTP {(int)response.StatusCode} ({response.ReasonPhrase})");
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger?.LogInformation("Received successful response from OpenRouter API.");

            return ParseAiResponse(responseBody, hardwareProfile, availableTweaks, preferences);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Exception while communicating with OpenRouter API. Engaging local heuristic fallback.");
            return GenerateHeuristicFallback(preferences, hardwareProfile, availableTweaks, $"Network error: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public string BuildSystemPrompt(HardwareProfile hardwareProfile, IReadOnlyList<TweakDefinition> availableTweaks)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are SilkThread AI Gaming Optimizer, an elite Windows kernel, hardware, and competitive esports optimization engineer.");
        sb.AppendLine("Your mission is to perform a deep technical diagnosis of the user's PC hardware, OS state, and gaming scenario, then select the optimal system tweaks.");
        sb.AppendLine();
        sb.AppendLine("### HARDWARE & OPERATING SYSTEM PROFILE:");

        // CPU
        var cpu = hardwareProfile.Cpu;
        sb.AppendLine(FormattableString.Invariant($"- CPU: {cpu.Name} ({cpu.Manufacturer}) | Physical Cores: {cpu.CoreCount}, Logical Processors: {cpu.LogicalProcessorCount} | Base/Max Clock: {cpu.MaxClockSpeedGhz:F2} GHz | Architecture: {cpu.Architecture} | VBS Supported: {cpu.SupportsVbs}"));

        // GPU
        var gpu = hardwareProfile.PrimaryGpu;
        sb.AppendLine(FormattableString.Invariant($"- GPU: {gpu.Name} ({gpu.Manufacturer}) | VRAM: {gpu.VramGb:F1} GB | Driver: {gpu.DriverVersion} | DirectX: {gpu.DirectXVersion} | HAGS Supported: {gpu.SupportsHags} | HAGS Active: {gpu.HagsEnabled}"));

        // RAM
        var ram = hardwareProfile.Ram;
        sb.AppendLine(FormattableString.Invariant($"- RAM: {ram.TotalGb:F1} GB Total, {ram.AvailableGb:F1} GB Available | Speed: {ram.SpeedMhz} MHz | Type: {ram.MemoryType} | Modules: {ram.ModuleCount}"));

        // OS
        var os = hardwareProfile.Os;
        sb.AppendLine($"- OS: {os.ProductName} ({os.Edition}, {os.DisplayVersion}, Build {os.BuildNumber}) | Power Plan: {os.ActivePowerPlanName} | Game Mode: {(os.GameModeEnabled ? "Active" : "Disabled")} | VBS/Core Isolation: {(os.CoreIsolationEnabled ? "Active" : "Disabled")}");

        // Disks & Storage
        sb.AppendLine("- Disks:");
        foreach (var disk in hardwareProfile.Storage)
        {
            sb.AppendLine(FormattableString.Invariant($"  * [{disk.DriveLetter}] {disk.Model} ({disk.MediaType}) | Size: {disk.TotalSizeGb:F0} GB | Free: {disk.FreeSpaceGb:F0} GB | System Drive: {disk.IsSystemDrive} | TRIM Supported: {disk.SupportsTrim}"));
        }

        // Chassis & Power
        var pwr = hardwareProfile.Power;
        sb.AppendLine($"- Form Factor: {(pwr.IsLaptop ? "Laptop" : "Desktop")} | Battery: {(pwr.HasBattery ? $"{pwr.BatteryPercent}% (AC: {pwr.IsAcConnected})" : "None (Desktop AC Power)")}");

        // System Diagnostic Flags
        var vbsStatus = os.CoreIsolationEnabled ? "Active (adds virtualization latency penalty for games)" : "Disabled (optimal for raw gaming DPC latency)";
        var hagsStatus = gpu.SupportsHags ? (gpu.HagsEnabled ? "Active" : "Disabled") : "Not Supported";
        var trimStatus = hardwareProfile.Storage.Any(s => s.SupportsTrim) ? "Active" : "Disabled";
        sb.AppendLine($"- Virtualization-Based Security (VBS): {vbsStatus}");
        sb.AppendLine($"- Hardware-Accelerated GPU Scheduling (HAGS): {hagsStatus}");
        sb.AppendLine($"- SSD TRIM: {trimStatus}");

        sb.AppendLine();
        sb.AppendLine("### AVAILABLE CANDIDATE TWEAKS TO SELECT FROM:");
        sb.AppendLine("You MUST ONLY select tweak IDs from this list. Never invent or hallucinate tweak IDs!");

        foreach (var tweak in availableTweaks)
        {
            sb.AppendLine($"- ID: {tweak.Id} | Title: \"{tweak.Title}\" | Category: {tweak.Category} | Risk: {tweak.Risk} | Rec: {tweak.Recommendation}");
        }

        sb.AppendLine();
        sb.AppendLine("### STRICT JSON OUTPUT REQUIREMENTS:");
        sb.AppendLine("Respond with pure JSON only matching this schema without markdown fences or extraneous text:");
        sb.AppendLine("{");
        sb.AppendLine("  \"analysisSummary\": \"Concise technical summary explaining hardware state, identified bottlenecks, and overall optimization plan.\",");
        sb.AppendLine("  \"recommendedTweakIds\": [\"id_1\", \"id_2\", ...],");
        sb.AppendLine("  \"tweakJustifications\": {");
        sb.AppendLine("    \"id_1\": \"Technical reason why id_1 directly benefits this PC and user scenario.\",");
        sb.AppendLine("    \"id_2\": \"Technical reason why id_2 directly benefits this PC and user scenario.\"");
        sb.AppendLine("  },");
        sb.AppendLine("  \"estimatedImpact\": \"Detailed quantitative prediction (e.g. -22% input latency, +9-14% 1% low FPS, 1.4GB RAM debloat).\"");
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <inheritdoc />
    public string BuildUserPrompt(UserOptimizationPreferences preferences)
    {
        var sb = new StringBuilder();
        sb.AppendLine("User Optimization Request:");
        sb.AppendLine($"- Gaming Scenario: {preferences.GamingScenario}");
        sb.AppendLine($"- Risk Tolerance: {preferences.RiskTolerance}");
        sb.AppendLine($"- Target Priority: {preferences.TargetPriority}");
        sb.AppendLine($"- Display Refresh Rate: {preferences.DisplayRefreshRate}");
        sb.AppendLine($"- Mouse Polling Rate: {preferences.MousePollingRate}");
        sb.AppendLine();
        sb.AppendLine("Please analyze my hardware profile, eliminate latency and frame pacing jitter for my target scenario, adhere strictly to my risk tolerance, and output your recommendation set in the required JSON schema.");
        return sb.ToString();
    }

    /// <inheritdoc />
    public AiTuningRecommendation ParseAiResponse(
        string responseContent,
        HardwareProfile hardwareProfile,
        IReadOnlyList<TweakDefinition> availableTweaks,
        UserOptimizationPreferences? preferences = null)
    {
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            return GenerateHeuristicFallback(
                preferences ?? CreateDefaultPreferences(),
                hardwareProfile,
                availableTweaks,
                "Empty AI response");
        }

        var jsonContent = ExtractJsonContent(responseContent);

        try
        {
            var dto = JsonSerializer.Deserialize<AiRecommendationDto>(jsonContent, JsonOptions);
            if (dto == null)
            {
                throw new JsonException("Deserialized AI recommendation DTO is null");
            }

            var tweakMap = availableTweaks.ToDictionary(t => t.Id, t => t, StringComparer.OrdinalIgnoreCase);

            // Filter recommended tweaks to only valid, non-hallucinated IDs
            var validTweakIds = new List<string>();
            if (dto.RecommendedTweakIds != null)
            {
                foreach (var id in dto.RecommendedTweakIds)
                {
                    if (tweakMap.TryGetValue(id, out var matchedTweak) && !validTweakIds.Contains(matchedTweak.Id))
                    {
                        validTweakIds.Add(matchedTweak.Id);
                    }
                }
            }

            // Fallback if the model returned 0 valid tweaks
            if (validTweakIds.Count == 0)
            {
                _logger?.LogWarning("AI response contained 0 recognized tweak IDs. Engaging heuristic fallback.");
                return GenerateHeuristicFallback(
                    preferences ?? CreateDefaultPreferences(),
                    hardwareProfile,
                    availableTweaks,
                    "AI response contained zero matching tweak IDs");
            }

            // Map and complete justifications
            var justifications = new Dictionary<string, string>();
            if (dto.TweakJustifications != null)
            {
                foreach (var kvp in dto.TweakJustifications)
                {
                    if (tweakMap.TryGetValue(kvp.Key, out var matchedTweak) && !string.IsNullOrWhiteSpace(kvp.Value))
                    {
                        justifications[matchedTweak.Id] = kvp.Value.Trim();
                    }
                }
            }

            // Ensure every recommended tweak has a justification
            foreach (var id in validTweakIds)
            {
                if (!justifications.ContainsKey(id) || string.IsNullOrWhiteSpace(justifications[id]))
                {
                    var tweak = tweakMap[id];
                    justifications[id] = !string.IsNullOrWhiteSpace(tweak.ProblemSolved)
                        ? $"{tweak.ProblemSolved} ({tweak.ExpectedResult})"
                        : tweak.Description;
                }
            }

            var analysisSummary = !string.IsNullOrWhiteSpace(dto.AnalysisSummary)
                ? dto.AnalysisSummary.Trim()
                : "SilkThread AI Optimization completed successfully.";

            var estimatedImpact = !string.IsNullOrWhiteSpace(dto.EstimatedImpact)
                ? dto.EstimatedImpact.Trim()
                : "System responsiveness and frametime consistency optimized.";

            var hardwareSummary = BuildHardwareSummary(hardwareProfile);

            return new AiTuningRecommendation(
                AnalysisSummary: analysisSummary,
                RecommendedTweakIds: validTweakIds,
                TweakJustifications: justifications,
                EstimatedImpact: estimatedImpact,
                HardwareSummary: hardwareSummary
            );
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to parse AI response JSON. Engaging heuristic fallback.");
            return GenerateHeuristicFallback(
                preferences ?? CreateDefaultPreferences(),
                hardwareProfile,
                availableTweaks,
                $"JSON Parse Error: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public AiTuningRecommendation GenerateHeuristicFallback(
        UserOptimizationPreferences preferences,
        HardwareProfile hardwareProfile,
        IReadOnlyList<TweakDefinition> availableTweaks,
        string? failureReason = null)
    {
        var hardwareSummary = BuildHardwareSummary(hardwareProfile);

        // 1. Compatibility filtering
        var compatible = availableTweaks.Where(tweak =>
        {
            if (_compatibilityEngine != null)
            {
                var check = _compatibilityEngine.EvaluateCompatibility(tweak, hardwareProfile);
                if (!check.IsCompatible) return false;
            }

            // Architecture & Vendor checks
            if (tweak.RequiresNvidiaGpu && !hardwareProfile.PrimaryGpu.IsNvidia) return false;
            if (tweak.RequiresAmdGpu && !hardwareProfile.PrimaryGpu.IsAmd) return false;
            if (tweak.RequiresIntelGpu && !hardwareProfile.PrimaryGpu.IsIntel) return false;
            if (tweak.RequiresIntelCpu && !hardwareProfile.Cpu.IsIntel) return false;
            if (tweak.RequiresAmdCpu && !hardwareProfile.Cpu.IsAmd) return false;

            // Form factor checks
            if (tweak.DesktopOnly && hardwareProfile.Power.IsLaptop) return false;
            if (tweak.LaptopOnly && !hardwareProfile.Power.IsLaptop) return false;

            // Storage checks
            if (tweak.RequiresSsdOrNvme && !hardwareProfile.Storage.Any(s => s.IsSsd)) return false;

            // Windows build check
            if (tweak.MinWindowsBuild > 0 && hardwareProfile.Os.BuildNumber < tweak.MinWindowsBuild) return false;

            return true;
        }).ToList();

        // 2. Risk tolerance filtering
        var riskTolerance = preferences.RiskTolerance;
        var allowedTweaks = compatible.Where(tweak =>
        {
            if (string.Equals(riskTolerance, "SafeOnly", StringComparison.OrdinalIgnoreCase))
            {
                return tweak.Risk == RiskLevel.Low;
            }
            if (string.Equals(riskTolerance, "Moderate", StringComparison.OrdinalIgnoreCase))
            {
                return tweak.Risk <= RiskLevel.Medium;
            }
            // Experimental allows High risk, but excludes Critical
            return tweak.Risk <= RiskLevel.High;
        }).ToList();

        if (allowedTweaks.Count == 0)
        {
            allowedTweaks = compatible.Where(t => t.Risk == RiskLevel.Low).ToList();
        }

        // 3. Scenario & Priority Scoring
        var scenario = preferences.GamingScenario;
        var priority = preferences.TargetPriority;
        var refreshRate = preferences.DisplayRefreshRate;
        var pollingRate = preferences.MousePollingRate;

        var scoredTweaks = allowedTweaks.Select(tweak =>
        {
            var score = 10;

            if (tweak.Recommendation == RecommendationLevel.Recommended) score += 15;
            else if (tweak.Recommendation == RecommendationLevel.Optional) score += 5;

            // Scenario Weights
            if (string.Equals(scenario, "Esports", StringComparison.OrdinalIgnoreCase))
            {
                if (tweak.Category == TweakCategory.InputAndLatency) score += 40;
                if (tweak.Category == TweakCategory.CpuOptimization) score += 30;
                if (tweak.Category == TweakCategory.GamingPerformance) score += 25;
                if (tweak.Category == TweakCategory.NetworkOptimization) score += 20;
                if (tweak.Category == TweakCategory.WindowsPerformance) score += 15;
            }
            else if (string.Equals(scenario, "AAA_Visuals", StringComparison.OrdinalIgnoreCase))
            {
                if (tweak.Category == TweakCategory.GpuOptimization) score += 40;
                if (tweak.Category == TweakCategory.GamingPerformance) score += 30;
                if (tweak.Category == TweakCategory.StorageOptimization) score += 25;
                if (tweak.Category == TweakCategory.WindowsPerformance) score += 15;
                if (tweak.Category == TweakCategory.CpuOptimization) score += 15;
            }
            else if (string.Equals(scenario, "Streaming", StringComparison.OrdinalIgnoreCase))
            {
                if (tweak.Category == TweakCategory.NetworkOptimization) score += 35;
                if (tweak.Category == TweakCategory.BackgroundProcesses) score += 30;
                if (tweak.Category == TweakCategory.WindowsServices) score += 25;
                if (tweak.Category == TweakCategory.RamOptimization) score += 20;
                if (tweak.Category == TweakCategory.CpuOptimization) score += 15;
            }
            else // Balanced
            {
                if (tweak.Category == TweakCategory.WindowsPerformance) score += 25;
                if (tweak.Category == TweakCategory.SystemMaintenance) score += 25;
                if (tweak.Category == TweakCategory.SecurityAndPrivacy) score += 20;
                if (tweak.Category == TweakCategory.StorageOptimization) score += 20;
                if (tweak.Category == TweakCategory.GamingPerformance) score += 15;
            }

            // Target Priority Boosts
            if (string.Equals(priority, "LowestLatency", StringComparison.OrdinalIgnoreCase))
            {
                if (tweak.Category == TweakCategory.InputAndLatency) score += 25;
                if (tweak.Category == TweakCategory.CpuOptimization) score += 15;
                if (tweak.Category == TweakCategory.NetworkOptimization) score += 10;
            }
            else if (string.Equals(priority, "MaxFps", StringComparison.OrdinalIgnoreCase))
            {
                if (tweak.Category == TweakCategory.GpuOptimization) score += 25;
                if (tweak.Category == TweakCategory.CpuOptimization) score += 20;
                if (tweak.Category == TweakCategory.GamingPerformance) score += 15;
            }
            else if (string.Equals(priority, "SmoothFrametime", StringComparison.OrdinalIgnoreCase))
            {
                if (tweak.Category == TweakCategory.CpuOptimization) score += 25;
                if (tweak.Category == TweakCategory.RamOptimization) score += 15;
                if (tweak.Category == TweakCategory.StorageOptimization) score += 15;
            }
            else if (string.Equals(priority, "MemoryDebloat", StringComparison.OrdinalIgnoreCase))
            {
                if (tweak.Category == TweakCategory.RamOptimization) score += 30;
                if (tweak.Category == TweakCategory.WindowsServices) score += 25;
                if (tweak.Category == TweakCategory.BackgroundProcesses) score += 20;
            }

            // Peripherals Boosts
            if (pollingRate.Contains("1000", StringComparison.OrdinalIgnoreCase) || pollingRate.Contains("4000", StringComparison.OrdinalIgnoreCase) || pollingRate.Contains("8000", StringComparison.OrdinalIgnoreCase))
            {
                if (tweak.Category == TweakCategory.InputAndLatency) score += 15;
            }
            if (refreshRate.Contains("240", StringComparison.OrdinalIgnoreCase) || refreshRate.Contains("360", StringComparison.OrdinalIgnoreCase) || refreshRate.Contains("540", StringComparison.OrdinalIgnoreCase))
            {
                if (tweak.Category == TweakCategory.CpuOptimization || tweak.Category == TweakCategory.InputAndLatency) score += 10;
            }

            return (Tweak: tweak, Score: score);
        })
        .OrderByDescending(x => x.Score)
        .ThenBy(x => x.Tweak.Title)
        .ToList();

        // 4. Select top tweaks (12 to 18 recommended tweaks)
        var selectedCount = Math.Min(16, scoredTweaks.Count);
        var selectedTweaks = scoredTweaks.Take(selectedCount).Select(x => x.Tweak).ToList();

        // 5. Build Technical Justifications
        var justifications = new Dictionary<string, string>();
        foreach (var t in selectedTweaks)
        {
            justifications[t.Id] = BuildContextualJustification(t, hardwareProfile, preferences);
        }

        // 6. Build Estimated Impact
        var estimatedImpact = BuildEstimatedImpact(preferences);

        // 7. Build Analysis Summary
        var notice = !string.IsNullOrEmpty(failureReason)
            ? $"[SilkThread Heuristic Engine: {failureReason}] "
            : "[SilkThread Heuristic Engine] ";

        var analysisSummary = FormattableString.Invariant($"{notice}Diagnosed hardware configuration ({hardwareProfile.Cpu.Name}, {hardwareProfile.PrimaryGpu.Name}, {hardwareProfile.Ram.TotalGb:F0}GB RAM). Tailored {selectedTweaks.Count} system tweaks for '{preferences.GamingScenario}' scenario with '{preferences.TargetPriority}' priority and '{preferences.RiskTolerance}' safety boundaries. Focused on eliminating thread scheduling jitter, input polling bottlenecks, and unnecessary background resource consumption.");

        return new AiTuningRecommendation(
            AnalysisSummary: analysisSummary,
            RecommendedTweakIds: selectedTweaks.Select(t => t.Id).ToList(),
            TweakJustifications: justifications,
            EstimatedImpact: estimatedImpact,
            HardwareSummary: hardwareSummary
        );
    }

    private static string BuildContextualJustification(
        TweakDefinition tweak,
        HardwareProfile hardwareProfile,
        UserOptimizationPreferences preferences)
    {
        return tweak.Category switch
        {
            TweakCategory.InputAndLatency =>
                $"Calibrates input thread dispatch for {preferences.MousePollingRate} polling rate, eliminating interrupt queue latency on {hardwareProfile.Cpu.Name}.",
            TweakCategory.CpuOptimization =>
                $"Prevents core downclocking and parking delays across all {hardwareProfile.Cpu.CoreCount} cores, ensuring flat frametimes for {preferences.GamingScenario}.",
            TweakCategory.GpuOptimization =>
                $"Optimizes DirectX and GPU driver queue scheduling for {hardwareProfile.PrimaryGpu.Name} to maintain stable frame delivery on {preferences.DisplayRefreshRate} displays.",
            TweakCategory.NetworkOptimization =>
                $"Disables network throttling and optimizes TCP ACK delivery to minimize network jitter and ping spikes in {preferences.GamingScenario}.",
            TweakCategory.RamOptimization =>
                $"Optimizes Windows memory manager and Standby List clearing to maintain maximum free memory on your {hardwareProfile.Ram.TotalGb:F0}GB RAM pool.",
            TweakCategory.BackgroundProcesses or TweakCategory.WindowsServices =>
                $"Suppresses non-essential OS background telemetry and services to prevent CPU priority inversions during competitive gameplay.",
            TweakCategory.StorageOptimization =>
                $"Optimizes filesystem caching and NVMe/SSD TRIM dispatch to avoid storage I/O hiccups during texture and map streaming.",
            _ =>
                !string.IsNullOrWhiteSpace(tweak.ProblemSolved)
                    ? $"{tweak.ProblemSolved} ({tweak.ExpectedResult})"
                    : tweak.Description
        };
    }

    private static string BuildEstimatedImpact(UserOptimizationPreferences preferences)
    {
        if (string.Equals(preferences.GamingScenario, "Esports", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(preferences.TargetPriority, "LowestLatency", StringComparison.OrdinalIgnoreCase))
        {
            return "Estimated -18% to -32% DPC and input queue latency, +10-15% 1% Low FPS consistency, ~1.2GB RAM freed for esports titles.";
        }

        if (string.Equals(preferences.GamingScenario, "AAA_Visuals", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(preferences.TargetPriority, "MaxFps", StringComparison.OrdinalIgnoreCase))
        {
            return "Estimated +6-12% average FPS in GPU-bound titles, reduced texture streaming stutter, smoothed frametimes at high display refresh.";
        }

        if (string.Equals(preferences.GamingScenario, "Streaming", StringComparison.OrdinalIgnoreCase))
        {
            return "Estimated 0 dropped frames during OBS ingest, -1.8GB background RAM footprint, eliminated CPU spikes from background services.";
        }

        return "Estimated +12-20% overall frametime pacing consistency, enhanced system responsiveness, -1.5GB background memory footprint.";
    }

    public static string BuildHardwareSummary(HardwareProfile profile)
    {
        var cpu = profile.Cpu;
        var gpu = profile.PrimaryGpu;
        var ram = profile.Ram;
        var os = profile.Os;
        var vbs = os.CoreIsolationEnabled ? "Active" : "Disabled";
        var hags = gpu.SupportsHags ? (gpu.HagsEnabled ? "Active" : "Disabled") : "N/A";
        var trim = profile.Storage.Any(s => s.SupportsTrim) ? "Active" : "Disabled";

        return FormattableString.Invariant($"CPU: {cpu.Name} ({cpu.CoreCount}C/{cpu.LogicalProcessorCount}T @ {cpu.MaxClockSpeedGhz:F1}GHz) | GPU: {gpu.Name} ({gpu.VramGb:F1}GB) | RAM: {ram.TotalGb:F0}GB ({ram.MemoryType} @ {ram.SpeedMhz}MHz) | OS: {os.ProductName} Build {os.BuildNumber} | VBS: {vbs} | HAGS: {hags} | TRIM: {trim}");
    }

    private static IReadOnlyList<TweakDefinition> PreFilterCandidateTweaks(
        UserOptimizationPreferences preferences,
        HardwareProfile hardwareProfile,
        IReadOnlyList<TweakDefinition> tweaks)
    {
        var filtered = tweaks.Where(t =>
        {
            // Vendor filters
            if (t.RequiresNvidiaGpu && !hardwareProfile.PrimaryGpu.IsNvidia) return false;
            if (t.RequiresAmdGpu && !hardwareProfile.PrimaryGpu.IsAmd) return false;
            if (t.RequiresIntelGpu && !hardwareProfile.PrimaryGpu.IsIntel) return false;
            if (t.RequiresIntelCpu && !hardwareProfile.Cpu.IsIntel) return false;
            if (t.RequiresAmdCpu && !hardwareProfile.Cpu.IsAmd) return false;

            // Form factor filters
            if (t.DesktopOnly && hardwareProfile.Power.IsLaptop) return false;
            if (t.LaptopOnly && !hardwareProfile.Power.IsLaptop) return false;

            // Storage
            if (t.RequiresSsdOrNvme && !hardwareProfile.Storage.Any(s => s.IsSsd)) return false;

            // Windows build
            if (t.MinWindowsBuild > 0 && hardwareProfile.Os.BuildNumber < t.MinWindowsBuild) return false;

            // Risk bounds
            if (string.Equals(preferences.RiskTolerance, "SafeOnly", StringComparison.OrdinalIgnoreCase))
            {
                return t.Risk == RiskLevel.Low;
            }
            if (string.Equals(preferences.RiskTolerance, "Moderate", StringComparison.OrdinalIgnoreCase))
            {
                return t.Risk <= RiskLevel.Medium;
            }

            return t.Risk <= RiskLevel.High;
        }).ToList();

        return filtered.Count > 0 ? filtered : tweaks;
    }

    private static string ExtractJsonContent(string raw)
    {
        var text = raw.Trim();

        // 1. If it's an OpenRouter response envelope with "choices", extract the assistant message content
        if (text.Contains("\"choices\"", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                using var doc = JsonDocument.Parse(text);
                if (doc.RootElement.TryGetProperty("choices", out var choices) &&
                    choices.GetArrayLength() > 0 &&
                    choices[0].TryGetProperty("message", out var msg) &&
                    msg.TryGetProperty("content", out var contentProp))
                {
                    var extracted = contentProp.GetString();
                    if (!string.IsNullOrWhiteSpace(extracted))
                    {
                        text = extracted.Trim();
                    }
                }
            }
            catch
            {
                // Proceed with raw string if parsing envelope failed
            }
        }

        // 2. Strip Markdown code fences if present (```json ... ``` or ``` ... ```)
        if (text.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            text = text[7..];
        }
        else if (text.StartsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            text = text[3..];
        }

        if (text.EndsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            text = text[..^3];
        }

        text = text.Trim();

        // 3. Extract JSON object substring between outer '{' and '}'
        var firstBrace = text.IndexOf('{');
        var lastBrace = text.LastIndexOf('}');
        if (firstBrace >= 0 && lastBrace > firstBrace)
        {
            text = text.Substring(firstBrace, lastBrace - firstBrace + 1);
        }

        return text;
    }

    private static HardwareProfile CreateFallbackHardwareProfile()
    {
        var cpu = new CpuInfo("x64 Multicore Processor", "Generic", Environment.ProcessorCount, Environment.ProcessorCount, "x64", 3.2, false, false, true, true);
        var gpu = new GpuInfo("Dedicated Gaming Graphics", "Generic", 8.0, "Latest", "DirectX 12", true, false, false, true, true);
        var ram = new RamInfo(16.0, 10.0, 2, 3200, "DDR4", new List<RamModuleInfo>());
        var storage = new List<DiskDriveInfo>
        {
            new("NVMe SSD", "NVMe", "NVMe SSD", 1024, 450, "C:", true, true)
        };
        var os = new OsInfo("Windows 11", "24H2", 26100, "Pro", "x64", "Balanced", "GUID", true, true, true);
        var power = new PowerAndChassisInfo(false, false, 100, false, true);
        var processes = new ProcessDiagnosticInfo(110, new List<string>(), new List<string>());
        var restore = new RestorePointStatus(true, true, "Active");
        return new HardwareProfile(cpu, new List<GpuInfo> { gpu }, gpu, ram, storage, os, power, processes, restore);
    }

    private static UserOptimizationPreferences CreateDefaultPreferences() =>
        new("Balanced", "SafeOnly", "MaxFps", "144Hz", "Default");

    public void Dispose()
    {
        if (_disposeClient)
        {
            _httpClient.Dispose();
        }
        GC.SuppressFinalize(this);
    }

    private sealed class AiRecommendationDto
    {
        [JsonPropertyName("analysisSummary")]
        public string? AnalysisSummary { get; set; }

        [JsonPropertyName("recommendedTweakIds")]
        public List<string>? RecommendedTweakIds { get; set; }

        [JsonPropertyName("tweakJustifications")]
        public Dictionary<string, string>? TweakJustifications { get; set; }

        [JsonPropertyName("estimatedImpact")]
        public string? EstimatedImpact { get; set; }
    }
}
