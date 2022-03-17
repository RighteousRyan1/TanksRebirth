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
using WiiPlayTanksRemake.Net;

namespace WiiPlayTanksRemake.GameContent.UI
{
    public static class MainMenu
    {
        public static bool Active { get; private set; } = true;

        private static Music Theme;

        private static List<Tank> tanks = new();

        private static Matrix View;
        private static Matrix Projection;

        private static Matrix ForwardView;

        #region Button Fields

        public static UITextButton PlayButton;

        public static UITextButton PlayButton_SinglePlayer;

        public static UITextButton PlayButton_LevelEditor;

        public static UITextButton PlayButton_Multiplayer;

        public static UITextButton CreateServerButton;
        public static UITextButton ConnectToServerButton;

        public static UITextButton StartMPGameButton;

        public static UITextButton DifficultiesButton;

        private static UIElement[] _menuElements;

        public static UITextInput UsernameInput;
        public static UITextInput IPInput;
        public static UITextInput PortInput;
        public static UITextInput PasswordInput;
        public static UITextInput ServerNameInput;

        public static UITextButton MakeTanksWoke; // make them calculate shots abnormally

        #endregion

        private static float _tnkSpeed = 2.4f;

        public static Tank MainMenuTank;

        // private static float _defaultLoadTime = 90;

        internal static bool isLoadingScene;

        // TODO: ingame ui doesn't work, but main menu ui does (wack)
        // TODO: get menu visuals working

        private static float _tnkRot;

        public static void Initialize()
        {
            ForwardView = Matrix.CreateScale(10) * Matrix.CreateLookAt(new(0, 0, 500), Vector3.Zero, Vector3.Up)
                * Matrix.CreateFromYawPitchRoll(1.563f, 0, 0.906f)
                * Matrix.CreateTranslation(499, 260/*342*/, 150);
            Projection = Matrix.CreateOrthographic(TankGame.Instance.GraphicsDevice.Viewport.Width, TankGame.Instance.GraphicsDevice.Viewport.Height, -2000f, 5000f);
            //Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(90), TankGame.Instance.GraphicsDevice.Viewport.AspectRatio, 0.01f, 1000f);
            View = Matrix.CreateScale(2) * Matrix.CreateLookAt(new(0, 0, 500), Vector3.Zero, Vector3.Up) * Matrix.CreateRotationX(MathHelper.PiOver2);

            // AddTravelingTank(TankTier.Black, 200, 200);

            SpriteFontBase font = TankGame.TextFont;

            Theme = Music.CreateMusicTrack("Main Menu Theme", "Assets/mainmenu/theme", TankGame.Settings.MusicVolume);

            #region Init Standard Buttons
            PlayButton = new(TankGame.GameLanguage.Play, font, Color.WhiteSmoke)
            {
                IsVisible = true,
            };
            PlayButton.SetDimensions(700, 450, 500, 50);
            PlayButton.OnLeftClick = (uiElement) =>
            {
                SetPrimaryMenuButtonsVisibility(false);
                SetPlayButtonsVisibility(true);
                GameUI.BackButton.IsVisible = true;
            };

            PlayButton_Multiplayer = new(TankGame.GameLanguage.Multiplayer, font, Color.WhiteSmoke)
            {
                IsVisible = false,
                Tooltip = "In the works!"
            };
            PlayButton_Multiplayer.SetDimensions(700, 750, 500, 50);

            PlayButton_Multiplayer.OnLeftClick = (uiElement) =>
            {
                SetPlayButtonsVisibility(false);
                SetMPButtonsVisibility(true);
            };

            DifficultiesButton = new(TankGame.GameLanguage.Difficulties, font, Color.WhiteSmoke)
            {
                IsVisible = false,
                Tooltip = "Change the difficulty of the game."
            };
            DifficultiesButton.SetDimensions(700, 550, 500, 50);
            DifficultiesButton.OnLeftClick = (element) =>
            {
                SetPlayButtonsVisibility(false);
                SetDifficultiesButtonsVisibility(true);
            };


            PlayButton_SinglePlayer = new(TankGame.GameLanguage.SinglePlayer, font, Color.WhiteSmoke)
            {
                IsVisible = false,
            };
            PlayButton_SinglePlayer.SetDimensions(700, 450, 500, 50);
            
            PlayButton_SinglePlayer.OnLeftClick = (uiElement) =>
            {
                SetMPButtonsVisibility(false);
                SetPlayButtonsVisibility(false);
                SetPrimaryMenuButtonsVisibility(false);
                GameHandler.StartTnkScene();
                Leave();

                isLoadingScene = true;

                RemoveAllMenuTanks();
            };

            InitializeDifficultyButtons();

            PlayButton_LevelEditor = new(TankGame.GameLanguage.LevelEditor, font, Color.WhiteSmoke)
            {
                IsVisible = false,
                Tooltip = "Coming Soon!"
            };
            PlayButton_LevelEditor.SetDimensions(700, 650, 500, 50);

            ConnectToServerButton = new(TankGame.GameLanguage.ConnectToServer, font, Color.WhiteSmoke)
            {
                IsVisible = false,
                Tooltip = "Connect to the written IP and Port in the form of ip:port"
            };
            ConnectToServerButton.SetDimensions(700, 100, 500, 50);
            ConnectToServerButton.OnLeftClick = (uiButton) =>
            {
                if (UsernameInput.IsEmpty())
                {
                    SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>("Assets/sounds/menu/menu_error"), SoundContext.Effect);
                    ChatSystem.SendMessage("Your username is empty!", Color.Red);
                    return;
                }
                if (PortInput.IsEmpty())
                {
                    SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>("Assets/sounds/menu/menu_error"), SoundContext.Effect);
                    ChatSystem.SendMessage("The port is empty!", Color.Red);
                    return;
                }
                if (IPInput.IsEmpty())
                {
                    SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>("Assets/sounds/menu/menu_error"), SoundContext.Effect);
                    ChatSystem.SendMessage("The IP address is not valid.", Color.Red);
                    return;
                }

