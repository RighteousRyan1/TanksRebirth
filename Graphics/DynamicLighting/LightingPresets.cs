using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using TanksRebirth.GameContent.Tanks;

namespace TanksRebirth.Graphics.DynamicLighting;

/// <summary>
/// The look of each lighting preset, plus helpers that create the per-frame lights for gameplay objects.
/// Only depends on MonoGame and <see cref="LightingSystem"/>; <see cref="LightingShowcase"/> connects it to the game.
/// </summary>
public static class LightingPresets {
    /// <summary>Presets in time-of-day order (F7 cycles through them in this order).</summary>
    public enum Preset {
        Off,
        Sunrise,
        LateMorning,
        Midday,
        MidAfternoon,
        LateAfternoon,
        GoldenHour,
        Evening,
        Dusk,
        Midnight,
        Blackout,
        /// <summary>Runs through all 24 hours, blending between the time-of-day presets. See <see cref="DayLengthSeconds"/>.</summary>
        DayCycle,
    }

    public static Preset Current { get; private set; } = Preset.Off;

    /// <summary>Whether the current preset wants headlights on the player tanks.</summary>
    public static bool WantsHeadlights { get; private set; }
    /// <summary>Multiplier for gameplay lights (shells, mines, explosions, headlights).</summary>
    public static float DynamicBrightness { get; private set; } = 1f;

    // ---------------------------------------------------------------------------------- scene coordinates
    // The room model is drawn at scale 10 around RoomScene.TableScenePos * 10, the board sits on the table at y = 0.

    /// <summary>Center of the two windows on the wall behind the board (-Z).</summary>
    public static readonly Vector3 BackWindows = new(-262f, 1137f, -421f);
    /// <summary>Center of the two windows on the right wall (+X).</summary>
    public static readonly Vector3 SideWindows = new(2050f, 1137f, 1485f);

    // room lamps
    public static readonly PointLight TableLamp = new(new Vector3(794f, 285f, -74f), new Color(255, 190, 120), 0.5f, 850f, true) { Priority = 5, Wrap = 0.3f };
    public static readonly SpotLight DeskLamp = new(new Vector3(-1030f, 412f, 150f), new Vector3(880f, -412f, -240f), new Color(255, 228, 180), 0.9f, 1800f, 7f, 12f, true) { Priority = 6 };
    public static readonly PointLight FloorLamp = new(new Vector3(1532f, 1080f, 2828f), new Color(255, 190, 120), 1.0f, 2600f) { Priority = 1, Wrap = 0.4f };
    public static readonly Light[] RoomLamps = [TableLamp, DeskLamp, FloorLamp];

    /// <summary>Emissive look for the room's lamp shades while the lamps are on.</summary>
    public static readonly MeshLighting GlowingShade = new() { ReceivesLight = false, Emissive = new Vector3(1.7f, 1.45f, 1.05f) };
    static readonly string[] _shadeMeshes = ["Lamp_Shade", "Floor_Lamp_Bowl"];

    // ============================================================================================ presets

    // ============================================================================================ time of day keyframes

    /// <summary>Everything that changes with the time of day. The day cycle blends between these.</summary>
    public struct TimeOfDay {
        public Color Sky, Ground;
        public float Ambient;
        /// <summary>Direction the sun (or moon) light travels, normalized.</summary>
        public Vector3 SunDirection;
        public Color SunColor;
        public float SunIntensity, SunWrap;
        public Color ShaftColor;
        public float ShaftDensity, ShaftAnisotropy, ShaftMarch;
        public float ShadowRadius;
        /// <summary>Room lamp brightness, 0 = off.</summary>
        public float TableLamp, DeskLamp, FloorLamp;
        public float Background;
        public float DynamicBrightness;
        /// <summary>Above 0.5 the player tanks get headlights.</summary>
        public float Headlights;

