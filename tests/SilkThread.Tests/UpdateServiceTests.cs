using System;
using System.Text.Json;
using SilkThread.App.Services;
using SilkThread.App.Views;
using Xunit;

namespace SilkThread.Tests;

public class UpdateServiceTests
{
    [Fact]
    public void UpdateTransaction_RollsBackReplacementsAndNewFilesAfterLockedFileFailure()
    {
        string sandbox = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SilkThread-test-" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(sandbox);
        try
        {
            string root = System.IO.Path.Combine(sandbox, "app");
            System.IO.Directory.CreateDirectory(root);
            string replaced = System.IO.Path.Combine(root, "b-existing.dll");
            string locked = System.IO.Path.Combine(root, "z-locked.dll");
            System.IO.File.WriteAllText(replaced, "original");
            System.IO.File.WriteAllText(locked, "locked-original");
            string patch = System.IO.Path.Combine(sandbox, "patch.zip");
            using (var archive = System.IO.Compression.ZipFile.Open(patch, System.IO.Compression.ZipArchiveMode.Create))
                foreach (string name in new[] { "a-new.dll", "b-existing.dll", "z-locked.dll" })
                {
                    using var writer = new System.IO.StreamWriter(archive.CreateEntry(name).Open());
                    writer.Write("updated");
                }
            using (var held = new System.IO.FileStream(locked, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
                Assert.Equal(1, RunTransaction(patch, root));
            Assert.Equal("original", System.IO.File.ReadAllText(replaced));
            Assert.Equal("locked-original", System.IO.File.ReadAllText(locked));
            Assert.False(System.IO.File.Exists(System.IO.Path.Combine(root, "a-new.dll")));
            string recovery = Assert.Single(System.IO.Directory.GetDirectories(sandbox, "SilkThread-recovery-*"));
            Assert.Equal("original", System.IO.File.ReadAllText(System.IO.Path.Combine(recovery, "backup", "b-existing.dll")));
            Assert.True(System.IO.File.Exists(System.IO.Path.Combine(recovery, "journal.json")));
            Assert.True(System.IO.File.Exists(patch));
        }
        finally { System.IO.Directory.Delete(sandbox, true); }
    }

    [Fact]
    public void UpdateTransaction_CommitsNestedFilesAndRemovesRecoveryAfterSuccess()
    {
        string sandbox = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SilkThread-test-" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(sandbox);
        try
        {
            string root = System.IO.Path.Combine(sandbox, "O'Brien % app");
            System.IO.Directory.CreateDirectory(root);
            string patch = System.IO.Path.Combine(sandbox, "patch.zip");
            using (var archive = System.IO.Compression.ZipFile.Open(patch, System.IO.Compression.ZipArchiveMode.Create))
            {
                using var writer = new System.IO.StreamWriter(archive.CreateEntry("Assets/test.txt").Open());
                writer.Write("updated");
            }
            Assert.Equal(0, RunTransaction(patch, root));
            Assert.Equal("updated", System.IO.File.ReadAllText(System.IO.Path.Combine(root, "Assets", "test.txt")));
            Assert.Empty(System.IO.Directory.GetDirectories(sandbox, "SilkThread-recovery-*"));
        }
        finally { System.IO.Directory.Delete(sandbox, true); }
    }

    private static int RunTransaction(string patch, string root)
    {
        string script = UpdateService.GeneratePatchTransactionScript(patch, root);
        string encoded = Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(script));
        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "powershell.exe", Arguments = "-NoProfile -NonInteractive -EncodedCommand " + encoded,
            UseShellExecute = false, CreateNoWindow = true
        })!;
        Assert.True(process.WaitForExit(30000));
        return process.ExitCode;
    }

    [Theory]
    [InlineData("../escape.dll")]
    [InlineData("..\\escape.dll")]
    [InlineData("C:/escape.dll")]
    [InlineData("/escape.dll")]
    [InlineData("app.dll:stream")]
    public void ValidatePatchArchive_RejectsUnsafeDestinations(string name)
    {
        using var buffer = new System.IO.MemoryStream();
        using var zip = new System.IO.Compression.ZipArchive(buffer, System.IO.Compression.ZipArchiveMode.Update);
        zip.CreateEntry(name);
        Assert.Throws<System.IO.InvalidDataException>(() =>
            UpdateService.ValidatePatchArchive(zip, @"C:\SilkThread"));
    }

    [Fact]
    public void ValidatePatchArchive_RejectsLinksAndDirectoryOnlyArchives()
    {
        using var buffer = new System.IO.MemoryStream();
        using var zip = new System.IO.Compression.ZipArchive(buffer, System.IO.Compression.ZipArchiveMode.Update);
        zip.CreateEntry("Assets/");
        Assert.Throws<System.IO.InvalidDataException>(() => UpdateService.ValidatePatchArchive(zip, @"C:\SilkThread"));
        zip.CreateEntry("link.dll").ExternalAttributes = unchecked((int)0xA1FF0000);
        Assert.Throws<System.IO.InvalidDataException>(() => UpdateService.ValidatePatchArchive(zip, @"C:\SilkThread"));
    }

    [Fact]
    public void ValidatePatchArchive_AcceptsNestedFiles()
    {
        using var buffer = new System.IO.MemoryStream();
        using var zip = new System.IO.Compression.ZipArchive(buffer, System.IO.Compression.ZipArchiveMode.Update);
        zip.CreateEntry("Assets/icon.png");
        UpdateService.ValidatePatchArchive(zip, @"C:\SilkThread");
    }

    [Fact]
    public void UpdateScript_AbortsBeforeRestartAndCleanupOnExtractionFailure()
    {
        string script = UpdateService.GenerateInPlaceUpdateScript(@"C:\patch.zip", @"C:\SilkThread", @"C:\SilkThread\app.exe");
        int guard = script.IndexOf("if errorlevel 1 exit /b 1", StringComparison.Ordinal);
        Assert.True(guard >= 0);
        Assert.True(guard < script.IndexOf("start \"\"", StringComparison.Ordinal));
        Assert.True(guard < script.IndexOf("del /f", StringComparison.Ordinal));
    }

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
        Assert.Contains("SILK_UPDATE_PAYLOAD", script);
        Assert.Contains("Expand-Archive", UpdateService.GeneratePatchTransactionScript(patchZip, appDir));
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

        Assert.Contains("$root = 'C:\\Program Files\\SilkThread'", UpdateService.GeneratePatchTransactionScript(patchZip, appDirWithBackslash));
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

    [Fact]
    public void ManifestDeserialization_WithDownloadSize_ParsesCorrectly()
    {
        string manifestJson = """
        {
          "version": "1.1.2",
          "title": "SilkThread v1.1.2",
          "description": "Release notes",
          "releaseDate": "2026-11-09",
          "patchUrl": "https://example.com/patch.zip",
          "patchSize": 3309712,
          "downloadUrl": "https://example.com/setup.exe",
          "downloadSize": 65432100
        }
        """;

        var updateInfo = JsonSerializer.Deserialize<UpdateInfo>(manifestJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(updateInfo);
        Assert.Equal("1.1.2", updateInfo.Version);
        Assert.Equal(3309712, updateInfo.PatchSize);
        Assert.Equal(65432100, updateInfo.DownloadSize);
    }

    [Theory]
    [InlineData(3309712, true, "3.2 МБ")]
    [InlineData(3309712, false, "3.2 MB")]
    [InlineData(1048576, true, "1 МБ")]
    [InlineData(1048576, false, "1 MB")]
    [InlineData(512000, true, "500 КБ")]
    [InlineData(512000, false, "500 KB")]
    public void FormatFileSize_VariousByteSizes_FormatsCorrectly(long bytes, bool isRu, string expected)
    {
        string result = UpdateDialog.FormatFileSize(bytes, isRu);
        Assert.Equal(expected, result);
    }
}
