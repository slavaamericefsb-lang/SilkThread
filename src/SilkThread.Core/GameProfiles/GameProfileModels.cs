using System;
using System.Collections.Generic;

namespace SilkThread.Core.GameProfiles;

public enum GameGenreCategory
{
    All,
    Shooter,
    Moba,
    BattleRoyale
}

public class LaunchArgItem
{
    public string Argument { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;

    public LaunchArgItem() { }

    public LaunchArgItem(string argument, string purpose, string impact)
    {
        Argument = argument;
        Purpose = purpose;
        Impact = impact;
    }
}

public class VideoSettingItem
{
    public string Name { get; set; } = string.Empty;
    public string RecommendedValue { get; set; } = string.Empty;
    public string EsportsImpact { get; set; } = string.Empty;
    public string Category { get; set; } = "Video";

    public VideoSettingItem() { }

    public VideoSettingItem(string name, string recommendedValue, string esportsImpact, string category = "Video")
    {
        Name = name;
        RecommendedValue = recommendedValue;
        EsportsImpact = esportsImpact;
        Category = category;
    }
}

public class ConfigFileInfo
{
    public string FileName { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public ConfigFileInfo() { }

    public ConfigFileInfo(string fileName, string path, string description)
    {
        FileName = fileName;
        Path = path;
        Description = description;
    }
}

public class GameProfile
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ShortTitle { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public GameGenreCategory GenreCategory { get; set; } = GameGenreCategory.Shooter;
    public string EngineBadge { get; set; } = string.Empty;
    public string Launcher { get; set; } = string.Empty;
    public string AccentColorHex { get; set; } = "#806BFF";
    public string IconGlyph { get; set; } = "\uE7FC";
    public string Description { get; set; } = string.Empty;

    public IReadOnlyList<string> ExecutableNames { get; set; } = Array.Empty<string>();
    public string LaunchOptions { get; set; } = string.Empty;
    public IReadOnlyList<LaunchArgItem> LaunchArgs { get; set; } = Array.Empty<LaunchArgItem>();
    public IReadOnlyList<VideoSettingItem> VideoSettings { get; set; } = Array.Empty<VideoSettingItem>();
    public IReadOnlyList<ConfigFileInfo> ConfigFiles { get; set; } = Array.Empty<ConfigFileInfo>();

    public bool DirectFlipSupported { get; set; } = true;
    public bool HighPrioritySupported { get; set; } = true;
    public bool HighGpuPreferenceSupported { get; set; } = true;
}