        public static TimeOfDay Lerp(in TimeOfDay a, in TimeOfDay b, float t) {
            static float F(float x, float y, float t) => x + (y - x) * t;
            var dir = Vector3.Lerp(a.SunDirection, b.SunDirection, t);
            dir = dir.LengthSquared() > 1e-6f ? Vector3.Normalize(dir) : b.SunDirection;
            return new TimeOfDay {
                Sky = Color.Lerp(a.Sky, b.Sky, t),
                Ground = Color.Lerp(a.Ground, b.Ground, t),
                Ambient = F(a.Ambient, b.Ambient, t),
                SunDirection = dir,
                SunColor = Color.Lerp(a.SunColor, b.SunColor, t),
                SunIntensity = F(a.SunIntensity, b.SunIntensity, t),
                SunWrap = F(a.SunWrap, b.SunWrap, t),
                ShaftColor = Color.Lerp(a.ShaftColor, b.ShaftColor, t),
                ShaftDensity = F(a.ShaftDensity, b.ShaftDensity, t),
                ShaftAnisotropy = F(a.ShaftAnisotropy, b.ShaftAnisotropy, t),
                ShaftMarch = F(a.ShaftMarch, b.ShaftMarch, t),
                ShadowRadius = F(a.ShadowRadius, b.ShadowRadius, t),
                TableLamp = F(a.TableLamp, b.TableLamp, t),
                DeskLamp = F(a.DeskLamp, b.DeskLamp, t),
                FloorLamp = F(a.FloorLamp, b.FloorLamp, t),
                Background = F(a.Background, b.Background, t),
                DynamicBrightness = F(a.DynamicBrightness, b.DynamicBrightness, t),
                Headlights = F(a.Headlights, b.Headlights, t),
            };
        }
    }

    /// <summary>
    /// Builds a <see cref="TimeOfDay"/> with the sun travelling from <paramref name="sunFrom"/> towards <paramref name="sunTo"/>
    /// (pick a point in a window and a point on the board to make the light come through that window).
    /// Lamps are off, the background untouched; use <c>with { ... }</c> to change those.
    /// </summary>
    /// <param name="ambient">overall ambient strength. ambient + sun of about 1.0 - 1.2 looks like normal daylight, below ~0.4 reads as night</param>
    /// <param name="wrap">0 = hard light, 1 = very soft</param>
    /// <param name="shaftDensity">strength of the light beams, 0 turns them off</param>
    /// <param name="anisotropy">how much the beams glow when looking towards the sun (0 - 0.9)</param>
    public static TimeOfDay Day(Color sky, Color ground, float ambient, Vector3 sunFrom, Vector3 sunTo,
        Color sunColor, float sunIntensity, float wrap, Color shaftColor, float shaftDensity,
        float anisotropy = 0.3f, float marchLength = 1400f, float shadowRadius = 560f, float dynamicBrightness = 0.6f) => new() {
        Sky = sky, Ground = ground, Ambient = ambient,
        SunDirection = Vector3.Normalize(sunTo - sunFrom),
        SunColor = sunColor, SunIntensity = sunIntensity, SunWrap = wrap,
        ShaftColor = shaftColor, ShaftDensity = shaftDensity, ShaftAnisotropy = anisotropy, ShaftMarch = marchLength,
        ShadowRadius = shadowRadius,
        Background = 1f,
        DynamicBrightness = dynamicBrightness,
    };

    // The back windows (behind the board) sit high on the wall, so only a high sun reaches the board through them
    // (about 66 - 68 degrees fills the whole board). Low morning / evening sun comes in through the side windows instead.

    /// <summary>very low, pink-orange sun through the far side window, cool lavender room</summary>
    public static TimeOfDay Sunrise = Day(sky: new Color(122, 116, 150), ground: new Color(86, 80, 92), ambient: 0.85f,
        sunFrom: new Vector3(2050f, 650f, 2100f), sunTo: Vector3.Zero,
        sunColor: new Color(255, 168, 128), sunIntensity: 0.75f, wrap: 0.3f,
        shaftColor: new Color(255, 180, 150), shaftDensity: 0.22f, anisotropy: 0.55f, shadowRadius: 650f, dynamicBrightness: 0.8f);

