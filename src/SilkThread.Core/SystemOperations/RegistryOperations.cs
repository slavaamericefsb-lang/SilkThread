using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Win32;
using SilkThread.Core.Execution;

namespace SilkThread.Core.SystemOperations;

public class RegistryValueSnapshot
{
    public bool Exists { get; set; }
    public bool ReadSucceeded { get; set; } = true;
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
            if (baseKey == null) { snapshot.ReadSucceeded = false; return snapshot; }

            using var key = baseKey.OpenSubKey(subKey, writable: false);
            if (key == null) return snapshot;

            var val = key.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            if (val != null)
            {
                snapshot.Exists = true;
                var kind = key.GetValueKind(valueName);
                snapshot.ValueKind = kind.ToString();

                snapshot.RawValue = EncodeSnapshotValue(kind, val);
            }
        }
        catch
        {
            snapshot.ReadSucceeded = false;
            // Access denied is not evidence that the original value was absent.
        }

        return snapshot;
    }

    public static bool SetDword(string hive, string subKey, string valueName, int value)
    {
        try
        {
            using var baseKey = GetHiveKey(hive);
            if (baseKey == null)
            {
                TweakExecutionService.Instance.LogRegistryFailure(hive, subKey, valueName, "Registry hive not accessible");
                return false;
            }

            using var key = baseKey.CreateSubKey(subKey, writable: true);
            if (key == null)
            {
                TweakExecutionService.Instance.LogRegistryFailure(hive, subKey, valueName, "Cannot create or open subkey");
                return false;
            }

            // Preserve existing String type if Windows expects a string for this value
            try
            {
                if (key.GetValue(valueName) != null && key.GetValueKind(valueName) == RegistryValueKind.String)
                {
                    key.SetValue(valueName, value.ToString(), RegistryValueKind.String);
                    TweakExecutionService.Instance.LogRegistry(hive, subKey, valueName, value.ToString(), "String");
                    return true;
                }
            }
            catch { }

            key.SetValue(valueName, value, RegistryValueKind.DWord);
            TweakExecutionService.Instance.LogRegistry(hive, subKey, valueName, value, "DWORD");
            return true;
        }
        catch (Exception ex)
        {
            TweakExecutionService.Instance.LogRegistryFailure(hive, subKey, valueName, ex.Message);
            return false;
        }
    }

    public static bool SetString(string hive, string subKey, string valueName, string value)
    {
        try
        {
            using var baseKey = GetHiveKey(hive);
            if (baseKey == null)
            {
                TweakExecutionService.Instance.LogRegistryFailure(hive, subKey, valueName, "Registry hive not accessible");
                return false;
            }

            using var key = baseKey.CreateSubKey(subKey, writable: true);
            if (key == null)
            {
                TweakExecutionService.Instance.LogRegistryFailure(hive, subKey, valueName, "Cannot create or open subkey");
                return false;
            }

            key.SetValue(valueName, value, RegistryValueKind.String);
            TweakExecutionService.Instance.LogRegistry(hive, subKey, valueName, value, "String");
            return true;
        }
        catch (Exception ex)
        {
            TweakExecutionService.Instance.LogRegistryFailure(hive, subKey, valueName, ex.Message);
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
                TweakExecutionService.Instance.Log($"[REGISTRY] Delete {hive}:\\{subKey} -> {valueName}");
            }
            return true;
        }
        catch (Exception ex)
        {
            TweakExecutionService.Instance.LogRegistryFailure(hive, subKey, valueName, $"Delete failed: {ex.Message}");
            return false;
        }
    }

    public static int? GetDword(string hive, string subKey, string valueName, bool requireExactKind = false)
    {
        try
        {
            using var baseKey = GetHiveKey(hive);
            if (baseKey == null) return null;

            using var key = baseKey.OpenSubKey(subKey, writable: false);
            if (key == null) return null;

            if (requireExactKind && key.GetValueKind(valueName) != RegistryValueKind.DWord) return null;
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

    public static string? GetString(string hive, string subKey, string valueName, bool requireExactKind = false)
    {
        try
        {
            using var baseKey = GetHiveKey(hive);
            if (baseKey == null) return null;

            using var key = baseKey.OpenSubKey(subKey, writable: false);
            if (key == null) return null;

            if (requireExactKind && key.GetValueKind(valueName) != RegistryValueKind.String) return null;
            return key.GetValue(valueName)?.ToString();
        }
        catch
        {
            return null;
        }
    }

    public static string EncodeSnapshotValue(RegistryValueKind kind, object value) => kind switch
    {
        RegistryValueKind.MultiString => JsonSerializer.Serialize((string[])value),
        RegistryValueKind.Binary or RegistryValueKind.None => Convert.ToBase64String((byte[])value),
        _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty
    };

    public static object DecodeSnapshotValue(RegistryValueKind kind, string? rawValue) => kind switch
    {
        RegistryValueKind.MultiString => JsonSerializer.Deserialize<string[]>(rawValue ?? "null")
            ?? throw new FormatException("Invalid multi-string snapshot."),
        RegistryValueKind.Binary or RegistryValueKind.None => Convert.FromBase64String(rawValue ?? throw new FormatException("Missing binary snapshot.")),
        RegistryValueKind.DWord => int.Parse(rawValue!, System.Globalization.CultureInfo.InvariantCulture),
        RegistryValueKind.QWord => long.Parse(rawValue!, System.Globalization.CultureInfo.InvariantCulture),
        RegistryValueKind.String or RegistryValueKind.ExpandString => rawValue ?? string.Empty,
        _ => throw new FormatException("Unsupported registry value kind.")
    };

    public static bool RestoreFromSnapshot(RegistryValueSnapshot snapshot)
    {
        try
        {
            if (!snapshot.ReadSucceeded) return false;
            // Decode before opening a writable key: corrupt data must not change the registry.
            if (snapshot.Exists)
            {
                if (!Enum.TryParse<RegistryValueKind>(snapshot.ValueKind, out var validatedKind)) return false;
                _ = DecodeSnapshotValue(validatedKind, snapshot.RawValue);
            }
            using var baseKey = GetHiveKey(snapshot.Hive);
            if (baseKey == null) return false;

            if (!snapshot.Exists)
            {
                // Value originally did not exist -> remove it
                return DeleteValue(snapshot.Hive, snapshot.SubKey, snapshot.ValueName);
            }

            using var key = baseKey.CreateSubKey(snapshot.SubKey, writable: true);
            if (key == null) return false;

            if (!Enum.TryParse<RegistryValueKind>(snapshot.ValueKind, out var kind)) return false;
            key.SetValue(snapshot.ValueName, DecodeSnapshotValue(kind, snapshot.RawValue), kind);
            return true;
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
