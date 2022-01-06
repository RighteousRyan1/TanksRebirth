using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using WiiPlayTanksRemake.Internals.Common.GameInput;
using WiiPlayTanksRemake.Internals.Core;
using WiiPlayTanksRemake.Internals.Common.GameUI;
using WiiPlayTanksRemake.Internals.Common.Utilities;
using WiiPlayTanksRemake.GameContent.Systems;
using WiiPlayTanksRemake.Internals.Common;
using WiiPlayTanksRemake.Graphics;
using System;

namespace WiiPlayTanksRemake.GameContent.UI
{
    public static class IngameUI
    {
        public static bool InOptions { get; set; }

        public static Keybind Pause = new("Up", Keys.Escape);

        public static UIPanel MissionInfoBar;

        public static UIImageButton ResumeButton;

        public static UIImageButton RestartButton;

        public static UIImageButton QuitButton;

        public static UIImageButton OptionsButton;

        public static UIImageButton VolumeButton;

        public static UIImageButton GraphicsButton;

        public static UIImageButton BackButton;

        public static UISlider MusicVolume;

        public static UISlider EffectsVolume;

        public static UISlider AmbientVolume;

        public static UIText MusicText;

        public static UIText EffectsText;

        public static UIText AmbientText;


        public static UIImageButton PerPixelLightingButton;

        public static UIImage PerPixelLightingToggle;

        public static UIImageButton VsyncButton;

        public static UIImage VsyncToggle;

        public static UIImageButton BorderlessWindowButton;

        public static UIImage BorderlessWindowToggle;

        public static UIImage Tooltip;

        public static bool Paused { get; set; } = false;

        public const int SETTINGS_BUTTONS_CT = 10;

        public static UIImageButton[] SettingsButtons = new UIImageButton[SETTINGS_BUTTONS_CT];

        private static int _delay;

        // TODO: make rect scissor work -> get powerups to be pickupable

