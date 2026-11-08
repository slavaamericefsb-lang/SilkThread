using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SilkThread.Core.AiOptimizer;
using SilkThread.Core.CompatibilityEngine;
using SilkThread.Core.HardwareDetection;
using SilkThread.Core.Models;
using SilkThread.Core.TweakDatabase;
using Xunit;

namespace SilkThread.Tests;

public class AiOptimizerTests
{
    private readonly HardwareProfile _sampleHardwareProfile;
    private readonly IReadOnlyList<TweakDefinition> _allTweaks;
    private readonly ICompatibilityEngine _compatibilityEngine;

    public AiOptimizerTests()
    {
        _allTweaks = TweakCatalog.GetAllTweaks();
        _compatibilityEngine = new CompatibilityEngine();

        var cpu = new CpuInfo(
            Name: "AMD Ryzen 7 7800X3D 8-Core Processor",
            Manufacturer: "Advanced Micro Devices",
            CoreCount: 8,
            LogicalProcessorCount: 16,
            Architecture: "x64",
            MaxClockSpeedGhz: 5.05,
            IsIntel: false,
            IsAmd: true,
            SupportsVirtualization: true,
            SupportsVbs: true
        );

        var gpu = new GpuInfo(
            Name: "NVIDIA GeForce RTX 4080",
            Manufacturer: "NVIDIA",
            VramGb: 16.0,
            DriverVersion: "560.94",
            DirectXVersion: "12.2",
            IsNvidia: true,
            IsAmd: false,
            IsIntel: false,
            SupportsHags: true,
            HagsEnabled: true
        );

        var ram = new RamInfo(
            TotalGb: 32.0,
            AvailableGb: 24.5,
            ModuleCount: 2,
            SpeedMhz: 6000,
            MemoryType: "DDR5",
            Modules: new List<RamModuleInfo>()
        );

        var storage = new List<DiskDriveInfo>
        {
            new("Samsung 990 PRO 2TB", "NVMe", "NVMe SSD", 2000, 1200, "C:", true, true)
        };

        var os = new OsInfo(
            ProductName: "Windows 11 Pro",
            DisplayVersion: "24H2",
            BuildNumber: 26100,
            Edition: "Pro",
            Architecture: "x64",
            ActivePowerPlanName: "Ultimate Performance",
            ActivePowerPlanGuid: "{e9a42b02-d5df-448d-aa00-03f14749eb61}",
            GameModeEnabled: true,
            CoreIsolationEnabled: false,
            IsWindows11: true
        );

        var power = new PowerAndChassisInfo(
            IsLaptop: false,
            HasBattery: false,
            BatteryPercent: 100,
            IsCharging: false,
            IsAcConnected: true
        );

        var processes = new ProcessDiagnosticInfo(115, new List<string> { "cs2.exe" }, new List<string>());
        var restore = new RestorePointStatus(true, true, "Ready");

        _sampleHardwareProfile = new HardwareProfile(
            cpu,
            new List<GpuInfo> { gpu },
            gpu,
            ram,
            storage,
            os,
            power,
            processes,
            restore
        );
    }

    #region 1. Prompt Generation Tests

