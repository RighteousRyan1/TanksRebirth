using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using TanksRebirth.GameContent;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.Systems.CommandsSystem;
using TanksRebirth.GameContent.Systems.ParticleSystem;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.GameContent.Tanks;
using TanksRebirth.GameContent.UI.MainMenu;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Utilities;
using Preset = TanksRebirth.Graphics.DynamicLighting.LightingPresets.Preset;
using TanksRebirth.Net;

namespace TanksRebirth.Graphics.DynamicLighting;

/// <summary>
/// Manages lighting for the lighting system.
/// </summary>
public static class LightManager {
    public static bool ShellLights = true;
    public static bool ExplosionLights = true;
    public static bool MineLights = true;

    /// <summary>Lets the room (walls, window frames, curtains...) cast sun shadows even when it's off screen.</summary>
    public static bool RoomCastsShadows {
        get => _roomCastsShadows;
        set {
            _roomCastsShadows = value;
            if (_initialized && LightingSystem.IsAvailable) {
                LightingSystem.SetCastsShadows(RoomScene.RoomSkyboxScene, value);
                SetRoomSeals(value);
            }
        }
    }
    // change this default to start with room shadows off
    static bool _roomCastsShadows = true;

    public static Preset Current => LightingPresets.Current;
    static Matrix[] _roomBones = [];
    static bool _initialized;

    /// <summary>Loads the lighting effect and hooks everything up. If the effect is missing the game runs as before.</summary>
    public static void Initialize(GraphicsDevice device) {
        if (_initialized)
            return;
        _initialized = true;

        Effect effect;
        try {
            effect = GameResources.GetGameResource<Effect>("Assets/shaders/lighting/lighting");
        }
        catch (Exception e) {
            TankGame.ClientLog.Write($"[Lighting] lighting.fx could not be loaded, dynamic lighting is off: {e.Message}", RebirthLogLevel.Warn);
            return;
        }
        if (!LightingSystem.Initialize(device, effect))
            return;

        // the sky dome around the room should neither block nor receive light
        var room = RoomScene.RoomSkyboxScene;
        LightingSystem.Instrument(room);
        LightingSystem.SetMeshLighting(room, "Sphere", new MeshLighting { Ignore = true });
        LightingSystem.SetCastsShadows(room, _roomCastsShadows);
        SetRoomSeals(_roomCastsShadows);

        LightingSystem.CollectLights += CollectLights;
        LightingSystem.CollectShadowCasters += CollectShadowCasters;

        RegisterCommands();

        // the lighting options live in the game's settings file
        if (TankGame.Settings is not null)
            LightingSettings.Current = TankGame.Settings.Lighting ??= new LightingSettings();

        // keep the settings in sync when F7 or a command changes the time of day
        LightingPresets.PresetChanged += preset => LightingSettings.Current.TimeOfDay = preset;
        LightingSettings.Current.Apply(force: true);

        TankGame.ClientLog.Write("Lighting system loaded.", RebirthLogLevel.Info);
    }

    public static void Apply(Preset preset) {
        LightingPresets.Apply(preset, RoomScene.RoomSkyboxScene);
    }

    public static void Cycle() {
        Apply(LightingPresets.Next(Current));
        TankGame.IngameConsole.Log($"Lighting: {Current}", Color.Gold);
    }

    /// <summary>Hotkeys and animation. Call from the game's Update.</summary>
    public static void Update() {
        if (!_initialized)
            return;

        // allow only if debug mode is on
        if (InputUtils.KeyJustPressed(Keys.F7))
            Cycle();

        if (LightingSystem.ScreenSplitEnabled) {
            LightingSystem.ScreenSplitPercent = MathHelper.Clamp(MouseUtils.Test.X, 0, 1);
        }

        UpdateModifierLooks();

        // the day cycle preset advances its clock here
        LightingPresets.UpdateDayCycle((float)TankGame.LastGameTime.ElapsedGameTime.TotalSeconds, RoomScene.RoomSkyboxScene);
    }

    // modifiers

    /// <summary>Height of the lanterns above the tanks. Lower than the top of a tank and the tank shadows everything around it.</summary>
    public static float LanternHeight = 30f;
    /// <summary>How much of the time of day's light is left during a storm.</summary>
    public static float StormDarkness = 0.4f;
    public static Color StormSky = new(70, 80, 104);
    public static Color StormGround = new(36, 38, 46);
    public static Color LightningColor = new(196, 214, 255);
    /// <summary>Sun intensity of the brightest lightning.</summary>
    public static float LightningIntensity = 1.5f;

