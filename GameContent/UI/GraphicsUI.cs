using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using WiiPlayTanksRemake.Internals.Common.GameUI;
using WiiPlayTanksRemake.Internals.UI;

namespace WiiPlayTanksRemake.GameContent.UI
{
    public static class GraphicsUI
    {
        public static UITextButton PerPixelLightingButton;

        public static UIImage PerPixelLightingToggle;

        public static UITextButton VsyncButton;

        public static UIImage VsyncToggle;

        public static UITextButton BorderlessWindowButton;

        public static UIImage BorderlessWindowToggle;

        public static UITextButton ResolutionButton;

        private static int _idxPair;

        private static KeyValuePair<int, int> curPair = new(TankGame.Settings.ResWidth, TankGame.Settings.ResHeight);

        private static KeyValuePair<int, int>[] commonResolutions = new KeyValuePair<int, int>[]
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

        public static void Initialize()
        {
            static void DrawIndic(SpriteBatch spriteBatch, Rectangle hitbox, bool active)
            {
                spriteBatch.Draw(TankGame.MagicPixel, hitbox, active ? Color.Green : Color.Red);
            }

            //Per-Pixel Lighting
            PerPixelLightingToggle = new(null, 1, (uiImage, spriteBatch) => DrawIndic(spriteBatch, uiImage.Hitbox, TankGame.Settings.Vsync))
            {
                Visible = false
            };
            PerPixelLightingToggle.SetDimensions(695, 95, 510, 160);

            PerPixelLightingButton = new("Per-Pixel Lighting", TankGame.Fonts.Default, Color.WhiteSmoke)
            {
                Visible = false,
                Tooltip = "Whether or not to draw lighting\non each individual pixel"
            };
            PerPixelLightingButton.SetDimensions(700, 100, 500, 150);
            PerPixelLightingButton.Tooltip = "Whether or not to draw lighting\non each individual pixel";
            PerPixelLightingButton.OnLeftClick = (uiElement) =>
            {
                TankGame.Settings.PerPixelLighting = !TankGame.Settings.PerPixelLighting;
            };

            //Vsync
            VsyncToggle = new(null, 1, (uiImage, spriteBatch) => DrawIndic(spriteBatch, uiImage.Hitbox, TankGame.Settings.Vsync))
            {
                Visible = false
            };
            VsyncToggle.SetDimensions(695, 345, 510, 160);

            VsyncButton = new("Vertical Sync", TankGame.Fonts.Default, Color.WhiteSmoke)
            {
                Visible = false
            };
            VsyncButton.SetDimensions(700, 350, 500, 150);
            VsyncButton.Tooltip = "Whether or not to render a 1 full\nframe cycle per second";
            VsyncButton.OnLeftClick = (uiElement) =>
            {
                TankGame.Instance.graphics.SynchronizeWithVerticalRetrace = TankGame.Settings.Vsync = !TankGame.Settings.Vsync;
                TankGame.Instance.graphics.ApplyChanges();
            };

            //Borderless Window
            BorderlessWindowToggle = new(null, 1, (uiImage, spriteBatch) => DrawIndic(spriteBatch, uiImage.Hitbox, TankGame.Settings.Vsync))
            {
                Visible = false
            };
            BorderlessWindowToggle.SetDimensions(695, 595, 510, 160);

            BorderlessWindowButton = new("Borderless Window", TankGame.Fonts.Default, Color.WhiteSmoke)
            {
                Visible = false
            };
            BorderlessWindowButton.SetDimensions(700, 600, 500, 150);
            BorderlessWindowButton.Tooltip = "Whether or not to run the\ngame window borderless";
            BorderlessWindowButton.OnLeftClick = (uiElement) =>
            {
                if (TankGame.Settings.BorderlessWindow)
                {
                    TankGame.Instance.graphics.PreferredBackBufferHeight -= 50;
                }
                else
                {
                    TankGame.Instance.graphics.PreferredBackBufferHeight += 50;
                }
                TankGame.Instance.Window.IsBorderless = TankGame.Settings.BorderlessWindow = !TankGame.Settings.BorderlessWindow;
                TankGame.Instance.graphics.ApplyChanges();
            };

            //Resolution
            ResolutionButton = new($"Resolution: {curPair.Key}x{curPair.Value}", TankGame.Fonts.Default, Color.WhiteSmoke)
            {
                Visible = false
            };
            ResolutionButton.SetDimensions(700, 850, 500, 150);
            ResolutionButton.Tooltip = "Changes the resolution of the game";
            ResolutionButton.OnLeftClick = (uiElement) =>
            {
                var tryFind = commonResolutions.FirstOrDefault(x => x.Key == curPair.Key);

                if (Array.IndexOf(commonResolutions, tryFind) > -1)
                {
                    _idxPair = Array.IndexOf(commonResolutions, tryFind);
                }

                _idxPair++;

                if (_idxPair >= commonResolutions.Length)
                    _idxPair = 0;

                curPair = commonResolutions[_idxPair];

                ResolutionButton.Text = $"Resolution: {curPair.Key}x{curPair.Value}";
            };
            ResolutionButton.OnRightClick = (uiElement) =>
            {
                var tryFind = commonResolutions.FirstOrDefault(x => x.Key == curPair.Key);

                if (Array.IndexOf(commonResolutions, tryFind) > -1)
                {
                    _idxPair = Array.IndexOf(commonResolutions, tryFind);
                }

                _idxPair--;

                if (_idxPair < 0)
                    _idxPair = commonResolutions.Length - 1;

                curPair = commonResolutions[_idxPair];

                ResolutionButton.Text = $"Resolution: {curPair.Key}x{curPair.Value}";
            };
        }

        public static void HideAll()
        {
            PerPixelLightingButton.Visible = false;
            VsyncButton.Visible = false;
            BorderlessWindowButton.Visible = false;
            ResolutionButton.Visible = false;
            PerPixelLightingToggle.Visible = false;
            VsyncToggle.Visible = false;
            BorderlessWindowButton.Visible = false;

            //little extra
            //TankGame.Settings.ResWidth = curPair.Key;
            //TankGame.Settings.ResHeight = curPair.Value;
        }

        public static void ShowAll()
        {
            PerPixelLightingButton.Visible = true;
            VsyncButton.Visible = true;
            BorderlessWindowButton.Visible = true;
            ResolutionButton.Visible = true;
            PerPixelLightingToggle.Visible = true;
            VsyncToggle.Visible = true;
            BorderlessWindowButton.Visible = true;
        }
    }
}