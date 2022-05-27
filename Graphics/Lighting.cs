using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TanksRebirth.GameContent;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.Graphics
{
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
            public LightProfile(float brightness, Color color)
            {
                this.Brightness = brightness;
                this.Color = color;

                IsNight = true;

                SunPower = 0f;
            }

            public void Apply(bool applySunPower)
            {
                LightColor = Color;
                ColorBrightness = Brightness;

                if (applySunPower)
                    LightPower = SunPower;

                Lighting.IsNight = IsNight;

                TankMusicSystem.forestAmbience = Music.CreateMusicTrack("Forest Ambient", IsNight ? "Assets/sounds/ambient/forestnight" : "Assets/sounds/ambient/forestday", 1f);
            }


        }

        public static bool AccurateShadows = false;

        public static Vector3 AccurateLightingDirection = new(0.25f, 1, -0.5f);

        public static Color LightColor = DefaultLightingColor;
        public static float ColorBrightness = 1f;

        public static float LightPower = 0f;
        public static bool IsNight { get; set; }

        public static LightProfile Dawn => new(0.5f, new Color(0, 25, 0)) { IsNight = true, SunPower = 0.6f };

        public static LightProfile Noon => new(0.65f, new Color(200, 200, 200)) { IsNight = false, SunPower = 1f };

        public static LightProfile Dusk => new(0.4f, new Color(255, 165, 0)) { IsNight = true, SunPower = 0.7f };

        public static LightProfile Midnight => new(0.15f, new Color(0, 0, 0)) { IsNight = true, SunPower = 0.5f };

        public static Color DefaultLightingColor => new Vector3(0.05333332f, 0.09882354f, 0.1819608f).ToColor();

        public static void SetDefaultGameLighting(this BasicEffect effect)
        {
            effect.LightingEnabled = true;
            effect.PreferPerPixelLighting = TankGame.Settings.PerPixelLighting;
            effect.EnableDefaultLighting();

            effect.TextureEnabled = true;

            effect.DirectionalLight0.Enabled = true;
            effect.DirectionalLight1.Enabled = false;
            effect.DirectionalLight2.Enabled = false;

            var ting = GameUtils.MousePosition.X / (GameUtils.WindowWidth + GameUtils.WindowWidth / 2);
            var ting2 = GameUtils.MousePosition.Y / (GameUtils.WindowHeight + GameUtils.WindowHeight / 2);

            var lightingConstant = 0.9f;

            //effect.DirectionalLight0.Direction = new Vector3(0, -0.7f, -0.7f);
            //effect.DirectionalLight1.Direction = new Vector3(0, -0.7f, 0.7f);
            effect.DirectionalLight0.Direction = Vector3.Down * lightingConstant; //+ new Vector3(ting, 0, ting2);

            effect.SpecularColor = new Vector3(LightPower) * (IsNight ? new Vector3(1) : LightColor.ToVector3());

            effect.AmbientLightColor = LightColor.ToVector3();

            effect.DiffuseColor = new(ColorBrightness);
        }

        public static void SetDefaultGameLighting_IngameEntities(this BasicEffect effect, float powerMultiplier = 1f, float ambientMultiplier = 1f, bool specular = false)
        {
            effect.LightingEnabled = true;
            effect.PreferPerPixelLighting = TankGame.Settings.PerPixelLighting;
            effect.EnableDefaultLighting();

            effect.DirectionalLight0.Enabled = true;
            effect.DirectionalLight1.Enabled = false;
            effect.DirectionalLight2.Enabled = false;

            var ting = GameUtils.MousePosition.X / (GameUtils.WindowWidth + GameUtils.WindowWidth / 2);
            var ting2 = GameUtils.MousePosition.Y / (GameUtils.WindowHeight + GameUtils.WindowHeight / 2);

            var lightingConstant = 1.1f * powerMultiplier;

            effect.DirectionalLight0.Direction = new Vector3(0, -1f, 0) * lightingConstant; //+ new Vector3(ting, 0, ting2);

            effect.SpecularColor = specular ? (Color.White.ToVector3() * LightPower) : new Vector3(LightPower) * (IsNight ? new Vector3(1) : LightColor.ToVector3());

            effect.AmbientLightColor = LightColor.ToVector3() * ambientMultiplier;

            effect.DiffuseColor = new(ColorBrightness);
        }
    }
}