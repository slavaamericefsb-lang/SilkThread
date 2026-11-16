using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

namespace SilkThread.App.Services;

public sealed record ElevationRestartRequest(int ParentId, long ParentStartedUtcTicks, string Route);
public enum ElevationRestartResult { Started, Cancelled, Failed }

/// <summary>Restarts the same executable via Windows UAC; the child waits for the old instance to exit.</summary>
public static class ElevationRestart
{
    public const string Switch = "--elevated-restart";
    private static readonly HashSet<string> Routes = new(StringComparer.Ordinal)
    {
        "Dashboard", "Tweaks", "Scanner", "Optimization", "Games", "SystemSettings", "History", "Restore", "Settings"
    };

    public static ElevationRestartRequest? Parse(IReadOnlyList<string> arguments)
    {
        if (arguments.Count != 4 || arguments[0] != Switch
            || !int.TryParse(arguments[1], NumberStyles.None, CultureInfo.InvariantCulture, out int pid) || pid <= 0
            || !long.TryParse(arguments[2], NumberStyles.None, CultureInfo.InvariantCulture, out long ticks)
            || ticks <= 0 || ticks > DateTime.MaxValue.Ticks || !Routes.Contains(arguments[3])) return null;
        return new(pid, ticks, arguments[3]);
    }

    public static ProcessStartInfo CreateStartInfo(string executable, ElevationRestartRequest request)
    {
        if (!Path.IsPathFullyQualified(executable) || !Routes.Contains(request.Route))
            throw new ArgumentException("Invalid restart target.");
        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = true, Verb = "runas", WorkingDirectory = Path.GetDirectoryName(executable)!
        };
        start.ArgumentList.Add(Switch);
        start.ArgumentList.Add(request.ParentId.ToString(CultureInfo.InvariantCulture));
        start.ArgumentList.Add(request.ParentStartedUtcTicks.ToString(CultureInfo.InvariantCulture));
        start.ArgumentList.Add(request.Route);
        return start;
    }

    public static ElevationRestartResult Start(string route)
    {
        try
        {
            using var parent = Process.GetCurrentProcess();
            var info = CreateStartInfo(Environment.ProcessPath ?? throw new InvalidOperationException("Executable unavailable."),
                new(parent.Id, parent.StartTime.ToUniversalTime().Ticks, route));
            using var child = Process.Start(info);
            return child != null ? ElevationRestartResult.Started : ElevationRestartResult.Failed;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223) { return ElevationRestartResult.Cancelled; }
        catch (Exception ex) { App.Log($"[Elevation restart]: {ex}"); return ElevationRestartResult.Failed; }
    }

    public static async Task<bool> WaitForParentAsync(ElevationRestartRequest request)
    {
        Process? parent = null;
        try
        {
            parent = Process.GetProcessById(request.ParentId);
            // PID reuse or a different executable must never delay/terminate another application.
            if (parent.StartTime.ToUniversalTime().Ticks != request.ParentStartedUtcTicks
                || !string.Equals(parent.MainModule?.FileName, Environment.ProcessPath, StringComparison.OrdinalIgnoreCase)) return false;
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await parent.WaitForExitAsync(timeout.Token);
            return true;
        }
        catch (ArgumentException) { return true; } // The parent already exited, as expected.
        catch (InvalidOperationException) when (parent?.HasExited == true) { return true; }
        catch (Exception ex) { App.Log($"[Elevation handoff]: {ex}"); return false; }
        finally { parent?.Dispose(); }
    }
}