    [Fact]
    public void BuildSystemPrompt_ContainsAllRequiredHardwareFieldsAndTweakCatalog()
    {
        using var service = new OpenRouterAiService(compatibilityEngine: _compatibilityEngine);

        var prompt = service.BuildSystemPrompt(_sampleHardwareProfile, _allTweaks);

        // CPU assertions
        Assert.Contains("AMD Ryzen 7 7800X3D", prompt);
        Assert.Contains("8", prompt); // Cores
        Assert.Contains("16", prompt); // Logical Processors
        Assert.Contains("5.05", prompt); // Clock Speed

        // GPU assertions
        Assert.Contains("NVIDIA GeForce RTX 4080", prompt);
        Assert.Contains("16.0", prompt); // VRAM
        Assert.Contains("HAGS Active: True", prompt);

        // RAM assertions
        Assert.Contains("32.0", prompt); // Total RAM
        Assert.Contains("DDR5", prompt);
        Assert.Contains("6000", prompt); // Speed

        // OS assertions
        Assert.Contains("Windows 11 Pro", prompt);
        Assert.Contains("26100", prompt); // Build
        Assert.Contains("Game Mode: Active", prompt);

        // Disks / Storage assertions
        Assert.Contains("Samsung 990 PRO", prompt);
        Assert.Contains("NVMe SSD", prompt);
        Assert.Contains("TRIM Supported: True", prompt);

        // Diagnostic flags
        Assert.Contains("Virtualization-Based Security (VBS)", prompt);
        Assert.Contains("Hardware-Accelerated GPU Scheduling (HAGS)", prompt);
        Assert.Contains("SSD TRIM", prompt);

        // Tweak Catalog inclusion
        Assert.Contains("cpu_power_throttling_disable", prompt);
        Assert.Contains("Disable Execution Power Throttling", prompt);
        Assert.Contains("CpuOptimization", prompt);
        Assert.Contains("Risk: Low", prompt);

        // Strict JSON schema instructions
        Assert.Contains("analysisSummary", prompt);
        Assert.Contains("recommendedTweakIds", prompt);
        Assert.Contains("tweakJustifications", prompt);
        Assert.Contains("estimatedImpact", prompt);
    }

    [Fact]
    public void BuildUserPrompt_IncludesAllUserOptimizationPreferences()
    {
        using var service = new OpenRouterAiService();

        var preferences = new UserOptimizationPreferences(
            GamingScenario: "Esports",
            RiskTolerance: "SafeOnly",
            TargetPriority: "LowestLatency",
            DisplayRefreshRate: "240Hz+",
            MousePollingRate: "1000Hz+"
        );

        var userPrompt = service.BuildUserPrompt(preferences);

        Assert.Contains("Esports", userPrompt);
        Assert.Contains("SafeOnly", userPrompt);
        Assert.Contains("LowestLatency", userPrompt);
        Assert.Contains("240Hz+", userPrompt);
        Assert.Contains("1000Hz+", userPrompt);
    }

    #endregion

    #region 2. JSON Parsing Tests

    [Fact]
    public void ParseAiResponse_PureJson_SuccessfullyExtractsRecommendations()
    {
        using var service = new OpenRouterAiService(compatibilityEngine: _compatibilityEngine);

        var sampleTweak1 = _allTweaks.First(t => t.Id == "cpu_power_throttling_disable");
        var sampleTweak2 = _allTweaks.First(t => t.Id == "cpu_core_parking_unhide");

        var json = $$"""
        {
          "analysisSummary": "System bottleneck analysis: CPU thread throttling detected. High refresh esports scenario requires constant full core availability.",
          "recommendedTweakIds": ["{{sampleTweak1.Id}}", "{{sampleTweak2.Id}}"],
          "tweakJustifications": {
            "{{sampleTweak1.Id}}": "Eliminates OS clock downscaling during CS2 matches.",
            "{{sampleTweak2.Id}}": "Unparks all physical Zen 4 cores to remove 2ms wake-up penalty."
          },
          "estimatedImpact": "-22% DPC latency, +12% 1% low FPS consistency, 0 audio dropouts."
        }
        """;

        var result = service.ParseAiResponse(json, _sampleHardwareProfile, _allTweaks);

        Assert.NotNull(result);
        Assert.Equal("System bottleneck analysis: CPU thread throttling detected. High refresh esports scenario requires constant full core availability.", result.AnalysisSummary);
        Assert.Equal(2, result.RecommendedTweakIds.Count);
        Assert.Contains(sampleTweak1.Id, result.RecommendedTweakIds);
        Assert.Contains(sampleTweak2.Id, result.RecommendedTweakIds);
        Assert.Equal("Eliminates OS clock downscaling during CS2 matches.", result.TweakJustifications[sampleTweak1.Id]);
        Assert.Equal("Unparks all physical Zen 4 cores to remove 2ms wake-up penalty.", result.TweakJustifications[sampleTweak2.Id]);
        Assert.Equal("-22% DPC latency, +12% 1% low FPS consistency, 0 audio dropouts.", result.EstimatedImpact);
        Assert.Contains("AMD Ryzen 7 7800X3D", result.HardwareSummary);
        Assert.Contains("NVIDIA GeForce RTX 4080", result.HardwareSummary);
    }

