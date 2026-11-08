using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Threading.Tasks;

namespace SilkThread.Core.SystemOperations;

public static class ElevationHelper
{
    public static bool IsAdministrator()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    public static bool RestartAsAdmin(string? args = null)
    {
        try
        {
            var exeName = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exeName)) return false;

            var startInfo = new ProcessStartInfo
            {
                FileName = exeName,
                Arguments = args ?? string.Empty,
                UseShellExecute = true,
                Verb = "runas"
            };

            Process.Start(startInfo);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public static class CommandOperations
{
    public static async Task<(int ExitCode, string Output, string Error)> RunAsync(string fileName, string arguments, int timeoutMs = 15000)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            var waitForExitTask = process.WaitForExitAsync();
            var completedTask = await Task.WhenAny(waitForExitTask, Task.Delay(timeoutMs));

            if (completedTask != waitForExitTask)
            {
                try { process.Kill(); } catch { }
                return (-1, string.Empty, "Process timed out");
            }

            string stdout = await stdoutTask;
            string stderr = await stderrTask;

            return (process.ExitCode, stdout, stderr);
        }
        catch (Exception ex)
        {
            return (-1, string.Empty, ex.Message);
        }
    }
}
