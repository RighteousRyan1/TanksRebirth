using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Tanks;
using TanksRebirth.GameContent.UI.MainMenu;
using TanksRebirth.Graphics;
using TanksRebirth.Graphics.DynamicLighting;
using TanksRebirth.Internals.Common.GameUI;
using TanksRebirth.Internals.Common.Utilities;
using Preset = TanksRebirth.Graphics.DynamicLighting.LightingPresets.Preset;

namespace TanksRebirth.GameContent.UI;

/// <summary>
/// The Video page of the settings window (<see cref="SettingsUI.Video"/>): a "Display" column and a "Dynamic StaticLighting"
/// column of option rows, and Apply / MakeSeekable buttons.
/// </summary>
/// <remarks>
/// Every row is a <see cref="SettingRow"/>: left click picks the next value, right click the previous one.
/// Most options apply immediately. Window mode and resolution only apply when "Apply" is pressed (switching on every
/// click would make the window jump around), and are discarded when the page is left without applying.
/// </remarks>
public static class GraphicsUI {
    // the rows GameUI refers to by name (kept for compatibility)
    public static SettingRow PPLButton = null!;
    public static SettingRow VSyncBtn = null!;
    public static SettingRow WinKindBtn = null!;
    public static SettingRow ResBtn = null!;
    public static SettingRow FadeTracksBtn = null!;
    public static SettingRow MenuGameplayBtn = null!;
    public static SettingRow FrameLimitBtn = null!;
    public static SettingRow MSAABtn = null!;
    public static SettingRow TrackLimitBtn = null!;

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

    public static UITextButton ApplyBtn = null!;
    public static UITextButton ResetBtn = null!;

    /// <summary>The resolution currently in use.</summary>
    public static KeyValuePair<int, int> CurrentRes;

    /// <summary>Whether the Video page is on screen. Clearing it closes the settings window.</summary>
    public static bool IsVisible;

    static SettingsPage Page => SettingsUI.Video;

    // window mode and resolution wait for "Apply"
    static WindowKind _pendingKind;
    static Point _pendingRes;
    static bool HasPendingDisplay => _pendingKind != TankGame.Settings.WindowKind ||
        (_pendingKind != WindowKind.FullscreenBorderless && _pendingRes != new Point(TankGame.Settings.ResWidth, TankGame.Settings.ResHeight));

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

    const float LeftX = SettingsUI.LeftX, RightX = SettingsUI.RightX, ColumnW = SettingsUI.ColumnW;
    static float RowY(int index) => SettingsUI.RowY(index);

    static LightingSettings Lighting => LightingSettings.Current;

    // setup