    /// <summary>fresh, slightly cool light coming steeply through the left back window</summary>
    public static TimeOfDay LateMorning = Day(sky: new Color(150, 160, 180), ground: new Color(112, 108, 104), ambient: 0.95f,
        sunFrom: new Vector3(-760f, 1250f, -421f), sunTo: new Vector3(-60f, 0f, 60f),
        sunColor: new Color(255, 244, 228), sunIntensity: 0.6f, wrap: 0.1f,
        shaftColor: new Color(240, 240, 235), shaftDensity: 0.12f);

    /// <summary>the highest, whitest sun: short shadows, the whole board in light (~68 degrees)</summary>
    public static TimeOfDay Midday = Day(sky: new Color(158, 164, 176), ground: new Color(120, 116, 110), ambient: 1f,
        sunFrom: new Vector3(-90f, 1485f, -600f), sunTo: Vector3.Zero,
        sunColor: new Color(255, 252, 244), sunIntensity: 0.62f, wrap: 0.05f,
        shaftColor: new Color(250, 248, 240), shaftDensity: 0.1f, dynamicBrightness: 0.55f);

    /// <summary>warm high sun through the right back window; the frame between the panes throws a bar across the map</summary>
    public static TimeOfDay MidAfternoon = Day(sky: new Color(150, 156, 172), ground: new Color(118, 110, 102), ambient: 1f,
        sunFrom: new Vector3(430f, 1200f, -421f), sunTo: new Vector3(60f, 0f, 60f),
        sunColor: new Color(255, 234, 204), sunIntensity: 0.62f, wrap: 0.1f,
        shaftColor: new Color(255, 228, 185), shaftDensity: 0.14f);

    /// <summary>the sun has moved round to the side windows: warmer, longer shadows</summary>
    public static TimeOfDay LateAfternoon = Day(sky: new Color(140, 140, 160), ground: new Color(112, 100, 92), ambient: 0.95f,
        sunFrom: new Vector3(2050f, 1500f, 1000f), sunTo: Vector3.Zero,
        sunColor: new Color(255, 214, 160), sunIntensity: 0.72f, wrap: 0.2f,
        shaftColor: new Color(255, 210, 160), shaftDensity: 0.16f, anisotropy: 0.45f, shadowRadius: 600f, dynamicBrightness: 0.7f);

    /// <summary>low warm sun through the side windows, long shadows across the board</summary>
    public static TimeOfDay GoldenHour = Day(sky: new Color(122, 112, 140), ground: new Color(98, 82, 72), ambient: 0.95f,
        sunFrom: SideWindows + new Vector3(0f, -120f, -580f), sunTo: Vector3.Zero,
        sunColor: new Color(255, 166, 92), sunIntensity: 0.9f, wrap: 0.3f,
        shaftColor: new Color(255, 170, 100), shaftDensity: 0.2f, anisotropy: 0.5f, marchLength: 1800f, shadowRadius: 620f, dynamicBrightness: 0.8f);

    /// <summary>the sun is nearly down: a deep orange-red stripe skimming across the board</summary>
    public static TimeOfDay Evening = Day(sky: new Color(108, 94, 126), ground: new Color(80, 64, 60), ambient: 0.8f,
        sunFrom: new Vector3(2050f, 640f, 900f), sunTo: Vector3.Zero,
        sunColor: new Color(255, 128, 76), sunIntensity: 0.8f, wrap: 0.35f,
        shaftColor: new Color(255, 120, 70), shaftDensity: 0.25f, anisotropy: 0.6f, marchLength: 1800f, shadowRadius: 650f, dynamicBrightness: 0.9f);