                if (int.TryParse(PortInput.Text, out var port))
                {
                    Client.CreateClient(UsernameInput.Text);
                    Client.AttemptConnectionTo(IPInput.Text, port, PasswordInput.Text);
                }
                else
                {
                    SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>("Assets/sounds/menu/menu_error"), SoundContext.Effect);
                    ChatSystem.SendMessage("That is not a valid port.", Color.Red);
                }
            };

            CreateServerButton = new(TankGame.GameLanguage.CreateServer, font, Color.WhiteSmoke)
            {
                IsVisible = false,
                Tooltip = "Create a server with the written IP and Port in the form of ip:port"
            };
            CreateServerButton.SetDimensions(700, 350, 500, 50);
            CreateServerButton.OnLeftClick = (uiButton) =>
            {
                if (int.TryParse(PortInput.Text, out var port))
                {
                    Server.CreateServer();
                    Server.StartServer(ServerNameInput.Text, port, IPInput.Text, PasswordInput.Text);
                    NetPlay.ServerName = ServerNameInput.Text;

                    Client.CreateClient(UsernameInput.Text);
                    Client.AttemptConnectionTo(IPInput.Text, port, PasswordInput.Text);

                    Server.ConnectedClients[0] = NetPlay.CurrentClient;
                }
                else
                {
                    SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>("Assets/sounds/menu/menu_error"), SoundContext.Effect);
                    ChatSystem.SendMessage("That is not a valid port.", Color.Red);
                }

            };
            StartMPGameButton = new(TankGame.GameLanguage.Play, font, Color.WhiteSmoke)
            {
                IsVisible = false,
                Tooltip = "Start the game with every client that is connected"
            };
            StartMPGameButton.OnLeftClick = (uiButton) =>
            {
                PlayButton_SinglePlayer.OnLeftClick?.Invoke(null); // starts the game

                Client.RequestStartGame();
                SetPlayButtonsVisibility(false);
            };
            StartMPGameButton.SetDimensions(700, 600, 500, 50);
            #endregion
            #region Input Boxes
            UsernameInput = new(font, Color.WhiteSmoke, 1f, 20)
            {
                IsVisible = false,
                StringToDisplayWhenThereIsNoText = "Username"
            };
            UsernameInput.SetDimensions(100, 400, 500, 50);

            IPInput = new(font, Color.WhiteSmoke, 1f, 15)
            {
                IsVisible = false,
                StringToDisplayWhenThereIsNoText = "Server IP address"
            };
            IPInput.SetDimensions(100, 500, 500, 50);

            PortInput = new(font, Color.WhiteSmoke, 1f, 5)
            {
                IsVisible = false,
                StringToDisplayWhenThereIsNoText = "Server Port"
            };
            PortInput.SetDimensions(100, 600, 500, 50);

            PasswordInput = new(font, Color.WhiteSmoke, 1f, 10)
            {
                IsVisible = false,
                StringToDisplayWhenThereIsNoText = "Server Password (Empty = None)"
            };
            PasswordInput.SetDimensions(100, 700, 500, 50);

            ServerNameInput = new(font, Color.WhiteSmoke, 1f, 10)
            {
                IsVisible = false,
                StringToDisplayWhenThereIsNoText = "Server Name (Server Creation)"
            };
            ServerNameInput.SetDimensions(100, 800, 500, 50);
            #endregion

            AddMainMenuTank();
            Open();

            _menuElements = new UIElement[] 
            { PlayButton, PlayButton_SinglePlayer, PlayButton_LevelEditor, PlayButton_Multiplayer, ConnectToServerButton, 
                CreateServerButton, UsernameInput, IPInput, PortInput, PasswordInput, ServerNameInput,
                DifficultiesButton
            };

            foreach (var e in _menuElements)
                e.OnMouseOver = (uiElement) => { SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>("Assets/sounds/menu/menu_tick"), SoundContext.Effect); };
        }

        private static void InitializeDifficultyButtons()
        {
            SpriteFontBase font = TankGame.TextFont;
            MakeTanksWoke = new("Tanks are Calculators", font, Color.White)
            {
                IsVisible = false,
                Tooltip = "ALL tanks will begin to look for angles\non you (and other enemies) outside of their immediate aim\nDo note that this uses significantly more CPU power"
            };
            MakeTanksWoke.OnLeftClick += (elem) => DifficultyModes.MakeTanksWoke = !DifficultyModes.MakeTanksWoke;
            MakeTanksWoke.SetDimensions(100, 300, 300, 50);
        }

        internal static void SetPlayButtonsVisibility(bool visible)
        {
            PlayButton_SinglePlayer.IsVisible = visible;
            PlayButton_LevelEditor.IsVisible = visible;
            PlayButton_Multiplayer.IsVisible = visible;
            DifficultiesButton.IsVisible = visible;
        }
        internal static void SetDifficultiesButtonsVisibility(bool visible)
        {
            MakeTanksWoke.IsVisible = visible;
        }
        internal static void SetPrimaryMenuButtonsVisibility(bool visible)
        {
            GameUI.OptionsButton.IsVisible = visible;
            PlayButton_SinglePlayer.IsVisible = visible;

            GameUI.QuitButton.IsVisible = visible;

            GameUI.BackButton.Size.Y = 50;

            PlayButton.IsVisible = visible;
        }
        internal static void SetMPButtonsVisibility(bool visible)
        {
            StartMPGameButton.IsVisible = visible;
            ConnectToServerButton.IsVisible = visible;
            CreateServerButton.IsVisible = visible;
            UsernameInput.IsVisible = visible;
            IPInput.IsVisible = visible;
            PasswordInput.IsVisible = visible;
            PortInput.IsVisible = visible;
            ServerNameInput.IsVisible = visible;
        }

        public static void Update()
        {
            MakeTanksWoke.Color = DifficultyModes.MakeTanksWoke ? Color.Green : Color.Red; 
            _tnkRot += 0.01f;

            MainMenuTank.TankRotation = _tnkRot;
            MainMenuTank.TurretRotation = -_tnkRot;

            Theme.volume = TankGame.Settings.MusicVolume;

            MainMenuTank.View = ForwardView;

            foreach (var tnk in tanks)
            {
                if (tnk is not AITank)
                    return;
                var pee = tnk.Body.Position;
                if (pee.X > 500)
                    pee.X = -500;

                tnk.Velocity.Y = _tnkSpeed;

                tnk.Body.Position = pee;
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

            Theme.volume = 0;

            HideAll();
        }

        public static void Open()
        {
            Active = true;
            GameUI.Paused = false;
            Theme.volume = 0.5f;
            Theme.Play();

            foreach (var block in Block.blocks)
                block?.SilentRemove();

            TankGame.OverheadView = false;
            TankGame.CameraRotationVector.Y = TankGame.DEFAULT_ORTHOGRAPHIC_ANGLE;
            TankGame.AddativeZoom = 1f;
            TankGame.CameraFocusOffset.Y = 0f;

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    var t = AddTravelingTank(AITank.PICK_ANY_THAT_ARE_IMPLEMENTED(), 1000 + (-i * 100), j * 55);

                    if (i % 2 == 0)
                        t.Velocity.Y = 1f;
                    else
                        t.Velocity.Y = -1f;
                }
            }

            TankMusicSystem.StopAll();

            SetPrimaryMenuButtonsVisibility(true);
            SetPlayButtonsVisibility(false);
            SetMPButtonsVisibility(false);

            GameUI.ResumeButton.IsVisible = false;
            GameUI.RestartButton.IsVisible = false;

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

        public static Tank AddTravelingTank(TankTier tier, float xOffset, float yOffset)
        {
            var extank = new AITank(tier, true, false)
            {
                Team = Team.NoTeam,
                Dead = false
            };
            extank.Body.Position = new Vector2(-500 + xOffset, yOffset);

            extank.TankRotation = MathHelper.PiOver2;

            extank.TurretRotation = extank.TankRotation;

            extank.View = View;
            extank.Projection = Projection;

            tanks.Add(extank);

            return extank;
        }

        public static void AddMainMenuTank()
        {

            MainMenuTank = new PlayerTank(PlayerType.Blue);
            MainMenuTank.IsIngame = false;
            // MainMenuTank.Body.Position = new(100, 100);
            MainMenuTank.Dead = false;

            MainMenuTank.TurretRotation = 0f;
            MainMenuTank.TurretRotation = 0f;

            MainMenuTank.Projection = Projection;
            MainMenuTank.View = ForwardView;

            tanks.Add(MainMenuTank);
        }

        public static void RemoveAllMenuTanks()
        {
            for (int i = 0; i < tanks.Count; i++)
            {
                tanks[i].RemoveSilently();
            }
        }

        public static void Render()
        {
            if (Active)
            {
                if ((NetPlay.CurrentServer is not null || Client.IsClientConnected()) && Server.ConnectedClients is not null || NetPlay.ServerName is not null)
                {
                    Vector2 initialPosition = new(GameUtils.WindowWidth * 0.75f, GameUtils.WindowHeight * 0.25f);
                    TankGame.spriteBatch.DrawString(TankGame.TextFont, $"\"{NetPlay.ServerName}\"", initialPosition - new Vector2(0, 40), Color.White, 0.6f);
                    TankGame.spriteBatch.DrawString(TankGame.TextFont, $"Connected Players:", initialPosition, Color.White, 0.6f);
                    for (int i = 0; i < Server.ConnectedClients.Count(x => x is not null); i++)
                    {
                        var client = Server.ConnectedClients[i];

                        TankGame.spriteBatch.DrawString(TankGame.TextFont, $"{client.Name}", initialPosition + new Vector2(0, 20) * (i + 1), Color.White, 0.6f);
                    }
                }
                var display = $"Tanks! Remake v{TankGame.Instance.GameVersion}\nThe original game and assets used in this game belongs to Nintendo\nDeveloped by RighteousRyan\nTANKS to all our contributors!";
                TankGame.spriteBatch.DrawString(TankGame.TextFont, display, new(8, GameUtils.WindowHeight - 8), Color.White, new(0.6f), 0f, new Vector2(0, TankGame.TextFont.MeasureString(display).Y));
            }
        }
    }

    public static class DifficultyModes
    {
        public static bool MakeTanksWoke { get; set; } = false;
    }
}