    [Fact]
    public void ParseAiResponse_OpenRouterEnvelopeJson_ExtractsChoicesContentAndParses()
    {
        using var service = new OpenRouterAiService(compatibilityEngine: _compatibilityEngine);

        var sampleTweak = _allTweaks.First(t => t.Id == "cpu_power_throttling_disable");

        var innerJson = $"{{\"analysisSummary\":\"Envelope unpacked.\",\"recommendedTweakIds\":[\"{sampleTweak.Id}\"],\"tweakJustifications\":{{\"{sampleTweak.Id}\":\"Core stability.\"}},\"estimatedImpact\":\"10% lower latency.\"}}";
        var escapedInnerJson = innerJson.Replace("\"", "\\\"");

        var openRouterResponse = $$"""
        {
          "id": "gen-1718293847-abcdef",
          "model": "liquid/lfm-3-1.7b:free",
          "choices": [
            {
              "message": {
                "role": "assistant",
                "content": "{{escapedInnerJson}}"
              }
            }
          ]
        }
        """;

        var result = service.ParseAiResponse(openRouterResponse, _sampleHardwareProfile, _allTweaks);

        Assert.NotNull(result);
        Assert.Equal("Envelope unpacked.", result.AnalysisSummary);
        Assert.Single(result.RecommendedTweakIds);
        Assert.Equal(sampleTweak.Id, result.RecommendedTweakIds[0]);
    }

    [Fact]
    public void ParseAiResponse_MarkdownFencedJson_StripsFencesAndParses()
    {
        using var service = new OpenRouterAiService(compatibilityEngine: _compatibilityEngine);

        var sampleTweak = _allTweaks.First(t => t.Id == "cpu_power_throttling_disable");

        var markdownWrapped = $$"""
        ```json
        {
          "analysisSummary": "Markdown fence stripped successfully.",
          "recommendedTweakIds": ["{{sampleTweak.Id}}"],
          "tweakJustifications": {
            "{{sampleTweak.Id}}": "Reason"
          },
          "estimatedImpact": "Low latency"
        }
        ```
        """;

        var result = service.ParseAiResponse(markdownWrapped, _sampleHardwareProfile, _allTweaks);

        Assert.NotNull(result);
        Assert.Equal("Markdown fence stripped successfully.", result.AnalysisSummary);
        Assert.Single(result.RecommendedTweakIds);
        Assert.Equal(sampleTweak.Id, result.RecommendedTweakIds[0]);
    }

    [Fact]
    public void ParseAiResponse_FiltersOutHallucinatedTweakIds()
    {
        using var service = new OpenRouterAiService(compatibilityEngine: _compatibilityEngine);

        var legitimateTweak = _allTweaks.First(t => t.Id == "cpu_power_throttling_disable");
        const string hallucinatedTweakId = "fake_ai_quantum_overclock_tweak_999";

        var json = $$"""
        {
          "analysisSummary": "Testing hallucination filtering.",
          "recommendedTweakIds": ["{{legitimateTweak.Id}}", "{{hallucinatedTweakId}}"],
          "tweakJustifications": {
            "{{legitimateTweak.Id}}": "Valid reason.",
            "{{hallucinatedTweakId}}": "Fake reason."
          },
          "estimatedImpact": "Optimal"
        }
        """;

        var result = service.ParseAiResponse(json, _sampleHardwareProfile, _allTweaks);

        Assert.NotNull(result);
        Assert.Single(result.RecommendedTweakIds);
        Assert.Equal(legitimateTweak.Id, result.RecommendedTweakIds[0]);
        Assert.DoesNotContain(hallucinatedTweakId, result.RecommendedTweakIds);
        Assert.DoesNotContain(hallucinatedTweakId, result.TweakJustifications.Keys);
    }

