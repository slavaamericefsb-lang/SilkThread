using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SilkThread.Core.Models;

namespace SilkThread.App.Services;

public class LiquidAiTweakSelection
{
    public string TweakId { get; set; } = string.Empty;
    public string Rationale { get; set; } = string.Empty;
    public string RiskAssessment { get; set; } = string.Empty;
}

public class LiquidAiResult
{
    public string PresetName { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<LiquidAiTweakSelection> Selections { get; set; } = new();
    public bool UsedOnlineModel { get; set; }
    public string ModelName { get; set; } = "liquid/lfm-3-1.7b:free";
    public string GenerationSource { get; set; } = "Локальный эксперт IOST";

    public bool IsOnlineAiGenerated
    {
        get => UsedOnlineModel;
        set => UsedOnlineModel = value;
    }

    public string AiModelUsed
    {
        get => ModelName;
        set => ModelName = value;
    }
}

public interface ILiquidAiOptimizerService
{
    Task<LiquidAiResult> GenerateOptimizationsAsync(
        HardwareProfile hardware,
        int scenarioIndex,
        int riskIndex,
        int priorityIndex,
        IReadOnlyList<TweakDefinition> allTweaks,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);

    LiquidAiResult GenerateExpertProfile(
        HardwareProfile hardware,
        int scenarioIndex,
        int riskIndex,
        int priorityIndex,
        string scenarioName,
        string riskName,
        string priorityName,
        IReadOnlyList<TweakDefinition> allTweaks,
        bool isRu);
}

public class LiquidAiOptimizerService : ILiquidAiOptimizerService
{
    private static readonly HttpClient s_httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(35)
    };

    private static readonly string[] s_fallbackModels = new[]
    {
        "openrouter/free",
        "google/gemma-4-26b-a4b-it:free",
        "nvidia/nemotron-3-nano-omni-30b-a3b-reasoning:free"
    };

    private readonly ISettingsService? _settingsService;
    private readonly ILocalizationService? _loc;

    public LiquidAiOptimizerService(ISettingsService? settingsService = null, ILocalizationService? loc = null)
    {
        _settingsService = settingsService;
        _loc = loc;
    }

    public async Task<LiquidAiResult> GenerateOptimizationsAsync(
        HardwareProfile hardware,
        int scenarioIndex,
        int riskIndex,
        int priorityIndex,
        IReadOnlyList<TweakDefinition> allTweaks,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        bool isRu = _loc?.CurrentLanguage == "ru";

        string scenarioName = scenarioIndex switch
        {
            0 => isRu ? "Киберспорт (CS2/Valorant)" : "Esports (CS2/Valorant)",
            1 => isRu ? "Тяжёлые AAA-игры" : "Heavy AAA Games",
            2 => isRu ? "Стриминг и OBS" : "Streaming & OBS",
            _ => isRu ? "Сбалансированный режим" : "Balanced Mode"
        };

        string riskName = riskIndex switch
        {
            0 => isRu ? "Безопасный (100% Safe)" : "Safe (100% Safe)",
            1 => isRu ? "Сбалансированный (Low + Medium)" : "Balanced (Low + Medium)",
            _ => isRu ? "Максимальный (All Risks)" : "Maximum Performance (All Risks)"
        };

        string priorityName = priorityIndex switch
        {
            0 => isRu ? "Минимальная задержка (Input Lag)" : "Lowest Input Lag",
            1 => isRu ? "Максимальный FPS" : "Maximum FPS",
            2 => isRu ? "Плавный фреймтайм" : "Smooth Frametime",
            _ => isRu ? "Очистка памяти" : "Memory Cleanup"
        };

        // Phase 1: Hardware Analysis
        progress?.Report(isRu 
            ? "Анализ аппаратного профиля и компонентов..." 
            : "Analyzing hardware profile and components...");
        await Task.Delay(350, cancellationToken);

        string apiKey = _settingsService?.Settings?.OpenRouterApiKey?.Trim() ?? "";
        string modelName = string.IsNullOrWhiteSpace(_settingsService?.Settings?.OpenRouterModel)
            ? "liquid/lfm-3-1.7b:free"
            : _settingsService.Settings.OpenRouterModel.Trim();

        // Attempt online neural generation via OpenRouter if API key is provided
        if (!string.IsNullOrEmpty(apiKey))
        {
            try
            {
                progress?.Report(isRu 
                    ? "ИИ-анализ библиотеки твиков через нейросеть..." 
                    : "Neural AI reasoning across all tweak categories...");

                var onlineResult = await QueryOpenRouterAsync(
                    hardware, scenarioName, riskName, priorityName, riskIndex, allTweaks, apiKey, modelName, cancellationToken);

                if (onlineResult != null && onlineResult.Selections.Count >= 20)
                {
                    onlineResult.PresetName = $"AI: {scenarioName} ({priorityName})";
                    onlineResult.GenerationSource = $"Нейросеть OpenRouter ({onlineResult.ModelName})";
                    onlineResult.IsOnlineAiGenerated = true;
                    onlineResult.AiModelUsed = onlineResult.ModelName;
                    return onlineResult;
                }
            }
            catch (Exception ex)
            {
                App.Log($"[IOST AI] Исключение онлайн-генерации: {ex.Message}. Переключение на локальный экспертный движок.");
            }
        }

        // Phase 2: Local Multi-Category AI Expert Engine
        App.Log("[IOST AI] Активация локального экспертного движка SilkThread IOST...");
        progress?.Report(isRu 
            ? "Формирование многокатегорийного профиля (ИИ-движок)..." 
            : "Synthesizing multi-category preset (AI Expert Engine)...");
        await Task.Delay(450, cancellationToken);

        return GenerateExpertProfile(hardware, scenarioIndex, riskIndex, priorityIndex, scenarioName, riskName, priorityName, allTweaks, isRu);
    }

