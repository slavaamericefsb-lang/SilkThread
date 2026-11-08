using System.Collections.Generic;
using SilkThread.Core.Models;

namespace SilkThread.Core.TweakDatabase;

public static class InputLatencyTweaks
{
    public static IEnumerable<TweakDefinition> GetTweaks()
    {
        yield return TweakBuilder.CreateMultiRegistry(
            id: "input_mouse_acceleration_disable",
            title: "Disable Windows Mouse Acceleration (Enhance Pointer Precision)",
            description: "Completely disables non-linear OS pointer acceleration for pure 1:1 raw hardware mouse movement.",
            howItWorks: "Sets MouseSpeed = 0, MouseThreshold1 = 0, and MouseThreshold2 = 0 in HKCU\\Control Panel\\Mouse.",
            problemSolved: "Windows dynamically alters cursor travel distance based on swipe speed, destroying muscle memory and aim consistency.",
            expectedResult: "Flawless, deterministic 1:1 mouse movement translation in desktop and game menus.",
            category: TweakCategory.InputAndLatency,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            dwords: new[]
            {
                ("HKCU", @"Control Panel\Mouse", "MouseSpeed", 0),
                ("HKCU", @"Control Panel\Mouse", "MouseThreshold1", 0),
                ("HKCU", @"Control Panel\Mouse", "MouseThreshold2", 0)
            }
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "input_keyboard_repeat_rate_max",
            title: "Maximize Keyboard Character Repeat Rate",
            description: "Sets the keyboard autorepeat speed to maximum hardware limit (30 characters per second).",
            howItWorks: "Sets 'KeyboardSpeed' to 31 in HKCU\\Control Panel\\Keyboard.",
            problemSolved: "Default rate causes sluggish repeated keystrokes when holding down movement or weapon switch keys.",
            expectedResult: "Instant, rapid response when holding down keys for continuous in-game actions.",
            category: TweakCategory.InputAndLatency,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Control Panel\Keyboard",
            valueName: "KeyboardSpeed",
            appliedValue: 31,
            fallbackDefaultValue: 31
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "input_keyboard_repeat_delay_min",
            title: "Minimize Keyboard Repeat Initial Delay",
            description: "Reduces the latency delay before key autorepeat engages to the minimum threshold (250ms).",
            howItWorks: "Sets 'KeyboardDelay' to 0 in HKCU\\Control Panel\\Keyboard.",
            problemSolved: "Eliminates hesitation between pressing a key and repeated action triggering.",
            expectedResult: "Fastest possible reaction for held-key mechanics (strafe tapping, continuous fire).",
            category: TweakCategory.InputAndLatency,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Control Panel\Keyboard",
            valueName: "KeyboardDelay",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "input_sticky_keys_prompt_disable",
            title: "Disable StickyKeys 5x Shift Trigger Popup",
            description: "Completely disables the popup dialog and beep triggered when tapping Shift 5 times in rapid succession.",
            howItWorks: "Sets 'Flags' to 506 in HKCU\\Control Panel\\Accessibility\\StickyKeys.",
            problemSolved: "Pressing Shift repeatedly in combat or sprinting minimizes the game with a modal OS prompt.",
            expectedResult: "Prevents accidental game interruptions and lost matches.",
            category: TweakCategory.InputAndLatency,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Control Panel\Accessibility\StickyKeys",
            valueName: "Flags",
            appliedValue: 506,
            fallbackDefaultValue: 510
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "input_toggle_keys_prompt_disable",
            title: "Disable ToggleKeys NumLock Beep & Prompt",
            description: "Disables the ToggleKeys audio cue and holding shortcut when pressing NumLock/CapsLock for 5 seconds.",
            howItWorks: "Sets 'Flags' to 58 in HKCU\\Control Panel\\Accessibility\\ToggleKeys.",
            problemSolved: "Prevents annoying OS audio beeps and key intercepts while using number pad keys in games.",
            expectedResult: "Silent and uninhibited toggle key operation.",
            category: TweakCategory.InputAndLatency,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Control Panel\Accessibility\ToggleKeys",
            valueName: "Flags",
            appliedValue: 58,
            fallbackDefaultValue: 62
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "input_filter_keys_disable",
            title: "Disable FilterKeys Key-Drop Mechanism",
            description: "Disables FilterKeys latency delays which ignore repeated or brief keystrokes.",
            howItWorks: "Sets 'Flags' to 122 in HKCU\\Control Panel\\Accessibility\\Keyboard Response.",
            problemSolved: "FilterKeys can drop brief micro-taps essential for competitive counter-strafing.",
            expectedResult: "Guaranteed registration of every quick key press.",
            category: TweakCategory.InputAndLatency,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Control Panel\Accessibility\Keyboard Response",
            valueName: "Flags",
            appliedValue: 122,
            fallbackDefaultValue: 126
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "input_usb_selective_suspend_disable",
            title: "Disable USB Selective Suspend for Input Devices",
            description: "Forces USB root hubs and controllers to never put USB mice, keyboards, or DACs into low-power sleep states.",
            howItWorks: "Sets 'DisableSelectiveSuspend' to 1 in HKLM\\SYSTEM\\CurrentControlSet\\Services\\USB.",
            problemSolved: "Prevents input latency spikes, missed clicks, and USB re-enumeration delays when moving mouse after brief pauses.",
            expectedResult: "Immediate, uninterrupted 1000Hz+ USB polling responsiveness.",
            category: TweakCategory.InputAndLatency,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Services\USB",
            valueName: "DisableSelectiveSuspend",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "input_cursor_shadow_disable",
            title: "Disable Mouse Cursor Drop Shadow",
            description: "Disables the alpha-blended drop shadow rendered underneath the mouse cursor.",
            howItWorks: "Sets 'CursorShadow' to 0 in HKCU\\Control Panel\\Desktop.",
            problemSolved: "Software cursor shadows require an extra GDI/DWM compositing step on every mouse movement tick.",
            expectedResult: "Reduces DWM compositing overhead during rapid mouse flicks.",
            category: TweakCategory.InputAndLatency,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Control Panel\Desktop",
            valueName: "CursorShadow",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "input_touch_prediction_latency",
            title: "Disable Pen and Touch Feedback Rings",
            description: "Disables the animated splash rings and hover circles for pen and touch digitizers.",
            howItWorks: "Sets 'ContactVisualization' to 0 in HKCU\\Control Panel\\Cursors.",
            problemSolved: "Touch prediction heuristics hook into the Windows message loop and delay pointer delivery.",
            expectedResult: "Clean and unencumbered pointer event dispatching.",
            category: TweakCategory.InputAndLatency,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Control Panel\Cursors",
            valueName: "ContactVisualization",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "input_mouse_hover_time_decrease",
            title: "Reduce Mouse Hover Time to 10ms",
            description: "Shortens the time the pointer must pause over a control before generating a hover event.",
            howItWorks: "Sets 'MouseHoverTime' to 10 in HKCU\\Control Panel\\Mouse.",
            problemSolved: "Default 400ms hover delay makes inventory tooltips and interactive UI elements feel unresponsive.",
            expectedResult: "Rapid tooltip display and fast inventory navigation.",
            category: TweakCategory.InputAndLatency,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Control Panel\Mouse",
            valueName: "MouseHoverTime",
            appliedValue: 10,
            fallbackDefaultValue: 400
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "input_double_click_time_standard",
            title: "Optimize Double-Click Detection Window",
            description: "Calibrates the maximum interval between clicks for double-click recognition to 450ms.",
            howItWorks: "Sets 'DoubleClickSpeed' to 450 in HKCU\\Control Panel\\Mouse.",
            problemSolved: "Overly long intervals register fast consecutive single-clicks (e.g. semi-auto gun taps) as unintended double-clicks.",
            expectedResult: "Accurate single-tap weapon burst registration in competitive shooters.",
            category: TweakCategory.InputAndLatency,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Control Panel\Mouse",
            valueName: "DoubleClickSpeed",
            appliedValue: 450,
            fallbackDefaultValue: 500
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "input_hid_driver_polling_priority",
            title: "Elevate Human Interface Device (HID) Driver Thread Priority",
            description: "Configures Windows kernel HID mouse class driver to process raw input events at elevated kernel priority.",
            howItWorks: "Sets 'ThreadPriority' to 31 in HKLM\\SYSTEM\\CurrentControlSet\\Services\\mouclass\\Parameters.",
            problemSolved: "Prevents mouse packet drops and micro-jitters during 100% CPU spikes in demanding game engines.",
            expectedResult: "Rock-solid 1000Hz, 4000Hz, and 8000Hz gaming mouse tracking smoothness.",
            category: TweakCategory.InputAndLatency,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Services\mouclass\Parameters",
            valueName: "ThreadPriority",
            appliedValue: 31,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "input_keyboard_data_queue_size",
            title: "Expand Keyboard Input Buffer Queue Size",
            description: "Increases the keyboard class driver raw buffer queue from 100 to 200 packets.",
            howItWorks: "Sets 'KeyboardDataQueueSize' to 200 in HKLM\\SYSTEM\\CurrentControlSet\\Services\\kbdclass\\Parameters.",
            problemSolved: "Eliminates dropped key inputs during extreme APM (Actions Per Minute) gameplay in RTS, rhythm, or fighting games.",
            expectedResult: "Zero dropped keystrokes during intensive simultaneous input bursts.",
            category: TweakCategory.InputAndLatency,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Services\kbdclass\Parameters",
            valueName: "KeyboardDataQueueSize",
            appliedValue: 200,
            fallbackDefaultValue: 100,
            requiresReboot: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "input_mouse_data_queue_size",
            title: "Expand Mouse Class Driver Buffer Queue Size",
            description: "Increases mouse packet buffer queue from 100 to 200 packets in mouclass driver.",
            howItWorks: "Sets 'MouseDataQueueSize' to 200 in HKLM\\SYSTEM\\CurrentControlSet\\Services\\mouclass\\Parameters.",
            problemSolved: "Prevents mouse buffer overflow and cursor stuttering when using 2000Hz, 4000Hz, or 8000Hz high-polling rate mice.",
            expectedResult: "Flawless tracking without cursor hitches on high-polling rate gaming mice.",
            category: TweakCategory.InputAndLatency,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Services\mouclass\Parameters",
            valueName: "MouseDataQueueSize",
            appliedValue: 200,
            fallbackDefaultValue: 100,
            requiresReboot: true
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "input_csgo_raw_input_buffer_coalescing",
            title: "Disable Windows Raw Input Message Coalescing",
            description: "Prevents Windows user subsystem from coalescing multiple raw WM_INPUT mouse/keyboard packets into single messages.",
            howItWorks: "Sets 'CoalesceInput' to 0 in HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\PrecisionTouchPad.",
            problemSolved: "Message coalescing introduces 4-8ms input delay by batching rapid mouse movements.",
            expectedResult: "Immediate per-poll mouse movement dispatch directly to game engine.",
            category: TweakCategory.InputAndLatency,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SOFTWARE\Microsoft\Windows\CurrentVersion\PrecisionTouchPad",
            valueName: "CoalesceInput",
            appliedValue: 0,
            fallbackDefaultValue: 1
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "input_gamepad_xinput_polling_latency",
            title: "Optimize Xbox Controller XInput Latency Profile",
            description: "Configures XInput controller driver to prioritize direct polling over energy-saving throttle loops.",
            howItWorks: "Sets 'PollingInterval' to 1 in HKLM\\SYSTEM\\CurrentControlSet\\Services\\xusb22\\Parameters.",
            problemSolved: "Removes variable controller polling delay for wired and wireless Xbox/XInput gamepads.",
            expectedResult: "Snappier analog stick and trigger response in racing, sports, and action games.",
            category: TweakCategory.InputAndLatency,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKLM",
            subKey: @"SYSTEM\CurrentControlSet\Services\xusb22\Parameters",
            valueName: "PollingInterval",
            appliedValue: 1,
            fallbackDefaultValue: 0
        );

        yield return TweakBuilder.CreateMultiRegistry(
            id: "input_pointer_trails_force_disable",
            title: "Disable Software Pointer Trails and Sonar Rings",
            description: "Ensures software cursor trailing and Ctrl-key location sonar rings are completely deactivated.",
            howItWorks: "Sets MouseTrails = 0 and MouseSonar = 0 in HKCU\\Control Panel\\Mouse.",
            problemSolved: "Software cursor trails force software rendering of mouse cursor instead of hardware GPU cursor plane.",
            expectedResult: "Zero DWM cursor compositing delay and lowest mouse latency.",
            category: TweakCategory.InputAndLatency,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            dwords: new[]
            {
                ("HKCU", @"Control Panel\Mouse", "MouseTrails", 0),
                ("HKCU", @"Control Panel\Mouse", "MouseSonar", 0)
            }
        );

        yield return TweakBuilder.CreateRegistryDword(
            id: "input_background_raw_input_lock",
            title: "Allow Direct Foreground Processing for Low-Latency Input",
            description: "Ensures Windows does not broadcast background input messages while a focused 3D game has mouse capture.",
            howItWorks: "Sets 'ForegroundLockTimeout' to 0 in HKCU\\Control Panel\\Desktop.",
            problemSolved: "Prevents Windows from delaying foreground focus transitions when clicking back into game windows.",
            expectedResult: "Instant mouse capture and zero focus-switch latency.",
            category: TweakCategory.InputAndLatency,
            risk: RiskLevel.Low,
            recommendation: RecommendationLevel.Recommended,
            hive: "HKCU",
            subKey: @"Control Panel\Desktop",
            valueName: "ForegroundLockTimeout",
            appliedValue: 0,
            fallbackDefaultValue: 200000
        );
    }
}
