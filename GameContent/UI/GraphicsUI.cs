using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using TanksRebirth.Graphics;
using TanksRebirth.Graphics.DynamicLighting;
using TanksRebirth.Internals.Common.GameUI;
using TanksRebirth.Internals.Common.Utilities;
using Preset = TanksRebirth.Graphics.DynamicLighting.LightingPresets.Preset;

namespace TanksRebirth.GameContent.UI;

// Note to self: ai really doesn't generate the most portable UI code.
// will de-shittify this so it's more front-facing (probably with UI backend rewrite)

/// <summary>
/// The Video page of the settings window.
/// </summary>
public static class GraphicsUI {
    // the rows GameUI refers to by name (kept for compatibility)
    public static SettingRow PPLButton = null!;
    public static SettingRow VSyncBtn = null!;
    public static SettingRow WinKindBtn = null!;
    public static SettingRow ResBtn = null!;
    public static SettingRow FrameLimitBtn = null!;
    public static SettingRow MSAABtn = null!;

    public static SettingRow LightingBtn = null!;
    public static SettingRow LightingQualityBtn = null!;
    public static SettingRow SunShadowsBtn = null!;
    public static SettingRow LampShadowsBtn = null!;
    public static SettingRow LightShaftsBtn = null!;
    public static SettingRow LightCountBtn = null!;
    public static SettingRow TimeOfDayBtn = null!;
    public static SettingRow DayLengthBtn = null!;
    public static SettingRow GameplayLightsBtn = null!;
    public static SettingRow RoomShadowsBtn = null!;

    public static SettingRow RainyWindowsBtn = null!;
    public static SettingRow RainStrengthBtn = null!;
    public static SettingRow BlobShadowsBtn = null!;
    public static SettingRow MenuBlurBtn = null!;

    public static UITextButton ApplyBtn = null!;
    public static UITextButton ResetBtn = null!;

    /// <summary>The resolution currently in use.</summary>
    public static KeyValuePair<int, int> CurrentRes;

    /// <summary>Whether the Video page is on screen. Clearing it closes the settings window.</summary>
    public static bool IsVisible;

    static SettingsPage Page => SettingsUI.Video;

    static WindowKind _pendingKind;
    static Point _pendingRes;
    static bool HasPendingDisplay => _pendingKind != TankGame.Settings.Display.WindowKind ||
        (_pendingKind != WindowKind.FullscreenBorderless && _pendingRes != new Point(TankGame.Settings.Display.ResWidth, TankGame.Settings.Display.ResHeight));

    static Point[] _resolutions = [];
    static readonly Point[] _fallbackResolutions = [
        new(1280, 720), new(1366, 768), new(1600, 900), new(1920, 1080), new(2560, 1440), new(3840, 2160)
    ];

    static readonly WindowKind[] _windowKinds = [WindowKind.Windowed, WindowKind.FullscreenBorderless, WindowKind.Fullscreen];
    static readonly Preset[] _timesOfDay = [
        Preset.Sunrise, Preset.LateMorning, Preset.Midday, Preset.MidAfternoon, Preset.LateAfternoon,
        Preset.GoldenHour, Preset.Evening, Preset.Dusk, Preset.Midnight, Preset.Blackout, Preset.DayCycle,
    ];
    static readonly float[] _dayLengths = [2f, 5f, 8f, 15f, 30f, 60f, 24f * 60f];
    static readonly string[] _rainStrengthNames = ["Light", "Medium", "Heavy"];

    static readonly float DisplayX = SettingsUI.ThirdX(0), LightingX = SettingsUI.ThirdX(1), QualityX = SettingsUI.ThirdX(2);
    const float ColumnW = SettingsUI.ThirdW;
    static float RowY(int index) => SettingsUI.RowY(index);

    // setup

