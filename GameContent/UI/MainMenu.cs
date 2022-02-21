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
using WiiPlayTanksRemake.Internals.Common.Framework.Audio;
using WiiPlayTanksRemake.Internals;
using Microsoft.Xna.Framework.Audio;
using WiiPlayTanksRemake.Enums;

namespace WiiPlayTanksRemake.GameContent.UI
{
    public static class MainMenu
    {
        public static bool Active { get; private set; } = true;

        private static Music Theme;

        private static List<AITank> tanks = new();

        private static Matrix View;
        private static Matrix Projection;

        public static UITextButton PlayButton;

        public static UITextButton PlayButton_SinglePlayer;

        public static UITextButton PlayButton_LevelEditor;

        public static UITextButton PlayButton_Multiplayer;

        private static UIElement[] _menuElements;

        private static float _tnkSpeed = 2.4f;

        // TODO: ingame ui doesn't work, but main menu ui does (wack)
        // TODO: get menu visuals working

        public static void Initialize()
        {
            SpriteFontBase font = TankGame.TextFont;

            Theme = Music.CreateMusicTrack("Main Menu Theme", "Assets/mainmenu/theme", TankGame.Settings.MusicVolume);

            Projection = Matrix.CreateOrthographic(TankGame.Instance.GraphicsDevice.Viewport.Width, TankGame.Instance.GraphicsDevice.Viewport.Height, -2000f, 5000f);
            //Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(90), TankGame.Instance.GraphicsDevice.Viewport.AspectRatio, 0.01f, 1000f);
            View = Matrix.CreateScale(2) * Matrix.CreateLookAt(/*new(0f, 0, 50f)*/ new(0, 0, 500), Vector3.Zero, Vector3.Up) * Matrix.CreateRotationX(MathHelper.PiOver2);

            Open();

            PlayButton = new(TankGame.GameLanguage.Play, font, Color.WhiteSmoke)
            {
                IsVisible = true,
            };
            PlayButton.SetDimensions(700, 100, 500, 50);
            PlayButton.OnLeftClick = (uiElement) =>
            {
                GameUI.OptionsButton.IsVisible = false;
                PlayButton_LevelEditor.IsVisible = true;
                PlayButton_Multiplayer.IsVisible = true;
                PlayButton_SinglePlayer.IsVisible = true;

                GameUI.QuitButton.IsVisible = false;
                GameUI.BackButton.IsVisible = true;

                GameUI.BackButton.Size.Y = 50;

                PlayButton.IsVisible = false;
            };

            PlayButton_Multiplayer = new(TankGame.GameLanguage.Multiplayer, font, Color.WhiteSmoke)
            {
                IsVisible = false,
                Tooltip = "Coming Soon!"
            };
            PlayButton_Multiplayer.SetDimensions(700, 600, 500, 50);

            PlayButton_SinglePlayer = new(TankGame.GameLanguage.SinglePlayer, font, Color.WhiteSmoke)
            {
                IsVisible = false,
            };
            PlayButton_SinglePlayer.SetDimensions(700, 100, 500, 50);

            PlayButton_SinglePlayer.OnLeftClick = (uiElement) =>
            {
                GameHandler.StartTnkScene();
                Leave();

                RemoveAllMenuTanks();
            };

            PlayButton_LevelEditor = new(TankGame.GameLanguage.LevelEditor, font, Color.WhiteSmoke)
            {
                IsVisible = false,
                Tooltip = "Coming Soon!"
            };
            PlayButton_LevelEditor.SetDimensions(700, 350, 500, 50);

            _menuElements = new UIElement[] { PlayButton, PlayButton_SinglePlayer, PlayButton_LevelEditor, PlayButton_Multiplayer };

            foreach (var e in _menuElements)
                e.OnMouseOver = (uiElement) => { SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>("Assets/sounds/menu_tick"), SoundContext.Effect); };
        }

        public static void Update()
        {
            Theme.volume = TankGame.Settings.MusicVolume;

            foreach (var tnk in tanks)
            {
                if (tnk.position.X > 500)
                    tnk.position.X = -500;

                tnk.velocity.X = _tnkSpeed;
            }
        }

        public static void Leave()
        {
            GraphicsUI.BatchVisible = false;
            ControlsUI.BatchVisible = false;
            VolumeUI.BatchVisible = false;
            GameUI.InOptions = false;
            Active = false;
            Theme.Stop();

            GameUI.OptionsButton.Size.Y = 150;
            GameUI.QuitButton.Size.Y = 150;

            HideAll();
        }

        public static void Open()
        {
            Theme.Play();

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    var t = AddTravelingTank(AITank.PICK_ANY_THAT_ARE_IMPLEMENTED(), 1000 + (-i * 100), j * 55);

                    if (i % 2 == 0)
                        t.velocity.Z = 1f;
                    else
                        t.velocity.Z = -1f;

                    t.enactBehavior = () =>
                    {
                        if (t.position.Z > 530)
                            t.position.Z = -30;

                        if (t.position.Z < -30)
                            t.position.Z = 530;
                    };
                }
            }

            GameUI.QuitButton.Size.Y = 50;
            GameUI.QuitButton.IsVisible = true;
            GameUI.OptionsButton.IsVisible = true;
            GameUI.OptionsButton.Size.Y = 50;
        }

        private static void HideAll()
        {
            PlayButton.IsVisible = false;
            PlayButton_SinglePlayer.IsVisible = false;
            PlayButton_Multiplayer.IsVisible = false;
            PlayButton_LevelEditor.IsVisible = false;

            GameUI.BackButton.IsVisible = false;
        }

        public static AITank AddTravelingTank(TankTier tier, float xOffset, float zOffset)
        {
            var extank = new AITank(tier, true, false);
            extank.Team = Team.NoTeam;
            extank.position = new Vector3(-500 + xOffset, 0, zOffset);
            extank.Dead = false;

            extank.TankRotation = MathHelper.PiOver2;

            extank.TurretRotation = extank.TankRotation;

            extank.View = View;
            extank.Projection = Projection;

            tanks.Add(extank);

            return extank;
        }

        public static void RemoveAllMenuTanks()
        {
            for (int i = 0; i < tanks.Count; i++)
            {
                tanks[i].RemoveSilently();
            }
        }
    }
}
