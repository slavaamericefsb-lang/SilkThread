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

        static string BatchPath(string value)
        {
            if (value.IndexOfAny(new[] { '\"', '\r', '\n' }) >= 0)
                throw new ArgumentException("Invalid update path.");
            return value.Replace("%", "%%");
        }
        string transaction = GeneratePatchTransactionScript(patchZipPath, appDir);
        string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(transaction));
        var sb = new StringBuilder();
        sb.AppendLine("@echo off");
        sb.AppendLine("setlocal DisableDelayedExpansion");
        sb.AppendLine("timeout /t 1 /nobreak >nul 2>&1 || ping 127.0.0.1 -n 2 >nul");
        // cmd.exe limits a single command line to 8191 characters. Store bounded
        // base64 chunks instead of passing the entire transaction on its command line.
        sb.AppendLine("set \"SILK_UPDATE_PAYLOAD=%~f0.payload\"");
        for (int offset = 0; offset < encoded.Length; offset += 120)
        {
            string redirect = offset == 0 ? ">" : ">>";
            sb.AppendLine($"{redirect} \"%SILK_UPDATE_PAYLOAD%\" echo {encoded.Substring(offset, Math.Min(120, encoded.Length - offset))}");
        }
        sb.AppendLine("powershell -NoProfile -NonInteractive -Command \"& ([scriptblock]::Create([Text.Encoding]::Unicode.GetString([Convert]::FromBase64String([IO.File]::ReadAllText($env:SILK_UPDATE_PAYLOAD)))))\"");
        sb.AppendLine("if errorlevel 1 exit /b 1");
        sb.AppendLine("del /f /q \"%SILK_UPDATE_PAYLOAD%\" >nul 2>&1");
        sb.AppendLine($"start \"\" \"{BatchPath(appExePath)}\"");
        sb.AppendLine($"del /f /q \"{BatchPath(patchZipPath)}\" >nul 2>&1");
        sb.AppendLine("(goto) 2>nul & del \"%~f0\"");
        return sb.ToString();
    }

    /// <summary>Stages the entire archive and journals original files before replacing any file.</summary>
    public static string GeneratePatchTransactionScript(string patchZipPath, string appDir)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(patchZipPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(appDir);
        static string Literal(string value) => "'" + value.Replace("'", "''") + "'";
        return "$patch = " + Literal(Path.GetFullPath(patchZipPath)) + "\r\n$root = " + Literal(Path.GetFullPath(appDir).TrimEnd('\\', '/')) + "\r\n" +
            """
            $ErrorActionPreference = 'Stop'
            $recovery = Join-Path (Split-Path -Parent $root) ('SilkThread-recovery-' + [Guid]::NewGuid().ToString('N'))
            $stage = Join-Path $recovery 'stage'
            $backup = Join-Path $recovery 'backup'
            $journal = @()
            $touched = @()
            try {
                New-Item -ItemType Directory -Path $stage,$backup | Out-Null
                Add-Type -AssemblyName System.IO.Compression.FileSystem
                $zip = [IO.Compression.ZipFile]::OpenRead($patch)
                try {
                    $prefix = $stage + [IO.Path]::DirectorySeparatorChar
                    foreach ($entry in $zip.Entries) {
                        $name = $entry.FullName.Replace('/', '\')
                        $destination = [IO.Path]::GetFullPath((Join-Path $stage $name))
                        if ([IO.Path]::IsPathRooted($name) -or $name.Contains(':') -or
                            !$destination.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase) -or
                            (($entry.ExternalAttributes -shr 16) -band 0xF000) -eq 0xA000) {
                            throw "Unsafe patch entry: $name"
                        }
                    }
                } finally { $zip.Dispose() }
                Expand-Archive -LiteralPath $patch -DestinationPath $stage -Force -ErrorAction Stop
                $files = @(Get-ChildItem -LiteralPath $stage -File -Recurse | Sort-Object FullName)
                if ($files.Count -eq 0) { throw 'Patch contains no files' }
                # Back up every original before committing the first staged file.
                foreach ($file in $files) {
                    $relative = $file.FullName.Substring($stage.Length + 1)
                    $destination = Join-Path $root $relative
                    $saved = Join-Path $backup $relative
                    $cursor = $destination
                    while ($cursor -and $cursor.Length -ge $root.Length) {
                        if (Test-Path -LiteralPath $cursor) {
                            if ((Get-Item -LiteralPath $cursor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Destination contains a link' }
                        }
                        $cursor = Split-Path -Parent $cursor
                    }
                    $existed = [IO.File]::Exists($destination)
                    if ($existed) {
                        [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($saved)) | Out-Null
                        [IO.File]::Copy($destination, $saved, $false)
                    } elseif (Test-Path -LiteralPath $destination) { throw "Destination is a directory: $destination" }
                    $journal += [pscustomobject]@{ Destination=$destination; Backup=$saved; Existed=$existed; Source=$file.FullName }
                }
                $journal | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $recovery 'journal.json') -Encoding UTF8
                foreach ($item in $journal) {
                    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($item.Destination)) | Out-Null
                    $touched += $item
                    [IO.File]::Copy($item.Source, $item.Destination, $true)
                }
            } catch {
                $failure = $_.ToString()
                [array]::Reverse($touched)
                foreach ($item in $touched) {
                    try {
                        if ($item.Existed) { [IO.File]::Copy($item.Backup, $item.Destination, $true) }
                        elseif ([IO.File]::Exists($item.Destination)) { [IO.File]::Delete($item.Destination) }
                    } catch { $failure += "`r`nRollback error: " + $_.ToString() }
                }
                if (Test-Path -LiteralPath $recovery) { $failure | Set-Content -LiteralPath (Join-Path $recovery 'failure.txt') }
                exit 1
            }
            # Recovery is removed only after all staged files have been committed.
            try { Remove-Item -LiteralPath $recovery -Recurse -Force -ErrorAction Stop } catch { }
            exit 0
            """;
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
                ValidatePatchArchive(zipCheck, AppContext.BaseDirectory);
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

                await Task.Delay(500).ConfigureAwait(false);
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

                await Task.Delay(500).ConfigureAwait(false);
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

    /// <summary>Validates every archive destination before an elevated extractor sees it.</summary>
    public static void ValidatePatchArchive(ZipArchive archive, string targetDirectory)
    {
        string root = Path.GetFullPath(targetDirectory).TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
        bool hasFiles = false;
        foreach (var entry in archive.Entries)
        {
            string name = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
            if (Path.IsPathRooted(name) || name.Contains(':') ||
                !Path.GetFullPath(Path.Combine(root, name)).StartsWith(root, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Patch entry escapes application directory: {entry.FullName}");

            // Unix symlinks can redirect subsequent extraction outside the destination.
            if (((entry.ExternalAttributes >> 16) & 0xF000) == 0xA000 ||
                (entry.ExternalAttributes & (int)FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException($"Patch contains a link: {entry.FullName}");
            if (!string.IsNullOrEmpty(entry.Name)) hasFiles = true;
        }
        if (!hasFiles) throw new InvalidDataException("Downloaded patch archive contains no files.");
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
