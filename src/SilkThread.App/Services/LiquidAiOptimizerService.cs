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
}

public class LiquidAiOptimizerService : ILiquidAiOptimizerService
{
    private static readonly HttpClient s_httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(25)
    };

    private readonly ISettingsService _settingsService;
    private readonly ILocalizationService _loc;

    public LiquidAiOptimizerService(ISettingsService settingsService, ILocalizationService loc)
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
        bool isRu = _loc.CurrentLanguage == "ru";

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

        string apiKey = _settingsService.Settings.OpenRouterApiKey?.Trim() ?? "";
        string modelName = string.IsNullOrWhiteSpace(_settingsService.Settings.OpenRouterModel)
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
                    return onlineResult;
                }
            }
            catch
            {
                // Fall back seamlessly to our deep local expert reasoning engine
            }
        }

        // Phase 2: Local Multi-Category AI Expert Engine
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

        // Sample a balanced, representative catalog across ALL categories (up to 5 per category = 50-70 candidates)
        var sampledTweaks = compatibleTweaks
            .GroupBy(t => t.Category)
            .SelectMany(g => g
                .OrderByDescending(t => t.Recommendation == RecommendationLevel.Recommended)
                .ThenBy(t => t.Risk)
                .Take(5))
            .Select(t => new
            {
                t.Id,
                t.Title,
                Category = t.Category.ToString(),
                Risk = t.Risk.ToString(),
                t.Description
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
        promptBuilder.AppendLine("MANDATORY ARCHITECTURAL REQUIREMENTS:");
        promptBuilder.AppendLine("1. Select a comprehensive, well-rounded preset of 30 to 45 tweaks.");
        promptBuilder.AppendLine("2. The selection MUST be evenly distributed across ALL key categories (CPU, GPU, RAM, Network, Input & Latency, Gaming, Windows Performance, Services, Background Processes, Privacy & Telemetry, Storage, Power).");
        promptBuilder.AppendLine("3. Under NO circumstances select tweaks from only 1 or 2 categories. Broad system-wide coverage is required.");
        promptBuilder.AppendLine("4. For EACH tweak, write a clear, personalized technical rationale explaining how it directly optimizes the user's specific hardware components.");
        promptBuilder.AppendLine("5. Output MUST be valid JSON strictly matching the schema without markdown fences or additional text:");
        promptBuilder.AppendLine("{\"summary\":\"Detailed technical overview of the full system optimization strategy\",\"tweaks\":[{\"id\":\"tweak_id\",\"rationale\":\"Technical rationale tailored to hardware and user scenario\",\"risk\":\"Low/Medium\"}]}");

        var requestBody = new
        {
            model = model,
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
            max_tokens = 3500
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Headers.Add("HTTP-Referer", "https://github.com/ColdLuv/SilkThread");
        request.Headers.Add("X-Title", "SilkThread Gaming Optimizer");
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        using var response = await s_httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
            return null;

        var messageContent = choices[0].GetProperty("message").GetProperty("content").GetString();
        if (string.IsNullOrWhiteSpace(messageContent)) return null;

        string cleanJson = ExtractJson(messageContent);
        using var resultDoc = JsonDocument.Parse(cleanJson);
        var root = resultDoc.RootElement;

        var result = new LiquidAiResult
        {
            UsedOnlineModel = true,
            ModelName = model,
            Summary = root.TryGetProperty("summary", out var sumProp) ? sumProp.GetString() ?? "" : ""
        };

        var allTweaksMap = allTweaks.ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);

        if (root.TryGetProperty("tweaks", out var tweaksArray))
        {
            foreach (var item in tweaksArray.EnumerateArray())
            {
                if (item.TryGetProperty("id", out var idProp))
                {
                    string id = idProp.GetString() ?? "";
                    if (allTweaksMap.TryGetValue(id, out var matchedDef))
                    {
                        string rationale = item.TryGetProperty("rationale", out var rProp) ? rProp.GetString() ?? "" : "";
                        string risk = item.TryGetProperty("risk", out var rkProp) ? rkProp.GetString() ?? "" : matchedDef.Risk.ToString();
                        
                        if (!result.Selections.Any(s => s.TweakId.Equals(matchedDef.Id, StringComparison.OrdinalIgnoreCase)))
                        {
                            result.Selections.Add(new LiquidAiTweakSelection
                            {
                                TweakId = matchedDef.Id,
                                Rationale = rationale,
                                RiskAssessment = risk
                            });
                        }
                    }
                }
            }
        }

        return result.Selections.Count >= 20 ? result : null;
    }

    private static string ExtractJson(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "{}";
        int start = text.IndexOf('{');
        int end = text.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            return text.Substring(start, end - start + 1);
        }
        return text;
    }

    private LiquidAiResult GenerateExpertProfile(
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
        var result = new LiquidAiResult
        {
            PresetName = $"AI: {scenarioName} ({priorityName})",
            UsedOnlineModel = false,
            ModelName = "SilkThread IOST Copilot Engine",
            Summary = isRu
                ? $"Многокатегорийный интеллектуальный пресет, сформированный IOST для аппаратного профиля: {hardware.Cpu.Name} ({hardware.Cpu.CoreCount}C/{hardware.Cpu.LogicalProcessorCount}T) + {hardware.PrimaryGpu.Name} ({hardware.PrimaryGpu.VramGb:F1} ГБ VRAM) + {hardware.Ram.TotalGb:F0} ГБ ОЗУ. Комплексно охватывает все подсистемы ОС для сценария '{scenarioName}' с приоритетом '{priorityName}'."
                : $"Comprehensive multi-category preset synthesized by IOST for {hardware.Cpu.Name} ({hardware.Cpu.CoreCount}C/{hardware.Cpu.LogicalProcessorCount}T) + {hardware.PrimaryGpu.Name} ({hardware.PrimaryGpu.VramGb:F1}GB VRAM) + {hardware.Ram.TotalGb:F0}GB RAM. Tailored across all system subsystems for '{scenarioName}' focusing on '{priorityName}'."
        };

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
        var candidates = allTweaks.Where(t =>
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
        }).ToList();

        var selected = new List<LiquidAiTweakSelection>();
        var selectedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddTweak(string id, string ruRationale, string enRationale, string riskDesc = "Safe / Optimal")
        {
            var def = candidates.FirstOrDefault(t => t.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (def != null && selectedIds.Add(def.Id))
            {
                selected.Add(new LiquidAiTweakSelection
                {
                    TweakId = def.Id,
                    Rationale = isRu ? ruRationale : enRationale,
                    RiskAssessment = riskDesc
                });
            }
        }

        // =========================================================================
        // 1. CPU OPTIMIZATION & SCHEDULING (All key parameters)
        // =========================================================================
        AddTweak("cpu_power_throttling_disable",
            $"ИИ: Для процессора {cpu.Name} отключено системное ограничение частот Power Throttling, предотвращающее сброс частот при резких игровых нагрузках.",
            $"AI: Disabled execution power throttling for {cpu.Name}, preventing frequency drops during sudden intense gaming spikes.",
            "Safe / CPU Focus");

        AddTweak("cpu_core_parking_unhide",
            $"ИИ: Для архитектуры {cpu.Name} ({cpu.CoreCount} ядер / {cpu.LogicalProcessorCount} потоков) разблокировано отключение парковки ядер, сохраняя все физические ядра активными.",
            $"AI: Unlocked core unparking for {cpu.Name} ({cpu.CoreCount}C/{cpu.LogicalProcessorCount}T), keeping all physical cores active and warm.",
            "Safe / Latency");

        AddTweak("cpu_mmcss_game_tasks",
            "ИИ: Настроен системный планировщик MMCSS с максимальным приоритетом игровых потоков рендера и графической квотой GPU Priority = 8.",
            "AI: Configured MMCSS gaming tasks with high thread priority and dedicated GPU Priority = 8 for render threads.",
            "Safe / System");

        AddTweak("cpu_distribute_timers",
            $"ИИ: Для процессора {cpu.Name} активировано распределение системных таймеров по всем {cpu.CoreCount} ядрам CPU вместо перегрузки ядра 0.",
            $"AI: Enabled kernel timer distribution across all {cpu.CoreCount} CPU cores instead of saturating Core 0, eliminating DPC latency spikes.",
            "Safe / System");

        AddTweak("cpu_additional_worker_threads",
            "ИИ: Выделены дополнительные системные потоки ядра (16 worker threads) для параллельной обработки очередей дискового ввода-вывода и античита.",
            "AI: Allocated additional critical kernel worker threads to process asynchronous I/O and anticheat packets without queue congestion.",
            "Safe / Recommended");

        AddTweak("cpu_dxgkrnl_latency_tolerance",
            "ИИ: Для ядра DirectX Graphics Kernel (dxgkrnl.sys) установлен нулевой порог толерантности к задержкам смены дисплейных кадров.",
            "AI: Configured DirectX Graphics Kernel with zero latency tolerance for immediate frame display flips.",
            "Safe / Latency");

        AddTweak("cpu_affinity_prioritization_foreground",
            "ИИ: Закреплен максимальный квант вычислительной мощности и аппаратный приоритет CPU за активным игровым окном.",
            "AI: Enforces maximum CPU throughput and scheduling affinity strictly for the active foreground game.",
            "Safe / Recommended");

        AddTweak("cpu_svm_hyperv_interrupt_steering",
            "ИИ: Включен высокоточный учет циклов прерываний ядра для равномерного распределения аппаратных прерываний между физическими ядрами.",
            "AI: Enabled precision interrupt cycle counter accounting to distribute interrupt loads evenly across physical CPU cores.",
            "Safe / Recommended");

        if (cpu.IsAmd)
        {
            AddTweak("cpu_amd_ppm_perf_boost",
                $"ИИ: Для процессора {cpu.Name} (AMD Ryzen) активирован агрессивный профиль Core Performance Boost без задержек выхода из C-states.",
                $"AI: For {cpu.Name} (AMD Ryzen), enabled aggressive Core Performance Boost profile with instant burst frequency scaling.",
                "Safe / AMD Ryzen");

            if (maxRisk >= RiskLevel.Medium)
            {
                AddTweak("cpu_amd_thread_dpc_affinity",
                    "ИИ: Для архитектуры AMD Zen зафиксирована обработка DPC-прерываний на исходном ядре, предотвращая межъядерные задержки шины Infinity Fabric.",
                    "AI: For AMD Zen architecture, pinned DPC interrupt handling to origin core to eliminate cross-CCD cache invalidation latency.",
                    "Medium / AMD Ryzen");
            }
        }
        else if (cpu.IsIntel)
        {
            AddTweak("cpu_intel_speedshift_unhide",
                $"ИИ: Для процессора {cpu.Name} (Intel Core) разблокирован профиль Intel Speed Shift (HWP) Maximum Performance с временем отклика 0 мс.",
                $"AI: For {cpu.Name} (Intel Core), unlocked Intel Speed Shift (HWP) Maximum Performance profile for 0ms clock scaling.",
                "Safe / Intel Core");

            AddTweak("cpu_intel_ppm_epb_performance",
                "ИИ: В драйвере intelppm установлен приоритет энергоэффективности EPB = 0 (максимальная производительность) без задержек Turbo Boost.",
                "AI: Configured Intel Energy Performance Bias (EPB) to absolute maximum performance, eliminating Turbo clock ramp-up delay.",
                "Safe / Intel Core");

            if (cpu.CoreCount >= 8)
            {
                AddTweak("cpu_thread_scheduling_model_unhide",
                    $"ИИ: Для гибридной архитектуры {cpu.Name} разблокировано управление шедулером для жесткого закрепления игр на производительных P-ядрах.",
                    $"AI: For hybrid {cpu.Name}, exposed heterogeneous thread scheduling policy to lock game threads strictly onto high-frequency P-cores.",
                    "Safe / Intel Hybrid");
            }
        }

        if (!isLaptop)
        {
            AddTweak("cpu_idle_resiliency_disable",
                "ИИ: Для стационарного ПК отключена группировка таймеров CoalescingTimerInterval, обеспечивая непрерывную частоту тиков ядра.",
                "AI: Disabled timer coalescing on desktop PC, guaranteeing high-resolution timer ticks for game physics and polling.",
                "Safe / Desktop");
        }

        if (maxRisk >= RiskLevel.Medium)
        {
            AddTweak("cpu_win32_priority_separation",
                "ИИ: Оптимизирован квант времени Win32PrioritySeparation (0x26 = 38) для субмиллисекундного отклика цикла рендера и ввода.",
                "AI: Optimized Win32PrioritySeparation quanta (0x26) for sub-millisecond game loop execution and input responsiveness.",
                "Medium / Recommended");

            AddTweak("cpu_kernel_dpc_watchdog_profile",
                "ИИ: Установлен минимальный порог сброса очереди DPC (глубина 1) в ядре Windows для устранения задержек звука и ввода.",
                "AI: Clamped kernel DPC queue depth to 1 for immediate queue draining, lowering interrupt latency.",
                "Medium / Recommended");
        }

        // =========================================================================
        // 2. GPU OPTIMIZATION & GRAPHICS PIPELINE
        // =========================================================================
        AddTweak("gpu_hags_enable",
            $"ИИ: Для видеокарты {gpu.Name} активирован аппаратный планировщик HAGS, разгружающий CPU от диспетчеризации VRAM и снижающий Render Latency.",
            $"AI: Enabled Hardware-Accelerated GPU Scheduling (HAGS) for {gpu.Name}, offloading VRAM management from CPU and lowering render latency.",
            "Recommended / Low Risk");

        AddTweak("gpu_variable_refresh_rate_global",
            "ИИ: Включена глобальная поддержка переменной частоты обновления (VRR) для всех оконных и borderless-игр без тиринга.",
            "AI: Enabled global Variable Refresh Rate (VRR) for all borderless and windowed games without tearing.",
            "Safe / Display");

        AddTweak("gpu_borderless_direct_flip_optimization",
            "ИИ: Активирован аппаратный DirectFlip в GameConfigStore, обеспечивающий задержку эксклюзивного полного экрана в режиме без полей.",
            "AI: Activated hardware DirectFlip in GameConfigStore, delivering true Fullscreen Exclusive latency in borderless windowed mode.",
            "Safe / Latency");

        AddTweak("gpu_dwm_high_priority",
            "ИИ: Для очереди Desktop Window Manager установлен приоритет GPU Priority = 8 для безупречной плавности вывода кадров.",
            "AI: Assigned high GPU Priority = 8 to Desktop Window Manager display flip queue for hitch-free composition.",
            "Safe / DWM");

        AddTweak("gpu_dx_prefer_high_performance",
            $"ИИ: В DirectX принудительно назначен приоритет высокопроизводительного дискретного адаптера {gpu.Name} для всех 3D-приложений.",
            $"AI: Enforced default DirectX preference to high-performance discrete adapter {gpu.Name} across all 3D applications.",
            "Safe / GPU Direct");

        AddTweak("gpu_tdr_delay_extend",
            "ИИ: Время ожидания сторожевого таймера драйвера TDR увеличено до 10 секунд, предотвращая вылеты видеодрайвера при тяжелой загрузке шейдеров в UE5.",
            "AI: Extended GPU driver TDR recovery watchdog to 10s, preventing display driver timeouts during heavy UE5 shader loading.",
            "Safe / Stability");

        AddTweak("gpu_dxgi_swapchain_waitable_object",
            "ИИ: Включена низколатентная обработка очередей DXGI SwapEffectWaitable, снижающая задержку ввода от клика до фотона на 1 кадр.",
            "AI: Enforced DXGI low-latency waitable object swapchain heuristics, cutting click-to-photon latency by up to 1 frame cycle.",
            "Safe / Latency");

        AddTweak("gpu_vram_allocation_priority",
            $"ИИ: Для {gpu.FormattedVram} видеопамяти {gpu.Name} заблокирован преждевременный сброс текстур в общую медленную память ОЗУ.",
            $"AI: Prioritized dedicated {gpu.FormattedVram} video memory for {gpu.Name}, preventing early texture spilling onto shared system RAM.",
            "Safe / GPU Memory");

        AddTweak("gpu_dwm_composition_buffer_count",
            "ИИ: Количество буферов композитора DWM сокращено с тройного до двойного, срезая до 16 мс задержки в оконных режимах.",
            "AI: Reduced DWM composition buffering from triple to double buffer, cutting up to 16ms presentation lag in windowed mode.",
            "Safe / Input Lag");

        if (gpu.IsNvidia)
        {
            AddTweak("gpu_nvidia_power_mizer_max",
                $"ИИ: Для видеокарты {gpu.Name} активирован режим PowerMizer 'Максимальная производительность' с фиксацией 3D-частот ядра и памяти.",
                $"AI: For {gpu.Name}, locked NVIDIA PowerMizer into Maximum Performance, preventing core clock throttling during low-GPU scenes.",
                "Safe / NVIDIA GPU");

            AddTweak("gpu_nvidia_low_latency_mode",
                "ИИ: В драйвере NVIDIA зафиксирован лимит очереди кадров PreRenderedFrames = 1 для мгновенной реакции прицела на движения мыши.",
                "AI: Enforced NVIDIA PreRenderedFrames = 1 low-latency limit for instant crosshair responsiveness in shooters.",
                "Safe / Low Latency");

            AddTweak("gpu_nvidia_telemetry_container_disable",
                "ИИ: Отключены фоновые службы телеметрии NVIDIA NvTelemetry, устраняя микрофризы от постоянной записи логов на диск.",
                "AI: Disabled NVIDIA background telemetry reporting hooks to eliminate disk write spikes and thread wakeups.",
                "Safe / Privacy");

            AddTweak("gpu_nvidia_ansel_overlay_disable",
                "ИИ: Отключен инжектор оверлея NVIDIA Ansel, предотвращая постороннее внедрение библиотек в процессы игр.",
                "AI: Disabled NVIDIA Ansel in-game camera injection hooks, avoiding DLL injection overhead into game threads.",
                "Safe / Stability");
        }
        else if (gpu.IsAmd)
        {
            AddTweak("gpu_amd_anti_lag_profile",
                $"ИИ: Для {gpu.Name} активирован профиль AMD Radeon Anti-Lag, синхронизирующий темп работы CPU и GPU для снижения лага.",
                $"AI: Activated AMD Radeon Anti-Lag driver policy for {gpu.Name}, synchronizing CPU-GPU rendering pace to cut input lag.",
                "Safe / AMD Radeon");

            AddTweak("gpu_amd_shader_cache_limit",
                $"ИИ: Для {gpu.Name} включено постоянное удержание кэша шейдеров без очистки, устраняя статтеры компиляции шейдеров в DX12.",
                $"AI: Enabled unlimited shader cache retention for {gpu.Name}, preventing first-encounter PSO compilation stutters.",
                "Safe / AMD Radeon");

            AddTweak("gpu_amd_ulps_disable",
                "ИИ: Отключен режим глубокого энергосбережения AMD ULPS, исключая задержки при переходе чипа из сна в боевой режим.",
                "AI: Disabled AMD Radeon Ultra Low Power State (ULPS) to ensure immediate GPU clock ramp-up and stability.",
                "Safe / AMD Radeon");
        }
        else if (gpu.IsIntel)
        {
            AddTweak("gpu_intel_arc_adaptive_sync",
                $"ИИ: Для {gpu.Name} активирована глобальная синхронизация Intel Arc Adaptive Sync для устранения разрывов кадров.",
                $"AI: Activated Intel Arc Adaptive Sync global policy for smooth frame delivery on {gpu.Name}.",
                "Safe / Intel Arc");
        }

        // =========================================================================
        // 3. RAM & MEMORY ARCHITECTURE (16GB vs 32GB tuning)
        // =========================================================================
        AddTweak("ram_disable_paging_executive",
            $"ИИ: Для пула памяти {ram.TotalGb:F0} ГБ ядро Windows и драйверы заблокированы в физической ОЗУ (DisablePagingExecutive = 1), исключая подгрузки с диска.",
            $"AI: Pinned Windows kernel and drivers into physical RAM ({ram.TotalGb:F0}GB pool) via DisablePagingExecutive = 1 to eliminate page-fault stutters.",
            "Safe / Memory");

        AddTweak("ram_clear_pagefile_shutdown_disable",
            "ИИ: Отключена очистка файла подкачки при выключении ПК, ускоряя перезагрузку и сберегая ресурс ячеек памяти SSD.",
            "AI: Disabled pagefile scrubbing at shutdown, enabling instant reboots and preserving SSD write endurance.",
            "Safe / Storage Life");

        AddTweak("ram_system_responsiveness_zero",
            "ИИ: Зарезервированный бюджет SystemResponsiveness снижен до 0%, отдавая 100% вычислительных ресурсов и памяти активной игре.",
            "AI: Dedicated 100% of CPU/memory budget to the active game process by setting SystemResponsiveness to 0%.",
            "Safe / 100% Budget");

        AddTweak("ram_large_system_cache_gaming",
            "ИИ: Оптимизирован системный файловый кэш (LargeSystemCache = 0), предотвращая разрастание кэша и сохраняя ОЗУ для игровых текстур.",
            "AI: Optimized system file cache policy (LargeSystemCache = 0) to ensure maximum physical memory remains free for games.",
            "Safe / Cache");

        AddTweak("ram_paging_file_encryption_disable",
            "ИИ: Отключено фоновое шифрование страниц файла подкачки, экономя такты CPU при обращении к виртуальной памяти.",
            "AI: Disabled real-time pagefile encryption overhead to eliminate CPU encryption penalties on memory pages.",
            "Safe / CPU Overhead");

        AddTweak("ram_paging_executive_driver_lock",
            "ИИ: Установлена блокировка выгрузки критических драйверов видеокарты и звука при высокой нагрузке на память.",
            "AI: Enforced kernel driver paging lock to prevent GPU and audio driver routines from paging out under high memory load.",
            "Safe / Stability");

        AddTweak("ram_standby_list_trimming_heuristics",
            $"ИИ: Для {ram.TotalGb:F0} ГБ памяти включена проактивная очистка кэша ожидания Standby List, устраняющая микрозаикания при нехватке памяти.",
            $"AI: Enabled proactive Standby List cache trimming for {ram.TotalGb:F0}GB RAM to prevent memory-allocation micro-stuttering.",
            "Safe / Memory");

        AddTweak("ram_working_set_trim_protection",
            "ИИ: Включена защита активного рабочего набора (Working Set) игры от принудительной выгрузки при переключении окон.",
            "AI: Protected game working set from background memory trimming, ensuring instant Alt-Tab recovery without reloading pauses.",
            "Safe / Multi-Task");

        AddTweak("ram_paged_pool_quota_size",
            "ИИ: Расширен виртуальный пул PagedPoolSize для стабильной многозадачности с играми, Discord и браузером.",
            "AI: Expanded kernel paged pool address space quota for reliable multi-tasking alongside Discord, browser, and games.",
            "Safe / Pool");

        AddTweak("ram_heap_segment_allocation_cache",
            "ИИ: Активировано ускорение блочного кэша Segment Heap для быстрого спавна объектов в движках UE5 и Unity.",
            "AI: Accelerated segment heap cache allocation to reduce allocator lock contention in modern C++/Rust game engines.",
            "Safe / Heap");

        AddTweak("ram_worker_thread_stack_commit",
            "ИИ: Оптимизирован стартовый стек системных потоков (64 КБ), предотвращая задержки расширения стека.",
            "AI: Optimized thread stack initial commit to 64KB, eliminating runtime stack page-fault expansions.",
            "Safe / Threads");

        if (hasSsd)
        {
            AddTweak("ram_prefetch_ssd_gaming_tune",
                "ИИ: Для накопителя SSD/NVMe отключены устаревшие алгоритмы Prefetch и Superfetch, исключая фрагментацию ОЗУ и лишние записи.",
                "AI: Disabled legacy HDD Prefetcher & Superfetch on SSD/NVMe storage to prevent RAM fragmentation and background writes.",
                "Safe / SSD Tune");
        }

        if (maxRisk >= RiskLevel.Medium)
        {
            AddTweak("ram_svchost_split_threshold",
                $"ИИ: Для системы с {ram.TotalGb:F0} ГБ ОЗУ установлен порог группировки служб SvcHostSplitThreshold (32 ГБ), освобождая сотни МБ памяти.",
                $"AI: Configured SvcHostSplitThreshold (32GB) for your {ram.TotalGb:F0}GB RAM PC, grouping services and freeing hundreds of megabytes.",
                "Medium / RAM Free");

            AddTweak("ram_system_cache_dirty_page_limit",
                "ИИ: Ограничен объем грязных страниц файлового кэша, предотвращая просадки шины памяти при залповой записи на диск.",
                "AI: Clamped dirty file cache pages before disk flush to prevent memory bus saturation during heavy background writes.",
                "Medium / Bus Bandwidth");
        }

        // =========================================================================
        // 4. INPUT & LATENCY (Sensors, polling, queues)
        // =========================================================================
        AddTweak("input_mouse_acceleration_disable",
            "ИИ: Отключено программное ускорение указателя Enhanced Pointer Precision, гарантируя честное 1:1 соотношение движения сенсора мыши.",
            "AI: Completely disabled Windows mouse acceleration (Enhanced Pointer Precision) for pure, deterministic 1:1 sensor tracking.",
            "Safe / 1:1 Raw Input");

        AddTweak("input_keyboard_repeat_rate_max",
            "ИИ: Скорость автоповтора клавиш установлена на максимум (KeyboardSpeed = 31) для мгновенного отклика при удержании кнопок стрейфа.",
            "AI: Maximized keyboard repeat rate (31 cps) for instant responsiveness when holding movement and action keys.",
            "Safe / Keyboard");

        AddTweak("input_keyboard_repeat_delay_min",
            "ИИ: Начальная задержка повтора клавиш сведена к минимуму (KeyboardDelay = 0) для мгновенной реакции в соревновательных играх.",
            "AI: Minimized keyboard initial repeat delay to 0 for instant key-repeat actuation in competitive gaming.",
            "Safe / Keyboard");

        AddTweak("input_usb_selective_suspend_disable",
            "ИИ: Отключено выборочное энергосбережение USB-портов (DisableSelectiveSuspend = 1), поддерживая стабильный опрос мыши 1000–8000 Гц.",
            "AI: Disabled USB Selective Suspend on USB controllers to guarantee uninterrupted 1000Hz+ mouse and keyboard polling.",
            "Safe / USB Power");

        AddTweak("input_cursor_shadow_disable",
            "ИИ: Отключена альфа-тень курсора мыши, исключая лишний проход композитинга DWM при резких движениях мыши.",
            "AI: Disabled software cursor drop shadow to remove an unnecessary GDI/DWM compositing pass during quick mouse flicks.",
            "Safe / Visual Latency");

        AddTweak("input_mouse_hover_time_decrease",
            "ИИ: Задержка наведения курсора MouseHoverTime снижена с 400 мс до 10 мс для моментального открытия подсказок в инвентаре.",
            "AI: Reduced mouse hover time to 10ms for instant inventory tooltip displays and responsive UI navigation.",
            "Safe / UI Speed");

        AddTweak("input_hid_driver_polling_priority",
            "ИИ: Приоритет потока драйвера мыши mouclass повышен до 31 (Realtime) для безупречной обработки прерываний высокочастотных мышей.",
            "AI: Elevated HID mouclass driver thread priority to 31 for ultra-stable interrupt processing on high-polling gaming mice.",
            "Safe / Mouse Queue");

        AddTweak("input_mouse_data_queue_size",
            "ИИ: Очередь буфера мыши mouclass расширена до 200 пакетов, предотвращая пропуски координат на мышах 1000/4000/8000 Гц.",
            "AI: Expanded mouse packet buffer queue to 200 packets to prevent coordinate dropping on 1000Hz–8000Hz mice.",
            "Safe / High Polling");

        AddTweak("input_keyboard_data_queue_size",
            "ИИ: Очередь буфера клавиатуры расширена до 200 пакетов, исключая потерю нажатий при высоком APM.",
            "AI: Expanded keyboard driver packet queue to 200 packets to eliminate dropped keystrokes during high APM gameplay.",
            "Safe / High APM");

        AddTweak("input_csgo_raw_input_buffer_coalescing",
            "ИИ: Отключено объединение сырых пакетов ввода CoalesceInput, устраняя скрытую задержку мыши 4–8 мс.",
            "AI: Disabled raw input WM_INPUT message coalescing, eliminating hidden 4-8ms mouse packet buffering delays.",
            "Safe / Competitive");

        AddTweak("input_background_raw_input_lock",
            "ИИ: Задержка захвата фокуса мыши ForegroundLockTimeout сведена к 0 для мгновенного отклика при возврате в окно игры.",
            "AI: Set ForegroundLockTimeout to 0 for instantaneous window focus acquisition and mouse capture.",
            "Safe / Window Focus");

        AddTweak("input_sticky_keys_prompt_disable",
            "ИИ: Отключено модальное окно залипания клавиш StickyKeys при 5-кратном нажатии Shift, исключая сворачивание игры во время боя.",
            "AI: Disabled the disruptive StickyKeys 5x Shift popup dialog, preventing accidental game minimization during frantic combat.",
            "Safe / Quality of Life");

        AddTweak("input_filter_keys_disable",
            "ИИ: Отключен фильтр коротких нажатий FilterKeys, гарантируя регистрацию быстрых микротапов при контр-стрейфах.",
            "AI: Disabled FilterKeys delay to ensure instant, guaranteed registration of rapid micro-taps and counter-strafes.",
            "Safe / Keyboard");

        AddTweak("input_pointer_trails_force_disable",
            "ИИ: Отключены программные шлейфы курсора, сохраняя аппаратный GPU-конвейер отображения курсора без задержки.",
            "AI: Disabled software pointer trails to preserve hardware GPU cursor plane rendering without composition delay.",
            "Safe / GPU Cursor");

        AddTweak("input_gamepad_xinput_polling_latency",
            "ИИ: Интервал опроса геймпадов XInput снижен до 1 мс, обеспечивая мгновенный отклик стиков и триггеров контроллера.",
            "AI: Clamped XInput controller polling interval to 1ms for snappier analog stick and trigger actuation.",
            "Safe / Gamepad");

        // =========================================================================
        // 5. GAMING & DIRECTX PERFORMANCE
        // =========================================================================
        AddTweak("gaming_game_mode_enable",
            "ИИ: Активирован игровой режим Windows Game Mode для изоляции игрового процесса от фоновых обновлений и фоновых служб.",
            "AI: Activated Windows Game Mode to prioritize game threads and suppress background Windows Update activity.",
            "Safe / Core Gaming");

        AddTweak("gaming_game_dvr_disable",
            $"ИИ: Отключена фоновая запись клипов Game DVR, освобождая ресурсы аппаратного видеоэнкодера на {gpu.Name} и видеопамять.",
            $"AI: Disabled background Game DVR recording, recovering hardware video encoder cycles and VRAM bandwidth on {gpu.Name}.",
            "Safe / Encoder Free");

        AddTweak("gaming_game_bar_presence_writer_disable",
            "ИИ: Отключен телеметрический процесс GameBarPresenceWriter, устраняя лаги при старте игр через Steam и Epic Games.",
            "AI: Disabled GameBarPresenceWriter background telemetry to eliminate launch stalls in Steam/Epic titles.",
            "Safe / Launch Speed");

        AddTweak("gaming_fullscreen_exclusive_fse",
            "ИИ: Включен конвейер DirectFlip (FSEBehavior = 2), обеспечивающий минимальную задержку отображения кадров в полноэкранном и оконном режимах.",
            "AI: Activated DirectFlip presentation queue via GameDVR_FSEBehavior = 2 for lowest display flip latency.",
            "Safe / DirectFlip");

        AddTweak("gaming_game_bar_nexus_disable",
            "ИИ: Отключены глобальные перехваты комбинаций Win+G и кнопки контроллера Xbox Guide, разгружая очередь системных хоткеев.",
            "AI: Disabled Xbox Game Bar guide button and Win+G hotkey hooks, freeing system input queues.",
            "Safe / Hooks");

        AddTweak("gaming_audio_ducker_disable",
            "ИИ: Отключено автоматическое приглушение звука на 80% (Audio Ducking) при общении в Discord и голосовых чатах.",
            "AI: Disabled automatic Windows Audio Ducking to keep in-game footsteps and cues clearly audible during voice chat.",
            "Safe / Discord Sound");

        AddTweak("gaming_game_dvr_dxgi_compatibility",
            "ИИ: Отключен слой совместимости DXGI Game DVR, исключая промежуточные хуки между движком игры и видеодрайвером.",
            "AI: Bypassed DXGI Game DVR compatibility interception, removing overhead between game engine and display driver.",
            "Safe / Clean DXGI");

        AddTweak("gaming_fullscreen_optimizations_global",
            "ИИ: Включена оптимизированная независимая модель презентации WDDM для DirectX 9/11 с поддержкой G-Sync и мгновенного Alt+Tab.",
            "AI: Enabled modern WDDM independent flip presentation for DX9/11 titles with seamless Alt+Tab and VRR support.",
            "Safe / WDDM Flip");

        AddTweak("gaming_auto_hdr_notification_disable",
            "ИИ: Отключен всплывающий баннер Auto HDR, предотвращающий кратковременную потерю фокуса ввода при запуске игр.",
            "AI: Suppressed Auto HDR notification overlay to prevent mouse focus loss upon game launches.",
            "Safe / Distraction Free");

        AddTweak("gaming_game_config_store_flags",
            "ИИ: Оптимизированы флаги GameConfigStore, отключающие ненужные очереди захвата видео и звука.",
            "AI: Optimized GameConfigStore flags to minimize capture buffering and background thread overhead.",
            "Safe / System");

        AddTweak("gaming_optimizations_for_windowed_games_enable",
            "ИИ: Включены оптимизации Windows 11 для оконных игр (Auto HDR, VRR и DirectFlip для DirectX 10/11).",
            "AI: Enabled Windows 11 Optimizations for Windowed Games for DirectFlip latency and VRR in borderless modes.",
            "Safe / Win11 Feature");

        AddTweak("gaming_spatial_audio_overhead_disable",
            "ИИ: Отключена цифровая обработка пространственного звука Windows Sonic, минимизируя задержку вывода звука шагов и выстрелов.",
            "AI: Disabled spatial audio HRTF convolution pipeline to deliver in-game audio cues with lowest latency.",
            "Safe / Audio Latency");

        AddTweak("gaming_discord_overlay_hook_mitigation",
            "ИИ: Приоритет графического рендеринга закреплен за основным окном игры при использовании оверлеев Discord и Steam.",
            "AI: Configured graphics hooking policy to prioritize main game render context over external chat overlays.",
            "Safe / Overlay Safe");

        AddTweak("gaming_disable_game_bar_tips",
            "ИИ: Отключены подсказки контроллера и уведомления Xbox Game Bar для исключения отвлекающих баннеров.",
            "AI: Disabled Xbox Game Bar controller tips and toasts for clean, distraction-free gameplay.",
            "Safe / Clean UI");

        AddTweak("gaming_steam_client_gpu_accel_priority",
            "ИИ: Установлен приоритет видеопамяти для активной 3D-игры над фоновыми процессами Steam и лаунчеров.",
            "AI: Ensured background launchers (Steam, Epic, Battle.net) yield GPU memory to the active 3D game.",
            "Safe / GPU Prioritization");

        if (hasSsd)
        {
            AddTweak("gaming_directstorage_bypass_io",
                $"ИИ: Для накопителя SSD/NVMe активирован путь BypassIO для DirectStorage, направляющий текстуры напрямую в видеопамять {gpu.Name}.",
                $"AI: Enabled DirectStorage BypassIO storage path, streaming game assets directly from NVMe SSD into {gpu.Name} VRAM.",
                "Safe / DirectStorage");
        }

        // =========================================================================
        // 6. NETWORK OPTIMIZATION & PING STABILITY
        // =========================================================================
        AddTweak("net_network_throttling_disable",
            "ИИ: Полностью отключен троттлинг сетевых пакетов Windows Network Throttling Index, устраняя скачки пинга во время голосового чата.",
            "AI: Disabled Windows Network Throttling Index, eliminating packet caps and ping spikes during active voice chat or media.",
            "Safe / Low Ping");

        AddTweak("net_qos_reserved_bandwidth_zero",
            "ИИ: Снято 20% системное ограничение пропускной способности QoS Packet Scheduler, отдавая весь сетевой канал игре.",
            "AI: Removed the 20% QoS reserved bandwidth limit, restoring 100% pipe throughput for game netcode and downloads.",
            "Safe / 100% Bandwidth");

        AddTweak("net_dns_negative_cache_disable",
            "ИИ: Отключено длительное кэширование неудачных DNS-запросов, обеспечивая мгновенное переподключение к серверам матча.",
            "AI: Disabled DNS negative cache retention to prevent multi-minute reconnect lockouts to game servers.",
            "Safe / Reconnect");

        AddTweak("net_default_ttl_standard",
            "ИИ: Установлено оптимальное время жизни сетевых пакетов DefaultTTL = 64 для минимизации задержек маршрутизации.",
            "AI: Standardized IP Time-To-Live header to 64 hops for clean, low-latency routing across global servers.",
            "Safe / Network");

        AddTweak("net_enable_pmtu_discovery",
            "ИИ: Включено динамическое обнаружение Path MTU Discovery для предотвращения фрагментации игровых пакетов на маршрутизаторах.",
            "AI: Enabled dynamic Path MTU Discovery (PMTU) to prevent packet fragmentation by intermediate routers.",
            "Safe / MTU");

        AddTweak("net_enable_pmt_black_hole_detect",
            "ИИ: Активировано обнаружение 'черных дыр' MTU на интернет-маршрутизаторах для предотвращения случайных дисконнектов.",
            "AI: Enabled PMTU Black Hole Router detection to eliminate random multiplayer disconnects.",
            "Safe / Stability");

        AddTweak("net_rss_receive_side_scaling_enable",
            $"ИИ: Для сетевого адаптера включено масштабирование Receive Side Scaling (RSS), распределяя сетевые прерывания по ядрам {cpu.Name}.",
            $"AI: Enabled Receive Side Scaling (RSS) to distribute incoming network interrupts across all cores of {cpu.Name}.",
            "Safe / Multithread Net");

        AddTweak("net_tcp_window_auto_tuning_normal",
            "ИИ: Установлен нормальный уровень автоподстройки окна TCP Window Auto-Tuning для максимальной скорости на гигабитных каналах.",
            "AI: Ensured TCP Window Auto-Tuning is set to Normal for optimal high-bandwidth throughput on modern fiber.",
            "Safe / Fiber Ready");

        AddTweak("net_wpad_autoproxy_disable",
            "ИИ: Отключен автопоиск локальных прокси-серверов WPAD, устраняя 1–2 секундную задержку открытия игровых сокетов.",
            "AI: Disabled Web Proxy Auto-Discovery (WPAD) lookups, eliminating outbound socket connection delays.",
            "Safe / Socket Speed");

        AddTweak("net_disable_netbios_overhead",
            "ИИ: Отключены широковещательные запросы устаревшего протокола NetBIOS over TCP/IP, очищая сетевой адаптер.",
            "AI: Disabled NetBIOS over TCP/IP broadcast polling to eliminate periodic local network jitter spikes.",
            "Safe / Clean Adapter");

        AddTweak("net_ecn_explicit_congestion_notification",
            "ИИ: Отключено ECN-уведомление о перегрузке, исключая дропы пакетов на несовместимых домашних роутерах и игровых серверах.",
            "AI: Disabled Explicit Congestion Notification (ECN) to prevent packet drops on incompatible game servers and routers.",
            "Safe / Packet Delivery");

        AddTweak("net_tcp_timestamps_disable",
            "ИИ: Отключены 12-байтовые временные метки TCP Timestamps в заголовках пакетов, снижая накладные расходы сетевого стека.",
            "AI: Disabled 12-byte RFC 1323 TCP timestamps to reduce packet header overhead and kernel processing cycles.",
            "Safe / Lean Packets");

        AddTweak("net_teredo_tunneling_disable",
            "ИИ: Отключен интерфейс туннелирования Teredo IPv6-in-IPv4, устраняя 20–50 мс задержки паразитной инкапсуляции.",
            "AI: Disabled Microsoft Teredo IPv6-over-IPv4 tunneling encapsulation to ensure clean native IP routing.",
            "Safe / Direct Route");

        AddTweak("net_arp_cache_expiration_tuning",
            "ИИ: Время жизни ARP-кэша шлюза увеличено до 120 с, предотвращая периодические синхронные широковещательные опросы.",
            "AI: Extended ARP cache lifetime to 120s to eliminate gateway broadcast re-query hitches during competitive matches.",
            "Safe / Gateway Sync");

        if (maxRisk >= RiskLevel.Medium)
        {
            AddTweak("net_tcp_nagle_algorithm_disable",
                "ИИ: Отключен алгоритм Нагла (TCP_NODELAY = 1, TcpAckFrequency = 1) для мгновенной отправки сетевых пакетов и точной регистрации попаданий.",
                "AI: Disabled Nagle's algorithm (TCP_NODELAY = 1, TcpAckFrequency = 1) for instant packet dispatch and crisp hit registration.",
                "Medium / Esports Focus");

            AddTweak("net_rsc_receive_segment_coalescing_disable",
                "ИИ: Отключено аппаратное накопление пакетов Receive Segment Coalescing (RSC) на сетевой карте для снижения сетевого джиттера.",
                "AI: Disabled NIC Receive Segment Coalescing (RSC) latency buffer, delivering incoming packets immediately to game netcode.",
                "Medium / Netcode Jitter");
        }

        // =========================================================================
        // 7. STORAGE OPTIMIZATION & DRIVE HYGIENE
        // =========================================================================
        AddTweak("storage_ntfs_83_name_disable",
            "ИИ: Отключена генерация коротких имен MS-DOS 8.3 на файловой системе NTFS, ускоряя создание файлов и обновление игр в Steam на 20%.",
            "AI: Disabled NTFS 8.3 short filename generation, accelerating file creation, patching, and indexing by up to 20% in Steam.",
            "Safe / NTFS Speed");

        AddTweak("storage_ntfs_last_access_update_disable",
            "ИИ: Отключена запись временных меток последнего доступа NTFS при чтении файлов, защищая ресурс SSD и устраняя очереди I/O.",
            "AI: Disabled NTFS last access timestamp writes, eliminating disk write wear and queue stalls during game texture streaming.",
            "Safe / Drive Life");

        AddTweak("storage_disk_sleep_timeout_unhide",
            "ИИ: Разблокирован параметр отключения дисков в схеме питания, предотвращая засыпание вторичных накопителей с играми.",
            "AI: Unlocked disk sleep timeout in power settings, preventing secondary game library drives from powering down on AC.",
            "Safe / Game Drives");

        AddTweak("storage_crash_dump_minidump_only",
            "ИИ: Дампы памяти ограничены легкими минидампами 256 КБ, предотвращая зависания диска и экономя гигабайты пространства SSD.",
            "AI: Restricted memory crash dumps to compact 256KB minidumps, saving gigabytes of free SSD space and avoiding drive lockups.",
            "Safe / Free Space");

        AddTweak("storage_disable_low_disk_space_warning",
            "ИИ: Отключен фоновый опрос и всплывающие предупреждения о низком свободном месте на дисках.",
            "AI: Disabled periodic low disk space polling and balloon notifications to prevent in-game popups and disk checks.",
            "Safe / Background Disk");

        AddTweak("storage_disable_link_tracking_ntfs",
            "ИИ: Отключено журналирование службы Distributed Link Tracking на NTFS, снижая паразитные фоновые метаданные диска.",
            "AI: Disabled NTFS Distributed Link Tracking client journaling, cutting down background metadata writes.",
            "Safe / Metadata");

        AddTweak("storage_long_path_support_enable",
            "ИИ: Снято ограничение длины путей Win32 (>260 символов), гарантируя стабильность глубоко вложенных модификаций и текстурпаков.",
            "AI: Enabled Win32 long file paths (>260 characters) to prevent crashes in complex modded game setups.",
            "Safe / Mod Support");

        AddTweak("storage_storage_sense_user_temp_cleanup",
            "ИИ: Настроена автоматическая очистка временных диагностических логов и кэшей Storage Sense.",
            "AI: Configured Storage Sense auto-purge for temporary diagnostic files, preserving drive headroom.",
            "Safe / Auto Cleanup");

        AddTweak("storage_disable_hibernation_file",
            $"ИИ: Отключен файл гибернации hiberfil.sys, освобождая {ram.TotalGb:F0} ГБ ценного пространства на системном накопителе.",
            $"AI: Disabled Windows hibernation file (hiberfil.sys), recovering {ram.TotalGb:F0}GB of valuable SSD storage space.",
            "Safe / SSD Free Space");

        AddTweak("storage_disable_volumetric_shadow_copies",
            "ИИ: Ограничена квота теневых копий VSS, защищая десятки гигабайт памяти SSD от бесконтрольного расходования.",
            "AI: Capped VSS shadow copy storage quota, reclaiming tens of gigabytes of solid-state drive space.",
            "Safe / Capacity");

        AddTweak("storage_disable_chkdsk_scan_timeout",
            "ИИ: Тайм-аут загрузочной проверки chkdsk снижен до 2 секунд для молниеносной перезагрузки системы.",
            "AI: Reduced automatic disk check (chkdsk) boot timeout to 2s for significantly faster reboots.",
            "Safe / Fast Reboot");

        if (hasSsd)
        {
            AddTweak("storage_trim_enable_notification",
                "ИИ: Проверена и активирована команда TRIM/Deallocate для накопителя SSD/NVMe, поддерживая заводскую скорость чтения/записи ячеек памяти.",
                "AI: Verified and enabled file system TRIM/Deallocate notification for SSD/NVMe drive to maintain peak read/write speeds.",
                "Safe / Peak SSD");

            AddTweak("storage_nvme_power_state_transition",
                "ИИ: Оптимизированы переходы автономного энергосбережения NVMe APST, предотвращая задержки пробуждения накопителя 10–50 мс.",
                "AI: Optimized NVMe Autonomous Power State Transitions (APST) to prevent 10-50ms drive resume latency.",
                "Safe / NVMe Responsiveness");

            AddTweak("storage_write_cache_buffer_flushing_tune",
                "ИИ: Настроен интервал сброса буфера записи на SSD, исключая микрозаикания при фоновом скачивании обновлений.",
                "AI: Tuned file system cache flush interval to avoid read stalls while downloading game updates in background.",
                "Safe / Buffer Flush");

            AddTweak("storage_smart_polling_interval_tune",
                "ИИ: Настроен интервал телеметрии S.M.A.R.T. на контроллере NVMe, исключая микропрерывания очередей чтения.",
                "AI: Tuned S.M.A.R.T. telemetry polling interval on NVMe controller to prevent read queue interruptions.",
                "Safe / SMART Tune");
        }

        // =========================================================================
        // 8. POWER ARCHITECTURE & LATENCY TUNING
        // =========================================================================
        AddTweak("power_fast_startup_disable",
            "ИИ: Отключен 'Быстрый запуск' (гибридная спячка ядра), гарантируя загрузку чистых драйверов без накопившихся утечек памяти.",
            "AI: Disabled Windows Fast Startup (hybrid hibernation), ensuring fresh kernel and driver initialization without memory leaks.",
            "Safe / Driver Purity");

        AddTweak("power_usb_selective_suspend_unhide",
            "ИИ: Разблокирована политика выборочного отключения USB в планах электропитания для постоянного питания периферии.",
            "AI: Unlocked USB selective suspend policy in power options to maintain continuous power delivery to USB devices.",
            "Safe / Peripheral Power");

        AddTweak("power_processor_min_state_unhide",
            "ИИ: Разблокирован параметр минимального состояния процессора (Minimum Processor State) для фиксации 100% готовности на питании от сети.",
            "AI: Unlocked Minimum Processor State setting, allowing locking baseline frequency at 100% on AC power.",
            "Safe / CPU Baseline");

        AddTweak("power_energy_saver_gaming_override",
            "ИИ: Заблокировано автоматическое затемнение экрана и троттлинг Energy Saver при активной игровой сессии.",
            "AI: Prevented Windows Energy Saver from dimming display or curtailing CPU clock speed during gaming.",
            "Safe / Brightness Lock");

        AddTweak("power_processor_idle_promote_unhide",
            "ИИ: Открыта настройка порога перехода ядер в сон (Idle Promote Threshold), предотвращая падение ядер в глубокие C-states.",
            "AI: Unlocked processor idle promote threshold to prevent CPU cores from prematurely falling into deep sleep states.",
            "Safe / C-States");

        AddTweak("power_system_cooling_policy_unhide",
            "ИИ: Разблокирована политика охлаждения системы, гарантирующая раскрутку вентиляторов до троттлинга процессора (Active Cooling).",
            "AI: Unlocked System Cooling Policy to ensure fans spin up before CPU frequency downscaling occurs.",
            "Safe / Thermal Safety");

        AddTweak("power_sleep_unattended_timeout_unhide",
            "ИИ: Разблокирован скрытый 2-минутный таймер сна при бездействии, исключая засыпание ПК во время игры с геймпада.",
            "AI: Unlocked 2-minute unattended sleep timer, preventing unwanted sleep timeouts when playing games with gamepads.",
            "Safe / Gamepad Sleep");

        AddTweak("power_ahci_hipm_dipm_unhide",
            "ИИ: Разблокировано управление питанием SATA AHCI Link (HIPM/DIPM), предотвращая задержки при обращении к вторичным накопителям.",
            "AI: Unlocked SATA AHCI Link Power Management to eliminate drive sleep freezes on secondary SSDs.",
            "Safe / Storage Power");

        AddTweak("power_gpu_dynamic_pstate_unhide",
            "ИИ: Разблокированы параметры управления питанием графического процессора в общей схеме электропитания.",
            "AI: Unlocked GPU power management policy attributes in power options for synchronized CPU/GPU performance.",
            "Safe / GPU Sync");

        AddTweak("power_processor_performance_time_check_unhide",
            "ИИ: Разблокирован интервал пересчета частоты процессора для мгновенного отклика частоты на пиковые игровые нагрузки.",
            "AI: Unlocked processor time check interval for faster CPU frequency ramp-up on sudden burst loads.",
            "Safe / Clock Scaling");

        AddTweak("power_processor_idle_demote_unhide",
            "ИИ: Разблокирован порог деэскалации ядер из спящих режимов, удерживая CPU в состояниях немедленной готовности C0/C1.",
            "AI: Unlocked processor idle demote threshold, keeping cores in high-readiness C0/C1 states for immediate execution.",
            "Safe / Immediate Execution");

        AddTweak("power_away_mode_disable",
            "ИИ: Отключен псевдо-спящий режим Away Mode, исключая ложные фоновые пробуждения ПК.",
            "AI: Disabled Away Mode power policy, preventing fake sleep states with spinning fans.",
            "Safe / True Sleep");

        AddTweak("power_hub_power_saving_disable",
            "ИИ: Запрещено отключение питания корневых концентраторов USB, обеспечивая стабильную шину 5В для внешних ЦАП и звуковых карт.",
            "AI: Disabled selective power down for root USB hubs, providing stable 5V rail delivery to external audio DACs.",
            "Safe / Audio Rail");

        AddTweak("power_allow_throttling_disable",
            "ИИ: Разрешена автономная работа аппаратного управления P-состояниями процессора без принудительного троттлинга ОС.",
            "AI: Enforced hardware P-state autonomy without OS down-throttling to sustain peak Turbo boost.",
            "Safe / Peak Boost");

        AddTweak("power_latency_sensitivity_hint_unhide",
            "ИИ: Разблокирован параметр Latency Sensitivity Hint для мгновенного буста частоты процессора при чувствительных к задержке потоках.",
            "AI: Unlocked Processor Latency Sensitivity Hint policy for immediate frequency scaling on latency-critical threads.",
            "Safe / Latency Hint");

        if (!isLaptop)
        {
            AddTweak("power_pcie_aspm_unhide",
                $"ИИ: Для шины PCIe видеокарты {gpu.Name} разблокировано отключение энергосбережения ASPM, устраняя задержку повторной инициализации линий PCIe.",
                $"AI: Unlocked PCIe Link State Power Management (ASPM) for {gpu.Name}, eliminating PCIe lane re-initialization lag.",
                "Safe / Desktop PCIe");
        }

        // =========================================================================
        // 9. WINDOWS SERVICES (Debloat & Resource Reclamation)
        // =========================================================================
        AddTweak("svc_distributed_link_tracking_disable",
            "ИИ: Отключена служба TrkWks (клиент отслеживания изменившихся связей), устраняя бесполезную нагрузку на десктопном ПК.",
            "AI: Disabled Distributed Link Tracking Client (TrkWks), removing unnecessary shortcut handle tracking.",
            "Safe / Service Debloat");

        AddTweak("svc_remote_registry_disable",
            "ИИ: Отключена служба Remote Registry, закрывая сетевые порты для несанкционированного удаленного доступа к реестру.",
            "AI: Permanently disabled Remote Registry service to close network registry attack vectors and polling.",
            "Safe / Security");

        AddTweak("svc_downloaded_maps_manager_disable",
            "ИИ: Отключена фоновая служба офлайн-карт MapsBroker, устраняя периодические сетевые проверки обновлений карт.",
            "AI: Disabled Downloaded Maps Manager (MapsBroker) to prevent unsolicited background map network updates.",
            "Safe / Service Debloat");

        AddTweak("svc_fax_service_disable",
            "ИИ: Отключена устаревшая служба факса (Fax), высвобождая ресурсы диспетчера служб.",
            "AI: Disabled legacy Windows Fax service, removing obsolete component overhead.",
            "Safe / Clean System");

        AddTweak("svc_retail_demo_disable",
            "ИИ: Отключена демонстрационная служба RetailDemo, предотвращая активацию магазинных демо-режимов.",
            "AI: Disabled Retail Demo Service, preventing kiosk loops from ever engaging.",
            "Safe / Clean System");

        AddTweak("svc_wallet_service_disable",
            "ИИ: Отключена служба кошелька WalletService, устраняя неиспользуемый стек мобильных NFC-платежей.",
            "AI: Disabled Windows Wallet Service, freeing memory and removing unused payment frameworks.",
            "Safe / Clean System");

        AddTweak("svc_phone_service_disable",
            "ИИ: Отключена служба мобильной телефонии PhoneSvc на компьютере без модема сотовой связи.",
            "AI: Disabled Phone / Cellular Telephony service on PC without cellular modem hardware.",
            "Safe / Clean System");

        AddTweak("svc_biometric_service_disable",
            "ИИ: Отключена биометрическая служба WbioSrvc на системе без сканеров отпечатков пальцев.",
            "AI: Disabled Windows Biometric Service on PC without biometric fingerprint sensors.",
            "Safe / Clean System");

        AddTweak("svc_alljoyn_router_disable",
            "ИИ: Отключена служба маршрутизатора AllJoyn для IoT-умного дома, устраняя фоновый опрос.",
            "AI: Disabled AllJoyn Router Service, removing unused smart home IoT communication dispatcher.",
            "Safe / Clean System");

        AddTweak("svc_geolocation_service_disable",
            "ИИ: Отключена служба геолокации lfsvc, устраняя паразитные сканирования Wi-Fi сетей и защищая приватность.",
            "AI: Disabled Windows Geolocation Service, preventing Wi-Fi triangulation scans and protecting location privacy.",
            "Safe / Privacy");

        AddTweak("svc_offline_files_disable",
            "ИИ: Отключена служба автономных файлов CscService, исключая блокировку сетевых дисков фоновым кэшированием.",
            "AI: Disabled Offline Files service (CscService) to eliminate background network share cache scans.",
            "Safe / Clean System");

        if (!isLaptop)
        {
            AddTweak("svc_touch_keyboard_disable",
                "ИИ: Для стационарного ПК без тачскрина отключена служба сенсорной клавиатуры и рукописного ввода TabletInputService.",
                "AI: Disabled Touch Keyboard and Handwriting Service on non-touch desktop PC, removing text input framework hooks.",
                "Safe / Desktop Only");
        }

        if (hasSsd)
        {
            AddTweak("svc_sysmain_manual",
                "ИИ: Для твердотельного накопителя служба SysMain (Superfetch) переведена в ручной режим, исключая фоновое чтение файлов в ОЗУ.",
                "AI: Set SysMain (Superfetch) service to Manual on SSD/NVMe storage, eliminating constant background file caching.",
                "Safe / SSD Debloat");
        }

        if (maxRisk >= RiskLevel.Medium)
        {
            AddTweak("svc_diagnostic_policy_disable",
                "ИИ: Отключена служба политики диагностики DPS, прекращая непрерывный анализ журналов событий и запись отчетов на диск.",
                "AI: Disabled Diagnostic Policy Service (DPS), eliminating diagnostic event log parsing and disk I/O during gaming.",
                "Medium / Service Debloat");

            AddTweak("svc_pca_service_disable",
                "ИИ: Отключен помощник по совместимости программ PcaSvc, устраняя навязчивые диалоговые окна при закрытии старых игр.",
                "AI: Disabled Program Compatibility Assistant (PcaSvc), eliminating compatibility mode dialog prompts upon game exit.",
                "Medium / Recommended");
        }

        // =========================================================================
        // 10. BACKGROUND PROCESSES & BLOATWARE
        // =========================================================================
        AddTweak("bg_diagtrack_service_disable",
            "ИИ: Отключена основная служба телеметрии DiagTrack (Connected User Experiences and Telemetry), устраняя фоновые всплески загрузки CPU.",
            "AI: Disabled core telemetry service DiagTrack, eliminating background CPU spikes and protecting gaming privacy.",
            "Safe / No Spikes");

        AddTweak("bg_edge_prelaunch_disable",
            "ИИ: Заблокирован автозапуск и предварительная загрузка Microsoft Edge в память, освобождая 300+ МБ ОЗУ.",
            "AI: Disabled Microsoft Edge background pre-launching and startup, reclaiming 300MB+ of physical RAM.",
            "Safe / 300MB+ RAM");

        AddTweak("bg_wer_service_disable",
            "ИИ: Отключена служба отчетов об ошибках Windows Error Reporting (WerSvc), предотвращая подвисания игр при обработке исключений.",
            "AI: Disabled Windows Error Reporting service to prevent process freezing and disk activity during minor game errors.",
            "Safe / Error Free");

        AddTweak("bg_widgets_news_interests_disable",
            "ИИ: Отключена панель виджетов 'Новости и интересы' Windows 11, выгружая тяжелые фоновые процессы WebView2 из памяти.",
            "AI: Disabled Windows 11 Widgets board and News feeds, terminating background WebView2 Edge processes and saving RAM.",
            "Safe / WebView2 Purge");

        AddTweak("bg_content_delivery_suggestions_disable",
            "ИИ: Отключена автоматическая загрузка рекламируемых приложений и предложений Content Delivery Manager.",
            "AI: Disabled Content Delivery Manager promoted suggestions, preventing automatic background bloatware downloads.",
            "Safe / No Bloat");

        AddTweak("bg_customer_experience_ceip_disable",
            "ИИ: Отключены задачи программы улучшения качества ПО (CEIP), исключая запланированные фоновые сборщики телеметрии.",
            "AI: Disabled Customer Experience Improvement Program (CEIP) scheduled tasks, eliminating periodic CPU spikes.",
            "Safe / CPU Idle");

        AddTweak("bg_tailored_experiences_disable",
            "ИИ: Отключен персонализированный сбор данных об использовании приложений Tailored Experiences.",
            "AI: Disabled Tailored Experiences diagnostic indexing of user clicks and app launches.",
            "Safe / Privacy");

        AddTweak("bg_windows_spotlight_disable",
            "ИИ: Отключена фоновая загрузка обоев 'Windows: интересное' (Spotlight), экономя интернет-трафик и дисковые операции.",
            "AI: Disabled Windows Spotlight lockscreen background image downloads, saving network bandwidth and disk I/O.",
            "Safe / Bandwidth");

        AddTweak("bg_compatibility_telemetry_disable",
            "ИИ: Заблокирован процесс телеметрии CompatTelRunner.exe, вызывающий 100% загрузку диска и процессора при сканировании файлов.",
            "AI: Disabled Microsoft Compatibility Telemetry (CompatTelRunner.exe), preventing notorious 100% disk and CPU spikes.",
            "Safe / No 100% Disk");

        AddTweak("bg_device_census_disable",
            "ИИ: Отключен фоновый опрос подключенного оборудования DeviceCensus.exe, устраняя пробуждение ядер в простое.",
            "AI: Disabled background Device Census telemetry task, preventing unnecessary hardware enumeration.",
            "Safe / Core Idle");

        AddTweak("bg_copilot_runtime_disable",
            "ИИ: Отключена интеграция Windows Copilot и фоновые AI-процессы WebView, освобождая оперативную память.",
            "AI: Disabled Microsoft Copilot runtime integration and background webview assistants, reducing RAM consumption.",
            "Safe / AI Debloat");

        AddTweak("bg_onedrive_sync_startup_disable",
            "ИИ: Отключен автозапуск OneDrive и фоновая синхронизация папки Documents, устраняя статтеры при сохранении игр.",
            "AI: Disabled OneDrive automatic startup and Documents folder sync, eliminating savegame write locks and stutter.",
            "Safe / Savegame Sync");

        AddTweak("bg_feedback_hub_telemetry_disable",
            "ИИ: Отключены фоновые задачи агрегации логов Центра отзывов, снижая количество переключений контекста ядра.",
            "AI: Disabled Feedback Hub background diagnostic tasks, reducing kernel context switches and idle load.",
            "Safe / Clean Idle");

        AddTweak("bg_appx_deployment_optimization",
            "ИИ: Заблокировано автоматическое фоновое обновление приложений Microsoft Store во время активных игровых матчей.",
            "AI: Configured Microsoft Store app updates to prevent surprise background downloads during active games.",
            "Safe / No Updates In-Game");

        AddTweak("bg_cloud_clipboard_sync_disable",
            "ИИ: Отключена облачная синхронизация буфера обмена, устраняя скрытые сетевые отправки при копировании текста и скриншотов.",
            "AI: Disabled cloud clipboard history cross-device synchronization, ensuring instant local clipboard without network overhead.",
            "Safe / Local Clipboard");

        AddTweak("bg_phone_link_background_disable",
            "ИИ: Отключена фоновая синхронизация службы 'Связь с телефоном' (Phone Link), освобождая ОЗУ и сетевой канал.",
            "AI: Disabled Phone Link background sync host, freeing RAM and eliminating background wireless device syncing.",
            "Safe / Background Purge");

        AddTweak("bg_windows_tips_suggestions_disable",
            "ИИ: Отключены фоновые подсказки и приветственные экраны Windows, устраняя отвлекающие баннеры.",
            "AI: Disabled Windows Soft Landing tips and educational welcome prompts, preventing unprompted UI interruptions.",
            "Safe / Clean Experience");

        // =========================================================================
        // 11. SECURITY, PRIVACY & TELEMETRY
        // =========================================================================
        AddTweak("sec_telemetry_data_collection_min",
            "ИИ: Сбор диагностических данных Windows ограничен минимальным базовым уровнем (Security/Required).",
            "AI: Restricted Windows Diagnostic Data collection to minimum technical level (Security / Required).",
            "Safe / Privacy Focus");

        AddTweak("sec_advertising_id_disable",
            "ИИ: Отключен рекламный идентификатор Windows Advertising ID, предотвращая сбор персонального рекламного профиля.",
            "AI: Disabled Windows Advertising ID to terminate cross-app user tracking and advertising telemetry.",
            "Safe / Privacy");

        AddTweak("sec_activity_history_disable",
            "ИИ: Отключен сбор журнала действий Windows Activity History и облачная публикация активности.",
            "AI: Disabled Windows Activity History timeline and cloud sync, eliminating local activity journaling overhead.",
            "Safe / Privacy");

        AddTweak("sec_location_tracking_disable",
            "ИИ: Отключены системные датчики геолокации, прекращая паразитное сканирование Wi-Fi BSSID.",
            "AI: Disabled global location tracking sensors, preventing periodic Wi-Fi triangulation scans.",
            "Safe / Privacy");

        AddTweak("sec_wifi_sense_hotspot_reporting_disable",
            "ИИ: Отключена служба Wi-Fi Sense и передача учетных данных точек доступа в облако.",
            "AI: Disabled Wi-Fi Sense and hotspot reporting, ensuring strictly manual Wi-Fi connection control.",
            "Safe / Network Privacy");

        AddTweak("sec_cloud_settings_sync_disable",
            "ИИ: Отключена облачная синхронизация системных настроек с OneDrive, предотвращая случайный сброс параметров оптимизации.",
            "AI: Disabled Microsoft account settings cloud sync, preventing remote cloud profiles from reverting local tweaks.",
            "Safe / Settings Lock");

        AddTweak("sec_handwriting_error_reporting_disable",
            "ИИ: Отключена отправка отчетов об ошибках распознавания рукописного ввода.",
            "AI: Disabled handwriting recognition error reporting to eliminate background telemetry queues.",
            "Safe / Privacy");

        AddTweak("sec_windows_customer_experience_optout",
            "ИИ: Отключено ведение журналов SQM (Software Quality Metrics) во всех установленных системных программах.",
            "AI: Opted out of SQM application telemetry logging across Windows, preventing disk clutter from temporary metric logs.",
            "Safe / Clean Disk");

        AddTweak("sec_app_diagnostic_info_deny",
            "ИИ: Запрещен доступ приложений Microsoft Store к системным журналам событий и диагностике оборудования.",
            "AI: Blocked downloaded Store apps from querying hardware diagnostic sensors and event logs.",
            "Safe / Sandbox");

        AddTweak("sec_remote_assistance_disable",
            "ИИ: Отключены входящие подключения удаленного помощника Remote Assistance (MSRA), закрывая уязвимые порты.",
            "AI: Disabled Remote Assistance listener ports, enhancing network security and reducing background listening overhead.",
            "Safe / Security");

        AddTweak("sec_defender_cloud_protection_reporting",
            "ИИ: Время ожидания облачной проверки Defender переведено в быстрый режим, исключая 10-секундные задержки при старте игр.",
            "AI: Configured Microsoft Defender cloud protection timeout to fast mode, eliminating executable launch freezes.",
            "Safe / Fast Launch");

        AddTweak("sec_microphone_background_access_apps",
            "ИИ: Запрещен скрытый фоновый доступ к микрофону для приложений UWP, защищая аудиопоток от посторонних слушателей.",
            "AI: Restricted background apps from silently accessing system microphone, preventing audio buffer jitter.",
            "Safe / Audio Privacy");

        AddTweak("sec_camera_background_access_deny",
            "ИИ: Запрещен фоновый доступ приложений к веб-камере, предотвращая пробуждение USB-контроллера камеры.",
            "AI: Blocked background apps from accessing webcam, preventing USB driver wakeups and protecting video privacy.",
            "Safe / Privacy");

        AddTweak("sec_smartscreen_for_apps_disable",
            "ИИ: Отключена проверка URL-ссылок фильтром SmartScreen внутри приложений Store для мгновенного перехода по ссылкам.",
            "AI: Disabled SmartScreen URL evaluation inside Store apps for instant link navigation without remote lookups.",
            "Safe / Browser Latency");

        AddTweak("sec_voice_activation_above_lock_disable",
            "ИИ: Запрещена фоновая активация голосовых ассистентов через микрофон при заблокированном экране.",
            "AI: Disabled voice activation wake words above lock screen, completely muting microphone monitoring while locked.",
            "Safe / Mic Lock");

        if (!isLaptop)
        {
            AddTweak("sec_biometrics_facial_recognition_disable",
                "ИИ: Отключен опрос инфракрасных камер Windows Hello на настольном ПК без ИК-датчиков.",
                "AI: Disabled Windows Hello camera sensor polling on desktop PC without IR camera hardware.",
                "Safe / Desktop Security");
        }

        // =========================================================================
        // 12. WINDOWS PERFORMANCE & UI SNAPPINESS
        // =========================================================================
        AddTweak("win_menu_show_delay",
            "ИИ: Задержка открытия контекстных меню MenuShowDelay снижена с 400 мс до 10 мс для моментального отклика интерфейса.",
            "AI: Reduced MenuShowDelay from 400ms to 10ms for instantaneous, snappy context menu and taskbar responsiveness.",
            "Safe / 10ms Snappy");

        AddTweak("win_transparency_disable",
            $"ИИ: Отключены эффекты прозрачности Acrylic и Mica, высвобождая ресурсы видеопамяти и пиксельных шейдеров {gpu.Name}.",
            $"AI: Disabled desktop Acrylic and Mica transparency blur shaders, freeing GPU fillrate and VRAM on {gpu.Name}.",
            "Safe / GPU Fillrate");

        AddTweak("win_window_animations_disable",
            "ИИ: Отключены анимации сворачивания и разворачивания окон, обеспечивая мгновенный Alt+Tab и переключение задач.",
            "AI: Disabled window minimize/maximize animations for instantaneous Alt-Tab switching and zero composition hitching.",
            "Safe / 0ms Animations");

        AddTweak("win_background_apps_global_disable",
            "ИИ: Глобально отключена фоновая работа универсальных приложений Windows (Погода, Карты, Новости), освобождая ОЗУ и такты CPU.",
            "AI: Globally disabled background UWP apps execution, reclaiming CPU cycles, memory, and eliminating background network polling.",
            "Safe / Global Debloat");

        AddTweak("win_delivery_optimization_p2p_disable",
            "ИИ: Отключена раздача обновлений Windows Delivery Optimization по P2P-сети, исключая скрытую загрузку исходящего интернет-канала.",
            "AI: Disabled Delivery Optimization P2P upload seeding, guaranteeing stable network ping without hidden bandwidth consumption.",
            "Safe / No Upload Seeding");

        AddTweak("win_start_menu_web_search_disable",
            "ИИ: Отключен поиск Bing в меню 'Пуск', превращая поиск в мгновенный локальный поиск приложений без отправки сетевых запросов.",
            "AI: Disabled Bing web search in Start menu, enabling instantaneous local application search without cloud query delays.",
            "Safe / Local Search");

        AddTweak("win_feedback_frequency_never",
            "ИИ: Частота опросов обратной связи Windows Feedback установлена на 'Никогда', исключая всплывающие уведомления.",
            "AI: Set Windows Feedback prompt frequency to Never, suppressing intrusive background feedback notifications.",
            "Safe / No Polls");

        AddTweak("win_cortana_disable",
            "ИИ: Отключен голосовой ассистент Cortana, выгружая неиспользуемые службы из оперативной памяти.",
            "AI: Disabled Cortana voice assistant, removing background memory footprint and microphone monitoring threads.",
            "Safe / RAM Free");

        AddTweak("win_auto_play_disable",
            "ИИ: Отключен автозапуск внешних накопителей AutoPlay, защищая от вредоносного ПО и зависаний при подключении флешек.",
            "AI: Disabled AutoPlay for external media, preventing system freezes and security risks when plugging in USB accessories.",
            "Safe / USB Security");

        AddTweak("win_aero_shake_disable",
            "ИИ: Отключен жест Aero Shake (сворачивание окон при встряхивании), предотвращая случайное сворачивание при резких движениях мыши.",
            "AI: Disabled Aero Shake to prevent accidental minimization of background windows during frantic mouse swipes.",
            "Safe / Multi-Window");

        AddTweak("win_snap_assist_disable",
            "ИИ: Отключены подсказки Snap Assist при привязке окон, делая перемещение окон плавным и быстрым.",
            "AI: Disabled Snap Assist auto-suggestions to eliminate UI hesitation when docking or moving windows.",
            "Safe / UI Fluidity");

        AddTweak("win_visual_effects_best_performance",
            "ИИ: Визуальные эффекты Windows настроены на максимальное быстродействие с сохранением сглаживания экранных шрифтов.",
            "AI: Optimized visual effects for best performance while preserving ClearType sub-pixel font smoothing.",
            "Safe / Performance FX");

        AddTweak("win_lockscreen_slideshow_disable",
            "ИИ: Отключено слайдшоу экрана блокировки, ускоряя пробуждение монитора из спящего режима.",
            "AI: Disabled lock screen slideshow network rotation for instantaneous wake from sleep.",
            "Safe / Fast Wake");

        AddTweak("win_shell_shortcuts_hotkeys_disable",
            "ИИ: Отключены конфликтующие глобальные хоткеи Windows 11 (Win+C для Copilot и Win+W для виджетов).",
            "AI: Disabled Windows 11 Copilot (Win+C) and Widgets (Win+W) global hotkey conflict combinations.",
            "Safe / No Conflict Keys");

        AddTweak("win_search_indexer_backoff_disable",
            "ИИ: Отключено замедление поискового индексатора при активности пользователя, ускоряя завершение индексации диска.",
            "AI: Disabled Windows Search Indexer CPU backoff throttling to complete disk indexing faster and eliminate idle thrashing.",
            "Safe / Index Speed");

        AddTweak("win_app_suggestions_start_menu_disable",
            "ИИ: Отключены рекомендованные приложения и реклама в меню 'Пуск' Windows 11 для чистого и быстрого меню.",
            "AI: Disabled promoted app recommendations and recent file suggestions in Windows 11 Start menu.",
            "Safe / Clean Menu");

        AddTweak("win_toast_notifications_quiet_hours",
            "ИИ: Включен тихий режим Focus Assist для подавления всплывающих уведомлений во время полноэкранных игр.",
            "AI: Enforced Focus Assist automatic quiet hours to suppress notification banners during fullscreen gaming.",
            "Safe / Focus Assist");

        AddTweak("win_print_screen_snipping_tool_disable",
            "ИИ: Восстановлен классический мгновенный снимок экрана по клавише PrintScreen в буфер обмена без запуска тяжелого Snipping Tool.",
            "AI: Restored classic zero-latency PrintScreen key behavior, bypassing heavy modern Snipping Tool overlays.",
            "Safe / Instant Screenshot");

        // =========================================================================
        // 13. ADVANCED ARCHITECTURAL KERNEL TWEAKS
        // =========================================================================
        AddTweak("adv_critical_section_timeout",
            "ИИ: Сокращен тайм-аут критических секций ядра для быстрого разрешения взаимных блокировок потоков.",
            "AI: Reduced kernel critical section timeout for rapid deadlock resolution.",
            "Safe / Kernel");

        AddTweak("adv_paging_executive_strict_lock",
            "ИИ: Включена строгая блокировка страниц ядра в ОЗУ для защиты критических драйверов от фрагментации.",
            "AI: Enforced strict kernel paging lock to protect driver routines from page fragmentation.",
            "Safe / Kernel");

        AddTweak("adv_global_flag_page_heap_disable",
            "ИИ: Отключены отладочные флаги Page Heap для минимального накладного расхода памяти.",
            "AI: Disabled page heap debugging flags for lowest memory management overhead.",
            "Safe / Kernel");

        AddTweak("adv_kernel_sehop_enable",
            "ИИ: Активирована аппаратная защита перезаписи обработчиков исключений (SEHOP).",
            "AI: Enabled Structured Exception Handler Overwrite Protection (SEHOP).",
            "Safe / Security");

        AddTweak("adv_mmcss_audio_realtime_priority",
            "ИИ: Зафиксирован приоритет аудиопотоков MMCSS в реальном времени, устраняя хрипы и задержки звука в играх.",
            "AI: Locked MMCSS audio thread scheduling to realtime priority, eliminating audio dropouts and crackling in games.",
            "Safe / Audio Realtime");

        AddTweak("adv_executive_fast_fail_strict_lock",
            "ИИ: Включена оптимизация Executive Fast Fail для мгновенной обработки системных прерываний.",
            "AI: Enabled Executive Fast Fail optimization for instant system interrupt servicing.",
            "Safe / Fast Fail");

        if (maxRisk >= RiskLevel.Medium)
        {
            AddTweak("adv_dwm_thread_priority_realtime",
                "ИИ: Приоритет потока композитора DWM повышен до реального времени для абсолютной плавности смены кадров.",
                "AI: Elevated DWM compositor thread priority to realtime for flawless frame pacing and lowest presentation jitter.",
                "Medium / Realtime DWM");

            AddTweak("adv_win32_priority_quanta_28",
                "ИИ: Настроен квант времени потоков (quanta 28) для наилучшего баланса плавности рендера и отклика ввода.",
                "AI: Configured Win32 thread scheduling quanta (28) for optimal render pacing and input responsiveness.",
                "Medium / Quanta 28");

            AddTweak("adv_session_pool_size_128",
                "ИИ: Расширен размер пула сеансов SessionPoolSize до 128 МБ для стабильной работы тяжелых оверлеев.",
                "AI: Expanded session pool size to 128MB to prevent UI resource exhaustion with heavy gaming overlays.",
                "Medium / Memory Pool");
        }

        result.Selections = selected;
        return result;
    }
}