    /// <summary>the sun is gone: only a soft purple sky glow through the back windows, the table lamp comes on</summary>
    public static TimeOfDay Dusk = Day(sky: new Color(96, 90, 138), ground: new Color(56, 50, 70), ambient: 0.62f,
        sunFrom: BackWindows, sunTo: Vector3.Zero,
        sunColor: new Color(150, 140, 205), sunIntensity: 0.2f, wrap: 0.6f,
        shaftColor: new Color(140, 130, 200), shaftDensity: 0.08f, dynamicBrightness: 1f)
        with { TableLamp = 0.4f, Background = 0.6f, Headlights = 1f };

    /// <summary>dark blue room, faint moonlight through the back windows, all the room lamps on</summary>
    public static TimeOfDay Midnight = Day(sky: new Color(48, 60, 100), ground: new Color(28, 28, 40), ambient: 0.32f,
        sunFrom: new Vector3(-382f, 1300f, -421f), sunTo: new Vector3(-262f, 0f, 59f),
        sunColor: new Color(130, 165, 255), sunIntensity: 0.16f, wrap: 0f,
        shaftColor: new Color(120, 150, 255), shaftDensity: 0.12f, dynamicBrightness: 1f)
        with { TableLamp = 0.5f, DeskLamp = 0.9f, FloorLamp = 1f, Background = 0.3f, Headlights = 1f };

    /// <summary>The static time-of-day presets.</summary>
    public static TimeOfDay? GetTimeOfDay(Preset preset) => preset switch {
        Preset.Sunrise => Sunrise,
        Preset.LateMorning => LateMorning,
        Preset.Midday => Midday,
        Preset.MidAfternoon => MidAfternoon,
        Preset.LateAfternoon => LateAfternoon,
        Preset.GoldenHour => GoldenHour,
        Preset.Evening => Evening,
        Preset.Dusk => Dusk,
        Preset.Midnight => Midnight,
        _ => null,
    };

    // ============================================================================================ day cycle

    /// <summary>Real seconds for one full 24 hour day in <see cref="Preset.DayCycle"/> (default: 8 minutes).</summary>
    public static float DayLengthSeconds = 480f;
    /// <summary>Freezes the day cycle clock.</summary>
    public static bool DayCyclePaused;
    /// <summary>Current hour of the day cycle, 0 - 24 (6.5 = 6:30 AM). Can be set to jump.</summary>
    public static float Hour {
        get => _hour;
        set => _hour = ((value % 24f) + 24f) % 24f;
    }
    static float _hour = 6f;

    /// <summary>
    /// Which preset is shown at which hour. The cycle blends between neighbours and wraps around midnight.
    /// Holding a preset for a while = list it twice (like Midnight at 0:00 and 4:30).
    /// </summary>
    public static readonly (float Hour, Preset Preset)[] DaySchedule = [
        (0f, Preset.Midnight),
        (4.5f, Preset.Midnight),
        (5.25f, Preset.Dusk),          // dawn twilight
        (6.25f, Preset.Sunrise),
        (7.25f, Preset.Sunrise),
        // between the side and back windows the sun is behind solid wall, so keep that crossover short
        (7.75f, Preset.LateMorning),
        (10f, Preset.LateMorning),
        (12.5f, Preset.Midday),
        (14.5f, Preset.MidAfternoon),
        (15.5f, Preset.MidAfternoon),
        (16f, Preset.LateAfternoon),   // back windows -> side windows (short crossover again)
        (17.25f, Preset.LateAfternoon),
        (18.25f, Preset.GoldenHour),
        (19.25f, Preset.Evening),
        (20.25f, Preset.Dusk),
        (22f, Preset.Midnight),
    ];

