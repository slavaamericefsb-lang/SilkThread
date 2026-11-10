using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using Microsoft.Win32;

namespace SilkThread.Installer;

public static class InstallEngine
{
    public const string AppName = "SilkThread";
    public const string AppDisplayName = "SilkThread — Windows Gaming Optimizer";
    public const string AppVersion = "1.2.0";
    public const string AppPublisher = "SilkThread";
    public const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\SilkThread";

    public static string GetDefaultInstallDir()
    {
        string progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        return Path.Combine(progFiles, AppName);
    }

    public static void ExtractPayload(string targetDirectory, Action<int, string>? progressCallback)
    {
        Directory.CreateDirectory(targetDirectory);
        var assembly = Assembly.GetExecutingAssembly();

        using var stream = assembly.GetManifestResourceStream("SilkThread.Installer.Payload.installer_payload.zip");
        if (stream == null)
        {
            throw new InvalidOperationException("Embedded installer payload not found!");
        }

        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        int totalEntries = archive.Entries.Count;
        int current = 0;

        foreach (var entry in archive.Entries)
        {
            current++;
            int percentage = (int)((current / (double)totalEntries) * 85);
            progressCallback?.Invoke(percentage, entry.FullName);

            string fullTargetDir = Path.GetFullPath(targetDirectory);
            if (!fullTargetDir.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                fullTargetDir += Path.DirectorySeparatorChar;
            }

            string destinationPath = Path.GetFullPath(Path.Combine(fullTargetDir, entry.FullName));
            if (!destinationPath.StartsWith(fullTargetDir, StringComparison.OrdinalIgnoreCase))
            {
                continue; // Prevent Zip Slip traversal attack
            }

            if (string.IsNullOrEmpty(entry.Name))
            {
                Directory.CreateDirectory(destinationPath);
            }
            else
            {
                string? parentDir = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(parentDir))
                {
                    Directory.CreateDirectory(parentDir);
                }

                entry.ExtractToFile(destinationPath, overwrite: true);
            }
        }
    }

    public static void CreateShortcuts(string targetDirectory, bool createDesktop, bool createStartMenu)
    {
        string exePath = Path.Combine(targetDirectory, "SilkThread.App.exe");
        string iconPath = Path.Combine(targetDirectory, "Assets", "AppIcon.ico");
        if (!File.Exists(iconPath))
        {
            iconPath = exePath;
        }

        Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType == null) return;

        dynamic? shell = Activator.CreateInstance(shellType);
        if (shell == null) return;

        if (createDesktop)
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string lnkPath = Path.Combine(desktop, "SilkThread.lnk");
                dynamic shortcut = shell.CreateShortcut(lnkPath);
                shortcut.TargetPath = exePath;
                shortcut.WorkingDirectory = targetDirectory;
                shortcut.IconLocation = iconPath;
                shortcut.Description = AppDisplayName;
                shortcut.Save();
            }
            catch { }
        }

        if (createStartMenu)
        {
            try
            {
                string programs = Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms);
                string silkDir = Path.Combine(programs, "SilkThread");
                Directory.CreateDirectory(silkDir);

                string lnkPath = Path.Combine(silkDir, "SilkThread.lnk");
                dynamic shortcut = shell.CreateShortcut(lnkPath);
                shortcut.TargetPath = exePath;
                shortcut.WorkingDirectory = targetDirectory;
                shortcut.IconLocation = iconPath;
                shortcut.Description = AppDisplayName;
                shortcut.Save();
            }
            catch { }
        }
    }

    public static void CreateUninstaller(string targetDirectory)
    {
        try
        {
            string currentExe = Environment.ProcessPath ?? "";
            if (File.Exists(currentExe))
            {
                string uninstallExe = Path.Combine(targetDirectory, "Uninstall.exe");
                File.Copy(currentExe, uninstallExe, overwrite: true);
            }
        }
        catch { }
    }

    public static void RegisterInWindows(string targetDirectory)
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(RegistryKeyPath, writable: true);
            if (key != null)
            {
                string exePath = Path.Combine(targetDirectory, "SilkThread.App.exe");
                string uninstallerPath = Path.Combine(targetDirectory, "Uninstall.exe");
                string iconPath = Path.Combine(targetDirectory, "Assets", "AppIcon.ico");

                key.SetValue("DisplayName", AppDisplayName);
                key.SetValue("DisplayVersion", AppVersion);
                key.SetValue("Publisher", AppPublisher);
                key.SetValue("InstallLocation", targetDirectory);
                key.SetValue("DisplayIcon", iconPath);
                key.SetValue("UninstallString", $"\"{uninstallerPath}\" /uninstall");
                key.SetValue("QuietUninstallString", $"\"{uninstallerPath}\" /uninstall /silent");
                key.SetValue("EstimatedSize", 165000, RegistryValueKind.DWord);
                key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            }
        }
        catch { }
    }

    public static void UnregisterFromWindows()
    {
        try
        {
            Registry.LocalMachine.DeleteSubKeyTree(RegistryKeyPath, throwOnMissingSubKey: false);
        }
        catch { }
    }

    public static void RemoveShortcuts()
    {
        try
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string lnkDesktop = Path.Combine(desktop, "SilkThread.lnk");
            if (File.Exists(lnkDesktop)) File.Delete(lnkDesktop);

            string programs = Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms);
            string silkDir = Path.Combine(programs, "SilkThread");
            if (Directory.Exists(silkDir)) Directory.Delete(silkDir, recursive: true);
        }
        catch { }
    }
}