    // this ALL needs localization bad
    public static void Initialize() {
        Page.Clear();
        Page.Opened = ResetPendingDisplay;

        CurrentRes = new(TankGame.Settings.Display.ResWidth, TankGame.Settings.Display.ResHeight);
        _resolutions = FindResolutions();
        ResetPendingDisplay();

        Page.Header("Display", DisplayX, width: ColumnW);
        Page.Header("Dynamic Lighting", LightingX, width: ColumnW);
        Page.Header("Graphics Quality", QualityX, width: ColumnW);

        var lang = TankGame.GameLanguage;
        
        // display
        WinKindBtn = Row(DisplayX, RowY(0), lang.Settings.WindowKind ?? "Window Mode",
            "Windowed, borderless (covers the screen, quick to alt-tab) or exclusive fullscreen. Press Apply to switch.",
            () => WindowKindName(_pendingKind) + (_pendingKind != TankGame.Settings.Display.WindowKind ? " *" : ""),
            dir => _pendingKind = GameUI.Cycle(_windowKinds, _pendingKind, dir));

        ResBtn = Row(DisplayX, RowY(1), lang.Settings.Resolution ?? "Resolution",
            "The window size (or the monitor mode in fullscreen). Borderless always uses your desktop resolution. Press Apply to switch.",
            () => _pendingKind == WindowKind.FullscreenBorderless
                ? $"{DesktopSize.X}x{DesktopSize.Y}"
                : $"{_pendingRes.X}x{_pendingRes.Y}" + (_pendingRes != new Point(TankGame.Settings.Display.ResWidth, TankGame.Settings.Display.ResHeight) ? " *" : ""),
            dir => _pendingRes = GameUI.Cycle(_resolutions, _pendingRes, dir),
            enabled: () => _pendingKind != WindowKind.FullscreenBorderless);

        VSyncBtn = Row(DisplayX, RowY(2), lang.Settings.VSync ?? "Vertical Sync",
            lang.Settings.VSyncDesc ?? "Synchronizes the game with your monitor's refresh rate to stop screen tearing.",
            () => GameUI.OnOff(TankGame.Settings.Display.Vsync),
            _ => {
                TankGame.Settings.Display.Vsync = !TankGame.Settings.Display.Vsync;
                var graphics = TankGame.Instance.Graphics;
                var current = TankGame.Instance.GraphicsDevice.PresentationParameters;
                // keep the current size: ApplyChanges would otherwise pick up whatever stale size was last requested
                graphics.PreferredBackBufferWidth = current.BackBufferWidth;
                graphics.PreferredBackBufferHeight = current.BackBufferHeight;
                graphics.SynchronizeWithVerticalRetrace = TankGame.Settings.Display.Vsync;
                graphics.ApplyChanges();
            });

        FrameLimitBtn = Row(DisplayX, RowY(3), "Frame Rate Limit",
            "Caps the frame rate while VSync is off.",
            () => TankGame.Settings.Display.TargetFPS <= 0 ? "Unlimited" : $"{TankGame.Settings.Display.TargetFPS} FPS",
            dir => TankGame.Settings.Display.TargetFPS = GameUI.Cycle(GraphicsSettings.FrameRateOptions, TankGame.Settings.Display.TargetFPS, dir),
            enabled: () => !TankGame.Settings.Display.Vsync);

        ApplyBtn = Page.Button(DisplayX, RowY(5), ColumnW, "Apply",
            "Switches to the selected window mode and resolution.", ApplyDisplay, () => HasPendingDisplay, highlight: true);
        ResetBtn = Page.Button(DisplayX, RowY(6), ColumnW, "Reset to Defaults",
            "Puts every option on this page back to its default (the display mode still needs Apply).", ResetToDefaults);

        // dynamic lighting
        LightingBtn = Row(LightingX, RowY(0), "Dynamic Lighting",
            "Real-time sun, shadows, lamps and light beams. Turn it off for the original look (and the best performance).",
            () => GameUI.OnOff(DynLightSettings.Current.Enabled) + (LightingSystem.IsAvailable || !DynLightSettings.Current.Enabled ? "" : " (unavailable)"),
            _ => {
                DynLightSettings.Current.Enabled = !DynLightSettings.Current.Enabled;
                ApplyLighting();
            });

        LightingQualityBtn = Row(LightingX, RowY(1), "Lighting Quality",
            "Sets all the lighting options below at once. Changing one of them by hand makes this Custom.",
            () => DynLightSettings.Current.QualityLevel.ToString(),
            dir => {
                LightingQualityLevel[] levels = [LightingQualityLevel.Low, LightingQualityLevel.Medium, LightingQualityLevel.High, LightingQualityLevel.Ultra];
                var current = DynLightSettings.Current.QualityLevel == LightingQualityLevel.Custom ? LightingQualityLevel.High : DynLightSettings.Current.QualityLevel;
                DynLightSettings.Current.SetQualityLevel(DynLightSettings.Current.QualityLevel == LightingQualityLevel.Custom ? current : GameUI.Cycle(levels, current, dir));
                ApplyLighting();
            },
            enabled: LightingOn);

        SunShadowsBtn = Row(LightingX, RowY(2), "Sun Shadows",
            "Shadows from the sun and moon, on the board and across the room. Ultra doubles the resolution (needs a HiDef capable GPU).",
            () => QualityName(DynLightSettings.Current.SunShadows),
            dir => { DynLightSettings.Current.SunShadows = GameUI.  Cycle(Enum.GetValues<ShadowQuality>(), DynLightSettings.Current.SunShadows, dir); ApplyLighting(true); },
            enabled: LightingOn);

        LampShadowsBtn = Row(LightingX, RowY(3), "Light Shadows",
            "Shadows from lamps, tank headlights and explosions. Higher settings shadow more lights with smoother edges.",
            () => QualityName(DynLightSettings.Current.LampShadows),
            dir => { DynLightSettings.Current.LampShadows = GameUI.Cycle(Enum.GetValues<ShadowQuality>(), DynLightSettings.Current.LampShadows, dir); ApplyLighting(true); },
            enabled: LightingOn);

        LightShaftsBtn = Row(LightingX, RowY(4), "Light Shafts",
            "Beams of sunlight through the windows. Higher settings render them at a higher resolution.",
            () => DynLightSettings.Current.LightShafts.ToString(),
            dir => { DynLightSettings.Current.LightShafts = GameUI.Cycle(Enum.GetValues<EffectQuality>(), DynLightSettings.Current.LightShafts, dir); ApplyLighting(true); },
            enabled: LightingOn);

        LightCountBtn = Row(LightingX, RowY(5), "Light Count",
            "How many lights can shine at once (lamps, headlights, shells, mines, explosions).",
            () => DynLightSettings.Current.LightCount switch {
                EffectQuality.Off => "Off",
                EffectQuality.Low => "Low (8)",
                EffectQuality.Medium => "Medium (16)",
                _ => "High (32)",
            },
            dir => { DynLightSettings.Current.LightCount = GameUI.Cycle(Enum.GetValues<EffectQuality>(), DynLightSettings.Current.LightCount, dir); ApplyLighting(true); },
            enabled: LightingOn);

        RoomShadowsBtn = Row(LightingX, RowY(6), "Room Shadows",
            "The room's walls, windows and furniture block the sun, so it only comes in through the windows. Off = the sun shines through the walls.",
            () => GameUI.OnOff(DynLightSettings.Current.RoomShadows),
            _ => { DynLightSettings.Current.RoomShadows = !DynLightSettings.Current.RoomShadows; ApplyLighting(); },
            enabled: () => LightingOn() && DynLightSettings.Current.SunShadows != ShadowQuality.Off);

        TimeOfDayBtn = Row(LightingX, RowY(7), "Time of Day",
            "The time of day in the room. Day/Night Cycle runs a clock: the sun moves and the colors change minute by minute.",
            () => DynLightSettings.Current.TimeOfDay == Preset.DayCycle
                ? $"Day/Night Cycle ({LightingPresets.HourText})"
                : DynLightSettings.Current.TimeOfDay.ToString().SplitByCamel(),
            dir => { DynLightSettings.Current.TimeOfDay = GameUI.Cycle(_timesOfDay, DynLightSettings.Current.TimeOfDay, dir); ApplyLighting(); },
            enabled: LightingOn);

        DayLengthBtn = Row(LightingX, RowY(8), "Day Length",
            "How long a full day takes in the Day/Night Cycle, in real time.",
            () => DynLightSettings.Current.DayLengthMinutes >= 24f * 60f - 1f ? "Real Time (24 hours)" : $"{DynLightSettings.Current.DayLengthMinutes:0.#} minutes",
            dir => { DynLightSettings.Current.DayLengthMinutes = GameUI.Cycle(_dayLengths, GameUI.NearestOf(_dayLengths, DynLightSettings.Current.DayLengthMinutes), dir); ApplyLighting(); },
            enabled: () => LightingOn() && DynLightSettings.Current.TimeOfDay == Preset.DayCycle);

        GameplayLightsBtn = Row(LightingX, RowY(9), "Gameplay Lights",
            "Whether to have lights for gameplay ",
            () => GameUI.OnOff(DynLightSettings.Current.GameplayLights),
            _ => { DynLightSettings.Current.GameplayLights = !DynLightSettings.Current.GameplayLights; ApplyLighting(); },
            enabled: LightingOn);

        // graphics quality
        MSAABtn = Row(QualityX, RowY(0), "Anti-Aliasing",
            "Smoothes jagged edges on 3D models (multisample anti-aliasing). Higher levels cost more GPU time.",
            () => TankGame.Settings.Graphics.MSAASamples <= 0 ? "Off" : $"MSAA {TankGame.Settings.Graphics.MSAASamples}x",
            dir => TankGame.Settings.Graphics.MSAASamples = GameUI.Cycle(GraphicsSettings.MSAAOptions, TankGame.Settings.Graphics.MSAASamples, dir));

        PPLButton = Row(QualityX, RowY(1), lang.Settings.PerPxLight ?? "Per-Pixel Lighting",
            lang.Settings.PerPxLightDesc ?? "Lights every pixel instead of every vertex. Smoother lighting on models.",
            () => GameUI.OnOff(TankGame.Settings.Graphics.PerPixelLighting),
            _ => TankGame.Settings.Graphics.PerPixelLighting = !TankGame.Settings.Graphics.PerPixelLighting);

        RainyWindowsBtn = Row(QualityX, RowY(2), "Rainy Windows",
            "Rain beads, drips and fog on the room's windows during thunderstorms.",
            () => GameUI.OnOff(TankGame.Settings.Graphics.RainyWindows),
            _ => { TankGame.Settings.Graphics.RainyWindows = !TankGame.Settings.Graphics.RainyWindows; GraphicsSettings.ApplyEffects(); });

        RainStrengthBtn = Row(QualityX, RowY(3), "Rain Strength",
            "How much water gathers on the windows during thunderstorms.",
            () => _rainStrengthNames[Array.IndexOf(GraphicsSettings.RainStrengthOptions, GameUI.NearestOf(GraphicsSettings.RainStrengthOptions, TankGame.Settings.Graphics.RainStrength))],
            dir => {
                TankGame.Settings.Graphics.RainStrength = GameUI.Cycle(GraphicsSettings.RainStrengthOptions,
                    GameUI.NearestOf(GraphicsSettings.RainStrengthOptions, TankGame.Settings.Graphics.RainStrength), dir);
                GraphicsSettings.ApplyEffects();
            },
            enabled: () => TankGame.Settings.Graphics.RainyWindows);

        BlobShadowsBtn = Row(QualityX, RowY(4), "Tank Blob Shadows",
            "The soft round shadows under tanks and mines.",
            () => GameUI.OnOff(TankGame.Settings.Graphics.TankBlobShadows),
            _ => { TankGame.Settings.Graphics.TankBlobShadows = !TankGame.Settings.Graphics.TankBlobShadows; GraphicsSettings.ApplyEffects(); });

        MenuBlurBtn = Row(QualityX, RowY(5), "Menu Blur",
            "Blurs the battle behind the main menu.",
            () => GameUI.OnOff(TankGame.Settings.Graphics.MenuBlur),
            _ => TankGame.Settings.Graphics.MenuBlur = !TankGame.Settings.Graphics.MenuBlur);
    }