    /// <summary>The blended lighting at any hour of the day.</summary>
    public static TimeOfDay SampleDay(float hour) {
        hour = ((hour % 24f) + 24f) % 24f;
        var count = DaySchedule.Length;
        for (int i = 0; i < count; i++) {
            var (h0, p0) = DaySchedule[i];
            var (h1, p1) = DaySchedule[(i + 1) % count];
            if (i + 1 == count) h1 += 24f;      // last entry wraps to the first one tomorrow
            var h = hour < h0 ? hour + 24f : hour;
            if (h < h0 || h > h1)
                continue;
            var t = h1 > h0 ? (h - h0) / (h1 - h0) : 0f;
            t = t * t * (3f - 2f * t);          // ease in and out of each keyframe
            return TimeOfDay.Lerp(GetTimeOfDay(p0)!.Value, GetTimeOfDay(p1)!.Value, t);
        }
        return GetTimeOfDay(DaySchedule[0].Preset)!.Value;
    }

    /// <summary>Advances the day cycle clock and updates the lighting. Call every update.</summary>
    public static void UpdateDayCycle(float elapsedSeconds, Model? room = null) {
        if (Current != Preset.DayCycle)
            return;
        if (!DayCyclePaused && DayLengthSeconds > 0f)
            Hour = _hour + elapsedSeconds / DayLengthSeconds * 24f;
        ApplyTimeOfDay(SampleDay(_hour), room);
    }

    /// <summary>The current day cycle time as text, e.g. "6:30 AM".</summary>
    public static string HourText {
        get {
            var minutes = (int)(_hour * 60f) % (24 * 60);
            var h = minutes / 60;
            return $"{(h % 12 == 0 ? 12 : h % 12)}:{minutes % 60:00} {(h < 12 ? "AM" : "PM")}";
        }
    }

    // ============================================================================================ presets

    /// <summary>Configures <see cref="LightingSystem"/> for a preset. <paramref name="room"/> (optional) gets glowing lamp shades when the lamps are on.</summary>
    public static void Apply(Preset preset, Model? room = null) {
        Current = preset;

        LightingSystem.Enabled = preset != Preset.Off && LightingSystem.IsAvailable;
        LightingSystem.MaxShadowedSpotLights = 3;
        LightingSystem.FocusPoint = Vector3.Zero;
        var sun = LightingSystem.Sun;
        sun.ShadowCenter = Vector3.Zero;
        sun.ShadowDepth = 9000f;
        sun.CastsShadows = true;

        switch (preset) {
            case Preset.DayCycle:
                ApplyTimeOfDay(SampleDay(_hour), room);
                return;

            case Preset.Blackout:
                // almost nothing but what the tanks carry
                LightingSystem.Lights.Clear();
                SetLampShades(room, false);
                var ambient = LightingSystem.Ambient;
                ambient.Sky = new Color(26, 30, 48);
                ambient.Ground = new Color(14, 14, 20);
                ambient.Intensity = 0.45f;
                sun.Enabled = false;
                sun.Shafts.Enabled = false;
                LightingSystem.MaxShadowedPointLights = 4;
                LightingSystem.BackgroundLight = 0.1f;
                WantsHeadlights = true;
                DynamicBrightness = 1.3f;
                return;
        }

        if (GetTimeOfDay(preset) is TimeOfDay time)
            ApplyTimeOfDay(time, room);
        else {
            // Off
            LightingSystem.Lights.Clear();
            SetLampShades(room, false);
        }
    }

    static bool _shadesGlowing;

