using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SilkThread.Core.SystemOperations;

namespace SilkThread.Core.GameProfiles;

public class GameProfileService : IGameProfileService
{
    private const string GpuPrefKey = @"Software\Microsoft\DirectX\UserGpuPreferences";
    private const string LayersKey = @"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers";
    private const string IfeoKeyPrefix = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options";
    private const string GameConfigStoreKey = @"System\GameConfigStore";

    public IReadOnlyList<GameProfile> GetAllProfiles()
    {
        return GameProfileCatalog.GetAllProfiles();
    }

    public GameProfile? GetProfileById(string id)
    {
        return GameProfileCatalog.GetProfileById(id);
    }

    public bool IsProfileApplied(string id)
    {
        var profile = GetProfileById(id);
        if (profile == null || profile.ExecutableNames.Count == 0) return false;

        foreach (var exe in profile.ExecutableNames)
        {
            var gpuSnap = RegistryOperations.SnapshotValue("HKCU", GpuPrefKey, exe);
            if (gpuSnap.Exists && gpuSnap.RawValue != null && gpuSnap.RawValue.Contains("GpuPreference=2"))
            {
                return true;
            }

            var layersSnap = RegistryOperations.SnapshotValue("HKCU", LayersKey, exe);
            if (layersSnap.Exists && layersSnap.RawValue != null && layersSnap.RawValue.Contains("DISABLEDXMAXIMIZEDWINDOWEDMODE"))
            {
                return true;
            }
        }

        return false;
    }

    public async Task<bool> ApplyProfileAsync(string id)
    {
        return await Task.Run(() =>
        {
            var profile = GetProfileById(id);
            if (profile == null) return false;

            bool success = true;

            foreach (var exe in profile.ExecutableNames)
            {
                // 1. High Performance GPU Preference
                try
                {
                    RegistryOperations.SetString("HKCU", GpuPrefKey, exe, "GpuPreference=2;");
                }
                catch
                {
                    success = false;
                }

                // 2. DirectFlip & Disable Fullscreen Optimizations
                try
                {
                    RegistryOperations.SetString("HKCU", LayersKey, exe, "~ DISABLEDXMAXIMIZEDWINDOWEDMODE HIGHDPIAWARE");
                }
                catch
                {
                    success = false;
                }

                // 3. High Process Priority via IFEO PerfOptions
                try
                {
                    string perfOptionsSubKey = $@"{IfeoKeyPrefix}\{exe}\PerfOptions";
                    RegistryOperations.SetDword("HKLM", perfOptionsSubKey, "CpuPriorityClass", 3);
                    RegistryOperations.SetDword("HKLM", perfOptionsSubKey, "IoPriority", 3);
                }
                catch
                {
                    // IFEO requires admin, if not admin, ignore failure
                }
            }

            // 4. Windows GameDVR DirectFlip flags
            try
            {
                RegistryOperations.SetDword("HKCU", GameConfigStoreKey, "GameDVR_FSEBehaviorMode", 2);
                RegistryOperations.SetDword("HKCU", GameConfigStoreKey, "GameDVR_HonorUserFSEBehaviorMode", 1);
                RegistryOperations.SetDword("HKCU", GameConfigStoreKey, "GameDVR_DXGIHonorFSEWindowsCompatible", 1);
            }
            catch { }

            return success;
        });
    }

    public async Task<bool> RevertProfileAsync(string id)
    {
        return await Task.Run(() =>
        {
            var profile = GetProfileById(id);
            if (profile == null) return false;

            foreach (var exe in profile.ExecutableNames)
            {
                try
                {
                    RegistryOperations.DeleteValue("HKCU", GpuPrefKey, exe);
                    RegistryOperations.DeleteValue("HKCU", LayersKey, exe);

                    string perfOptionsSubKey = $@"{IfeoKeyPrefix}\{exe}\PerfOptions";
                    RegistryOperations.DeleteValue("HKLM", perfOptionsSubKey, "CpuPriorityClass");
                    RegistryOperations.DeleteValue("HKLM", perfOptionsSubKey, "IoPriority");
                }
                catch { }
            }

            return true;
        });
    }
}
