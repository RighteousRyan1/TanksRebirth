using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using TanksRebirth.GameContent;

namespace TanksRebirth.Graphics.DynamicLighting;

/// <summary>
/// The look of the scene: the time-of-day presets, the day cycle, the room lamps. Says nothing about performance
/// (that's <see cref="LightingQuality"/>) and nothing about game objects (that's <see cref="GameplayLights"/>).
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
        /// <summary>A running clock: the sun and moon move across the sky minute by minute and the colors follow. See <see cref="DayLengthSeconds"/>.</summary>
        DayCycle,
    }

    public static Preset Current { get; private set; } = Preset.Off;

    /// <summary>Raised after a different preset is applied (also by F7 and the /lighting command), so a settings menu can follow along.</summary>
    public static event Action<Preset>? PresetChanged;

    /// <summary>Whether the current preset wants headlights on the player tanks.</summary>
    public static bool WantsHeadlights { get; private set; }
    /// <summary>Multiplier for gameplay lights (shells, mines, explosions, headlights).</summary>
    public static float DynamicBrightness { get; private set; } = 1f;

    // The room model is drawn at scale 10 around RoomScene.TableScenePos * 10, the board sits on the table at y = 0.

    /// <summary>Center of the two windows on the wall behind the board (-Z).</summary>
    public static readonly Vector3 BackWindows = new(-262f, 1137f, -421f);
    /// <summary>Center of the two windows on the right wall (+X).</summary>
    public static readonly Vector3 SideWindows = new(2050f, 1137f, 1485f);
    /// <summary>Everything inside the room (walls, floor, ceiling). The sun's room-wide shadow map covers exactly this.</summary>
    public static readonly BoundingBox RoomBounds = new(new Vector3(-3650f, -620f, -425f), new Vector3(2055f, 2130f, 3305f));
    /// <summary>The board plus its outer walls and a little of the table: what the game camera looks at. Gets the sharp shadows.</summary>
    public static readonly BoundingBox BoardBounds = new(new Vector3(-330f, -10f, -280f), new Vector3(330f, 140f, 280f));

    // room lamps
    // the bulb sits just above the brass stem (which ends at y = 317); any lower and the stem swallows the light
    public static readonly PointLight TableLamp = new(new Vector3(794f, 345f, -74f), new Color(255, 190, 120), 0.8f, 1100f, true) { Priority = 5, Wrap = 0.3f };
    // the desk lamp on the left of the board and the little picture light over the grandfather clock are unshadowed
    // spots: they only reach furniture that nothing stands in front of, so they leave the shadow slots to the tanks

    /// <summary>Bulb inside the desk lamp's tilted shade (left of the board), shining out of the shade onto the desk and its book.</summary>
    public static readonly SpotLight DeskLamp = new(new Vector3(-1108.787f, 604.177f, 54.381f), new Vector3(0.199f, -0.897f, 0.393f), new Color(255, 222, 170), 0.9f, 1000f, 30f, 50f, false) { Priority = 6, Wrap = 0.2f };
    /// <summary>Picture light in front of the clock's hood (the face is at about (1525, 1016, 122), facing (-0.71, 0, 0.71)), shining down onto the dial.
    /// It's brighter than the other lamps because it hits the dark dial at a grazing angle.</summary>
    public static readonly SpotLight ClockLight = new(new Vector3(1426f, 1260f, 221f), new Vector3(99f, -260f, -99f), new Color(255, 214, 160), 3.5f, 900f, 25f, 42f, false) { Priority = 4, Wrap = 0.2f };
    public static readonly PointLight FloorLamp = new(new Vector3(1532f, 1080f, 2828f), new Color(255, 190, 120), 1.0f, 2600f) { Priority = 1, Wrap = 0.4f };
    public static readonly Light[] RoomLamps = [TableLamp, DeskLamp, ClockLight, FloorLamp];

    /// <summary>Emissive look for the room's lamp shades while the lamps are on.</summary>
    /// <remarks>Lit shades don't cast shadows: a real shade is translucent, and as a solid caster it would box the bulb in.</remarks>
    public static readonly MeshLighting GlowingShade = new() { ReceivesLight = false, CastsShadows = false, Emissive = new Vector3(1.7f, 1.45f, 1.05f) };
    static readonly string[] _shadeMeshes = ["Lamp_Shade", "Floor_Lamp_Bowl"];

    // presets / ToD keyframes

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
        public float TableLamp, DeskLamp, ClockLight, FloorLamp;
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
                ClockLight = F(a.ClockLight, b.ClockLight, t),
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
        with { TableLamp = 0.6f, DeskLamp = 0.9f, ClockLight = 3.5f, FloorLamp = 1f, Background = 0.6f, Headlights = 1f };

    /// <summary>dark blue room, faint moonlight through the back windows, all the room lamps on</summary>
    public static TimeOfDay Midnight = Day(sky: new Color(48, 60, 100), ground: new Color(28, 28, 40), ambient: 0.32f,
        sunFrom: new Vector3(-382f, 1300f, -421f), sunTo: new Vector3(-262f, 0f, 59f),
        sunColor: new Color(130, 165, 255), sunIntensity: 0.16f, wrap: 0f,
        shaftColor: new Color(120, 150, 255), shaftDensity: 0.12f, dynamicBrightness: 1f)
        with { TableLamp = 0.8f, DeskLamp = 0.9f, ClockLight = 3.5f, FloorLamp = 1f, Background = 0.3f, Headlights = 1f };

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

    // day cycle

    /// <summary>Real seconds for one full 24 hour day in <see cref="Preset.DayCycle"/> (default: 8 minutes).</summary>
    public static float DayLengthSeconds = 480f;
    /// <summary>Freezes the day cycle clock.</summary>
    public static bool DayCyclePaused;
    /// <summary>Current clock time of the day cycle, 0 - 24 (6.5 = 6:30 AM). Can be set to jump.</summary>
    public static float Hour {
        get => _hour;
        set => _hour = ((value % 24f) + 24f) % 24f;
    }
    static float _hour = 6f;

    // ### SUN PATH ###
    // The day cycle runs a real sun (and moon) across the sky: the time of day gives the sun's position, and the
    // sun's height above the horizon gives the colors. The room is oriented with south = -Z (the back windows),
    // east = -X and west = +X (the side windows), so the sun comes in through the back windows from late
    // morning to early afternoon and low through the side windows around sunset.

    /// <summary>Latitude in degrees. Higher = lower sun and longer days in summer.</summary>
    public static float Latitude = 40f;
    /// <summary>Sun declination in degrees: 23 = midsummer (long day, high sun), 0 = equinox, -23 = midwinter.</summary>
    public static float Declination = 23f;
    /// <summary>Clock time when the sun is highest (13:00 = summer time).</summary>
    public static float SolarNoon = 13f;
    /// <summary>Turns the compass relative to the room, in degrees. Moves where the sun patches land.</summary>
    public static float CompassRotation = -15f;
    /// <summary>Declination of the (full) moon, which is opposite the sun: highest at <see cref="SolarNoon"/> + 12h.</summary>
    public static float MoonDeclination = 10f;

    /// <summary>Unit vector pointing from the scene towards the sun at a given clock time.</summary>
    public static Vector3 SunPosition(float hour) => SkyPosition(hour, Declination, SolarNoon);
    /// <summary>Unit vector pointing from the scene towards the moon at a given clock time.</summary>
    public static Vector3 MoonPosition(float hour) => SkyPosition(hour, MoonDeclination, SolarNoon + 12f);

    /// <summary>Height of the sun above the horizon in degrees (negative at night).</summary>
    public static float SunElevation(float hour) => MathHelper.ToDegrees(MathF.Asin(SunPosition(hour).Y));

    static Vector3 SkyPosition(float hour, float declination, float noon) {
        var h = MathHelper.ToRadians((hour - noon) * 15f);     // hour angle
        var lat = MathHelper.ToRadians(Latitude);
        var dec = MathHelper.ToRadians(declination);
        var east = -MathF.Cos(dec) * MathF.Sin(h);
        var north = MathF.Cos(lat) * MathF.Sin(dec) - MathF.Sin(lat) * MathF.Cos(dec) * MathF.Cos(h);
        var up = MathF.Sin(lat) * MathF.Sin(dec) + MathF.Cos(lat) * MathF.Cos(dec) * MathF.Cos(h);
        var r = MathHelper.ToRadians(CompassRotation);
        (east, north) = (east * MathF.Cos(r) - north * MathF.Sin(r), east * MathF.Sin(r) + north * MathF.Cos(r));
        return Vector3.Normalize(new Vector3(-east, up, north));   // room: east = -X, north = +Z
    }

    // sky colors
    // How the scene looks for a given sun height. The sun direction stored in these is ignored (the sun path sets it).

    static TimeOfDay Sky(Color sky, Color ground, float ambient, Color sunColor, float sunIntensity, float wrap,
        Color shaftColor, float shaftDensity, float anisotropy, float marchLength, float shadowRadius, float dynamicBrightness) => new() {
        Sky = sky, Ground = ground, Ambient = ambient,
        SunDirection = Vector3.Down,
        SunColor = sunColor, SunIntensity = sunIntensity, SunWrap = wrap,
        ShaftColor = shaftColor, ShaftDensity = shaftDensity, ShaftAnisotropy = anisotropy, ShaftMarch = marchLength,
        ShadowRadius = shadowRadius, Background = 1f, DynamicBrightness = dynamicBrightness,
    };

    // this is so cooked
    /// <summary>
    /// The look of the scene by sun elevation (degrees), from night to the highest sun. The day cycle blends between
    /// neighbouring entries, so the colors change continuously as the sun moves. Edit these to restyle the whole day.
    /// </summary>
    public static readonly (float Elevation, TimeOfDay Look)[] SkyGradient = [
        // night: dark blue room, all lamps on (the moon is added separately)
        (-18f, Sky(new Color(40, 52, 92), new Color(24, 24, 36), 0.32f, Color.Black, 0f, 0f, Color.Black, 0f, 0.3f, 1400f, 560f, 1f)
            with { TableLamp = 0.8f, DeskLamp = 0.9f, ClockLight = 3.5f, FloorLamp = 1f, Background = 0.3f, Headlights = 1f }),
        // nautical twilight
        (-9f, Sky(new Color(66, 68, 116), new Color(38, 36, 54), 0.42f, Color.Black, 0f, 0f, Color.Black, 0f, 0.3f, 1400f, 560f, 1f)
            with { TableLamp = 0.8f, DeskLamp = 0.6f, ClockLight = 2.2f, FloorLamp = 0.8f, Background = 0.4f, Headlights = 1f }),
        // blue hour, the table lamp is on
        (-3f, Sky(new Color(96, 90, 138), new Color(56, 50, 70), 0.62f, new Color(255, 100, 60), 0f, 0.4f, new Color(255, 110, 70), 0f, 0.6f, 1800f, 650f, 1f)
            with { TableLamp = 0.6f, Background = 0.6f, Headlights = 1f }),
        // sun on the horizon
        (0f, Sky(new Color(118, 104, 140), new Color(84, 70, 72), 0.76f, new Color(255, 110, 62), 0f, 0.35f, new Color(255, 115, 70), 0f, 0.6f, 1800f, 650f, 0.95f)
            with { TableLamp = 0.25f, Background = 0.8f }),
        // deep orange low sun
        (5f, Sky(new Color(122, 110, 140), new Color(96, 80, 72), 0.88f, new Color(255, 138, 80), 0.85f, 0.35f, new Color(255, 130, 80), 0.25f, 0.6f, 1800f, 650f, 0.9f)
            with { Background = 0.95f }),
        // golden hour
        (14f, Sky(new Color(126, 118, 146), new Color(102, 88, 78), 0.93f, new Color(255, 168, 98), 0.9f, 0.3f, new Color(255, 170, 100), 0.2f, 0.5f, 1800f, 620f, 0.8f)),
        // warm afternoon / morning
        (28f, Sky(new Color(140, 142, 162), new Color(112, 102, 94), 0.95f, new Color(255, 214, 162), 0.74f, 0.2f, new Color(255, 210, 160), 0.16f, 0.45f, 1600f, 600f, 0.7f)),
        // bright day
        (48f, Sky(new Color(150, 157, 175), new Color(117, 111, 104), 1f, new Color(255, 240, 216), 0.64f, 0.1f, new Color(252, 238, 220), 0.13f, 0.3f, 1400f, 560f, 0.6f)),
        // high summer sun
        (70f, Sky(new Color(158, 164, 176), new Color(120, 116, 110), 1f, new Color(255, 252, 244), 0.62f, 0.05f, new Color(250, 248, 240), 0.1f, 0.3f, 1400f, 560f, 0.55f)),
    ];

    /// <summary>Moonlight at night (fades in after the blue hour, scaled by how high the moon is).</summary>
    public static Color MoonColor = new(130, 165, 255);
    public static float MoonIntensity = 0.16f;
    public static Color MoonShaftColor = new(120, 150, 255);
    public static float MoonShaftDensity = 0.12f;

    /// <summary>The scene's look for a sun elevation, blended from <see cref="SkyGradient"/>.</summary>
    public static TimeOfDay SampleSky(float sunElevation) {
        var g = SkyGradient;
        if (sunElevation <= g[0].Elevation) return g[0].Look;
        for (int i = 1; i < g.Length; i++) {
            if (sunElevation > g[i].Elevation) continue;
            var t = (sunElevation - g[i - 1].Elevation) / (g[i].Elevation - g[i - 1].Elevation);
            return TimeOfDay.Lerp(g[i - 1].Look, g[i].Look, t);
        }
        return g[^1].Look;
    }

    /// <summary>The lighting at any clock time (0 - 24), minute by minute.</summary>
    public static TimeOfDay SampleDay(float hour) {
        hour = ((hour % 24f) + 24f) % 24f;
        var sunPos = SunPosition(hour);
        var sunElevation = MathHelper.ToDegrees(MathF.Asin(sunPos.Y));
        var look = SampleSky(sunElevation);

        if (sunElevation > -4f) {
            // the sun (its intensity is already 0 below the horizon)
            look.SunDirection = -sunPos;
            return look;
        }

        // the moon takes over once the sun is well down (both are dark at the switch, so there is no pop)
        var moonPos = MoonPosition(hour);
        var moonElevation = MathHelper.ToDegrees(MathF.Asin(moonPos.Y));
        var fade = SmoothStep(-4f, -12f, sunElevation) * SmoothStep(0f, 10f, moonElevation);
        look.SunDirection = -moonPos;
        look.SunColor = MoonColor;
        look.SunIntensity = MoonIntensity * fade;
        look.SunWrap = 0f;
        look.ShaftColor = MoonShaftColor;
        look.ShaftDensity = MoonShaftDensity * fade;
        look.ShaftAnisotropy = 0.3f;
        look.ShaftMarch = 1400f;
        look.ShadowRadius = 560f;
        return look;
    }

    // this could totally go somewhere else
    static float SmoothStep(float edge0, float edge1, float x) {
        var t = MathHelper.Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
        return t * t * (3f - 2f * t);
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

    // presets

    /// <summary>Configures <see cref="LightingSystem"/> for a preset. <paramref name="room"/> (optional) gets glowing lamp shades when the lamps are on.</summary>
    public static void Apply(Preset preset, Model? room = null) {
        var changed = Current != preset;
        ApplyCore(preset, room);
        if (changed)
            PresetChanged?.Invoke(preset);
    }

    static void ApplyCore(Preset preset, Model? room) {
        Current = preset;

        // Off shows the unlit game but leaves the user's on/off choice (LightingSystem.Enabled) alone
        LightingSystem.Suspended = preset == Preset.Off;
        // one per player tank headlight (the room's spots are unshadowed)
        LightingSystem.MaxShadowedSpotLights = 4;
        LightingSystem.FocusPoint = Vector3.Zero;
        var sun = LightingSystem.Sun;
        sun.ShadowCenter = Vector3.Zero;
        sun.ShadowDepth = 9000f;
        sun.CastsShadows = true;
        sun.RoomShadows = true;
        sun.RoomShadowBounds = RoomBounds;
        sun.ShadowBounds = BoardBounds;

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
        ClockLight.Intensity = time.ClockLight;
        FloorLamp.Intensity = time.FloorLamp;
        foreach (var lamp in RoomLamps) {
            if (lamp.Intensity > 0.01f)
                lights.Add(lamp);
        }
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
}
