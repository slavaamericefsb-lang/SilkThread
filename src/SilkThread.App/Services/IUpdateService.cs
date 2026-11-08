using System;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace SilkThread.App.Services;

/// <summary>
/// Remote update manifest schema.
/// </summary>
public sealed class UpdateInfo
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("downloadUrl")]
    public string DownloadUrl { get; set; } = string.Empty;

    [JsonPropertyName("patchUrl")]
    public string? PatchUrl { get; set; }

    [JsonPropertyName("patchSize")]
    public long? PatchSize { get; set; }

    [JsonPropertyName("releaseDate")]
    public string ReleaseDate { get; set; } = string.Empty;
}

/// <summary>
/// Progress reporting during package download.
/// </summary>
public readonly record struct UpdateDownloadProgress(
    double Percentage,
    long BytesReceived,
    long? TotalBytes);

/// <summary>
/// Service contract for remote update checks and self-updating.
/// </summary>
public interface IUpdateService
{
    /// <summary>
    /// Current installed application version (e.g. "1.1.0").
    /// </summary>
    string CurrentVersion { get; }

    /// <summary>
    /// Last detected remote version on server (e.g. "1.1.0").
    /// </summary>
    string? LastRemoteVersion { get; }

    /// <summary>
    /// Remote URL endpoint hosting version.json.
    /// </summary>
    string UpdateEndpointUrl { get; set; }

    /// <summary>
    /// Checks the remote endpoint for newer versions.
    /// </summary>
    Task<UpdateInfo?> CheckForUpdatesAsync(bool isManualCheck = false, CancellationToken ct = default);

    /// <summary>
    /// Downloads the new installer to %TEMP%\SilkThread_Setup.exe and launches it, exiting current app.
    /// </summary>
    Task DownloadAndInstallUpdateAsync(UpdateInfo update, IProgress<UpdateDownloadProgress>? progress = null, CancellationToken ct = default);

    /// <summary>
    /// Raised when a new update is detected.
    /// </summary>
    event EventHandler<UpdateInfo>? UpdateAvailable;
}
