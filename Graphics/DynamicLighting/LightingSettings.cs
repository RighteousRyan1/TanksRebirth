using System;
using Preset = TanksRebirth.Graphics.DynamicLighting.LightingPresets.Preset;

namespace TanksRebirth.Graphics.DynamicLighting;

/// <summary>Overall quality preset, like the "Graphics quality" dropdown at the top of most settings pages.</summary>
public enum LightingQualityLevel {
    Low,
    Medium,
    High,
    Ultra,
    /// <summary>The individual options were changed by hand.</summary>
    Custom,
}

/// <summary>Quality of one shadow feature.</summary>
public enum ShadowQuality {
    Off,
    Low,
    Medium,
    High,
    Ultra,
}

/// <summary>Quality of an effect that is either cheap or expensive.</summary>
public enum EffectQuality {
    Off,
    Low,
    Medium,
    High,
}

/// <summary>
/// Lighting options for the graphics settings page. Plain properties, so it saves alongside the rest of the config.
/// </summary>
/// <remarks>
/// Change properties, then call <see cref="Apply"/>.
/// </remarks>
public sealed class LightingSettings {
    /// <summary>The settings in use. Replace it (e.g. with the loaded config) and call <see cref="Apply"/>.</summary>
    public static LightingSettings Current { get; set; } = new();

    // general stuff

    /// <summary>Dynamic lighting on or off. Disabled frees all performance decreases and frees the video memory.</summary>
    public bool Enabled { get; set; } = false;
    /// <summary>Time of day (or Blackout / DayCycle). Off is better expressed with <see cref="Enabled"/>.</summary>
    public Preset TimeOfDay { get; set; } = Preset.MidAfternoon;
    /// <summary>Real minutes for one 24 hour day when <see cref="TimeOfDay"/> is DayCycle.</summary>
    public float DayLengthMinutes { get; set; } = 8f;
    /// <summary>Lights on shells, mines and explosions (headlights are part of the night presets).</summary>
    public bool GameplayLights { get; set; } = true;
    /// <summary>The room's walls, windows and furniture block the sun (sun patches through the windows). Off = the sun shines through the room.</summary>
    public bool RoomShadows { get; set; } = true;

    // lighting quality

    /// <summary>The preset these options came from (Custom once one was changed by hand).</summary>
    public LightingQualityLevel QualityLevel { get; set; } = LightingQualityLevel.High;
    /// <summary>Sun and moon shadows (Ultra doubles the resolution of the board shadows, HiDef only).</summary>
    public ShadowQuality SunShadows { get; set; } = ShadowQuality.High;
    /// <summary>Shadows from lamps, headlights and explosions.</summary>
    public ShadowQuality LampShadows { get; set; } = ShadowQuality.High;
    /// <summary>Volumetric sun beams.</summary>
    public EffectQuality LightShafts { get; set; } = EffectQuality.Medium;
    /// <summary>How many lights can be on screen at once.</summary>
    public EffectQuality LightCount { get; set; } = EffectQuality.High;

    /// <summary>Sets every quality option from a preset, like picking Low / Medium / High / Ultra in a menu.</summary>
    public void SetQualityLevel(LightingQualityLevel level) {
        QualityLevel = level;
        switch (level) {
            case LightingQualityLevel.Low:
                SunShadows = ShadowQuality.Low;
                LampShadows = ShadowQuality.Low;
                LightShafts = EffectQuality.Off;
                LightCount = EffectQuality.Low;
                break;
            case LightingQualityLevel.Medium:
                SunShadows = ShadowQuality.Medium;
                LampShadows = ShadowQuality.Medium;
                LightShafts = EffectQuality.Low;
                LightCount = EffectQuality.Medium;
                break;
            case LightingQualityLevel.High:
                SunShadows = ShadowQuality.High;
                LampShadows = ShadowQuality.High;
                LightShafts = EffectQuality.Medium;
                LightCount = EffectQuality.High;
                break;
            case LightingQualityLevel.Ultra:
                SunShadows = ShadowQuality.Ultra;
                LampShadows = ShadowQuality.Ultra;
                LightShafts = EffectQuality.High;
                LightCount = EffectQuality.High;
                break;
        }
    }

