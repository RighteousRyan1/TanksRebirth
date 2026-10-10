using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.Graphics;

// i probably need to de-shittify this

/// <summary>Represents a system in which to render static lighting for the world.</summary>
public static class StaticLighting {
    /// <summary>A custom time of day for the lighting and brightness.</summary>
    public struct LightProfile(float brightness, Color color) {
        public float Brightness = brightness;
        public Color Color = color;

        public bool IsNight = true;

        public float SunPower = 0f;

        public readonly void Apply(bool applySunPower) {
            LightColor = Color;
            ColorBrightness = Brightness;

            LightPower = applySunPower ? SunPower : 0f;

            /*if (StaticLighting.IsNight != IsNight)
                TankMusicSystem.SnowLoop = new OggMusic("Snow Loop", IsNight ? "Content/Assets/sounds/ambient/forestnight" : "Content/Assets/sounds/ambient/forestday", 1f);*/

            StaticLighting.IsNight = IsNight;
        }
    }
    static Color LightColor = DefaultLightingColor;
    static float ColorBrightness = 1f;

    static float LightPower = 0f;

    /// <summary>The game light the tank shader's look is tuned for the game's default lighting.</summary>
    public static readonly LightProfile ReferenceLight = new(0.75f, new Color(150, 150, 170));

    /// <summary>
    /// How bright per channel the current light is compared to <see cref="ReferenceLight"/>.
    /// </summary>
    public static Vector3 RelativeSceneLight {
        get {
            var current = (LightColor.ToVector3() + Vector3.One) * ColorBrightness;
            var reference = (ReferenceLight.Color.ToVector3() + Vector3.One) * ReferenceLight.Brightness;
            return current / reference;
        }
    }

    /// <summary>The ambient light color multiplied by the diffuse brightness; the constant term batched sprites add to their emissive color.</summary>
    public static Vector3 AmbientDiffuseProduct => LightColor.ToVector3() * ColorBrightness;
    private static bool IsNight { get; set; }
    // i do not recall where i got these numbers from
    static readonly Color DefaultLightingColor = new Vector3(0.05333332f, 0.09882354f, 0.1819608f).ToColor();

    public static void SetDefaultGameLighting(this BasicEffect effect) {
        const float lightingConstant = 0.9f;

        effect.LightingEnabled = true;
        effect.PreferPerPixelLighting = TankGame.Settings.PerPixelLighting;
        effect.EnableDefaultLighting();

        effect.TextureEnabled = true;

        effect.DirectionalLight0.Enabled = true;
        effect.DirectionalLight1.Enabled = false;
        effect.DirectionalLight2.Enabled = false;

        //var ting = MouseUtils.MousePosition.X / (WindowUtils.WindowWidth + WindowUtils.WindowWidth / 2);
        //var ting2 = MouseUtils.MousePosition.Y / (WindowUtils.WindowHeight + WindowUtils.WindowHeight / 2);


        //effect.DirectionalLight0.Direction = new Vector3(0, -0.7f, -0.7f);
        //effect.DirectionalLight1.Direction = new Vector3(0, -0.7f, 0.7f);
        effect.DirectionalLight0.Direction = Vector3.Down * lightingConstant; //+ new Vector3(ting, 0, ting2);

        effect.SpecularColor = new Vector3(LightPower) * (IsNight ? new Vector3(1) : LightColor.ToVector3());

        effect.AmbientLightColor = LightColor.ToVector3();

        effect.DiffuseColor = new(ColorBrightness);
    }

    public static void SetDefaultGameLighting_IngameEntities(this BasicEffect effect, float powerMultiplier = 1f, float ambientMultiplier = 1f, bool specular = false, Vector3 lightDir = default) {
        effect.LightingEnabled = true;
        effect.PreferPerPixelLighting = TankGame.Settings.PerPixelLighting;
        effect.EnableDefaultLighting();

        effect.DirectionalLight0.Enabled = true;
        effect.DirectionalLight1.Enabled = false;
        effect.DirectionalLight2.Enabled = false;

        var lightingConstant = 1f * powerMultiplier;

        if (lightDir == default)
            lightDir = Vector3.Down;

        // poor practice but it makes it look correct
        effect.DirectionalLight0.Direction = lightDir * lightingConstant;
        effect.SpecularColor = specular ? (Color.White.ToVector3() * LightPower) : new Vector3(LightPower) * (IsNight ? new Vector3(1) : LightColor.ToVector3());
        effect.AmbientLightColor = LightColor.ToVector3() * ambientMultiplier;
        effect.DiffuseColor = new(ColorBrightness);
    }

    public static void SetDefaultGameLighting_Room(this BasicEffect effect, Vector3 lightingDirection) {
        const float lightingConstant = 0.9f;

        effect.LightingEnabled = true;
        effect.PreferPerPixelLighting = TankGame.Settings.PerPixelLighting;
        effect.EnableDefaultLighting();
        effect.SetDefaultGameLighting();

        return;

        effect.TextureEnabled = true;

        //var ting = MouseUtils.MousePosition.X / (WindowUtils.WindowWidth + WindowUtils.WindowWidth / 2);
        //var ting2 = MouseUtils.MousePosition.Y / (WindowUtils.WindowHeight + WindowUtils.WindowHeight / 2);

        //effect.DirectionalLight0.Direction = new Vector3(0, -0.7f, -0.7f);
        //effect.DirectionalLight1.Direction = new Vector3(0, -0.7f, 0.7f);
        var lightVariation = 45;
        //effect.DirectionalLight0.Direction = lightingDirection * lightingConstant; //+ new Vector3(ting, 0, ting2);
        effect.DirectionalLight0.DiffuseColor = LightColor.ToVector3();
        //effect.DirectionalLight1.Direction = lightingDirection.RotateXZ(MathHelper.ToRadians(lightVariation)) * lightingConstant;
        effect.DirectionalLight1.DiffuseColor = LightColor.ToVector3();
        //effect.DirectionalLight2.Direction = lightingDirection.RotateXZ(-MathHelper.ToRadians(lightVariation)) * lightingConstant;
        effect.DirectionalLight2.DiffuseColor = LightColor.ToVector3();

        effect.EmissiveColor = LightColor.ToVector3() * LightPower * 0.5f;

        effect.FogEnabled = true;
        effect.FogColor = LightColor.ToVector3();
        effect.FogStart = 10000f;
        effect.FogEnd = 75000f;

        effect.SpecularColor = LightPower * LightColor.ToVector3();

        effect.AmbientLightColor = LightColor.ToVector3();

        effect.DiffuseColor = new(ColorBrightness);
    }
}