    public static void Initialize() {
        Page.Clear();
        Page.Opened = ResetPendingDisplay;

        CurrentRes = new(TankGame.Settings.ResWidth, TankGame.Settings.ResHeight);
        _resolutions = FindResolutions();
        ResetPendingDisplay();

        Page.Header("Display", LeftX);
        Page.Header("Dynamic Lighting", RightX);

        var lang = TankGame.GameLanguage;
        
        // display
        WinKindBtn = Row(LeftX, RowY(0), lang.Settings.WindowKind ?? "Window Mode",
            "Windowed, borderless (covers the screen, quick to alt-tab) or exclusive fullscreen. Press Apply to switch.",
            () => WindowKindName(_pendingKind) + (_pendingKind != TankGame.Settings.WindowKind ? " *" : ""),
            dir => _pendingKind = Cycle(_windowKinds, _pendingKind, dir));

        ResBtn = Row(LeftX, RowY(1), lang.Settings.Resolution ?? "Resolution",
            "The window size (or the monitor mode in fullscreen). Borderless always uses your desktop resolution. Press Apply to switch.",
            () => _pendingKind == WindowKind.FullscreenBorderless
                ? $"{DesktopSize.X}x{DesktopSize.Y}"
                : $"{_pendingRes.X}x{_pendingRes.Y}" + (_pendingRes != new Point(TankGame.Settings.ResWidth, TankGame.Settings.ResHeight) ? " *" : ""),
            dir => _pendingRes = Cycle(_resolutions, _pendingRes, dir),
            enabled: () => _pendingKind != WindowKind.FullscreenBorderless);

        VSyncBtn = Row(LeftX, RowY(2), lang.Settings.VSync ?? "Vertical Sync",
            lang.Settings.VSyncDesc ?? "Synchronizes the game with your monitor's refresh rate to stop screen tearing.",
            () => OnOff(TankGame.Settings.Vsync),
            _ => {
                TankGame.Settings.Vsync = !TankGame.Settings.Vsync;
                var graphics = TankGame.Instance.Graphics;
                var current = TankGame.Instance.GraphicsDevice.PresentationParameters;
                // keep the current size: ApplyChanges would otherwise pick up whatever stale size was last requested
                graphics.PreferredBackBufferWidth = current.BackBufferWidth;
                graphics.PreferredBackBufferHeight = current.BackBufferHeight;
                graphics.SynchronizeWithVerticalRetrace = TankGame.Settings.Vsync;
                graphics.ApplyChanges();
            });

        FrameLimitBtn = Row(LeftX, RowY(3), "Frame Rate Limit",
            "Caps the frame rate while VSync is off (VSync already limits it to your monitor's refresh rate).",
            () => TankGame.Settings.TargetFPS <= 0 ? "Unlimited" : $"{TankGame.Settings.TargetFPS} FPS",
            dir => TankGame.Settings.TargetFPS = Cycle(GraphicsSettings.FrameRateOptions, TankGame.Settings.TargetFPS, dir),
            enabled: () => !TankGame.Settings.Vsync);

        MSAABtn = Row(LeftX, RowY(4), "Anti-Aliasing",
            "Smooths jagged edges on 3D models (multisample anti-aliasing). Higher levels cost more GPU time.",
            () => TankGame.Settings.MSAASamples <= 0 ? "Off" : $"MSAA {TankGame.Settings.MSAASamples}x",
            dir => TankGame.Settings.MSAASamples = Cycle(GraphicsSettings.MSAAOptions, TankGame.Settings.MSAASamples, dir));

        PPLButton = Row(LeftX, RowY(5), lang.Settings.PerPxLight ?? "Per-Pixel Lighting",
            lang.Settings.PerPxLightDesc ?? "Lights every pixel instead of every vertex. Smoother lighting on models.",
            () => OnOff(TankGame.Settings.PerPixelLighting),
            _ => TankGame.Settings.PerPixelLighting = !TankGame.Settings.PerPixelLighting);

        FadeTracksBtn = Row(LeftX, RowY(6), lang.Settings.FadeTracks ?? "Fading Tank Tracks",
            lang.Settings.FadeTracksDesc ?? "Tank tracks slowly fade away. Saves performance in long missions.",
            () => OnOff(TankGame.Settings.FadeFootprints),
            _ => TankFootprint.ShouldTracksFade = TankGame.Settings.FadeFootprints = !TankGame.Settings.FadeFootprints);

        TrackLimitBtn = Row(LeftX, RowY(7), "Tank Track Limit",
            "How many tank tracks can stay on the ground before the oldest disappear.",
            () => $"{TankGame.Settings.TankFootprintLimit:N0}",
            dir => GraphicsSettings.SetFootprintLimit(Cycle(GraphicsSettings.FootprintLimitOptions,
                NearestOf(GraphicsSettings.FootprintLimitOptions, TankGame.Settings.TankFootprintLimit), dir)));

        MenuGameplayBtn = Row(LeftX, RowY(8), lang.Settings.MenuGameplay ?? "Menu Gameplay",
            lang.Settings.MenuGameplayDesc ?? "Tanks battle in the background of the main menu.",
            () => OnOff(TankGame.Settings.MenuGameplayEnabled),
            _ => {
                TankGame.Settings.MenuGameplayEnabled = !TankGame.Settings.MenuGameplayEnabled;
                if (!MainMenuUI.IsActive)
                    return;
                foreach (var tank in GameHandler.AllTanks)
                    tank?.Remove(true);
            });

        ApplyBtn = Page.Button(LeftX, RowY(9), (ColumnW - 20) / 2, "Apply",
            "Switches to the selected window mode and resolution.", ApplyDisplay, () => HasPendingDisplay, highlight: true);
        ResetBtn = Page.Button(LeftX + (ColumnW + 20) / 2, RowY(9), (ColumnW - 20) / 2, "Reset to Defaults",
            "Puts every option on this page back to its default (the display mode still needs Apply).", ResetToDefaults);

        // dynamic lighting
        LightingBtn = Row(RightX, RowY(0), "Dynamic Lighting",
            "Real-time sun, shadows, lamps and light beams. Turn it off for the original look (and the best performance).",
            () => OnOff(Lighting.Enabled) + (LightingSystem.IsAvailable || !Lighting.Enabled ? "" : " (unavailable)"),
            _ => {
                Lighting.Enabled = !Lighting.Enabled;
                ApplyLighting();
            });

        LightingQualityBtn = Row(RightX, RowY(1), "Lighting Quality",
            "Sets all the lighting options below at once. Changing one of them by hand makes this Custom.",
            () => Lighting.QualityLevel.ToString(),
            dir => {
                LightingQualityLevel[] levels = [LightingQualityLevel.Low, LightingQualityLevel.Medium, LightingQualityLevel.High, LightingQualityLevel.Ultra];
                var current = Lighting.QualityLevel == LightingQualityLevel.Custom ? LightingQualityLevel.High : Lighting.QualityLevel;
                Lighting.SetQualityLevel(Lighting.QualityLevel == LightingQualityLevel.Custom ? current : Cycle(levels, current, dir));
                ApplyLighting();
            },
            enabled: LightingOn);

        SunShadowsBtn = Row(RightX, RowY(2), "Sun Shadows",
            "Shadows from the sun and moon, on the board and across the room. Ultra doubles the resolution (needs a HiDef capable GPU).",
            () => QualityName(Lighting.SunShadows),
            dir => { Lighting.SunShadows = Cycle(Enum.GetValues<ShadowQuality>(), Lighting.SunShadows, dir); ApplyLighting(true); },
            enabled: LightingOn);

        LampShadowsBtn = Row(RightX, RowY(3), "Light Shadows",
            "Shadows from lamps, tank headlights and explosions. Higher settings shadow more lights with smoother edges.",
            () => QualityName(Lighting.LampShadows),
            dir => { Lighting.LampShadows = Cycle(Enum.GetValues<ShadowQuality>(), Lighting.LampShadows, dir); ApplyLighting(true); },
            enabled: LightingOn);

        LightShaftsBtn = Row(RightX, RowY(4), "Light Shafts",
            "Beams of sunlight through the windows. Higher settings render them at a higher resolution.",
            () => Lighting.LightShafts.ToString(),
            dir => { Lighting.LightShafts = Cycle(Enum.GetValues<EffectQuality>(), Lighting.LightShafts, dir); ApplyLighting(true); },
            enabled: LightingOn);

        LightCountBtn = Row(RightX, RowY(5), "Light Count",
            "How many lights can shine at once (lamps, headlights, shells, mines, explosions).",
            () => Lighting.LightCount switch {
                EffectQuality.Off => "Off",
                EffectQuality.Low => "Low (8)",
                EffectQuality.Medium => "Medium (16)",
                _ => "High (32)",
            },
            dir => { Lighting.LightCount = Cycle(Enum.GetValues<EffectQuality>(), Lighting.LightCount, dir); ApplyLighting(true); },
            enabled: LightingOn);

        RoomShadowsBtn = Row(RightX, RowY(6), "Room Shadows",
            "The room's walls, windows and furniture block the sun, so it only comes in through the windows. Off = the sun shines through the walls.",
            () => OnOff(Lighting.RoomShadows),
            _ => { Lighting.RoomShadows = !Lighting.RoomShadows; ApplyLighting(); },
            enabled: () => LightingOn() && Lighting.SunShadows != ShadowQuality.Off);

        TimeOfDayBtn = Row(RightX, RowY(7), "Time of Day",
            "The time of day in the room. Day/Night Cycle runs a clock: the sun moves and the colors change minute by minute.",
            () => Lighting.TimeOfDay == Preset.DayCycle
                ? $"Day/Night Cycle ({LightingPresets.HourText})"
                : Lighting.TimeOfDay.ToString().SplitByCamel(),
            dir => { Lighting.TimeOfDay = Cycle(_timesOfDay, Lighting.TimeOfDay, dir); ApplyLighting(); },
            enabled: LightingOn);

        DayLengthBtn = Row(RightX, RowY(8), "Day Length",
            "How long a full day takes in the Day/Night Cycle, in real time.",
            () => Lighting.DayLengthMinutes >= 24f * 60f - 1f ? "Real Time (24 hours)" : $"{Lighting.DayLengthMinutes:0.#} minutes",
            dir => { Lighting.DayLengthMinutes = Cycle(_dayLengths, NearestOf(_dayLengths, Lighting.DayLengthMinutes), dir); ApplyLighting(); },
            enabled: () => LightingOn() && Lighting.TimeOfDay == Preset.DayCycle);

        GameplayLightsBtn = Row(RightX, RowY(9), "Gameplay Lights",
            "Glows on shells, blinking mines and explosion flashes.",
            () => OnOff(Lighting.GameplayLights),
            _ => { Lighting.GameplayLights = !Lighting.GameplayLights; ApplyLighting(); },
            enabled: LightingOn);
    }

