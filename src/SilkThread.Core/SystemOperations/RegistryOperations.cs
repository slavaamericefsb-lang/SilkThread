using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Win32;

namespace SilkThread.Core.SystemOperations;

public class RegistryValueSnapshot
{
    public bool Exists { get; set; }
    public string Hive { get; set; } = string.Empty;
    public string SubKey { get; set; } = string.Empty;
    public string ValueName { get; set; } = string.Empty;
    public string? ValueKind { get; set; }
    public string? RawValue { get; set; }

    public string ToJson() => JsonSerializer.Serialize(this);
    public static RegistryValueSnapshot? FromJson(string json) => JsonSerializer.Deserialize<RegistryValueSnapshot>(json);
}

public class RegistryMultiSnapshot
{
    public List<RegistryValueSnapshot> Snapshots { get; set; } = new();
    public string ToJson() => JsonSerializer.Serialize(this);
    public static RegistryMultiSnapshot? FromJson(string json) => JsonSerializer.Deserialize<RegistryMultiSnapshot>(json);
}

public static class RegistryOperations
{
    private static RegistryKey? GetHiveKey(string hive)
    {
        var h = hive.ToUpperInvariant() switch
        {
            "HKLM" or "HKEY_LOCAL_MACHINE" => RegistryHive.LocalMachine,
            "HKCU" or "HKEY_CURRENT_USER" => RegistryHive.CurrentUser,
            "HKCR" or "HKEY_CLASSES_ROOT" => RegistryHive.ClassesRoot,
            "HKU" or "HKEY_USERS" => RegistryHive.Users,
            _ => (RegistryHive?)null
        };

        if (h == null) return null;
        return RegistryKey.OpenBaseKey(h.Value, RegistryView.Default);
    }

    public static RegistryValueSnapshot SnapshotValue(string hive, string subKey, string valueName)
    {
        var snapshot = new RegistryValueSnapshot
        {
            Hive = hive,
            SubKey = subKey,
            ValueName = valueName,
            Exists = false
        };

        try
        {
            using var baseKey = GetHiveKey(hive);
            if (baseKey == null) return snapshot;

            using var key = baseKey.OpenSubKey(subKey, writable: false);
            if (key == null) return snapshot;

            var val = key.GetValue(valueName);
            if (val != null)
            {
                snapshot.Exists = true;
                var kind = key.GetValueKind(valueName);
                snapshot.ValueKind = kind.ToString();

                if (kind == RegistryValueKind.Binary && val is byte[] bytes)
                {
                    snapshot.RawValue = Convert.ToBase64String(bytes);
                }
                else
                {
                    snapshot.RawValue = val.ToString();
                }
            }
        }
        catch
        {
            // If access denied or missing, return not exists
        }

        return snapshot;
    }

    public static bool SetDword(string hive, string subKey, string valueName, int value)
    {
        try
        {
            using var baseKey = GetHiveKey(hive);
            if (baseKey == null) return false;

            using var key = baseKey.CreateSubKey(subKey, writable: true);
            if (key == null) return false;

            // Preserve existing String type if Windows expects a string for this value
            try
            {
                if (key.GetValue(valueName) != null && key.GetValueKind(valueName) == RegistryValueKind.String)
                {
                    key.SetValue(valueName, value.ToString(), RegistryValueKind.String);
                    return true;
                }
            }
            catch { }

            key.SetValue(valueName, value, RegistryValueKind.DWord);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool SetString(string hive, string subKey, string valueName, string value)
    {
        try
        {
            using var baseKey = GetHiveKey(hive);
            if (baseKey == null) return false;

            using var key = baseKey.CreateSubKey(subKey, writable: true);
            if (key == null) return false;

            key.SetValue(valueName, value, RegistryValueKind.String);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool DeleteValue(string hive, string subKey, string valueName)
    {
        try
        {
            using var baseKey = GetHiveKey(hive);
            if (baseKey == null) return false;

            using var key = baseKey.OpenSubKey(subKey, writable: true);
            if (key == null) return true; // Already gone

            if (key.GetValue(valueName) != null)
            {
                key.DeleteValue(valueName, throwOnMissingValue: false);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static int? GetDword(string hive, string subKey, string valueName)
    {
        try
        {
            using var baseKey = GetHiveKey(hive);
            if (baseKey == null) return null;

            using var key = baseKey.OpenSubKey(subKey, writable: false);
            if (key == null) return null;

            var val = key.GetValue(valueName);
            if (val is int intVal) return intVal;
            if (val != null && int.TryParse(val.ToString(), out int parsed)) return parsed;
            return null;
        }
        catch
        {
            return null;
        }
    }

    public static string? GetString(string hive, string subKey, string valueName)
    {
        try
        {
            using var baseKey = GetHiveKey(hive);
            if (baseKey == null) return null;

            using var key = baseKey.OpenSubKey(subKey, writable: false);
            if (key == null) return null;

            return key.GetValue(valueName)?.ToString();
        }
        catch
        {
            return null;
        }
    }

    public static bool RestoreFromSnapshot(RegistryValueSnapshot snapshot)
    {
        try
        {
            using var baseKey = GetHiveKey(snapshot.Hive);
            if (baseKey == null) return false;

            if (!snapshot.Exists)
            {
                // Value originally did not exist -> remove it
                return DeleteValue(snapshot.Hive, snapshot.SubKey, snapshot.ValueName);
            }

            using var key = baseKey.CreateSubKey(snapshot.SubKey, writable: true);
            if (key == null) return false;

            if (Enum.TryParse<RegistryValueKind>(snapshot.ValueKind, out var kind))
            {
                object valueToSet;
                if (kind == RegistryValueKind.DWord && int.TryParse(snapshot.RawValue, out int dwordVal))
                {
                    valueToSet = dwordVal;
                }
                else if (kind == RegistryValueKind.QWord && long.TryParse(snapshot.RawValue, out long qwordVal))
                {
                    valueToSet = qwordVal;
                }
                else if (kind == RegistryValueKind.Binary && !string.IsNullOrEmpty(snapshot.RawValue))
                {
                    try
                    {
                        valueToSet = Convert.FromBase64String(snapshot.RawValue);
                    }
                    catch
                    {
                        valueToSet = Array.Empty<byte>();
                    }
                }
                else
                {
                    valueToSet = snapshot.RawValue ?? string.Empty;
                }

                key.SetValue(snapshot.ValueName, valueToSet, kind);
                return true;
            }
            else
            {
                key.SetValue(snapshot.ValueName, snapshot.RawValue ?? string.Empty);
                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    public static bool RestoreFromMultiSnapshot(RegistryMultiSnapshot multiSnapshot)
    {
        bool allOk = true;
        foreach (var item in multiSnapshot.Snapshots)
        {
            if (!RestoreFromSnapshot(item))
            {
                allOk = false;
            }
        }
        return allOk;
    }
}
