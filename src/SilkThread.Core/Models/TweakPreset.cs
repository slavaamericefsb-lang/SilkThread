using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SilkThread.Core.Models;

public class TweakPreset
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; set; } = Environment.UserName;

    [JsonPropertyName("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("tweakIds")]
    public List<string> TweakIds { get; set; } = new();

    [JsonPropertyName("generationSource")]
    public string? GenerationSource { get; set; }

    [JsonPropertyName("usedOnlineModel")]
    public bool UsedOnlineModel { get; set; }

    [JsonPropertyName("isOnlineAiGenerated")]
    public bool IsOnlineAiGenerated
    {
        get => UsedOnlineModel;
        set => UsedOnlineModel = value;
    }

    [JsonPropertyName("aiModelUsed")]
    public string? AiModelUsed
    {
        get => GenerationSource;
        set => GenerationSource = value;
    }

    [JsonIgnore]
    public int TotalCount => TweakIds?.Count ?? 0;

    [JsonIgnore]
    public string FormattedDate => CreatedAtUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
}