    /// <summary>Opens the settings window on this page, or closes the window.</summary>
    public static void SetVisibility(bool visibility) {
        if (visibility)
            SettingsUI.Open(SettingsUI.Video);
        else if (SettingsUI.IsOpen && SettingsUI.Current == SettingsUI.Video)
            SettingsUI.Close();
    }

    static void ResetPendingDisplay() {
        _pendingKind = TankGame.Settings.Display.WindowKind;
        _pendingRes = new Point(TankGame.Settings.Display.ResWidth, TankGame.Settings.Display.ResHeight);
    }

    // actions

    static void ApplyDisplay() {
        if (!HasPendingDisplay)
            return;
        TankGame.Settings.Display.WindowKind = _pendingKind;
        if (_pendingKind != WindowKind.FullscreenBorderless) {
            TankGame.Settings.Display.ResWidth = _pendingRes.X;
            TankGame.Settings.Display.ResHeight = _pendingRes.Y;
        }
        WindowUtils.ApplyDisplayMode(TankGame.Settings.Display.WindowKind, TankGame.Settings.Display.ResWidth, TankGame.Settings.Display.ResHeight);
        CurrentRes = new(TankGame.Settings.Display.ResWidth, TankGame.Settings.Display.ResHeight);
    }

    static void ApplyLighting(bool qualityOptionChanged = false) {
        if (qualityOptionChanged)
            DynLightSettings.Current.UpdateQualityLevel();
        TankGame.Settings.Lighting = DynLightSettings.Current;
        DynLightSettings.Current.Apply();
    }