    private async Task<LiquidAiResult?> QueryOpenRouterAsync(
        HardwareProfile hardware,
        string scenarioName,
        string riskName,
        string priorityName,
        int riskIndex,
        IReadOnlyList<TweakDefinition> allTweaks,
        string apiKey,
        string model,
        CancellationToken ct)
    {
        var maxRisk = riskIndex switch
        {
            0 => RiskLevel.Low,
            1 => RiskLevel.Medium,
            _ => RiskLevel.High
        };

        bool isLaptop = hardware.Power.IsLaptop;
        bool hasSsd = hardware.Storage.Any(s => s.IsSsd);

        // Pre-filter candidate tweaks by risk and hardware compatibility
        var compatibleTweaks = allTweaks.Where(t =>
        {
            if (t.Risk > maxRisk) return false;
            if (t.RequiresIntelCpu && !hardware.Cpu.IsIntel) return false;
            if (t.RequiresAmdCpu && !hardware.Cpu.IsAmd) return false;
            if (t.RequiresNvidiaGpu && !hardware.PrimaryGpu.IsNvidia) return false;
            if (t.RequiresAmdGpu && !hardware.PrimaryGpu.IsAmd) return false;
            if (t.RequiresIntelGpu && !hardware.PrimaryGpu.IsIntel) return false;
            if (t.DesktopOnly && isLaptop) return false;
            if (t.LaptopOnly && !isLaptop) return false;
            if (t.RequiresSsdOrNvme && !hasSsd) return false;
            if (t.MinWindowsBuild > 0 && hardware.Os.BuildNumber < t.MinWindowsBuild) return false;
            return true;
        }).ToList();

        // Provide comprehensive candidate catalog across ALL categories (up to 15 per category = 180-225 candidates)
        var sampledTweaks = compatibleTweaks
            .GroupBy(t => t.Category)
            .SelectMany(g => g
                .OrderByDescending(t => t.Recommendation == RecommendationLevel.Recommended)
                .ThenBy(t => t.Risk)
                .Take(15))
            .Select(t => new
            {
                t.Id,
                t.Title,
                Category = t.Category.ToString(),
                Risk = t.Risk.ToString(),
                Problem = !string.IsNullOrWhiteSpace(t.ProblemSolved) ? t.ProblemSolved : t.Description
            })
            .ToList();

        var promptBuilder = new StringBuilder();
        promptBuilder.AppendLine("User Hardware Configuration:");
        promptBuilder.AppendLine($"- CPU: {hardware.Cpu.Name} ({hardware.Cpu.CoreCount}C/{hardware.Cpu.LogicalProcessorCount}T, Max {hardware.Cpu.MaxClockSpeedGhz:F2} GHz, Vendor: {(hardware.Cpu.IsAmd ? "AMD" : hardware.Cpu.IsIntel ? "Intel" : "Other")})");
        promptBuilder.AppendLine($"- GPU: {hardware.PrimaryGpu.Name} ({hardware.PrimaryGpu.VramGb:F1}GB VRAM, Vendor: {(hardware.PrimaryGpu.IsNvidia ? "NVIDIA" : hardware.PrimaryGpu.IsAmd ? "AMD" : "Intel")})");
        promptBuilder.AppendLine($"- RAM: {hardware.Ram.TotalGb:F1}GB ({hardware.Ram.MemoryType} @ {hardware.Ram.SpeedMhz} MHz)");
        promptBuilder.AppendLine($"- Storage: {(hasSsd ? "High-Speed SSD/NVMe" : "HDD Storage")} (TRIM: {(hardware.Storage.Any(s => s.SupportsTrim) ? "Supported" : "No")})");
        promptBuilder.AppendLine($"- OS: {hardware.Os.ProductName} {hardware.Os.Edition} (Build {hardware.Os.BuildNumber})");
        promptBuilder.AppendLine($"- Form Factor: {(isLaptop ? "Laptop" : "Desktop Workstation")}");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Target Goals & Boundaries:");
        promptBuilder.AppendLine($"- Scenario: {scenarioName}");
        promptBuilder.AppendLine($"- Risk Level: {riskName}");
        promptBuilder.AppendLine($"- Main Optimization Priority: {priorityName}");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Available Candidate Tweaks Library (Representative samples from ALL categories):");
        promptBuilder.AppendLine(JsonSerializer.Serialize(sampledTweaks));
        promptBuilder.AppendLine();
        int targetMin = riskIndex switch
        {
            0 => 60,
            1 => 85,
            _ => 120
        };
        int targetMax = riskIndex switch
        {
            0 => 80,
            1 => 120,
            _ => 160
        };

        promptBuilder.AppendLine("MANDATORY ARCHITECTURAL REQUIREMENTS:");
        promptBuilder.AppendLine($"1. Select a comprehensive, deep optimization profile of {targetMin} to {targetMax}+ tweaks (scaled by risk level: ~60–80 for Safe, ~85–120 for Balanced, ~120–160+ for Maximum).");
        promptBuilder.AppendLine("2. The selection MUST be evenly distributed across ALL key categories (CPU & Schedulers, GPU & Graphics, RAM & Memory, Network, Input & Latency, Gaming & DirectX, Services Debloat, Telemetry & Privacy, Storage & NVMe, Power & Maintenance, Windows Performance & UI, Advanced).");
        promptBuilder.AppendLine($"3. Under NO circumstances select only 20-40 tweaks. A comprehensive, full-system profile of at least {targetMin} tweaks is strictly required.");
        promptBuilder.AppendLine("4. For EACH tweak, write a clear, personalized technical rationale explaining how it directly optimizes the user's specific hardware components.");
        promptBuilder.AppendLine("5. Output MUST be valid JSON strictly matching the schema without markdown fences or additional text:");
        promptBuilder.AppendLine("{\"summary\":\"Detailed technical overview of the full system optimization strategy\",\"tweaks\":[{\"id\":\"tweak_id\",\"rationale\":\"Technical rationale tailored to hardware and user scenario\",\"risk\":\"Low/Medium\"}]}");

        var modelsToTry = new List<string>();
        if (!string.IsNullOrWhiteSpace(model))
        {
            modelsToTry.Add(model.Trim());
        }
        else
        {
            modelsToTry.Add("liquid/lfm-3-1.7b:free");
        }

        foreach (var fb in s_fallbackModels)
        {
            if (!modelsToTry.Any(m => string.Equals(m, fb, StringComparison.OrdinalIgnoreCase)))
            {
                modelsToTry.Add(fb);
            }
        }

        var allTweaksMap = allTweaks.ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var currentModel in modelsToTry)
        {
            if (ct.IsCancellationRequested) break;

            var requestBody = new
            {
                model = currentModel,
                messages = new object[]
                {
                    new
                    {
                        role = "system",
                        content = "You are SilkThread AI Gaming Optimizer, an elite Windows kernel, hardware, and competitive esports optimization engineer. Respond ONLY with valid JSON matching the requested schema."
                    },
                    new
                    {
                        role = "user",
                        content = promptBuilder.ToString()
                    }
                },
                temperature = 0.2,
                max_tokens = 4000
            };

            try
            {
                App.Log($"[IOST AI] Отправка запроса к модели {currentModel}...");

                using var request = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                request.Headers.Add("HTTP-Referer", "https://github.com/ColdLuv/SilkThread");
                request.Headers.Add("X-Title", "SilkThread Gaming Optimizer");
                request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

                using var response = await s_httpClient.SendAsync(request, ct);
                App.Log($"[IOST AI] Ответ от {currentModel}: HTTP {response.StatusCode}");

                if ((int)response.StatusCode == 429)
                {
                    App.Log($"[IOST AI] Модель {currentModel} превысила лимит запросов (HTTP 429). Переключение на резервную модель...");
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    App.Log($"[IOST AI] Модель {currentModel} вернула ошибку HTTP {response.StatusCode}. Пробуем следующую модель...");
                    continue;
                }

                var json = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
                {
                    App.Log($"[IOST AI] Модель {currentModel} вернула пустой список choices.");
                    continue;
                }

                if (!choices[0].TryGetProperty("message", out var msg))
                {
                    App.Log($"[IOST AI] В ответе модели {currentModel} отсутствует message.");
                    continue;
                }

                string? messageContent = null;
                if (msg.TryGetProperty("content", out var contentProp) && contentProp.ValueKind == JsonValueKind.String)
                {
                    messageContent = contentProp.GetString();
                }
                if (string.IsNullOrWhiteSpace(messageContent) && msg.TryGetProperty("reasoning", out var reasoningProp) && reasoningProp.ValueKind == JsonValueKind.String)
                {
                    messageContent = reasoningProp.GetString();
                }

                if (string.IsNullOrWhiteSpace(messageContent))
                {
                    App.Log($"[IOST AI] В ответе модели {currentModel} content и reasoning пусты.");
                    continue;
                }

                string cleanJson = ExtractJson(messageContent);
                using var resultDoc = JsonDocument.Parse(cleanJson);
                var root = resultDoc.RootElement;

                var result = new LiquidAiResult
                {
                    UsedOnlineModel = true,
                    IsOnlineAiGenerated = true,
                    ModelName = currentModel,
                    AiModelUsed = currentModel,
                    GenerationSource = $"Нейросеть OpenRouter ({currentModel})",
                    Summary = root.TryGetProperty("summary", out var sumProp) 
                        ? sumProp.GetString() ?? "" 
                        : (root.TryGetProperty("analysisSummary", out var asProp) ? asProp.GetString() ?? "" : "")
                };

                JsonElement tweaksArray = default;
                bool hasTweaksArray = false;

                if (root.ValueKind == JsonValueKind.Array)
                {
                    tweaksArray = root;
                    hasTweaksArray = true;
                }
                else if (root.TryGetProperty("tweaks", out var tProp) && tProp.ValueKind == JsonValueKind.Array)
                {
                    tweaksArray = tProp;
                    hasTweaksArray = true;
                }
                else if (root.TryGetProperty("recommendedTweakIds", out var rProp) && rProp.ValueKind == JsonValueKind.Array)
                {
                    tweaksArray = rProp;
                    hasTweaksArray = true;
                }

                if (hasTweaksArray)
                {
                    foreach (var item in tweaksArray.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.Object)
                        {
                            if (item.TryGetProperty("id", out var idProp))
                            {
                                string id = idProp.GetString() ?? "";
                                if (allTweaksMap.TryGetValue(id, out var matchedDef))
                                {
                                    string rationale = item.TryGetProperty("rationale", out var ratProp) 
                                        ? ratProp.GetString() ?? "" 
                                        : (item.TryGetProperty("justification", out var justProp) ? justProp.GetString() ?? "" : "");
                                    string risk = item.TryGetProperty("risk", out var rkProp) ? rkProp.GetString() ?? "" : matchedDef.Risk.ToString();
                                    
                                    if (!result.Selections.Any(s => s.TweakId.Equals(matchedDef.Id, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        result.Selections.Add(new LiquidAiTweakSelection
                                        {
                                            TweakId = matchedDef.Id,
                                            Rationale = !string.IsNullOrWhiteSpace(rationale) ? rationale : (!string.IsNullOrWhiteSpace(matchedDef.ProblemSolved) ? matchedDef.ProblemSolved : matchedDef.Description),
                                            RiskAssessment = risk
                                        });
                                    }
                                }
                            }
                        }
                        else if (item.ValueKind == JsonValueKind.String)
                        {
                            string id = item.GetString() ?? "";
                            if (allTweaksMap.TryGetValue(id, out var matchedDef))
                            {
                                if (!result.Selections.Any(s => s.TweakId.Equals(matchedDef.Id, StringComparison.OrdinalIgnoreCase)))
                                {
                                    result.Selections.Add(new LiquidAiTweakSelection
                                    {
                                        TweakId = matchedDef.Id,
                                        Rationale = !string.IsNullOrWhiteSpace(matchedDef.ProblemSolved) ? matchedDef.ProblemSolved : matchedDef.Description,
                                        RiskAssessment = matchedDef.Risk.ToString()
                                    });
                                }
                            }
                        }
                    }
                }

                if (result.Selections.Count >= 20)
                {
                    App.Log($"[IOST AI] Успешно получено {result.Selections.Count} рекомендаций от нейросети ({currentModel})!");
                    return result;
                }
                else
                {
                    App.Log($"[IOST AI] Модель {currentModel} сформировала недостаточно рекомендаций ({result.Selections.Count} < 20).");
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                App.Log($"[IOST AI] Ошибка при обращении к {currentModel}: {ex.Message}");
            }
        }

        App.Log("[IOST AI] Не удалось получить валидный ответ от онлайн-моделей. Переход к локальному экспертному движку...");
        return null;
    }

    private static string ExtractJson(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "{}";
        var raw = text.Trim();

        // 1. Check for Markdown code fences (e.g. ```json ... ``` or ``` ... ```)
        var fenceMatch = System.Text.RegularExpressions.Regex.Match(raw, @"```(?:json)?\s*([\s\S]*?)\s*```", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (fenceMatch.Success)
        {
            var fencedContent = fenceMatch.Groups[1].Value.Trim();
            var extractedFromFence = TryExtractBalancedJson(fencedContent);
            if (!string.IsNullOrEmpty(extractedFromFence))
            {
                return extractedFromFence;
            }
        }

        // 2. Try extracting balanced JSON object directly (handles conversational prefix/postfix)
        var directExtraction = TryExtractBalancedJson(raw);
        if (!string.IsNullOrEmpty(directExtraction))
        {
            return directExtraction;
        }

        // 3. Fallback: match "tweaks" or "recommendedTweakIds" array directly using Regex
        var arrayMatch = System.Text.RegularExpressions.Regex.Match(raw, @"""(?:tweaks|recommendedTweakIds)""\s*:\s*\[", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (arrayMatch.Success)
        {
            int openBracketIndex = raw.IndexOf('[', arrayMatch.Index);
            if (openBracketIndex >= 0)
            {
                var extractedArray = ExtractBalancedBracketString(raw, openBracketIndex, '[', ']');
                if (!string.IsNullOrEmpty(extractedArray))
                {
                    return $"{{\"summary\":\"AI Generated System Preset\",\"tweaks\":{extractedArray}}}";
                }
            }
        }

        // 4. Fallback: match any top-level JSON array
        int firstBracket = raw.IndexOf('[');
        if (firstBracket >= 0)
        {
            var extractedArray = ExtractBalancedBracketString(raw, firstBracket, '[', ']');
            if (!string.IsNullOrEmpty(extractedArray))
            {
                return $"{{\"summary\":\"AI Generated System Preset\",\"tweaks\":{extractedArray}}}";
            }
        }

        // 5. Ultimate fallback: between first { and last }
        int start = raw.IndexOf('{');
        int end = raw.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            return raw.Substring(start, end - start + 1);
        }

        return raw;
    }

    private static bool IsValidRecommendationObject(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object) return false;
        return root.TryGetProperty("tweaks", out _) ||
               root.TryGetProperty("recommendedTweakIds", out _) ||
               root.TryGetProperty("summary", out _) ||
               root.TryGetProperty("analysisSummary", out _);
    }

    private static string? TryExtractBalancedJson(string text)
    {
        // First try standard first '{' and last '}' if it produces valid JSON immediately
        int firstBrace = text.IndexOf('{');
        int lastBrace = text.LastIndexOf('}');
        if (firstBrace >= 0 && lastBrace > firstBrace)
        {
            string candidate = text.Substring(firstBrace, lastBrace - firstBrace + 1);
            try
            {
                using var doc = JsonDocument.Parse(candidate);
                if (IsValidRecommendationObject(doc.RootElement))
                {
                    return candidate;
                }
            }
            catch
            {
                // Continue with balanced scan
            }
        }

        // Balanced brace scan to find the first valid complete JSON object
        int searchStart = 0;
        while ((searchStart = text.IndexOf('{', searchStart)) >= 0)
        {
            var candidate = ExtractBalancedBracketString(text, searchStart, '{', '}');
            if (!string.IsNullOrEmpty(candidate))
            {
                try
                {
                    using var doc = JsonDocument.Parse(candidate);
                    if (IsValidRecommendationObject(doc.RootElement))
                    {
                        return candidate;
                    }
                }
                catch
                {
                    // Continue searching next '{'
                }
            }
            searchStart++;
        }

        return null;
    }

    private static string? ExtractBalancedBracketString(string text, int startIndex, char openChar, char closeChar)
    {
        if (startIndex < 0 || startIndex >= text.Length || text[startIndex] != openChar)
            return null;

        int depth = 0;
        bool inString = false;
        bool escape = false;

        for (int i = startIndex; i < text.Length; i++)
        {
            char c = text[i];
            if (escape)
            {
                escape = false;
                continue;
            }

            if (c == '\\')
            {
                escape = true;
                continue;
            }

            if (c == '"')
            {
                inString = !inString;
                continue;
            }

            if (!inString)
            {
                if (c == openChar)
                {
                    depth++;
                }
                else if (c == closeChar)
                {
                    depth--;
                    if (depth == 0)
                    {
                        return text.Substring(startIndex, i - startIndex + 1);
                    }
                }
            }
        }

        return null;
    }

    [Flags]
    public enum TweakTag
    {
        None = 0,
        Input = 1 << 0,             // Raw Input, mouse, keyboard polling, cursor, xinput
        Network = 1 << 1,           // TCP NoDelay, MTU, QoS, DNS, RSS, RSC
        TimerResolution = 1 << 2,   // Distribute timers, timer resolution, idle resiliency, critical section
        DwmDirectFlip = 1 << 3,     // DirectFlip, DWM high priority, borderless, buffer count, DXGI waitable
        GpuCore = 1 << 4,           // HAGS, DirectX, VRAM, PowerMizer, Anti-Lag, Shader Cache, VRR
        CpuThrottling = 1 << 5,     // Core unparking, Power throttling, CPB/SpeedShift, EPB, C-states
        PowerPlan = 1 << 6,         // Power plan, min processor state, cooling policy, PCIe ASPM
        GameMode = 1 << 7,          // Game mode, GameConfigStore, Game Bar disable, FSE
        StreamingObs = 1 << 8,      // Audio MMCSS, Game DVR, Discord hook mitigation, Process priority
        AudioLatency = 1 << 9,      // MMCSS audio realtime, spatial audio disable, audio ducker
        FrametimeSmooth = 1 << 10,  // Standby list, working set protection, shader cache, DPC watchdog
        MemoryStandby = 1 << 11,    // Standby list trimming, working set trim protect
        MemoryClean = 1 << 12,      // Edge prelaunch, Copilot, Widgets, SvcHost split, Paging executive
        ServicesDebloat = 1 << 13,  // Maps, Fax, Wallet, Biometric, Geolocation, TrkWks, Remote registry
        TelemetryPrivacy = 1 << 14, // DiagTrack, Advertising ID, CEIP, CompatTelRunner, DeviceCensus
        SystemHygiene = 1 << 15,    // Fast startup, Windows tips, MenuShowDelay, AutoPlay, Search indexer
        StorageTune = 1 << 16       // NTFS 8.3, Last access, TRIM, NVMe APST, DirectStorage BypassIO
    }

    public enum TweakCategoryGroup
    {
        CpuSchedulers,
        GpuGraphics,
        RamMemory,
        NetworkLatency,
        InputPeripherals,
        GamingDirectX,
        ServicesDebloat,
        TelemetryPrivacy,
        StorageNvme,
        PowerMaintenance,
        WindowsPerformanceUi
    }

    private static TweakCategoryGroup GetCategoryGroup(string tweakId, IReadOnlyDictionary<string, TweakDefinition> defMap)
    {
        if (defMap.TryGetValue(tweakId, out var def))
        {
            return def.Category switch
            {
                TweakCategory.CpuOptimization => TweakCategoryGroup.CpuSchedulers,
                TweakCategory.GpuOptimization => TweakCategoryGroup.GpuGraphics,
                TweakCategory.RamOptimization => TweakCategoryGroup.RamMemory,
                TweakCategory.NetworkOptimization => TweakCategoryGroup.NetworkLatency,
                TweakCategory.InputAndLatency => TweakCategoryGroup.InputPeripherals,
                TweakCategory.GamingPerformance => TweakCategoryGroup.GamingDirectX,
                TweakCategory.WindowsServices => TweakCategoryGroup.ServicesDebloat,
                TweakCategory.BackgroundProcesses => TweakCategoryGroup.TelemetryPrivacy,
                TweakCategory.SecurityAndPrivacy => TweakCategoryGroup.TelemetryPrivacy,
                TweakCategory.StorageOptimization => TweakCategoryGroup.StorageNvme,
                TweakCategory.PowerManagement => TweakCategoryGroup.PowerMaintenance,
                TweakCategory.SystemMaintenance => TweakCategoryGroup.PowerMaintenance,
                TweakCategory.WindowsPerformance => TweakCategoryGroup.WindowsPerformanceUi,
                TweakCategory.WindowsExplorerAndUi => TweakCategoryGroup.WindowsPerformanceUi,
                TweakCategory.AdvancedTweaks => tweakId.StartsWith("adv_spectre", StringComparison.OrdinalIgnoreCase) ||
                                               tweakId.StartsWith("adv_hpet", StringComparison.OrdinalIgnoreCase) ||
                                               tweakId.StartsWith("adv_critical", StringComparison.OrdinalIgnoreCase) ||
                                               tweakId.StartsWith("adv_fast_fail", StringComparison.OrdinalIgnoreCase) ||
                                               tweakId.StartsWith("adv_executive", StringComparison.OrdinalIgnoreCase) ||
                                               tweakId.StartsWith("adv_win32", StringComparison.OrdinalIgnoreCase) ||
                                               tweakId.StartsWith("adv_device_latency", StringComparison.OrdinalIgnoreCase)
                    ? TweakCategoryGroup.CpuSchedulers
                    : (tweakId.StartsWith("adv_dwm", StringComparison.OrdinalIgnoreCase)
                        ? TweakCategoryGroup.GpuGraphics
                        : (tweakId.StartsWith("adv_heap", StringComparison.OrdinalIgnoreCase) ||
                           tweakId.StartsWith("adv_system_pages", StringComparison.OrdinalIgnoreCase) ||
                           tweakId.StartsWith("adv_session_pool", StringComparison.OrdinalIgnoreCase) ||
                           tweakId.StartsWith("adv_paging", StringComparison.OrdinalIgnoreCase) ||
                           tweakId.StartsWith("adv_kernel_pool", StringComparison.OrdinalIgnoreCase)
                            ? TweakCategoryGroup.RamMemory
                            : TweakCategoryGroup.CpuSchedulers)),
                _ => TweakCategoryGroup.WindowsPerformanceUi
            };
        }

        if (tweakId.StartsWith("cpu_") || tweakId.StartsWith("adv_")) return TweakCategoryGroup.CpuSchedulers;
        if (tweakId.StartsWith("gpu_")) return TweakCategoryGroup.GpuGraphics;
        if (tweakId.StartsWith("ram_")) return TweakCategoryGroup.RamMemory;
        if (tweakId.StartsWith("net_")) return TweakCategoryGroup.NetworkLatency;
        if (tweakId.StartsWith("input_")) return TweakCategoryGroup.InputPeripherals;
        if (tweakId.StartsWith("gaming_")) return TweakCategoryGroup.GamingDirectX;
        if (tweakId.StartsWith("svc_")) return TweakCategoryGroup.ServicesDebloat;
        if (tweakId.StartsWith("sec_") || tweakId.StartsWith("bg_")) return TweakCategoryGroup.TelemetryPrivacy;
        if (tweakId.StartsWith("storage_")) return TweakCategoryGroup.StorageNvme;
        if (tweakId.StartsWith("power_") || tweakId.StartsWith("maint_")) return TweakCategoryGroup.PowerMaintenance;
        return TweakCategoryGroup.WindowsPerformanceUi;
    }

    private static TweakTag InferTagsForCategory(TweakCategory category)
    {
        return category switch
        {
            TweakCategory.CpuOptimization => TweakTag.CpuThrottling | TweakTag.TimerResolution,
            TweakCategory.GpuOptimization => TweakTag.GpuCore | TweakTag.DwmDirectFlip,
            TweakCategory.RamOptimization => TweakTag.MemoryClean | TweakTag.MemoryStandby,
            TweakCategory.GamingPerformance => TweakTag.GameMode | TweakTag.DwmDirectFlip,
            TweakCategory.WindowsPerformance => TweakTag.SystemHygiene | TweakTag.DwmDirectFlip,
            TweakCategory.InputAndLatency => TweakTag.Input,
            TweakCategory.NetworkOptimization => TweakTag.Network,
            TweakCategory.StorageOptimization => TweakTag.StorageTune,
            TweakCategory.BackgroundProcesses => TweakTag.TelemetryPrivacy | TweakTag.MemoryClean,
            TweakCategory.WindowsServices => TweakTag.ServicesDebloat | TweakTag.MemoryClean,
            TweakCategory.PowerManagement => TweakTag.PowerPlan | TweakTag.CpuThrottling,
            TweakCategory.WindowsExplorerAndUi => TweakTag.SystemHygiene,
            TweakCategory.SecurityAndPrivacy => TweakTag.TelemetryPrivacy,
            TweakCategory.SystemMaintenance => TweakTag.SystemHygiene | TweakTag.StorageTune,
            TweakCategory.AdvancedTweaks => TweakTag.TimerResolution | TweakTag.FrametimeSmooth,
            _ => TweakTag.SystemHygiene
        };
    }

    private static int GetGroupMinQuota(TweakCategoryGroup group, int riskIndex, int scenarioIndex, int priorityIndex)
    {
        return (group, riskIndex) switch
        {
            (TweakCategoryGroup.CpuSchedulers, 0) => scenarioIndex == 0 || priorityIndex == 1 ? 6 : 5,
            (TweakCategoryGroup.CpuSchedulers, 1) => scenarioIndex == 0 || priorityIndex == 1 ? 12 : 10,
            (TweakCategoryGroup.CpuSchedulers, _) => scenarioIndex == 0 || priorityIndex == 1 ? 15 : 13,

            (TweakCategoryGroup.GpuGraphics, 0) => scenarioIndex == 1 || priorityIndex == 1 ? 6 : 5,
            (TweakCategoryGroup.GpuGraphics, 1) => scenarioIndex == 1 || priorityIndex == 1 ? 12 : 10,
            (TweakCategoryGroup.GpuGraphics, _) => scenarioIndex == 1 || priorityIndex == 1 ? 15 : 13,

            (TweakCategoryGroup.RamMemory, 0) => priorityIndex == 3 || priorityIndex == 2 ? 5 : 4,
            (TweakCategoryGroup.RamMemory, 1) => priorityIndex == 3 || priorityIndex == 2 ? 9 : 8,
            (TweakCategoryGroup.RamMemory, _) => priorityIndex == 3 || priorityIndex == 2 ? 12 : 10,

            (TweakCategoryGroup.NetworkLatency, 0) => scenarioIndex == 0 || scenarioIndex == 2 ? 6 : 5,
            (TweakCategoryGroup.NetworkLatency, 1) => scenarioIndex == 0 || scenarioIndex == 2 ? 14 : 11,
            (TweakCategoryGroup.NetworkLatency, _) => scenarioIndex == 0 || scenarioIndex == 2 ? 18 : 15,

            (TweakCategoryGroup.InputPeripherals, 0) => scenarioIndex == 0 || priorityIndex == 0 ? 6 : 5,
            (TweakCategoryGroup.InputPeripherals, 1) => scenarioIndex == 0 || priorityIndex == 0 ? 11 : 9,
            (TweakCategoryGroup.InputPeripherals, _) => scenarioIndex == 0 || priorityIndex == 0 ? 14 : 12,

            (TweakCategoryGroup.GamingDirectX, 0) => scenarioIndex == 1 || scenarioIndex == 0 ? 6 : 5,
            (TweakCategoryGroup.GamingDirectX, 1) => scenarioIndex == 1 || scenarioIndex == 0 ? 13 : 11,
            (TweakCategoryGroup.GamingDirectX, _) => scenarioIndex == 1 || scenarioIndex == 0 ? 16 : 14,

            (TweakCategoryGroup.ServicesDebloat, 0) => scenarioIndex == 3 || priorityIndex == 3 ? 5 : 4,
            (TweakCategoryGroup.ServicesDebloat, 1) => scenarioIndex == 3 || priorityIndex == 3 ? 12 : 10,
            (TweakCategoryGroup.ServicesDebloat, _) => scenarioIndex == 3 || priorityIndex == 3 ? 20 : 16,

            (TweakCategoryGroup.TelemetryPrivacy, 0) => scenarioIndex == 3 || priorityIndex == 3 ? 6 : 5,
            (TweakCategoryGroup.TelemetryPrivacy, 1) => scenarioIndex == 3 || priorityIndex == 3 ? 13 : 11,
            (TweakCategoryGroup.TelemetryPrivacy, _) => scenarioIndex == 3 || priorityIndex == 3 ? 22 : 17,

            (TweakCategoryGroup.StorageNvme, 0) => scenarioIndex == 1 ? 5 : 4,
            (TweakCategoryGroup.StorageNvme, 1) => scenarioIndex == 1 ? 9 : 7,
            (TweakCategoryGroup.StorageNvme, _) => scenarioIndex == 1 ? 12 : 10,

            (TweakCategoryGroup.PowerMaintenance, 0) => scenarioIndex == 1 ? 5 : 4,
            (TweakCategoryGroup.PowerMaintenance, 1) => scenarioIndex == 1 ? 9 : 7,
            (TweakCategoryGroup.PowerMaintenance, _) => scenarioIndex == 1 ? 12 : 10,

            (TweakCategoryGroup.WindowsPerformanceUi, 0) => 4,
            (TweakCategoryGroup.WindowsPerformanceUi, 1) => 8,
            (TweakCategoryGroup.WindowsPerformanceUi, _) => 13,

            _ => 5
        };
    }

    private sealed record CandidateTweakItem(
        string Id,
        string RuRationale,
        string EnRationale,
        string RiskDesc,
        TweakTag Tags,
        Func<HardwareProfile, bool>? HardwareFilter = null
    );

    public LiquidAiResult GenerateExpertProfile(
        HardwareProfile hardware,
        int scenarioIndex,
        int riskIndex,
        int priorityIndex,
        string scenarioName,
        string riskName,
        string priorityName,
        IReadOnlyList<TweakDefinition> allTweaks,
        bool isRu)
    {
        var maxRisk = riskIndex switch
        {
            0 => RiskLevel.Low,
            1 => RiskLevel.Medium,
            _ => RiskLevel.High
        };

        bool isLaptop = hardware.Power.IsLaptop;
        bool hasSsd = hardware.Storage.Any(s => s.IsSsd);
        var cpu = hardware.Cpu;
        var gpu = hardware.PrimaryGpu;
        var ram = hardware.Ram;

        // Filter compatible candidates by risk and hardware capabilities
        var compatibleDefMap = allTweaks.Where(t =>
        {
            if (t.Risk > maxRisk) return false;
            if (t.RequiresIntelCpu && !cpu.IsIntel) return false;
            if (t.RequiresAmdCpu && !cpu.IsAmd) return false;
            if (t.RequiresNvidiaGpu && !gpu.IsNvidia) return false;
            if (t.RequiresAmdGpu && !gpu.IsAmd) return false;
            if (t.RequiresIntelGpu && !gpu.IsIntel) return false;
            if (t.DesktopOnly && isLaptop) return false;
            if (t.LaptopOnly && !isLaptop) return false;
            if (t.RequiresSsdOrNvme && !hasSsd) return false;
            if (t.MinWindowsBuild > 0 && hardware.Os.BuildNumber < t.MinWindowsBuild) return false;
            return true;
        }).ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);

        var catalog = new List<CandidateTweakItem>
        {
            // =========================================================================
            // 1. CPU OPTIMIZATION & SCHEDULING
            // =========================================================================
            new("cpu_power_throttling_disable",
                $"ИИ: Для процессора {cpu.Name} отключено системное ограничение частот Power Throttling, предотвращающее сброс частот при резких игровых нагрузках.",
                $"AI: Disabled execution power throttling for {cpu.Name}, preventing frequency drops during sudden intense gaming spikes.",
                "Safe / CPU Focus",
                TweakTag.CpuThrottling),

            new("cpu_core_parking_unhide",
                $"ИИ: Для архитектуры {cpu.Name} ({cpu.CoreCount} ядер / {cpu.LogicalProcessorCount} потоков) разблокировано отключение парковки ядер, сохраняя все физические ядра активными.",
                $"AI: Unlocked core unparking for {cpu.Name} ({cpu.CoreCount}C/{cpu.LogicalProcessorCount}T), keeping all physical cores active and warm.",
                "Safe / Latency",
                TweakTag.CpuThrottling),

            new("cpu_mmcss_game_tasks",
                "ИИ: Настроен системный планировщик MMCSS с максимальным приоритетом игровых потоков рендера и графической квотой GPU Priority = 8.",
                "AI: Configured MMCSS gaming tasks with high thread priority and dedicated GPU Priority = 8 for render threads.",
                "Safe / System",
                TweakTag.StreamingObs | TweakTag.FrametimeSmooth | TweakTag.AudioLatency),

            new("cpu_distribute_timers",
                $"ИИ: Для процессора {cpu.Name} активировано распределение системных таймеров по всем {cpu.CoreCount} ядрам CPU вместо перегрузки ядра 0.",
                $"AI: Enabled kernel timer distribution across all {cpu.CoreCount} CPU cores instead of saturating Core 0, eliminating DPC latency spikes.",
                "Safe / System",
                TweakTag.TimerResolution | TweakTag.FrametimeSmooth),

            new("cpu_additional_worker_threads",
                "ИИ: Выделены дополнительные системные потоки ядра (16 worker threads) для параллельной обработки очередей дискового ввода-вывода и античита.",
                "AI: Allocated additional critical kernel worker threads to process asynchronous I/O and anticheat packets without queue congestion.",
                "Safe / Recommended",
                TweakTag.CpuThrottling | TweakTag.StreamingObs),

            new("cpu_dxgkrnl_latency_tolerance",
                "ИИ: Для ядра DirectX Graphics Kernel (dxgkrnl.sys) установлен нулевой порог толерантности к задержкам смены дисплейных кадров.",
                "AI: Configured DirectX Graphics Kernel with zero latency tolerance for immediate frame display flips.",
                "Safe / Latency",
                TweakTag.DwmDirectFlip | TweakTag.Input),

            new("cpu_affinity_prioritization_foreground",
                "ИИ: Закреплен максимальный квант вычислительной мощности и аппаратный приоритет CPU за активным игровым окном.",
                "AI: Enforces maximum CPU throughput and scheduling affinity strictly for the active foreground game.",
                "Safe / Recommended",
                TweakTag.CpuThrottling | TweakTag.StreamingObs),

            new("cpu_svm_hyperv_interrupt_steering",
                "ИИ: Включен высокоточный учет циклов прерываний ядра для равномерного распределения аппаратных прерываний между физическими ядрами.",
                "AI: Enabled precision interrupt cycle counter accounting to distribute interrupt loads evenly across physical CPU cores.",
                "Safe / Recommended",
                TweakTag.CpuThrottling | TweakTag.FrametimeSmooth),

            new("cpu_amd_ppm_perf_boost",
                $"ИИ: Для процессора {cpu.Name} (AMD Ryzen) активирован агрессивный профиль Core Performance Boost без задержек выхода из C-states.",
                $"AI: For {cpu.Name} (AMD Ryzen), enabled aggressive Core Performance Boost profile with instant burst frequency scaling.",
                "Safe / AMD Ryzen",
                TweakTag.CpuThrottling,
                hw => hw.Cpu.IsAmd),

            new("cpu_amd_thread_dpc_affinity",
                "ИИ: Для архитектуры AMD Zen зафиксирована обработка DPC-прерываний на исходном ядре, предотвращая межъядерные задержки шины Infinity Fabric.",
                "AI: For AMD Zen architecture, pinned DPC interrupt handling to origin core to eliminate cross-CCD cache invalidation latency.",
                "Medium / AMD Ryzen",
                TweakTag.CpuThrottling | TweakTag.FrametimeSmooth,
                hw => hw.Cpu.IsAmd),

            new("cpu_intel_speedshift_unhide",
                $"ИИ: Для процессора {cpu.Name} (Intel Core) разблокирован профиль Intel Speed Shift (HWP) Maximum Performance с временем отклика 0 мс.",
                $"AI: For {cpu.Name} (Intel Core), unlocked Intel Speed Shift (HWP) Maximum Performance profile for 0ms clock scaling.",
                "Safe / Intel Core",
                TweakTag.CpuThrottling,
                hw => hw.Cpu.IsIntel),

            new("cpu_intel_ppm_epb_performance",
                "ИИ: В драйвере intelppm установлен приоритет энергоэффективности EPB = 0 (максимальная производительность) без задержек Turbo Boost.",
                "AI: Configured Intel Energy Performance Bias (EPB) to absolute maximum performance, eliminating Turbo clock ramp-up delay.",
                "Safe / Intel Core",
                TweakTag.CpuThrottling,
                hw => hw.Cpu.IsIntel),

            new("cpu_thread_scheduling_model_unhide",
                $"ИИ: Для гибридной архитектуры {cpu.Name} разблокировано управление шедулером для жесткого закрепления игр на производительных P-ядрах.",
                $"AI: For hybrid {cpu.Name}, exposed heterogeneous thread scheduling policy to lock game threads strictly onto high-frequency P-cores.",
                "Safe / Intel Hybrid",
                TweakTag.CpuThrottling,
                hw => hw.Cpu.IsIntel && hw.Cpu.CoreCount >= 8),

            new("cpu_idle_resiliency_disable",
                "ИИ: Для стационарного ПК отключена группировка таймеров CoalescingTimerInterval, обеспечивая непрерывную частоту тиков ядра.",
                "AI: Disabled timer coalescing on desktop PC, guaranteeing high-resolution timer ticks for game physics and polling.",
                "Safe / Desktop",
                TweakTag.TimerResolution | TweakTag.Input,
                hw => !hw.Power.IsLaptop),

            new("cpu_win32_priority_separation",
                "ИИ: Оптимизирован квант времени Win32PrioritySeparation (0x26 = 38) для субмиллисекундного отклика цикла рендера и ввода.",
                "AI: Optimized Win32PrioritySeparation quanta (0x26) for sub-millisecond game loop execution and input responsiveness.",
                "Medium / Recommended",
                TweakTag.CpuThrottling | TweakTag.Input | TweakTag.StreamingObs),

            new("cpu_kernel_dpc_watchdog_profile",
                "ИИ: Установлен минимальный порог сброса очереди DPC (глубина 1) в ядре Windows для устранения задержек звука и ввода.",
                "AI: Clamped kernel DPC queue depth to 1 for immediate queue draining, lowering interrupt latency.",
                "Medium / Recommended",
                TweakTag.FrametimeSmooth | TweakTag.Input),

            // =========================================================================
            // 2. GPU OPTIMIZATION & GRAPHICS PIPELINE
            // =========================================================================
            new("gpu_hags_enable",
                $"ИИ: Для видеокарты {gpu.Name} активирован аппаратный планировщик HAGS, разгружающий CPU от диспетчеризации VRAM и снижающий Render Latency.",
                $"AI: Enabled Hardware-Accelerated GPU Scheduling (HAGS) for {gpu.Name}, offloading VRAM management from CPU and lowering render latency.",
                "Recommended / Low Risk",
                TweakTag.GpuCore | TweakTag.GameMode),

            new("gpu_variable_refresh_rate_global",
                "ИИ: Включена глобальная поддержка переменной частоты обновления (VRR) для всех оконных и borderless-игр без тиринга.",
                "AI: Enabled global Variable Refresh Rate (VRR) for all borderless and windowed games without tearing.",
                "Safe / Display",
                TweakTag.GpuCore),

            new("gpu_borderless_direct_flip_optimization",
                "ИИ: Активирован аппаратный DirectFlip в GameConfigStore, обеспечивающий задержку эксклюзивного полного экрана в режиме без полей.",
                "AI: Activated hardware DirectFlip in GameConfigStore, delivering true Fullscreen Exclusive latency in borderless windowed mode.",
                "Safe / Latency",
                TweakTag.DwmDirectFlip | TweakTag.Input),

            new("gpu_dwm_high_priority",
                "ИИ: Для очереди Desktop Window Manager установлен приоритет GPU Priority = 8 для безупречной плавности вывода кадров.",
                "AI: Assigned high GPU Priority = 8 to Desktop Window Manager display flip queue for hitch-free composition.",
                "Safe / DWM",
                TweakTag.DwmDirectFlip | TweakTag.FrametimeSmooth),

            new("gpu_dx_prefer_high_performance",
                $"ИИ: В DirectX принудительно назначен приоритет высокопроизводительного дискретного адаптера {gpu.Name} для всех 3D-приложений.",
                $"AI: Enforced default DirectX preference to high-performance discrete adapter {gpu.Name} across all 3D applications.",
                "Safe / GPU Direct",
                TweakTag.GpuCore),

            new("gpu_tdr_delay_extend",
                "ИИ: Время ожидания сторожевого таймера драйвера TDR увеличено до 10 секунд, предотвращая вылеты видеодрайвера при тяжелой загрузке шейдеров в UE5.",
                "AI: Extended GPU driver TDR recovery watchdog to 10s, preventing display driver timeouts during heavy UE5 shader loading.",
                "Safe / Stability",
                TweakTag.GpuCore | TweakTag.FrametimeSmooth),

            new("gpu_dxgi_swapchain_waitable_object",
                "ИИ: Включена низколатентная обработка очередей DXGI SwapEffectWaitable, снижающая задержку ввода от клика до фотона на 1 кадр.",
                "AI: Enforced DXGI low-latency waitable object swapchain heuristics, cutting click-to-photon latency by up to 1 frame cycle.",
                "Safe / Latency",
                TweakTag.DwmDirectFlip | TweakTag.Input | TweakTag.FrametimeSmooth),

            new("gpu_vram_allocation_priority",
                $"ИИ: Для {gpu.FormattedVram} видеопамяти {gpu.Name} заблокирован преждевременный сброс текстур в общую медленную память ОЗУ.",
                $"AI: Prioritized dedicated {gpu.FormattedVram} video memory for {gpu.Name}, preventing early texture spilling onto shared system RAM.",
                "Safe / GPU Memory",
                TweakTag.GpuCore),

            new("gpu_dwm_composition_buffer_count",
                "ИИ: Количество буферов композитора DWM сокращено с тройного до двойного, срезая до 16 мс задержки в оконных режимах.",
                "AI: Reduced DWM composition buffering from triple to double buffer, cutting up to 16ms presentation lag in windowed mode.",
                "Safe / Input Lag",
                TweakTag.DwmDirectFlip | TweakTag.Input),

            new("gpu_nvidia_power_mizer_max",
                $"ИИ: Для видеокарты {gpu.Name} активирован режим PowerMizer 'Максимальная производительность' с фиксацией 3D-частот ядра и памяти.",
                $"AI: For {gpu.Name}, locked NVIDIA PowerMizer into Maximum Performance, preventing core clock throttling during low-GPU scenes.",
                "Safe / NVIDIA GPU",
                TweakTag.GpuCore,
                hw => hw.PrimaryGpu.IsNvidia),

            new("gpu_nvidia_low_latency_mode",
                "ИИ: В драйвере NVIDIA зафиксирован лимит очереди кадров PreRenderedFrames = 1 для мгновенной реакции прицела на движения мыши.",
                "AI: Enforced NVIDIA PreRenderedFrames = 1 low-latency limit for instant crosshair responsiveness in shooters.",
                "Safe / Low Latency",
                TweakTag.GpuCore | TweakTag.Input,
                hw => hw.PrimaryGpu.IsNvidia),

            new("gpu_nvidia_telemetry_container_disable",
                "ИИ: Отключены фоновые службы телеметрии NVIDIA NvTelemetry, устраняя микрофризы от постоянной записи логов на диск.",
                "AI: Disabled NVIDIA background telemetry reporting hooks to eliminate disk write spikes and thread wakeups.",
                "Safe / Privacy",
                TweakTag.TelemetryPrivacy | TweakTag.MemoryClean,
                hw => hw.PrimaryGpu.IsNvidia),

            new("gpu_nvidia_ansel_overlay_disable",
                "ИИ: Отключен инжектор оверлея NVIDIA Ansel, предотвращая постороннее внедрение библиотек в процессы игр.",
                "AI: Disabled NVIDIA Ansel in-game camera injection hooks, avoiding DLL injection overhead into game threads.",
                "Safe / Stability",
                TweakTag.GpuCore | TweakTag.StreamingObs,
                hw => hw.PrimaryGpu.IsNvidia),

            new("gpu_amd_anti_lag_profile",
                $"ИИ: Для {gpu.Name} активирован профиль AMD Radeon Anti-Lag, синхронизирующий темп работы CPU и GPU для снижения лага.",
                $"AI: Activated AMD Radeon Anti-Lag driver policy for {gpu.Name}, synchronizing CPU-GPU rendering pace to cut input lag.",
                "Safe / AMD Radeon",
                TweakTag.GpuCore | TweakTag.Input,
                hw => hw.PrimaryGpu.IsAmd),

            new("gpu_amd_shader_cache_limit",
                $"ИИ: Для {gpu.Name} включено постоянное удержание кэша шейдеров без очистки, устраняя статтеры компиляции шейдеров в DX12.",
                $"AI: Enabled unlimited shader cache retention for {gpu.Name}, preventing first-encounter PSO compilation stutters.",
                "Safe / AMD Radeon",
                TweakTag.GpuCore | TweakTag.FrametimeSmooth,
                hw => hw.PrimaryGpu.IsAmd),

            new("gpu_amd_ulps_disable",
                "ИИ: Отключен режим глубокого энергосбережения AMD ULPS, исключая задержки при переходе чипа из сна в боевой режим.",
                "AI: Disabled AMD Radeon Ultra Low Power State (ULPS) to ensure immediate GPU clock ramp-up and stability.",
                "Safe / AMD Radeon",
                TweakTag.GpuCore,
                hw => hw.PrimaryGpu.IsAmd),

            new("gpu_intel_arc_adaptive_sync",
                $"ИИ: Для {gpu.Name} активирована глобальная синхронизация Intel Arc Adaptive Sync для устранения разрывов кадров.",
                $"AI: Activated Intel Arc Adaptive Sync global policy for smooth frame delivery on {gpu.Name}.",
                "Safe / Intel Arc",
                TweakTag.GpuCore,
                hw => hw.PrimaryGpu.IsIntel),

            // =========================================================================
            // 3. RAM & MEMORY ARCHITECTURE
            // =========================================================================
            new("ram_disable_paging_executive",
                $"ИИ: Для пула памяти {ram.TotalGb:F0} ГБ ядро Windows и драйверы заблокированы в физической ОЗУ (DisablePagingExecutive = 1), исключая подгрузки с диска.",
                $"AI: Pinned Windows kernel and drivers into physical RAM ({ram.TotalGb:F0}GB pool) via DisablePagingExecutive = 1 to eliminate page-fault stutters.",
                "Safe / Memory",
                TweakTag.MemoryClean | TweakTag.FrametimeSmooth),

            new("ram_clear_pagefile_shutdown_disable",
                "ИИ: Отключена очистка файла подкачки при выключении ПК, ускоряя перезагрузку и сберегая ресурс ячеек памяти SSD.",
                "AI: Disabled pagefile scrubbing at shutdown, enabling instant reboots and preserving SSD write endurance.",
                "Safe / Storage Life",
                TweakTag.StorageTune | TweakTag.SystemHygiene),

            new("ram_system_responsiveness_zero",
                "ИИ: Зарезервированный бюджет SystemResponsiveness снижен до 0%, отдавая 100% вычислительных ресурсов и памяти активной игре.",
                "AI: Dedicated 100% of CPU/memory budget to the active game process by setting SystemResponsiveness to 0%.",
                "Safe / 100% Budget",
                TweakTag.GameMode | TweakTag.CpuThrottling),

            new("ram_large_system_cache_gaming",
                "ИИ: Оптимизирован системный файловый кэш (LargeSystemCache = 0), предотвращая разрастание кэша и сохраняя ОЗУ для игровых текстур.",
                "AI: Optimized system file cache policy (LargeSystemCache = 0) to ensure maximum physical memory remains free for games.",
                "Safe / Cache",
                TweakTag.MemoryClean),

            new("ram_paging_file_encryption_disable",
                "ИИ: Отключено фоновое шифрование страниц файла подкачки, экономя такты CPU при обращении к виртуальной памяти.",
                "AI: Disabled real-time pagefile encryption overhead to eliminate CPU encryption penalties on memory pages.",
                "Safe / CPU Overhead",
                TweakTag.CpuThrottling | TweakTag.MemoryClean),

            new("ram_paging_executive_driver_lock",
                "ИИ: Установлена блокировка выгрузки критических драйверов видеокарты и звука при высокой нагрузке на память.",
                "AI: Enforced kernel driver paging lock to prevent GPU and audio driver routines from paging out under high memory load.",
                "Safe / Stability",
                TweakTag.FrametimeSmooth),

            new("ram_standby_list_trimming_heuristics",
                $"ИИ: Для {ram.TotalGb:F0} ГБ памяти включена проактивная очистка кэша ожидания Standby List, устраняющая микрозаикания при нехватке памяти.",
                $"AI: Enabled proactive Standby List cache trimming for {ram.TotalGb:F0}GB RAM to prevent memory-allocation micro-stuttering.",
                "Safe / Memory",
                TweakTag.MemoryStandby | TweakTag.MemoryClean | TweakTag.FrametimeSmooth),

            new("ram_working_set_trim_protection",
                "ИИ: Включена защита активного рабочего набора (Working Set) игры от принудительной выгрузки при переключении окон.",
                "AI: Protected game working set from background memory trimming, ensuring instant Alt-Tab recovery without reloading pauses.",
                "Safe / Multi-Task",
                TweakTag.MemoryStandby | TweakTag.StreamingObs | TweakTag.FrametimeSmooth),

            new("ram_paged_pool_quota_size",
                "ИИ: Расширен виртуальный пул PagedPoolSize для стабильной многозадачности с играми, Discord и браузером.",
                "AI: Expanded kernel paged pool address space quota for reliable multi-tasking alongside Discord, browser, and games.",
                "Safe / Pool",
                TweakTag.StreamingObs | TweakTag.SystemHygiene),

            new("ram_heap_segment_allocation_cache",
                "ИИ: Активировано ускорение блочного кэша Segment Heap для быстрого спавна объектов в движках UE5 и Unity.",
                "AI: Accelerated segment heap cache allocation to reduce allocator lock contention in modern C++/Rust game engines.",
                "Safe / Heap",
                TweakTag.FrametimeSmooth | TweakTag.GpuCore),

            new("ram_worker_thread_stack_commit",
                "ИИ: Оптимизирован стартовый стек системных потоков (64 КБ), предотвращая задержки расширения стека.",
                "AI: Optimized thread stack initial commit to 64KB, eliminating runtime stack page-fault expansions.",
                "Safe / Threads",
                TweakTag.CpuThrottling | TweakTag.FrametimeSmooth),

            new("ram_prefetch_ssd_gaming_tune",
                "ИИ: Для накопителя SSD/NVMe отключены устаревшие алгоритмы Prefetch и Superfetch, исключая фрагментацию ОЗУ и лишние записи.",
                "AI: Disabled legacy HDD Prefetcher & Superfetch on SSD/NVMe storage to prevent RAM fragmentation and background writes.",
                "Safe / SSD Tune",
                TweakTag.MemoryClean | TweakTag.StorageTune,
                hw => hw.Storage.Any(s => s.IsSsd)),

            new("ram_svchost_split_threshold",
                $"ИИ: Для системы с {ram.TotalGb:F0} ГБ ОЗУ установлен порог группировки служб SvcHostSplitThreshold (32 ГБ), освобождая сотни МБ памяти.",
                $"AI: Configured SvcHostSplitThreshold (32GB) for your {ram.TotalGb:F0}GB RAM PC, grouping services and freeing hundreds of megabytes.",
                "Medium / RAM Free",
                TweakTag.MemoryClean | TweakTag.ServicesDebloat),

            new("ram_system_cache_dirty_page_limit",
                "ИИ: Ограничен объем грязных страниц файлового кэша, предотвращая просадки шины памяти при залповой записи на диск.",
                "AI: Clamped dirty file cache pages before disk flush to prevent memory bus saturation during heavy background writes.",
                "Medium / Bus Bandwidth",
                TweakTag.FrametimeSmooth | TweakTag.StorageTune),

            // =========================================================================
            // 4. INPUT & LATENCY
            // =========================================================================
            new("input_mouse_acceleration_disable",
                "ИИ: Отключено программное ускорение указателя Enhanced Pointer Precision, гарантируя честное 1:1 соотношение движения сенсора мыши.",
                "AI: Completely disabled Windows mouse acceleration (Enhanced Pointer Precision) for pure, deterministic 1:1 sensor tracking.",
                "Safe / 1:1 Raw Input",
                TweakTag.Input),

            new("input_keyboard_repeat_rate_max",
                "ИИ: Скорость автоповтора клавиш установлена на максимум (KeyboardSpeed = 31) для мгновенного отклика при удержании кнопок стрейфа.",
                "AI: Maximized keyboard repeat rate (31 cps) for instant responsiveness when holding movement and action keys.",
                "Safe / Keyboard",
                TweakTag.Input),

            new("input_keyboard_repeat_delay_min",
                "ИИ: Начальная задержка повтора клавиш сведена к минимуму (KeyboardDelay = 0) для мгновенной реакции в соревновательных играх.",
                "AI: Minimized keyboard initial repeat delay to 0 for instant key-repeat actuation in competitive gaming.",
                "Safe / Keyboard",
                TweakTag.Input),

            new("input_usb_selective_suspend_disable",
                "ИИ: Отключено выборочное энергосбережение USB-портов (DisableSelectiveSuspend = 1), поддерживая стабильный опрос мыши 1000–8000 Гц.",
                "AI: Disabled USB Selective Suspend on USB controllers to guarantee uninterrupted 1000Hz+ mouse and keyboard polling.",
                "Safe / USB Power",
                TweakTag.Input | TweakTag.PowerPlan),

            new("input_cursor_shadow_disable",
                "ИИ: Отключена альфа-тень курсора мыши, исключая лишний проход композитинга DWM при резких движениях мыши.",
                "AI: Disabled software cursor drop shadow to remove an unnecessary GDI/DWM compositing pass during quick mouse flicks.",
                "Safe / Visual Latency",
                TweakTag.Input | TweakTag.DwmDirectFlip),

            new("input_mouse_hover_time_decrease",
                "ИИ: Задержка наведения курсора MouseHoverTime снижена с 400 мс до 10 мс для моментального открытия подсказок в инвентаре.",
                "AI: Reduced mouse hover time to 10ms for instant inventory tooltip displays and responsive UI navigation.",
                "Safe / UI Speed",
                TweakTag.Input | TweakTag.SystemHygiene),

            new("input_hid_driver_polling_priority",
                "ИИ: Приоритет потока драйвера мыши mouclass повышен до 31 (Realtime) для безупречной обработки прерываний высокочастотных мышей.",
                "AI: Elevated HID mouclass driver thread priority to 31 for ultra-stable interrupt processing on high-polling gaming mice.",
                "Safe / Mouse Queue",
                TweakTag.Input),

            new("input_mouse_data_queue_size",
                "ИИ: Очередь буфера мыши mouclass расширена до 200 пакетов, предотвращая пропуски координат на мышах 1000/4000/8000 Гц.",
                "AI: Expanded mouse packet buffer queue to 200 packets to prevent coordinate dropping on 1000Hz–8000Hz mice.",
                "Safe / High Polling",
                TweakTag.Input),

            new("input_keyboard_data_queue_size",
                "ИИ: Очередь буфера клавиатуры расширена до 200 пакетов, исключая потерю нажатий при высоком APM.",
                "AI: Expanded keyboard driver packet queue to 200 packets to eliminate dropped keystrokes during high APM gameplay.",
                "Safe / High APM",
                TweakTag.Input),

            new("input_csgo_raw_input_buffer_coalescing",
                "ИИ: Отключено объединение сырых пакетов ввода CoalesceInput, устраняя скрытую задержку мыши 4–8 мс.",
                "AI: Disabled raw input WM_INPUT message coalescing, eliminating hidden 4-8ms mouse packet buffering delays.",
                "Safe / Competitive",
                TweakTag.Input),

            new("input_background_raw_input_lock",
                "ИИ: Задержка захвата фокуса мыши ForegroundLockTimeout сведена к 0 для мгновенного отклика при возврате в окно игры.",
                "AI: Set ForegroundLockTimeout to 0 for instantaneous window focus acquisition and mouse capture.",
                "Safe / Window Focus",
                TweakTag.Input),

            new("input_sticky_keys_prompt_disable",
                "ИИ: Отключено модальное окно залипания клавиш StickyKeys при 5-кратном нажатии Shift, исключая сворачивание игры во время боя.",
                "AI: Disabled the disruptive StickyKeys 5x Shift popup dialog, preventing accidental game minimization during frantic combat.",
                "Safe / Quality of Life",
                TweakTag.Input | TweakTag.SystemHygiene),

            new("input_filter_keys_disable",
                "ИИ: Отключен фильтр коротких нажатий FilterKeys, гарантируя регистрацию быстрых микротапов при контр-стрейфах.",
                "AI: Disabled FilterKeys delay to ensure instant, guaranteed registration of rapid micro-taps and counter-strafes.",
                "Safe / Keyboard",
                TweakTag.Input),

            new("input_pointer_trails_force_disable",
                "ИИ: Отключены программные шлейфы курсора, сохраняя аппаратный GPU-конвейер отображения курсора без задержки.",
                "AI: Disabled software pointer trails to preserve hardware GPU cursor plane rendering without composition delay.",
                "Safe / GPU Cursor",
                TweakTag.Input | TweakTag.DwmDirectFlip),

            new("input_gamepad_xinput_polling_latency",
                "ИИ: Интервал опроса геймпадов XInput снижен до 1 мс, обеспечивая мгновенный отклик стиков и триггеров контроллера.",
                "AI: Clamped XInput controller polling interval to 1ms for snappier analog stick and trigger actuation.",
                "Safe / Gamepad",
                TweakTag.Input),

            // =========================================================================
            // 5. GAMING & DIRECTX PERFORMANCE
            // =========================================================================
            new("gaming_game_mode_enable",
                "ИИ: Активирован игровой режим Windows Game Mode для изоляции игрового процесса от фоновых обновлений и фоновых служб.",
                "AI: Activated Windows Game Mode to prioritize game threads and suppress background Windows Update activity.",
                "Safe / Core Gaming",
                TweakTag.GameMode | TweakTag.GpuCore | TweakTag.CpuThrottling),

            new("gaming_game_dvr_disable",
                $"ИИ: Отключена фоновая запись клипов Game DVR, освобождая ресурсы аппаратного видеоэнкодера на {gpu.Name} и видеопамять.",
                $"AI: Disabled background Game DVR recording, recovering hardware video encoder cycles and VRAM bandwidth on {gpu.Name}.",
                "Safe / Encoder Free",
                TweakTag.StreamingObs | TweakTag.GameMode | TweakTag.GpuCore),

            new("gaming_game_bar_presence_writer_disable",
                "ИИ: Отключен телеметрический процесс GameBarPresenceWriter, устраняя лаги при старте игр через Steam и Epic Games.",
                "AI: Disabled GameBarPresenceWriter background telemetry to eliminate launch stalls in Steam/Epic titles.",
                "Safe / Launch Speed",
                TweakTag.GameMode | TweakTag.TelemetryPrivacy),

            new("gaming_fullscreen_exclusive_fse",
                "ИИ: Включен конвейер DirectFlip (FSEBehavior = 2), обеспечивающий минимальную задержку отображения кадров в полноэкранном и оконном режимах.",
                "AI: Activated DirectFlip presentation queue via GameDVR_FSEBehavior = 2 for lowest display flip latency.",
                "Safe / DirectFlip",
                TweakTag.DwmDirectFlip | TweakTag.GameMode | TweakTag.Input),

            new("gaming_game_bar_nexus_disable",
                "ИИ: Отключены глобальные перехваты комбинаций Win+G и кнопки контроллера Xbox Guide, разгружая очередь системных хоткеев.",
                "AI: Disabled Xbox Game Bar guide button and Win+G hotkey hooks, freeing system input queues.",
                "Safe / Hooks",
                TweakTag.GameMode | TweakTag.Input),

            new("gaming_audio_ducker_disable",
                "ИИ: Отключено автоматическое приглушение звука на 80% (Audio Ducking) при общении в Discord и голосовых чатах.",
                "AI: Disabled automatic Windows Audio Ducking to keep in-game footsteps and cues clearly audible during voice chat.",
                "Safe / Discord Sound",
                TweakTag.StreamingObs | TweakTag.AudioLatency),

            new("gaming_game_dvr_dxgi_compatibility",
                "ИИ: Отключен слой совместимости DXGI Game DVR, исключая промежуточные хуки между движком игры и видеодрайвером.",
                "AI: Bypassed DXGI Game DVR compatibility interception, removing overhead between game engine and display driver.",
                "Safe / Clean DXGI",
                TweakTag.DwmDirectFlip | TweakTag.GpuCore),

            new("gaming_fullscreen_optimizations_global",
                "ИИ: Включена оптимизированная независимая модель презентации WDDM для DirectX 9/11 с поддержкой G-Sync и мгновенного Alt+Tab.",
                "AI: Enabled modern WDDM independent flip presentation for DX9/11 titles with seamless Alt+Tab and VRR support.",
                "Safe / WDDM Flip",
                TweakTag.DwmDirectFlip | TweakTag.GameMode),

            new("gaming_auto_hdr_notification_disable",
                "ИИ: Отключен всплывающий баннер Auto HDR, предотвращающий кратковременную потерю фокуса ввода при запуске игр.",
                "AI: Suppressed Auto HDR notification overlay to prevent mouse focus loss upon game launches.",
                "Safe / Distraction Free",
                TweakTag.GameMode | TweakTag.SystemHygiene),

            new("gaming_game_config_store_flags",
                "ИИ: Оптимизированы флаги GameConfigStore, отключающие ненужные очереди захвата видео и звука.",
                "AI: Optimized GameConfigStore flags to minimize capture buffering and background thread overhead.",
                "Safe / System",
                TweakTag.GameMode | TweakTag.StreamingObs),

            new("gaming_optimizations_for_windowed_games_enable",
                "ИИ: Включены оптимизации Windows 11 для оконных игр (Auto HDR, VRR и DirectFlip для DirectX 10/11).",
                "AI: Enabled Windows 11 Optimizations for Windowed Games for DirectFlip latency and VRR in borderless modes.",
                "Safe / Win11 Feature",
                TweakTag.DwmDirectFlip | TweakTag.GpuCore | TweakTag.GameMode),

            new("gaming_spatial_audio_overhead_disable",
                "ИИ: Отключена цифровая обработка пространственного звука Windows Sonic, минимизируя задержку вывода звука шагов и выстрелов.",
                "AI: Disabled spatial audio HRTF convolution pipeline to deliver in-game audio cues with lowest latency.",
                "Safe / Audio Latency",
                TweakTag.AudioLatency | TweakTag.StreamingObs),

            new("gaming_discord_overlay_hook_mitigation",
                "ИИ: Приоритет графического рендеринга закреплен за основным окном игры при использовании оверлеев Discord и Steam.",
                "AI: Configured graphics hooking policy to prioritize main game render context over external chat overlays.",
                "Safe / Overlay Safe",
                TweakTag.StreamingObs | TweakTag.GpuCore),

            new("gaming_disable_game_bar_tips",
                "ИИ: Отключены подсказки контроллера и уведомления Xbox Game Bar для исключения отвлекающих баннеров.",
                "AI: Disabled Xbox Game Bar controller tips and toasts for clean, distraction-free gameplay.",
                "Safe / Clean UI",
                TweakTag.GameMode | TweakTag.SystemHygiene),

            new("gaming_steam_client_gpu_accel_priority",
                "ИИ: Установлен приоритет видеопамяти для активной 3D-игры над фоновыми процессами Steam и лаунчеров.",
                "AI: Ensured background launchers (Steam, Epic, Battle.net) yield GPU memory to the active 3D game.",
                "Safe / GPU Prioritization",
                TweakTag.GpuCore | TweakTag.StreamingObs),

            new("gaming_directstorage_bypass_io",
                $"ИИ: Для накопителя SSD/NVMe активирован путь BypassIO для DirectStorage, направляющий текстуры напрямую в видеопамять {gpu.Name}.",
                $"AI: Enabled DirectStorage BypassIO storage path, streaming game assets directly from NVMe SSD into {gpu.Name} VRAM.",
                "Safe / DirectStorage",
                TweakTag.GpuCore | TweakTag.StorageTune,
                hw => hw.Storage.Any(s => s.IsSsd)),

            // =========================================================================
            // 6. NETWORK OPTIMIZATION & PING STABILITY
            // =========================================================================
            new("net_network_throttling_disable",
                "ИИ: Полностью отключен троттлинг сетевых пакетов Windows Network Throttling Index, устраняя скачки пинга во время голосового чата.",
                "AI: Disabled Windows Network Throttling Index, eliminating packet caps and ping spikes during active voice chat or media.",
                "Safe / Low Ping",
                TweakTag.Network | TweakTag.StreamingObs),

            new("net_qos_reserved_bandwidth_zero",
                "ИИ: Снято 20% системное ограничение пропускной способности QoS Packet Scheduler, отдавая весь сетевой канал игре.",
                "AI: Removed the 20% QoS reserved bandwidth limit, restoring 100% pipe throughput for game netcode and downloads.",
                "Safe / 100% Bandwidth",
                TweakTag.Network | TweakTag.StreamingObs),

            new("net_dns_negative_cache_disable",
                "ИИ: Отключено длительное кэширование неудачных DNS-запросов, обеспечивая мгновенное переподключение к серверам матча.",
                "AI: Disabled DNS negative cache retention to prevent multi-minute reconnect lockouts to game servers.",
                "Safe / Reconnect",
                TweakTag.Network),

            new("net_default_ttl_standard",
                "ИИ: Установлено оптимальное время жизни сетевых пакетов DefaultTTL = 64 для минимизации задержек маршрутизации.",
                "AI: Standardized IP Time-To-Live header to 64 hops for clean, low-latency routing across global servers.",
                "Safe / Network",
                TweakTag.Network),

            new("net_enable_pmtu_discovery",
                "ИИ: Включено динамическое обнаружение Path MTU Discovery для предотвращения фрагментации игровых пакетов на маршрутизаторах.",
                "AI: Enabled dynamic Path MTU Discovery (PMTU) to prevent packet fragmentation by intermediate routers.",
                "Safe / MTU",
                TweakTag.Network),

            new("net_enable_pmt_black_hole_detect",
                "ИИ: Активировано обнаружение 'черных дыр' MTU на интернет-маршрутизаторах для предотвращения случайных дисконнектов.",
                "AI: Enabled PMTU Black Hole Router detection to eliminate random multiplayer disconnects.",
                "Safe / Stability",
                TweakTag.Network),

            new("net_rss_receive_side_scaling_enable",
                $"ИИ: Для сетевого адаптера включено масштабирование Receive Side Scaling (RSS), распределяя сетевые прерывания по ядрам {cpu.Name}.",
                $"AI: Enabled Receive Side Scaling (RSS) to distribute incoming network interrupts across all cores of {cpu.Name}.",
                "Safe / Multithread Net",
                TweakTag.Network | TweakTag.StreamingObs),

            new("net_tcp_window_auto_tuning_normal",
                "ИИ: Установлен нормальный уровень автоподстройки окна TCP Window Auto-Tuning для максимальной скорости на гигабитных каналах.",
                "AI: Ensured TCP Window Auto-Tuning is set to Normal for optimal high-bandwidth throughput on modern fiber.",
                "Safe / Fiber Ready",
                TweakTag.Network | TweakTag.StreamingObs),

            new("net_wpad_autoproxy_disable",
                "ИИ: Отключен автопоиск локальных прокси-серверов WPAD, устраняя 1–2 секундную задержку открытия игровых сокетов.",
                "AI: Disabled Web Proxy Auto-Discovery (WPAD) lookups, eliminating outbound socket connection delays.",
                "Safe / Socket Speed",
                TweakTag.Network),

            new("net_disable_netbios_overhead",
                "ИИ: Отключены широковещательные запросы устаревшего протокола NetBIOS over TCP/IP, очищая сетевой адаптер.",
                "AI: Disabled NetBIOS over TCP/IP broadcast polling to eliminate periodic local network jitter spikes.",
                "Safe / Clean Adapter",
                TweakTag.Network),

            new("net_ecn_explicit_congestion_notification",
                "ИИ: Отключено ECN-уведомление о перегрузке, исключая дропы пакетов на несовместимых домашних роутерах и игровых серверах.",
                "AI: Disabled Explicit Congestion Notification (ECN) to prevent packet drops on incompatible game servers and routers.",
                "Safe / Packet Delivery",
                TweakTag.Network),

            new("net_tcp_timestamps_disable",
                "ИИ: Отключены 12-байтовые временные метки TCP Timestamps в заголовках пакетов, снижая накладные расходы сетевого стека.",
                "AI: Disabled 12-byte RFC 1323 TCP timestamps to reduce packet header overhead and kernel processing cycles.",
                "Safe / Lean Packets",
                TweakTag.Network),

            new("net_teredo_tunneling_disable",
                "ИИ: Отключен интерфейс туннелирования Teredo IPv6-in-IPv4, устраняя 20–50 мс задержки паразитной инкапсуляции.",
                "AI: Disabled Microsoft Teredo IPv6-over-IPv4 tunneling encapsulation to ensure clean native IP routing.",
                "Safe / Direct Route",
                TweakTag.Network),

            new("net_arp_cache_expiration_tuning",
                "ИИ: Время жизни ARP-кэша шлюза увеличено до 120 с, предотвращая периодические синхронные широковещательные опросы.",
                "AI: Extended ARP cache lifetime to 120s to eliminate gateway broadcast re-query hitches during competitive matches.",
                "Safe / Gateway Sync",
                TweakTag.Network | TweakTag.StreamingObs),

            new("net_tcp_nagle_algorithm_disable",
                "ИИ: Отключен алгоритм Нагла (TCP_NODELAY = 1, TcpAckFrequency = 1) для мгновенной отправки сетевых пакетов и точной регистрации попаданий.",
                "AI: Disabled Nagle's algorithm (TCP_NODELAY = 1, TcpAckFrequency = 1) for instant packet dispatch and crisp hit registration.",
                "Medium / Esports Focus",
                TweakTag.Network | TweakTag.Input),

            new("net_rsc_receive_segment_coalescing_disable",
                "ИИ: Отключено аппаратное накопление пакетов Receive Segment Coalescing (RSC) на сетевой карте для снижения сетевого джиттера.",
                "AI: Disabled NIC Receive Segment Coalescing (RSC) latency buffer, delivering incoming packets immediately to game netcode.",
                "Medium / Netcode Jitter",
                TweakTag.Network | TweakTag.Input),

            // =========================================================================
            // 7. STORAGE OPTIMIZATION & DRIVE HYGIENE
            // =========================================================================
            new("storage_ntfs_83_name_disable",
                "ИИ: Отключена генерация коротких имен MS-DOS 8.3 на файловой системе NTFS, ускоряя создание файлов и обновление игр в Steam на 20%.",
                "AI: Disabled NTFS 8.3 short filename generation, accelerating file creation, patching, and indexing by up to 20% in Steam.",
                "Safe / NTFS Speed",
                TweakTag.StorageTune),

            new("storage_ntfs_last_access_update_disable",
                "ИИ: Отключена запись временных меток последнего доступа NTFS при чтении файлов, защищая ресурс SSD и устраняя очереди I/O.",
                "AI: Disabled NTFS last access timestamp writes, eliminating disk write wear and queue stalls during game texture streaming.",
                "Safe / Drive Life",
                TweakTag.StorageTune),

            new("storage_disk_sleep_timeout_unhide",
                "ИИ: Разблокирован параметр отключения дисков в схеме питания, предотвращая засыпание вторичных накопителей с играми.",
                "AI: Unlocked disk sleep timeout in power settings, preventing secondary game library drives from powering down on AC.",
                "Safe / Game Drives",
                TweakTag.StorageTune | TweakTag.PowerPlan),

            new("storage_crash_dump_minidump_only",
                "ИИ: Дампы памяти ограничены легкими минидампами 256 КБ, предотвращая зависания диска и экономя гигабайты пространства SSD.",
                "AI: Restricted memory crash dumps to compact 256KB minidumps, saving gigabytes of free SSD space and avoiding drive lockups.",
                "Safe / Free Space",
                TweakTag.StorageTune | TweakTag.MemoryClean),

            new("storage_disable_low_disk_space_warning",
                "ИИ: Отключен фоновый опрос и всплывающие предупреждения о низком свободном месте на дисках.",
                "AI: Disabled periodic low disk space polling and balloon notifications to prevent in-game popups and disk checks.",
                "Safe / Background Disk",
                TweakTag.StorageTune | TweakTag.SystemHygiene),

            new("storage_disable_link_tracking_ntfs",
                "ИИ: Отключено журналирование службы Distributed Link Tracking на NTFS, снижая паразитные фоновые метаданные диска.",
                "AI: Disabled NTFS Distributed Link Tracking client journaling, cutting down background metadata writes.",
                "Safe / Metadata",
                TweakTag.StorageTune | TweakTag.ServicesDebloat),

            new("storage_long_path_support_enable",
                "ИИ: Снято ограничение длины путей Win32 (>260 символов), гарантируя стабильность глубоко вложенных модификаций и текстурпаков.",
                "AI: Enabled Win32 long file paths (>260 characters) to prevent crashes in complex modded game setups.",
                "Safe / Mod Support",
                TweakTag.StorageTune),

            new("storage_storage_sense_user_temp_cleanup",
                "ИИ: Настроена автоматическая очистка временных диагностических логов и кэшей Storage Sense.",
                "AI: Configured Storage Sense auto-purge for temporary diagnostic files, preserving drive headroom.",
                "Safe / Auto Cleanup",
                TweakTag.StorageTune | TweakTag.MemoryClean),

            new("storage_disable_hibernation_file",
                $"ИИ: Отключен файл гибернации hiberfil.sys, освобождая {ram.TotalGb:F0} ГБ ценного пространства на системном накопителе.",
                $"AI: Disabled Windows hibernation file (hiberfil.sys), recovering {ram.TotalGb:F0}GB of valuable SSD storage space.",
                "Safe / SSD Free Space",
                TweakTag.StorageTune | TweakTag.MemoryClean),

            new("storage_disable_volumetric_shadow_copies",
                "ИИ: Ограничена квота теневых копий VSS, защищая десятки гигабайт памяти SSD от бесконтрольного расходования.",
                "AI: Capped VSS shadow copy storage quota, reclaiming tens of gigabytes of solid-state drive space.",
                "Safe / Capacity",
                TweakTag.StorageTune | TweakTag.MemoryClean),

            new("storage_disable_chkdsk_scan_timeout",
                "ИИ: Тайм-аут загрузочной проверки chkdsk снижен до 2 секунд для молниеносной перезагрузки системы.",
                "AI: Reduced automatic disk check (chkdsk) boot timeout to 2s for significantly faster reboots.",
                "Safe / Fast Reboot",
                TweakTag.StorageTune | TweakTag.SystemHygiene),

            new("storage_trim_enable_notification",
                "ИИ: Проверена и активирована команда TRIM/Deallocate для накопителя SSD/NVMe, поддерживая заводскую скорость чтения/записи ячеек памяти.",
                "AI: Verified and enabled file system TRIM/Deallocate notification for SSD/NVMe drive to maintain peak read/write speeds.",
                "Safe / Peak SSD",
                TweakTag.StorageTune,
                hw => hw.Storage.Any(s => s.IsSsd)),

            new("storage_nvme_power_state_transition",
                "ИИ: Оптимизированы переходы автономного энергосбережения NVMe APST, предотвращая задержки пробуждения накопителя 10–50 мс.",
                "AI: Optimized NVMe Autonomous Power State Transitions (APST) to prevent 10-50ms drive resume latency.",
                "Safe / NVMe Responsiveness",
                TweakTag.StorageTune | TweakTag.PowerPlan,
                hw => hw.Storage.Any(s => s.IsSsd)),

            new("storage_write_cache_buffer_flushing_tune",
                "ИИ: Настроен интервал сброса буфера записи на SSD, исключая микрозаикания при фоновом скачивании обновлений.",
                "AI: Tuned file system cache flush interval to avoid read stalls while downloading game updates in background.",
                "Safe / Buffer Flush",
                TweakTag.StorageTune | TweakTag.FrametimeSmooth,
                hw => hw.Storage.Any(s => s.IsSsd)),

            new("storage_smart_polling_interval_tune",
                "ИИ: Настроен интервал телеметрии S.M.A.R.T. на контроллере NVMe, исключая микропрерывания очередей чтения.",
                "AI: Tuned S.M.A.R.T. telemetry polling interval on NVMe controller to prevent read queue interruptions.",
                "Safe / SMART Tune",
                TweakTag.StorageTune | TweakTag.FrametimeSmooth,
                hw => hw.Storage.Any(s => s.IsSsd)),

            // =========================================================================
            // 8. POWER ARCHITECTURE & LATENCY TUNING
            // =========================================================================
            new("power_fast_startup_disable",
                "ИИ: Отключен 'Быстрый запуск' (гибридная спячка ядра), гарантируя загрузку чистых драйверов без накопившихся утечек памяти.",
                "AI: Disabled Windows Fast Startup (hybrid hibernation), ensuring fresh kernel and driver initialization without memory leaks.",
                "Safe / Driver Purity",
                TweakTag.PowerPlan | TweakTag.SystemHygiene),

            new("power_usb_selective_suspend_unhide",
                "ИИ: Разблокирована политика выборочного отключения USB в планах электропитания для постоянного питания периферии.",
                "AI: Unlocked USB selective suspend policy in power options to maintain continuous power delivery to USB devices.",
                "Safe / Peripheral Power",
                TweakTag.PowerPlan | TweakTag.Input),

            new("power_processor_min_state_unhide",
                "ИИ: Разблокирован параметр минимального состояния процессора (Minimum Processor State) для фиксации 100% готовности на питании от сети.",
                "AI: Unlocked Minimum Processor State setting, allowing locking baseline frequency at 100% on AC power.",
                "Safe / CPU Baseline",
                TweakTag.PowerPlan | TweakTag.CpuThrottling),

            new("power_energy_saver_gaming_override",
                "ИИ: Заблокировано автоматическое затемнение экрана и троттлинг Energy Saver при активной игровой сессии.",
                "AI: Prevented Windows Energy Saver from dimming display or curtailing CPU clock speed during gaming.",
                "Safe / Brightness Lock",
                TweakTag.PowerPlan | TweakTag.GameMode),

            new("power_processor_idle_promote_unhide",
                "ИИ: Открыта настройка порога перехода ядер в сон (Idle Promote Threshold), предотвращая падение ядер в глубокие C-states.",
                "AI: Unlocked processor idle promote threshold to prevent CPU cores from prematurely falling into deep sleep states.",
                "Safe / C-States",
                TweakTag.PowerPlan | TweakTag.CpuThrottling),

            new("power_system_cooling_policy_unhide",
                "ИИ: Разблокирована политика охлаждения системы, гарантирующая раскрутку вентиляторов до троттлинга процессора (Active Cooling).",
                "AI: Unlocked System Cooling Policy to ensure fans spin up before CPU frequency downscaling occurs.",
                "Safe / Thermal Safety",
                TweakTag.PowerPlan | TweakTag.CpuThrottling),

            new("power_sleep_unattended_timeout_unhide",
                "ИИ: Разблокирован скрытый 2-минутный таймер сна при бездействии, исключая засыпание ПК во время игры с геймпада.",
                "AI: Unlocked 2-minute unattended sleep timer, preventing unwanted sleep timeouts when playing games with gamepads.",
                "Safe / Gamepad Sleep",
                TweakTag.PowerPlan),

            new("power_ahci_hipm_dipm_unhide",
                "ИИ: Разблокировано управление питанием SATA AHCI Link (HIPM/DIPM), предотвращая задержки при обращении к вторичным накопителям.",
                "AI: Unlocked SATA AHCI Link Power Management to eliminate drive sleep freezes on secondary SSDs.",
                "Safe / Storage Power",
                TweakTag.PowerPlan | TweakTag.StorageTune),

            new("power_gpu_dynamic_pstate_unhide",
                "ИИ: Разблокированы параметры управления питанием графического процессора в общей схеме электропитания.",
                "AI: Unlocked GPU power management policy attributes in power options for synchronized CPU/GPU performance.",
                "Safe / GPU Sync",
                TweakTag.PowerPlan | TweakTag.GpuCore),

            new("power_processor_performance_time_check_unhide",
                "ИИ: Разблокирован интервал пересчета частоты процессора для мгновенного отклика частоты на пиковые игровые нагрузки.",
                "AI: Unlocked processor time check interval for faster CPU frequency ramp-up on sudden burst loads.",
                "Safe / Clock Scaling",
                TweakTag.PowerPlan | TweakTag.CpuThrottling),

            new("power_processor_idle_demote_unhide",
                "ИИ: Разблокирован порог деэскалации ядер из спящих режимов, удерживая CPU в состояниях немедленной готовности C0/C1.",
                "AI: Unlocked processor idle demote threshold, keeping cores in high-readiness C0/C1 states for immediate execution.",
                "Safe / Immediate Execution",
                TweakTag.PowerPlan | TweakTag.CpuThrottling),

            new("power_away_mode_disable",
                "ИИ: Отключен псевдо-спящий режим Away Mode, исключая ложные фоновые пробуждения ПК.",
                "AI: Disabled Away Mode power policy, preventing fake sleep states with spinning fans.",
                "Safe / True Sleep",
                TweakTag.PowerPlan | TweakTag.SystemHygiene),

            new("power_hub_power_saving_disable",
                "ИИ: Запрещено отключение питания корневых концентраторов USB, обеспечивая стабильную шину 5В для внешних ЦАП и звуковых карт.",
                "AI: Disabled selective power down for root USB hubs, providing stable 5V rail delivery to external audio DACs.",
                "Safe / Audio Rail",
                TweakTag.PowerPlan | TweakTag.Input),

            new("power_allow_throttling_disable",
                "ИИ: Разрешена автономная работа аппаратного управления P-состояниями процессора без принудительного троттлинга ОС.",
                "AI: Enforced hardware P-state autonomy without OS down-throttling to sustain peak Turbo boost.",
                "Safe / Peak Boost",
                TweakTag.PowerPlan | TweakTag.CpuThrottling),

            new("power_latency_sensitivity_hint_unhide",
                "ИИ: Разблокирован параметр Latency Sensitivity Hint для мгновенного буста частоты процессора при чувствительных к задержке потоках.",
                "AI: Unlocked Processor Latency Sensitivity Hint policy for immediate frequency scaling on latency-critical threads.",
                "Safe / Latency Hint",
                TweakTag.PowerPlan | TweakTag.CpuThrottling | TweakTag.Input),

            new("power_pcie_aspm_unhide",
                $"ИИ: Для шины PCIe видеокарты {gpu.Name} разблокировано отключение энергосбережения ASPM, устраняя задержку повторной инициализации линий PCIe.",
                $"AI: Unlocked PCIe Link State Power Management (ASPM) for {gpu.Name}, eliminating PCIe lane re-initialization lag.",
                "Safe / Desktop PCIe",
                TweakTag.PowerPlan | TweakTag.GpuCore,
                hw => !hw.Power.IsLaptop),

            // =========================================================================
            // 9. WINDOWS SERVICES
            // =========================================================================
            new("svc_distributed_link_tracking_disable",
                "ИИ: Отключена служба TrkWks (клиент отслеживания изменившихся связей), устраняя бесполезную нагрузку на десктопном ПК.",
                "AI: Disabled Distributed Link Tracking Client (TrkWks), removing unnecessary shortcut handle tracking.",
                "Safe / Service Debloat",
                TweakTag.ServicesDebloat | TweakTag.MemoryClean),

            new("svc_remote_registry_disable",
                "ИИ: Отключена служба Remote Registry, закрывая сетевые порты для несанкционированного удаленного доступа к реестру.",
                "AI: Permanently disabled Remote Registry service to close network registry attack vectors and polling.",
                "Safe / Security",
                TweakTag.ServicesDebloat | TweakTag.TelemetryPrivacy),

            new("svc_downloaded_maps_manager_disable",
                "ИИ: Отключена фоновая служба офлайн-карт MapsBroker, устраняя периодические сетевые проверки обновлений карт.",
                "AI: Disabled Downloaded Maps Manager (MapsBroker) to prevent unsolicited background map network updates.",
                "Safe / Service Debloat",
                TweakTag.ServicesDebloat | TweakTag.MemoryClean),

            new("svc_fax_service_disable",
                "ИИ: Отключена устаревшая служба факса (Fax), высвобождая ресурсы диспетчера служб.",
                "AI: Disabled legacy Windows Fax service, removing obsolete component overhead.",
                "Safe / Clean System",
                TweakTag.ServicesDebloat | TweakTag.MemoryClean),

            new("svc_retail_demo_disable",
                "ИИ: Отключена демонстрационная служба RetailDemo, предотвращая активацию магазинных демо-режимов.",
                "AI: Disabled Retail Demo Service, preventing kiosk loops from ever engaging.",
                "Safe / Clean System",
                TweakTag.ServicesDebloat | TweakTag.MemoryClean),

            new("svc_wallet_service_disable",
                "ИИ: Отключена служба кошелька WalletService, устраняя неиспользуемый стек мобильных NFC-платежей.",
                "AI: Disabled Windows Wallet Service, freeing memory and removing unused payment frameworks.",
                "Safe / Clean System",
                TweakTag.ServicesDebloat | TweakTag.MemoryClean),

            new("svc_phone_service_disable",
                "ИИ: Отключена служба мобильной телефонии PhoneSvc на компьютере без модема сотовой связи.",
                "AI: Disabled Phone / Cellular Telephony service on PC without cellular modem hardware.",
                "Safe / Clean System",
                TweakTag.ServicesDebloat | TweakTag.MemoryClean),

            new("svc_biometric_service_disable",
                "ИИ: Отключена биометрическая служба WbioSrvc на системе без сканеров отпечатков пальцев.",
                "AI: Disabled Windows Biometric Service on PC without biometric fingerprint sensors.",
                "Safe / Clean System",
                TweakTag.ServicesDebloat | TweakTag.MemoryClean),

            new("svc_alljoyn_router_disable",
                "ИИ: Отключена служба маршрутизатора AllJoyn для IoT-умного дома, устраняя фоновый опрос.",
                "AI: Disabled AllJoyn Router Service, removing unused smart home IoT communication dispatcher.",
                "Safe / Clean System",
                TweakTag.ServicesDebloat | TweakTag.MemoryClean),

            new("svc_geolocation_service_disable",
                "ИИ: Отключена служба геолокации lfsvc, устраняя паразитные сканирования Wi-Fi сетей и защищая приватность.",
                "AI: Disabled Windows Geolocation Service, preventing Wi-Fi triangulation scans and protecting location privacy.",
                "Safe / Privacy",
                TweakTag.ServicesDebloat | TweakTag.TelemetryPrivacy),

            new("svc_offline_files_disable",
                "ИИ: Отключена служба автономных файлов CscService, исключая блокировку сетевых дисков фоновым кэшированием.",
                "AI: Disabled Offline Files service (CscService) to eliminate background network share cache scans.",
                "Safe / Clean System",
                TweakTag.ServicesDebloat | TweakTag.MemoryClean),

            new("svc_touch_keyboard_disable",
                "ИИ: Для стационарного ПК без тачскрина отключена служба сенсорной клавиатуры и рукописного ввода TabletInputService.",
                "AI: Disabled Touch Keyboard and Handwriting Service on non-touch desktop PC, removing text input framework hooks.",
                "Safe / Desktop Only",
                TweakTag.ServicesDebloat | TweakTag.MemoryClean,
                hw => !hw.Power.IsLaptop),

            new("svc_sysmain_manual",
                "ИИ: Для твердотельного накопителя служба SysMain (Superfetch) переведена в ручной режим, исключая фоновое чтение файлов в ОЗУ.",
                "AI: Set SysMain (Superfetch) service to Manual on SSD/NVMe storage, eliminating constant background file caching.",
                "Safe / SSD Debloat",
                TweakTag.ServicesDebloat | TweakTag.MemoryClean | TweakTag.StorageTune,
                hw => hw.Storage.Any(s => s.IsSsd)),

            new("svc_diagnostic_policy_disable",
                "ИИ: Отключена служба политики диагностики DPS, прекращая непрерывный анализ журналов событий и запись отчетов на диск.",
                "AI: Disabled Diagnostic Policy Service (DPS), eliminating diagnostic event log parsing and disk I/O during gaming.",
                "Medium / Service Debloat",
                TweakTag.ServicesDebloat | TweakTag.TelemetryPrivacy),

            new("svc_pca_service_disable",
                "ИИ: Отключен помощник по совместимости программ PcaSvc, устраняя навязчивые диалоговые окна при закрытии старых игр.",
                "AI: Disabled Program Compatibility Assistant (PcaSvc), eliminating compatibility mode dialog prompts upon game exit.",
                "Medium / Recommended",
                TweakTag.ServicesDebloat | TweakTag.SystemHygiene),

            // =========================================================================
            // 10. BACKGROUND PROCESSES & BLOATWARE
            // =========================================================================
            new("bg_diagtrack_service_disable",
                "ИИ: Отключена основная служба телеметрии DiagTrack (Connected User Experiences and Telemetry), устраняя фоновые всплески загрузки CPU.",
                "AI: Disabled core telemetry service DiagTrack, eliminating background CPU spikes and protecting gaming privacy.",
                "Safe / No Spikes",
                TweakTag.TelemetryPrivacy | TweakTag.MemoryClean | TweakTag.CpuThrottling),

            new("bg_edge_prelaunch_disable",
                "ИИ: Заблокирован автозапуск и предварительная загрузка Microsoft Edge в память, освобождая 300+ МБ ОЗУ.",
                "AI: Disabled Microsoft Edge background pre-launching and startup, reclaiming 300MB+ of physical RAM.",
                "Safe / 300MB+ RAM",
                TweakTag.MemoryClean | TweakTag.SystemHygiene),

            new("bg_wer_service_disable",
                "ИИ: Отключена служба отчетов об ошибках Windows Error Reporting (WerSvc), предотвращая подвисания игр при обработке исключений.",
                "AI: Disabled Windows Error Reporting service to prevent process freezing and disk activity during minor game errors.",
                "Safe / Error Free",
                TweakTag.TelemetryPrivacy | TweakTag.SystemHygiene),

            new("bg_widgets_news_interests_disable",
                "ИИ: Отключена панель виджетов 'Новости и интересы' Windows 11, выгружая тяжелые фоновые процессы WebView2 из памяти.",
                "AI: Disabled Windows 11 Widgets board and News feeds, terminating background WebView2 Edge processes and saving RAM.",
                "Safe / WebView2 Purge",
                TweakTag.MemoryClean | TweakTag.SystemHygiene),

            new("bg_content_delivery_suggestions_disable",
                "ИИ: Отключена автоматическая загрузка рекламируемых приложений и предложений Content Delivery Manager.",
                "AI: Disabled Content Delivery Manager promoted suggestions, preventing automatic background bloatware downloads.",
                "Safe / No Bloat",
                TweakTag.TelemetryPrivacy | TweakTag.SystemHygiene),

            new("bg_customer_experience_ceip_disable",
                "ИИ: Отключены задачи программы улучшения качества ПО (CEIP), исключая запланированные фоновые сборщики телеметрии.",
                "AI: Disabled Customer Experience Improvement Program (CEIP) scheduled tasks, eliminating periodic CPU spikes.",
                "Safe / CPU Idle",
                TweakTag.TelemetryPrivacy | TweakTag.CpuThrottling),

            new("bg_tailored_experiences_disable",
                "ИИ: Отключен персонализированный сбор данных об использовании приложений Tailored Experiences.",
                "AI: Disabled Tailored Experiences diagnostic indexing of user clicks and app launches.",
                "Safe / Privacy",
                TweakTag.TelemetryPrivacy),

            new("bg_windows_spotlight_disable",
                "ИИ: Отключена фоновая загрузка обоев 'Windows: интересное' (Spotlight), экономя интернет-трафик и дисковые операции.",
                "AI: Disabled Windows Spotlight lockscreen background image downloads, saving network bandwidth and disk I/O.",
                "Safe / Bandwidth",
                TweakTag.SystemHygiene | TweakTag.Network),

            new("bg_compatibility_telemetry_disable",
                "ИИ: Заблокирован процесс телеметрии CompatTelRunner.exe, вызывающий 100% загрузку диска и процессора при сканировании файлов.",
                "AI: Disabled Microsoft Compatibility Telemetry (CompatTelRunner.exe), preventing notorious 100% disk and CPU spikes.",
                "Safe / No 100% Disk",
                TweakTag.TelemetryPrivacy | TweakTag.CpuThrottling | TweakTag.StorageTune),

            new("bg_device_census_disable",
                "ИИ: Отключен фоновый опрос подключенного оборудования DeviceCensus.exe, устраняя пробуждение ядер в простое.",
                "AI: Disabled background Device Census telemetry task, preventing unnecessary hardware enumeration.",
                "Safe / Core Idle",
                TweakTag.TelemetryPrivacy | TweakTag.CpuThrottling),

            new("bg_copilot_runtime_disable",
                "ИИ: Отключена интеграция Windows Copilot и фоновые AI-процессы WebView, освобождая оперативную память.",
                "AI: Disabled Microsoft Copilot runtime integration and background webview assistants, reducing RAM consumption.",
                "Safe / AI Debloat",
                TweakTag.MemoryClean | TweakTag.SystemHygiene),

            new("bg_onedrive_sync_startup_disable",
                "ИИ: Отключен автозапуск OneDrive и фоновая синхронизация папки Documents, устраняя статтеры при сохранении игр.",
                "AI: Disabled OneDrive automatic startup and Documents folder sync, eliminating savegame write locks and stutter.",
                "Safe / Savegame Sync",
                TweakTag.MemoryClean | TweakTag.FrametimeSmooth | TweakTag.StorageTune),

            new("bg_feedback_hub_telemetry_disable",
                "ИИ: Отключены фоновые задачи агрегации логов Центра отзывов, снижая количество переключений контекста ядра.",
                "AI: Disabled Feedback Hub background diagnostic tasks, reducing kernel context switches and idle load.",
                "Safe / Clean Idle",
                TweakTag.TelemetryPrivacy | TweakTag.SystemHygiene),

            new("bg_appx_deployment_optimization",
                "ИИ: Заблокировано автоматическое фоновое обновление приложений Microsoft Store во время активных игровых матчей.",
                "AI: Configured Microsoft Store app updates to prevent surprise background downloads during active games.",
                "Safe / No Updates In-Game",
                TweakTag.SystemHygiene | TweakTag.Network),

            new("bg_cloud_clipboard_sync_disable",
                "ИИ: Отключена облачная синхронизация буфера обмена, устраняя скрытые сетевые отправки при копировании текста и скриншотов.",
                "AI: Disabled cloud clipboard history cross-device synchronization, ensuring instant local clipboard without network overhead.",
                "Safe / Local Clipboard",
                TweakTag.TelemetryPrivacy | TweakTag.Network),

            new("bg_phone_link_background_disable",
                "ИИ: Отключена фоновая синхронизация службы 'Связь с телефоном' (Phone Link), освобождая ОЗУ и сетевой канал.",
                "AI: Disabled Phone Link background sync host, freeing RAM and eliminating background wireless device syncing.",
                "Safe / Background Purge",
                TweakTag.MemoryClean | TweakTag.Network),

            new("bg_windows_tips_suggestions_disable",
                "ИИ: Отключены фоновые подсказки и приветственные экраны Windows, устраняя отвлекающие баннеры.",
                "AI: Disabled Windows Soft Landing tips and educational welcome prompts, preventing unprompted UI interruptions.",
                "Safe / Clean Experience",
                TweakTag.SystemHygiene),

            // =========================================================================
            // 11. SECURITY, PRIVACY & TELEMETRY
            // =========================================================================
            new("sec_telemetry_data_collection_min",
                "ИИ: Сбор диагностических данных Windows ограничен минимальным базовым уровнем (Security/Required).",
                "AI: Restricted Windows Diagnostic Data collection to minimum technical level (Security / Required).",
                "Safe / Privacy Focus",
                TweakTag.TelemetryPrivacy | TweakTag.MemoryClean),

            new("sec_advertising_id_disable",
                "ИИ: Отключен рекламный идентификатор Windows Advertising ID, предотвращая сбор персонального рекламного профиля.",
                "AI: Disabled Windows Advertising ID to terminate cross-app user tracking and advertising telemetry.",
                "Safe / Privacy",
                TweakTag.TelemetryPrivacy),

            new("sec_activity_history_disable",
                "ИИ: Отключен сбор журнала действий Windows Activity History и облачная публикация активности.",
                "AI: Disabled Windows Activity History timeline and cloud sync, eliminating local activity journaling overhead.",
                "Safe / Privacy",
                TweakTag.TelemetryPrivacy),

            new("sec_location_tracking_disable",
                "ИИ: Отключены системные датчики геолокации, прекращая паразитное сканирование Wi-Fi BSSID.",
                "AI: Disabled global location tracking sensors, preventing periodic Wi-Fi triangulation scans.",
                "Safe / Privacy",
                TweakTag.TelemetryPrivacy),

            new("sec_wifi_sense_hotspot_reporting_disable",
                "ИИ: Отключена служба Wi-Fi Sense и передача учетных данных точек доступа в облако.",
                "AI: Disabled Wi-Fi Sense and hotspot reporting, ensuring strictly manual Wi-Fi connection control.",
                "Safe / Network Privacy",
                TweakTag.TelemetryPrivacy | TweakTag.Network),

            new("sec_cloud_settings_sync_disable",
                "ИИ: Отключена облачная синхронизация системных настроек с OneDrive, предотвращая случайный сброс параметров оптимизации.",
                "AI: Disabled Microsoft account settings cloud sync, preventing remote cloud profiles from reverting local tweaks.",
                "Safe / Settings Lock",
                TweakTag.TelemetryPrivacy | TweakTag.SystemHygiene),

            new("sec_handwriting_error_reporting_disable",
                "ИИ: Отключена отправка отчетов об ошибках распознавания рукописного ввода.",
                "AI: Disabled handwriting recognition error reporting to eliminate background telemetry queues.",
                "Safe / Privacy",
                TweakTag.TelemetryPrivacy),

            new("sec_windows_customer_experience_optout",
                "ИИ: Отключено ведение журналов SQM (Software Quality Metrics) во всех установленных системных программах.",
                "AI: Opted out of SQM application telemetry logging across Windows, preventing disk clutter from temporary metric logs.",
                "Safe / Clean Disk",
                TweakTag.TelemetryPrivacy),

            new("sec_app_diagnostic_info_deny",
                "ИИ: Запрещен доступ приложений Microsoft Store к системным журналам событий и диагностике оборудования.",
                "AI: Blocked downloaded Store apps from querying hardware diagnostic sensors and event logs.",
                "Safe / Sandbox",
                TweakTag.TelemetryPrivacy),

            new("sec_remote_assistance_disable",
                "ИИ: Отключены входящие подключения удаленного помощника Remote Assistance (MSRA), закрывая уязвимые порты.",
                "AI: Disabled Remote Assistance listener ports, enhancing network security and reducing background listening overhead.",
                "Safe / Security",
                TweakTag.TelemetryPrivacy | TweakTag.Network),

            new("sec_defender_cloud_protection_reporting",
                "ИИ: Время ожидания облачной проверки Defender переведено в быстрый режим, исключая 10-секундные задержки при старте игр.",
                "AI: Configured Microsoft Defender cloud protection timeout to fast mode, eliminating executable launch freezes.",
                "Safe / Fast Launch",
                TweakTag.GameMode | TweakTag.SystemHygiene),

            new("sec_microphone_background_access_apps",
                "ИИ: Запрещен скрытый фоновый доступ к микрофону для приложений UWP, защищая аудиопоток от посторонних слушателей.",
                "AI: Restricted background apps from silently accessing system microphone, preventing audio buffer jitter.",
                "Safe / Audio Privacy",
                TweakTag.AudioLatency | TweakTag.StreamingObs),

            new("sec_camera_background_access_deny",
                "ИИ: Запрещен фоновый доступ приложений к веб-камере, предотвращая пробуждение USB-контроллера камеры.",
                "AI: Blocked background apps from accessing webcam, preventing USB driver wakeups and protecting video privacy.",
                "Safe / Privacy",
                TweakTag.StreamingObs | TweakTag.SystemHygiene),

            new("sec_smartscreen_for_apps_disable",
                "ИИ: Отключена проверка URL-ссылок фильтром SmartScreen внутри приложений Store для мгновенного перехода по ссылкам.",
                "AI: Disabled SmartScreen URL evaluation inside Store apps for instant link navigation without remote lookups.",
                "Safe / Browser Latency",
                TweakTag.SystemHygiene),

            new("sec_voice_activation_above_lock_disable",
                "ИИ: Запрещена фоновая активация голосовых ассистентов через микрофон при заблокированном экране.",
                "AI: Disabled voice activation wake words above lock screen, completely muting microphone monitoring while locked.",
                "Safe / Mic Lock",
                TweakTag.AudioLatency | TweakTag.TelemetryPrivacy),

            new("sec_biometrics_facial_recognition_disable",
                "ИИ: Отключен опрос инфракрасных камер Windows Hello на настольном ПК без ИК-датчиков.",
                "AI: Disabled Windows Hello camera sensor polling on desktop PC without IR camera hardware.",
                "Safe / Desktop Security",
                TweakTag.TelemetryPrivacy | TweakTag.ServicesDebloat,
                hw => !hw.Power.IsLaptop),

            // =========================================================================
            // 12. WINDOWS PERFORMANCE & UI SNAPPINESS
            // =========================================================================
            new("win_menu_show_delay",
                "ИИ: Задержка открытия контекстных меню MenuShowDelay снижена с 400 мс до 10 мс для моментального отклика интерфейса.",
                "AI: Reduced MenuShowDelay from 400ms to 10ms for instantaneous, snappy context menu and taskbar responsiveness.",
                "Safe / 10ms Snappy",
                TweakTag.SystemHygiene),

            new("win_transparency_disable",
                $"ИИ: Отключены эффекты прозрачности Acrylic и Mica, высвобождая ресурсы видеопамяти и пиксельных шейдеров {gpu.Name}.",
                $"AI: Disabled desktop Acrylic and Mica transparency blur shaders, freeing GPU fillrate and VRAM on {gpu.Name}.",
                "Safe / GPU Fillrate",
                TweakTag.GpuCore | TweakTag.DwmDirectFlip),

            new("win_window_animations_disable",
                "ИИ: Отключены анимации сворачивания и разворачивания окон, обеспечивая мгновенный Alt+Tab и переключение задач.",
                "AI: Disabled window minimize/maximize animations for instantaneous Alt-Tab switching and zero composition hitching.",
                "Safe / 0ms Animations",
                TweakTag.SystemHygiene | TweakTag.DwmDirectFlip),

            new("win_background_apps_global_disable",
                "ИИ: Глобально отключена фоновая работа универсальных приложений Windows (Погода, Карты, Новости), освобождая ОЗУ и такты CPU.",
                "AI: Globally disabled background UWP apps execution, reclaiming CPU cycles, memory, and eliminating background network polling.",
                "Safe / Global Debloat",
                TweakTag.MemoryClean | TweakTag.CpuThrottling | TweakTag.SystemHygiene),

            new("win_delivery_optimization_p2p_disable",
                "ИИ: Отключена раздача обновлений Windows Delivery Optimization по P2P-сети, исключая скрытую загрузку исходящего интернет-канала.",
                "AI: Disabled Delivery Optimization P2P upload seeding, guaranteeing stable network ping without hidden bandwidth consumption.",
                "Safe / No Upload Seeding",
                TweakTag.Network | TweakTag.SystemHygiene),

            new("win_start_menu_web_search_disable",
                "ИИ: Отключен поиск Bing в меню 'Пуск', превращая поиск в мгновенный локальный поиск приложений без отправки сетевых запросов.",
                "AI: Disabled Bing web search in Start menu, enabling instantaneous local application search without cloud query delays.",
                "Safe / Local Search",
                TweakTag.SystemHygiene | TweakTag.Network),

            new("win_feedback_frequency_never",
                "ИИ: Частота опросов обратной связи Windows Feedback установлена на 'Никогда', исключая всплывающие уведомления.",
                "AI: Set Windows Feedback prompt frequency to Never, suppressing intrusive background feedback notifications.",
                "Safe / No Polls",
                TweakTag.SystemHygiene | TweakTag.TelemetryPrivacy),

            new("win_cortana_disable",
                "ИИ: Отключен голосовой ассистент Cortana, выгружая неиспользуемые службы из оперативной памяти.",
                "AI: Disabled Cortana voice assistant, removing background memory footprint and microphone monitoring threads.",
                "Safe / RAM Free",
                TweakTag.MemoryClean | TweakTag.ServicesDebloat),

            new("win_auto_play_disable",
                "ИИ: Отключен автозапуск внешних накопителей AutoPlay, защищая от вредоносного ПО и зависаний при подключении флешек.",
                "AI: Disabled AutoPlay for external media, preventing system freezes and security risks when plugging in USB accessories.",
                "Safe / USB Security",
                TweakTag.SystemHygiene),

            new("win_aero_shake_disable",
                "ИИ: Отключен жест Aero Shake (сворачивание окон при встряхивании), предотвращая случайное сворачивание при резких движениях мыши.",
                "AI: Disabled Aero Shake to prevent accidental minimization of background windows during frantic mouse swipes.",
                "Safe / Multi-Window",
                TweakTag.SystemHygiene | TweakTag.Input),

            new("win_snap_assist_disable",
                "ИИ: Отключены подсказки Snap Assist при привязке окон, делая перемещение окон плавным и быстрым.",
                "AI: Disabled Snap Assist auto-suggestions to eliminate UI hesitation when docking or moving windows.",
                "Safe / UI Fluidity",
                TweakTag.SystemHygiene),

            new("win_visual_effects_best_performance",
                "ИИ: Визуальные эффекты Windows настроены на максимальное быстродействие с сохранением сглаживания экранных шрифтов.",
                "AI: Optimized visual effects for best performance while preserving ClearType sub-pixel font smoothing.",
                "Safe / Performance FX",
                TweakTag.SystemHygiene | TweakTag.DwmDirectFlip),

            new("win_lockscreen_slideshow_disable",
                "ИИ: Отключено слайдшоу экрана блокировки, ускоряя пробуждение монитора из спящего режима.",
                "AI: Disabled lock screen slideshow network rotation for instantaneous wake from sleep.",
                "Safe / Fast Wake",
                TweakTag.SystemHygiene),

            new("win_shell_shortcuts_hotkeys_disable",
                "ИИ: Отключены конфликтующие глобальные хоткеи Windows 11 (Win+C для Copilot и Win+W для виджетов).",
                "AI: Disabled Windows 11 Copilot (Win+C) and Widgets (Win+W) global hotkey conflict combinations.",
                "Safe / No Conflict Keys",
                TweakTag.SystemHygiene | TweakTag.Input),

            new("win_search_indexer_backoff_disable",
                "ИИ: Отключено замедление поискового индексатора при активности пользователя, ускоряя завершение индексации диска.",
                "AI: Disabled Windows Search Indexer CPU backoff throttling to complete disk indexing faster and eliminate idle thrashing.",
                "Safe / Index Speed",
                TweakTag.StorageTune | TweakTag.SystemHygiene),

            new("win_app_suggestions_start_menu_disable",
                "ИИ: Отключены рекомендованные приложения и реклама в меню 'Пуск' Windows 11 для чистого и быстрого меню.",
                "AI: Disabled promoted app recommendations and recent file suggestions in Windows 11 Start menu.",
                "Safe / Clean Menu",
                TweakTag.SystemHygiene | TweakTag.TelemetryPrivacy),

            new("win_toast_notifications_quiet_hours",
                "ИИ: Включен тихий режим Focus Assist для подавления всплывающих уведомлений во время полноэкранных игр.",
                "AI: Enforced Focus Assist automatic quiet hours to suppress notification banners during fullscreen gaming.",
                "Safe / Focus Assist",
                TweakTag.GameMode | TweakTag.SystemHygiene),

            new("win_print_screen_snipping_tool_disable",
                "ИИ: Восстановлен классический мгновенный снимок экрана по клавише PrintScreen в буфер обмена без запуска тяжелого Snipping Tool.",
                "AI: Restored classic zero-latency PrintScreen key behavior, bypassing heavy modern Snipping Tool overlays.",
                "Safe / Instant Screenshot",
                TweakTag.SystemHygiene | TweakTag.Input),

            // =========================================================================
            // 13. ADVANCED ARCHITECTURAL KERNEL TWEAKS
            // =========================================================================
            new("adv_critical_section_timeout",
                "ИИ: Сокращен тайм-аут критических секций ядра для быстрого разрешения взаимных блокировок потоков.",
                "AI: Reduced kernel critical section timeout for rapid deadlock resolution.",
                "Safe / Kernel",
                TweakTag.TimerResolution | TweakTag.CpuThrottling | TweakTag.FrametimeSmooth),

            new("adv_paging_executive_strict_lock",
                "ИИ: Включена строгая блокировка страниц ядра в ОЗУ для защиты критических драйверов от фрагментации.",
                "AI: Enforced strict kernel paging lock to protect driver routines from page fragmentation.",
                "Safe / Kernel",
                TweakTag.MemoryClean | TweakTag.FrametimeSmooth),

            new("adv_global_flag_page_heap_disable",
                "ИИ: Отключены отладочные флаги Page Heap для минимального накладного расхода памяти.",
                "AI: Disabled page heap debugging flags for lowest memory management overhead.",
                "Safe / Kernel",
                TweakTag.MemoryClean | TweakTag.FrametimeSmooth),

            new("adv_kernel_sehop_enable",
                "ИИ: Активирована аппаратная защита перезаписи обработчиков исключений (SEHOP).",
                "AI: Enabled Structured Exception Handler Overwrite Protection (SEHOP).",
                "Safe / Security",
                TweakTag.SystemHygiene),

            new("adv_mmcss_audio_realtime_priority",
                "ИИ: Зафиксирован приоритет аудиопотоков MMCSS в реальном времени, устраняя хрипы и задержки звука в играх.",
                "AI: Locked MMCSS audio thread scheduling to realtime priority, eliminating audio dropouts and crackling in games.",
                "Safe / Audio Realtime",
                TweakTag.AudioLatency | TweakTag.StreamingObs | TweakTag.FrametimeSmooth),

            new("adv_executive_fast_fail_strict_lock",
                "ИИ: Включена оптимизация Executive Fast Fail для мгновенной обработки системных прерываний.",
                "AI: Enabled Executive Fast Fail optimization for instant system interrupt servicing.",
                "Safe / Fast Fail",
                TweakTag.TimerResolution | TweakTag.CpuThrottling),

            new("adv_dwm_thread_priority_realtime",
                "ИИ: Приоритет потока композитора DWM повышен до реального времени для абсолютной плавности смены кадров.",
                "AI: Elevated DWM compositor thread priority to realtime for flawless frame pacing and lowest presentation jitter.",
                "Medium / Realtime DWM",
                TweakTag.DwmDirectFlip | TweakTag.FrametimeSmooth),

            new("adv_win32_priority_quanta_28",
                "ИИ: Настроен квант времени потоков (quanta 28) для наилучшего баланса плавности рендера и отклика ввода.",
                "AI: Configured Win32 thread scheduling quanta (28) for optimal render pacing and input responsiveness.",
                "Medium / Quanta 28",
                TweakTag.CpuThrottling | TweakTag.FrametimeSmooth | TweakTag.StreamingObs),

            new("adv_session_pool_size_128",
                "ИИ: Расширен размер пула сеансов SessionPoolSize до 128 МБ для стабильной работы тяжелых оверлеев.",
                "AI: Expanded session pool size to 128MB to prevent UI resource exhaustion with heavy gaming overlays.",
                "Medium / Memory Pool",
                TweakTag.StreamingObs | TweakTag.MemoryClean),

            // -------------------------------------------------------------------------
            // ADVANCED ARCHITECTURAL KERNEL EXTENSIONS
            // -------------------------------------------------------------------------
            new("adv_device_latency_interrupt_affinity",
                "ИИ: Активирована приоритетная маршрутизация прерываний MSI (Message Signaled Interrupts), устраняя разделение IRQ-линий и аппаратные всплески DPC.",
                "AI: Enabled Message Signaled Interrupts (MSI) prioritization, eliminating shared IRQ line conflicts and hardware DPC latency spikes.",
                "Safe / MSI Priority",
                TweakTag.TimerResolution | TweakTag.CpuThrottling | TweakTag.FrametimeSmooth),

            new("adv_heap_decommit_threshold",
                "ИИ: Увеличен порог освобождения блоков кучи процессов (Heap Decommit), снижая накладные расходы на постоянное выделение и возврат памяти ОС.",
                "AI: Increased heap decommit free block threshold to reduce memory allocation churn and OS paging locks.",
                "Safe / Heap Tune",
                TweakTag.MemoryClean | TweakTag.FrametimeSmooth),

            new("adv_hpet_synthetic_timer_tune",
                "ИИ: Настроена подсистема таймеров ядра на прямое обращение к аппаратному счетчику TSC процессора в обход накладных расходов синтетических таймеров.",
                "AI: Configured kernel timer subsystem to use direct hardware TSC timers, bypassing synthetic timer overhead.",
                "Safe / Hardware TSC",
                TweakTag.TimerResolution | TweakTag.Input),

            new("adv_kernel_dpc_timeout_polynomial",
                "ИИ: Скорректирован интервал тайм-аута сторожевого таймера DPC для предотвращения сбоев при интенсивной компиляции шейдеров в DX12.",
                "AI: Adjusted kernel DPC watchdog timer interval to prevent execution hitches during heavy DirectX 12 shader compilation.",
                "Safe / DPC Watchdog",
                TweakTag.FrametimeSmooth | TweakTag.GpuCore),

            new("adv_kernel_pool_tagging_gaming",
                "ИИ: Включена оптимизация блочного выделения памяти (Pool Tagging) для быстрого отслеживания и изоляции драйверных буферов.",
                "AI: Enabled kernel pool tagging optimization for streamlined slab memory allocation and driver buffer tracking.",
                "Safe / Kernel Pool",
                TweakTag.MemoryClean | TweakTag.FrametimeSmooth),

            new("adv_protection_mode_object_directories",
                "ИИ: Обеспечена защита системных каталогов объектов ядра ProtectionMode для стабильности античитов и изоляции системных дескрипторов.",
                "AI: Enforced kernel object directory ProtectionMode to safeguard system handles and anticheat process stability.",
                "Safe / Security",
                TweakTag.SystemHygiene),

            new("adv_spectre_meltdown_override_experimental",
                "ИИ: Отключены программные барьеры спекулятивного выполнения Spectre/Meltdown, возвращая до 15% чистой производительности CPU в играх.",
                "AI: Disabled Spectre/Meltdown branch prediction barriers to recover up to 15% pure CPU throughput in gaming.",
                "High / Experimental",
                TweakTag.CpuThrottling | TweakTag.TimerResolution),

            new("adv_system_pages_unlimited",
                "ИИ: Системный параметр SystemPages переведен в динамический режим (0), позволяя ядру создавать максимальный пул записей таблиц страниц.",
                "AI: Set SystemPages to dynamic maximum (0), allowing the Windows kernel to scale page table entries to full RAM capacity.",
                "Safe / Kernel Pages",
                TweakTag.MemoryClean | TweakTag.FrametimeSmooth),

            // -------------------------------------------------------------------------
            // BACKGROUND & STARTUP OPTIMIZATIONS
            // -------------------------------------------------------------------------
            new("bg_smartscreen_prompt_latency",
                "ИИ: Оптимизирован тайм-аут проверок фильтра SmartScreen, исключая секундные задержки при старте исполняемых файлов игр и лаунчеров.",
                "AI: Optimized SmartScreen validation timeout to eliminate executable and launcher startup delays.",
                "Medium / Fast Launch",
                TweakTag.SystemHygiene | TweakTag.Network),

            // -------------------------------------------------------------------------
            // CPU & THREAD POLICIES
            // -------------------------------------------------------------------------
            new("cpu_hetero_short_thread_runtime",
                "ИИ: Разблокирована политика диспетчеризации коротких фоновых потоков на гибридных CPU, изолируя основные ядра рендера от фоновых микрозадач.",
                "AI: Unlocked heterogeneous short-running thread scheduling policy to steer background micro-tasks away from primary game render cores.",
                "Safe / Thread Scheduler",
                TweakTag.CpuThrottling | TweakTag.FrametimeSmooth),

            new("cpu_processor_boost_mode_unhide",
                "ИИ: Разблокирован параметр выбора режима буста процессора (Performance Boost Mode) для фиксации агрессивного турбо-режима.",
                "AI: Unlocked Processor Performance Boost Mode policy to enable aggressive Turbo Boost frequency scaling.",
                "Safe / Turbo Boost",
                TweakTag.CpuThrottling | TweakTag.PowerPlan),

            // -------------------------------------------------------------------------
            // GAMING PERFORMANCE & SERVICES
            // -------------------------------------------------------------------------
            new("gaming_directx_gpu_energy_driver",
                "ИИ: Отключены энергосберегающие подсказки DirectX видеодрайверу, удерживая максимальные частоты видеокарты в продолжительных 3D-сессиях.",
                "AI: Disabled DirectX energy driver downscaling hints, locking maximum GPU clock speeds during extended 3D gaming sessions.",
                "Medium / Clock Lock",
                TweakTag.GpuCore | TweakTag.GameMode),

            new("gaming_xbox_live_auth_manager_disable",
                "ИИ: Отключена служба аутентификации Xbox Live (XblAuthManager), устраняя фоновые сетевые запросы при игре через Steam и Epic Games.",
                "AI: Disabled Xbox Live Auth Manager (XblAuthManager) background service to eliminate network token polls during Steam/Epic gaming.",
                "Safe / Background Free",
                TweakTag.GameMode | TweakTag.ServicesDebloat | TweakTag.Network),

            // -------------------------------------------------------------------------
            // GPU PIPELINE & SCHEDULER
            // -------------------------------------------------------------------------
            new("gpu_preemption_scheduler_enable",
                "ИИ: Включено аппаратное вытеснение (Preemption) в шедулере DirectX для мгновенного переключения контекста рендеринга и снижения задержки.",
                "AI: Enabled fine-grained hardware GPU preemption in DirectX scheduler for rapid context switching and reduced display lag.",
                "Medium / GPU Preemption",
                TweakTag.GpuCore | TweakTag.DwmDirectFlip | TweakTag.FrametimeSmooth),

            // -------------------------------------------------------------------------
            // INPUT & PERIPHERALS
            // -------------------------------------------------------------------------
            new("input_double_click_time_standard",
                "ИИ: Интервал распознавания двойного клика откалиброван до 450 мс для безупречной регистрации быстрых одиночных выстрелов (тэппинга) в шутерах.",
                "AI: Calibrated double-click detection interval to 450ms for precise registration of rapid single-tap shots in shooters.",
                "Safe / Click Speed",
                TweakTag.Input),

            new("input_toggle_keys_prompt_disable",
                "ИИ: Отключен звуковой сигнал и окно активации ToggleKeys при удержании NumLock/CapsLock, предотвращая системные звуки в бою.",
                "AI: Disabled ToggleKeys sound alerts and holding prompt, preventing accidental OS audio cues and key intercepts.",
                "Safe / Clean Input",
                TweakTag.Input | TweakTag.SystemHygiene),

            new("input_touch_prediction_latency",
                "ИИ: Отключена визуализация сенсорного ввода и предсказание касаний, разгружая цикл обработки сообщений мыши WM_INPUT.",
                "AI: Disabled touch visualization feedback and prediction heuristics, streamlining the raw WM_INPUT pointer dispatch loop.",
                "Safe / Raw Dispatch",
                TweakTag.Input | TweakTag.DwmDirectFlip),

            // -------------------------------------------------------------------------
            // NETWORK OPTIMIZATIONS
            // -------------------------------------------------------------------------
            new("net_syn_attack_protect_tune",
                "ИИ: Настроен порог защиты от SYN-флуда, устраняя ложные блокировки пакетов при обновлении списка серверов в играх Valve/Steam.",
                "AI: Tuned SYN attack protection threshold to prevent handshake throttling during server browser ping queries in Steam titles.",
                "Safe / Server Browser",
                TweakTag.Network),

            new("net_tcp_max_data_retransmissions",
                "ИИ: Количество повторных попыток отправки потерянных TCP-пакетов снижено с 5 до 3 для ускорения восстановления соединения при сбоях.",
                "AI: Reduced TCP data retransmission retries from 5 to 3 to trigger immediate socket recovery during network drops.",
                "Medium / Low Jitter",
                TweakTag.Network | TweakTag.Input),

            // -------------------------------------------------------------------------
            // POWER MANAGEMENT
            // -------------------------------------------------------------------------
            new("power_modern_standby_disable",
                "ИИ: Для стационарного ПК принудительно включен классический режим сна S3 вместо Modern Standby, устраняя фантомный нагрев и зависания.",
                "AI: Enforced classic S3 sleep state over Modern Standby on desktop PC, eliminating phantom power drain and sleep crashes.",
                "Medium / Desktop Sleep",
                TweakTag.PowerPlan | TweakTag.SystemHygiene,
                hw => !hw.Power.IsLaptop),

            // -------------------------------------------------------------------------
            // RAM & MEMORY EXTENSIONS
            // -------------------------------------------------------------------------
            new("ram_io_page_lock_limit",
                "ИИ: Лимит блокировки страниц ввода-вывода IoPageLockLimit расширен до 16 МБ, устраняя пробуксовку очередей при стриминге DirectStorage.",
                "AI: Expanded IoPageLockLimit buffer to 16MB to prevent memory lock bottlenecks during high-throughput DirectStorage streaming.",
                "Medium / I/O Buffer",
                TweakTag.MemoryClean | TweakTag.StorageTune | TweakTag.FrametimeSmooth),

            new("ram_memory_compression_store",
                "ИИ: Оптимизирован кэш второго уровня подсистемы сжатия памяти для быстрого извлечения страниц без задержек декомпрессии на CPU.",
                "AI: Optimized memory compression second-level data cache to minimize CPU decompression latency during game execution.",
                "Medium / Compression Tune",
                TweakTag.MemoryClean | TweakTag.FrametimeSmooth),

            new("ram_pool_usage_maximum",
                "ИИ: Квота неподкачиваемого пула памяти NonPagedPool увеличена до 60%, гарантируя запас памяти для сетевых и аудио буферов.",
                "AI: Increased NonPagedPool memory quota to 60%, providing ample headroom for network packet buffers and audio streams.",
                "Medium / Driver Pool",
                TweakTag.MemoryClean | TweakTag.Network | TweakTag.AudioLatency),

            new("ram_session_view_size",
                "ИИ: Размер пула сеансов GUI SessionViewSize увеличен до 96 МБ, предотвращая падения интерфейса при работе с Discord, OBS и оверлеями.",
                "AI: Increased GUI SessionViewSize allocation to 96MB, preventing desktop heap crashes with Discord, OBS, and high-DPI overlays.",
                "Safe / Session Pool",
                TweakTag.MemoryClean | TweakTag.StreamingObs),

            // -------------------------------------------------------------------------
            // SECURITY & PRIVACY
            // -------------------------------------------------------------------------
            new("sec_defender_sample_submission_disable",
                "ИИ: Отключена автоматическая отправка образцов файлов в Defender, исключая зависания игровых модификаций и трейнеров при запуске.",
                "AI: Disabled automatic file sample uploading in Microsoft Defender, preventing launch freezes on custom game mods.",
                "Medium / Mod Friendly",
                TweakTag.TelemetryPrivacy | TweakTag.Network),

            new("sec_inking_typing_personalization_disable",
                "ИИ: Отключен фоновый сбор телеметрии ввода с клавиатуры и словарей рукописного ввода, сохраняя приватность набора.",
                "AI: Terminated keystroke personalization and typing diagnostic data harvesting to safeguard typing privacy.",
                "Safe / Keystroke Privacy",
                TweakTag.TelemetryPrivacy | TweakTag.Input),

            // -------------------------------------------------------------------------
            // STORAGE & FILESYSTEM
            // -------------------------------------------------------------------------
            new("storage_flush_file_buffers_optimization",
                "ИИ: Оптимизированы интервалы сброса буферов записи на диск, предотвращая задержки синхронизации при потоковой загрузке уровней.",
                "AI: Configured storage write cache buffer flush thresholds to avoid synchronous flush stalls during game level streaming.",
                "Medium / Flush Buffer",
                TweakTag.StorageTune | TweakTag.FrametimeSmooth,
                hw => hw.Storage.Any(s => s.IsSsd)),

            new("storage_ntfs_memory_usage_increase",
                "ИИ: Выделен расширенный объем физической памяти для файлового кэша NTFS (NtfsMemoryUsage = 2), ускоряя загрузку модов и текстур.",
                "AI: Increased physical memory allocation for NTFS filesystem metadata cache to accelerate asset loading in heavily modded titles.",
                "Medium / NTFS Cache",
                TweakTag.StorageTune | TweakTag.MemoryClean),

            new("storage_ntfs_mft_zone_reservation",
                "ИИ: Зарезервирована зона таблицы MFT на NTFS (NtfsMftZoneReservation = 2), предотвращая фрагментацию файлов на накопителях.",
                "AI: Optimized NTFS Master File Table zone reservation to prevent MFT fragmentation and speed up game asset discovery.",
                "Medium / MFT Zone",
                TweakTag.StorageTune),

            // -------------------------------------------------------------------------
            // WINDOWS SERVICES DEBLOAT
            // -------------------------------------------------------------------------
            new("svc_print_spooler_manual",
                "ИИ: Служба диспетчера печати (Spooler) переведена в ручной режим, освобождая память и закрывая фоновый RPC-сервер на игровом ПК.",
                "AI: Set Print Spooler service to Manual on gaming PC, recovering memory and closing listening RPC endpoints.",
                "Safe / Print Debloat",
                TweakTag.ServicesDebloat | TweakTag.MemoryClean),

            new("svc_windows_search_manual",
                "ИИ: Служба Windows Search переведена в ручной режим, устраняя фоновое индексирование игровых файлов и разгружая накопитель.",
                "AI: Configured Windows Search indexer service to Manual, eliminating background disk thrashing during game sessions.",
                "Medium / Search Debloat",
                TweakTag.ServicesDebloat | TweakTag.StorageTune | TweakTag.MemoryClean),

            // -------------------------------------------------------------------------
            // SYSTEM MAINTENANCE (17 TWEAKS)
            // -------------------------------------------------------------------------
            new("maint_auto_reboot_update_disable",
                "ИИ: Запрещена принудительная перезагрузка системы службой Windows Update при активном сеансе пользователя во время игры.",
                "AI: Prevented Windows Update from forcibly rebooting the system while user is logged in during active sessions.",
                "Safe / No Forced Reboot",
                TweakTag.SystemHygiene),

            new("maint_driver_overwriting_disable",
                "ИИ: Заблокирована автоматическая перезапись чистых видеодрайверов NVIDIA/AMD типовыми версиями из Центра обновлений Windows.",
                "AI: Blocked Windows Update from silently overwriting clean GPU display drivers with generic OEM versions.",
                "Safe / Driver Protection",
                TweakTag.SystemHygiene | TweakTag.GpuCore),

            new("maint_maintenance_wake_disable",
                "ИИ: Отключены таймеры автоматического пробуждения ПК для выполнения ночного обслуживания Windows.",
                "AI: Disabled Automatic Maintenance wake timers to prevent sleep interruptions.",
                "Safe / Sleep Maintenance",
                TweakTag.PowerPlan | TweakTag.SystemHygiene),

            new("maint_update_peer_caching_disable",
                "ИИ: Отключен локальный HTTP-сервер пиринговой раздачи обновлений Delivery Optimization (порт 7680).",
                "AI: Disabled Delivery Optimization local HTTP peer caching server on port 7680.",
                "Safe / LAN Clean",
                TweakTag.Network | TweakTag.SystemHygiene),

            new("maint_event_log_system_size",
                "ИИ: Буфер системного журнала событий расширен до 64 МБ, предотвращая потерю записей при диагностике сбоев.",
                "AI: Expanded System Event Log circular buffer to 64MB to preserve crash diagnostics.",
                "Safe / Diagnostics",
                TweakTag.SystemHygiene | TweakTag.StorageTune),

            new("maint_event_log_application_size",
                "ИИ: Буфер журнала приложений расширен до 64 МБ для фиксации дампов вылетов игровых движков.",
                "AI: Expanded Application Event Log circular buffer to 64MB for game crash diagnosis.",
                "Safe / Diagnostics",
                TweakTag.SystemHygiene | TweakTag.StorageTune),

            new("maint_auto_update_download_notification",
                "ИИ: Настроено предварительное уведомление перед загрузкой обновлений Windows, исключая скрытые просадки пинга.",
                "AI: Configured notification prompt before downloading Windows updates to avoid bandwidth drops in matches.",
                "Safe / Bandwidth Guard",
                TweakTag.SystemHygiene | TweakTag.Network),

            new("maint_smart_card_removal_policy_disable",
                "ИИ: Отключен опрос подсистемы смарт-карт и аппаратных криптографических считывателей на десктопе.",
                "AI: Disabled smart card cryptographic subsystem polling on desktop gaming PC.",
                "Safe / Clean Subsystem",
                TweakTag.ServicesDebloat | TweakTag.SystemHygiene),

            new("maint_disk_cleanup_old_logs",
                "ИИ: Включена регулярная очистка устаревших дампов памяти и логов ошибок старше 7 дней для сбережения SSD.",
                "AI: Enabled automatic purge of memory crash dumps older than 7 days to preserve SSD capacity.",
                "Safe / SSD Clean",
                TweakTag.StorageTune | TweakTag.MemoryClean),

            new("maint_component_store_cleanup_threshold",
                "ИИ: Включена агрессивная очистка устаревших пакетов хранилища компонентов WinSxS, освобождая гигабайты памяти на SSD.",
                "AI: Enabled WinSxS component store scavenging to reclaim gigabytes of SSD disk space.",
                "Safe / WinSxS Clean",
                TweakTag.StorageTune | TweakTag.MemoryClean),

            new("maint_corrupt_icon_cache_auto_refresh",
                "ИИ: Кэш иконок Windows Shell увеличен до 4096 КБ, предотвращая мерцание и долгую перерисовку ярлыков на рабочем столе.",
                "AI: Expanded Windows Shell icon cache to 4MB to prevent desktop shortcut flickering and redraw stalls.",
                "Safe / Icon Cache",
                TweakTag.SystemHygiene | TweakTag.DwmDirectFlip),

            new("maint_windows_update_active_hours_expand",
                "ИИ: Период активности (Active Hours) расширен до 18 часов (с 6:00 до 0:00), полностью исключая перезагрузки во время вечерних игр.",
                "AI: Expanded Windows Update Active Hours to 18 hours to prevent evening gaming reboot interruptions.",
                "Safe / Active Hours",
                TweakTag.SystemHygiene | TweakTag.GameMode),

            new("maint_disk_defrag_ssd_disable",
                "ИИ: Отключено redundant-дефрагментирование твердотельного накопителя SSD, предотвращая лишний износ ячеек памяти.",
                "AI: Disabled redundant scheduled defragmentation analysis on SSD to prevent write wear.",
                "Safe / SSD Health",
                TweakTag.StorageTune,
                hw => hw.Storage.Any(s => s.IsSsd)),

            new("maint_system_restore_frequency_limit",
                "ИИ: Установлен суточный интервал создания контрольных точек восстановления, предотвращая дублирование точек и засорение диска.",
                "AI: Enforced 24-hour restore point interval to avoid duplicate snapshots and drive bloat.",
                "Safe / Restore Frequency",
                TweakTag.StorageTune | TweakTag.SystemHygiene),

            new("maint_delivery_optimization_cache_clean",
                "ИИ: Срок хранения кэша Delivery Optimization ограничен 3 днями, предотвращая бесконтрольное разрастание папки загрузок.",
                "AI: Capped Delivery Optimization cache retention to 3 days to reclaim disk space.",
                "Safe / Cache Clean",
                TweakTag.StorageTune | TweakTag.MemoryClean),

            new("maint_wer_queue_size_limit",
                "ИИ: Очередь отчетов об ошибках Windows Error Reporting ограничена 5 записями, экономя дисковое пространство.",
                "AI: Restricted Windows Error Reporting queue to 5 reports, preventing disk clutter.",
                "Safe / Queue Limit",
                TweakTag.TelemetryPrivacy | TweakTag.StorageTune),

            new("maint_bsod_auto_restart_enable",
                "ИИ: Отключена мгновенная перезагрузка при BSOD, сохраняя читаемость стоп-кодов и сбойных модулей для диагностики.",
                "AI: Disabled instantaneous auto-reboot on bugcheck (BSOD) so crash stop codes remain visible.",
                "Safe / BSOD Diagnosis",
                TweakTag.SystemHygiene),

            // -------------------------------------------------------------------------
            // WINDOWS EXPLORER & UI (17 TWEAKS)
            // -------------------------------------------------------------------------
            new("ui_show_file_extensions",
                "ИИ: Включено постоянное отображение расширений файлов (.cfg, .ini, .exe), упрощая настройку конфигов игр.",
                "AI: Enabled file extensions display for all file types for seamless game config editing.",
                "Safe / Explorer View",
                TweakTag.SystemHygiene),

            new("ui_show_hidden_files",
                "ИИ: Включено отображение скрытых файлов и системных папок (AppData), ускоряя доступ к кэшам и сейвам игр.",
                "AI: Enabled hidden files and AppData folders display for rapid access to game configs and saves.",
                "Safe / Explorer View",
                TweakTag.SystemHygiene),

            new("ui_separate_process_folders",
                "ИИ: Окна Проводника изолированы в отдельные процессы памяти, предотвращая падение оболочки рабочего стола.",
                "AI: Configured folder windows in isolated process memory space to prevent shell crashes.",
                "Safe / Process Isolation",
                TweakTag.SystemHygiene | TweakTag.MemoryClean),

            new("ui_open_explorer_to_this_pc",
                "ИИ: Проводник настроен на открытие раздела 'Этот компьютер' по умолчанию вместо облачной панели Быстрого доступа.",
                "AI: Set File Explorer default landing to 'This PC' for immediate drive access.",
                "Safe / Snappy Explorer",
                TweakTag.SystemHygiene),

            new("ui_compact_view_explorer",
                "ИИ: В Проводнике Windows 11 включен компактный режим отображения без избыточных сенсорных отступов.",
                "AI: Enabled compact spacing mode in Windows 11 File Explorer for high information density.",
                "Safe / Compact UI",
                TweakTag.SystemHygiene),

            new("ui_classic_context_menu",
                "ИИ: Восстановлено полноценное классическое контекстное меню Windows 11 без медленного клика 'Показать дополнительные параметры'.",
                "AI: Restored classic full right-click context menu in Windows 11, eliminating secondary click delays.",
                "Safe / Classic Menu",
                TweakTag.SystemHygiene | TweakTag.Input),

            new("ui_disable_recent_files_quick_access",
                "ИИ: Отключено отслеживание и сохранение недавно открытых файлов в панели быстрого доступа Проводника.",
                "AI: Disabled recent files tracking in Quick Access to eliminate metadata overhead and preserve privacy.",
                "Safe / Clean Explorer",
                TweakTag.TelemetryPrivacy | TweakTag.SystemHygiene),

            new("ui_disable_thumbnail_cache_clearing",
                "ИИ: Запрещено удаление кэша эскизов при перезагрузке, устраняя повторную генерацию миниатюр в папках со скриншотами.",
                "AI: Prevented thumbnail cache purging on restart to eliminate redundant image thumbnail rebuilding.",
                "Safe / Thumbnail Cache",
                TweakTag.StorageTune | TweakTag.SystemHygiene),

            new("ui_show_super_hidden_system_files",
                "ИИ: Разблокировано отображение защищенных системных файлов для продвинутой диагностики и редактирования конфигов.",
                "AI: Unlocked protected operating system files visibility for advanced system inspection.",
                "Safe / Power User",
                TweakTag.SystemHygiene),

            new("ui_show_drive_letters_first",
                "ИИ: Буквы дисков вынесены перед метками томов (C:, D:), ускоряя визуальный поиск накопителей в проводнике.",
                "AI: Configured drive letters to display before volume labels for rapid drive identification.",
                "Safe / Clean UI",
                TweakTag.SystemHygiene),

            new("ui_disable_sharing_wizard",
                "ИИ: Отключен мастер общего доступа в проводнике, восстанавливая прямую вкладку управления правами доступа NTFS.",
                "AI: Disabled Explorer network sharing wizard, restoring direct NTFS permissions tab.",
                "Safe / Security Tab",
                TweakTag.SystemHygiene | TweakTag.Network),

            new("ui_disable_folder_type_auto_discovery",
                "ИИ: Отключено автоматическое сканирование содержимого папок (Folder Template Sniffing), устраняя задержки открытия папок с модами.",
                "AI: Disabled folder template auto-discovery sniffing, eliminating stalls when opening directories with thousands of files.",
                "Safe / Folder Speed",
                TweakTag.StorageTune | TweakTag.SystemHygiene),

            new("ui_taskbar_search_icon_only",
                "ИИ: Широкая строка поиска Windows 11 свернута в компактную иконку, освобождая место на панели задач.",
                "AI: Minimized taskbar search box into a clean icon to maximize taskbar workspace.",
                "Safe / Clean Taskbar",
                TweakTag.SystemHygiene),

            new("ui_disable_shortcut_suffix",
                "ИИ: Отключено автоматическое добавление суффикса ' - Ярлык' к создаваемым ярлыкам на рабочем столе.",
                "AI: Disabled ' - Shortcut' suffix on newly created desktop and folder links.",
                "Safe / Clean Shortcuts",
                TweakTag.SystemHygiene),

            new("ui_taskbar_combine_behavior",
                "ИИ: Настроена группировка кнопок панели задач 'Когда панель переполнена', сохраняя читаемость заголовков открытых окон.",
                "AI: Configured taskbar button grouping to 'When Taskbar is Full' to keep window titles readable.",
                "Safe / Multi-Tasking",
                TweakTag.SystemHygiene),

            new("ui_disable_navigation_pane_expand_open",
                "ИИ: Отключено автоматическое раскрытие дерева папок в левой панели Проводника, ускоряя навигацию.",
                "AI: Disabled navigation pane auto-expansion in File Explorer for clean, hitch-free folder browsing.",
                "Safe / Fluid Navigation",
                TweakTag.SystemHygiene),

            new("ui_restore_classic_ribbon_file_transfer",
                "ИИ: Диалог копирования файлов настроен на постоянное отображение развернутого графика скорости передачи.",
                "AI: Configured file transfer dialogs to always show the full detailed transfer rate graph.",
                "Safe / File Progress",
                TweakTag.SystemHygiene | TweakTag.StorageTune)
        };

        // Guarantee all compatible tweaks in the entire database have candidate representations
        var catalogIds = new HashSet<string>(catalog.Select(c => c.Id), StringComparer.OrdinalIgnoreCase);
        foreach (var def in compatibleDefMap.Values)
        {
            if (!catalogIds.Contains(def.Id))
            {
                var tags = InferTagsForCategory(def.Category);
                string desc = !string.IsNullOrWhiteSpace(def.ProblemSolved) ? def.ProblemSolved : def.Description;
                catalog.Add(new CandidateTweakItem(
                    def.Id,
                    $"ИИ: {desc} ({def.ExpectedResult})",
                    $"AI: {desc} ({def.ExpectedResult})",
                    def.Risk.ToString(),
                    tags
                ));
            }
        }

        // Filter eligible candidate definitions based on hardware profile and system compatibility
        var eligibleCandidates = catalog
            .Where(item => compatibleDefMap.ContainsKey(item.Id))
            .Where(item => item.HardwareFilter == null || item.HardwareFilter(hardware))
            .ToList();

        // Base target count scaled strictly by Risk Level:
        // Risk 0 (Safe): ~60–80 tweaks (target range 65-79)
        // Risk 1 (Balanced): ~85–120 tweaks (target range 92-118)
        // Risk 2 (Maximum): ~120–160+ tweaks (target range 128-158)
        int baseTarget = riskIndex switch
        {
            0 => 68,
            1 => 96,
            _ => 132
        };

        // Modulate dynamically by Scenario:
        int scenarioOffset = scenarioIndex switch
        {
            0 => riskIndex switch { 0 => 2, 1 => 3, _ => 5 },    // Esports: focused on latency/net/cpu
            1 => riskIndex switch { 0 => 6, 1 => 10, _ => 13 },  // AAA: deep graphics, gaming, storage, power
            2 => riskIndex switch { 0 => 4, 1 => 6, _ => 9 },    // Streaming: audio, net, multitasking, services
            _ => riskIndex switch { 0 => -3, 1 => -5, _ => -6 }  // Balanced: conservative hygiene
        };

        // Modulate dynamically by Priority:
        int priorityOffset = priorityIndex switch
        {
            0 => riskIndex switch { 0 => 3, 1 => 5, _ => 7 },   // Lowest Latency
            1 => riskIndex switch { 0 => 4, 1 => 6, _ => 9 },   // Max FPS
            2 => riskIndex switch { 0 => 2, 1 => 3, _ => 5 },   // Smooth Frametime
            _ => riskIndex switch { 0 => 0, 1 => 1, _ => 2 }    // Clean RAM
        };

        // Modulate by hardware capability:
        int hwOffset = 0;
        if (gpu.IsNvidia || gpu.IsAmd) hwOffset += (riskIndex == 0 ? 1 : 2);
        if (cpu.IsAmd || cpu.IsIntel) hwOffset += 1;
        if (hasSsd) hwOffset += (riskIndex == 0 ? 1 : 2);
        if (!isLaptop) hwOffset += (riskIndex == 0 ? 1 : 2);
        if (ram.TotalGb >= 32) hwOffset += 1;

        int targetCount = baseTarget + scenarioOffset + priorityOffset + hwOffset;
        int minTargetBound = riskIndex switch { 0 => 60, 1 => 85, _ => 120 };
        int maxTargetBound = riskIndex switch { 0 => 80, 1 => 120, _ => 165 };
        targetCount = Math.Clamp(targetCount, minTargetBound, maxTargetBound);

        int CalculateScore(CandidateTweakItem item)
        {
            int score = 50;

            // 1. Scenario weighting
            switch (scenarioIndex)
            {
                case 0: // Киберспорт (CS2/Valorant): Prioritize Input, Network, Timer Resolution, DWM DirectFlip
                    if ((item.Tags & TweakTag.Input) != 0) score += 45;
                    if ((item.Tags & TweakTag.Network) != 0) score += 40;
                    if ((item.Tags & TweakTag.TimerResolution) != 0) score += 40;
                    if ((item.Tags & TweakTag.DwmDirectFlip) != 0) score += 35;
                    if ((item.Tags & TweakTag.GameMode) != 0) score += 20;
                    if (item.Id.Contains("mouse_acceleration", StringComparison.OrdinalIgnoreCase)) score += 35;
                    if (item.Id.Contains("nagle", StringComparison.OrdinalIgnoreCase)) score += 35;
                    if (item.Id.Contains("rsc", StringComparison.OrdinalIgnoreCase)) score += 35;
                    if ((item.Tags & (TweakTag.ServicesDebloat | TweakTag.TelemetryPrivacy)) != 0 
                        && (item.Tags & TweakTag.Input) == 0 
                        && (item.Tags & TweakTag.Network) == 0) score -= 15;
                    break;

                case 1: // Тяжёлые AAA-игры: Prioritize GPU, CPU Core unparking, Power plan, Game Mode, Storage
                    if ((item.Tags & TweakTag.GpuCore) != 0) score += 45;
                    if ((item.Tags & TweakTag.CpuThrottling) != 0) score += 40;
                    if ((item.Tags & TweakTag.PowerPlan) != 0) score += 35;
                    if ((item.Tags & TweakTag.GameMode) != 0) score += 30;
                    if ((item.Tags & TweakTag.StorageTune) != 0) score += 25;
                    if ((item.Tags & TweakTag.FrametimeSmooth) != 0) score += 20;
                    if (item.Id.Contains("hags", StringComparison.OrdinalIgnoreCase)) score += 35;
                    if (item.Id.Contains("core_parking", StringComparison.OrdinalIgnoreCase)) score += 35;
                    if ((item.Tags & TweakTag.Network) != 0 && (item.Tags & TweakTag.GpuCore) == 0) score -= 15;
                    break;

                case 2: // Стриминг и OBS: Prioritize Network buffer, Audio MMCSS latency, CPU process priority, Game DVR
                    if ((item.Tags & TweakTag.StreamingObs) != 0) score += 50;
                    if ((item.Tags & TweakTag.AudioLatency) != 0) score += 45;
                    if ((item.Tags & TweakTag.Network) != 0) score += 35;
                    if ((item.Tags & TweakTag.FrametimeSmooth) != 0) score += 30;
                    if ((item.Tags & TweakTag.CpuThrottling) != 0) score += 25;
                    if ((item.Tags & TweakTag.ServicesDebloat) != 0 && (item.Tags & TweakTag.StreamingObs) == 0) score -= 10;
                    break;

                default: // 3: Базовое / Сбалансированное: Broad general system hygiene
                    if ((item.Tags & TweakTag.SystemHygiene) != 0) score += 30;
                    if ((item.Tags & TweakTag.TelemetryPrivacy) != 0) score += 25;
                    if ((item.Tags & TweakTag.ServicesDebloat) != 0) score += 20;
                    if ((item.Tags & TweakTag.StorageTune) != 0) score += 20;
                    if ((item.Tags & TweakTag.GpuCore) != 0) score += 20;
                    if ((item.Tags & TweakTag.CpuThrottling) != 0) score += 20;
                    if ((item.Tags & (TweakTag.TimerResolution | TweakTag.Input)) != 0 && (item.Tags & TweakTag.SystemHygiene) == 0) score -= 10;
                    break;
            }

            // 2. Priority weighting
            switch (priorityIndex)
            {
                case 0: // Минимальная задержка: Boost Input & Network tweaks
                    if ((item.Tags & TweakTag.Input) != 0) score += 35;
                    if ((item.Tags & TweakTag.Network) != 0) score += 30;
                    if ((item.Tags & TweakTag.TimerResolution) != 0) score += 25;
                    if ((item.Tags & TweakTag.DwmDirectFlip) != 0) score += 25;
                    break;

                case 1: // Максимальный FPS: Boost GPU & CPU throttling tweaks
                    if ((item.Tags & TweakTag.GpuCore) != 0) score += 35;
                    if ((item.Tags & TweakTag.CpuThrottling) != 0) score += 35;
                    if ((item.Tags & TweakTag.PowerPlan) != 0) score += 25;
                    if ((item.Tags & TweakTag.GameMode) != 0) score += 20;
                    break;

                case 2: // Плавный фреймтайм: Boost Memory standby list, MMCSS, Shader cache tweaks
                    if ((item.Tags & TweakTag.FrametimeSmooth) != 0) score += 35;
                    if ((item.Tags & TweakTag.MemoryStandby) != 0) score += 35;
                    if ((item.Tags & TweakTag.AudioLatency) != 0) score += 30;
                    if ((item.Tags & TweakTag.DwmDirectFlip) != 0) score += 25;
                    break;

                case 3: // Очистка памяти: Boost Services, Telemetry, RAM cache tweaks
                    if ((item.Tags & TweakTag.MemoryClean) != 0) score += 40;
                    if ((item.Tags & TweakTag.ServicesDebloat) != 0) score += 35;
                    if ((item.Tags & TweakTag.TelemetryPrivacy) != 0) score += 30;
                    if ((item.Tags & TweakTag.SystemHygiene) != 0) score += 25;
                    break;
            }

            // 3. Hardware & configuration boosts
            if (cpu.IsAmd && item.Id.Contains("amd", StringComparison.OrdinalIgnoreCase)) score += 25;
            if (cpu.IsIntel && item.Id.Contains("intel", StringComparison.OrdinalIgnoreCase)) score += 25;
            if (gpu.IsNvidia && item.Id.Contains("nvidia", StringComparison.OrdinalIgnoreCase)) score += 25;
            if (gpu.IsAmd && item.Id.Contains("amd", StringComparison.OrdinalIgnoreCase)) score += 25;
            if (gpu.IsIntel && item.Id.Contains("intel", StringComparison.OrdinalIgnoreCase)) score += 25;
            if (hasSsd && (item.Tags & TweakTag.StorageTune) != 0) score += 15;
            if (!isLaptop && item.Id.Contains("pcie", StringComparison.OrdinalIgnoreCase)) score += 15;
            if (ram.TotalGb <= 16 && (item.Tags & TweakTag.MemoryClean) != 0) score += 20;
            if (ram.TotalGb >= 32 && item.Id.Contains("paging_executive", StringComparison.OrdinalIgnoreCase)) score += 20;

            return score;
        }

        // Rank candidates by dynamic heuristic score and category-depth distribution
        var scoredCandidates = eligibleCandidates
            .Select(item => new { Item = item, Score = CalculateScore(item) })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Item.Id)
            .ToList();

        // Group scored candidates by category group to guarantee deep representation across all categories
        var groupedCandidates = scoredCandidates
            .GroupBy(sc => GetCategoryGroup(sc.Item.Id, compatibleDefMap))
            .ToDictionary(g => g.Key, g => g.ToList());

        var selectedItems = new List<CandidateTweakItem>();
        var selectedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Phase 1: Guarantee base depth across all category groups up to their minimum quotas
        foreach (var groupKv in groupedCandidates)
        {
            if (selectedItems.Count >= targetCount) break;
            var quota = GetGroupMinQuota(groupKv.Key, riskIndex, scenarioIndex, priorityIndex);
            foreach (var sc in groupKv.Value.Take(quota))
            {
                if (selectedItems.Count >= targetCount) break;
                if (selectedIds.Add(sc.Item.Id))
                {
                    selectedItems.Add(sc.Item);
                }
            }
        }

        // Phase 2: Top up remaining slots to hit dynamic targetCount from overall highest-scoring candidates
        foreach (var sc in scoredCandidates)
        {
            if (selectedItems.Count >= targetCount) break;
            if (selectedIds.Add(sc.Item.Id))
            {
                selectedItems.Add(sc.Item);
            }
        }

        var selected = new List<LiquidAiTweakSelection>();
        foreach (var item in selectedItems)
        {
            selected.Add(new LiquidAiTweakSelection
            {
                TweakId = item.Id,
                Rationale = isRu ? item.RuRationale : item.EnRationale,
                RiskAssessment = item.RiskDesc
            });
        }

        string scenarioDesc = scenarioIndex switch
        {
            0 => isRu ? "соревновательного киберспорта (минимальный инпут-лаг, сетевые очереди и DirectFlip)" : "competitive esports (ultra-low input latency, network queues, and DirectFlip)",
            1 => isRu ? "тяжёлых AAA-игр (максимальная утилизация GPU, разблокировка ядер CPU и VRAM-буфер)" : "heavy AAA gaming (maximum GPU utilization, unparked CPU cores, and VRAM bandwidth)",
            2 => isRu ? "стриминга и OBS (стабильность фреймтайма, аудиопотоки MMCSS и разгрузка энкодера)" : "streaming & OBS (frametime stability, MMCSS audio priorities, and encoder offloading)",
            _ => isRu ? "сбалансированной оптимизации системы (комплексная очистка фоновых служб и телеметрии)" : "balanced system hygiene (comprehensive background services and telemetry purging)"
        };

        string priorityDesc = priorityIndex switch
        {
            0 => isRu ? "с упором на мгновенный отклик ввода" : "focused on lowest click-to-photon latency",
            1 => isRu ? "с упором на максимальный FPS" : "focused on maximum framerate throughput",
            2 => isRu ? "с упором на идеальную плавность фреймтайма" : "focused on hitch-free frametime pacing",
            _ => isRu ? "с упором на освобождение памяти и служб" : "focused on RAM reclamation and service debloat"
        };

        var result = new LiquidAiResult
        {
            PresetName = $"AI: {scenarioName} ({priorityName})",
            UsedOnlineModel = false,
            IsOnlineAiGenerated = false,
            ModelName = "SilkThread IOST Copilot Engine",
            AiModelUsed = "SilkThread IOST Copilot Engine",
            GenerationSource = "Локальный эксперт IOST",
            Summary = isRu
                ? $"Интеллектуальный пресет IOST ({selected.Count} параметров), синтезированный под аппаратный профиль: {hardware.Cpu.Name} ({hardware.Cpu.CoreCount}C/{hardware.Cpu.LogicalProcessorCount}T) + {hardware.PrimaryGpu.Name} ({hardware.PrimaryGpu.VramGb:F1} ГБ VRAM) + {hardware.Ram.TotalGb:F0} ГБ ОЗУ. Адаптирован для {scenarioDesc} {priorityDesc}."
                : $"IOST dynamic expert preset ({selected.Count} tweaks) synthesized for hardware profile: {hardware.Cpu.Name} ({hardware.Cpu.CoreCount}C/{hardware.Cpu.LogicalProcessorCount}T) + {hardware.PrimaryGpu.Name} ({hardware.PrimaryGpu.VramGb:F1}GB VRAM) + {hardware.Ram.TotalGb:F0}GB RAM. Tailored for {scenarioDesc} {priorityDesc}.",
            Selections = selected
        };

        return result;
    }
}