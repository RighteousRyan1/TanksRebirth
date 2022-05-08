using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TanksRebirth.Internals.Common.GameInput;
using TanksRebirth.Internals.Core;
using TanksRebirth.Internals.Common.GameUI;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals.Common;
using TanksRebirth.Graphics;
using System;
using TanksRebirth.Internals.UI;
using TanksRebirth.Internals.Common.Framework.Input;
using System.Collections.Generic;
using System.Linq;
using FontStashSharp;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals;
using Microsoft.Xna.Framework.Audio;
using TanksRebirth.Enums;
using TanksRebirth.Net;
using System.IO;
using System.Diagnostics;

namespace TanksRebirth.GameContent.UI
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

        internal static List<UIElement> campaignNames = new();

        public static UITextInput UsernameInput;
        public static UITextInput IPInput;
        public static UITextInput PortInput;
        public static UITextInput PasswordInput;
        public static UITextInput ServerNameInput;

        #endregion

        #region Diff Buttons

        public static UITextButton TanksAreCalculators; // make them calculate shots abnormally
        public static UITextButton PieFactory;
        public static UITextButton UltraMines;
        public static UITextButton BulletHell;
        public static UITextButton AllInvisible;
        public static UITextButton AllStationary;
        public static UITextButton Armored;
        public static UITextButton AllHoming;
        public static UITextButton BumpUp;
        public static UITextButton MeanGreens;
        public static UITextButton InfiniteLives;

        public static UITextButton MasterModBuff;
        public static UITextButton MarbleModBuff;

        public static UITextButton MachineGuns;

        public static UITextButton RandomizedTanks;

        public static UITextButton ThunderMode;

        public static UITextButton ThirdPerson;

        public static UITextButton AiCompanion;
        public static UITextButton Shotguns;
        #endregion

        private static float _tnkSpeed = 2.4f;

        public static Tank MainMenuTank;
        // TODO: get menu visuals working

        public static void Initialize()
        {
            ForwardView = Matrix.CreateScale(10) * Matrix.CreateLookAt(new(0, 0, 500), Vector3.Zero, Vector3.Up)
                * Matrix.CreateFromYawPitchRoll(1.563f, 0, 0.906f)
                * Matrix.CreateTranslation(499, 260, 150);
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
            PlayButton.SetDimensions(700, 550, 500, 50);
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
                SetCampaignDisplay();
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
                    Server.StartServer(ServerNameInput.GetRealText(), port, IPInput.Text, PasswordInput.Text);
                    NetPlay.ServerName = ServerNameInput.Text;

                    Client.CreateClient(UsernameInput.Text);
                    Client.AttemptConnectionTo(IPInput.Text, port, PasswordInput.Text);

                    Server.ConnectedClients[0] = NetPlay.CurrentClient;

                    StartMPGameButton.IsVisible = true;
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
                DefaultString = "Username"
            };
            UsernameInput.SetDimensions(100, 400, 500, 50);

            IPInput = new(font, Color.WhiteSmoke, 1f, 15)
            {
                IsVisible = false,
                DefaultString = "Server IP address"
            };
            IPInput.SetDimensions(100, 500, 500, 50);

            PortInput = new(font, Color.WhiteSmoke, 1f, 5)
            {
                IsVisible = false,
                DefaultString = "Server Port"
            };
            PortInput.SetDimensions(100, 600, 500, 50);

            PasswordInput = new(font, Color.WhiteSmoke, 1f, 10)
            {
                IsVisible = false,
                DefaultString = "Server Password (Empty = None)"
            };
            PasswordInput.SetDimensions(100, 700, 500, 50);

            ServerNameInput = new(font, Color.WhiteSmoke, 1f, 10)
            {
                IsVisible = false,
                DefaultString = "Server Name (Server Creation)"
            };
            ServerNameInput.SetDimensions(100, 800, 500, 50);
            #endregion

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
            TanksAreCalculators = new("Tanks are Calculators", font, Color.White)
            {
                IsVisible = false,
                Tooltip = "ALL tanks will begin to look for angles" +
                "\non you (and other enemies) outside of their immediate aim." +
                "\nDo note that this uses significantly more CPU power.",
                OnLeftClick = (elem) => Difficulties.Types["TanksAreCalculators"] = !Difficulties.Types["TanksAreCalculators"]
            };
            TanksAreCalculators.SetDimensions(100, 300, 300, 40);

            PieFactory = new("Lemon Pie Factory", font, Color.White)
            {
                IsVisible = false,
                Tooltip = "Makes yellow tanks absurdly more dangerous by" +
                "\nturning them into mine-laying machines." +
                "\nOh, yeah. They're immune to explosions now too.",
                OnLeftClick = (elem) => Difficulties.Types["PieFactory"] = !Difficulties.Types["PieFactory"]
            };
            PieFactory.SetDimensions(100, 350, 300, 40);

            UltraMines = new("Ultra Mines", font, Color.White)
            {
                IsVisible = false,
                Tooltip = "Mines are now 2x as deadly!" +
                "\nTheir explosion radii are now 2x as big!",
                OnLeftClick = (elem) => Difficulties.Types["UltraMines"] = !Difficulties.Types["UltraMines"]
            };
            UltraMines.SetDimensions(100, 400, 300, 40);

            BulletHell = new("東方 Mode", font, Color.White)
            {
                IsVisible = false,
                Tooltip = "Ricochet counts are now tripled!",
                OnLeftClick = (elem) => Difficulties.Types["BulletHell"] = !Difficulties.Types["BulletHell"]
            };
            BulletHell.SetDimensions(100, 450, 300, 40);

            AllInvisible = new("All Invisible", font, Color.White)
            {
                IsVisible = false,
                Tooltip = "Every single non-player tank is now invisible and no longer lay tracks!",
                OnLeftClick = (elem) => Difficulties.Types["AllInvisible"] = !Difficulties.Types["AllInvisible"]
            };
            AllInvisible.SetDimensions(100, 500, 300, 40);

            AllStationary = new("All Stationary", font, Color.White)
            {
                IsVisible = false,
                Tooltip = "Every single non-player tank is now stationary." +
                "\nThis should REDUCE difficulty.",
                OnLeftClick = (elem) => Difficulties.Types["AllStationary"] = !Difficulties.Types["AllStationary"]
            };
            AllStationary.SetDimensions(100, 550, 300, 40);

            AllHoming = new("Seekers", font, Color.White)
            {
                IsVisible = false,
                Tooltip = "Every enemy tank now has homing bullets.",
                OnLeftClick = (elem) => Difficulties.Types["AllHoming"] = !Difficulties.Types["AllHoming"]
            };
            AllHoming.SetDimensions(100, 600, 300, 40);

            Armored = new("Armored", font, Color.White)
            {
                IsVisible = false,
                Tooltip = "Every single non-player tank has 3 armor points added to it.",
                OnLeftClick = (elem) => Difficulties.Types["Armored"] = !Difficulties.Types["Armored"]
            };
            Armored.SetDimensions(100, 650, 300, 40);

            BumpUp = new("Bump Up", font, Color.White)
            {
                IsVisible = false,
                Tooltip = "Makes the game a bit harder by \"Bumping up\" each tank, giving them one extra tier.",
                OnLeftClick = (elem) => Difficulties.Types["BumpUp"] = !Difficulties.Types["BumpUp"]
            };
            BumpUp.SetDimensions(100, 700, 300, 40);

            MeanGreens = new("Mean Greens", font, Color.White)
            {
                IsVisible = false,
                Tooltip = "Makes every tank a green tank." +
                "\n\"Bump Up\" effects are nullified.",
                OnLeftClick = (elem) => Difficulties.Types["MeanGreens"] = !Difficulties.Types["MeanGreens"]
            };
            MeanGreens.SetDimensions(100, 750, 300, 40);

            InfiniteLives = new("Infinite Lives", font, Color.White)
            {
                IsVisible = false,
                Tooltip = "You now have infinite lives. Have fun!",
                OnLeftClick = (elem) => Difficulties.Types["InfiniteLives"] = !Difficulties.Types["InfiniteLives"]
            };
            InfiniteLives.SetDimensions(450, 300, 300, 40);

            MasterModBuff = new("Master Mod Buff", font, Color.White)
            {
                IsVisible = false,
                Tooltip = "Vanilla tanks become their master mod counterparts." +
                "\nWill not work with \"Marble Mod Buff\" enabled.",
                OnLeftClick = (elem) => Difficulties.Types["MasterModBuff"] = !Difficulties.Types["MasterModBuff"]
            };
            MasterModBuff.SetDimensions(450, 350, 300, 40);

            MarbleModBuff = new("Marble Mod Buff", font, Color.White)
            {
                IsVisible = false,
                Tooltip = "Vanilla tanks become their marble mod counterparts." +
                "\nWill not work with \"Master Mod Buff\" enabled.",
                OnLeftClick = (elem) => Difficulties.Types["MarbleModBuff"] = !Difficulties.Types["MarbleModBuff"]
            };
            MarbleModBuff.SetDimensions(450, 400, 300, 40);

            MachineGuns = new("Machine Guns", font, Color.White)
            {
                IsVisible = false,
                Tooltip = "Every tank now sprays bullets at you.",
                OnLeftClick = (elem) => Difficulties.Types["MachineGuns"] = !Difficulties.Types["MachineGuns"]
            };
            MachineGuns.SetDimensions(450, 450, 300, 40);
            
            RandomizedTanks = new("Randomized Tanks", font, Color.White)
            {
                IsVisible = false,
                Tooltip = "Every tank is now randomized." +
                "\nA black tank could appear where a brown tank would be!",
                OnLeftClick = (elem) => Difficulties.Types["RandomizedTanks"] = !Difficulties.Types["RandomizedTanks"]
            };
            RandomizedTanks.SetDimensions(450, 500, 300, 40);
            
            ThunderMode = new("Thunder Mode", font, Color.White)
            {
                IsVisible = false,
                Tooltip = "The scene is much darker, and thunder is your only source of decent light.",
                OnLeftClick = (elem) => Difficulties.Types["ThunderMode"] = !Difficulties.Types["ThunderMode"]
            };
            ThunderMode.SetDimensions(450, 550, 300, 40);
            
            ThirdPerson = new("Third Person Mode", font, Color.White)
            {
                IsVisible = false,
                Tooltip = "Make the game a third person shooter!" +
                "\nYou can move around inter-directionally with WASD, and aim by dragging the mouse.",
                OnLeftClick = (elem) => Difficulties.Types["ThirdPerson"] = !Difficulties.Types["ThirdPerson"]
            };
            ThirdPerson.SetDimensions(450, 600, 300, 40);

            AiCompanion = new("AI Companion", font, Color.White)
            {
                IsVisible = false,
                Tooltip = "A random tank will spawn at your location and help you throughout every mission.",
                OnLeftClick = (elem) => Difficulties.Types["AiCompanion"] = !Difficulties.Types["AiCompanion"]
            };
            AiCompanion.SetDimensions(450, 650, 300, 40);

            Shotguns = new("Shotguns", font, Color.White)
            {
                IsVisible = false,
                Tooltip = "Every tank now fires a spread of bullets.",
                OnLeftClick = (elem) => Difficulties.Types["Shotguns"] = !Difficulties.Types["Shotguns"]
            };
            Shotguns.SetDimensions(450, 700, 300, 40);
        }
        private static void SetCampaignDisplay()
        {
            SetPlayButtonsVisibility(false);
            
            foreach (var elem in campaignNames)
                elem?.Remove();
            // get all the campaign folders from the SaveDirectory + Campaigns
            Directory.CreateDirectory(Path.Combine(TankGame.SaveDirectory, "Campaigns"));
            var campaignFolders = IOUtils.GetSubFolders(Path.Combine(TankGame.SaveDirectory, "Campaigns"), true);
            var campaignPaths = IOUtils.GetSubFolders(Path.Combine(TankGame.SaveDirectory, "Campaigns"), false);

            // add a new UIElement for each campaign folder
            int totalOffset = 0;
            
            for (int i = 0; i < campaignFolders.Length; i++)
            {

                int offset = i * 60;
                totalOffset += offset;
                var name = campaignFolders[i];
                var fullPath = campaignPaths[i];

                // get all mission files from the campaign folder
                var missions = Directory.GetFiles(fullPath).Where(str => str.EndsWith(".mission")).ToArray();

                int numTanks = 0;

                var campaign = Campaign.LoadFromFolder(name, false);

                foreach (var path in missions)
                {
                    var mission = Path.GetFileName(path);
                    // load the mission file, then count each tank, then add that to the total
                    var loaded = Mission.Load(mission, name);
                    numTanks += loaded.Tanks.Count(x => !x.IsPlayer);
                }

                var elem = new UITextButton(name, TankGame.TextFont, Color.White, 0.8f)
                {
                    IsVisible = true,
                    Tooltip = missions.Length + " missions" +
                    $"\n{numTanks} tanks total" +
                    $"\n\nName: {campaign.Properties.Name}" +
                    $"\nDescription: {campaign.Properties.Description}" +
                    $"\nVersion: {campaign.Properties.Version}" +
                    $"\nStarting Lives: {campaign.Properties.StartingLives}" +
                    // display all tags in a string
                    $"\nTags: {string.Join(", ", campaign.Properties.Tags)}"
                };
                elem.SetDimensions(700, 100 + offset, 300, 40);
                //elem.HasScissor = true;
                //elem.
                elem.OnLeftClick += (el) =>
                {
                    GameHandler.LoadedCampaign = Campaign.LoadFromFolder(elem.Text, true);

                    foreach (var elem in campaignNames)
                        elem.Remove();

                    IntermissionSystem.TimeBlack = 240;

                    GameHandler.ShouldMissionsProgress = true;

                    // GameHandler.LoadedCampaign.LoadMission(20);

                    // Leave();

                    IntermissionSystem.SetTime(600);
                };
                elem.OnMouseOver = (uiElement) => { SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>("Assets/sounds/menu/menu_tick"), SoundContext.Effect); };
                campaignNames.Add(elem);
            }
            var extra = new UITextButton("Freeplay", TankGame.TextFont, Color.White, 0.8f)
            {
                IsVisible = true,
                Tooltip = "Play without a campaign!",
            };
            extra.SetDimensions(1150, 100, 300, 40);
            extra.OnMouseOver = (uiElement) => { SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>("Assets/sounds/menu/menu_tick"), SoundContext.Effect); };
            //elem.HasScissor = true;
            //elem.
            extra.OnLeftClick += (el) =>
            {
                foreach (var elem in campaignNames)
                    elem.Remove();

                GameHandler.ShouldMissionsProgress = false;

                IntermissionSystem.TimeBlack = 150;
                // Leave();
            };
            campaignNames.Add(extra);
            
            TankGame.cunoSucksElement.Remove();
            TankGame.cunoSucksElement = new();
            TankGame.cunoSucksElement.SetDimensions(-1000789342, -783218, 0, 0);

            GC.Collect();
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
            TanksAreCalculators.IsVisible = visible;
            PieFactory.IsVisible = visible;
            UltraMines.IsVisible = visible;
            BulletHell.IsVisible = visible;
            AllInvisible.IsVisible = visible;
            AllStationary.IsVisible = visible;
            Armored.IsVisible = visible;
            AllHoming.IsVisible = visible;
            BumpUp.IsVisible = visible;
            MeanGreens.IsVisible = visible;
            InfiniteLives.IsVisible = visible;
            MasterModBuff.IsVisible = visible;
            MarbleModBuff.IsVisible = visible;
            MachineGuns.IsVisible = visible;
            RandomizedTanks.IsVisible = visible;
            ThunderMode.IsVisible = visible;
            ThirdPerson.IsVisible = visible;
            AiCompanion.IsVisible = visible;
            Shotguns.IsVisible = visible;
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
            ConnectToServerButton.IsVisible = visible;
            CreateServerButton.IsVisible = visible;
            UsernameInput.IsVisible = visible;
            IPInput.IsVisible = visible;
            PasswordInput.IsVisible = visible;
            PortInput.IsVisible = visible;
            ServerNameInput.IsVisible = visible;
            StartMPGameButton.IsVisible = visible && Server.serverNetManager is not null;
        }

        public static void Update()
        {
            TanksAreCalculators.Color = Difficulties.Types["TanksAreCalculators"] ? Color.Lime : Color.Red;
            PieFactory.Color = Difficulties.Types["PieFactory"] ? Color.Lime : Color.Red;
            UltraMines.Color = Difficulties.Types["UltraMines"] ? Color.Lime : Color.Red;
            BulletHell.Color = Difficulties.Types["BulletHell"] ? Color.Lime : Color.Red;
            AllInvisible.Color = Difficulties.Types["AllInvisible"] ? Color.Lime : Color.Red;
            AllStationary.Color = Difficulties.Types["AllStationary"] ? Color.Lime : Color.Red;
            AllHoming.Color = Difficulties.Types["AllHoming"] ? Color.Lime : Color.Red;
            Armored.Color = Difficulties.Types["Armored"] ? Color.Lime : Color.Red;
            BumpUp.Color = Difficulties.Types["BumpUp"] ? Color.Lime : Color.Red;
            MeanGreens.Color = Difficulties.Types["MeanGreens"] ? Color.Lime : Color.Red;
            InfiniteLives.Color = Difficulties.Types["InfiniteLives"] ? Color.Lime : Color.Red;
            MasterModBuff.Color = Difficulties.Types["MasterModBuff"] ? Color.Lime : Color.Red;
            MarbleModBuff.Color = Difficulties.Types["MarbleModBuff"] ? Color.Lime : Color.Red;
            MachineGuns.Color = Difficulties.Types["MachineGuns"] ? Color.Lime : Color.Red;
            RandomizedTanks.Color = Difficulties.Types["RandomizedTanks"] ? Color.Lime : Color.Red;
            ThunderMode.Color = Difficulties.Types["ThunderMode"] ? Color.Lime : Color.Red;
            ThirdPerson.Color = Difficulties.Types["ThirdPerson"] ? Color.Lime : Color.Red;
            AiCompanion.Color = Difficulties.Types["AiCompanion"] ? Color.Lime : Color.Red;
            Shotguns.Color = Difficulties.Types["Shotguns"] ? Color.Lime : Color.Red;

            Theme.Volume = TankGame.Settings.MusicVolume;

            foreach (var tnk in tanks)
            {
                if (tnk is not AITank)
                    return;
                var pos = tnk.Body.Position;
                if (pos.X > 500)
                    pos.X = -500;

                tnk.Velocity.Y = _tnkSpeed;

                tnk.Body.Position = pos;
            }
        }

        public static void Leave()
        {
            PlayerTank.Lives = PlayerTank.StartingLives;
            GameHandler.StartTnkScene();
            SetMPButtonsVisibility(false);
            SetPlayButtonsVisibility(false);
            SetPrimaryMenuButtonsVisibility(false);
            GraphicsUI.BatchVisible = false;
            ControlsUI.BatchVisible = false;
            VolumeUI.BatchVisible = false;
            GameUI.InOptions = false;
            Active = false;
            Theme.Stop();

            RemoveAllMenuTanks();

            GameUI.OptionsButton.Size.Y = 150;
            GameUI.QuitButton.Size.Y = 150;

            Theme.Volume = 0;

            GameUI.QuitButton.Position.Y += 50;
            GameUI.OptionsButton.Position.Y -= 75;

            HideAll();
        }

        public static void Open()
        {
            Active = true;
            GameUI.Paused = false;
            Theme.Volume = 0.5f;
            Theme.Play();

            foreach (var block in Block.AllBlocks)
                block?.Remove();
            foreach (var mine in Mine.AllMines)
                mine?.Remove();
            foreach (var shell in Shell.AllShells)
                shell?.Remove();
            foreach (var tank in GameHandler.AllTanks)
                tank?.Remove();

            if (GameHandler.ClearTracks is not null)
                GameHandler.ClearTracks.OnLeftClick?.Invoke(null);
            if (GameHandler.ClearChecks is not null)
                GameHandler.ClearChecks.OnLeftClick?.Invoke(null);

            TankGame.OverheadView = false;
            TankGame.CameraRotationVector.Y = TankGame.DEFAULT_ORTHOGRAPHIC_ANGLE;
            TankGame.AddativeZoom = 1f;
            TankGame.CameraFocusOffset.Y = 0f;

            GameUI.QuitButton.Position.Y -= 50;
            GameUI.OptionsButton.Position.Y += 75;

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    var t = AddTravelingTank(AITank.PickRandomTier(), 1000 + (-i * 100), j * 55);

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
            var extank = new AITank(tier, default, true, false)
            {
                Team = TankTeam.NoTeam,
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

            MainMenuTank = new PlayerTank(PlayerType.Blue)
            {
                IsIngame = false,
                Dead = false,

                TurretRotation = 0f,

                Projection = Projection,
                View = ForwardView
            };

            tanks.Add(MainMenuTank);
        }

        public static void RemoveAllMenuTanks()
        {
            for (int i = 0; i < tanks.Count; i++)
            {
                tanks[i].Remove();
            }
            tanks.Clear();
        }

        private static readonly string tanksMessage = $"Tanks! Rebirth ALPHA v{TankGame.Instance.GameVersion}\nThe original game and assets used in this game belongs to Nintendo\nDeveloped by RighteousRyan\nTANKS to all our contributors!";
        private static readonly string keyDisplay = "For anyone needing a list of keys for\ndebugging purposes, here you go:\n" +
            "i - spawns the powerup listed at the top\n" +
            "; - spawns a  mine at the mouse\n" +
            "' - spawns a still bullet at the mouse\n" +
            "insert - hides all debugging UI\n" +
            "mouse2 - allows you to change the angle of the camera\n" +
            "mouse3 - allows the dragging of the camera on an axis\n" +
            "add and subtract - allow scaling of the game\n" +
            "q - toggles fps mode (not working ATM)\n" +
            "num7 and num9 - changes tank to spawn\n" +
            "num1 and num3 - changes the team of the tank spawned\n" +
            "k while hovering a tank - kill that tank\n" +
            "home - spawn a tank\n" +
            "end - spawn a tank crate\n" +
            "pgup - spawns a plethora of the tank type you choose and it's respective team\n" +
            "pgdown - spawns a player\n" +
            "mult - change debug level by +1\n" +
            "divide - change debug level by -1\n" +
            "z and x - change placed block type\n" +
            "j - overhead level editor view (level editor) (for now)\n" +
            ", and . - change block heights\n\n" +
            "Also, ahead of time- sorry if you have a < 66% keyboard!";
        public static void Render()
        {
            if (Active)
            {
                #region Various things
                if ((NetPlay.CurrentServer is not null && (Server.ConnectedClients is not null || NetPlay.ServerName is not null)) || (Client.IsConnected() && Client.lobbyDataReceived))
                {
                    Vector2 initialPosition = new(GameUtils.WindowWidth * 0.75f, GameUtils.WindowHeight * 0.25f);
                    TankGame.spriteBatch.DrawString(TankGame.TextFont, $"\"{NetPlay.ServerName}\"", initialPosition - new Vector2(0, 40), Color.White, 0.6f);
                    TankGame.spriteBatch.DrawString(TankGame.TextFont, $"Connected Players:", initialPosition, Color.White, 0.6f);
                    for (int i = 0; i < Server.ConnectedClients.Count(x => x is not null); i++)
                    {
                        var client = Server.ConnectedClients[i];

                        Color textCol = Color.White;
                        if (NetPlay.CurrentClient.Id == i)
                            textCol = Color.Green;

                        TankGame.spriteBatch.DrawString(TankGame.TextFont, $"{client.Name}" + $" ({client.Id})", initialPosition + new Vector2(0, 20) * (i + 1), textCol, 0.6f);
                    }
                }
                var tanksMessageSize = TankGame.TextFont.MeasureString(tanksMessage);
                TankGame.spriteBatch.DrawString(TankGame.TextFont, tanksMessage, new(8, GameUtils.WindowHeight - 8), Color.White, new(0.6f), 0f, new Vector2(0, tanksMessageSize.Y));

                TankGame.spriteBatch.DrawString(TankGame.TextFont, tanksMessage, new(8, GameUtils.WindowHeight - 8), Color.White, new(0.6f), 0f, new Vector2(0, tanksMessageSize.Y));
                if (PlayButton.IsVisible)
                    TankGame.spriteBatch.DrawString(TankGame.TextFont, keyDisplay, new(12, 12), Color.White, new(0.6f), 0f, Vector2.Zero);
                #endregion

                if (campaignNames.Count == 1)
                {
                    TankGame.spriteBatch.DrawString(TankGame.TextFont, $"You have no campaigns!" +
                        $"\nTry downloading the Vanilla campaign by pressing 'Enter' or making your own." +
                        $"\nCampaign folders belong in '{Path.Combine(TankGame.SaveDirectory, "Campaigns")}' (press TAB to open on Windows)", new(12, 12), Color.White, new(0.75f), 0f, Vector2.Zero);

                    if (TankGame.IsWindows)
                    {
                        if (Input.KeyJustPressed(Keys.Tab))
                        {
                            if (Directory.Exists(Path.Combine(TankGame.SaveDirectory, "Campaigns")))
                                Process.Start("explorer.exe", Path.Combine(TankGame.SaveDirectory, "Campaigns"));
                            // do note that this fails on windows lol
                        }
                        if (Input.KeyJustPressed(Keys.Enter))
                        {
                            Process.Start(new ProcessStartInfo("https://github.com/RighteousRyan1/TanksRebirth/releases/download/1.3-alpha/VanillaCampaign.rar")
                            {
                                UseShellExecute = true,
                            });
                        }
                    }
                }
            }
        }
    }
}
