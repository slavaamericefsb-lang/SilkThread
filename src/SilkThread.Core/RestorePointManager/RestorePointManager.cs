using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.Json;
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
    Task<(bool Success, string Message)> EnableProtectionAsync(string drive = "C:\\");
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
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore");
            if (key?.GetValue("DisableSR") is int disabled && disabled == 1)
                return new(false, false, "Защита системы выключена настройкой Windows.");
            var points = await GetRestorePointsAsync();
            // Existing restore points and VSS storage do not prove that monitoring is enabled now.
            return new(true, false, $"Чтение доступно. Найдено точек: {points.Count}. Состояние защиты диска проверяйте в параметрах защиты системы Windows.") { ProtectionStateKnown = false };
        }
        catch (UnauthorizedAccessException)
        {
            return new(false, false, "Для чтения точек восстановления нужны права администратора. Нажмите «Пользователь» вверху окна и подтвердите перезапуск через UAC.") { ProtectionStateKnown = false };
        }
        catch (Exception ex)
        {
            return new(false, false, $"Не удалось прочитать точки восстановления: {ex.Message}") { ProtectionStateKnown = false };
        }
    }

    public async Task<(bool Success, string Message, long SequenceNumber)> CreateRestorePointAsync(string description)
    {
        if (!ElevationHelper.IsAdministrator())
        {
            return (false, "Для создания точки восстановления требуются права администратора.", 0);
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
                    var endRpi = new RESTOREPOINTINFO
                    {
                        dwEventType = END_SYSTEM_CHANGE,
                        dwRestorePtType = MODIFY_SETTINGS,
                        llSequenceNumber = status.llSequenceNumber,
                        szDescription = description
                    };
                    SRSetRestorePointW(ref endRpi, ref status);

                    return (true, $"Точка восстановления «{description}» успешно создана.", status.llSequenceNumber);
                }
            }
            catch
            {
                // Fallback to PowerShell Checkpoint-Computer
            }

            // Fallback: PowerShell Checkpoint-Computer
            try
            {
                var escapedDesc = description.Replace("'", "''");
                var script = $"Checkpoint-Computer -Description '{escapedDesc}' -RestorePointType 'MODIFY_SETTINGS' -ErrorAction Stop";
                var psResult = await CommandOperations.RunAsync("powershell.exe", $"-NoProfile -NonInteractive -Command \"{script}\"", 30000);

                if (psResult.ExitCode == 0)
                {
                    return (true, $"Точка восстановления «{description}» успешно создана.", 0);
                }
                else
                {
                    string err = string.IsNullOrWhiteSpace(psResult.Error) ? psResult.Output : psResult.Error;
                    return (false, $"Не удалось создать точку восстановления: {err.Trim()}", 0);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка при создании точки восстановления: {ex.Message}", 0);
            }
        });
    }

    public async Task<List<RestorePointItem>> GetRestorePointsAsync()
    {
        return await Task.Run(async () =>
        {
            var list = new List<RestorePointItem>();

            Exception? readError = null;
            // 1. First attempt: WMI root\default:SystemRestore
            try
            {
                var scope = new ManagementScope(@"\\.\root\default", new ConnectionOptions { Timeout = TimeSpan.FromSeconds(5), EnablePrivileges = true });
                scope.Connect();

                var opt = new System.Management.EnumerationOptions { ReturnImmediately = true, Timeout = TimeSpan.FromSeconds(5) };
                using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT * FROM SystemRestore"), opt);
                using var results = searcher.Get();
                foreach (ManagementObject mo in results)
                {
                    using (mo)
                    {
                        long seq = 0;
                        if (mo["SequenceNumber"] != null)
                            long.TryParse(mo["SequenceNumber"].ToString(), out seq);

                        string desc = mo["Description"]?.ToString() ?? "Restore Point";
                        string rawType = mo["RestorePointType"]?.ToString() ?? "12";
                        string rawEvent = mo["EventType"]?.ToString() ?? "100";

                        DateTime created = ParseCreationTime(mo["CreationTime"]);
                        string humanType = ResolveHumanReadableType(rawType, rawEvent, desc);

                        list.Add(new RestorePointItem(seq, desc, created, humanType));
                    }
                }
            }
            catch (Exception ex)
            {
                readError = ex;
            }
            if (readError == null) return list.OrderByDescending(r => r.SequenceNumber).ToList();
            if (IsAccessDenied(readError)) throw new UnauthorizedAccessException("Windows denied access to SystemRestore.", readError);

            // 2. Fallback: PowerShell Get-ComputerRestorePoint
            var script = "[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false); $ErrorActionPreference='Stop'; $points = @(Get-ComputerRestorePoint -ErrorAction Stop | Select-Object SequenceNumber,Description,CreationTime,RestorePointType,EventType); ConvertTo-Json -InputObject $points -Compress";
            var psRes = await CommandOperations.RunAsync("powershell.exe", $"-NoProfile -NonInteractive -Command \"{script}\"", 15000);
            if (psRes.ExitCode != 0)
                throw new InvalidOperationException($"SystemRestore could not be read: {readError.Message}; {psRes.Error.Trim()}", readError);
            return ParseRestorePointsJson(psRes.Output);
        });
    }

    public static bool IsAccessDenied(Exception error) => error is UnauthorizedAccessException
        || error is ManagementException { ErrorCode: ManagementStatus.AccessDenied }
        || error.HResult is unchecked((int)0x80070005) or unchecked((int)0x80041003);

    public static List<RestorePointItem> ParseRestorePointsJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var rows = doc.RootElement.ValueKind == JsonValueKind.Array ? doc.RootElement.EnumerateArray().ToArray()
            : doc.RootElement.ValueKind == JsonValueKind.Object ? new[] { doc.RootElement }
            : throw new JsonException("Expected restore point array or object.");
        return rows.Select(e => ParseJsonRestorePoint(e) ?? throw new JsonException("Invalid restore point record."))
            .OrderByDescending(p => p.SequenceNumber).ToList();
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

    public async Task<(bool Success, string Message)> EnableProtectionAsync(string drive = "C:\\")
    {
        if (!ElevationHelper.IsAdministrator())
        {
            return (false, "Для включения защиты системы требуются права администратора.");
        }

        return await Task.Run(async () =>
        {
            try
            {
                var cleanDrive = drive.TrimEnd('\\');
                var script = $"Enable-ComputerRestore -Drive '{cleanDrive}\\'; vssadmin resize shadowstorage /for={cleanDrive} /on={cleanDrive} /maxsize=10GB";
                var res = await CommandOperations.RunAsync("powershell.exe", $"-NoProfile -NonInteractive -Command \"{script}\"", 20000);
                if (res.ExitCode == 0)
                {
                    return (true, $"Защита системы на диске {cleanDrive} успешно включена.");
                }
                return (false, $"Не удалось включить защиту: {res.Error}");
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка при включении защиты системы: {ex.Message}");
            }
        });
    }

    public static string ResolveHumanReadableType(string? rawType, string? eventType, string description)
    {
        if (description.Contains("SilkThread", StringComparison.OrdinalIgnoreCase))
        {
            return "Перед оптимизацией SilkThread";
        }

        if (description.Contains("Checkpoint", StringComparison.OrdinalIgnoreCase) ||
            description.Contains("Пользовател", StringComparison.OrdinalIgnoreCase) ||
            description.Contains("Manual", StringComparison.OrdinalIgnoreCase) ||
            description.Contains("User", StringComparison.OrdinalIgnoreCase))
        {
            return "Создано пользователем";
        }

        if (int.TryParse(rawType, out int typeInt))
        {
            return typeInt switch
            {
                0 => "Установка ПО",
                1 => "Удаление ПО",
                10 => "Установка драйвера",
                11 or 12 => "Изменение параметров",
                13 => "Отмененная операция",
                6 => "Восстановление системы",
                7 => "Контрольная точка Windows",
                _ => "Создано пользователем"
            };
        }

        if (!string.IsNullOrWhiteSpace(rawType))
        {
            if (rawType.Contains("APPLICATION_INSTALL", StringComparison.OrdinalIgnoreCase)) return "Установка ПО";
            if (rawType.Contains("APPLICATION_UNINSTALL", StringComparison.OrdinalIgnoreCase)) return "Удаление ПО";
            if (rawType.Contains("DEVICE_DRIVER_INSTALL", StringComparison.OrdinalIgnoreCase)) return "Установка драйвера";
            if (rawType.Contains("MODIFY_SETTINGS", StringComparison.OrdinalIgnoreCase)) return "Изменение параметров";
            if (rawType.Contains("CANCELLED_OPERATION", StringComparison.OrdinalIgnoreCase)) return "Отмененная операция";
        }

        return "Создано пользователем";
    }

    public static DateTime ParseCreationTime(object? rawTime)
    {
        if (rawTime == null) return DateTime.Now;

        if (rawTime is DateTime dt)
        {
            return dt.Kind == DateTimeKind.Utc ? dt.ToLocalTime() : dt;
        }

        string str = rawTime.ToString()?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(str)) return DateTime.Now;

        // 1. Check if WMI CIM_DATETIME format: yyyyMMddHHmmss.xxxxxx+zzz
        if (str.Length >= 14 && char.IsDigit(str[0]) && char.IsDigit(str[1]) && char.IsDigit(str[2]) && char.IsDigit(str[3]))
        {
            try
            {
                int year = int.Parse(str.Substring(0, 4));
                int month = int.Parse(str.Substring(4, 2));
                int day = int.Parse(str.Substring(6, 2));
                int hour = int.Parse(str.Substring(8, 2));
                int min = int.Parse(str.Substring(10, 2));
                int sec = int.Parse(str.Substring(12, 2));
                return new DateTime(year, month, day, hour, min, sec, DateTimeKind.Local);
            }
            catch { }
        }

        // 2. Standard DateTime parse
        if (DateTime.TryParse(str, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeLocal, out var parsed))
        {
            return parsed.Kind == DateTimeKind.Utc ? parsed.ToLocalTime() : parsed;
        }

        if (DateTime.TryParse(str, out var currentCultureParsed))
        {
            return currentCultureParsed.Kind == DateTimeKind.Utc ? currentCultureParsed.ToLocalTime() : currentCultureParsed;
        }

        return DateTime.Now;
    }

    private static RestorePointItem? ParseJsonRestorePoint(JsonElement elem)
    {
        try
        {
            long seq = 0;
            if (elem.TryGetProperty("SequenceNumber", out var seqProp))
            {
                if (seqProp.ValueKind == JsonValueKind.Number)
                    seq = seqProp.GetInt64();
                else
                    long.TryParse(seqProp.GetString(), out seq);
            }

            if (seq <= 0) return null;
            string desc = elem.TryGetProperty("Description", out var descProp) ? descProp.GetString() ?? "Restore Point" : "Restore Point";
            string rawType = elem.TryGetProperty("RestorePointType", out var typeProp) ? typeProp.ToString() : "12";
            string rawEvent = elem.TryGetProperty("EventType", out var eventProp) ? eventProp.ToString() : "100";

            DateTime created = DateTime.Now;
            if (elem.TryGetProperty("CreationTime", out var ctProp))
            {
                created = ParseCreationTime(ctProp.GetString());
            }

            string humanType = ResolveHumanReadableType(rawType, rawEvent, desc);
            return new RestorePointItem(seq, desc, created, humanType);
        }
        catch
        {
            return null;
        }
    }
}
