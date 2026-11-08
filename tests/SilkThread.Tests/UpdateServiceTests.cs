using System;
using System.Text.Json;
using SilkThread.App.Services;
using Xunit;

namespace SilkThread.Tests;

public class UpdateServiceTests
{
    [Theory]
    [InlineData("1.1.0", "1.0.0", true)]
    [InlineData("1.0.0", "1.1.0", false)]
    [InlineData("1.1.0", "1.1.0", false)]
    [InlineData("v1.1.0", "1.0.0", true)]
    [InlineData("V1.1.0", "1.1.0", false)]
    [InlineData("1.1.1", "1.1.0", true)]
    [InlineData("2.0.0", "1.9.9", true)]
    [InlineData("2.0", "1.9.9", true)]
    [InlineData("1.0.0", "2.0", false)]
    [InlineData("1.2.0.1", "1.2.0.0", true)]
    [InlineData("1.2.0.0", "1.2.0.1", false)]
    public void IsNewerVersion_ValidVersionStrings_ReturnsExpectedResult(string remote, string current, bool expected)
    {
        bool result = UpdateService.IsNewerVersion(remote, current);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsNewerVersion_EmptyOrNullRemote_ReturnsFalse()
    {
        Assert.False(UpdateService.IsNewerVersion("", "1.0.0"));
        Assert.False(UpdateService.IsNewerVersion("   ", "1.0.0"));
        Assert.False(UpdateService.IsNewerVersion(null!, "1.0.0"));
    }

    [Fact]
    public void IsNewerVersion_EmptyOrNullCurrent_ReturnsTrue()
    {
        Assert.True(UpdateService.IsNewerVersion("1.1.0", ""));
        Assert.True(UpdateService.IsNewerVersion("1.1.0", "  "));
        Assert.True(UpdateService.IsNewerVersion("1.1.0", null!));
    }

    [Fact]
    public void ManifestDeserialization_ValidManifestJson_ParsesCorrectly()
    {
        string manifestJson = """
        {
          "version": "1.1.0",
          "title": "SilkThread v1.1.0 — Minimal Dark UI & IOST",
          "description": "Масштабный релиз v1.1.0:\n• Полный перевод на дизайн-систему Minimal Dark UI и Bento Grid",
          "releaseDate": "2026-11-08",
          "downloadUrl": "https://github.com/slavaamericefsb-lang/SilkThread/releases/latest/download/SilkThread_Setup.exe"
        }
        """;

        var updateInfo = JsonSerializer.Deserialize<UpdateInfo>(manifestJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(updateInfo);
        Assert.Equal("1.1.0", updateInfo.Version);
        Assert.Equal("SilkThread v1.1.0 — Minimal Dark UI & IOST", updateInfo.Title);
        Assert.Contains("Minimal Dark UI", updateInfo.Description);
        Assert.Equal("2026-11-08", updateInfo.ReleaseDate);
        Assert.Equal("https://github.com/slavaamericefsb-lang/SilkThread/releases/latest/download/SilkThread_Setup.exe", updateInfo.DownloadUrl);
    }

    [Fact]
    public void ManifestDeserialization_PascalCaseJson_ParsesCorrectly()
    {
        string manifestJson = """
        {
          "Version": "1.1.0",
          "Title": "SilkThread Update",
          "Description": "Test release",
          "ReleaseDate": "2026-11-08",
          "DownloadUrl": "https://example.com/setup.exe"
        }
        """;

        var updateInfo = JsonSerializer.Deserialize<UpdateInfo>(manifestJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(updateInfo);
        Assert.Equal("1.1.0", updateInfo.Version);
        Assert.Equal("SilkThread Update", updateInfo.Title);
    }

    [Fact]
    public void ManifestDeserialization_InvalidJson_ThrowsJsonException()
    {
        string invalidJson = "{ invalid json content }";

        Assert.ThrowsAny<JsonException>(() =>
        {
            JsonSerializer.Deserialize<UpdateInfo>(invalidJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        });
    }

    [Fact]
    public void ManifestDeserialization_EmptyObject_ProducesEmptyVersion()
    {
        string emptyJson = "{}";

        var updateInfo = JsonSerializer.Deserialize<UpdateInfo>(emptyJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(updateInfo);
        Assert.True(string.IsNullOrWhiteSpace(updateInfo.Version));
    }

    [Fact]
    public void UpdateDeliveryEngine_VersionVerification_Detects1_1_0_Over_1_0_0()
    {
        // Core requirement check: IsNewerVersion("1.1.0", "1.0.0") == true
        bool isNewer = UpdateService.IsNewerVersion("1.1.0", "1.0.0");
        Assert.True(isNewer, "UpdateService must identify 1.1.0 as newer than 1.0.0");
    }

    [Fact]
    public void ManifestDeserialization_WithPatchUrlAndSize_ParsesCorrectly()
    {
        string manifestJson = """
        {
          "version": "1.1.2",
          "title": "SilkThread v1.1.2 — In-place update",
          "description": "Seamless patch update without full reinstall",
          "releaseDate": "2026-11-09",
          "patchUrl": "https://github.com/slavaamericefsb-lang/SilkThread/releases/latest/download/SilkThread_Patch.zip",
          "patchSize": 3296744,
          "downloadUrl": "https://github.com/slavaamericefsb-lang/SilkThread/releases/latest/download/SilkThread_Setup.exe"
        }
        """;

        var updateInfo = JsonSerializer.Deserialize<UpdateInfo>(manifestJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(updateInfo);
        Assert.Equal("1.1.2", updateInfo.Version);
        Assert.Equal("https://github.com/slavaamericefsb-lang/SilkThread/releases/latest/download/SilkThread_Patch.zip", updateInfo.PatchUrl);
        Assert.Equal(3296744, updateInfo.PatchSize);
        Assert.Equal("https://github.com/slavaamericefsb-lang/SilkThread/releases/latest/download/SilkThread_Setup.exe", updateInfo.DownloadUrl);
    }

    [Fact]
    public void ManifestDeserialization_WithoutPatchUrl_HasNullPatchProperties()
    {
        string manifestJson = """
        {
          "version": "1.1.0",
          "downloadUrl": "https://github.com/slavaamericefsb-lang/SilkThread/releases/latest/download/SilkThread_Setup.exe"
        }
        """;

        var updateInfo = JsonSerializer.Deserialize<UpdateInfo>(manifestJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(updateInfo);
        Assert.Null(updateInfo.PatchUrl);
        Assert.Null(updateInfo.PatchSize);
    }

    [Fact]
    public void IsPatchUpdateAvailable_ValidPatchUrl_ReturnsTrue()
    {
        var update = new UpdateInfo
        {
            Version = "1.1.2",
            PatchUrl = "https://example.com/SilkThread_Patch.zip"
        };

        Assert.True(UpdateService.IsPatchUpdateAvailable(update));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsPatchUpdateAvailable_NullOrEmptyPatchUrl_ReturnsFalse(string? patchUrl)
    {
        var update = new UpdateInfo
        {
            Version = "1.1.2",
            PatchUrl = patchUrl
        };

        Assert.False(UpdateService.IsPatchUpdateAvailable(update));
        Assert.False(UpdateService.IsPatchUpdateAvailable(null));
    }

    [Fact]
    public void GenerateInPlaceUpdateScript_ValidPaths_ContainsExpectedCommands()
    {
        string patchZip = @"C:\Users\User\AppData\Local\Temp\SilkThread_Patch.zip";
        string appDir = @"C:\Program Files\SilkThread";
        string appExe = @"C:\Program Files\SilkThread\SilkThread.App.exe";

        string script = UpdateService.GenerateInPlaceUpdateScript(patchZip, appDir, appExe);

        Assert.Contains("@echo off", script);
        Assert.Contains("timeout /t 1 /nobreak", script);
        Assert.Contains("tar -xf \"" + patchZip + "\" -C \"" + appDir + "\"", script);
        Assert.Contains("Expand-Archive", script);
        Assert.Contains("start \"\" \"" + appExe + "\"", script);
        Assert.Contains("del /f /q \"" + patchZip + "\"", script);
        Assert.Contains("(goto) 2>nul & del \"%~f0\"", script);
    }

    [Fact]
    public void GenerateInPlaceUpdateScript_TrimsTrailingSlashesFromAppDir()
    {
        string patchZip = @"C:\Temp\SilkThread_Patch.zip";
        string appDirWithBackslash = @"C:\Program Files\SilkThread\";
        string appExe = @"C:\Program Files\SilkThread\SilkThread.App.exe";

        string script = UpdateService.GenerateInPlaceUpdateScript(patchZip, appDirWithBackslash, appExe);

        // Crucial verification: Must NOT have trailing backslash right before closing quote `\"`
        // which causes Windows C runtime / tar argument parsing failure
        Assert.Contains("-C \"C:\\Program Files\\SilkThread\"", script);
        Assert.DoesNotContain("-C \"C:\\Program Files\\SilkThread\\\"", script);
    }

    [Fact]
    public void GenerateInPlaceUpdateScript_NullOrEmptyArgs_ThrowsArgumentException()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            UpdateService.GenerateInPlaceUpdateScript("", @"C:\App", @"C:\App\App.exe"));

        Assert.ThrowsAny<ArgumentException>(() =>
            UpdateService.GenerateInPlaceUpdateScript(@"C:\patch.zip", "", @"C:\App\App.exe"));

        Assert.ThrowsAny<ArgumentException>(() =>
            UpdateService.GenerateInPlaceUpdateScript(@"C:\patch.zip", @"C:\App", ""));
    }
}