    static bool _storm, _lantern, _filtering;
    static Thunder? _lastStrike;
    static Vector3 _strikeDirection = Vector3.Down;

    static void UpdateModifierLooks() {
        _storm = Modifiers.IsOn(Modifiers.THUNDER) && !MainMenuUI.IsActive;
        _lantern = Modifiers.IsOn(Modifiers.LANTERN) && !MainMenuUI.IsActive;

        var filtering = _storm || _lantern;
        if (!filtering && !_filtering)
            return;
        _filtering = filtering;
        LightingPresets.Filter = filtering ? FilterLook : null;

        if (_storm && SceneManager.ThunderStrike != _lastStrike) {
            _lastStrike = SceneManager.ThunderStrike;
            _strikeDirection = PickStrikeDirection();
        }

        // static presets apply their look only once, so it's applied again while the modifiers change it (and once more to undo them)
        LightingPresets.Refresh(RoomScene.RoomSkyboxScene);
    }

    static void FilterLook(ref LightingPresets.TimeOfDay time) {
        if (_lantern) {
            // everything is dark, only the lanterns light the board
            time.Sky = new Color(22, 24, 38);
            time.Ground = new Color(10, 10, 14);
            time.Ambient = 0.3f;
            time.SunIntensity = 0f;
            time.ShaftDensity = 0f;
            time.TableLamp = time.DeskLamp = time.ClockLight = time.FloorLamp = 0f;
            time.Background = 0.05f;
            time.Headlights = 0f;
            time.DynamicBrightness = 1.3f;
        }

        if (!_storm)
            return;

        // overcast: no direct sun, and a darker, bluer version of the time of day
        time.Sky = Color.Lerp(time.Sky, StormSky, 0.6f);
        time.Ground = Color.Lerp(time.Ground, StormGround, 0.6f);
        time.Ambient *= StormDarkness;
        time.Background *= StormDarkness;
        if (!_lantern) {
            time.Headlights = 1f;
            time.DynamicBrightness = MathF.Max(time.DynamicBrightness, 1f);
        }

        // the lightning takes the place of the sun, so it casts shadows through the windows
        var flash = SceneManager.ThunderFlash;
        time.SunDirection = _strikeDirection;
        time.SunColor = LightningColor;
        time.SunIntensity = flash * LightningIntensity;
        time.SunWrap = 0.3f;
        time.ShadowRadius = 620f;
        time.ShaftColor = LightningColor;
        time.ShaftDensity = flash * 0.3f;
        time.ShaftAnisotropy = 0.4f;
        time.ShaftMarch = 1800f;
        time.Sky = Color.Lerp(time.Sky, LightningColor, flash * 0.5f);
        time.Ambient += flash * 0.5f;
        time.Background += flash * 0.8f;
    }

    // picks a lightning strike direction... should probably sync at some point
    static Vector3 PickStrikeDirection() {
        var rng = Client.ClientRandom;
        var to = new Vector3(rng.NextFloat(-200f, 200f), 0f, rng.NextFloat(-150f, 150f));
        var from = rng.Next(2) == 0
            ? LightingPresets.BackWindows + new Vector3(rng.NextFloat(-400f, 300f), rng.NextFloat(0f, 150f), 0f)
            : LightingPresets.SideWindows + new Vector3(0f, rng.NextFloat(0f, 350f), rng.NextFloat(-500f, 0f));
        return Vector3.Normalize(to - from);
    }

    static void CollectShadowCasters() {
        if (!RoomCastsShadows || !RoomScene.EnableDraw || !LightingSystem.Sun.Enabled)
            return;

        // the room is frustum culled by RoomScene, but its walls and windows still have to block the sun
        var room = RoomScene.RoomSkyboxScene;
        if (_roomBones.Length != room.Bones.Count)
            _roomBones = new Matrix[room.Bones.Count];
        room.CopyAbsoluteBoneTransformsTo(_roomBones);

        foreach (var mesh in room.Meshes) {
            if (mesh.Name.Contains("glass", StringComparison.OrdinalIgnoreCase))
                continue;
            LightingSystem.SubmitShadowCaster(mesh, _roomBones[mesh.ParentBone.Index]);
        }
    }