    [Fact]
    public void ParseAiResponse_InvalidJsonSyntax_TriggersHeuristicFallback()
    {
        using var service = new OpenRouterAiService(compatibilityEngine: _compatibilityEngine);

        var malformedJson = "{ broken json content: invalid syntax ...";

        var preferences = new UserOptimizationPreferences("Esports", "SafeOnly", "LowestLatency", "240Hz+", "1000Hz+");
        var result = service.ParseAiResponse(malformedJson, _sampleHardwareProfile, _allTweaks, preferences);

        Assert.NotNull(result);
        Assert.NotEmpty(result.RecommendedTweakIds);
        Assert.Contains("Heuristic Engine", result.AnalysisSummary);
    }

    [Fact]
    public void ParseAiResponse_ZeroValidTweaks_TriggersHeuristicFallback()
    {
        using var service = new OpenRouterAiService(compatibilityEngine: _compatibilityEngine);

        var emptyTweaksJson = """
        {
          "analysisSummary": "No tweaks recommended.",
          "recommendedTweakIds": [],
          "tweakJustifications": {},
          "estimatedImpact": "None"
        }
        """;

        var preferences = new UserOptimizationPreferences("Esports", "SafeOnly", "LowestLatency", "240Hz+", "1000Hz+");
        var result = service.ParseAiResponse(emptyTweaksJson, _sampleHardwareProfile, _allTweaks, preferences);

        Assert.NotNull(result);
        Assert.NotEmpty(result.RecommendedTweakIds);
        Assert.Contains("Heuristic Engine", result.AnalysisSummary);
    }

    #endregion

    #region 3. Heuristic Fallback Engine Tests

    [Fact]
    public void GenerateHeuristicFallback_EsportsSafeOnly_SelectsOnlySafeTweaks()
    {
        using var service = new OpenRouterAiService(compatibilityEngine: _compatibilityEngine);

        var preferences = new UserOptimizationPreferences(
            GamingScenario: "Esports",
            RiskTolerance: "SafeOnly",
            TargetPriority: "LowestLatency",
            DisplayRefreshRate: "240Hz+",
            MousePollingRate: "1000Hz+"
        );

        var result = service.GenerateHeuristicFallback(preferences, _sampleHardwareProfile, _allTweaks);

        Assert.NotNull(result);
        Assert.NotEmpty(result.RecommendedTweakIds);
        Assert.True(result.RecommendedTweakIds.Count >= 5, "Expected at least 5 recommended tweaks in heuristic fallback");

        var tweakMap = _allTweaks.ToDictionary(t => t.Id);

        foreach (var tweakId in result.RecommendedTweakIds)
        {
            Assert.True(tweakMap.ContainsKey(tweakId), $"Recommended tweak {tweakId} must exist in catalog");
            var tweak = tweakMap[tweakId];

            // Risk boundary assertion
            Assert.Equal(RiskLevel.Low, tweak.Risk);

            // Justification assertion
            Assert.True(result.TweakJustifications.ContainsKey(tweakId), $"Tweak {tweakId} must have justification");
            Assert.False(string.IsNullOrWhiteSpace(result.TweakJustifications[tweakId]));
        }

        Assert.Contains("Heuristic Engine", result.AnalysisSummary);
        Assert.Contains("AMD Ryzen 7 7800X3D", result.HardwareSummary);
        Assert.False(string.IsNullOrWhiteSpace(result.EstimatedImpact));
    }