    /// <summary>Call after changing an individual option from the menu.</summary>
    public void UpdateQualityLevel() {
        foreach (var level in new[] { LightingQualityLevel.Low, LightingQualityLevel.Medium, LightingQualityLevel.High, LightingQualityLevel.Ultra }) {
            var preset = new LightingSettings();
            preset.SetQualityLevel(level);
            if (preset.SunShadows == SunShadows && preset.LampShadows == LampShadows &&
                preset.LightShafts == LightShafts && preset.LightCount == LightCount) {
                QualityLevel = level;
                return;
            }
        }
        QualityLevel = LightingQualityLevel.Custom;
    }

    /// <summary>Turns the menu options into the numbers <see cref="LightingSystem"/> works with.</summary>
    public LightingQuality ToQuality() {
        var q = new LightingQuality {
            // low still looks pretty decent
            SunShadows = SunShadows != ShadowQuality.Off
        };
        // (the board map is fitted tightly to the board, so 2048 is already sharp; Ultra's 4096 is for close-ups)
        (q.SunShadowMapSize, q.RoomShadows, q.RoomShadowMapSize, q.RoomShadowRefreshInterval) = SunShadows switch {
            ShadowQuality.Low => (1024, true, 1024, 8),
            ShadowQuality.Medium => (2048, true, 1024, 6),
            ShadowQuality.High => (2048, true, 2048, 4),
            ShadowQuality.Ultra => (4096, true, 2048, 2),
            _ => (1024, false, 1024, 8),
        };

        // lamps: how many get shadows, how sharp, how smooth
        (q.MaxShadowedPointLights, q.MaxShadowedSpotLights, q.ShadowAtlasSize, q.SoftLocalShadows) = LampShadows switch {
            ShadowQuality.Off => (0, 0, 2048, false),
            // low is quite cheap, medium is still pretty cheap, high is slightly cheap, ultra is maxed out
            ShadowQuality.Low => (2, 2, 2048, false),
            ShadowQuality.Medium => (4, 4, 2048, true),
            ShadowQuality.High => (6, 6, 4096, true),
            ShadowQuality.Ultra => (LightingSystem.MAX_SHADOW_SLOTS, LightingSystem.MAX_SHADOW_SLOTS, 4096, true),
            _ => (4, 4, 2048, true),
        };

        q.LightShafts = LightShafts != EffectQuality.Off;
        // full resolution costs 4x half resolution and looks the same. oh well, people can fry their gpus
        q.ShaftDownsample = LightShafts switch {
            EffectQuality.Low => 4,
            EffectQuality.Medium => 3,
            _ => 2,
        };

        q.MaxLocalLights = LightCount switch {
            EffectQuality.Off => 0,
            EffectQuality.Low => 8,
            EffectQuality.Medium => 16,
            _ => 32,
        };
        return q;
    }

    /// <summary>
    /// Pushes these settings into the lighting system.
    /// </summary>
    public void Apply(bool force = false) {
        var wasEnabled = LightingSystem.Enabled;
        LightingSystem.Enabled = Enabled;
        LightingSystem.Quality = ToQuality();
        // free the shadow maps and buffers when turned off (they come back on demand)
        if (wasEnabled && !Enabled)
            LightingSystem.Unload();

        LightingPresets.DayLengthSeconds = MathF.Max(0.1f, DayLengthMinutes) * 60f;
        LightManager.ShellLights = GameplayLights;
        LightManager.MineLights = GameplayLights;
        LightManager.ExplosionLights = GameplayLights;
        LightManager.RoomCastsShadows = RoomShadows;

        if (force || LightingPresets.Current != TimeOfDay)
            LightManager.Apply(TimeOfDay);
    }

    /// <summary>A copy, so a menu can edit a candidate and only <see cref="Apply"/> it on "Apply" / "OK".</summary>
    public LightingSettings Clone() => (LightingSettings)MemberwiseClone();
}