    static void ResetToDefaults() {
        var defaults = new GameConfig();
        _pendingKind = defaults.Display.WindowKind;
        _pendingRes = new Point(defaults.Display.ResWidth, defaults.Display.ResHeight);

        if (TankGame.Settings.Display.Vsync != defaults.Display.Vsync)
            VSyncBtn.Step(1);
        TankGame.Settings.Graphics.PerPixelLighting = defaults.Graphics.PerPixelLighting;
        TankGame.Settings.Display.TargetFPS = defaults.Display.TargetFPS;
        TankGame.Settings.Graphics.MSAASamples = defaults.Graphics.MSAASamples;
        TankGame.Settings.Graphics.RainyWindows = defaults.Graphics.RainyWindows;
        TankGame.Settings.Graphics.RainStrength = defaults.Graphics.RainStrength;
        TankGame.Settings.Graphics.TankBlobShadows = defaults.Graphics.TankBlobShadows;
        TankGame.Settings.Graphics.MenuBlur = defaults.Graphics.MenuBlur;
        TankGame.Settings.Gameplay.FirstPersonFOV = defaults.Gameplay.FirstPersonFOV;
        GraphicsSettings.ApplyEffects();

        // keep the same settings object (DynLightSettings.Current) and copy the defaults into it
        var d = new DynLightSettings();
        DynLightSettings.Current.Enabled = d.Enabled;
        DynLightSettings.Current.TimeOfDay = d.TimeOfDay;
        DynLightSettings.Current.DayLengthMinutes = d.DayLengthMinutes;
        DynLightSettings.Current.GameplayLights = d.GameplayLights;
        DynLightSettings.Current.RoomShadows = d.RoomShadows;
        DynLightSettings.Current.SetQualityLevel(d.QualityLevel);
        ApplyLighting();
    }

