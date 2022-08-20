using Microsoft.Xna.Framework;
using System;
using TanksRebirth.Internals.Common.Framework.Input;
using TanksRebirth.Internals.Common.GameUI;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.UI
{
    public static class VolumeUI
    {
        public static UISlider MusicVolume;

        public static UISlider EffectsVolume;

        public static UISlider AmbientVolume;

        public static UIText MusicText;

        public static UIText EffectsText;

        public static UIText AmbientText;

        public static bool BatchVisible { get; set; }

        public static void Initialize()
        {
            //Music
            MusicVolume = new()
            {
                IsVisible = false,
                FallThroughInputs = true
            };
            MusicVolume.SetDimensions(() => new Vector2(700, 100).ToResolution(), () => new Vector2(500, 150).ToResolution());
            MusicVolume.Tooltip = $"{Math.Round(TankGame.Settings.MusicVolume * 100, 1)}%";
            MusicVolume.Initialize();
            MusicVolume.Value = TankGame.Settings.MusicVolume;
            MusicVolume.BarWidth = 15;
            MusicVolume.SliderColor = Color.WhiteSmoke;

            MusicText = new(TankGame.GameLanguage.MusicVolume, TankGame.TextFont, Color.Black)
            {
                IgnoreMouseInteractions = true,
                IsVisible = false,
                FallThroughInputs = true
            };
            MusicText.SetDimensions(() => new Vector2(950, 175).ToResolution(), () => new Vector2(500, 150).ToResolution());

            //Effects
            EffectsVolume = new()
            {
                IsVisible = false
            };
            EffectsVolume.SetDimensions(() => new Vector2(700, 350).ToResolution(), () => new Vector2(500, 150).ToResolution());
            EffectsVolume.Tooltip = $"{Math.Round(TankGame.Settings.EffectsVolume * 100, 1)}%";
            EffectsVolume.Initialize();
            EffectsVolume.Value = TankGame.Settings.EffectsVolume;
            EffectsVolume.BarWidth = 15;
            EffectsVolume.SliderColor = Color.WhiteSmoke;

            EffectsText = new(TankGame.GameLanguage.EffectsVolume, TankGame.TextFont, Color.Black)
            {
                IgnoreMouseInteractions = true,
                IsVisible = false
            };
            EffectsText.SetDimensions(() => new Vector2(950, 425).ToResolution(), () => new Vector2(500, 150).ToResolution());

            //Ambient
            AmbientVolume = new()
            {
                IsVisible = false
            };
            AmbientVolume.SetDimensions(() => new Vector2(700, 600).ToResolution(), () => new Vector2(500, 150).ToResolution());
            AmbientVolume.Tooltip = $"{Math.Round(TankGame.Settings.AmbientVolume * 100, 1)}%";
            AmbientVolume.Initialize();
            AmbientVolume.Value = TankGame.Settings.AmbientVolume;
            AmbientVolume.BarWidth = 15;
            AmbientVolume.SliderColor = Color.WhiteSmoke;

            AmbientText = new(TankGame.GameLanguage.AmbientVolume, TankGame.TextFont, Color.Black)
            {
                IgnoreMouseInteractions = true,
                IsVisible = false
            };
            AmbientText.SetDimensions(() => new Vector2(950, 675).ToResolution(), () => new Vector2(500, 150).ToResolution());
        }

        public static void HideAll()
        {
            MusicVolume.IsVisible = false;
            EffectsVolume.IsVisible = false;
            AmbientVolume.IsVisible = false;
            MusicText.IsVisible = false;
            EffectsText.IsVisible = false;
            AmbientText.IsVisible = false;
        }

        public static void ShowAll()
        {
            MusicVolume.IsVisible = true;
            EffectsVolume.IsVisible = true;
            AmbientVolume.IsVisible = true;
            MusicText.IsVisible = true;
            EffectsText.IsVisible = true;
            AmbientText.IsVisible = true;
        }
    }
}