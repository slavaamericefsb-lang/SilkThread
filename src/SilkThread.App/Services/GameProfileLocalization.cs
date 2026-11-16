using System;
using System.Collections.Generic;
using SilkThread.Core.GameProfiles;

namespace SilkThread.App.Services;

public record GameTranslation(
    string Genre,
    string Description,
    IReadOnlyList<LaunchArgItem> LaunchArgs,
    IReadOnlyList<VideoSettingItem> VideoSettings,
    IReadOnlyList<ConfigFileInfo> ConfigFiles
);

public static class GameProfileLocalization
{
    public static string GetGenre(string gameId, string fallback)
    {
        bool isRu = LocalizationService.Instance.CurrentLanguage == "ru";
        if (isRu) return fallback;
        return EnTranslations.TryGetValue(gameId, out var t) ? t.Genre : fallback;
    }

    public static string GetDescription(string gameId, string fallback)
    {
        bool isRu = LocalizationService.Instance.CurrentLanguage == "ru";
        if (isRu) return fallback;
        return EnTranslations.TryGetValue(gameId, out var t) ? t.Description : fallback;
    }

    public static IReadOnlyList<LaunchArgItem> GetLaunchArgs(string gameId, IReadOnlyList<LaunchArgItem> fallback)
    {
        bool isRu = LocalizationService.Instance.CurrentLanguage == "ru";
        if (isRu) return fallback;
        return EnTranslations.TryGetValue(gameId, out var t) ? t.LaunchArgs : fallback;
    }

    public static IReadOnlyList<VideoSettingItem> GetVideoSettings(string gameId, IReadOnlyList<VideoSettingItem> fallback)
    {
        bool isRu = LocalizationService.Instance.CurrentLanguage == "ru";
        if (isRu) return fallback;
        return EnTranslations.TryGetValue(gameId, out var t) ? t.VideoSettings : fallback;
    }

    public static IReadOnlyList<ConfigFileInfo> GetConfigFiles(string gameId, IReadOnlyList<ConfigFileInfo> fallback)
    {
        bool isRu = LocalizationService.Instance.CurrentLanguage == "ru";
        if (isRu) return fallback;
        return EnTranslations.TryGetValue(gameId, out var t) ? t.ConfigFiles : fallback;
    }

