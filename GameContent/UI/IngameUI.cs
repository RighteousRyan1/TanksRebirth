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
using WiiPlayTanksRemake.Internals.UI;
using WiiPlayTanksRemake.Internals.Common.Framework.Input;
using System.Collections.Generic;
using System.Linq;
using FontStashSharp;

namespace WiiPlayTanksRemake.GameContent.UI
{
    public static class IngameUI
    {
        public static bool InOptions { get; set; }

        public static Keybind Pause = new("Up", Keys.Escape);

        public static UIPanel MissionInfoBar;

        public static UITextButton ResumeButton;

        public static UITextButton RestartButton;

        public static UITextButton QuitButton;

        public static UITextButton OptionsButton;

        public static UITextButton VolumeButton;

        public static UITextButton GraphicsButton;

        public static UITextButton ControlsButton;

        public static UITextButton BackButton;
        

        public static UIElement[] menuElements;
        public static UIElement[] graphicsElements;

        // public static UIDropdown test;
        // public static UIImageButton dropperTest;
        // public static UIImageButton dropperTest2;

        public static bool Paused { get; set; } = false;

        private static int _delay;

        private static float _gpuSettingsOffset = 0f;

        // TODO: make rect scissor work -> get powerups to be pickupable

        internal static void Initialize()
        {
            var ttColor = Color.LightGray;

            SpriteFontBase font = TankGame.TextFont;
            Vector2 drawOrigin = font.MeasureString("Mission 1") / 2f;
            MissionInfoBar = new((uiPanel, spriteBatch) => spriteBatch.DrawString(font, "Mission 1", uiPanel.Hitbox.Center.ToVector2(), Color.White, new Vector2(1.5f), 0f, drawOrigin));
            MissionInfoBar.BackgroundColor = Color.Red;
            MissionInfoBar.SetDimensions(650, 1000, 500, 50);

            ResumeButton = new(TankGame.GameLanguage.Resume, font, Color.WhiteSmoke)
            {
                Visible = false
            };
            ResumeButton.SetDimensions(700, 100, 500, 150);
            ResumeButton.OnLeftClick = (uiElement) => Paused = false;

            RestartButton = new(TankGame.GameLanguage.StartOver, font, Color.WhiteSmoke)
            {
                Visible = false,
            };
            RestartButton.SetDimensions(700, 350, 500, 150);

            OptionsButton = new(TankGame.GameLanguage.Options, font, Color.WhiteSmoke)
            {
                Visible = false
            };
            OptionsButton.SetDimensions(700, 600, 500, 150);
            OptionsButton.OnLeftClick = (uiElement) =>
            {
                _delay = 1;
                InOptions = true;
                ResumeButton.Visible = false;
                RestartButton.Visible = false;
                QuitButton.Visible = false;
                OptionsButton.Visible = false;
                VolumeButton.Visible = true;
                GraphicsButton.Visible = true;
                ControlsButton.Visible = true;
                BackButton.Visible = true;
            };

            VolumeButton = new(TankGame.GameLanguage.Volume, font, Color.WhiteSmoke)
            {
                Visible = false
            };
            VolumeButton.SetDimensions(700, 100, 500, 150);
            VolumeButton.OnLeftClick = (uiElement) =>
            {
                VolumeUI.BatchVisible = true;
                VolumeUI.ShowAll();
                VolumeUI.MusicVolume.IgnoreMouseInteractions = true;
                _delay = 1;
                VolumeButton.Visible = false;
                GraphicsButton.Visible = false;
                ControlsButton.Visible = false;
            };

            GraphicsButton = new(TankGame.GameLanguage.Graphics, font, Color.WhiteSmoke)
            {
                Visible = false
            };
            GraphicsButton.SetDimensions(700, 350, 500, 150);
            GraphicsButton.OnLeftClick = (uiElement) =>
            {
                GraphicsUI.BatchVisible = true;
                GraphicsUI.ShowAll();
                GraphicsUI.VsyncButton.IgnoreMouseInteractions = true;
                _delay = 1;
                VolumeButton.Visible = false;
                GraphicsButton.Visible = false;
                ControlsButton.Visible = false;
            };

            ControlsButton = new(TankGame.GameLanguage.Controls, font, Color.WhiteSmoke)
            {
                Visible = false
            };
            ControlsButton.SetDimensions(700, 600, 500, 150);
            ControlsButton.OnLeftClick = (uiElement) =>
            {
                ControlsUI.BatchVisible = true;
                ControlsUI.ShowAll();
                VolumeButton.Visible = false;
                GraphicsButton.Visible = false;
                ControlsButton.Visible = false;
            };

            QuitButton = new(TankGame.GameLanguage.Quit, font, Color.WhiteSmoke)
            {
                Visible = false
            };
            QuitButton.SetDimensions(700, 850, 500, 150);
            QuitButton.OnLeftClick = (ui) => TankGame.Quit();

            BackButton = new(TankGame.GameLanguage.Back, font, Color.WhiteSmoke)
            {
                Visible = false
            };
            BackButton.SetDimensions(700, 850, 500, 150);
            BackButton.OnLeftClick = (uiElement) => HandleBackButton();

            GraphicsUI.Initialize();
            ControlsUI.Initialize();
            VolumeUI.Initialize();
            PostInitialize();
        }

