using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SilkThread.App.Services;

/// <summary>
/// Remote update management service for SilkThread.
/// Checks remote manifest (version.json), downloads the installer to %TEMP%,
/// spawns the installer with administrative elevation and gracefully shuts down SilkThread.
/// </summary>
public sealed class UpdateService : IUpdateService
{
    private static readonly HttpClient s_httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    public string CurrentVersion { get; }

    public string? LastRemoteVersion { get; private set; }

    public string UpdateEndpointUrl { get; set; } = 
        "https://raw.githubusercontent.com/slavaamericefsb-lang/SilkThread/main/version.json";

    public event EventHandler<UpdateInfo>? UpdateAvailable;

    public UpdateService()
    {
        var ver = Assembly.GetExecutingAssembly().GetName().Version;
        if (ver != null)
        {
            CurrentVersion = ver.Build >= 0 
                ? (ver.Revision > 0 ? $"{ver.Major}.{ver.Minor}.{ver.Build}.{ver.Revision}" : $"{ver.Major}.{ver.Minor}.{ver.Build}")
                : $"{ver.Major}.{ver.Minor}.0";
        }
        else
        {
            CurrentVersion = "1.1.2";
        }
    }

    /// <summary>
    /// Checks for updates against the remote JSON endpoint.
    /// </summary>
    public async Task<UpdateInfo?> CheckForUpdatesAsync(bool isManualCheck = false, CancellationToken ct = default)
    {
        try
        {
            var uriBuilder = new UriBuilder(UpdateEndpointUrl);
            string cacheBustParam = $"nocache={DateTime.UtcNow.Ticks}";
            if (string.IsNullOrEmpty(uriBuilder.Query) || uriBuilder.Query == "?")
            {
                uriBuilder.Query = cacheBustParam;
            }
            else
            {
                uriBuilder.Query = uriBuilder.Query.TrimStart('?') + "&" + cacheBustParam;
            }

            string requestUrl = uriBuilder.Uri.ToString();
            App.Log($"[UpdateService] Checking for updates at: {requestUrl}");

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("SilkThread-App", CurrentVersion));
            request.Headers.CacheControl = new CacheControlHeaderValue 
            { 
                NoCache = true,
                NoStore = true,
                MustRevalidate = true
            };
            request.Headers.Pragma.ParseAdd("no-cache");

            using var response = await s_httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                App.Log($"[UpdateService] Remote server returned HTTP {response.StatusCode}");
                if (isManualCheck)
                {
                    throw new HttpRequestException($"Remote server returned HTTP {response.StatusCode} ({(int)response.StatusCode})");
                }
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var updateInfo = JsonSerializer.Deserialize<UpdateInfo>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (updateInfo == null || string.IsNullOrWhiteSpace(updateInfo.Version))
            {
                App.Log("[UpdateService] Empty or invalid JSON received from remote update manifest.");
                if (isManualCheck)
                {
                    throw new InvalidOperationException("Empty or invalid update manifest received from remote server.");
                }
                return null;
            }

            LastRemoteVersion = updateInfo.Version.Trim();
            App.Log($"[UpdateService] Текущая версия: {CurrentVersion} | Версия на сервере: {LastRemoteVersion}");

            if (IsNewerVersion(LastRemoteVersion, CurrentVersion))
            {
                App.Log($"[UpdateService] Newer version available: {LastRemoteVersion} ({updateInfo.Title})");
                UpdateAvailable?.Invoke(this, updateInfo);
                return updateInfo;
            }

            App.Log($"[UpdateService] Application is currently up-to-date (Current: {CurrentVersion}, Remote: {LastRemoteVersion}).");
            return null;
        }
        catch (OperationCanceledException)
        {
            App.Log("[UpdateService] Update check canceled.");
            return null;
        }
        catch (Exception ex)
        {
            App.Log($"[UpdateService] Update check exception: {ex.Message}");
            if (isManualCheck)
            {
                throw;
            }
            return null;
        }
    }

    /// <summary>
    /// Checks whether the update info points to a lightweight in-place patch.
    /// </summary>
    public static bool IsPatchUpdateAvailable(UpdateInfo? update) =>
        update != null && !string.IsNullOrWhiteSpace(update.PatchUrl);

    /// <summary>
    /// Generates the standalone Windows batch script used to extract the patch archive directly over
    /// the application directory and cleanly relaunch the updated executable without an installer wizard.
    /// </summary>
    public static string GenerateInPlaceUpdateScript(string patchZipPath, string appDir, string appExePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(patchZipPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(appDir);
        ArgumentException.ThrowIfNullOrWhiteSpace(appExePath);

        string cleanAppDir = appDir.TrimEnd('\\', '/');

        var sb = new StringBuilder();
        sb.AppendLine("@echo off");
        sb.AppendLine("chcp 65001 >nul");
        sb.AppendLine();
        sb.AppendLine(":: 1. Wait for caller process to exit and release file handles");
        sb.AppendLine("timeout /t 1 /nobreak >nul 2>&1 || ping 127.0.0.1 -n 2 >nul");
        sb.AppendLine();
        sb.AppendLine(":: 2. Extract patch archive directly over application directory");
        sb.AppendLine("where tar >nul 2>&1");
        sb.AppendLine("if %errorlevel% equ 0 (");
        sb.AppendLine($"    tar -xf \"{patchZipPath}\" -C \"{cleanAppDir}\"");
        sb.AppendLine(") else (");
        sb.AppendLine($"    powershell -NoProfile -NonInteractive -Command \"Expand-Archive -Path '{patchZipPath}' -DestinationPath '{cleanAppDir}' -Force\"");
        sb.AppendLine(")");
        sb.AppendLine();
        sb.AppendLine(":: Ensure files replaced; retry once if still locked");
        sb.AppendLine("if not %errorlevel% equ 0 (");
        sb.AppendLine("    timeout /t 1 /nobreak >nul 2>&1");
        sb.AppendLine("    where tar >nul 2>&1");
        sb.AppendLine("    if %errorlevel% equ 0 (");
        sb.AppendLine($"        tar -xf \"{patchZipPath}\" -C \"{cleanAppDir}\"");
        sb.AppendLine("    )");
        sb.AppendLine(")");
        sb.AppendLine();
        sb.AppendLine(":: 3. Restart updated application");
        sb.AppendLine($"start \"\" \"{appExePath}\"");
        sb.AppendLine();
        sb.AppendLine(":: 4. Cleanup patch archive and self-delete script");
        sb.AppendLine($"del /f /q \"{patchZipPath}\" >nul 2>&1");
        sb.AppendLine("(goto) 2>nul & del \"%~f0\"");

        return sb.ToString();
    }

    /// <summary>
    /// Downloads the update payload (lightweight patch or full installer) and installs it.
    /// If PatchUrl is present, downloads ~3 MB patch zip and performs seamless in-place extraction over AppContext.BaseDirectory.
    /// If PatchUrl is absent, falls back to DownloadUrl full installer.
    /// </summary>
    public async Task DownloadAndInstallUpdateAsync(
        UpdateInfo update, 
        IProgress<UpdateDownloadProgress>? progress = null, 
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(update);

        bool isPatch = IsPatchUpdateAvailable(update);
        string downloadUrl = isPatch ? update.PatchUrl! : update.DownloadUrl;

        if (string.IsNullOrWhiteSpace(downloadUrl))
        {
            throw new InvalidOperationException("Download URL is not specified in update manifest.");
        }

        string tempDestination = isPatch
            ? Path.Combine(Path.GetTempPath(), "SilkThread_Patch.zip")
            : Path.Combine(Path.GetTempPath(), "SilkThread_Setup.exe");

        App.Log($"[UpdateService] Starting download (PatchMode={isPatch}) from {downloadUrl} to {tempDestination}");

        // Remove any previous temporary file
        if (File.Exists(tempDestination))
        {
            try { File.Delete(tempDestination); } catch { }
        }

        using (var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl))
        {
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("SilkThread-App", CurrentVersion));

            using var response = await s_httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            long? totalBytes = response.Content.Headers.ContentLength;
            if (!totalBytes.HasValue && isPatch && update.PatchSize.HasValue && update.PatchSize.Value > 0)
            {
                totalBytes = update.PatchSize.Value;
            }

            await using var contentStream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            await using var fileStream = new FileStream(tempDestination, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

            var buffer = new byte[81920];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct).ConfigureAwait(false);
                totalRead += bytesRead;

                double percentage = totalBytes.HasValue && totalBytes.Value > 0
                    ? Math.Min(100.0, (double)totalRead / totalBytes.Value * 100.0)
                    : 0.0;

                progress?.Report(new UpdateDownloadProgress(percentage, totalRead, totalBytes));
            }

            await fileStream.FlushAsync(ct).ConfigureAwait(false);
        }

        long downloadedLength = new FileInfo(tempDestination).Length;
        App.Log($"[UpdateService] Download finished successfully. File size: {downloadedLength} bytes.");

        if (isPatch)
        {
            // Verify patch archive integrity before applying
            try
            {
                using var zipCheck = ZipFile.OpenRead(tempDestination);
                if (zipCheck.Entries.Count == 0)
                {
                    throw new InvalidDataException("Downloaded patch archive contains no files.");
                }
            }
            catch (Exception ex)
            {
                App.Log($"[UpdateService] Invalid patch zip downloaded: {ex.Message}");
                throw new InvalidOperationException($"Downloaded patch archive is corrupt or invalid: {ex.Message}", ex);
            }

            // Prepare in-place update script
            string appDir = AppContext.BaseDirectory.TrimEnd('\\', '/');
            string appExe = Path.Combine(appDir, "SilkThread.App.exe");
            if (!File.Exists(appExe) && !string.IsNullOrEmpty(Environment.ProcessPath))
            {
                appExe = Environment.ProcessPath;
            }

            string scriptPath = Path.Combine(Path.GetTempPath(), "SilkThread_Update_InPlace.cmd");
            string scriptContent = GenerateInPlaceUpdateScript(tempDestination, appDir, appExe);
            await File.WriteAllTextAsync(scriptPath, scriptContent, new UTF8Encoding(false), ct).ConfigureAwait(false);

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = scriptPath,
                    UseShellExecute = true,
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                App.Log("[UpdateService] Launching lightweight in-place updater script with elevation...");
                Process.Start(psi);

                await Task.Delay(500, ct).ConfigureAwait(false);
                App.Log("[UpdateService] Exiting SilkThread to release files for seamless patch.");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                App.Log($"[UpdateService] Failed to start in-place updater script: {ex.Message}");
                throw;
            }
        }
        else
        {
            // Fallback: Spawn full installer process and exit app
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = tempDestination,
                    UseShellExecute = true,
                    Verb = "runas"
                };

                App.Log("[UpdateService] Launching fallback installer process...");
                Process.Start(psi);

                await Task.Delay(500, ct).ConfigureAwait(false);
                App.Log("[UpdateService] Exiting SilkThread to release file handles for installer.");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                App.Log($"[UpdateService] Failed to start installer process: {ex.Message}");
                throw;
            }
        }
    }

    /// <summary>
    /// Compares two version strings (e.g. "1.2.0" vs "1.0.0" or "v2.0" vs "1.5.2").
    /// Returns true if remoteVersion is strictly greater than currentVersion.
    /// </summary>
    public static bool IsNewerVersion(string remoteVersion, string currentVersion)
    {
        if (string.IsNullOrWhiteSpace(remoteVersion)) return false;
        if (string.IsNullOrWhiteSpace(currentVersion)) return true;

        var cleanRemote = CleanVersionString(remoteVersion);
        var cleanCurrent = CleanVersionString(currentVersion);

        if (Version.TryParse(cleanRemote, out var vRemote) && Version.TryParse(cleanCurrent, out var vCurrent))
        {
            return vRemote > vCurrent;
        }

        // Semantic segmented fallback: 1.2.3.4
        var rParts = cleanRemote.Split('.');
        var cParts = cleanCurrent.Split('.');
        int maxLen = Math.Max(rParts.Length, cParts.Length);

        for (int i = 0; i < maxLen; i++)
        {
            int rVal = i < rParts.Length && int.TryParse(rParts[i], out var rv) ? rv : 0;
            int cVal = i < cParts.Length && int.TryParse(cParts[i], out var cv) ? cv : 0;

            if (rVal > cVal) return true;
            if (rVal < cVal) return false;
        }

        return false;
    }

    private static string CleanVersionString(string ver)
    {
        ver = ver.Trim();
        if (ver.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            ver = ver[1..].Trim();
        }

        // Normalize "2.0" to "2.0.0" if needed for Version.TryParse
        int dotCount = 0;
        foreach (char c in ver)
        {
            if (c == '.') dotCount++;
        }

        if (dotCount == 1)
        {
            ver += ".0";
        }

        return ver;
    }
}
