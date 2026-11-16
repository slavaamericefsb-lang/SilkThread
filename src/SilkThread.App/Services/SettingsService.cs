using System;
using System.IO;
using System.Text.Json;
using Microsoft.UI.Xaml;

namespace SilkThread.App.Services;

public class AppSettingsModel
{
    public ElementTheme AppTheme { get; set; } = ElementTheme.Dark;
    public string Language { get; set; } = "ru";
    public bool ConfirmDangerousTweaks { get; set; } = true;
    public bool AutoCreateRestorePoint { get; set; } = true;
    public bool HideIncompatibleTweaks { get; set; } = true;
    public bool ShowRebootNotifications { get; set; } = true;
    public string OpenRouterApiKey { get; set; } = string.Empty;
    public string OpenRouterModel { get; set; } = "liquid/lfm-3-1.7b:free";
    public int LastOptimizationProfileIndex { get; set; } = 0;
}

public interface ISettingsService
{
    AppSettingsModel Settings { get; }
    int LastOptimizationProfileIndex { get; set; }
    void Save();
    void SaveSettings() => Save();
    event EventHandler? SettingsChanged;
}

public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;
    private readonly object _fileLock = new();
    public AppSettingsModel Settings { get; private set; }

    public int LastOptimizationProfileIndex
    {
        get => Settings.LastOptimizationProfileIndex;
        set => Settings.LastOptimizationProfileIndex = value;
    }

    public event EventHandler? SettingsChanged;

    public void SaveSettings() => Save();

    public SettingsService() : this(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SilkThread", "settings.json")) { }

    public SettingsService(string settingsFilePath)
    {
        _settingsFilePath = Path.GetFullPath(settingsFilePath);
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath)!);
        Settings = LoadSettings();
    }

    private AppSettingsModel LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var loaded = JsonSerializer.Deserialize<AppSettingsModel>(json);
                if (loaded != null)
                {
                    loaded.AppTheme = ElementTheme.Dark;
                    return loaded;
                }
            }
        }
        catch { }

        return new AppSettingsModel();
    }

    public void Save()
    {
        try
        {
            lock (_fileLock)
            {
                var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
                string temporaryPath = _settingsFilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    File.WriteAllText(temporaryPath, json);
                    File.Move(temporaryPath, _settingsFilePath, overwrite: true);
                }
                finally
                {
                    if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
                }
            }
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex) { App.Log($"[Settings Save failed]: {ex.Message}"); }
    }
}
