using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;

namespace SilkThread.App.Helpers;

/// <summary>Enforces the outer window's minimum tracking size in physical pixels.</summary>
internal sealed class WindowSizeConstraint
{
    private readonly nint _handle;
    private readonly SubclassProc _callback;
    private readonly int _width, _height;

    public WindowSizeConstraint(Window window, int width, int height)
    {
        _handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
        _width = width;
        _height = height;
        _callback = WindowProc;
        if (!SetWindowSubclass(_handle, _callback, 1, 0))
            throw new System.ComponentModel.Win32Exception();
        window.Closed += (_, _) => RemoveWindowSubclass(_handle, _callback, 1);
    }

    private nint WindowProc(nint hwnd, uint message, nuint wParam, nint lParam, nuint id, nuint data)
    {
        var result = DefSubclassProc(hwnd, message, wParam, lParam);
        if (message == 0x0024) // WM_GETMINMAXINFO; preserve the system's monitor/maximize constraints.
        {
            var limits = Marshal.PtrToStructure<MinMaxInfo>(lParam);
            limits.MinTrackSize.X = Math.Max(limits.MinTrackSize.X, _width);
            limits.MinTrackSize.Y = Math.Max(limits.MinTrackSize.Y, _height);
            Marshal.StructureToPtr(limits, lParam, false);
        }
        return result;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo { public Point Reserved, MaxSize, MaxPosition, MinTrackSize, MaxTrackSize; }
    private delegate nint SubclassProc(nint hwnd, uint message, nuint wParam, nint lParam, nuint id, nuint data);
    [DllImport("comctl32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowSubclass(nint hwnd, SubclassProc callback, nuint id, nuint data);
    [DllImport("comctl32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RemoveWindowSubclass(nint hwnd, SubclassProc callback, nuint id);
    [DllImport("comctl32.dll")]
    private static extern nint DefSubclassProc(nint hwnd, uint message, nuint wParam, nint lParam);
}