    /// <summary>Opens the settings window on this page, or closes the window.</summary>
    public static void SetVisibility(bool visibility) {
        if (visibility)
            SettingsUI.Open(SettingsUI.Video);
        else if (SettingsUI.IsOpen && SettingsUI.Current == SettingsUI.Video)
            SettingsUI.Close();
    }

    static void ResetPendingDisplay() {
        _pendingKind = TankGame.Settings.WindowKind;
        _pendingRes = new Point(TankGame.Settings.ResWidth, TankGame.Settings.ResHeight);
    }

    // actions

    static void ApplyDisplay() {
        if (!HasPendingDisplay)
            return;
        TankGame.Settings.WindowKind = _pendingKind;
        if (_pendingKind != WindowKind.FullscreenBorderless) {
            TankGame.Settings.ResWidth = _pendingRes.X;
            TankGame.Settings.ResHeight = _pendingRes.Y;
        }
        WindowUtils.ApplyDisplayMode(TankGame.Settings.WindowKind, TankGame.Settings.ResWidth, TankGame.Settings.ResHeight);
        CurrentRes = new(TankGame.Settings.ResWidth, TankGame.Settings.ResHeight);
    }

    static void ApplyLighting(bool qualityOptionChanged = false) {
        if (qualityOptionChanged)
            Lighting.UpdateQualityLevel();
        TankGame.Settings.Lighting = Lighting;
        Lighting.Apply();
    }

