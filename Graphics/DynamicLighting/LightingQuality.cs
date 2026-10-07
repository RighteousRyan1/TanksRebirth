namespace TanksRebirth.Graphics.DynamicLighting;

/// <summary>
/// How much work <see cref="LightingSystem"/> is allowed to do. This is the performance side only: the scene
/// (presets, time of day, lamps) decides what the lighting <i>looks</i> like, quality decides how expensive it may be.
/// When the two disagree, quality wins: a preset asking for 5 shadowed lamps gets at most
/// <see cref="MaxShadowedPointLights"/> of them.
/// </summary>
/// <remarks>
/// Assign a new instance (or change fields of <see cref="LightingSystem.Quality"/>) at any time; render targets are
/// resized or released on the next frame. Engine level, no game types: the game's graphics menu maps its own options
/// onto this (see <see cref="LightingSettings.ToQuality"/>).
/// </remarks>
public sealed class LightingQuality {
    // ------------------------------------------------------------------------------ sun

    /// <summary>Sun and moon shadows. Off = the sun still lights the scene but nothing blocks it.</summary>
    public bool SunShadows = true;
    /// <summary>Resolution of the sharp sun shadow map around the board. 4096 needs the HiDef profile.</summary>
    public int SunShadowMapSize = 2048;
    /// <summary>The second, room-wide sun shadow map (sun patches through the windows, shadows away from the board).</summary>
    public bool RoomShadows = true;
    /// <summary>Resolution of the room-wide sun shadow map.</summary>
    public int RoomShadowMapSize = 2048;

    // ------------------------------------------------------------------------------ local lights

    /// <summary>Most point, spot and gameplay lights evaluated per frame (the rest are dropped by priority).</summary>
    public int MaxLocalLights = 32;
    /// <summary>Cap on point lights with shadows (each costs 6 shadow renders). 0 turns their shadows off. Hard cap: 8.</summary>
    public int MaxShadowedPointLights = 8;
    /// <summary>Cap on spot lights with shadows. 0 turns their shadows off. Hard cap: 3.</summary>
    public int MaxShadowedSpotLights = 3;
    /// <summary>Resolution of the point / spot shadow atlas: 2048 or 4096 (HiDef only, ~128 MB).</summary>
    public int ShadowAtlasSize = 4096;
    /// <summary>Smooth 3x3 filtered lamp shadows (16 taps) instead of the cheaper 2x2 filter (4 taps).</summary>
    public bool SoftLocalShadows = true;

    // ------------------------------------------------------------------------------ light shafts

    /// <summary>Volumetric sun beams. Needs <see cref="SunShadows"/>.</summary>
    public bool LightShafts = true;
    /// <summary>Resolution divisor of the light shafts: 1 = full, 2 = half, 4 = quarter resolution.</summary>
    public int ShaftDownsample = 2;

    /// <summary>A copy, so a menu can edit a candidate without touching the live settings.</summary>
    public LightingQuality Clone() => (LightingQuality)MemberwiseClone();
}
