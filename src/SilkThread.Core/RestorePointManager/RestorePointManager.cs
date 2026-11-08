using System;
using System.Collections.Generic;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Win32;
using SilkThread.Core.Models;
using SilkThread.Core.SystemOperations;

namespace SilkThread.Core.RestorePointManager;

public interface IRestorePointManager
{
    Task<RestorePointStatus> CheckStatusAsync();
    Task<(bool Success, string Message, long SequenceNumber)> CreateRestorePointAsync(string description);
    Task<List<RestorePointItem>> GetRestorePointsAsync();
    Task<(bool Success, string Message)> DeleteRestorePointAsync(long sequenceNumber);
}

public class RestorePointManager : IRestorePointManager
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct RESTOREPOINTINFO
    {
        public int dwEventType;
        public int dwRestorePtType;
        public long llSequenceNumber;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szDescription;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct STATEMGRSTATUS
    {
        public uint nStatus;
        public long llSequenceNumber;
    }

    private const int BEGIN_SYSTEM_CHANGE = 100;
    private const int END_SYSTEM_CHANGE = 101;
    private const int MODIFY_SETTINGS = 12;

    [DllImport("srclient.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool SRSetRestorePointW(ref RESTOREPOINTINFO pRestorePointInfo, ref STATEMGRSTATUS pSMgrStatus);

    public async Task<RestorePointStatus> CheckStatusAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                // Check if SR is disabled via Registry
                using var hklm = Registry.LocalMachine;
                using var key = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore", writable: false);
                if (key != null)
                {
                    var disableVal = key.GetValue("DisableSR");
                    if (disableVal is int d && d == 1)
                    {
                        return new RestorePointStatus(false, false, "System Restore is explicitly disabled in Windows system policies.");
                    }
                }

                // Query WMI root\default:SystemRestore
                try
                {
                    var scope = new ManagementScope(@"\\.\root\default");
                    scope.Connect();
                    using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT * FROM SystemRestore"));
                    using var results = searcher.Get();

                    return new RestorePointStatus(true, true, "System Restore is enabled and system drive protection is active.");
                }
                catch (ManagementException mex)
                {
                    // If class doesn't exist or VSS is disabled
                    return new RestorePointStatus(false, false, $"System Restore service error: {mex.Message}");
                }
            }
            catch (Exception ex)
            {
                return new RestorePointStatus(false, false, $"Unable to verify System Restore: {ex.Message}");
            }
        });
    }

    public async Task<(bool Success, string Message, long SequenceNumber)> CreateRestorePointAsync(string description)
    {
        if (!ElevationHelper.IsAdministrator())
        {
            return (false, "Administrator privileges are required to create a System Restore point.", 0);
        }

        return await Task.Run(async () =>
        {
            // First attempt: native srclient.dll
            try
            {
                var rpi = new RESTOREPOINTINFO
                {
                    dwEventType = BEGIN_SYSTEM_CHANGE,
                    dwRestorePtType = MODIFY_SETTINGS,
                    llSequenceNumber = 0,
                    szDescription = description
                };

                var status = new STATEMGRSTATUS();

                bool ok = SRSetRestorePointW(ref rpi, ref status);
                if (ok && status.nStatus == 0)
                {
                    // Commit end system change
                    var endRpi = new RESTOREPOINTINFO
                    {
                        dwEventType = END_SYSTEM_CHANGE,
                        dwRestorePtType = MODIFY_SETTINGS,
                        llSequenceNumber = status.llSequenceNumber,
                        szDescription = description
                    };
                    SRSetRestorePointW(ref endRpi, ref status);

                    return (true, $"System Restore point '{description}' created successfully.", status.llSequenceNumber);
                }
            }
            catch
            {
                // Fallback to powershell Checkpoint-Computer
            }

            // Fallback: PowerShell Checkpoint-Computer
            try
            {
                var escapedDesc = description.Replace("'", "''");
                var script = $"Checkpoint-Computer -Description '{escapedDesc}' -RestorePointType 'MODIFY_SETTINGS' -ErrorAction Stop";
                var psResult = await CommandOperations.RunAsync("powershell.exe", $"-NoProfile -NonInteractive -Command \"{script}\"", 30000);

                if (psResult.ExitCode == 0)
                {
                    return (true, $"System Restore point '{description}' created via PowerShell.", 0);
                }
                else
                {
                    string err = string.IsNullOrWhiteSpace(psResult.Error) ? psResult.Output : psResult.Error;
                    return (false, $"System Restore creation failed: {err.Trim()}", 0);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Restore point creation encountered an error: {ex.Message}", 0);
            }
        });
    }

    public async Task<List<RestorePointItem>> GetRestorePointsAsync()
    {
        return await Task.Run(() =>
        {
            var list = new List<RestorePointItem>();

            try
            {
                var scope = new ManagementScope(@"\\.\root\default");
                scope.Connect();

                using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT * FROM SystemRestore"));
                using var results = searcher.Get();
                foreach (ManagementObject mo in results)
                {
                    using (mo)
                    {
                        long seq = 0;
                        if (mo["SequenceNumber"] != null)
                            long.TryParse(mo["SequenceNumber"].ToString(), out seq);

                        string desc = mo["Description"]?.ToString() ?? "Restore Point";
                        string type = mo["RestorePointType"]?.ToString() ?? "System";

                        DateTime created = DateTime.UtcNow;
                        if (mo["CreationTime"] != null)
                        {
                            try
                            {
                                created = ManagementDateTimeConverter.ToDateTime(mo["CreationTime"].ToString());
                            }
                            catch { }
                        }

                        list.Add(new RestorePointItem(seq, desc, created, type));
                    }
                }
            }
            catch
            {
                // In case WMI is unavailable or non-elevated
            }

            return list;
        });
    }

    public async Task<(bool Success, string Message)> DeleteRestorePointAsync(long sequenceNumber)
    {
        if (!ElevationHelper.IsAdministrator())
        {
            return (false, "Для удаления точек восстановления требуются права администратора.");
        }

        return await Task.Run(async () =>
        {
            try
            {
                var script = $"Get-CimInstance -ClassName SystemRestore -Namespace 'root/default' -ErrorAction SilentlyContinue | Where-Object {{ $_.SequenceNumber -eq {sequenceNumber} }} | Remove-CimInstance -ErrorAction SilentlyContinue";
                var psResult = await CommandOperations.RunAsync("powershell.exe", $"-NoProfile -NonInteractive -Command \"{script}\"", 15000);
                return (true, $"Точка восстановления #{sequenceNumber} удалена.");
            }
            catch (Exception ex)
            {
                return (false, $"Не удалось удалить точку восстановления: {ex.Message}");
            }
        });
    }
}
