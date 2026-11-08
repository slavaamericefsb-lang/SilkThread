using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using SilkThread.Core.SystemOperations;

namespace SilkThread.App.Services;

public static class FilePickerHelper
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct OPENFILENAME
    {
        public int lStructSize;
        public IntPtr hwndOwner;
        public IntPtr hInstance;
        public string lpstrFilter;
        public string lpstrCustomFilter;
        public int nMaxCustFilter;
        public int nFilterIndex;
        public IntPtr lpstrFile;
        public int nMaxFile;
        public string lpstrFileTitle;
        public int nMaxFileTitle;
        public string lpstrInitialDir;
        public string lpstrTitle;
        public int Flags;
        public short nFileOffset;
        public short nFileExtension;
        public string lpstrDefExt;
        public IntPtr lCustData;
        public IntPtr lpfnHook;
        public string lpTemplateName;
        public IntPtr pvReserved;
        public int dwReserved;
        public int FlagsEx;
    }

    private const int OFN_EXPLORER = 0x00080000;
    private const int OFN_FILEMUSTEXIST = 0x00001000;
    private const int OFN_PATHMUSTEXIST = 0x00000800;
    private const int OFN_OVERWRITEPROMPT = 0x00000002;
    private const int OFN_NOCHANGEDIR = 0x00000008;

    [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool GetOpenFileNameW(ref OPENFILENAME ofn);

    [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool GetSaveFileNameW(ref OPENFILENAME ofn);

    public static async Task<string?> PickOpenPresetFileAsync(nint? hwnd = null)
    {
        var windowHandle = hwnd ?? App.MainWindowHandle;

        if (!ElevationHelper.IsAdministrator())
        {
            try
            {
                var picker = new FileOpenPicker();
                picker.ViewMode = PickerViewMode.List;
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                picker.FileTypeFilter.Add(".silkpreset");
                picker.FileTypeFilter.Add(".json");

                if (windowHandle != 0)
                {
                    WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);
                }

                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    return file.Path;
                }
                return null;
            }
            catch (Exception ex)
            {
                App.Log($"[FilePickerHelper] WinRT OpenPicker failed: {ex.Message}. Falling back to Win32.");
            }
        }

        return ShowWin32OpenFileDialog(windowHandle);
    }

    public static async Task<string?> PickSavePresetFileAsync(string suggestedFileName = "MyTweaks.silkpreset", nint? hwnd = null)
    {
        var windowHandle = hwnd ?? App.MainWindowHandle;

        if (!ElevationHelper.IsAdministrator())
        {
            try
            {
                var picker = new FileSavePicker();
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                picker.FileTypeChoices.Add("SilkThread Preset (*.silkpreset)", new[] { ".silkpreset" });
                picker.DefaultFileExtension = ".silkpreset";
                picker.SuggestedFileName = suggestedFileName;

                if (windowHandle != 0)
                {
                    WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);
                }

                var file = await picker.PickSaveFileAsync();
                if (file != null)
                {
                    return file.Path;
                }
                return null;
            }
            catch (Exception ex)
            {
                App.Log($"[FilePickerHelper] WinRT SavePicker failed: {ex.Message}. Falling back to Win32.");
            }
        }

        return ShowWin32SaveFileDialog(suggestedFileName, windowHandle);
    }

    private static string? ShowWin32OpenFileDialog(IntPtr hwndOwner)
    {
        const int maxFile = 2048;
        IntPtr pFile = Marshal.StringToCoTaskMemUni(new string('\0', maxFile));
        try
        {
            var ofn = new OPENFILENAME();
            ofn.lStructSize = Marshal.SizeOf<OPENFILENAME>();
            ofn.hwndOwner = hwndOwner;
            ofn.lpstrFilter = "SilkThread Presets (*.silkpreset;*.json)\0*.silkpreset;*.json\0All Files (*.*)\0*.*\0\0";
            ofn.lpstrFile = pFile;
            ofn.nMaxFile = maxFile;
            ofn.lpstrTitle = "Выберите файл пресета SilkThread";
            ofn.Flags = OFN_EXPLORER | OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR;
            ofn.lpstrDefExt = "silkpreset";

            if (GetOpenFileNameW(ref ofn))
            {
                return Marshal.PtrToStringUni(pFile);
            }
            return null;
        }
        finally
        {
            Marshal.FreeCoTaskMem(pFile);
        }
    }

    private static string? ShowWin32SaveFileDialog(string defaultName, IntPtr hwndOwner)
    {
        const int maxFile = 2048;
        string initialBuf = defaultName.PadRight(maxFile, '\0');
        IntPtr pFile = Marshal.StringToCoTaskMemUni(initialBuf);
        try
        {
            var ofn = new OPENFILENAME();
            ofn.lStructSize = Marshal.SizeOf<OPENFILENAME>();
            ofn.hwndOwner = hwndOwner;
            ofn.lpstrFilter = "SilkThread Preset (*.silkpreset)\0*.silkpreset\0JSON Files (*.json)\0*.json\0All Files (*.*)\0*.*\0\0";
            ofn.lpstrFile = pFile;
            ofn.nMaxFile = maxFile;
            ofn.lpstrTitle = "Сохранить пресет SilkThread";
            ofn.Flags = OFN_EXPLORER | OFN_OVERWRITEPROMPT | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR;
            ofn.lpstrDefExt = "silkpreset";

            if (GetSaveFileNameW(ref ofn))
            {
                return Marshal.PtrToStringUni(pFile);
            }
            return null;
        }
        finally
        {
            Marshal.FreeCoTaskMem(pFile);
        }
    }
}