    static void ResetToDefaults() {
        var defaults = new GameConfig();
        _pendingKind = defaults.WindowKind;
        _pendingRes = new Point(defaults.ResWidth, defaults.ResHeight);

        if (TankGame.Settings.Vsync != defaults.Vsync)
            VSyncBtn.Step(1);
        TankGame.Settings.PerPixelLighting = defaults.PerPixelLighting;
        TankGame.Settings.TargetFPS = defaults.TargetFPS;
        TankGame.Settings.MSAASamples = defaults.MSAASamples;
        GraphicsSettings.SetFootprintLimit(defaults.TankFootprintLimit);
        TankFootprint.ShouldTracksFade = TankGame.Settings.FadeFootprints = defaults.FadeFootprints;
        if (TankGame.Settings.MenuGameplayEnabled != defaults.MenuGameplayEnabled)
            MenuGameplayBtn.Step(1);

        // keep the same settings object (LightingSettings.Current) and copy the defaults into it
        var d = new LightingSettings();
        Lighting.Enabled = d.Enabled;
        Lighting.TimeOfDay = d.TimeOfDay;
        Lighting.DayLengthMinutes = d.DayLengthMinutes;
        Lighting.GameplayLights = d.GameplayLights;
        Lighting.RoomShadows = d.RoomShadows;
        Lighting.SetQualityLevel(d.QualityLevel);
        ApplyLighting();
    }

    // ------------------------------------------------------------------------------------------ helpers

    static SettingRow Row(float x, float y, string label, string description, Func<string> value, Action<int> step, Func<bool>? enabled = null)
        => Page.Row(x, y, label, description, value, step, enabled);

    static bool LightingOn() => Lighting.Enabled;

    static string OnOff(bool value) => TankGame.GameLanguage.GetEnablement(value);

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
        list.Add(new Point(TankGame.Settings.ResWidth, TankGame.Settings.ResHeight));
        return list.Distinct().OrderBy(r => r.X).ThenBy(r => r.Y).ToArray();
    }

    /// <summary>The next (dir = 1) or previous (dir = -1) entry, wrapping around. Unknown values start from the first entry.</summary>
    static T Cycle<T>(T[] values, T current, int dir) {
        var index = Array.IndexOf(values, current);
        if (index < 0)
            return values[0];
        return values[((index + dir) % values.Length + values.Length) % values.Length];
    }

    static float NearestOf(float[] values, float value) => values.OrderBy(v => MathF.Abs(v - value)).First();
    static int NearestOf(int[] values, int value) => values.OrderBy(v => Math.Abs(v - value)).First();
}
