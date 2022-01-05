using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WiiPlayTanksRemake.GameContent;
using WiiPlayTanksRemake.GameContent.Systems;
using WiiPlayTanksRemake.Internals;
using WiiPlayTanksRemake.Internals.Common.Utilities;

namespace WiiPlayTanksRemake.Graphics
{
    public static class Lighting
    {

        public struct DayState
        {
            public float brightness;
            public Color color;

            public bool isNight;

            public float sunPower;
            public DayState(float brightness, Color color)
            {
                this.brightness = brightness;
                this.color = color;

                isNight = true;

                sunPower = 0f;
            }

            public void Apply(bool applySunPower)
            {
                LightColor = color;
                ColorBrightness = brightness;

                if (applySunPower)
                    LightPower = sunPower;

                IsNight = isNight;

                TankMusicSystem.forestAmbience = Music.CreateMusicTrack("Forest Ambient", isNight ? "Assets/sounds/ambient/forestnight" : "Assets/sounds/ambient/forestday", 1f);
            }


        }

        public static Color LightColor = DefaultLightingColor;
        public static float ColorBrightness = 1f;

        public static float LightPower = 0f;

        public static bool PerPixelLighting { get; set; }
        
        public static bool IsNight { get; set; }

        public static DayState Dawn => new(0.5f, new Color(0, 25, 0)) { isNight = true, sunPower = 0.6f };

        public static DayState Noon => new(0.65f, new Color(200, 200, 200)) { isNight = false, sunPower = 1f };

        public static DayState Dusk => new(0.4f, new Color(255, 165, 0)) { isNight = true, sunPower = 0.7f };

        public static DayState Midnight => new(0.15f, new Color(0, 0, 0)) { isNight = true, sunPower = 0.5f };

        public static Color DefaultLightingColor => new Vector3(0.05333332f, 0.09882354f, 0.1819608f).ToColor();
        public static void SetDefaultGameLighting(this BasicEffect effect)
        {
            effect.LightingEnabled = true;
            effect.PreferPerPixelLighting = PerPixelLighting;
            effect.EnableDefaultLighting();

            effect.TextureEnabled = true;

            effect.DirectionalLight0.Enabled = true;
            effect.DirectionalLight1.Enabled = true;
            effect.DirectionalLight2.Enabled = false;

            effect.DirectionalLight0.Direction = new Vector3(0, -0.7f, -0.7f);
            effect.DirectionalLight1.Direction = new Vector3(0, -0.7f, 0.7f);


            effect.SpecularColor = new Vector3(LightPower) * (IsNight ? new Vector3(1) : LightColor.ToVector3());

            effect.AmbientLightColor = LightColor.ToVector3();

            effect.DiffuseColor = new(ColorBrightness);
        }
        public static void SetDefaultGameLighting_IngameEntities(this BasicEffect effect)
        {
            effect.LightingEnabled = true;
            effect.PreferPerPixelLighting = PerPixelLighting;
            effect.EnableDefaultLighting();

            effect.DirectionalLight0.Enabled = true;
            effect.DirectionalLight1.Enabled = false;
            effect.DirectionalLight2.Enabled = false;

            effect.DirectionalLight1.Enabled = true;
            //effect.DirectionalLight0.Direction = Vector3.Down;
            effect.DirectionalLight0.Direction = new Vector3(0, -0.6f, -0.6f);
            effect.DirectionalLight1.Direction = new Vector3(0, -0.6f, 0.6f);
            // effect.DirectionalLight0.Direction = new(0, -1, -1);
            effect.SpecularColor = new Vector3(LightPower) * (IsNight ? new Vector3(1) : LightColor.ToVector3());

            effect.AmbientLightColor = LightColor.ToVector3();

            effect.DiffuseColor = new(ColorBrightness);
        }
    }
}