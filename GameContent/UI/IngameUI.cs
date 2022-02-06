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

        public static UIImageButton ControlsButton;

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


        public static UIImageButton ResolutionButton;


        public static UIImageButton UpKeybindButton;

        public static UIImageButton LeftKeybindButton;

        public static UIImageButton RightKeybindButton;

        public static UIImageButton DownKeybindButton;

        public static UIImageButton MineKeybindButton;

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
            #region oh god please help me

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

            ControlsButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Controls", Color.WhiteSmoke))
            {
                Visible = false
            };
            ControlsButton.SetDimensions(700, 600, 500, 150);
            ControlsButton.OnLeftClick += ControlsButton_OnLeftClick;

            QuitButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Quit", Color.WhiteSmoke))
            {
                Visible = false
            };
            QuitButton.SetDimensions(700, 850, 500, 150);
            QuitButton.OnLeftClick += QuitButton_OnMouseClick;

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
            PerPixelLightingButton.Tooltip = "Whether or not to draw lighting\non each individual pixel";

            VsyncToggle = new(null, 1, (uiImage, spriteBatch) => QuickIndicator(uiImage, spriteBatch, TankGame.Settings.Vsync ? Color.Green : Color.Red))
            {
                Visible = false
            };
            VsyncToggle.SetDimensions(695, 345, 510, 160);

            VsyncButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Vertical Sync", Color.WhiteSmoke))
            {
                Visible = false
            };
            VsyncButton.SetDimensions(700, 350, 500, 150);
            VsyncButton.OnLeftClick += VsyncButton_OnMouseClick;
            VsyncButton.Tooltip = "Whether or not to render a 1 full\nframe cycle per second";

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
            BorderlessWindowButton.Tooltip = "Whether or not to run the\ngame window borderless";

            MusicVolume = new()
            {
                Visible = false
            };
            MusicVolume.SetDimensions(700, 100, 500, 150);
            MusicVolume.Tooltip = $"{Math.Round(TankGame.Settings.MusicVolume * 100, 1)}%";
            MusicVolume.Initialize();
            MusicVolume.Value = TankGame.Settings.MusicVolume;
            MusicVolume.BarWidth = 15;
            MusicVolume.SliderColor = Color.WhiteSmoke;

            EffectsVolume = new()
            {
                Visible = false
            };
            EffectsVolume.SetDimensions(700, 350, 500, 150);
            EffectsVolume.Tooltip = $"{Math.Round(TankGame.Settings.EffectsVolume * 100, 1)}%";
            EffectsVolume.Initialize();
            EffectsVolume.Value = TankGame.Settings.EffectsVolume;
            EffectsVolume.BarWidth = 15;
            EffectsVolume.SliderColor = Color.WhiteSmoke;

            AmbientVolume = new()
            {
                Visible = false
            };
            AmbientVolume.SetDimensions(700, 600, 500, 150);
            AmbientVolume.Tooltip = $"{Math.Round(TankGame.Settings.AmbientVolume * 100, 1)}%";
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

            UpKeybindButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Up: " + PlayerTank.controlUp.AssignedKey.ParseKey(), Color.WhiteSmoke))
            {
                Visible = false
            };
            UpKeybindButton.SetDimensions(550, 200, 300, 150);
            UpKeybindButton.OnLeftClick += UpKeybindButton_OnLeftClick;

            LeftKeybindButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Left: " + PlayerTank.controlLeft.AssignedKey.ParseKey(), Color.WhiteSmoke))
            {
                Visible = false
            };
            LeftKeybindButton.SetDimensions(1050, 200, 300, 150);
            LeftKeybindButton.OnLeftClick += LeftKeybindButton_OnLeftClick;

            RightKeybindButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Right: " + PlayerTank.controlRight.AssignedKey.ParseKey(), Color.WhiteSmoke))
            {
                Visible = false
            };
            RightKeybindButton.SetDimensions(550, 400, 300, 150);
            RightKeybindButton.OnLeftClick += RightKeybindButton_OnLeftClick;

            DownKeybindButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Down: " + PlayerTank.controlDown.AssignedKey.ParseKey(), Color.WhiteSmoke))
            {
                Visible = false
            };
            DownKeybindButton.SetDimensions(1050, 400, 300, 150);
            DownKeybindButton.OnLeftClick += DownKeybindButton_OnLeftClick;

            MineKeybindButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Mine: " + PlayerTank.controlMine.AssignedKey.ParseKey(), Color.WhiteSmoke))
            {
                Visible = false
            };
            MineKeybindButton.SetDimensions(800, 600, 300, 150);
            MineKeybindButton.OnLeftClick += MineKeybindButton_OnLeftClick;

            #endregion

            ResolutionButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, $"Resolution: {curPair.Key}x{curPair.Value}", Color.WhiteSmoke))
            {
                Visible = false
            };
            ResolutionButton.SetDimensions(700, 850, 500, 150);
            ResolutionButton.OnLeftClick += ResolutionButton_OnLeftClick;
            ResolutionButton.OnRightClick += ResolutionButton_OnRightClick;

            BackButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Back", Color.WhiteSmoke))
            {
                Visible = false
            };
            BackButton.SetDimensions(700, 850, 500, 150);
            BackButton.OnLeftClick += BackButton_OnMouseClick;

            //test = new("Hi", TankGame.Fonts.Default, Color.AliceBlue);
            //test.SetDimensions(800, 500, 200, 100);

            //dropperTest = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "test", Color.WhiteSmoke));
            //test.Append(dropperTest);
            //test.Initialize();

            //dropperTest2 = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "test2", Color.WhiteSmoke));
            //test.Append(dropperTest2);
            //test.Initialize();

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
                VsyncButton,
                PerPixelLightingButton,
                BorderlessWindowButton,
                ResolutionButton,
                MusicVolume,
                EffectsVolume,
                AmbientVolume
            };
            graphicsElements = new UIElement[]
            {
                VsyncButton,
                VsyncToggle,
                PerPixelLightingButton,
                PerPixelLightingToggle,
                BorderlessWindowButton,
                BorderlessWindowToggle,
                ResolutionButton
            };
            foreach (var button in graphicsElements)
            {
                button.HasScissor = true;
                button.Scissor = new(0, (int)(GameUtils.WindowHeight * 0.05f), GameUtils.WindowWidth, (int)(GameUtils.WindowHeight * 0.7f));
            }
        }

        #region Clicks

        private static void MineKeybindButton_OnLeftClick(UIElement obj)
        {
            MineKeybindButton.Remove();
            MineKeybindButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Press a key", Color.WhiteSmoke));
            MineKeybindButton.SetDimensions(800, 600, 300, 150);
            PlayerTank.controlMine.OnKeyReassigned += PlaceMine_OnKeyReassigned;
            PlayerTank.controlMine.PendKeyReassign = true;
        }

        private static void PlaceMine_OnKeyReassigned(Keys key)
        {
            MineKeybindButton.Remove();
            MineKeybindButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Mine: " + key.ParseKey(), Color.WhiteSmoke));
            MineKeybindButton.SetDimensions(800, 600, 300, 150);
            TankGame.Settings.MineKeybind = key;
            MineKeybindButton.OnLeftClick += MineKeybindButton_OnLeftClick;
            PlayerTank.controlMine.OnKeyReassigned -= PlaceMine_OnKeyReassigned;
        }

        private static void DownKeybindButton_OnLeftClick(UIElement obj)
        {
            DownKeybindButton.Remove();
            DownKeybindButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Press a key", Color.WhiteSmoke));
            DownKeybindButton.SetDimensions(1050, 400, 300, 150);
            PlayerTank.controlDown.OnKeyReassigned += ControlDown_OnKeyReassigned;
            PlayerTank.controlDown.PendKeyReassign = true;
        }

        private static void ControlDown_OnKeyReassigned(Keys key)
        {
            DownKeybindButton.Remove();
            DownKeybindButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Down: " + key.ParseKey(), Color.WhiteSmoke));
            DownKeybindButton.SetDimensions(1050, 400, 300, 150);
            TankGame.Settings.DownKeybind = key;
            DownKeybindButton.OnLeftClick += DownKeybindButton_OnLeftClick;
            PlayerTank.controlRight.OnKeyReassigned -= ControlDown_OnKeyReassigned;
        }

        private static void RightKeybindButton_OnLeftClick(UIElement obj)
        {
            RightKeybindButton.Remove();
            RightKeybindButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Press a key", Color.WhiteSmoke));
            RightKeybindButton.SetDimensions(550, 400, 300, 150);
            PlayerTank.controlRight.OnKeyReassigned += ControlRight_OnKeyReassigned;
            PlayerTank.controlRight.PendKeyReassign = true;
        }

        private static void ControlRight_OnKeyReassigned(Keys key)
        {
            RightKeybindButton.Remove();
            RightKeybindButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Right: " + key.ParseKey(), Color.WhiteSmoke));
            RightKeybindButton.SetDimensions(550, 400, 300, 150);
            TankGame.Settings.RightKeybind = key;
            RightKeybindButton.OnLeftClick += RightKeybindButton_OnLeftClick;
            PlayerTank.controlRight.OnKeyReassigned -= ControlRight_OnKeyReassigned;
        }

        private static void LeftKeybindButton_OnLeftClick(UIElement obj)
        {
            LeftKeybindButton.Remove();
            LeftKeybindButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Press a key", Color.WhiteSmoke));
            LeftKeybindButton.SetDimensions(1050, 200, 300, 150);
            PlayerTank.controlLeft.OnKeyReassigned += ControlLeft_OnKeyReassigned;
            PlayerTank.controlLeft.PendKeyReassign = true;
        }

        private static void ControlLeft_OnKeyReassigned(Keys key)
        {
            LeftKeybindButton.Remove();
            LeftKeybindButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Left: " + key.ParseKey(), Color.WhiteSmoke));
            LeftKeybindButton.SetDimensions(1050, 200, 300, 150);
            TankGame.Settings.LeftKeybind = key;
            LeftKeybindButton.OnLeftClick += LeftKeybindButton_OnLeftClick;
            PlayerTank.controlLeft.OnKeyReassigned -= ControlLeft_OnKeyReassigned;
        }

        private static void UpKeybindButton_OnLeftClick(UIElement obj)
        {
            UpKeybindButton.Remove();
            UpKeybindButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Press a key", Color.WhiteSmoke));
            UpKeybindButton.SetDimensions(550, 200, 300, 150);
            PlayerTank.controlUp.OnKeyReassigned += ControlUp_OnKeyReassigned;
            PlayerTank.controlUp.PendKeyReassign = true;
        }

        private static void ControlUp_OnKeyReassigned(Keys key)
        {
            UpKeybindButton.Remove();
            UpKeybindButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Up: " + key.ParseKey(), Color.WhiteSmoke));
            UpKeybindButton.SetDimensions(550, 200, 300, 150);
            TankGame.Settings.UpKeybind = key;
            UpKeybindButton.OnLeftClick += UpKeybindButton_OnLeftClick;
            PlayerTank.controlUp.OnKeyReassigned -= ControlUp_OnKeyReassigned;
        }

        private static void ControlsButton_OnLeftClick(UIElement obj)
        {
            VolumeButton.Visible = false;
            GraphicsButton.Visible = false;
            ControlsButton.Visible = false;
            UpKeybindButton.Visible = true;
            LeftKeybindButton.Visible = true;
            RightKeybindButton.Visible = true;
            DownKeybindButton.Visible = true;
            MineKeybindButton.Visible = true;
        }

        private static void BorderlessWindowButton_OnMouseClick(UIElement affectedElement)
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
            BorderlessWindowButton.Tooltip = "Whether or not to run the\ngame window borderless";
        }

        private static void VsyncButton_OnMouseClick(UIElement affectedElement)
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
            VsyncButton.Tooltip = "Whether or not to enable\nvertical synchronization";
        }

        private static void PerPixelLightingButton_OnMouseClick(UIElement affectedElement)
        {
            TankGame.Settings.PerPixelLighting = !TankGame.Settings.PerPixelLighting;
            PerPixelLightingToggle.Remove();
            PerPixelLightingToggle = new(null, 1, (uiImage, spriteBatch) => QuickIndicator(uiImage, spriteBatch, TankGame.Settings.PerPixelLighting ? Color.Green : Color.Red));
            PerPixelLightingToggle.SetDimensions(695, 95, 510, 160);
            PerPixelLightingButton.Remove();
            PerPixelLightingButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Per-Pixel Lighting", Color.WhiteSmoke));
            PerPixelLightingButton.SetDimensions(700, 100, 500, 150);
            PerPixelLightingButton.OnLeftClick += PerPixelLightingButton_OnMouseClick;
            PerPixelLightingButton.Tooltip = "Whether or not to draw lighting\non each individual pixel";
        }

        private static void GraphicsButton_OnMouseClick(UIElement affectedElement)
        {
            _delay = 1;
            VsyncButton.IgnoreMouseInteractions = true;
            VolumeButton.Visible = false;
            GraphicsButton.Visible = false;
            ControlsButton.Visible = false;
            PerPixelLightingButton.Visible = true;
            VsyncButton.Visible = true;
            BorderlessWindowButton.Visible = true;
            PerPixelLightingToggle.Visible = true;
            VsyncToggle.Visible = true;
            BorderlessWindowToggle.Visible = true;
            ResolutionButton.Visible = true;
        }

        private static void VolumeButton_OnMouseClick(UIElement affectedElement)
        {
            _delay = 1;
            MusicVolume.IgnoreMouseInteractions = true;
            VolumeButton.Visible = false;
            GraphicsButton.Visible = false;
            ControlsButton.Visible = false;
            AmbientVolume.Visible = true;
            EffectsVolume.Visible = true;
            MusicVolume.Visible = true;
            MusicText.Visible = true;
            EffectsText.Visible = true;
            AmbientText.Visible = true;
        }

        private static void BackButton_OnMouseClick(UIElement affectedElement)
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
                ControlsButton.Visible = true;
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
                ControlsButton.Visible = true;
                ResolutionButton.Visible = false;

                TankGame.Settings.ResWidth = curPair.Key;
                TankGame.Settings.ResHeight = curPair.Value;

                TankGame.Instance.graphics.PreferredBackBufferWidth = TankGame.Settings.ResWidth;
                TankGame.Instance.graphics.PreferredBackBufferHeight = TankGame.Settings.ResHeight;

                TankGame.Instance.graphics.ApplyChanges();
            }
            else if (UpKeybindButton.Visible)
            {
                UpKeybindButton.Visible = false;
                LeftKeybindButton.Visible = false;
                RightKeybindButton.Visible = false;
                DownKeybindButton.Visible = false;
                MineKeybindButton.Visible = false;
                VolumeButton.Visible = true;
                GraphicsButton.Visible = true;
                ControlsButton.Visible = true;
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
                ControlsButton.Visible = false;
            }
        }

        private static void OptionsButton_OnMouseClick(UIElement affectedElement)
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
        }

        private static void QuitButton_OnMouseClick(UIElement affectedElement)
            => TankGame.Quit();

        private static void ResumeButton_OnMouseClick(UIElement affectedElement) {
            Paused = false;
        }

        private static void ResolutionButton_OnLeftClick(UIElement obj)
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

            var pos = ResolutionButton.Position;

            //ResolutionButton.Remove();
            ResolutionButton.UniqueDraw = (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, $"Resolution: {curPair.Key}x{curPair.Value}", Color.WhiteSmoke);
        }

        private static void ResolutionButton_OnRightClick(UIElement obj)
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

            var pos = ResolutionButton.Position;

            //ResolutionButton.Remove();
            ResolutionButton.UniqueDraw = (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, $"Resolution: {curPair.Key}x{curPair.Value}", Color.WhiteSmoke);
        }

        private static int _idxPair = 0;
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

        #endregion

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

        private static int _newScroll;
        private static int _oldScroll;

        public static void UpdateButtons()
        {
            _newScroll = Input.CurrentMouseSnapshot.ScrollWheelValue;

            if (_newScroll != _oldScroll)
                foreach (var b in graphicsElements)
                    b.Position = new(b.Position.X, b.Position.Y + (_newScroll - _oldScroll));

            var text = $"Mission 1        x{AITank.CountAll()}";
            Vector2 drawOrigin = TankGame.Fonts.Default.MeasureString(text) / 2f;
            MissionInfoBar.UniqueDraw =
                (uiPanel, spriteBatch) => spriteBatch.DrawString(TankGame.Fonts.Default, text, uiPanel.Hitbox.Center.ToVector2(), Color.White, 0, drawOrigin, 1.5f, SpriteEffects.None, 1f);
            if (Pause.JustPressed) {
                if (InOptions)
                {
                    BackButton_OnMouseClick(null);
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

            TankGame.Settings.MusicVolume = MusicVolume.Value;

            TankGame.Settings.EffectsVolume = EffectsVolume.Value;
            TankGame.Settings.AmbientVolume = AmbientVolume.Value;

            if (MusicVolume.Value <= 0.01f)
                MusicVolume.Value = 0f;

            if (EffectsVolume.Value <= 0.01f)
                EffectsVolume.Value = 0f;

            if (AmbientVolume.Value <= 0.01f)
                AmbientVolume.Value = 0f;

            TankMusicSystem.UpdateVolume();

            if (_delay > 0 && !Input.MouseLeft)
                _delay--;
            if (_delay <= 0)
            {
                MusicVolume.IgnoreMouseInteractions = false;
                VsyncButton.IgnoreMouseInteractions = false;
            }
            MusicVolume.Tooltip = $"{Math.Round(TankGame.Settings.MusicVolume * 100, 1)}%";
            EffectsVolume.Tooltip = $"{Math.Round(TankGame.Settings.EffectsVolume * 100, 1)}%";
            AmbientVolume.Tooltip = $"{Math.Round(TankGame.Settings.AmbientVolume * 100, 1)}%";

            _oldScroll = _newScroll;


        }
    }
}