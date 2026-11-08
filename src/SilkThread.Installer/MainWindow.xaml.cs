using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace SilkThread.Installer;

public partial class MainWindow : Window
{
    private string _currentLang = "ru";
    private readonly bool _isUninstallMode;
    private string _targetDirectory;

    public MainWindow(bool isUninstallMode = false)
    {
        InitializeComponent();
        _isUninstallMode = isUninstallMode;
        _targetDirectory = InstallEngine.GetDefaultInstallDir();
        TxtInstallPath.Text = _targetDirectory;

        if (_isUninstallMode)
        {
            PanelWelcome.Visibility = Visibility.Collapsed;
            PanelProgress.Visibility = Visibility.Collapsed;
            PanelFinished.Visibility = Visibility.Collapsed;
            PanelUninstall.Visibility = Visibility.Visible;
            TitleText.Text = "SilkThread 2.0 — Удаление";
        }
        else
        {
            UpdateLanguage();
        }
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    private void BtnMinimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnLang_Click(object sender, RoutedEventArgs e)
    {
        _currentLang = _currentLang == "ru" ? "en" : "ru";
        UpdateLanguage();
    }

    private void UpdateLanguage()
    {
        bool isRu = _currentLang == "ru";
        BtnLang.Content = isRu ? "RU / EN" : "EN / RU";

        if (!_isUninstallMode)
        {
            TitleText.Text = isRu ? "SilkThread 2.0 — Установка" : "SilkThread 2.0 — Setup";
            LblHeroTitle.Text = isRu ? "Добро пожаловать в SilkThread 2.0" : "Welcome to SilkThread 2.0";
            LblHeroDesc.Text = isRu 
                ? "Профессиональный оптимизатор Windows для максимального FPS и минимального Input Lag." 
                : "Professional Windows gaming optimizer for maximum FPS and minimal latency.";
            LblInstallDir.Text = isRu ? "Папка установки:" : "Installation directory:";
            ChkDesktop.Content = isRu ? "Создать ярлык на Рабочем столе" : "Create Desktop shortcut";
            ChkStartMenu.Content = isRu ? "Создать ярлык в меню Пуск" : "Create Start Menu shortcut";
            ChkLaunchAfter.Content = isRu ? "Запустить SilkThread после завершения установки" : "Launch SilkThread upon completion";
            LblSpaceInfo.Text = isRu ? "Требуемое место на диске: ~165 МБ" : "Required disk space: ~165 MB";
            LblProgressTitle.Text = isRu ? "Установка SilkThread 2.0..." : "Installing SilkThread 2.0...";
            LblDoneTitle.Text = isRu ? "SilkThread успешно установлен!" : "SilkThread installed successfully!";
            LblDoneDesc.Text = isRu 
                ? "Программа готова к оптимизации компонентов Windows и запуску игр." 
                : "SilkThread is ready to optimize Windows components and launch games.";
        }
        else
        {
            TitleText.Text = isRu ? "SilkThread 2.0 — Удаление" : "SilkThread 2.0 — Uninstall";
            LblUninstallTitle.Text = isRu ? "Удаление SilkThread 2.0" : "Uninstall SilkThread 2.0";
            LblUninstallDesc.Text = isRu 
                ? "Вы действительно хотите удалить SilkThread, все его компоненты и ярлыки?" 
                : "Are you sure you want to completely remove SilkThread, all components, and shortcuts?";
        }
    }

    private void BtnBrowse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = _currentLang == "ru" ? "Выберите папку для установки SilkThread" : "Select Installation Folder",
            InitialDirectory = _targetDirectory
        };

        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.FolderName))
        {
            _targetDirectory = dialog.FolderName;
            TxtInstallPath.Text = _targetDirectory;
        }
    }

    private async void BtnInstall_Click(object sender, RoutedEventArgs e)
    {
        _targetDirectory = TxtInstallPath.Text.Trim();
        if (string.IsNullOrWhiteSpace(_targetDirectory))
        {
            _targetDirectory = InstallEngine.GetDefaultInstallDir();
        }

        PanelWelcome.Visibility = Visibility.Collapsed;
        PanelProgress.Visibility = Visibility.Visible;
        ProgInstall.Value = 5;
        LblProgressStatus.Text = _currentLang == "ru" ? "Распаковка компонентов приложения..." : "Extracting application components...";

        bool createDesktop = ChkDesktop.IsChecked == true;
        bool createStartMenu = ChkStartMenu.IsChecked == true;
        bool launchAfter = ChkLaunchAfter.IsChecked == true;

        try
        {
            await Task.Run(() =>
            {
                InstallEngine.ExtractPayload(_targetDirectory, (pct, file) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        ProgInstall.Value = pct;
                        LblProgressDetail.Text = Path.GetFileName(file);
                    });
                });

                Dispatcher.Invoke(() =>
                {
                    ProgInstall.Value = 90;
                    LblProgressStatus.Text = _currentLang == "ru" ? "Создание ярлыков..." : "Creating shortcuts...";
                });

                InstallEngine.CreateShortcuts(_targetDirectory, createDesktop, createStartMenu);

                Dispatcher.Invoke(() =>
                {
                    ProgInstall.Value = 95;
                    LblProgressStatus.Text = _currentLang == "ru" ? "Регистрация приложения в Windows..." : "Registering in Windows...";
                });

                InstallEngine.CreateUninstaller(_targetDirectory);
                InstallEngine.RegisterInWindows(_targetDirectory);

                Dispatcher.Invoke(() =>
                {
                    ProgInstall.Value = 100;
                });
            });

            await Task.Delay(400);
            PanelProgress.Visibility = Visibility.Collapsed;
            PanelFinished.Visibility = Visibility.Visible;

            if (launchAfter)
            {
                LaunchSilkThread();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                (_currentLang == "ru" ? "Ошибка установки: " : "Installation error: ") + ex.Message,
                "SilkThread Setup",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            PanelProgress.Visibility = Visibility.Collapsed;
            PanelWelcome.Visibility = Visibility.Visible;
        }
    }

    private void BtnFinish_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnLaunchApp_Click(object sender, RoutedEventArgs e)
    {
        LaunchSilkThread();
        Close();
    }

    private void LaunchSilkThread()
    {
        try
        {
            string exePath = Path.Combine(_targetDirectory, "SilkThread.App.exe");
            if (File.Exists(exePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = _targetDirectory,
                    UseShellExecute = true
                });
            }
        }
        catch { }
    }

    private void BtnDoUninstall_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            InstallEngine.RemoveShortcuts();
            InstallEngine.UnregisterFromWindows();

            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            // Schedule deletion of directory after exit
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c ping 127.0.0.1 -n 2 > nul & rmdir /s /q \"{appDir}\"",
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            });

            MessageBox.Show(
                _currentLang == "ru" ? "SilkThread успешно удален." : "SilkThread has been uninstalled successfully.",
                "SilkThread",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
