using SilkThread.App.Services;
using Xunit;

namespace SilkThread.Tests;

public class SettingsPersistenceTests
{
    [Fact]
    public void Save_ReloadsPreferencesAndLeavesNoTemporaryFiles()
    {
        string directory = Path.Combine(Path.GetTempPath(), "SilkThread-settings-" + Guid.NewGuid());
        try
        {
            string path = Path.Combine(directory, "settings.json");
            var service = new SettingsService(path);
            service.Settings.Language = "en";
            service.Settings.AutoCreateRestorePoint = false;
            service.Save();
            service.Settings.Language = "ru";
            service.Save();
            var reloaded = new SettingsService(path);
            Assert.Equal("ru", reloaded.Settings.Language);
            Assert.False(reloaded.Settings.AutoCreateRestorePoint);
            Assert.Single(Directory.GetFiles(directory));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