    // helpers

    static SettingRow Row(float x, float y, string label, string description, Func<string> value, Action<int> step, Func<bool>? enabled = null)
        => Page.Row(x, y, label, description, value, step, enabled, ColumnW);

    static bool LightingOn() => DynLightSettings.Current.Enabled;

    static string QualityName(ShadowQuality quality) =>
        quality == ShadowQuality.Ultra && TankGame.Instance.GraphicsDevice.GraphicsProfile != GraphicsProfile.HiDef
            ? "Ultra (as High: no HiDef)"
            : quality.ToString();

    static string WindowKindName(WindowKind kind) => kind switch {
        WindowKind.FullscreenBorderless => "Borderless",
        WindowKind.Fullscreen => "Fullscreen",
        _ => "Windowed",
    };

    static Point DesktopSize {
        get {
            var mode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            return new Point(mode.Width, mode.Height);
        }
    }

    /// <summary>The monitor's modes (at least 1024x576), or a list of common sizes if the driver reports none.</summary>
    static Point[] FindResolutions() {
        var list = new List<Point>();
        try {
            foreach (var mode in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes)
                if (mode.Width >= 1024 && mode.Height >= 576)
                    list.Add(new Point(mode.Width, mode.Height));
        }
        catch {
            // some drivers can't enumerate modes
        }
        if (list.Count == 0)
            list.AddRange(_fallbackResolutions.Where(r => r.X <= DesktopSize.X && r.Y <= DesktopSize.Y));
        // always offer the current one, even if it's odd (a resized window)
        list.Add(new Point(TankGame.Settings.Display.ResWidth, TankGame.Settings.Display.ResHeight));
        return list.Distinct().OrderBy(r => r.X).ThenBy(r => r.Y).ToArray();
    }
}