    static void CollectLights() {
        GameplayLights.BeginFrame();

        var anySortOfTankLighting = LightingPresets.HeadlightsEnabled || _lantern;
        if (anySortOfTankLighting) {
            var lights = LightingPresets.HeadlightsEnabled;
            foreach (var tank in GameHandler.AllPlayerTanks) {
                if (tank is null || tank.IsDestroyed) continue;

                if (lights) GameplayLights.AddHeadlight(tank.TurretPosition3D - new Vector3(0, 5, 0), tank.TurretPosition - tank.Position, tank == PlayerTank.ClientTank);
                if (_lantern) {
                    // RuntimeData.RunTime produces weird results
                    var time = (float)TankGame.LastGameTime.TotalGameTime.TotalSeconds;
                    GameplayLights.AddLantern(tank.Position3D + new Vector3(0, LanternHeight, 0), time + tank.PlayerId * 1.7f, tank == PlayerTank.ClientTank);
                }
            }
        }

        if (ShellLights) {
            foreach (var shell in Shell.AllShells) {
                if (shell is null) continue;
                if (!shell.Properties.Visuals.HasFlag(VisualFlags.Flaming)) continue;

                var color = Color.Orange;
                GameplayLights.AddShellLight(shell.Position3D - Vector3.Normalize(shell.Velocity3D) * 20f, color);
            }
        }

        if (MineLights) {
            foreach (var mine in Mine.AllMines) {
                if (mine is null) continue;
                if (!mine.RedTick) continue;

                // only creates a light when flashing red
                GameplayLights.AddMineLight(mine);
            }
        }

        if (ExplosionLights) {
            foreach (var explosion in Explosion.Explosions) {
                if (explosion is null)
                    continue;
                var life = 1f - explosion.LifeTime / MathF.Max(explosion.LingerDuration, 1f);
                GameplayLights.AddExplosionLight(explosion.Position3D, explosion.MaxScale * Explosion.MAGIC_EXPLOSION_NUMBER, life);
            }
        }
    }

    /// <summary>
    /// The room model has small gaps where the walls meet the ceiling, which let thin lines of sun through when the
    /// sun is in the right spot. These invisible blockers sit in the walls above the windows and on top of the ceiling to seal them.
    /// (Only while the room casts shadows: without walls they would just block the sun.)
    /// </summary>
    static void SetRoomSeals(bool enabled) {
        LightingSystem.SunBlockers.Clear();
        if (!enabled)
            return;
        const float ceiling = 2131f, top = 2400f, sealFrom = 2000f, overlap = 6f;
        // the outer walls are at x = -3500 / 1986 and z = -358 / 3300, the windows sit ~60 further out
        float west = -3500f, east = 1986f, back = -358f, front = 3300f, depth = 70f;
        // a lid just above the ceiling, no wider than the walls (wider would cut off sun coming in through the windows)
        LightingSystem.SunBlockers.Add(new BoundingBox(new Vector3(west - overlap, ceiling, back - overlap), new Vector3(east + overlap, top, front + overlap)));
        // thin bands in the walls above the windows, covering the gap between the top of the walls and the ceiling
        LightingSystem.SunBlockers.Add(new BoundingBox(new Vector3(east - overlap, sealFrom, back - depth), new Vector3(east + depth, top, front + depth)));
        LightingSystem.SunBlockers.Add(new BoundingBox(new Vector3(west - depth, sealFrom, back - depth), new Vector3(east + depth, top, back + overlap)));
        LightingSystem.SunBlockers.Add(new BoundingBox(new Vector3(west - depth, sealFrom, back - depth), new Vector3(west + overlap, top, front + depth)));
        LightingSystem.SunBlockers.Add(new BoundingBox(new Vector3(west - depth, sealFrom, front - overlap), new Vector3(east + depth, top, front + depth)));
    }

