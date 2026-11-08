using System.Collections.Generic;
using System.Threading.Tasks;
using SilkThread.Core.Models;

namespace SilkThread.Core.PresetManager;

public class PresetTweakEvaluation
{
    public string TweakId { get; set; } = string.Empty;
    public TweakDefinition? Tweak { get; set; }
    public CompatibilityResult Compatibility { get; set; } = CompatibilityResult.Compatible;
    public bool IsCompatible => Compatibility.IsCompatible;
    public string IncompatibilityReason => Compatibility.Reason;
    public bool IsSelected { get; set; }
    public string? AiRationale { get; set; }
}

public class EvaluatedPreset
{
    public TweakPreset Preset { get; set; } = null!;
    public List<PresetTweakEvaluation> Tweaks { get; set; } = new();
    public int TotalCount => Tweaks.Count;
    public int IncompatibleCount => Tweaks.Count(t => !t.IsCompatible);
    public int CompatibleCount => Tweaks.Count(t => t.IsCompatible);
    public bool HasIncompatible => IncompatibleCount > 0;
}

public interface IPresetManager
{
    Task ExportPresetAsync(TweakPreset preset, string filePath);
    Task<TweakPreset> ImportPresetAsync(string filePath);
    Task<EvaluatedPreset> EvaluatePresetAsync(TweakPreset preset, HardwareProfile? profile = null);
    Task<EvaluatedPreset> LoadAndEvaluatePresetAsync(string filePath);
    Task<TweakPreset> CreatePresetFromTweakIdsAsync(string name, string description, IEnumerable<string> tweakIds, string? author = null);
}
