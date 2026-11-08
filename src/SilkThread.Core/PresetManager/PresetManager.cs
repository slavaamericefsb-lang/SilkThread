using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SilkThread.Core.CompatibilityEngine;
using SilkThread.Core.HardwareDetection;
using SilkThread.Core.Models;
using SilkThread.Core.TweakEngine;

namespace SilkThread.Core.PresetManager;

public class PresetManager : IPresetManager
{
    private readonly ITweakEngine _tweakEngine;
    private readonly ICompatibilityEngine _compatibilityEngine;
    private readonly IHardwareScanner _hardwareScanner;
    private readonly ILogger<PresetManager>? _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public PresetManager(
        ITweakEngine tweakEngine,
        ICompatibilityEngine compatibilityEngine,
        IHardwareScanner hardwareScanner,
        ILogger<PresetManager>? logger = null)
    {
        _tweakEngine = tweakEngine;
        _compatibilityEngine = compatibilityEngine;
        _hardwareScanner = hardwareScanner;
        _logger = logger;
    }

    public async Task ExportPresetAsync(TweakPreset preset, string filePath)
    {
        if (preset == null) throw new ArgumentNullException(nameof(preset));
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Path cannot be empty", nameof(filePath));

        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(preset, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
        _logger?.LogInformation("Preset '{Name}' ({Count} tweaks) exported to '{Path}'", preset.Name, preset.TweakIds.Count, filePath);
    }

    public async Task<TweakPreset> ImportPresetAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Preset file not found: {filePath}", filePath);
        }

        var json = await File.ReadAllTextAsync(filePath);
        var preset = JsonSerializer.Deserialize<TweakPreset>(json, JsonOptions);
        if (preset == null)
        {
            throw new InvalidOperationException("Failed to parse preset file content as TweakPreset.");
        }

        preset.TweakIds ??= new List<string>();
        _logger?.LogInformation("Preset '{Name}' imported with {Count} tweaks from '{Path}'", preset.Name, preset.TweakIds.Count, filePath);
        return preset;
    }

    public async Task<EvaluatedPreset> EvaluatePresetAsync(TweakPreset preset, HardwareProfile? profile = null)
    {
        if (preset == null) throw new ArgumentNullException(nameof(preset));

        profile ??= await _hardwareScanner.ScanAsync();

        var evaluations = new List<PresetTweakEvaluation>();

        foreach (var tweakId in preset.TweakIds)
        {
            var def = _tweakEngine.GetTweakById(tweakId);
            if (def != null)
            {
                var compat = _compatibilityEngine.EvaluateCompatibility(def, profile);
                evaluations.Add(new PresetTweakEvaluation
                {
                    TweakId = def.Id,
                    Tweak = def,
                    Compatibility = compat,
                    // Incompatible: disabled by default; Compatible: enabled by default
                    IsSelected = compat.IsCompatible
                });
            }
            else
            {
                evaluations.Add(new PresetTweakEvaluation
                {
                    TweakId = tweakId,
                    Tweak = null,
                    Compatibility = CompatibilityResult.Incompatible($"Твик '{tweakId}' не найден в каталоге текущей версии SilkThread."),
                    IsSelected = false
                });
            }
        }

        // Split & Sort: Incompatible tweaks are sorted TO THE VERY BEGINNING OF THE LIST!
        var sorted = evaluations
            .OrderBy(t => t.IsCompatible ? 1 : 0)
            .ThenBy(t => t.Tweak?.Title ?? t.TweakId)
            .ToList();

        return new EvaluatedPreset
        {
            Preset = preset,
            Tweaks = sorted
        };
    }

    public async Task<EvaluatedPreset> LoadAndEvaluatePresetAsync(string filePath)
    {
        var preset = await ImportPresetAsync(filePath);
        return await EvaluatePresetAsync(preset);
    }

    public Task<TweakPreset> CreatePresetFromTweakIdsAsync(string name, string description, IEnumerable<string> tweakIds, string? author = null)
    {
        var preset = new TweakPreset
        {
            Version = "1.0",
            Name = string.IsNullOrWhiteSpace(name) ? "Пользовательский пресет" : name.Trim(),
            Description = description?.Trim() ?? string.Empty,
            Author = string.IsNullOrWhiteSpace(author) ? Environment.UserName : author.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            TweakIds = tweakIds.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
        };

        return Task.FromResult(preset);
    }
}