    /// <summary>Pushes a <see cref="TimeOfDay"/> into the lighting system. Cheap enough to call every frame.</summary>
    public static void ApplyTimeOfDay(in TimeOfDay time, Model? room = null) {
        var a = LightingSystem.Ambient;
        a.Sky = time.Sky;
        a.Ground = time.Ground;
        a.Intensity = time.Ambient;

        var sun = LightingSystem.Sun;
        sun.Enabled = time.SunIntensity > 0.001f;
        sun.Direction = time.SunDirection;
        sun.Color = time.SunColor;
        sun.Intensity = time.SunIntensity;
        sun.Wrap = time.SunWrap;
        sun.ShadowRadius = time.ShadowRadius;
        sun.Shafts.Enabled = time.ShaftDensity > 0.001f;
        sun.Shafts.Color = time.ShaftColor;
        sun.Shafts.Density = time.ShaftDensity;
        sun.Shafts.Anisotropy = time.ShaftAnisotropy;
        sun.Shafts.MarchLength = time.ShaftMarch;

        // room lamps fade in and out
        var lights = LightingSystem.Lights;
        lights.Clear();
        TableLamp.Intensity = time.TableLamp;
        DeskLamp.Intensity = time.DeskLamp;
        FloorLamp.Intensity = time.FloorLamp;
        foreach (var lamp in RoomLamps)
            if (lamp.Intensity > 0.01f)
                lights.Add(lamp);
        LightingSystem.MaxShadowedPointLights = lights.Count > 1 ? 5 : 4;

        var glowing = time.TableLamp > 0.2f;
        // (only touch the meshes when the lamps switch on or off)
        if (room is not null && (glowing != _shadesGlowing || Current != Preset.DayCycle)) {
            SetLampShades(room, glowing);
            _shadesGlowing = glowing;
        }

        LightingSystem.BackgroundLight = time.Background;
        WantsHeadlights = time.Headlights > 0.5f;
        DynamicBrightness = time.DynamicBrightness;
    }

    /// <summary>Lets you write <c>/lighting late afternoon</c>, <c>/lighting noon</c>... Returns false for unknown names.</summary>
    public static bool TryParse(string text, out Preset preset) {
        var key = text.Replace(" ", "").Replace("-", "").Replace("_", "").ToLowerInvariant();
        switch (key) {
            case "dawn": case "morning": preset = key == "dawn" ? Preset.Sunrise : Preset.LateMorning; return true;
            case "noon": case "day": preset = Preset.Midday; return true;
            case "afternoon": preset = Preset.MidAfternoon; return true;
            case "golden": case "sunset": preset = key == "golden" ? Preset.GoldenHour : Preset.Evening; return true;
            case "night": preset = Preset.Midnight; return true;
            case "dark": preset = Preset.Blackout; return true;
            case "cycle": case "day/night": case "daynight": case "24h": preset = Preset.DayCycle; return true;
        }
        return Enum.TryParse(key, ignoreCase: true, out preset);
    }

    /// <summary>The next preset in time-of-day order (wraps around to Off).</summary>
    public static Preset Next(Preset preset) {
        var next = (int)preset + 1;
        return Enum.IsDefined(typeof(Preset), next) ? (Preset)next : Preset.Off;
    }