        internal static void Initialize()
        {
            var ttColor = Color.LightGray;

            SpriteFont font = TankGame.Fonts.Default;
            Vector2 drawOrigin = font.MeasureString("Mission 1") / 2f;
            MissionInfoBar = new((uiPanel, spriteBatch) => spriteBatch.DrawString(font, "Mission 1", uiPanel.Hitbox.Center.ToVector2(), Color.White, 0, drawOrigin, 1.5f, SpriteEffects.None, 1f));
            MissionInfoBar.BackgroundColor = Color.Red;
            MissionInfoBar.SetDimensions(650, 1000, 500, 50);

            ResumeButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Resume", Color.WhiteSmoke))
            {
                Visible = false
            };
            ResumeButton.SetDimensions(700, 100, 500, 150);
            ResumeButton.OnLeftClick += ResumeButton_OnMouseClick;

            RestartButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Start Over", Color.WhiteSmoke))
            {
                Visible = false,
            };
            RestartButton.SetDimensions(700, 350, 500, 150);

            OptionsButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Options", Color.WhiteSmoke))
            {
                Visible = false
            };
            OptionsButton.SetDimensions(700, 600, 500, 150);
            OptionsButton.OnLeftClick += OptionsButton_OnMouseClick;

            VolumeButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Volume", Color.WhiteSmoke))
            {
                Visible = false
            };
            VolumeButton.SetDimensions(700, 100, 500, 150);
            VolumeButton.OnLeftClick += VolumeButton_OnMouseClick;

            GraphicsButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Graphics", Color.WhiteSmoke))
            {
                Visible = false
            };
            GraphicsButton.SetDimensions(700, 350, 500, 150);
            GraphicsButton.OnLeftClick += GraphicsButton_OnMouseClick;

            QuitButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Quit", Color.WhiteSmoke))
            {
                Visible = false
            };
            QuitButton.SetDimensions(700, 850, 500, 150);
            QuitButton.OnLeftClick += QuitButton_OnMouseClick;

            BackButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Back", Color.WhiteSmoke))
            {
                Visible = false
            };
            BackButton.SetDimensions(700, 850, 500, 150);
            BackButton.OnLeftClick += BackButton_OnMouseClick;

            PerPixelLightingToggle = new(null, 1, (uiImage, spriteBatch) => QuickIndicator(uiImage, spriteBatch, TankGame.Settings.PerPixelLighting ? Color.Green : Color.Red))
            {
                Visible = false
            };
            PerPixelLightingToggle.SetDimensions(695, 95, 510, 160);

            PerPixelLightingButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Per-Pixel Lighting", Color.WhiteSmoke))
            {
                Visible = false
            };
            PerPixelLightingButton.SetDimensions(700, 100, 500, 150);
            PerPixelLightingButton.OnLeftClick += PerPixelLightingButton_OnMouseClick;
            PerPixelLightingButton.OnMouseOver += (element) =>
            {
                Tooltip.Remove();
                Tooltip = new(null, 1, (uiImage, spriteBatch) => QuickIndicator(uiImage, spriteBatch, ttColor, "Whether or not to draw lighting\non each individual pixel"))
                {
                    IgnoreMouseInteractions = true
                };
                Tooltip.SetDimensions(GameUtils.MouseX, GameUtils.MouseY, 475, 100);
            };
            PerPixelLightingButton.OnMouseOut += (element) =>
            {
                Tooltip.Visible = false;
            };

            VsyncToggle = new(null, 1, (uiImage, spriteBatch) => QuickIndicator(uiImage, spriteBatch, TankGame.Settings.Vsync ? Color.Green : Color.Red))
            {
                Visible = false
            };
            VsyncToggle.SetDimensions(695, 345, 510, 160);

            VsyncButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "VSync", Color.WhiteSmoke))
            {
                Visible = false
            };
            VsyncButton.SetDimensions(700, 350, 500, 150);
            VsyncButton.OnLeftClick += VsyncButton_OnMouseClick;
            VsyncButton.OnMouseOver += (element) =>
            {
                Tooltip.Remove();
                Tooltip = new(null, 1, (uiImage, spriteBatch) => QuickIndicator(uiImage, spriteBatch, ttColor, "Whether or not to enable\nvertical synchronization"))
                {
                    IgnoreMouseInteractions = true
                };
                Tooltip.SetDimensions(GameUtils.MouseX, GameUtils.MouseY, 400, 100);
            };
            VsyncButton.OnMouseOut += (element) =>
            {
                Tooltip.Visible = false;
            };

            BorderlessWindowToggle = new(null, 1, (uiImage, spriteBatch) => QuickIndicator(uiImage, spriteBatch, TankGame.Settings.BorderlessWindow ? Color.Green : Color.Red))
            {
                Visible = false
            };
            BorderlessWindowToggle.SetDimensions(695, 595, 510, 160);

            BorderlessWindowButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Borderless Window", Color.WhiteSmoke))
            {
                Visible = false
            };
            BorderlessWindowButton.SetDimensions(700, 600, 500, 150);
            BorderlessWindowButton.OnLeftClick += BorderlessWindowButton_OnMouseClick;
            BorderlessWindowButton.OnMouseOver += (element) =>
            {
                Tooltip.Remove();
                Tooltip = new(null, 1, (uiImage, spriteBatch) => QuickIndicator(uiImage, spriteBatch, ttColor, "Whether or not to run the\ngame window borderless"))
                {
                    IgnoreMouseInteractions = true
                };
                Tooltip.SetDimensions(GameUtils.MouseX, GameUtils.MouseY, 400, 100);
            };
            BorderlessWindowButton.OnMouseOut += (element) =>
            {
                Tooltip.Visible = false;
            };

            MusicVolume = new()
            {
                Visible = false
            };
            MusicVolume.SetDimensions(700, 100, 500, 150);
            MusicVolume.BarOverAction = (element) =>
            {
                Tooltip.Remove();
                Tooltip = new(null, 1, (uiImage, spriteBatch) => QuickIndicator(uiImage, spriteBatch, ttColor, $"{Math.Round(TankGame.Settings.MusicVolume * 100, 1)}%"))
                {
                    IgnoreMouseInteractions = true
                };
                Tooltip.SetDimensions(GameUtils.MouseX, GameUtils.MouseY, 185, 75);
            };
            MusicVolume.OnMouseOut += (element) =>
            {
                Tooltip.Visible = false;
            };
            MusicVolume.Initialize();
            MusicVolume.Value = TankGame.Settings.MusicVolume;
            MusicVolume.BarWidth = 15;
            MusicVolume.SliderColor = Color.WhiteSmoke;

