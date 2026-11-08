using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace SilkThread.Installer;

public partial class App : Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        string[] args = e.Args;
        bool isSilent = args.Any(a => a.Equals("/S", StringComparison.OrdinalIgnoreCase) || 
                                     a.Equals("/silent", StringComparison.OrdinalIgnoreCase) ||
                                     a.Equals("-quiet", StringComparison.OrdinalIgnoreCase));

        string processName = Path.GetFileNameWithoutExtension(Environment.ProcessPath ?? "");
        bool isUninstall = args.Any(a => a.Equals("/uninstall", StringComparison.OrdinalIgnoreCase)) ||
                           processName.Equals("Uninstall", StringComparison.OrdinalIgnoreCase);

        if (isSilent)
        {
            if (isUninstall)
            {
                InstallEngine.RemoveShortcuts();
                InstallEngine.UnregisterFromWindows();
            }
            else
            {
                string targetDir = InstallEngine.GetDefaultInstallDir();
                // Check if custom /D= was provided
                var dirArg = args.FirstOrDefault(a => a.StartsWith("/D=", StringComparison.OrdinalIgnoreCase));
                if (dirArg != null)
                {
                    targetDir = dirArg.Substring(3).Trim('\"');
                }

                InstallEngine.ExtractPayload(targetDir, null);
                InstallEngine.CreateShortcuts(targetDir, true, true);
                InstallEngine.CreateUninstaller(targetDir);
                InstallEngine.RegisterInWindows(targetDir);
            }

            Shutdown(0);
            return;
        }

        var mainWindow = new MainWindow(isUninstall);
        mainWindow.Show();
    }
}
