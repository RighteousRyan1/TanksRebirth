using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using TanksRebirth.Internals.Common.GameUI;
using TanksRebirth.Internals.UI;

namespace TanksRebirth.GameContent.UI
{
    public static class GraphicsUI
    {
        public static UITextButton PerPixelLightingButton;

        public static UIImage PerPixelLightingToggle;

        public static UITextButton VsyncButton;

        public static UIImage VsyncToggle;

        public static UITextButton FullScreenButton;

        public static UIImage FullScreenToggle;

        public static UITextButton ResolutionButton;

        private static int _idxPair;

        public static KeyValuePair<int, int> CurrentRes = new(TankGame.Settings.ResWidth, TankGame.Settings.ResHeight);

        private static KeyValuePair<int, int>[] CommonResolutions = new KeyValuePair<int, int>[]
        {
            new(640, 480),
            new(1280, 720),
            new(1920, 1080),
            new(2560, 1440),
            new(2048, 1080),
            new(3840, 2160),
            new(7680, 4320)
        };

        public static bool BatchVisible { get; set; }

        public static void DrawBooleanIndicator(SpriteBatch spriteBatch, Rectangle hitbox, bool active)
        {
            spriteBatch.Draw(TankGame.WhitePixel, hitbox, active ? Color.Green : Color.Red);
        }

        public static void Initialize()
        {

            //Per-Pixel Lighting
            PerPixelLightingToggle = new(null, 1, (uiImage, spriteBatch) => DrawBooleanIndicator(spriteBatch, uiImage.Hitbox, TankGame.Settings.PerPixelLighting))
            {
                IsVisible = false,
                IgnoreMouseInteractions = true
            };
            PerPixelLightingToggle.SetDimensions(695, 95, 510, 160);

            PerPixelLightingButton = new(TankGame.GameLanguage.PerPxLight, TankGame.TextFont, Color.WhiteSmoke)
            {
                IsVisible = false,
                Tooltip = TankGame.GameLanguage.PerPxLightDesc
            };
            PerPixelLightingButton.SetDimensions(700, 100, 500, 150);
            PerPixelLightingButton.OnLeftClick = (uiElement) =>
            {
                TankGame.Settings.PerPixelLighting = !TankGame.Settings.PerPixelLighting;
            };

            //Vsync
            VsyncToggle = new(null, 1, (uiImage, spriteBatch) => DrawBooleanIndicator(spriteBatch, uiImage.Hitbox, TankGame.Settings.Vsync))
            {
                IsVisible = false,
                IgnoreMouseInteractions = true
            };
            VsyncToggle.SetDimensions(695, 345, 510, 160);

            VsyncButton = new(TankGame.GameLanguage.VSync, TankGame.TextFont, Color.WhiteSmoke)
            {
                IsVisible = false,
                Tooltip = TankGame.GameLanguage.VSyncDesc
            };
            VsyncButton.SetDimensions(700, 350, 500, 150);
            VsyncButton.OnLeftClick = (uiElement) =>
            {
                TankGame.Instance.Graphics.SynchronizeWithVerticalRetrace = TankGame.Settings.Vsync = !TankGame.Settings.Vsync;
                TankGame.Instance.Graphics.ApplyChanges();
            };

            //Borderless Window
            FullScreenToggle = new(null, 1, (uiImage, spriteBatch) => DrawBooleanIndicator(spriteBatch, uiImage.Hitbox, TankGame.Settings.FullScreen))
            {
                IsVisible = false,
                IgnoreMouseInteractions = true
            };
            FullScreenToggle.SetDimensions(695, 595, 510, 160);

            FullScreenButton = new(TankGame.GameLanguage.BorderlessWindow, TankGame.TextFont, Color.WhiteSmoke)
            {
                IsVisible = false,
                Tooltip = TankGame.GameLanguage.BorderlessWindowDesc
            };
            FullScreenButton.SetDimensions(700, 600, 500, 150);
            FullScreenButton.OnLeftClick = (uiElement) =>
            {
                if (TankGame.Settings.FullScreen)
                {
                    TankGame.Instance.Graphics.PreferredBackBufferHeight -= 50;
                }
                else
                {
                    TankGame.Instance.Graphics.PreferredBackBufferHeight += 50;
                }
                TankGame.Instance.Graphics.IsFullScreen = TankGame.Settings.FullScreen = !TankGame.Settings.FullScreen;
                TankGame.Instance.Graphics.ApplyChanges();
            };

            //Resolution
            ResolutionButton = new($"{TankGame.GameLanguage.Resolution}: {CurrentRes.Key}x{CurrentRes.Value}", TankGame.TextFont, Color.WhiteSmoke)
            {
                IsVisible = false,
                Tooltip = TankGame.GameLanguage.ResolutionDesc
            };
            ResolutionButton.SetDimensions(700, 850, 500, 150);
            ResolutionButton.OnLeftClick = (uiElement) =>
            {
                var tryFind = CommonResolutions.FirstOrDefault(x => x.Key == CurrentRes.Key);

                if (Array.IndexOf(CommonResolutions, tryFind) > -1)
                {
                    _idxPair = Array.IndexOf(CommonResolutions, tryFind);
                }

                _idxPair++;

                if (_idxPair >= CommonResolutions.Length)
                    _idxPair = 0;

                CurrentRes = CommonResolutions[_idxPair];

                ResolutionButton.Text = $"{TankGame.GameLanguage.Resolution}: {CurrentRes.Key}x{CurrentRes.Value}";
            };
            ResolutionButton.OnRightClick = (uiElement) =>
            {
                var tryFind = CommonResolutions.FirstOrDefault(x => x.Key == CurrentRes.Key);

                if (Array.IndexOf(CommonResolutions, tryFind) > -1)
                {
                    _idxPair = Array.IndexOf(CommonResolutions, tryFind);
                }

                _idxPair--;

                if (_idxPair < 0)
                    _idxPair = CommonResolutions.Length - 1;

                CurrentRes = CommonResolutions[_idxPair];

                ResolutionButton.Text = $"{TankGame.GameLanguage.Resolution}: {CurrentRes.Key}x{CurrentRes.Value}";
            };
        }

        public static void HideAll()
        {
            PerPixelLightingButton.IsVisible = false;
            VsyncButton.IsVisible = false;
            FullScreenButton.IsVisible = false;
            ResolutionButton.IsVisible = false;
            PerPixelLightingToggle.IsVisible = false;
            VsyncToggle.IsVisible = false;
            FullScreenButton.IsVisible = false;

            //little extra
            //TankGame.Settings.ResWidth = curPair.Key;
            //TankGame.Settings.ResHeight = curPair.Value;
        }

        public static void ShowAll()
        {
            PerPixelLightingButton.IsVisible = true;
            VsyncButton.IsVisible = true;
            FullScreenButton.IsVisible = true;
            ResolutionButton.IsVisible = true;
            PerPixelLightingToggle.IsVisible = true;
            VsyncToggle.IsVisible = true;
            FullScreenButton.IsVisible = true;
        }
    }
}