            EffectsVolume = new()
            {
                Visible = false
            };
            EffectsVolume.SetDimensions(700, 350, 500, 150);
            EffectsVolume.BarOverAction = (element) =>
            {
                Tooltip.Remove();
                Tooltip = new(null, 1, (uiImage, spriteBatch) => QuickIndicator(uiImage, spriteBatch, ttColor, $"{Math.Round(TankGame.Settings.EffectsVolume * 100, 1)}%"))
                {
                    IgnoreMouseInteractions = true
                };
                Tooltip.SetDimensions(GameUtils.MouseX, GameUtils.MouseY, 185, 75);
            };
            EffectsVolume.OnMouseOut += (element) =>
            {
                Tooltip.Visible = false;
            };
            EffectsVolume.Initialize();
            EffectsVolume.Value = TankGame.Settings.EffectsVolume;
            EffectsVolume.BarWidth = 15;
            EffectsVolume.SliderColor = Color.WhiteSmoke;

            AmbientVolume = new()
            {
                Visible = false
            };
            AmbientVolume.SetDimensions(700, 600, 500, 150);
            AmbientVolume.BarOverAction = (element) =>
            {
                Tooltip.Remove();
                Tooltip = new(null, 1, (uiImage, spriteBatch) => QuickIndicator(uiImage, spriteBatch, ttColor, $"{Math.Round(TankGame.Settings.AmbientVolume * 100, 1)}%"))
                {
                    IgnoreMouseInteractions = true
                };
                Tooltip.SetDimensions(GameUtils.MouseX, GameUtils.MouseY, 185, 75);
            };
            AmbientVolume.OnMouseOut += (element) =>
            {
                Tooltip.Visible = false;
            };
            AmbientVolume.Initialize();
            AmbientVolume.Value = TankGame.Settings.AmbientVolume;
            AmbientVolume.BarWidth = 15;
            AmbientVolume.SliderColor = Color.WhiteSmoke;

            MusicText = new("Music Volume", TankGame.Fonts.Default, Color.Black)
            {
                IgnoreMouseInteractions = true,
                Visible = false
            };
            MusicText.SetDimensions(950, 175, 500, 150);

            EffectsText = new("Effects Volume", TankGame.Fonts.Default, Color.Black)
            {
                IgnoreMouseInteractions = true,
                Visible = false
            };
            EffectsText.SetDimensions(950, 425, 500, 150);

            AmbientText = new("Ambient Volume", TankGame.Fonts.Default, Color.Black)
            {
                IgnoreMouseInteractions = true,
                Visible = false
            };
            AmbientText.SetDimensions(950, 675, 500, 150);