    static void RegisterCommands() {
        CommandGlobals.Commands[new CommandInput("lighting", "Dynamic lighting: a time of day (sunrise, late morning, midday, mid afternoon, late afternoon, golden hour, evening, dusk, midnight), blackout, cycle, time [hour], daylength [minutes], season [summer/spring/winter], pause, quality [low/medium/high/ultra], enable, disable, off, split, stats, list.")] =
            new CommandOutput(netSync: false, requireCheats: false, args => {
                var arg = string.Join(' ', args).Trim().ToLowerInvariant();
                var parts = arg.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var culture = System.Globalization.CultureInfo.InvariantCulture;

                // day cycle controls
                switch (parts.Length > 0 ? parts[0] : string.Empty) {
                    case "time":
                        if (parts.Length > 1 && float.TryParse(parts[1].Replace(':', '.'), System.Globalization.NumberStyles.Float, culture, out var hour)) {
                            // "14.5" or "14:30" both work
                            if (parts[1].Contains(':')) hour = MathF.Floor(hour) + (hour - MathF.Floor(hour)) * 100f / 60f;
                            LightingPresets.Hour = hour;
                            if (Current != Preset.DayCycle) Apply(Preset.DayCycle);
                        }
                        TankGame.IngameConsole.Log($"Lighting: day cycle at {LightingPresets.HourText}", Color.Gold);
                        return;
                    case "daylength":
                        if (parts.Length > 1 && float.TryParse(parts[1], System.Globalization.NumberStyles.Float, culture, out var minutes) && minutes > 0f)
                            LightingSettings.Current.DayLengthMinutes = minutes;
                        LightingSettings.Current.Apply();
                        TankGame.IngameConsole.Log($"Lighting: a day lasts {LightingPresets.DayLengthSeconds / 60f:0.#} minutes", Color.Gold);
                        return;
                    case "season":
                        // summer = long day / high sun, winter = short day / low sun, or a declination in degrees
                        if (parts.Length > 1) {
                            LightingPresets.Declination = parts[1] switch {
                                "summer" => 23f,
                                "spring" or "autumn" or "fall" => 0f,
                                "winter" => -23f,
                                _ => float.TryParse(parts[1], System.Globalization.NumberStyles.Float, culture, out var d) ? MathHelper.Clamp(d, -23.5f, 23.5f) : LightingPresets.Declination,
                            };
                        }
                        TankGame.IngameConsole.Log($"Lighting: sun declination {LightingPresets.Declination:0.#} degrees, noon sun at {LightingPresets.SunElevation(LightingPresets.SolarNoon):0} degrees", Color.Gold);
                        return;
                    case "quality":
                        if (parts.Length > 1 && Enum.TryParse<LightingQualityLevel>(parts[1], ignoreCase: true, out var level)) {
                            LightingSettings.Current.SetQualityLevel(level);
                            LightingSettings.Current.Apply();
                        }
                        TankGame.IngameConsole.Log($"Lighting: quality {LightingSettings.Current.QualityLevel}", Color.Gold);
                        return;
                    case "test":
                        LightingStressTest.Toggle();
                        TankGame.IngameConsole.Log(LightingStressTest.IsRunning
                            ? $"Lighting test on ({LightingSystem.MAX_SHADOW_SLOTS} point and {LightingSystem.MAX_SHADOW_SLOTS} spot lights, quality {LightingSettings.Current.QualityLevel})"
                            : "Lighting test off", Color.Gold);
                        return;
                    case "enable":
                    case "disable":
                        LightingSettings.Current.Enabled = parts[0] == "enable";
                        LightingSettings.Current.Apply();
                        TankGame.IngameConsole.Log($"Lighting: dynamic lighting {(LightingSettings.Current.Enabled ? "on" : "off")}", Color.Gold);
                        return;
                    case "pause":
                        LightingPresets.DayCyclePaused = !LightingPresets.DayCyclePaused;
                        TankGame.IngameConsole.Log(LightingPresets.DayCyclePaused ? $"Lighting: day cycle paused at {LightingPresets.HourText}" : "Lighting: day cycle running", Color.Gold);
                        return;
                }

                switch (arg) {
                    case "":
                        Cycle();
                        return;
                    case "split":
                        LightingSystem.ScreenSplitEnabled = !LightingSystem.ScreenSplitEnabled;
                        if (!LightingSystem.ScreenSplitEnabled)
                            LightingSystem.ScreenSplitPercent = 0;
                        TankGame.PlayerMice[0].IsActive = !LightingSystem.ScreenSplitEnabled;
                        break;
                    case "stats":
                        TankGame.IngameConsole.Log(LightingSystem.Stats.ToString(), Color.Gold);
                        return;
                    case "list":
                        TankGame.IngameConsole.Log("Lighting presets: " + string.Join(", ", Enum.GetNames<Preset>()), Color.Gold);
                        return;
                    default:
                        if (!LightingPresets.TryParse(arg, out var preset)) {
                            TankGame.IngameConsole.Log($"Unknown lighting preset '{arg}'. Try /lighting list", Color.Red);
                            return;
                        }
                        Apply(preset);
                        break;
                }
                var extra = Current == Preset.DayCycle ? $" ({LightingPresets.HourText}, a day lasts {LightingPresets.DayLengthSeconds / 60f:0.#} min)" : string.Empty;
                TankGame.IngameConsole.Log($"Lighting: {Current}{extra}" + (LightingSystem.ScreenSplitPercent > 0f ? " (split view)" : string.Empty), Color.Gold);
            });
    }
}