    static void SetLampShades(Model? room, bool glowing) {
        if (room is null)
            return;
        foreach (var mesh in room.Meshes) {
            foreach (var name in _shadeMeshes) {
                if (!mesh.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (glowing) LightingSystem.SetMeshLighting(mesh, GlowingShade);
                else LightingSystem.ClearMeshLighting(mesh);
            }
        }
    }

    // === gameplay lights

    static readonly List<PointLight> _pointPool = [];
    static readonly List<SpotLight> _spotPool = [];
    static int _pointsUsed, _spotsUsed;

    /// <summary>Call once at the start of collecting a frame's gameplay lights (recycles pooled lights).</summary>
    public static void BeginGameplayLights() {
        _pointsUsed = 0;
        _spotsUsed = 0;
    }

    /// <summary>Headlight + soft glow for a tank. <paramref name="forward"/> is the turret direction on the XZ plane.</summary>
    public static void AddTankLights(Tank tank, Vector2 forward, Color playerColor, bool isLocalPlayer) {
        if (forward.LengthSquared() < 1e-6f)
            forward = new Vector2(0f, 1f);
        forward.Normalize();
        var forward3 = new Vector3(forward.X, 0f, forward.Y);

        var head = NextSpot();
        head.Position = tank.TurretPosition3D; // tank + new Vector3(0f, 17f, 0f) + forward3 * 4f;
        head.Direction = forward3 + new Vector3(0f, -0.32f, 0f);
        head.Color = new Color(255, 236, 196);
        head.Intensity = 1.7f * DynamicBrightness;
        head.Range = 340f;
        head.InnerAngle = MathHelper.ToRadians(5f);
        head.OuterAngle = MathHelper.ToRadians(50f);
        head.CastsShadows = true;
        head.Priority = isLocalPlayer ? 10 : 8;
        LightingSystem.AddFrameLight(head);

        /*var glow = NextPoint();
        glow.Position = tank.Position3D + new Vector3(0f, 26f, 0f);
        glow.Color = Color.Lerp(playerColor, Color.White, 0.55f);
        glow.Intensity = 0.55f * DynamicBrightness;
        glow.Range = 75f;
        glow.Wrap = 0.6f;
        glow.CastsShadows = false;
        glow.Priority = 3;
        LightingSystem.AddFrameLight(glow);*/
    }

    /// <summary>Small light travelling with a shell.</summary>
    public static void AddShellLight(Vector3 shellPosition, Color color) {
        var light = NextPoint();
        light.Position = shellPosition + new Vector3(0f, 4f, 0f);
        light.Color = color;
        light.Intensity = 0.9f * DynamicBrightness;
        light.Range = 70f;
        light.Wrap = 0.4f;
        light.CastsShadows = false;
        light.Priority = 1;
        LightingSystem.AddFrameLight(light);
    }

    /// <summary>Red blinking mine light; <paramref name="fuse"/> goes from 1 (just placed) to 0 (detonating).</summary>
    public static void AddMineLight(Vector3 minePosition, float fuse, float timeSeconds) {
        var urgency = 1f - MathHelper.Clamp(fuse, 0f, 1f);
        var rate = MathHelper.Lerp(2.5f, 14f, urgency * urgency);
        var blink = MathF.Pow(0.5f + 0.5f * MathF.Sin(timeSeconds * rate * MathF.PI + minePosition.X), 3f);
        var light = NextPoint();
        light.Position = minePosition + new Vector3(0f, 9f, 0f);
        light.Color = new Color(255, 40, 30);
        light.Intensity = (0.25f + 1.1f * blink) * DynamicBrightness;
        light.Range = 55f;
        light.Wrap = 0.5f;
        light.CastsShadows = false;
        light.Priority = 1;
        LightingSystem.AddFrameLight(light);
    }

    /// <summary>Shadow casting explosion flash. <paramref name="life"/> goes from 1 (new) to 0 (gone).</summary>
    public static void AddExplosionLight(Vector3 position, float radius, float life) {
        if (life <= 0f)
            return;
        var light = NextPoint();
        light.Position = position + new Vector3(0f, 14f, 0f);
        light.Color = new Color(255, 168, 82);
        light.Intensity = 1.9f * MathF.Pow(MathHelper.Clamp(life, 0f, 1f), 1.5f) * MathF.Max(DynamicBrightness, 0.8f);
        light.Range = 80f + radius * 3f;
        light.Wrap = 0.2f;
        light.CastsShadows = true;
        light.Priority = 9;
        LightingSystem.AddFrameLight(light);
    }

    static PointLight NextPoint() {
        if (_pointsUsed == _pointPool.Count)
            _pointPool.Add(new PointLight());
        var light = _pointPool[_pointsUsed++];
        light.Enabled = true;
        light.ShadowBias = 0.75f;
        return light;
    }

    static SpotLight NextSpot() {
        if (_spotsUsed == _spotPool.Count)
            _spotPool.Add(new SpotLight());
        var light = _spotPool[_spotsUsed++];
        light.Enabled = true;
        light.ShadowBias = 0.75f;
        return light;
    }
}
