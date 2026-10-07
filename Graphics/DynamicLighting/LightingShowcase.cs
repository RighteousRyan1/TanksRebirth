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
using TanksRebirth.GameContent.Tanks;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common;
using Preset = TanksRebirth.Graphics.DynamicLighting.LightingPresets.Preset;

namespace TanksRebirth.Graphics.DynamicLighting;

/// <summary>
/// Connects the lighting to Tanks Rebirth: loads the effect, feeds it the room, tanks, shells, mines and explosions,
/// applies <see cref="LightingSettings.Current"/>, and adds hotkeys and a chat command.
/// </summary>
/// <remarks>
/// <para>The pieces, from engine to game:</para>
/// <list type="bullet">
/// <item><see cref="LightingSystem"/>, <see cref="Light"/> and friends, <see cref="LightCaptureEffect"/>: the renderer (MonoGame only)</item>
/// <item><see cref="LightingQuality"/>: how expensive the renderer may be</item>
/// <item><see cref="LightingPresets"/>: what the scene looks like (time of day, day cycle, room lamps)</item>
/// <item><see cref="GameplayLights"/>: what headlights, shells, mines and explosion lights look like</item>
/// <item><see cref="LightingSettings"/>: the options a graphics menu shows, mapped onto the above</item>
/// <item>this class: the game glue (game objects, room model, hotkeys, commands)</item>
/// </list>
/// <list type="bullet">
/// <item>F7 - cycle presets through the day (Sunrise ... Midnight, Blackout, DayCycle, Off)</item>
/// <item>F8 - before / after split screen (left half = original)</item>
/// <item>/lighting [preset name|off|split|stats|list], e.g. /lighting late afternoon</item>
/// <item>/lighting cycle, /lighting time [hour], /lighting daylength [minutes], /lighting pause - the 24 hour day cycle</item>
/// <item>/lighting quality [low|medium|high|ultra], /lighting enable, /lighting disable - the settings</item>
/// </list>
/// </remarks>
public static class LightingShowcase {
    public static bool ShellLights = true;
    public static bool ExplosionLights = true;
    public static bool MineLights = true;

    // allow this to be a config
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

        LightingSystem.Log = message => TankGame.ClientLog.Write(message, LogType.Warn);

        Effect effect;
        try {
            effect = GameResources.GetGameResource<Effect>("Assets/shaders/lighting/lighting");
        }
        catch (Exception e) {
            TankGame.ClientLog.Write($"[Lighting] lighting.fx could not be loaded, dynamic lighting is off: {e.Message}", LogType.Warn);
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

        TankGame.ClientLog.Write("[Lighting] Dynamic lighting ready. F7 = cycle presets, F8 = before/after split.", LogType.Info);
    }

    public static void Apply(Preset preset) {
        LightingPresets.Apply(preset, RoomScene.RoomSkyboxScene);
    }

    public static void Cycle() {
        Apply(LightingPresets.Next(Current));
        ChatSystem.SendMessage($"Lighting: {Current}", Color.Gold);
    }

    /// <summary>Hotkeys and animation. Call from the game's Update.</summary>
    public static void Update() {
        if (!_initialized)
            return;

        if (InputUtils.KeyJustPressed(Keys.F7))
            Cycle();
        if (InputUtils.KeyJustPressed(Keys.F8)) {
            LightingSystem.SplitScreen = LightingSystem.SplitScreen > 0f ? 0f : 0.5f;
            ChatSystem.SendMessage(LightingSystem.SplitScreen > 0f ? "Lighting: split view (left = original)" : "Lighting: split view off", Color.Gold);
        }

        // the day cycle preset advances its clock here
        LightingPresets.UpdateDayCycle((float)TankGame.LastGameTime.ElapsedGameTime.TotalSeconds, RoomScene.RoomSkyboxScene);
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
        var time = (float)TankGame.LastGameTime.TotalGameTime.TotalSeconds;

        if (LightingPresets.WantsHeadlights) {
            foreach (var tank in GameHandler.AllPlayerTanks) {
                if (tank is null || tank.IsDestroyed)
                    continue;
                GameplayLights.AddHeadlight(tank.TurretPosition3D - new Vector3(0, 5, 0), tank.TurretPosition - tank.Position, tank == PlayerTank.ClientTank);
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
                if (mine is null)
                    continue;
                GameplayLights.AddMineLight(mine.Position3D, mine.DetonateTime / MathF.Max(mine.DetonateTimeMax, 1f), time);
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
                        ChatSystem.SendMessage($"Lighting: day cycle at {LightingPresets.HourText}", Color.Gold);
                        return;
                    case "daylength":
                        if (parts.Length > 1 && float.TryParse(parts[1], System.Globalization.NumberStyles.Float, culture, out var minutes) && minutes > 0f)
                            LightingSettings.Current.DayLengthMinutes = minutes;
                        LightingSettings.Current.Apply();
                        ChatSystem.SendMessage($"Lighting: a day lasts {LightingPresets.DayLengthSeconds / 60f:0.#} minutes", Color.Gold);
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
                        ChatSystem.SendMessage($"Lighting: sun declination {LightingPresets.Declination:0.#} degrees, noon sun at {LightingPresets.SunElevation(LightingPresets.SolarNoon):0} degrees", Color.Gold);
                        return;
                    case "quality":
                        if (parts.Length > 1 && Enum.TryParse<LightingQualityLevel>(parts[1], ignoreCase: true, out var level)) {
                            LightingSettings.Current.SetQualityLevel(level);
                            LightingSettings.Current.Apply();
                        }
                        ChatSystem.SendMessage($"Lighting: quality {LightingSettings.Current.QualityLevel}", Color.Gold);
                        return;
                    case "enable":
                    case "disable":
                        LightingSettings.Current.Enabled = parts[0] == "enable";
                        LightingSettings.Current.Apply();
                        ChatSystem.SendMessage($"Lighting: dynamic lighting {(LightingSettings.Current.Enabled ? "on" : "off")}", Color.Gold);
                        return;
                    case "pause":
                        LightingPresets.DayCyclePaused = !LightingPresets.DayCyclePaused;
                        ChatSystem.SendMessage(LightingPresets.DayCyclePaused ? $"Lighting: day cycle paused at {LightingPresets.HourText}" : "Lighting: day cycle running", Color.Gold);
                        return;
                }

                switch (arg) {
                    case "":
                        Cycle();
                        return;
                    case "split":
                        LightingSystem.SplitScreen = LightingSystem.SplitScreen > 0f ? 0f : 0.5f;
                        break;
                    case "stats":
                        TankGame.IngameConsole.Log(LightingSystem.Stats.ToString(), Color.Gold);
                        return;
                    case "list":
                        ChatSystem.SendMessage("Lighting presets: " + string.Join(", ", Enum.GetNames<Preset>()), Color.Gold);
                        return;
                    default:
                        if (!LightingPresets.TryParse(arg, out var preset)) {
                            ChatSystem.SendMessage($"Unknown lighting preset '{arg}'. Try /lighting list", Color.Red);
                            return;
                        }
                        Apply(preset);
                        break;
                }
                var extra = Current == Preset.DayCycle ? $" ({LightingPresets.HourText}, a day lasts {LightingPresets.DayLengthSeconds / 60f:0.#} min)" : string.Empty;
                ChatSystem.SendMessage($"Lighting: {Current}{extra}" + (LightingSystem.SplitScreen > 0f ? " (split view)" : string.Empty), Color.Gold);
            });
    }
}