            Tooltip = new(null, 1, (uiImage, spriteBatch) => QuickIndicator(uiImage, spriteBatch, ttColor, "ODST"))
            {
                Visible = false,
                IgnoreMouseInteractions = true
            };
            Tooltip.SetDimensions(0, 0, 475, 100);
        }

        private static void BorderlessWindowButton_OnMouseClick(Internals.UI.UIElement affectedElement)
        {
            TankGame.Instance.graphics.PreferredBackBufferWidth = 1920;
            TankGame.Instance.graphics.PreferredBackBufferHeight = 1080;
            TankGame.Instance.Window.IsBorderless = TankGame.Settings.BorderlessWindow = !TankGame.Settings.BorderlessWindow;
            TankGame.Instance.graphics.ApplyChanges();
            BorderlessWindowToggle.Remove();
            BorderlessWindowToggle = new(null, 1, (uiImage, spriteBatch) => QuickIndicator(uiImage, spriteBatch, TankGame.Settings.BorderlessWindow ? Color.Green : Color.Red));
            BorderlessWindowToggle.SetDimensions(695, 595, 510, 160);
            BorderlessWindowButton.Remove();
            BorderlessWindowButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Borderless Window", Color.WhiteSmoke));
            BorderlessWindowButton.SetDimensions(700, 600, 500, 150);
            BorderlessWindowButton.OnLeftClick += BorderlessWindowButton_OnMouseClick;
            BorderlessWindowButton.OnMouseOver += (element) =>
            {
                Tooltip.Remove();
                Tooltip = new(null, 1, (uiImage, spriteBatch) => QuickIndicator(uiImage, spriteBatch, Color.White, "Whether or not to run the\ngame window borderless"))
                {
                    IgnoreMouseInteractions = true
                };
                Tooltip.SetDimensions(GameUtils.MouseX, GameUtils.MouseY, 400, 100);
            };
            BorderlessWindowButton.OnMouseOut += (element) =>
            {
                Tooltip.Visible = false;
            };
        }

        private static void VsyncButton_OnMouseClick(Internals.UI.UIElement affectedElement)
        {
            TankGame.Instance.graphics.SynchronizeWithVerticalRetrace = TankGame.Settings.Vsync = !TankGame.Settings.Vsync;
            TankGame.Instance.graphics.ApplyChanges();
            VsyncToggle.Remove();
            VsyncToggle = new(null, 1, (uiImage, spriteBatch) => QuickIndicator(uiImage, spriteBatch, TankGame.Settings.Vsync ? Color.Green : Color.Red));
            VsyncToggle.SetDimensions(695, 345, 510, 160);
            VsyncButton.Remove();
            VsyncButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "VSync", Color.WhiteSmoke));
            VsyncButton.SetDimensions(700, 350, 500, 150);
            VsyncButton.OnLeftClick += VsyncButton_OnMouseClick;
            VsyncButton.OnMouseOver += (element) =>
            {
                Tooltip.Remove();
                Tooltip = new(null, 1, (uiImage, spriteBatch) => QuickIndicator(uiImage, spriteBatch, Color.White, "Whether or not to enable\nvertical synchronization"))
                {
                    IgnoreMouseInteractions = true
                };
                Tooltip.SetDimensions(GameUtils.MouseX, GameUtils.MouseY, 400, 100);
            };
            VsyncButton.OnMouseOut += (element) =>
            {
                Tooltip.Visible = false;
            };
        }

        private static void PerPixelLightingButton_OnMouseClick(Internals.UI.UIElement affectedElement)
        {
            Lighting.PerPixelLighting = TankGame.Settings.PerPixelLighting = !TankGame.Settings.PerPixelLighting;
            PerPixelLightingToggle.Remove();
            PerPixelLightingToggle = new(null, 1, (uiImage, spriteBatch) => QuickIndicator(uiImage, spriteBatch, TankGame.Settings.PerPixelLighting ? Color.Green : Color.Red));
            PerPixelLightingToggle.SetDimensions(695, 95, 510, 160);
            PerPixelLightingButton.Remove();
            PerPixelLightingButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Per-Pixel Lighting", Color.WhiteSmoke));
            PerPixelLightingButton.SetDimensions(700, 100, 500, 150);
            PerPixelLightingButton.OnLeftClick += PerPixelLightingButton_OnMouseClick;
            PerPixelLightingButton.OnMouseOver += (element) =>
            {
                Tooltip.Remove();
                Tooltip = new(null, 1, (uiImage, spriteBatch) => QuickIndicator(uiImage, spriteBatch, Color.White, "Whether or not to draw lighting\non each individual pixel"))
                {
                    IgnoreMouseInteractions = true
                };
                Tooltip.SetDimensions(GameUtils.MouseX, GameUtils.MouseY, 475, 100);
            };
            PerPixelLightingButton.OnMouseOut += (element) =>
            {
                Tooltip.Visible = false;
            };
        }

        private static void GraphicsButton_OnMouseClick(Internals.UI.UIElement affectedElement)
        {
            _delay = 1;
            VsyncButton.IgnoreMouseInteractions = true;
            VolumeButton.Visible = false;
            GraphicsButton.Visible = false;
            PerPixelLightingButton.Visible = true;
            VsyncButton.Visible = true;
            BorderlessWindowButton.Visible = true;
            PerPixelLightingToggle.Visible = true;
            VsyncToggle.Visible = true;
            BorderlessWindowToggle.Visible = true;
        }

        private static void VolumeButton_OnMouseClick(Internals.UI.UIElement affectedElement)
        {
            _delay = 1;
            MusicVolume.IgnoreMouseInteractions = true;
            VolumeButton.Visible = false;
            GraphicsButton.Visible = false;
            AmbientVolume.Visible = true;
            EffectsVolume.Visible = true;
            MusicVolume.Visible = true;
            MusicText.Visible = true;
            EffectsText.Visible = true;
            AmbientText.Visible = true;
        }

        private static void BackButton_OnMouseClick(Internals.UI.UIElement affectedElement)
        {
            if (AmbientVolume.Visible)
            {
                MusicVolume.Visible = false;
                EffectsVolume.Visible = false;
                AmbientVolume.Visible = false;
                MusicText.Visible = false;
                EffectsText.Visible = false;
                AmbientText.Visible = false;
                VolumeButton.Visible = true;
                GraphicsButton.Visible = true;
            }
            else if (PerPixelLightingButton.Visible)
            {
                PerPixelLightingButton.Visible = false;
                PerPixelLightingToggle.Visible = false;
                VsyncButton.Visible = false;
                VsyncToggle.Visible = false;
                BorderlessWindowButton.Visible = false;
                BorderlessWindowToggle.Visible = false;
                VolumeButton.Visible = true;
                GraphicsButton.Visible = true;
            }
            else
            {
                InOptions = false;
                MusicVolume.Visible = false;
                EffectsVolume.Visible = false;
                AmbientVolume.Visible = false;
                MusicText.Visible = false;
                EffectsText.Visible = false;
                AmbientText.Visible = false;
                BackButton.Visible = false;
                VolumeButton.Visible = false;
                GraphicsButton.Visible = false;
            }
        }

        private static void OptionsButton_OnMouseClick(Internals.UI.UIElement affectedElement)
        {
            _delay = 1;
            InOptions = true;
            ResumeButton.Visible = false;
            RestartButton.Visible = false;
            QuitButton.Visible = false;
            OptionsButton.Visible = false;
            VolumeButton.Visible = true;
            GraphicsButton.Visible = true;
            BackButton.Visible = true;
        }

        private static void QuitButton_OnMouseClick(Internals.UI.UIElement affectedElement)
        {
            TankGame.Instance.Exit();
        }

        private static void ResumeButton_OnMouseClick(Internals.UI.UIElement affectedElement) {
            Paused = false;
        }

        public static void QuickButton(UIImage imageButton, SpriteBatch spriteBatch, string text, Color color, float scale = 1f, bool onLeft = false)
        {
            Texture2D texture = TankGame.UITextures.UIPanelBackground;

            int border = 12;

            Rectangle Hitbox = imageButton.Hitbox;

            int middleX = Hitbox.X + border;
            int rightX = Hitbox.Right - border;

            int middleY = Hitbox.Y + border;
            int bottomY = Hitbox.Bottom - border;

            spriteBatch.Draw(texture, new Rectangle(Hitbox.X, Hitbox.Y, border, border), new Rectangle(0, 0, border, border), imageButton.MouseHovering ? Color.CornflowerBlue : color);
            spriteBatch.Draw(texture, new Rectangle(middleX, Hitbox.Y, Hitbox.Width - border * 2, border), new Rectangle(border, 0, texture.Width - border * 2, border), imageButton.MouseHovering ? Color.CornflowerBlue : color);
            spriteBatch.Draw(texture, new Rectangle(rightX, Hitbox.Y, border, border), new Rectangle(texture.Width - border, 0, border, border), imageButton.MouseHovering ? Color.CornflowerBlue : color);

            spriteBatch.Draw(texture, new Rectangle(Hitbox.X, middleY, border, Hitbox.Height - border * 2), new Rectangle(0, border, border, texture.Height - border * 2), imageButton.MouseHovering ? Color.CornflowerBlue : color);
            spriteBatch.Draw(texture, new Rectangle(middleX, middleY, Hitbox.Width - border * 2, Hitbox.Height - border * 2), new Rectangle(border, border, texture.Width - border * 2, texture.Height - border * 2), imageButton.MouseHovering ? Color.CornflowerBlue : color);
            spriteBatch.Draw(texture, new Rectangle(rightX, middleY, border, Hitbox.Height - border * 2), new Rectangle(texture.Width - border, border, border, texture.Height - border * 2), imageButton.MouseHovering ? Color.CornflowerBlue : color);

            spriteBatch.Draw(texture, new Rectangle(Hitbox.X, bottomY, border, border), new Rectangle(0, texture.Height - border, border, border), imageButton.MouseHovering ? Color.CornflowerBlue : color);
            spriteBatch.Draw(texture, new Rectangle(middleX, bottomY, Hitbox.Width - border * 2, border), new Rectangle(border, texture.Height - border, texture.Width - border * 2, border), imageButton.MouseHovering ? Color.CornflowerBlue : color);
            spriteBatch.Draw(texture, new Rectangle(rightX, bottomY, border, border), new Rectangle(texture.Width - border, texture.Height - border, border, border), imageButton.MouseHovering ? Color.CornflowerBlue : color);
            SpriteFont font = TankGame.Fonts.Default;
            Vector2 drawOrigin = font.MeasureString(text) / 2f;
            spriteBatch.DrawString(font, text, onLeft ? new Vector2(imageButton.Hitbox.Center.X - 50, imageButton.Hitbox.Center.Y) : imageButton.Hitbox.Center.ToVector2(), Color.Black, 0, drawOrigin, scale, SpriteEffects.None, 1f);
        }

        public static void QuickIndicator(UIImage image, SpriteBatch spriteBatch, Color color, string text = null)
        {
            Texture2D texture = TankGame.UITextures.UIPanelBackground;

            int border = 12;

            Rectangle Hitbox = image.Hitbox;

            int middleX = Hitbox.X + border;
            int rightX = Hitbox.Right - border;

            int middleY = Hitbox.Y + border;
            int bottomY = Hitbox.Bottom - border;

            spriteBatch.Draw(texture, new Rectangle(Hitbox.X, Hitbox.Y, border, border), new Rectangle(0, 0, border, border), color);
            spriteBatch.Draw(texture, new Rectangle(middleX, Hitbox.Y, Hitbox.Width - border * 2, border), new Rectangle(border, 0, texture.Width - border * 2, border), color);
            spriteBatch.Draw(texture, new Rectangle(rightX, Hitbox.Y, border, border), new Rectangle(texture.Width - border, 0, border, border), color);

            spriteBatch.Draw(texture, new Rectangle(Hitbox.X, middleY, border, Hitbox.Height - border * 2), new Rectangle(0, border, border, texture.Height - border * 2), color);
            spriteBatch.Draw(texture, new Rectangle(middleX, middleY, Hitbox.Width - border * 2, Hitbox.Height - border * 2), new Rectangle(border, border, texture.Width - border * 2, texture.Height - border * 2), color);
            spriteBatch.Draw(texture, new Rectangle(rightX, middleY, border, Hitbox.Height - border * 2), new Rectangle(texture.Width - border, border, border, texture.Height - border * 2), color);

            spriteBatch.Draw(texture, new Rectangle(Hitbox.X, bottomY, border, border), new Rectangle(0, texture.Height - border, border, border),  color);
            spriteBatch.Draw(texture, new Rectangle(middleX, bottomY, Hitbox.Width - border * 2, border), new Rectangle(border, texture.Height - border, texture.Width - border * 2, border), color);
            spriteBatch.Draw(texture, new Rectangle(rightX, bottomY, border, border), new Rectangle(texture.Width - border, texture.Height - border, border, border), color);
            if (text != null)
            {
                SpriteFont font = TankGame.Fonts.Default;
                Vector2 drawOrigin = font.MeasureString(text) / 2f;
                spriteBatch.DrawString(font, text, image.Hitbox.Center.ToVector2(), Color.Black, 0, drawOrigin, 1, SpriteEffects.None, 1f);
            }
        }

        public static void UpdateButtons()
        {
            var text = $"Mission 1        x{AITank.CountAll()}";
            Vector2 drawOrigin = TankGame.Fonts.Default.MeasureString(text) / 2f;
            MissionInfoBar.UniqueDraw =
                (uiPanel, spriteBatch) => spriteBatch.DrawString(TankGame.Fonts.Default, text, uiPanel.Hitbox.Center.ToVector2(), Color.White, 0, drawOrigin, 1.5f, SpriteEffects.None, 1f);
            if (Pause.JustPressed) {
                if (InOptions)
                {
                    if (AmbientVolume.Visible)
                    {
                        MusicVolume.Visible = false;
                        EffectsVolume.Visible = false;
                        AmbientVolume.Visible = false;
                        MusicText.Visible = false;
                        EffectsText.Visible = false;
                        AmbientText.Visible = false;
                        VolumeButton.Visible = true;
                        GraphicsButton.Visible = true;
                    }
                    else if (PerPixelLightingButton.Visible)
                    {
                        PerPixelLightingButton.Visible = false;
                        PerPixelLightingToggle.Visible = false;
                        VsyncButton.Visible = false;
                        VsyncToggle.Visible = false;
                        BorderlessWindowButton.Visible = false;
                        BorderlessWindowToggle.Visible = false;
                        VolumeButton.Visible = true;
                        GraphicsButton.Visible = true;
                    }
                    else
                    {
                        InOptions = false;
                        ResumeButton.Visible = true;
                        RestartButton.Visible = true;
                        QuitButton.Visible = true;
                        OptionsButton.Visible = true;
                        MusicVolume.Visible = false;
                        EffectsVolume.Visible = false;
                        AmbientVolume.Visible = false;
                        MusicText.Visible = false;
                        EffectsText.Visible = false;
                        AmbientText.Visible = false;
                        BackButton.Visible = false;
                        VolumeButton.Visible = false;
                        GraphicsButton.Visible = false;
                    }
                    return;
                }
                if (Paused)
                    TankMusicSystem.ResumeAll();
                else
                    TankMusicSystem.PauseAll();
                Paused = !Paused;
            }

            if (WPTR.GetElementAt(GameUtils.MousePosition) == null)
            {
                Tooltip.Visible = false;
            }

            if (!InOptions)
            {
                ResumeButton.Visible = Paused;
                RestartButton.Visible = Paused;
                QuitButton.Visible = Paused;
                OptionsButton.Visible = Paused;
            }

            if (MusicVolume.Value != 0f)
                TankGame.Settings.MusicVolume = MusicVolume.Value;

            TankGame.Settings.EffectsVolume = EffectsVolume.Value;
            TankGame.Settings.AmbientVolume = AmbientVolume.Value;
            TankMusicSystem.UpdateVolume();

            if (_delay > 0 && !Input.MouseLeft)
                _delay--;
            if (_delay <= 0)
            {
                MusicVolume.IgnoreMouseInteractions = false;
                VsyncButton.IgnoreMouseInteractions = false;
            }
        }

        public class Options
        {
            public SettingsButton<float> MasterVolButton;
        }

        public class SettingsButton<TSource>
        {
            public UIImageButton button;

            public string name;

            private static int num_sets_btns;

            public SettingsButton(bool setDefaultDimensions = true)
            {
                if (setDefaultDimensions)
                    SetDefDims();
                num_sets_btns++;
            }

            public void ApplyChanges(ref TSource value)
            {

            }

            public void SetDefDims()
            {
                button = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, name, Color.WhiteSmoke));
                button.SetDimensions(new Rectangle(100, 100 * num_sets_btns, GameUtils.WindowWidth - 200, 50));

                button.HasScissor = true;
                button.Scissor = GeometryUtils.CreateRectangleFromCenter(GameUtils.WindowWidth / 2, GameUtils.WindowHeight / 2, GameUtils.WindowWidth / 4, (int)(GameUtils.WindowHeight * 0.4f));
            }
        }
    }
}