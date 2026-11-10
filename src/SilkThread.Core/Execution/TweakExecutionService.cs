using System;
using System.Text.RegularExpressions;

namespace SilkThread.Core.Execution;

public interface ITweakExecutionService
{
    event Action<string>? OperationLogged;
    void Log(string operation);
    void LogRegistry(string hive, string subKey, string valueName, object? value, string valueKind);
    void LogRegistryFailure(string hive, string subKey, string valueName, string reason);
    void LogService(string serviceName, string startupType, string state);
    void LogServiceFailure(string serviceName, string reason);
    void LogPowerShell(string command, int exitCode, string? output = null, string? error = null);
    void LogCommand(string fileName, string arguments, int exitCode, string? output = null, string? error = null);
}

public class TweakExecutionService : ITweakExecutionService
{
    private static readonly Lazy<TweakExecutionService> _lazyInstance = new(() => new TweakExecutionService());
    public static TweakExecutionService Instance => _lazyInstance.Value;

    public event Action<string>? OperationLogged;

    public void Log(string operation)
    {
        OperationLogged?.Invoke(operation);
    }

    public void LogRegistry(string hive, string subKey, string valueName, object? value, string valueKind)
    {
        string h = NormalizeHive(hive);
        string displayVal = value?.ToString() ?? "0";
        Log($"[REGISTRY] Set {h}:\\{subKey} -> {valueName} = {displayVal} ({valueKind})");
    }

    public void LogRegistryFailure(string hive, string subKey, string valueName, string reason)
    {
        string h = NormalizeHive(hive);
        Log($"[REGISTRY] ✗ FAILED: Set {h}:\\{subKey} -> {valueName} ({reason})");
    }

    public void LogService(string serviceName, string startupType, string state)
    {
        Log($"[SERVICE] {serviceName} -> Set StartupType = {startupType}, State = {state}");
    }

    public void LogServiceFailure(string serviceName, string reason)
    {
        Log($"[SERVICE] ✗ FAILED: {serviceName} ({reason})");
    }

    public void LogPowerShell(string command, int exitCode, string? output = null, string? error = null)
    {
        string cleanCmd = command;
        var m = Regex.Match(command, @"-Command\s+""?(.*?)""?$", RegexOptions.IgnoreCase);
        if (m.Success)
        {
            cleanCmd = m.Groups[1].Value.Trim().TrimEnd('"');
        }

        string line = $"[POWERSHELL] {cleanCmd} -> ExitCode: {exitCode}";
        if (exitCode != 0 && !string.IsNullOrWhiteSpace(error))
        {
            line += $" (Error: {error.Trim()})";
        }
        else if (!string.IsNullOrWhiteSpace(output) && output.Trim().Length > 0 && output.Trim().Length <= 80)
        {
            line += $" ({output.Trim()})";
        }
        Log(line);
    }

    public void LogCommand(string fileName, string arguments, int exitCode, string? output = null, string? error = null)
    {
        string line = $"[COMMAND] {fileName} {arguments} -> ExitCode: {exitCode}";
        if (exitCode != 0 && !string.IsNullOrWhiteSpace(error))
        {
            line += $" (Error: {error.Trim()})";
        }
        Log(line);
    }

    private static string NormalizeHive(string hive)
    {
        var h = hive.TrimEnd('\\', ':').ToUpperInvariant();
        return h switch
        {
            "HKEY_LOCAL_MACHINE" => "HKLM",
            "HKEY_CURRENT_USER" => "HKCU",
            "HKEY_CLASSES_ROOT" => "HKCR",
            "HKEY_USERS" => "HKU",
            _ => h
        };
    }
}