    private static readonly Dictionary<string, GameTranslation> EnTranslations = new()
    {
        ["cs2"] = new GameTranslation(
            Genre: "Tactical Shooter",
            Description: "Valve's flagship competitive tactical shooter built on Source 2 with sub-tick network architecture.",
            LaunchArgs: new[]
            {
                new LaunchArgItem("-novid", "Skip Valve / Source 2 intro videos", "Instantly loads directly into the main menu without delay"),
                new LaunchArgItem("-fullscreen", "Enforce exclusive fullscreen display mode", "Bypasses DWM compositor overhead for minimal display latency"),
                new LaunchArgItem("+fps_max 0", "Remove framerate cap", "Minimizes frametime delivery lag and frame queuing delay"),
                new LaunchArgItem("-nojoy", "Disable controller / joystick polling subsystem", "Frees worker thread cycles and eliminates polling-induced microstutters"),
                new LaunchArgItem("+exec autoexec.cfg", "Auto-execute autoexec.cfg configuration", "Ensures competitive keybinds, crosshair, and network rates load on launch")
            },
            VideoSettings: new[]
            {
                new VideoSettingItem("Display Mode", "Exclusive Fullscreen", "Lowest possible frame presentation latency"),
                new VideoSettingItem("Resolution / Aspect Ratio", "1920x1080 (or 1280x960 4:3 stretched)", "Wider enemy models and higher framerate"),
                new VideoSettingItem("Shadow Quality", "Low / Very Low", "Eliminates FPS drops during smoke grenades and utility throws"),
                new VideoSettingItem("Model / Texture Detail", "Low / Medium", "Clear silhouette visibility of agents without VRAM lag"),
                new VideoSettingItem("NVIDIA Reflex Low Latency", "Enabled + Boost", "Locks maximum GPU clock and reduces render queue input lag"),
                new VideoSettingItem("Ambient Occlusion", "Disabled", "Removes corner shading, making enemy silhouettes brighter"),
                new VideoSettingItem("Vertical Sync (V-Sync)", "Disabled", "Eliminates frame buffering delay and cursor lag when aiming"),
                new VideoSettingItem("Multisampling Anti-Aliasing (MSAA)", "CMAA2 or 2x MSAA", "Clean geometric edges without temporal TAA motion blur")
            },
            ConfigFiles: new[]
            {
                new ConfigFileInfo("autoexec.cfg", @"<Steam>\steamapps\common\Counter-Strike Global Offensive\game\csgo\cfg\autoexec.cfg", "Custom esports keybinds, crosshair, viewmodel, and tickrate settings"),
                new ConfigFileInfo("cs2_video.txt", @"<Steam>\userdata\<AccountID>\730\local\cfg\cs2_video.txt", "Low-level Source 2 graphics engine configuration file")
            }
        ),

        ["dota2"] = new GameTranslation(
            Genre: "MOBA",
            Description: "Valve's legendary competitive MOBA, tuned for intense 5v5 teamfights without 1% Low FPS dips.",
            LaunchArgs: new[]
            {
                new LaunchArgItem("-novid", "Skip Valve intro video", "Fast startup into the main menu"),
                new LaunchArgItem("-high", "High Windows process priority", "Protects against background interruptions and FPS drops in teamfights"),
                new LaunchArgItem("-map dota", "Preload Dota map into RAM on startup", "Completely eliminates first-load stutters and match connect freezes"),
                new LaunchArgItem("-nojoy", "Disable joystick / controller subsystem", "Saves RAM and reduces unnecessary background input polling"),
                new LaunchArgItem("-console", "Enable developer console (~)", "Fast access to net_graph, ping, and network diagnostic commands"),
                new LaunchArgItem("+fps_max 0", "Unlock framerate limit", "Smoothest ability animations on 144–360 Hz esports monitors")
            },
            VideoSettings: new[]
            {
                new VideoSettingItem("Display Mode", "Exclusive Fullscreen", "Lowest mouse cursor input latency for rapid clicking"),
                new VideoSettingItem("Resolution", "Native (100% Render Scale)", "Pixel-sharp hero and creep last-hitting precision"),
                new VideoSettingItem("Shadow Quality", "Off or Low", "Critical 1% Low FPS boost during massive ultimate teamfights"),
                new VideoSettingItem("Animated Portraits", "Disabled", "Reduces HUD rendering load on CPU worker threads"),
                new VideoSettingItem("Anti-Aliasing", "Off or FXAA", "Sharp hero visibility without blurring small icons or indicators"),
                new VideoSettingItem("Vertical Sync", "Disabled", "Eliminates edge-panning input lag"),
                new VideoSettingItem("Effect Quality", "Low / Medium", "Stable frametimes during intensive spell casting")
            },
            ConfigFiles: new[]
            {
                new ConfigFileInfo("autoexec.cfg", @"<Steam>\steamapps\common\dota 2 beta\game\dota\cfg\autoexec.cfg", "Network rates, quickcast configurations, and custom camera keybinds"),
                new ConfigFileInfo("video.txt", @"<Steam>\steamapps\common\dota 2 beta\game\dota\cfg\video.txt", "Source 2 graphics and rendering configuration file for Dota 2")
            }
        ),

        ["valorant"] = new GameTranslation(
            Genre: "Tactical Shooter",
            Description: "Riot Games' tactical shooter on UE4 with Vanguard kernel-level anti-cheat, demanding ultra-low input latency.",
            LaunchArgs: new[]
            {
                new LaunchArgItem("-high", "High process priority", "Prioritizes render threads over background OS workloads"),
                new LaunchArgItem("-useallavailablecores", "Use all CPU cores", "Enables full multithreaded task scheduling on Unreal Engine 4"),
                new LaunchArgItem("-nomansky", "Disable background sky rendering", "Slight framerate boost on entry-level GPUs")
            },
            VideoSettings: new[]
            {
                new VideoSettingItem("Display Mode", "Exclusive Fullscreen", "Direct framebuffer scanout bypassing DWM compositing lag"),
                new VideoSettingItem("Material Quality", "Low", "Maximum sustained FPS and elimination of distracting specular reflections"),
                new VideoSettingItem("Texture Quality", "Low / Medium", "Frees GPU VRAM and eliminates hitching when turning corners"),
                new VideoSettingItem("Detail Quality", "Low", "Cleans visual clutter on walls, chokepoints, and sightlines"),
                new VideoSettingItem("UI Quality", "Low", "Reduces HUD memory consumption without affecting crosshair clarity"),
                new VideoSettingItem("Vignette", "Disabled", "Even brightness across screen edges for peripheral visibility"),
                new VideoSettingItem("Vertical Sync", "Disabled", "Lowest possible cursor lag for crisp flickshots"),
                new VideoSettingItem("Anti-Aliasing", "None or MSAA 2x", "Zero motion blur during fast 180-degree camera turns"),
                new VideoSettingItem("NVIDIA Reflex Low Latency", "On + Boost", "Suppresses GPU render queuing to minimum hardware latency")
            },
            ConfigFiles: new[]
            {
                new ConfigFileInfo("GameUserSettings.ini", @"%LOCALAPPDATA%\VALORANT\Saved\Config\<UserGUID>\Windows\GameUserSettings.ini", "Primary resolution, scaling, and window mode preferences")
            }
        ),

        ["apex"] = new GameTranslation(
            Genre: "Battle Royale",
            Description: "Respawn's high-mobility battle royale on a heavily modified Source engine with ultra-fast movement.",
            LaunchArgs: new[]
            {
                new LaunchArgItem("+fps_max 0", "Unlock framerate limit", "Smooth motion beyond default 144 FPS cap"),
                new LaunchArgItem("-fullscreen", "Exclusive fullscreen mode", "Direct frame output bypassing Windows desktop compositing"),
                new LaunchArgItem("-novid", "Skip EA / Respawn intros", "Instant entry into matchmaking lobby screen"),
                new LaunchArgItem("-dev", "Developer startup mode", "Skips intro cinematic sequence for faster asset loading"),
                new LaunchArgItem("+exec autoexec.cfg", "Auto-execute autoexec.cfg", "Loads low-level graphics and network CVars"),
                new LaunchArgItem("-high", "High CPU priority", "Mitigates input processing latency during chaotic firefights")
            },
            VideoSettings: new[]
            {
                new VideoSettingItem("Display Mode", "Fullscreen", "Eliminates Windows DWM composition latency"),
                new VideoSettingItem("Field of View (FOV)", "104 – 110", "Maximum peripheral view for tracking nearby enemies"),
                new VideoSettingItem("Texture Streaming Budget", "Very Low (None / 2GB)", "Completely prevents texture streaming microstutters across POIs"),
                new VideoSettingItem("Shadow Quality", "Disabled", "Massive 1% Low FPS gain across open canyon zones"),
                new VideoSettingItem("Sun Shadow Coverage", "Low", "Reduces framerate drops when looking across vast vistas"),
                new VideoSettingItem("Model Detail", "Low", "Improves legend silhouette visibility"),
                new VideoSettingItem("Effects Detail", "Low", "Stable frametime under Gibraltar / Bangalore bombardments"),
                new VideoSettingItem("NVIDIA Reflex Low Latency", "Enabled + Boost", "Lowest latency between trigger click and hit registration")
            },
            ConfigFiles: new[]
            {
                new ConfigFileInfo("videoconfig.txt", @"%USERPROFILE%\Saved Games\Respawn\Apex\local\videoconfig.txt", "Disable dynamic shadows via setting.csm_enabled 0 and tune LOD biases"),
                new ConfigFileInfo("autoexec.cfg", @"<SteamOrOrigin>\Apex Legends\cfg\autoexec.cfg", "CVars to disable view bobbing, ragdolls, and particle debris")
            }
        ),

        ["fortnite"] = new GameTranslation(
            Genre: "Battle Royale",
            Description: "Epic Games' flagship battle royale built on Unreal Engine 5 with specialized Performance Mode rendering.",
            LaunchArgs: new[]
            {
                new LaunchArgItem("-USEALLAVAILABLECORES", "Use all available CPU cores", "Accelerates shader compilation and physics task scheduling"),
                new LaunchArgItem("-high", "High Windows process priority", "Protects against stuttering during rapid build battles"),
                new LaunchArgItem("-nosplash", "Disable splash screen", "Faster client initialization without splash window"),
                new LaunchArgItem("-preferredProcessor", "Prefer fast P-cores", "Optimal thread binding on hybrid Intel / AMD CPU architectures")
            },
            VideoSettings: new[]
            {
                new VideoSettingItem("Rendering Mode", "Performance Mode", "Maximum framerate, minimal input lag, and transparent build visibility"),
                new VideoSettingItem("3D Resolution", "100%", "Native pixel clarity without blurring distant player silhouettes"),
                new VideoSettingItem("View Distance", "Near or Medium", "High framerate; player models still render at maximum distance"),
                new VideoSettingItem("Textures", "Low", "Instant asset streaming and minimal VRAM memory footprint"),
                new VideoSettingItem("Meshes", "Low", "Simplified building geometry ('bubble builds') for highest frametimes"),
                new VideoSettingItem("NVIDIA Reflex Low Latency", "On + Boost", "Eliminates GPU frame queuing during rapid edits and box fights")
            },
            ConfigFiles: new[]
            {
                new ConfigFileInfo("GameUserSettings.ini", @"%LOCALAPPDATA%\FortniteGame\Saved\Config\WindowsClient\GameUserSettings.ini", "Low-level UE5 engine parameters, motion blur disabling, and custom framerate locks")
            }
        ),

        ["warzone"] = new GameTranslation(
            Genre: "Battle Royale / Shooter",
            Description: "Massive battle royale on the IW Engine, demanding high VRAM bandwidth and balanced multi-core CPU scheduling.",
            LaunchArgs: new[]
            {
                new LaunchArgItem("-d3d11", "Force DirectX 11 API", "Stabilizes frametimes on select graphics configurations"),
                new LaunchArgItem("-high", "High Windows process priority", "Prioritizes Warzone worker threads over background processes")
            },
            VideoSettings: new[]
            {
                new VideoSettingItem("Display Mode", "Exclusive Fullscreen", "Direct display access without desktop compositor buffering"),
                new VideoSettingItem("Render Resolution", "100%", "Pixel-perfect reticle accuracy for long-range target acquisition"),
                new VideoSettingItem("VRAM Scale Target", "70 – 80%", "Prevents VRAM overflow and disastrous texture spilling into system RAM"),
                new VideoSettingItem("Texture Resolution", "Low / Very Low", "Eliminates hitching when entering dense urban points of interest"),
                new VideoSettingItem("Shadow Map Resolution", "Very Low", "Significant boost to average and 1% low framerate"),
                new VideoSettingItem("World / Weapon Motion Blur", "Disabled (World & Weapon)", "Clean image clarity during rapid camera sweeps"),
                new VideoSettingItem("NVIDIA Reflex Low Latency", "On + Boost", "Reduces latency between trigger click and weapon fire"),
                new VideoSettingItem("Depth of Field", "Disabled", "Sharp spotting of snipers and glint at distance")
            },
            ConfigFiles: new[]
            {
                new ConfigFileInfo("options.4.cod23.cst", @"%USERPROFILE%\Documents\Call of Duty\players\options.4.cod23.cst", "RendererWorkerCount CPU core pinning and VRAM scaling configuration")
            }
        ),

        ["pubg"] = new GameTranslation(
            Genre: "Battle Royale",
            Description: "Tactical hardcore battle royale on UE4 across vast 8x8 km maps, heavily reliant on CPU IPC and RAM sub-timings.",
            LaunchArgs: new[]
            {
                new LaunchArgItem("-USEALLAVAILABLECORES", "Use all available CPU cores", "Accelerates parallel physics and character animation computations"),
                new LaunchArgItem("-malloc=system", "Windows system memory allocator", "Optimizes memory block pooling and suppresses heap allocation stutters"),
                new LaunchArgItem("-high", "High CPU priority in Windows", "Prioritizes frame dispatch over background OS tasks"),
                new LaunchArgItem("-refresh 240", "Force monitor refresh rate", "Guarantees maximum display refresh rate enforcement")
            },
            VideoSettings: new[]
            {
                new VideoSettingItem("Display Mode", "Fullscreen (Exclusive)", "Eliminates windowed composition input lag"),
                new VideoSettingItem("Render Scale", "100%", "Clean pixel alignment without upscaling artifacts"),
                new VideoSettingItem("Anti-Aliasing", "Medium", "Reduces edge staircasing without blurring moving silhouettes"),
                new VideoSettingItem("Post-Processing", "Very Low", "Disables bloom and glare effects for cleaner visibility"),
                new VideoSettingItem("Shadows", "Very Low", "Easy detection of enemies lurking under building and cliff shadows"),
                new VideoSettingItem("Foliage", "Very Low", "Minimal grass rendering density at long range"),
                new VideoSettingItem("Vertical Sync", "Disabled", "Instant mouse response when tracking targets")
            },
            ConfigFiles: new[]
            {
                new ConfigFileInfo("GameUserSettings.ini", @"%LOCALAPPDATA%\TslGame\Saved\Config\WindowsNoEditor\GameUserSettings.ini", "Competitive resolution, graphical fidelity, and framerate configuration")
            }
        ),

        ["r6s"] = new GameTranslation(
            Genre: "Tactical Shooter",
            Description: "Tactical competitive shooter with destructive environments, demanding instant reaction times on corner peeks.",
            LaunchArgs: new[]
            {
                new LaunchArgItem("-high", "High process priority in Windows", "Stable framerate during environmental wall destruction and explosions"),
                new LaunchArgItem("-vulkan", "Launch via modern Vulkan API", "Reduces CPU draw call overhead and frametime variance"),
                new LaunchArgItem("-con_enable 1", "Enable developer console", "Allows real-time telemetry monitoring for tickrate and network latency")
            },
            VideoSettings: new[]
            {
                new VideoSettingItem("Display Mode", "Exclusive Fullscreen", "Reduces display presentation latency to hardware minimum"),
                new VideoSettingItem("Aspect Ratio", "16:9 or 4:3 / 3:2 Stretched", "Stretches operator player models for easier headshots"),
                new VideoSettingItem("Field of View (FOV)", "84 – 90", "Broad peripheral vision for holding pixel angles and tight peek holes"),
                new VideoSettingItem("Texture Quality", "Medium", "Optimal balance of weapon skin clarity and VRAM allocation"),
                new VideoSettingItem("LOD Quality", "Ultra", "Preserves crisp head silhouettes of operators on long-range peeks"),
                new VideoSettingItem("Lens Effects & Reflections", "Off", "Clean sightlines without distracting flares or reflections"),
                new VideoSettingItem("NVIDIA Reflex Low Latency", "On + Boost", "Instant shot registration on pre-fire peeks")
            },
            ConfigFiles: new[]
            {
                new ConfigFileInfo("GameSettings.ini", @"%USERPROFILE%\Documents\My Games\Rainbow Six - Siege\<UserGUID>\GameSettings.ini", "Direct mouse Raw Input configuration, custom FOV, and low-latency audio")
            }
        )
    };
}
