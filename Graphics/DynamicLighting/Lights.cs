using Microsoft.Xna.Framework;

namespace TanksRebirth.Graphics.DynamicLighting;

/// <summary>Base class for local (positional) lights.</summary>
/// <remarks>
/// Colors are linear multipliers on top of the existing scene: a light buffer value of 1 leaves a
/// pixel exactly as the game drew it, 0 is black and values up to 2 over-brighten.
/// </remarks>
public abstract class Light {
    /// <summary>Lights that are not enabled are skipped entirely.</summary>
    public bool Enabled = true;
    /// <summary>World position of the light.</summary>
    public Vector3 Position;
    /// <summary>Light color. Multiplied by <see cref="Intensity"/>.</summary>
    public Color Color = Color.White;
    /// <summary>Brightness multiplier. 1 = full color at the light's center.</summary>
    public float Intensity = 1f;
    /// <summary>Distance at which the light fades to zero (smooth windowed falloff).</summary>
    public float Range = 200f;
    /// <summary>0 = hard lambert falloff, 1 = light wraps fully around objects (softer look).</summary>
    public float Wrap = 0.15f;
    /// <summary>Whether this light renders a shadow map. Shadow slots are limited, see <see cref="LightingSystem.MaxShadowedPointLights"/>.</summary>
    public bool CastsShadows;
    /// <summary>Lights with higher priority are picked first for the limited light and shadow budget.</summary>
    public int Priority;
    /// <summary>Extra depth bias in world units, raise this if a light shows shadow acne.</summary>
    public float ShadowBias = 0.75f;

    internal Vector3 ColorVector => Color.ToVector3() * Intensity;
}

/// <summary>A light that shines in every direction from a point (a bulb, a lantern, an explosion flash...).</summary>
public class PointLight : Light {
    public PointLight() { }
    public PointLight(Vector3 position, Color color, float intensity, float range, bool castsShadows = false) {
        Position = position;
        Color = color;
        Intensity = intensity;
        Range = range;
        CastsShadows = castsShadows;
    }
}

/// <summary>A cone of light (a desk lamp, a flashlight, a tank headlight...).</summary>
public class SpotLight : Light {
    /// <summary>Direction the cone points at. Does not need to be normalized.</summary>
    public Vector3 Direction = Vector3.Down;
    /// <summary>Full-brightness half angle of the cone, in radians.</summary>
    public float InnerAngle = MathHelper.ToRadians(20f);
    /// <summary>Half angle where the cone fades to zero, in radians.</summary>
    public float OuterAngle = MathHelper.ToRadians(30f);

    public SpotLight() { }
    public SpotLight(Vector3 position, Vector3 direction, Color color, float intensity, float range,
        float innerDegrees, float outerDegrees, bool castsShadows = false) {
        Position = position;
        Direction = direction;
        Color = color;
        Intensity = intensity;
        Range = range;
        InnerAngle = MathHelper.ToRadians(innerDegrees);
        OuterAngle = MathHelper.ToRadians(outerDegrees);
        CastsShadows = castsShadows;
    }

    /// <summary>Aims the light at a world position.</summary>
    public void LookAt(Vector3 target) => Direction = target - Position;
}

/// <summary>Light from very far away. All rays are parallel (the sun, the moon).</summary>
public class SunLight {
    public bool Enabled = true;
    /// <summary>The direction the light travels in (from the sun towards the ground). Does not need to be normalized.</summary>
    public Vector3 Direction = new(0.1f, -1f, 0.35f);
    public Color Color = Color.White;
    public float Intensity = 0.5f;
    /// <summary>0 = hard lambert falloff, 1 = light wraps fully around objects.</summary>
    public float Wrap;

    public bool CastsShadows = true;
    /// <summary>Center of the area that receives sharp shadows.</summary>
    public Vector3 ShadowCenter = Vector3.Zero;
    /// <summary>Radius around <see cref="ShadowCenter"/> covered by the shadow map. Smaller = sharper.</summary>
    public float ShadowRadius = 520f;
    /// <summary>How far along the light direction shadow casters are collected (centered on <see cref="ShadowCenter"/>).</summary>
    public float ShadowDepth = 9000f;
    /// <summary>Depth bias in world units.</summary>
    public float ShadowBias = 1.5f;

    /// <summary>Volumetric light shafts (god rays) ray marched through the shadow map.</summary>
    public LightShafts Shafts = new();

    /// <summary>Points the light so that it travels from <paramref name="from"/> towards <paramref name="to"/>.</summary>
    public void Aim(Vector3 from, Vector3 to) => Direction = to - from;

    internal Vector3 ColorVector => Color.ToVector3() * Intensity;
}

/// <summary>Settings for the sun's volumetric light shafts.</summary>
public class LightShafts {
    public bool Enabled;
    public Color Color = new(255, 236, 200);
    /// <summary>Overall strength of the effect.</summary>
    public float Density = 0.18f;
    /// <summary>Length (world units) of the ray marched from each surface back towards the camera.</summary>
    public float MarchLength = 1600f;
    /// <summary>Henyey-Greenstein anisotropy. 0 = uniform, closer to 1 = glows strongly when looking into the light.</summary>
    public float Anisotropy = 0.35f;
    /// <summary>Resolution divisor of the shaft buffer (2 = half resolution).</summary>
    public int Downsample = 2;
}

/// <summary>Light that is everywhere. Blends between a sky color (up facing surfaces) and a ground color (down facing).</summary>
public class AmbientLight {
    public Color Sky = new(140, 140, 150);
    public Color Ground = new(110, 105, 100);
    public float Intensity = 1f;

    internal Vector3 SkyVector => Sky.ToVector3() * Intensity;
    internal Vector3 GroundVector => Ground.ToVector3() * Intensity;
}

/// <summary>Per mesh options. Assign with <see cref="LightingSystem.SetMeshLighting(Microsoft.Xna.Framework.Graphics.ModelMesh, MeshLighting)"/>.</summary>
public class MeshLighting {
    public static readonly MeshLighting Default = new();

    /// <summary>The mesh is invisible to the lighting system (doesn't occlude, receive or cast).</summary>
    public bool Ignore;
    /// <summary>The mesh blocks light for shadow maps.</summary>
    public bool CastsShadows = true;
    /// <summary>When false the mesh keeps the look the game gave it (multiplied by <see cref="Emissive"/>).</summary>
    public bool ReceivesLight = true;
    /// <summary>Used when <see cref="ReceivesLight"/> is false. White = unchanged, above 1 glows (like a lamp shade).</summary>
    public Vector3 Emissive = Vector3.One;
}
