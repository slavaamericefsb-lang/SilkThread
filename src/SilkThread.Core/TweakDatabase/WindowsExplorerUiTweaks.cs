using System.Collections.Generic;
using SilkThread.Core.Models;

namespace SilkThread.Core.TweakDatabase;

public static class WindowsExplorerUiTweaks
{
    public static IEnumerable<TweakDefinition> GetTweaks()
    {
        yield return TweakBuilder.CreateRegistryDword(
            id: "ui_show_file_extensions",
            title: "Show File Extensions for Known File Types",
            description: "Forces Windows Explorer to always display file extensions (.exe, .ini, .dll, .cfg).",
            howItWorks: "Sets 'HideFileExt' to 0 in HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced.",
            problemSolved: "Hiding extensions causes accidental execution of malicious files masked as icons and makes game modding confusing.",
            expectedResult: "Accurate file name identification and enhanced system security.",
            category: TweakCategory.WindowsExplorerAndUi,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
            valueName: "HideFileExt",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "ui_show_hidden_files",
            title: "Show Hidden Files, Folders, and Drives",
            description: "Makes hidden files and system app directories visible in File Explorer.",
            howItWorks: "Sets 'Hidden' to 1 in HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced.",
            problemSolved: "Critical game configuration and save files stored in %LocalAppData% and AppData are hidden by default.",
            expectedResult: "Instant access to game save directories and configuration files.",
            category: TweakCategory.WindowsExplorerAndUi,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
            valueName: "Hidden",
            appliedValue: 1,
            fallbackDefaultValue: 2
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "ui_separate_process_folders",
            title: "Launch Folder Windows in a Separate Process",
            description: "Runs individual File Explorer windows in isolated process memory space.",
            howItWorks: "Sets 'SeparateProcess' to 1 in HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced.",
            problemSolved: "If an explorer window crashes or hangs on a network folder or corrupt video, it no longer crashes the desktop taskbar.",
            expectedResult: "Bulletproof desktop stability and isolated explorer crashes.",
            category: TweakCategory.WindowsExplorerAndUi,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
            valueName: "SeparateProcess",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "ui_open_explorer_to_this_pc",
            title: "Open File Explorer to 'This PC' by Default",
            description: "Opens File Explorer directly to disk drives and hardware volumes rather than Quick Access cloud folders.",
            howItWorks: "Sets 'LaunchTo' to 1 in HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced.",
            problemSolved: "Quick Access hangs for seconds attempting to enumerate disconnected network shares or recent cloud files.",
            expectedResult: "Instantaneous File Explorer launch directly showing drives C:, D:, etc.",
            category: TweakCategory.WindowsExplorerAndUi,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
            valueName: "LaunchTo",
            appliedValue: 1,
            fallbackDefaultValue: 2
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "ui_compact_view_explorer",
            title: "Enable Compact Spacing Mode in File Explorer",
            description: "Reduces excessive touch padding between files and folders in Windows 11 File Explorer.",
            howItWorks: "Sets 'UseCompactMode' to 1 in HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced.",
            problemSolved: "Windows 11 defaults to huge touch-friendly padding, reducing visible files on screen by 50%.",
            expectedResult: "Clean, high-density desktop file lists ideal for PC mouse users.",
            category: TweakCategory.WindowsExplorerAndUi,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
            valueName: "UseCompactMode",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryString(
            id: "ui_classic_context_menu",
            title: "Restore Classic Full Right-Click Context Menu in Windows 11",
            description: "Restores the comprehensive classic context menu, bypassing the slow 'Show more options' secondary click.",
            howItWorks: "Registers {86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\\InprocServer32 override in HKCU\\Software\\Classes\\CLSID with an empty default string.",
            problemSolved: "Windows 11 forces a two-click process with slow animation to access WinRAR, 7-Zip, graphics control panels, and mod tools.",
            expectedResult: "Instant, one-click access to all desktop context menu utilities.",
            category: TweakCategory.WindowsExplorerAndUi,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32",
            valueName: "",
            appliedValue: "",
            fallbackDefaultValue: null,
            minWindowsBuild: 22000
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "ui_disable_recent_files_quick_access",
            title: "Disable Recent Files Tracking in Quick Access",
            description: "Stops File Explorer from maintaining and displaying a list of recently opened files.",
            howItWorks: "Sets 'ShowRecent' to 0 in HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer.",
            problemSolved: "Constantly updates metadata MRU lists in the registry, causing disk churn and revealing private files.",
            expectedResult: "Clean Quick Access sidebar and improved file privacy.",
            category: TweakCategory.WindowsExplorerAndUi,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Windows\CurrentVersion\Explorer",
            valueName: "ShowRecent",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "ui_disable_thumbnail_cache_clearing",
            title: "Prevent Automatic Thumbnail Cache Purging",
            description: "Prevents Windows Disk Cleanup from wiping out the thumbnail cache on reboots.",
            howItWorks: "Sets 'Autorun' to 0 in HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VolumeCaches\\Thumbnail Cleaner.",
            problemSolved: "Wiping thumbnails forces Windows to re-render thousands of image/video icons on every folder visit, pegging CPU.",
            expectedResult: "Smooth folder opening with permanently cached media thumbnails.",
            category: TweakCategory.WindowsExplorerAndUi,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Thumbnail Cleaner",
            valueName: "Autorun",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "ui_show_super_hidden_system_files",
            title: "Show Protected Operating System Files (Advanced)",
            description: "Unhides critical hidden protected operating system files for advanced troubleshooting.",
            howItWorks: "Sets 'ShowSuperHidden' to 1 in HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced.",
            problemSolved: "Allows viewing desktop.ini, pagefile.sys, and game licensing files directly.",
            expectedResult: "Full visibility of all system-protected files.",
            category: TweakCategory.WindowsExplorerAndUi,
            risk: RiskLevel.Medium,
            recommendation: RecommendationLevel.Optional,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
            valueName: "ShowSuperHidden",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "ui_show_drive_letters_first",
            title: "Show Drive Letters Before Drive Labels",
            description: "Displays drive letters ahead of volume labels (e.g. 'C: (NVMe_Games)' instead of 'NVMe_Games (C:)').",
            howItWorks: "Sets 'ShowDriveLettersFirst' to 4 in HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer.",
            problemSolved: "Allows rapid keyboard navigation and instant drive letter recognition.",
            expectedResult: "Clear and organized drive listings in File Explorer.",
            category: TweakCategory.WindowsExplorerAndUi,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Windows\CurrentVersion\Explorer",
            valueName: "ShowDriveLettersFirst",
            appliedValue: 4,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "ui_disable_sharing_wizard",
            title: "Disable Network Sharing Wizard in Explorer",
            description: "Restores the direct security permissions tab instead of the simplified wizard.",
            howItWorks: "Sets 'SharingWizardOn' to 0 in HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced.",
            problemSolved: "Sharing wizard obscures actual folder NTFS permissions required when hosting local game dedicated servers.",
            expectedResult: "Direct access to advanced folder permissions and security properties.",
            category: TweakCategory.WindowsExplorerAndUi,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
            valueName: "SharingWizardOn",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryString(
            id: "ui_disable_folder_type_auto_discovery",
            title: "Disable Folder Template Auto-Discovery Sniffing",
            description: "Prevents File Explorer from scanning every file in large folders to guess whether it is a music/video/picture folder.",
            howItWorks: "Sets 'FolderType' default override to 'NotSpecified' in HKCU\\Software\\Classes\\Local Settings\\Software\\Microsoft\\Windows\\Shell\\Bags\\AllFolders\\Shell.",
            problemSolved: "Eliminates huge green loading bar freezes when opening large game, mod, or download folders with thousands of files.",
            expectedResult: "Instant folder browsing without directory content parsing delays.",
            category: TweakCategory.WindowsExplorerAndUi,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\Bags\AllFolders\Shell",
            valueName: "FolderType",
            appliedValue: "NotSpecified"
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "ui_taskbar_search_icon_only",
            title: "Minimize Windows 11 Taskbar Search Box to Clean Icon",
            description: "Replaces the wide search box with a compact search icon on the taskbar to maximize taskbar space.",
            howItWorks: "Sets 'SearchboxTaskbarMode' to 1 in HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Search.",
            problemSolved: "The large search box wastes valuable taskbar real estate and accidental clicks open heavy Bing overlays.",
            expectedResult: "More taskbar room for running game and app windows.",
            category: TweakCategory.WindowsExplorerAndUi,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Windows\CurrentVersion\Search",
            valueName: "SearchboxTaskbarMode",
            appliedValue: 1,
            fallbackDefaultValue: 2
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "ui_disable_shortcut_suffix",
            title: "Disable ' - Shortcut' Suffix on Created Links",
            description: "Stops Windows from appending ' - Shortcut' text to newly created desktop and folder shortcuts.",
            howItWorks: "Sets 'link' to 0 in HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer.",
            problemSolved: "Creates clean shortcut names directly matching original game executables without manual renaming.",
            expectedResult: "Clean desktop shortcut naming without redundant suffixes.",
            category: TweakCategory.WindowsExplorerAndUi,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Windows\CurrentVersion\Explorer",
            valueName: "link",
            appliedValue: 0,
            fallbackDefaultValue: 26
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "ui_taskbar_combine_behavior",
            title: "Configure Taskbar Button Grouping Mode to When Full",
            description: "Configures Windows 11 taskbar to keep window labels visible and only combine when the taskbar is full.",
            howItWorks: "Sets 'TaskbarGlomLevel' to 1 in HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced.",
            problemSolved: "Windows 11 combines all windows of the same app into a single icon, requiring multiple clicks to switch.",
            expectedResult: "Fast single-click window switching with clearly visible window titles.",
            category: TweakCategory.WindowsExplorerAndUi,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
            valueName: "TaskbarGlomLevel",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "ui_disable_navigation_pane_expand_open",
            title: "Disable Navigation Pane Auto-Expand to Current Folder",
            description: "Prevents the left navigation sidebar in File Explorer from frantically expanding every folder tree level.",
            howItWorks: "Sets 'NavPaneExpandToCurrentFolder' to 0 in HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced.",
            problemSolved: "Expanding huge folder trees on every navigation causes UI stutter and tree scroll jumps.",
            expectedResult: "Smooth, stable navigation pane behavior without unexpected tree jumps.",
            category: TweakCategory.WindowsExplorerAndUi,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
            valueName: "NavPaneExpandToCurrentFolder",
            appliedValue: 0,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "ui_restore_classic_ribbon_file_transfer",
            title: "Show Full Details by Default in File Transfer Dialogs",
            description: "Forces file copy, move, and extraction progress dialogs to always show the expanded speed graph and details.",
            howItWorks: "Sets 'ShowSpeedGraph' to 1 in HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\OperationStatusManager.",
            problemSolved: "Eliminates the extra click needed on 'More details' every time you copy game archives or patches.",
            expectedResult: "Immediate real-time visibility into disk transfer speeds and ETA.",
            category: TweakCategory.WindowsExplorerAndUi,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Software\Microsoft\Windows\CurrentVersion\Explorer\OperationStatusManager",
            valueName: "ShowSpeedGraph",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );
    }
}