        private static void PostInitialize()
        {
            menuElements = new UIElement[]
            {
                ResumeButton,
                RestartButton,
                QuitButton,
                OptionsButton,
                VolumeButton,
                GraphicsButton,
                ControlsButton,
                BackButton,
                GraphicsUI.VsyncButton,
                GraphicsUI.PerPixelLightingButton,
                GraphicsUI.BorderlessWindowButton,
                GraphicsUI.ResolutionButton,
                VolumeUI.MusicVolume,
                VolumeUI.EffectsVolume,
                VolumeUI.AmbientVolume
            };
            graphicsElements = new UIElement[]
            {
                GraphicsUI.VsyncButton,
                GraphicsUI.VsyncToggle,
                GraphicsUI.PerPixelLightingButton,
                GraphicsUI.PerPixelLightingToggle,
                GraphicsUI.BorderlessWindowButton,
                GraphicsUI.BorderlessWindowToggle,
                GraphicsUI.ResolutionButton
            };
            foreach (var button in graphicsElements)
            {
                button.HasScissor = true;
                button.Scissor = new(0, (int)(GameUtils.WindowHeight * 0.05f), GameUtils.WindowWidth, (int)(GameUtils.WindowHeight * 0.7f));
            }
        }

        private static void HandleBackButton()
        {
            if (VolumeUI.BatchVisible)
            {
                VolumeUI.BatchVisible = false;
                VolumeUI.HideAll();
                VolumeButton.Visible = true;
                GraphicsButton.Visible = true;
                ControlsButton.Visible = true;
            }
            else if (GraphicsUI.BatchVisible)
            {
                GraphicsUI.BatchVisible = false;
                GraphicsUI.HideAll();
                VolumeButton.Visible = true;
                GraphicsButton.Visible = true;
                ControlsButton.Visible = true;

                //TankGame.Instance.graphics.PreferredBackBufferWidth = TankGame.Settings.ResWidth;
                //TankGame.Instance.graphics.PreferredBackBufferHeight = TankGame.Settings.ResHeight;

                //TankGame.Instance.graphics.ApplyChanges();

                // FIXME: acts weird
                // TankGame.Instance.CalculateProjection();
            }
            else if (ControlsUI.BatchVisible)
            {
                ControlsUI.BatchVisible = false;
                ControlsUI.HideAll();
                VolumeButton.Visible = true;
                GraphicsButton.Visible = true;
                ControlsButton.Visible = true;
            }
            else
            {
                InOptions = false;
                VolumeUI.HideAll();
                BackButton.Visible = false;
                VolumeButton.Visible = false;
                GraphicsButton.Visible = false;
                ControlsButton.Visible = false;
            }
        }

        private static int _newScroll;
        private static int _oldScroll;

        public static void UpdateButtons()
        {
            _newScroll = Input.CurrentMouseSnapshot.ScrollWheelValue;

            if (_newScroll != _oldScroll)
            {
                //if (_gpuSettingsOffset < 0)
                    //_gpuSettingsOffset = 0;
                //if (_gpuSettingsOffset < -240)
                    //_gpuSettingsOffset = -240;
                _gpuSettingsOffset = _newScroll - _oldScroll;
                foreach (var b in graphicsElements)
                {
                    b.Position = new(b.Position.X, b.Position.Y + _gpuSettingsOffset);
                    b.MouseHovering = false;
                }
                // ChatSystem.SendMessage(_gpuSettingsOffset, Color.White, "<Debug>");
            }
            var text = $"{TankGame.GameLanguage.Mission} 1        x{AITank.CountAll()}";
            Vector2 drawOrigin = TankGame.TextFont.MeasureString(text) / 2f;
            MissionInfoBar.UniqueDraw =
                (uiPanel, spriteBatch) => spriteBatch.DrawString(TankGame.TextFont, text, uiPanel.Hitbox.Center.ToVector2(), Color.White, new Vector2(1.5f), 0, drawOrigin);
            if (Pause.JustPressed) {
                if (InOptions)
                {
                    HandleBackButton();
                    return;
                }
                if (Paused)
                    TankMusicSystem.ResumeAll();
                else
                    TankMusicSystem.PauseAll();
                Paused = !Paused;
            }

            if (!InOptions)
            {
                ResumeButton.Visible = Paused;
                RestartButton.Visible = Paused;
                QuitButton.Visible = Paused;
                OptionsButton.Visible = Paused;
            }

            TankGame.Settings.MusicVolume = VolumeUI.MusicVolume.Value;
            TankGame.Settings.EffectsVolume = VolumeUI.EffectsVolume.Value;
            TankGame.Settings.AmbientVolume = VolumeUI.AmbientVolume.Value;

            if (VolumeUI.MusicVolume.Value <= 0.01f)
                VolumeUI.MusicVolume.Value = 0f;

            if (VolumeUI.EffectsVolume.Value <= 0.01f)
                VolumeUI.EffectsVolume.Value = 0f;

            if (VolumeUI.AmbientVolume.Value <= 0.01f)
                VolumeUI.AmbientVolume.Value = 0f;

            TankMusicSystem.UpdateVolume();

            if (_delay > 0 && !Input.MouseLeft)
                _delay--;
            if (_delay <= 0)
            {
                VolumeUI.MusicVolume.IgnoreMouseInteractions = false;
                GraphicsUI.VsyncButton.IgnoreMouseInteractions = false;
            }
            VolumeUI.MusicVolume.Tooltip = $"{Math.Round(TankGame.Settings.MusicVolume * 100, 1)}%";
            VolumeUI.EffectsVolume.Tooltip = $"{Math.Round(TankGame.Settings.EffectsVolume * 100, 1)}%";
            VolumeUI.AmbientVolume.Tooltip = $"{Math.Round(TankGame.Settings.AmbientVolume * 100, 1)}%";

            _oldScroll = _newScroll;
        }
    }
}