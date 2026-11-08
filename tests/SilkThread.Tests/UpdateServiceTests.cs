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
}