    [Fact]
    public void GenerateHeuristicFallback_RespectsGpuHardwareVendorCompatibility()
    {
        // Construct hardware profile with AMD GPU only (no NVIDIA)
        var amdGpu = new GpuInfo(
            Name: "AMD Radeon RX 7900 XTX",
            Manufacturer: "AMD",
            VramGb: 24.0,
            DriverVersion: "24.10.1",
            DirectXVersion: "12.2",
            IsNvidia: false,
            IsAmd: true,
            IsIntel: false,
            SupportsHags: true,
            HagsEnabled: true
        );

        var amdProfile = _sampleHardwareProfile with
        {
            PrimaryGpu = amdGpu,
            Gpus = new List<GpuInfo> { amdGpu }
        };

        using var service = new OpenRouterAiService(compatibilityEngine: _compatibilityEngine);

        var preferences = new UserOptimizationPreferences("AAA_Visuals", "Moderate", "MaxFps", "144Hz", "Default");
        var result = service.GenerateHeuristicFallback(preferences, amdProfile, _allTweaks);

        var tweakMap = _allTweaks.ToDictionary(t => t.Id);

        foreach (var tweakId in result.RecommendedTweakIds)
        {
            var tweak = tweakMap[tweakId];
            Assert.False(tweak.RequiresNvidiaGpu, $"NVIDIA tweak {tweak.Id} should not be recommended for AMD GPU system");
        }
    }

    [Fact]
    public void GenerateHeuristicFallback_LaptopSystem_ExcludesDesktopOnlyTweaks()
    {
        var laptopPower = new PowerAndChassisInfo(
            IsLaptop: true,
            HasBattery: true,
            BatteryPercent: 85,
            IsCharging: true,
            IsAcConnected: true
        );

        var laptopProfile = _sampleHardwareProfile with { Power = laptopPower };

        using var service = new OpenRouterAiService(compatibilityEngine: _compatibilityEngine);

        var preferences = new UserOptimizationPreferences("Balanced", "Moderate", "SmoothFrametime", "144Hz", "Default");
        var result = service.GenerateHeuristicFallback(preferences, laptopProfile, _allTweaks);

        var tweakMap = _allTweaks.ToDictionary(t => t.Id);

        foreach (var tweakId in result.RecommendedTweakIds)
        {
            var tweak = tweakMap[tweakId];
            Assert.False(tweak.DesktopOnly, $"Desktop-only tweak {tweak.Id} should not be recommended on a laptop");
        }
    }

    #endregion

