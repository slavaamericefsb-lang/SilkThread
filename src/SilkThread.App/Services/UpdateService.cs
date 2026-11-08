using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
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

    public string UpdateEndpointUrl { get; set; } = 
        "https://raw.githubusercontent.com/SilkThreadApp/SilkThread/main/version.json";

    public event EventHandler<UpdateInfo>? UpdateAvailable;

    public UpdateService()
    {
        var ver = Assembly.GetExecutingAssembly().GetName().Version;
        CurrentVersion = ver != null ? $"{ver.Major}.{ver.Minor}.{ver.Build}" : "2.0.0";
    }

    /// <summary>
    /// Checks for updates against the remote JSON endpoint.
    /// </summary>
    public async Task<UpdateInfo?> CheckForUpdatesAsync(bool isManualCheck = false, CancellationToken ct = default)
    {
        try
        {
            App.Log($"[UpdateService] Checking for updates at: {UpdateEndpointUrl}");

            using var request = new HttpRequestMessage(HttpMethod.Get, UpdateEndpointUrl);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("SilkThread-App", CurrentVersion));
            request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };

            using var response = await s_httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                App.Log($"[UpdateService] Remote server returned HTTP {response.StatusCode}");
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
                return null;
            }

            App.Log($"[UpdateService] Remote version: {updateInfo.Version}, Current version: {CurrentVersion}");

            if (IsNewerVersion(updateInfo.Version, CurrentVersion))
            {
                App.Log($"[UpdateService] Newer version available: {updateInfo.Version} ({updateInfo.Title})");
                UpdateAvailable?.Invoke(this, updateInfo);
                return updateInfo;
            }

            App.Log("[UpdateService] Application is currently up-to-date.");
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
    /// Downloads the updated installer to %TEMP%\SilkThread_Setup.exe and launches it,
    /// exiting the current application process so binary files can be replaced.
    /// </summary>
    public async Task DownloadAndInstallUpdateAsync(
        UpdateInfo update, 
        IProgress<UpdateDownloadProgress>? progress = null, 
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(update);
        if (string.IsNullOrWhiteSpace(update.DownloadUrl))
        {
            throw new InvalidOperationException("Download URL is not specified in update manifest.");
        }

        string tempPath = Path.Combine(Path.GetTempPath(), "SilkThread_Setup.exe");
        App.Log($"[UpdateService] Downloading setup from {update.DownloadUrl} to {tempPath}");

        // Remove any previous downloaded installer file
        if (File.Exists(tempPath))
        {
            try { File.Delete(tempPath); } catch { }
        }

        using (var request = new HttpRequestMessage(HttpMethod.Get, update.DownloadUrl))
        {
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("SilkThread-App", CurrentVersion));

            using var response = await s_httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            long? totalBytes = response.Content.Headers.ContentLength;

            await using var contentStream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            await using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

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

        App.Log($"[UpdateService] Download finished successfully. File size: {new FileInfo(tempPath).Length} bytes.");

        // Spawn installer process and exit app
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = tempPath,
                UseShellExecute = true,
                Verb = "runas"
            };

            App.Log("[UpdateService] Launching installer process...");
            Process.Start(psi);

            // Give process moment to spawn before exit
            await Task.Delay(500, ct).ConfigureAwait(false);
            App.Log("[UpdateService] Exiting SilkThread to release file handles for update.");
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            App.Log($"[UpdateService] Failed to start installer process: {ex.Message}");
            throw;
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
