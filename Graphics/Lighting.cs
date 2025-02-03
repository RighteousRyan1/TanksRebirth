using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TanksRebirth.GameContent;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.Graphics;
/// <summary>Represents a system in which to render lighting for the world.</summary>
public static class Lighting
{
    /// <summary>A custom time of day for the lighting and brightness.</summary>
    public struct LightProfile
    {
        public float Brightness;
        public Color Color;

        public bool IsNight;

        public float SunPower;
        public LightProfile(float brightness, Color color) {
            Brightness = brightness;
            Color = color;

            IsNight = true;

            SunPower = 0f;
        }

        public void Apply(bool applySunPower) {
            LightColor = Color;
            ColorBrightness = Brightness;

            LightPower = applySunPower ? SunPower : 0f;

            /*if (Lighting.IsNight != IsNight)
                TankMusicSystem.SnowLoop = new OggMusic("Snow Loop", IsNight ? "Content/Assets/sounds/ambient/forestnight" : "Content/Assets/sounds/ambient/forestday", 1f);*/

            Lighting.IsNight = IsNight;
        }
    }

    public static bool AccurateShadows = false;

    public static readonly Vector3 AccurateLightingDirection = new(0.25f, 1, -0.5f);

    private static Color LightColor = DefaultLightingColor;
    private static float ColorBrightness = 1f;

    private static float LightPower = 0f;
    private static bool IsNight { get; set; }

    public static readonly LightProfile Dawn = new(0.5f, new Color(0, 25, 0)) { IsNight = true, SunPower = 0.6f };

    public static readonly LightProfile Noon = new(0.65f, new Color(200, 200, 200)) { IsNight = false, SunPower = 1f };

    public static readonly LightProfile Dusk = new(0.4f, new Color(255, 165, 0)) { IsNight = true, SunPower = 0.7f };

    public static readonly LightProfile Midnight = new(0.15f, new Color(0, 0, 0)) { IsNight = true, SunPower = 0.5f };

    private static readonly Color DefaultLightingColor = new Vector3(0.05333332f, 0.09882354f, 0.1819608f).ToColor();

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

    public static void SetDefaultGameLighting_IngameEntities(this BasicEffect effect, float powerMultiplier = 1f, float ambientMultiplier = 1f, bool specular = false, Vector3 lightDir = default)
    {
        effect.LightingEnabled = true;
        effect.PreferPerPixelLighting = TankGame.Settings.PerPixelLighting;
        effect.EnableDefaultLighting();

        effect.DirectionalLight0.Enabled = true;
        effect.DirectionalLight1.Enabled = false;
        effect.DirectionalLight2.Enabled = false;

        //var ting = MouseUtils.MousePosition.X / (WindowUtils.WindowWidth - WindowUtils.WindowWidth / 2);
        //var ting2 = MouseUtils.MousePosition.Y / (WindowUtils.WindowHeight - WindowUtils.WindowHeight / 2);

        var lightingConstant = 1f * powerMultiplier;

        if (lightDir == default)
            lightDir = Vector3.Down;

        effect.DirectionalLight0.Direction = lightDir * lightingConstant; //+ new Vector3(ting, 0, ting2);

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
