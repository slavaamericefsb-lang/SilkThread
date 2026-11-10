using System;
using System.ServiceProcess;
using System.Text.Json;
using Microsoft.Win32;

namespace SilkThread.Core.SystemOperations;

public class ServiceStateSnapshot
{
    public string ServiceName { get; set; } = string.Empty;
    public bool Exists { get; set; }
    public int? OriginalStartType { get; set; } // 2=Auto, 3=Manual, 4=Disabled
    public int? DelayedAutoStart { get; set; }
    public string? OriginalStatus { get; set; } // Running, Stopped, etc.

    public string ToJson() => JsonSerializer.Serialize(this);
    public static ServiceStateSnapshot? FromJson(string json) => JsonSerializer.Deserialize<ServiceStateSnapshot>(json);
}

public static class ServiceOperations
{
    private const string ServicesRegPath = @"SYSTEM\CurrentControlSet\Services";

    public static ServiceStateSnapshot SnapshotService(string serviceName)
    {
        var snapshot = new ServiceStateSnapshot
        {
            ServiceName = serviceName,
            Exists = false
        };

        try
        {
            using var hklm = Registry.LocalMachine;
            using var key = hklm.OpenSubKey($@"{ServicesRegPath}\{serviceName}", writable: false);
            if (key != null)
            {
                snapshot.Exists = true;
                var startVal = key.GetValue("Start");
                if (startVal is int s) snapshot.OriginalStartType = s;

                var delayedVal = key.GetValue("DelayedAutostart");
                if (delayedVal is int d) snapshot.DelayedAutoStart = d;
            }

            using var sc = new ServiceController(serviceName);
            snapshot.OriginalStatus = sc.Status.ToString();
        }
        catch
        {
            // Service might not exist or access limited
        }

        return snapshot;
    }

    public static bool SetServiceStartType(string serviceName, int startType)
    {
        try
        {
            using var hklm = Registry.LocalMachine;
            using var key = hklm.OpenSubKey($@"{ServicesRegPath}\{serviceName}", writable: true);
            if (key == null) return false;

            key.SetValue("Start", startType, RegistryValueKind.DWord);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static int? GetServiceStartType(string serviceName)
    {
        try
        {
            using var hklm = Registry.LocalMachine;
            using var key = hklm.OpenSubKey($@"{ServicesRegPath}\{serviceName}", writable: false);
            if (key == null) return null;

            var val = key.GetValue("Start");
            if (val is int s) return s;
            return null;
        }
        catch
        {
            return null;
        }
    }

    public static string GetServiceStatus(string serviceName)
    {
        try
        {
            using var sc = new ServiceController(serviceName);
            return sc.Status.ToString();
        }
        catch
        {
            return "Unknown";
        }
    }

    public static bool StopServiceIfRunning(string serviceName, int timeoutSeconds = 5)
    {
        try
        {
            using var sc = new ServiceController(serviceName);
            if (sc.Status == ServiceControllerStatus.Running)
            {
                sc.Stop();
                sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(timeoutSeconds));
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool RestoreFromSnapshot(ServiceStateSnapshot snapshot)
    {
        if (!snapshot.Exists || !snapshot.OriginalStartType.HasValue) return false;

        bool ok = SetServiceStartType(snapshot.ServiceName, snapshot.OriginalStartType.Value);
        if (snapshot.DelayedAutoStart.HasValue)
        {
            try
            {
                using var hklm = Registry.LocalMachine;
                using var key = hklm.OpenSubKey($@"{ServicesRegPath}\{snapshot.ServiceName}", writable: true);
                key?.SetValue("DelayedAutostart", snapshot.DelayedAutoStart.Value, RegistryValueKind.DWord);
            }
            catch { }
        }

        return ok;
    }
}
