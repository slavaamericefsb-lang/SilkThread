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
}

public interface ISettingsService
{
    AppSettingsModel Settings { get; }
    void Save();
    void SaveSettings() => Save();
    event EventHandler? SettingsChanged;
}

public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;
    public AppSettingsModel Settings { get; private set; }

    public event EventHandler? SettingsChanged;

    public void SaveSettings() => Save();

    public SettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "SilkThread");
        Directory.CreateDirectory(dir);
        _settingsFilePath = Path.Combine(dir, "settings.json");

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
            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFilePath, json);
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
        catch { }
    }
}
