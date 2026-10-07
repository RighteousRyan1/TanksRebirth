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
/// Connects <see cref="LightingSystem"/> and <see cref="LightingPresets"/> to Tanks Rebirth: loads the effect,
/// feeds it the room, tanks, shells, mines and explosions, and adds hotkeys and a chat command.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>F7 - cycle presets through the day (Sunrise ... Midnight, Blackout, Off)</item>
/// <item>F8 - before / after split screen (left half = original)</item>
/// <item>/lighting [preset name|off|split|stats|list], e.g. /lighting late afternoon</item>
/// <item>/lighting cycle, /lighting time [hour], /lighting daylength [minutes], /lighting pause - the 24 hour day cycle</item>
/// </list>
/// </remarks>
public static class LightingShowcase {
    /// <summary>The preset applied when the game starts.</summary>
    public static Preset StartupPreset = Preset.MidAfternoon;

    public static bool ShellLights = true;
    public static bool ExplosionLights = true;
    public static bool MineLights = true;

    // allow this to be a config
    /// <summary>Lets the room (walls, window frames, curtains...) cast sun shadows even when it's off screen.</summary>
    public static bool RoomCastsShadows {
        get => _roomCastsShadows;
        set {
            _roomCastsShadows = value;
            if (_initialized && LightingSystem.IsAvailable)
                LightingSystem.SetCastsShadows(RoomScene.RoomSkyboxScene, value);
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

        LightingSystem.CollectLights += CollectLights;
        LightingSystem.CollectShadowCasters += CollectShadowCasters;

        RegisterCommands();
        Apply(StartupPreset);

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

    static Color PlayerColor(PlayerTank player)
        => PlayerID.PlayerTankColors[Math.Clamp(player.PlayerId, 0, PlayerID.PlayerTankColors.Length - 1)];

    static void CollectLights() {
        LightingPresets.BeginGameplayLights();
        var time = (float)TankGame.LastGameTime.TotalGameTime.TotalSeconds;

        if (LightingPresets.WantsHeadlights) {
            foreach (var tank in GameHandler.AllPlayerTanks) {
                if (tank is null || tank.IsDestroyed)
                    continue;
                LightingPresets.AddTankLights(tank, tank.TurretPosition - tank.Position, PlayerColor(tank), tank == PlayerTank.ClientTank);
            }
        }

        if (ShellLights) {
            foreach (var shell in Shell.AllShells) {
                if (shell is null)
                    continue;
                var color = shell.Owner is PlayerTank player
                    ? Color.Lerp(PlayerColor(player), Color.White, 0.35f)
                    : new Color(255, 150, 70);
                LightingPresets.AddShellLight(shell.Position3D, color);
            }
        }

        if (MineLights) {
            foreach (var mine in Mine.AllMines) {
                if (mine is null)
                    continue;
                LightingPresets.AddMineLight(mine.Position3D, mine.DetonateTime / MathF.Max(mine.DetonateTimeMax, 1f), time);
            }
        }

        if (ExplosionLights) {
            foreach (var explosion in Explosion.Explosions) {
                if (explosion is null)
                    continue;
                var life = 1f - explosion.LifeTime / MathF.Max(explosion.LingerDuration, 1f);
                LightingPresets.AddExplosionLight(explosion.Position3D, explosion.MaxScale * Explosion.MAGIC_EXPLOSION_NUMBER, life);
            }
        }
    }

    static void RegisterCommands() {
        CommandGlobals.Commands[new CommandInput("lighting", "Dynamic lighting: a time of day (sunrise, late morning, midday, mid afternoon, late afternoon, golden hour, evening, dusk, midnight), blackout, cycle, time [hour], daylength [minutes], pause, off, split, stats, list.")] =
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
                            LightingPresets.DayLengthSeconds = minutes * 60f;
                        ChatSystem.SendMessage($"Lighting: a day lasts {LightingPresets.DayLengthSeconds / 60f:0.#} minutes", Color.Gold);
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
