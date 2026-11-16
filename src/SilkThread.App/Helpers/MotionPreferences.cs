using Windows.UI.ViewManagement;

namespace SilkThread.App.Helpers;

/// <summary>Shared motion rhythm; custom effects follow the Windows accessibility setting.</summary>
internal static class MotionPreferences
{
    public const int HoverMilliseconds = 140;
    public const int EntranceMilliseconds = 180;
    public const int ExitMilliseconds = 90;
    public const int ProgressMilliseconds = 320;
    private static readonly UISettings Settings = new();
    public static bool AnimationsEnabled => Settings.AnimationsEnabled;
}
