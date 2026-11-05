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
        Preset.GoldenHour, Preset.Evening, Preset.Dusk, Preset.Midnight, Preset.Blackout, Preset.DayCycle, Preset.Faithful,
    ];
    static readonly float[] _dayLengths = [2f, 5f, 8f, 15f, 30f, 60f, 24f * 60f];

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

        var lang = TankGame.GameLanguage;

        Page.Header(lang.Settings.HeaderDisplay, DisplayX, width: ColumnW);
        Page.Header(lang.Settings.HeaderLighting, LightingX, width: ColumnW);
        Page.Header(lang.Settings.HeaderQuality, QualityX, width: ColumnW);

        // display
        WinKindBtn = Row(DisplayX, RowY(0), lang.Settings.WindowKind,
            lang.Settings.WindowModeDesc,
            () => WindowKindName(_pendingKind) + (_pendingKind != TankGame.Settings.Display.WindowKind ? " *" : ""),
            dir => _pendingKind = GameUI.Cycle(_windowKinds, _pendingKind, dir));

        ResBtn = Row(DisplayX, RowY(1), lang.Settings.Resolution,
            lang.Settings.ResolutionInfoDesc,
            () => _pendingKind == WindowKind.FullscreenBorderless
                ? $"{DesktopSize.X}x{DesktopSize.Y}"
                : $"{_pendingRes.X}x{_pendingRes.Y}" + (_pendingRes != new Point(TankGame.Settings.Display.ResWidth, TankGame.Settings.Display.ResHeight) ? " *" : ""),
            dir => _pendingRes = GameUI.Cycle(_resolutions, _pendingRes, dir),
            enabled: () => _pendingKind != WindowKind.FullscreenBorderless);

        VSyncBtn = Row(DisplayX, RowY(2), lang.Settings.VSync,
            lang.Settings.VSyncDesc,
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

        FrameLimitBtn = Row(DisplayX, RowY(3), lang.Settings.FrameLimit,
            lang.Settings.FrameLimitDesc,
            () => TankGame.Settings.Display.TargetFPS <= 0 ? lang.Settings.Unlimited : $"{TankGame.Settings.Display.TargetFPS} FPS",
            dir => TankGame.Settings.Display.TargetFPS = GameUI.Cycle(GraphicsSettings.FrameRateOptions, TankGame.Settings.Display.TargetFPS, dir),
            enabled: () => !TankGame.Settings.Display.Vsync);

        ApplyBtn = Page.Button(DisplayX, RowY(5), ColumnW, lang.Settings.Apply,
            lang.Settings.ApplyDesc, ApplyDisplay, () => HasPendingDisplay, highlight: false);
        ResetBtn = Page.Button(DisplayX, RowY(6), ColumnW, lang.Settings.ResetDefaults,
            lang.Settings.ResetPageDesc, ResetToDefaults);

        // dynamic lighting
        LightingBtn = Row(LightingX, RowY(0), lang.Settings.DynamicLighting,
            lang.Settings.DynamicLightingDesc,
            () => GameUI.OnOff(DynLightSettings.Current.Enabled) + (LightingSystem.IsAvailable || !DynLightSettings.Current.Enabled ? "" : $" ({lang.Settings.Unavailable})"),
            _ => {
                DynLightSettings.Current.Enabled = !DynLightSettings.Current.Enabled;
                ApplyLighting();
            });

        LightingQualityBtn = Row(LightingX, RowY(1), lang.Settings.LightingQuality,
            lang.Settings.LightingQualityDesc,
            () => LevelName(DynLightSettings.Current.QualityLevel),
            dir => {
                LightingQualityLevel[] levels = [LightingQualityLevel.Low, LightingQualityLevel.Medium, LightingQualityLevel.High, LightingQualityLevel.Ultra];
                var current = DynLightSettings.Current.QualityLevel == LightingQualityLevel.Custom ? LightingQualityLevel.High : DynLightSettings.Current.QualityLevel;
                DynLightSettings.Current.SetQualityLevel(DynLightSettings.Current.QualityLevel == LightingQualityLevel.Custom ? current : GameUI.Cycle(levels, current, dir));
                ApplyLighting();
            },
            enabled: LightingOn);

        SunShadowsBtn = Row(LightingX, RowY(2), lang.Settings.SunShadows,
            lang.Settings.SunShadowsDesc,
            () => QualityName(DynLightSettings.Current.SunShadows),
            dir => { DynLightSettings.Current.SunShadows = GameUI.  Cycle(Enum.GetValues<ShadowQuality>(), DynLightSettings.Current.SunShadows, dir); ApplyLighting(true); },
            enabled: LightingOn);

        LampShadowsBtn = Row(LightingX, RowY(3), lang.Settings.LightShadows,
            lang.Settings.LightShadowsDesc,
            () => QualityName(DynLightSettings.Current.LocalShadows),
            dir => { DynLightSettings.Current.LocalShadows = GameUI.Cycle(Enum.GetValues<ShadowQuality>(), DynLightSettings.Current.LocalShadows, dir); ApplyLighting(true); },
            enabled: LightingOn);

        LightShaftsBtn = Row(LightingX, RowY(4), lang.Settings.LightShafts,
            lang.Settings.LightShaftsDesc,
            () => LevelName(DynLightSettings.Current.LightShafts),
            dir => { DynLightSettings.Current.LightShafts = GameUI.Cycle(Enum.GetValues<EffectQuality>(), DynLightSettings.Current.LightShafts, dir); ApplyLighting(true); },
            enabled: LightingOn);

        LightCountBtn = Row(LightingX, RowY(5), lang.Settings.LightCount,
            lang.Settings.LightCountDesc,
            () => DynLightSettings.Current.LightCount switch {
                EffectQuality.Off => LevelName(EffectQuality.Off),
                EffectQuality.Low => $"{LevelName(EffectQuality.Low)} (8)",
                EffectQuality.Medium => $"{LevelName(EffectQuality.Medium)} (16)",
                _ => $"{LevelName(EffectQuality.High)} (32)",
            },
            dir => { DynLightSettings.Current.LightCount = GameUI.Cycle(Enum.GetValues<EffectQuality>(), DynLightSettings.Current.LightCount, dir); ApplyLighting(true); },
            enabled: LightingOn);

        RoomShadowsBtn = Row(LightingX, RowY(6), lang.Settings.RoomShadows,
            lang.Settings.RoomShadowsDesc,
            () => GameUI.OnOff(DynLightSettings.Current.RoomShadows),
            _ => { DynLightSettings.Current.RoomShadows = !DynLightSettings.Current.RoomShadows; ApplyLighting(); },
            enabled: () => LightingOn() && DynLightSettings.Current.SunShadows != ShadowQuality.Off);

        TimeOfDayBtn = Row(LightingX, RowY(7), lang.Settings.TimeOfDay,
            lang.Settings.TimeOfDayDesc,
            () => DynLightSettings.Current.TimeOfDay == Preset.DayCycle
                ? $"{TimeName(Preset.DayCycle)} ({LightingPresets.HourText})"
                : TimeName(DynLightSettings.Current.TimeOfDay),
            dir => { DynLightSettings.Current.TimeOfDay = GameUI.Cycle(_timesOfDay, DynLightSettings.Current.TimeOfDay, dir); ApplyLighting(); },
            enabled: LightingOn);

        DayLengthBtn = Row(LightingX, RowY(8), lang.Settings.DayLength,
            lang.Settings.DayLengthDesc,
            () => DynLightSettings.Current.DayLengthMinutes >= 24f * 60f - 1f
                ? lang.Settings.RealTime
                : string.Format(lang.Settings.DayLengthMinutes, DynLightSettings.Current.DayLengthMinutes.ToString("0.#")),
            dir => { DynLightSettings.Current.DayLengthMinutes = GameUI.Cycle(_dayLengths, GameUI.NearestOf(_dayLengths, DynLightSettings.Current.DayLengthMinutes), dir); ApplyLighting(); },
            enabled: () => LightingOn() && DynLightSettings.Current.TimeOfDay == Preset.DayCycle);

        GameplayLightsBtn = Row(LightingX, RowY(9), lang.Settings.GameplayLights,
            lang.Settings.GameplayLightsDesc,
            () => GameUI.OnOff(DynLightSettings.Current.GameplayLights),
            _ => { DynLightSettings.Current.GameplayLights = !DynLightSettings.Current.GameplayLights; ApplyLighting(); },
            enabled: LightingOn);

        // graphics quality
        MSAABtn = Row(QualityX, RowY(0), lang.Settings.AntiAliasing,
            lang.Settings.AntiAliasingDesc,
            () => TankGame.Settings.Graphics.MSAASamples <= 0 ? lang.Settings.Off : $"MSAA {TankGame.Settings.Graphics.MSAASamples}x",
            dir => TankGame.Settings.Graphics.MSAASamples = GameUI.Cycle(GraphicsSettings.MSAAOptions, TankGame.Settings.Graphics.MSAASamples, dir));

        PPLButton = Row(QualityX, RowY(1), lang.Settings.PerPxLight,
            lang.Settings.PerPxLightDesc,
            () => GameUI.OnOff(TankGame.Settings.Graphics.PerPixelLighting),
            _ => TankGame.Settings.Graphics.PerPixelLighting = !TankGame.Settings.Graphics.PerPixelLighting);

        RainyWindowsBtn = Row(QualityX, RowY(2), lang.Settings.RainyWindows,
            lang.Settings.RainyWindowsDesc,
            () => GameUI.OnOff(TankGame.Settings.Graphics.RainyWindows),
            _ => { TankGame.Settings.Graphics.RainyWindows = !TankGame.Settings.Graphics.RainyWindows; GraphicsSettings.ApplyEffects(); });

        RainStrengthBtn = Row(QualityX, RowY(3), lang.Settings.RainStrength,
            lang.Settings.RainStrengthDesc,
            () => RainStrengthName(
                Array.IndexOf(
                    GraphicsSettings.RainStrengthOptions,
                    GameUI.NearestOf(GraphicsSettings.RainStrengthOptions, TankGame.Settings.Graphics.RainStrength)
                    )),
            dir => {
                TankGame.Settings.Graphics.RainStrength = GameUI.Cycle(GraphicsSettings.RainStrengthOptions,
                    GameUI.NearestOf(GraphicsSettings.RainStrengthOptions, TankGame.Settings.Graphics.RainStrength), dir);
                GraphicsSettings.ApplyEffects();
            },
            enabled: () => TankGame.Settings.Graphics.RainyWindows);

        BlobShadowsBtn = Row(QualityX, RowY(4), lang.Settings.BlobShadows,
            lang.Settings.BlobShadowsDesc,
            () => GameUI.OnOff(TankGame.Settings.Graphics.TankBlobShadows),
            _ => { TankGame.Settings.Graphics.TankBlobShadows = !TankGame.Settings.Graphics.TankBlobShadows; GraphicsSettings.ApplyEffects(); });

        MenuBlurBtn = Row(QualityX, RowY(5), lang.Settings.MenuBlur,
            lang.Settings.MenuBlurDesc,
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
            ? TankGame.GameLanguage.Settings.UltraNoHiDef
            : LevelName(quality);

    /// <summary>The name of a quality level. Works for any of the quality enums since they share their names.</summary>
    static string LevelName<T>(T level) where T : Enum {
        var settings = TankGame.GameLanguage.Settings;
        return level.ToString() switch {
            "Off" => settings.Off,
            "Low" => settings.QualityLow,
            "Medium" => settings.QualityMedium,
            "High" => settings.QualityHigh,
            "Ultra" => settings.QualityUltra,
            _ => settings.QualityCustom,
        };
    }

    static string TimeName(Preset preset) {
        var settings = TankGame.GameLanguage.Settings;
        return preset switch {
            Preset.Sunrise => settings.TimeSunrise,
            Preset.LateMorning => settings.TimeLateMorning,
            Preset.Midday => settings.TimeMidday,
            Preset.MidAfternoon => settings.TimeMidAfternoon,
            Preset.LateAfternoon => settings.TimeLateAfternoon,
            Preset.GoldenHour => settings.TimeGoldenHour,
            Preset.Evening => settings.TimeEvening,
            Preset.Dusk => settings.TimeDusk,
            Preset.Midnight => settings.TimeMidnight,
            Preset.Blackout => settings.TimeBlackout,
            Preset.DayCycle => settings.TimeDayCycle,
            _ => preset.ToString().SplitByCamel(),
        };
    }

    static string RainStrengthName(int index) {
        var settings = TankGame.GameLanguage.Settings;
        return index switch {
            0 => settings.RainLight,
            1 => settings.RainMedium,
            _ => settings.RainHeavy,
        };
    }

    static string WindowKindName(WindowKind kind) {
        var settings = TankGame.GameLanguage.Settings;
        return kind switch {
            WindowKind.FullscreenBorderless => settings.WindowBorderless,
            WindowKind.Fullscreen => settings.WindowFullscreen,
            _ => settings.WindowWindowed,
        };
    }

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