    #region 4. Network Resilience & Mocked API Integration Tests

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request, cancellationToken);
        }
    }

    [Fact]
    public async Task GetOptimizationRecommendationsAsync_OnHttp500_SeamlesslyReturnsFallbackWithoutThrowing()
    {
        var mockHandler = new MockHttpMessageHandler((req, ct) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("{\"error\":\"Internal server error on OpenRouter\"}", Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        });

        using var httpClient = new HttpClient(mockHandler);
        using var service = new OpenRouterAiService(
            compatibilityEngine: _compatibilityEngine,
            httpClient: httpClient,
            apiKey: "test-openrouter-key"
        );

        var preferences = new UserOptimizationPreferences("Esports", "SafeOnly", "LowestLatency", "240Hz+", "1000Hz+");

        var result = await service.GetOptimizationRecommendationsAsync(preferences, _sampleHardwareProfile, _allTweaks);

        Assert.NotNull(result);
        Assert.NotEmpty(result.RecommendedTweakIds);
        Assert.Contains("Heuristic Engine", result.AnalysisSummary);
        Assert.Contains("HTTP 500", result.AnalysisSummary);
    }

    [Fact]
    public async Task GetOptimizationRecommendationsAsync_OnNetworkException_SeamlesslyReturnsFallbackWithoutThrowing()
    {
        var mockHandler = new MockHttpMessageHandler((req, ct) =>
        {
            throw new HttpRequestException("Simulated connection timeout or network outage");
        });

        using var httpClient = new HttpClient(mockHandler);
        using var service = new OpenRouterAiService(
            compatibilityEngine: _compatibilityEngine,
            httpClient: httpClient,
            apiKey: "test-openrouter-key"
        );

        var preferences = new UserOptimizationPreferences("Esports", "SafeOnly", "LowestLatency", "240Hz+", "1000Hz+");

        var result = await service.GetOptimizationRecommendationsAsync(preferences, _sampleHardwareProfile, _allTweaks);

        Assert.NotNull(result);
        Assert.NotEmpty(result.RecommendedTweakIds);
        Assert.Contains("Heuristic Engine", result.AnalysisSummary);
        Assert.Contains("Network error", result.AnalysisSummary);
    }

    [Fact]
    public async Task GetOptimizationRecommendationsAsync_OnSuccess_SendsRequiredHeadersAndParsesResult()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;

        var sampleTweak = _allTweaks.First(t => t.Id == "cpu_power_throttling_disable");

        var mockHandler = new MockHttpMessageHandler(async (req, ct) =>
        {
            capturedRequest = req;
            capturedBody = await req.Content!.ReadAsStringAsync(ct);

            var aiJson = $$"""
            {
              "analysisSummary": "Elite esports optimization tailored for Ryzen 7 7800X3D and RTX 4080.",
              "recommendedTweakIds": ["{{sampleTweak.Id}}"],
              "tweakJustifications": {
                "{{sampleTweak.Id}}": "Disables power throttling for CS2."
              },
              "estimatedImpact": "-25% DPC latency, +15% 1% Low FPS"
            }
            """;

            var escapedAiJson = aiJson.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");

            var responseJson = $$"""
            {
              "id": "gen-test-12345",
              "choices": [
                {
                  "message": {
                    "role": "assistant",
                    "content": "{{escapedAiJson}}"
                  }
                }
              ]
            }
            """;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
        });

        using var httpClient = new HttpClient(mockHandler);
        using var service = new OpenRouterAiService(
            compatibilityEngine: _compatibilityEngine,
            httpClient: httpClient,
            apiKey: "test-openrouter-key"
        );

        var preferences = new UserOptimizationPreferences("Esports", "SafeOnly", "LowestLatency", "240Hz+", "1000Hz+");

        var result = await service.GetOptimizationRecommendationsAsync(preferences, _sampleHardwareProfile, _allTweaks);

        // Verify result
        Assert.NotNull(result);
        Assert.Equal("Elite esports optimization tailored for Ryzen 7 7800X3D and RTX 4080.", result.AnalysisSummary);
        Assert.Single(result.RecommendedTweakIds);
        Assert.Equal(sampleTweak.Id, result.RecommendedTweakIds[0]);
        Assert.Equal("Disables power throttling for CS2.", result.TweakJustifications[sampleTweak.Id]);
        Assert.Equal("-25% DPC latency, +15% 1% Low FPS", result.EstimatedImpact);

        // Verify headers
        Assert.NotNull(capturedRequest);
        Assert.True(capturedRequest.Headers.Contains("Authorization"));
        var authHeader = capturedRequest.Headers.GetValues("Authorization").First();
        Assert.Equal("Bearer test-openrouter-key", authHeader);

        Assert.True(capturedRequest.Headers.Contains("HTTP-Referer"));
        Assert.Equal("https://github.com/ColdLuv/SilkThread", capturedRequest.Headers.GetValues("HTTP-Referer").First());

        Assert.True(capturedRequest.Headers.Contains("X-Title"));
        Assert.Equal("SilkThread Gaming Optimizer", capturedRequest.Headers.GetValues("X-Title").First());

        // Verify request payload contains model and json_object response format
        Assert.NotNull(capturedBody);
        Assert.Contains("liquid/lfm-3-1.7b:free", capturedBody);
        Assert.Contains("json_object", capturedBody);
        Assert.Contains("0.2", capturedBody); // temperature
    }

    #endregion
}
