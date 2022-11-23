using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TanksRebirth.Internals.Common;
using System;
using TanksRebirth.Internals.UI;
using System.Collections.Generic;
using System.Linq;
using FontStashSharp;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;

using TanksRebirth.GameContent.Properties;
using TanksRebirth.GameContent.Cosmetics;
using TanksRebirth.GameContent.ModSupport;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Systems.Coordinates;
using TanksRebirth.Internals.Common.GameUI;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals;
using TanksRebirth.Net;
using System.Xml.Linq;

namespace TanksRebirth.GameContent.UI
{
    public static class MainMenu
    {
        public static bool Active { get; private set; } = true;

        public static OggMusic Theme;
        private static bool _musicFading;

        private static List<Tank> tanks = new();

        private static Matrix View;
        private static Matrix Projection;

        public delegate void MenuOpenDelegate();
        public delegate void MenuCloseDelegate();
        public static event MenuOpenDelegate OnMenuOpen;
        public static event MenuCloseDelegate OnMenuClose;

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
        public static UITextButton DisconnectButton;

        public static UITextButton CosmeticsMenuButton;

        public static UITextButton StatsMenu;

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
        public static UITextButton Predictions;

        public static UITextButton RandomizedPlayer;
        public static UITextButton BulletBlocking;

        public static UITextButton FFA;

        public static UITextButton LanternMode;

        #endregion

        private static float _tnkSpeed = 2.4f;

        public static int MissionCheckpoint = 0;

        public static Texture2D LogoTexture;
        public static Vector2 LogoPosition;
        public static Vector2 LogoScale = new(0.5f);
        public static float LogoRotation;
        public static float LogoRotationSpeed = 1f;

        private static float _sclOffset = 0f;
        private static float _sclApproach = 0.5f;
        private static float _sclAcc = 0.005f;

        // TODO: get menu visuals working

        public static RenderableCrate Crate;

        private static int _spinCd = 30;
        private static float _spinOffset; // shut up IDE pls CS0649 bs

        private static float _spinTarget;

        //private static float _spinMod = 0.001f;
        //private static float _spinLenience = 1.5f;
        private static float _bpm = 75;
        private static float _rotationBpm = 500; // more? (200 default)
        private static float _sclMax = 50; // 250 default
        private static float _rotDelta = 5; // 300 usually?

        // not always properly set, fix later
        // this code is becoming so shit i want to vomit but i don't know any better
        public enum State
        {
            LoadingMods,
            PrimaryMenu,
            PlayList,
            Campaigns,
            Mulitplayer,
            Cosmetics,
            Difficulties,
            Options,
            StatsMenu
        }

        public static State MenuState;

        #region Chest Stuff
        private static bool _openingCrate; // we don't use this yet since the stuff isn't exactly implemented.
        #endregion

        private static bool _initialized;

        public static void InitializeBasics()
        {
            MenuState = State.PrimaryMenu;
            Crate = new(new(0, 0, 0), TankGame.GameView, TankGame.GameProjection);
            Crate.ChestPosition = new(0, 500, 250);
            Crate.LidPosition = Crate.ChestPosition; //new(67.5f, 500, 137.5f);
            Crate.Rotation.Y = MathHelper.Pi + 0.25f;
            Crate.Rotation.X = -MathHelper.PiOver4;
            Crate.Rotation.Z = 0f;
            Crate.LidRotation = Crate.Rotation;
            LogoTexture = GameResources.GetGameResource<Texture2D>("Assets/tanks_rebirth_logo");

            Projection = Matrix.CreateOrthographic(TankGame.Instance.GraphicsDevice.Viewport.Width, TankGame.Instance.GraphicsDevice.Viewport.Height, -2000f, 5000f);
            //Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(90), TankGame.Instance.GraphicsDevice.Viewport.AspectRatio, 0.01f, 1000f);
            View = Matrix.CreateScale(2) * Matrix.CreateLookAt(new(0, 0, 500), Vector3.Zero, Vector3.Up) * Matrix.CreateRotationX(MathHelper.PiOver2);

            // AddTravelingTank(TankTier.Black, 200, 200);

            Theme = new("Main Menu Theme", "Content/Assets/mainmenu/theme", TankGame.Settings.MusicVolume);
            Theme.Volume = TankGame.Settings.MusicVolume * 0.1f;
        }
        public static void InitializeUIGraphics()
        {
            _initialized = true;

            TankGame.Instance.Window.ClientSizeChanged += UpdateProjection;
            var font = TankGame.TextFont;
            #region Init Standard Buttons
            PlayButton = new(TankGame.GameLanguage.Play, font, Color.WhiteSmoke)
            {
                IsVisible = true,
            };
            PlayButton.SetDimensions(() => new Vector2(700, 550).ToResolution(), () => new Vector2(500, 50).ToResolution());
            PlayButton.OnLeftClick = (uiElement) =>
            {
                GameUI.BackButton.IsVisible = true;
                MenuState = State.PlayList;
            };

            PlayButton_Multiplayer = new(TankGame.GameLanguage.Multiplayer, font, Color.WhiteSmoke)
            {
                IsVisible = false,
                Tooltip = "In the works!"
            };
            PlayButton_Multiplayer.SetDimensions(() => new Vector2(700, 750).ToResolution(), () => new Vector2(500, 50).ToResolution());

            PlayButton_Multiplayer.OnLeftClick = (uiElement) =>
            {
                SetPlayButtonsVisibility(false);
                SetMPButtonsVisibility(true);
                MenuState = State.Mulitplayer;
            };

            DifficultiesButton = new(TankGame.GameLanguage.Difficulties, font, Color.WhiteSmoke)
            {
                IsVisible = false,
                Tooltip = "Change the difficulty of the game."
            };
            DifficultiesButton.SetDimensions(() => new Vector2(700, 550).ToResolution(), () => new Vector2(500, 50).ToResolution());
            DifficultiesButton.OnLeftClick = (element) =>
            {
                MenuState = State.Difficulties;
            };


            PlayButton_SinglePlayer = new(TankGame.GameLanguage.SinglePlayer, font, Color.WhiteSmoke)
            {
                IsVisible = false,
                Tooltip = "Choose from your downloaded campaigns."
            };
            PlayButton_SinglePlayer.SetDimensions(() => new Vector2(700, 450).ToResolution(), () => new Vector2(500, 50).ToResolution());

            PlayButton_SinglePlayer.OnLeftClick = (uiElement) =>
            {
                SetCampaignDisplay();
                MenuState = State.Campaigns;
            };

            InitializeDifficultyButtons();

            PlayButton_LevelEditor = new(TankGame.GameLanguage.LevelEditor, font, Color.WhiteSmoke) {
                IsVisible = false,
                Tooltip = "Create your own levels and campaigns with ease!"
            };
            PlayButton_LevelEditor.SetDimensions(() => new Vector2(700, 650).ToResolution(), () => new Vector2(500, 50).ToResolution());
            PlayButton_LevelEditor.OnLeftClick = (b) => {
                LevelEditor.Initialize();
                LevelEditor.Open();
            };

            ConnectToServerButton = new(TankGame.GameLanguage.ConnectToServer, font, Color.WhiteSmoke) {
                IsVisible = false,
                Tooltip = "Connect to the written IP and Port in the form of ip:port"
            };
            ConnectToServerButton.SetDimensions(() => new Vector2(700, 100).ToResolution(), () => new Vector2(500, 50).ToResolution());
            ConnectToServerButton.OnLeftClick = (uiButton) => {
                /*if (UsernameInput.IsEmpty()) {
                    SoundPlayer.PlaySoundInstance("Assets/sounds/menu/menu_error", SoundContext.Effect);
                    ChatSystem.SendMessage("Your username is empty!", Color.Red);
                    return;
                }
                if (PortInput.IsEmpty()) {
                    SoundPlayer.PlaySoundInstance("Assets/sounds/menu/menu_error", SoundContext.Effect);
                    ChatSystem.SendMessage("The port is empty!", Color.Red);
                    return;
                }
                if (IPInput.IsEmpty()) {
                    SoundPlayer.PlaySoundInstance("Assets/sounds/menu/menu_error", SoundContext.Effect);
                    ChatSystem.SendMessage("The IP address is not valid.", Color.Red);
                    return;
                }*/

                if (int.TryParse(PortInput.Text, out var port)) {
                    Client.CreateClient(UsernameInput.GetRealText());
                    Client.AttemptConnectionTo(IPInput.Text, port, PasswordInput.Text);
                }
                else {
                    //SoundPlayer.PlaySoundInstance("Assets/sounds/menu/menu_error", SoundContext.Effect);
                    //ChatSystem.SendMessage("That is not a valid port.", Color.Red);

                    Client.CreateClient("client");
                    Client.AttemptConnectionTo("localhost", 7777, string.Empty);
                }
            };

            CreateServerButton = new(TankGame.GameLanguage.CreateServer, font, Color.WhiteSmoke)
            {
                IsVisible = false,
                Tooltip = "Create a server with the written IP and Port in the form of ip:port"
            };
            CreateServerButton.SetDimensions(() => new Vector2(700, 350).ToResolution(), () => new Vector2(500, 50).ToResolution());
            CreateServerButton.OnLeftClick = (uiButton) =>
            {
                if (int.TryParse(PortInput.Text, out var port))
                {
                    Server.CreateServer();

                    Server.StartServer(ServerNameInput.GetRealText(), port, IPInput.GetRealText(), PasswordInput.GetRealText());
                    NetPlay.ServerName = ServerNameInput.GetRealText();

                    Client.CreateClient(UsernameInput.GetRealText());
                    Client.AttemptConnectionTo(IPInput.GetRealText(), port, PasswordInput.GetRealText());

                    Server.ConnectedClients[0] = NetPlay.CurrentClient;

                    StartMPGameButton.IsVisible = true;
                }
                else // debuggin'!
                {
                    //SoundPlayer.PlaySoundInstance("Assets/sounds/menu/menu_error", SoundContext.Effect);
                    //ChatSystem.SendMessage("That is not a valid port.", Color.Red);
                    Server.CreateServer();

                    Server.StartServer("test_name", 7777, "localhost", string.Empty);

                    NetPlay.ServerName = ServerNameInput.GetRealText();

                    Client.CreateClient("host");
                    Client.AttemptConnectionTo("localhost", 7777, string.Empty);

                    Server.ConnectedClients[0] = NetPlay.CurrentClient;
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

                SetPlayButtonsVisibility(false);

                MenuState = State.Campaigns;
            };
            StartMPGameButton.SetDimensions(() => new Vector2(700, 600).ToResolution(), () => new Vector2(500, 50).ToResolution());

            CosmeticsMenuButton = new("Cosmetics Menu", font, Color.WhiteSmoke)
            {
                IsVisible = false,
                Tooltip = "Use some of your currency and luck out\non items for your tank!"
            };
            CosmeticsMenuButton.SetDimensions(() => new Vector2(50, 50).ToResolution(), () => new Vector2(300, 50).ToResolution());
            CosmeticsMenuButton.OnLeftClick += (elem) =>
            {
                CosmeticsMenuButton.IsVisible = false;
                SetPlayButtonsVisibility(false);
                SetMPButtonsVisibility(false);
                SetPrimaryMenuButtonsVisibility(false);

                MenuState = State.Cosmetics;
            };
            #endregion
            #region Input Boxes
            UsernameInput = new(font, Color.WhiteSmoke, 1f, 20)
            {
                IsVisible = false,
                DefaultString = "Username"
            };
            UsernameInput.SetDimensions(() => new Vector2(100, 400).ToResolution(), () => new Vector2(500, 50).ToResolution());

            IPInput = new(font, Color.WhiteSmoke, 1f, 15)
            {
                IsVisible = false,
                DefaultString = "Server IP address"
            };
            IPInput.SetDimensions(() => new Vector2(100, 500).ToResolution(), () => new Vector2(500, 50).ToResolution());

            PortInput = new(font, Color.WhiteSmoke, 1f, 5)
            {
                IsVisible = false,
                DefaultString = "Server Port"
            };
            PortInput.SetDimensions(() => new Vector2(100, 600).ToResolution(), () => new Vector2(500, 50).ToResolution());

            PasswordInput = new(font, Color.WhiteSmoke, 1f, 10)
            {
                IsVisible = false,
                DefaultString = "Server Password (Empty = None)"
            };
            PasswordInput.SetDimensions(() => new Vector2(100, 700).ToResolution(), () => new Vector2(500, 50).ToResolution());

            DisconnectButton = new("Disconnect", font, Color.WhiteSmoke, 1f)
            {
                IsVisible = false,
                OnLeftClick = (arg) =>
                {
                    Client.client.Disconnect();
                }
            };
            DisconnectButton.SetDimensions(() => new Vector2(100, 800).ToResolution(), () => new Vector2(500, 50).ToResolution());

            ServerNameInput = new(font, Color.WhiteSmoke, 1f, 10)
            {
                IsVisible = false,
                DefaultString = "Server Name (Server Creation)"
            };
            ServerNameInput.SetDimensions(() => new Vector2(100, 800).ToResolution(), () => new Vector2(500, 50).ToResolution());

            StatsMenu = new("Game Stats", font, Color.WhiteSmoke)
            { 
                IsVisible = false,
                OnLeftClick = (a) => { MenuState = State.StatsMenu; },
                Tooltip = "View your all-time statistics for this game!"
            };
            StatsMenu.SetDimensions(() => new Vector2(WindowUtils.WindowWidth / 2 - 90.ToResolutionX(), WindowUtils.WindowHeight - 100.ToResolutionY()), () => new Vector2(180, 50).ToResolution());
            #endregion

            Open();

            _menuElements = new UIElement[] 
            { PlayButton, PlayButton_SinglePlayer, PlayButton_LevelEditor, PlayButton_Multiplayer, ConnectToServerButton, 
                CreateServerButton, UsernameInput, IPInput, PortInput, PasswordInput, ServerNameInput,
                DifficultiesButton
            };

            foreach (var e in _menuElements)
                e.OnMouseOver = (uiElement) => { SoundPlayer.PlaySoundInstance("Assets/sounds/menu/menu_tick", SoundContext.Effect); };
        }

        public static void RenderCrate()
        {
            Crate.View = View;
            Crate.Projection = Projection;
            Crate?.Render();
            //Crate.Rotation.Y = MathHelper.Pi;
            //Crate.Rotation.X = MathHelper.PiOver2;

            if (_openingCrate)
                Crate.LidPosition.Z -= 1f;
            else
                Crate.LidPosition = Crate.ChestPosition;
        }

        private static void UpdateLogo()
        {
            LogoPosition = new Vector2(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight / 4);

            //_bpm = MouseUtils.MousePosition.Y / WindowUtils.WindowHeight * 1000;
            //_sclMax = MouseUtils.MousePosition.X / WindowUtils.WindowWidth * 1000;
            // _rotDelta = MouseUtils.MousePosition.Y / WindowUtils.WindowHeight * 1000;

            LogoRotation = MathF.Sin((float)TankGame.LastGameTime.TotalGameTime.TotalMilliseconds / _rotationBpm) / _rotDelta + _spinOffset;
            LogoScale = (MathF.Sin((float)TankGame.LastGameTime.TotalGameTime.TotalMilliseconds / _bpm) / _sclMax + _sclOffset).ToResolution();

            if (_sclOffset < _sclApproach)
                _sclOffset += _sclAcc * TankGame.DeltaTime;
            else
                _sclOffset = _sclApproach;

            //if (_spinOffset <= _spinTarget)
                //GameUtils.SoftStep(ref _spinOffset, _spinTarget, _spinSpeed
                // considerations...
        }

        private static void UpdateProjection(object sender, EventArgs e)
        {
            Projection = Matrix.CreateOrthographic(TankGame.Instance.GraphicsDevice.Viewport.Width, TankGame.Instance.GraphicsDevice.Viewport.Height, -2000f, 5000f);
            View = Matrix.CreateScale(2) * Matrix.CreateLookAt(new(0, 0, 500), Vector3.Zero, Vector3.Up) * Matrix.CreateRotationX(MathHelper.PiOver2);
        }
        private static bool _diffButtonsInitialized;
        private static void InitializeDifficultyButtons()
        {
            _diffButtonsInitialized = true;
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

            //init predictions
            Predictions = new("Predictions", font, Color.White)
            {
                IsVisible = false,
                Tooltip = "Every tank predicts your future position.",
                OnLeftClick = (elem) => Difficulties.Types["Predictions"] = !Difficulties.Types["Predictions"]
            };
            Predictions.SetDimensions(450, 750, 300, 40);

            RandomizedPlayer = new("Randomized Player", font, Color.White)
            {
                IsVisible = false,
                Tooltip = "You become a random enemy tank every life.",
                OnLeftClick = (elem) => Difficulties.Types["RandomPlayer"] = !Difficulties.Types["RandomPlayer"]
            };
            RandomizedPlayer.SetDimensions(800, 300, 300, 40);

            BulletBlocking = new("Bullet Blocking", font, Color.White)
            {
                IsVisible = false,
                Tooltip = "Enemies *attempt* to block your bullets." +
                "\nIt doesn't always work, sometimes even killing teammates.\nHigh fire-rate enemies are mostly affected.",
                OnLeftClick = (elem) => Difficulties.Types["BulletBlocking"] = !Difficulties.Types["BulletBlocking"]
            };
            BulletBlocking.SetDimensions(800, 350, 300, 40);

            FFA = new("Free-for-all", font, Color.White)
            {
                IsVisible = false,
                Tooltip = "Every tank is on their own!",
                OnLeftClick = (elem) => Difficulties.Types["FFA"] = !Difficulties.Types["FFA"]
            };
            FFA.SetDimensions(800, 400, 300, 40);

            LanternMode = new("Lantern Mode", font, Color.White)
            {
                IsVisible = false,
                Tooltip = "Everything is dark. Only you and your lantern can save you now.",
                OnLeftClick = (elem) => {
                    Difficulties.Types["LanternMode"] = !Difficulties.Types["LanternMode"];
                    GameShaders.LanternMode = Difficulties.Types["LanternMode"];
                }
            };
            LanternMode.SetDimensions(800, 450, 300, 40);
        }
        private static void SetCampaignDisplay()
        {
            SetPlayButtonsVisibility(false);

            foreach (var elem in campaignNames)
                elem?.Remove();
            // get all the campaign folders from the SaveDirectory + Campaigns
            var path = Path.Combine(TankGame.SaveDirectory, "Campaigns");
            Directory.CreateDirectory(path);
            // add a new UIElement for each campaign folder

            var campaignFiles = Directory.GetFiles(path).Where(file => file.EndsWith(".campaign")).ToArray();

            for (int i = 0; i < campaignFiles.Length; i++)
            {
                int offset = i * 60;
                var name = campaignFiles[i];

                int numTanks = 0;
                var campaign = Campaign.Load(name);

                var missions = campaign.CachedMissions;

                foreach (var mission in missions)
                {
                    // load the mission file, then count each tank, then add that to the total
                    numTanks += mission.Tanks.Count(x => !x.IsPlayer);
                }

                var elem = new UITextButton(Path.GetFileNameWithoutExtension(name), TankGame.TextFont, Color.White, 0.8f)
                {
                    IsVisible = true,
                    Tooltip = missions.Length + " missions" +
                    $"\n{numTanks} tanks total" +
                    $"\n\nName: {campaign.MetaData.Name}" +
                    $"\nDescription: {campaign.MetaData.Description}" +
                    $"\nVersion: {campaign.MetaData.Version}" +
                    $"\nStarting Lives: {campaign.MetaData.StartingLives}" +
                    $"\nBonus Life Count: {campaign.MetaData.ExtraLivesMissions.Length}" +
                    // display all tags in a string
                    $"\nTags: {string.Join(", ", campaign.MetaData.Tags)}" +
                    $"\n\nRight click to DELETE ME."
                };
                elem.SetDimensions(() => new Vector2(700, 100 + offset).ToResolution(), () => new Vector2(300, 40).ToResolution());
                //elem.HasScissor = true;
                //elem.
                elem.OnLeftClick += (el) =>
                {
                    var noExt = Path.GetFileNameWithoutExtension(name);
                    PrepareGameplay(noExt);
                    TransitionToGame();
                };
                elem.OnRightClick += (el) =>
                {
                    var path = Path.Combine(TankGame.SaveDirectory, "Campaigns", elem.Text);

                    File.Delete(path + ".campaign");
                    SetCampaignDisplay();
                };
                elem.OnMouseOver = (uiElement) => { SoundPlayer.PlaySoundInstance("Assets/sounds/menu/menu_tick", SoundContext.Effect); };
                campaignNames.Add(elem);
            }
            var extra = new UITextButton("Freeplay", TankGame.TextFont, Color.White, 0.8f)
            {
                IsVisible = true,
                Tooltip = "Play without a campaign!",
            };
            extra.SetDimensions(() => new Vector2(1150, 100).ToResolution(), () => new Vector2(300, 40).ToResolution());
            extra.OnMouseOver = (uiElement) => { SoundPlayer.PlaySoundInstance("Assets/sounds/menu/menu_tick", SoundContext.Effect); };
            //elem.HasScissor = true;
            //elem.
            extra.OnLeftClick += (el) =>
            {
                foreach (var elem in campaignNames)
                    elem.Remove();

                GameProperties.ShouldMissionsProgress = false;

                IntermissionSystem.TimeBlack = 150;
            };
            campaignNames.Add(extra);

            UIElement.CunoSucks();
        }
        public static void PrepareGameplay(string name, bool throughNet = false)
        {
            var camp = Campaign.Load(Path.Combine("Campaigns", name + ".campaign"));

            // check if the CampaignCheckpoint number is less than the number of missions in the array
            if (MissionCheckpoint >= camp.CachedMissions.Length)
            {
                // if it is, notify the user that the checkpoint is too high via the chat, and play the error sound
                ChatSystem.SendMessage($"Campaign '{name}' has no mission {MissionCheckpoint + 1}.", Color.Red);
                SoundPlayer.SoundError();
                return;
            }

            // if it is, load the mission
            GameProperties.LoadedCampaign = camp;

            GameProperties.LoadedCampaign.LoadMission(MissionCheckpoint); // loading the mission specified
            if (Client.IsConnected() && !throughNet)
            {
                //Client.SendCampaignBytes(camp);
                Client.SendCampaign(name);
                Client.RequestStartGame(MissionCheckpoint, true);
            }
            PlayerTank.StartingLives = camp.MetaData.StartingLives;
            IntermissionSystem.StripColor = camp.MetaData.MissionStripColor;
            IntermissionSystem.BackgroundColor = camp.MetaData.BackgroundColor;
        }
        public static void TransitionToGame()
        {
            foreach (var elem in campaignNames)
                elem?.Remove();

            IntermissionSystem.TimeBlack = 240;

            GameProperties.ShouldMissionsProgress = true;

            _musicFading = true;

            IntermissionSystem.SetTime(600);
        }
        internal static void SetPlayButtonsVisibility(bool visible)
        {
            PlayButton_SinglePlayer.IsVisible = visible;
            PlayButton_LevelEditor.IsVisible = visible;
            PlayButton_Multiplayer.IsVisible = visible;
            DifficultiesButton.IsVisible = visible;
            CosmeticsMenuButton.IsVisible = visible;
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
            Predictions.IsVisible = visible;
            RandomizedPlayer.IsVisible = visible;
            BulletBlocking.IsVisible = visible;
            FFA.IsVisible = visible;
            LanternMode.IsVisible = visible;
        }
        internal static void SetPrimaryMenuButtonsVisibility(bool visible)
        {
            GameUI.OptionsButton.IsVisible = visible;

            GameUI.QuitButton.IsVisible = visible;

            GameUI.BackButton.Size.Y = 50;

            PlayButton.IsVisible = visible;

            StatsMenu.IsVisible = visible;
        }
        internal static void SetMPButtonsVisibility(bool visible)
        {
            ConnectToServerButton.IsVisible = visible;
            CreateServerButton.IsVisible = visible;
            UsernameInput.IsVisible = visible;
            IPInput.IsVisible = visible;
            PasswordInput.IsVisible = visible;
            PortInput.IsVisible = visible;
            ServerNameInput.IsVisible = visible && !Client.IsConnected();
            DisconnectButton.IsVisible = visible && Client.IsConnected();
            StartMPGameButton.IsVisible = visible && Server.serverNetManager is not null;
        }

        // this method is causing considerable amounts of garbage collection!
        internal static void RenderStats(Vector2 genericStatsPos, Vector2 tankKillsPos, Anchor aligning)
        {
            List<string> info = new()
            {
                $"Total Tank Kills: {TankGame.GameData.TotalKills}",
                $"Tank Kills w/Bullets: {TankGame.GameData.BulletKills}",
                $"Tank Kills w/Bounced Bullets: {TankGame.GameData.BounceKills}",
                $"Tank Kills w/Mines: {TankGame.GameData.MineKills}",
                $"Missions Completed: {TankGame.GameData.MissionsCompleted}",
                $"Campaigns Completed: {TankGame.GameData.CampaignsCompleted}",
                $"Deaths: {TankGame.GameData.Deaths}",
                $"Suicides: {TankGame.GameData.Suicides}",
                $"Time Played Total: {TankGame.GameData.TimePlayed.StringFormat()} ({(TankGame.GameData.TimePlayed.TotalMinutes < 120 ? $"{TankGame.GameData.TimePlayed.TotalMinutes:0.#} minutes" : $"{TankGame.GameData.TimePlayed.TotalHours:#.#} hours")})",
                $"Current Play Session: {TankGame.CurrentSessionTimer.Elapsed.StringFormat()}"
            };

            for (int i = 0; i < info.Count; i++)
                TankGame.SpriteRenderer.DrawString(TankGame.TextFont, info[i], genericStatsPos + Vector2.UnitY * (i * 25), Color.White, Vector2.One, 0f, GameUtils.GetAnchor(aligning, TankGame.TextFont.MeasureString(info[i])), 0f); 
            TankGame.SpriteRenderer.DrawString(TankGame.TextFont, "Tanks Killed by Type:", tankKillsPos, Color.White, Vector2.One, 0f, GameUtils.GetAnchor(aligning, TankGame.TextFont.MeasureString("Tanks Killed by Type:")), 0f);
            for (int i = 2; i < TankGame.GameData.TankKills.Count; i++) {
                var elem = TankGame.GameData.TankKills.ElementAt(i);
                var split = TankID.Collection.GetKey(elem.Key).SplitByCamel();
                var display = $"{split}: {elem.Value}";
                TankGame.SpriteRenderer.DrawString(TankGame.TextFont, display, tankKillsPos + Vector2.UnitY * ((i - 1) * 25), Color.White, Vector2.One, 0f, GameUtils.GetAnchor(aligning, TankGame.TextFont.MeasureString(display)), 0f);
            }
            if (TankGame.GameData.ReadOutdatedFile)
                TankGame.SpriteRenderer.DrawString(TankGame.TextFont, $"Outdated save file ({TankGame.GameData.Name})! Delete the old one!", new Vector2(8, 8), Color.White, Vector2.One, 0f, Vector2.Zero, 0f);
            TankGame.SpriteRenderer.DrawString(TankGame.TextFont, "Press ESC to return", WindowUtils.WindowBottom - Vector2.UnitY * 40, Color.White, Vector2.One, 0f, GameUtils.GetAnchor(aligning, TankGame.TextFont.MeasureString("Press ESC to leave")), 0f);
        }

        private static float _newMisCd;
        private static float _timeToWait = 180;

        public static void Update()
        {
            if (!_initialized || !_diffButtonsInitialized)
                return;

            if (_curMenuMission.Blocks != null) {
                var missionComplete = GameHandler.NothingCanHappenAnymore(_curMenuMission, out bool victory);

                if (missionComplete) {
                    // TODO: finish.
                    _newMisCd += TankGame.DeltaTime;
                    if (_newMisCd > _timeToWait)
                        LoadTemplateMission();
                }
                else
                    _newMisCd = 0;
            }

            #region General Stuff
            if (MenuState == State.StatsMenu)
                if (InputUtils.KeyJustPressed(Keys.Escape))
                    MenuState = State.PrimaryMenu;
            if (_spinCd > 0)
                _spinCd--;
            else {
                _spinTarget += MathHelper.Tau;
                _spinCd = 420;
            }
            SetPlayButtonsVisibility(MenuState == State.PlayList);
            SetMPButtonsVisibility(MenuState == State.Mulitplayer);
            SetPrimaryMenuButtonsVisibility(MenuState == State.PrimaryMenu);
            SetDifficultiesButtonsVisibility(MenuState == State.Difficulties);

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
            Predictions.Color = Difficulties.Types["Predictions"] ? Color.Lime : Color.Red;
            RandomizedPlayer.Color = Difficulties.Types["RandomPlayer"] ? Color.Lime : Color.Red;
            BulletBlocking.Color = Difficulties.Types["BulletBlocking"] ? Color.Lime : Color.Red;
            FFA.Color = Difficulties.Types["FFA"] ? Color.Lime : Color.Red;
            LanternMode.Color = Difficulties.Types["LanternMode"] ? Color.Lime : Color.Red;

            if (_musicFading)
            {
                if (Theme.Volume > 0)
                    Theme.Volume -= 0.0075f;
            }
            else
                Theme.Volume = TankGame.Settings.MusicVolume * 0.1f;

            #endregion
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

            GameHandler.CleanupEntities();
            PlacementSquare.ResetSquares();
            GameHandler.CleanupScene();

            RemoveAllMenuTanks();

            GameUI.OptionsButtonSize.Y = 150;
            GameUI.QuitButtonSize.Y = 150;

            GameUI.QuitButtonPos.Y += 50;
            GameUI.OptionsButtonPos.Y -= 75;

            HideAll();
            // UIElement.ResizeAndRelocate();

            OnMenuClose?.Invoke();
        }

        private static Mission _curMenuMission;
        private static List<Mission> _cachedMissions = new();

        public static void Open()
        {
            _musicFading = false;
            _sclOffset = 0;
            MenuState = State.PrimaryMenu;
            Active = true;
            GameUI.Paused = false;
            Theme.Play();

            TankGame.DoZoomStuff();
            // GameHandler.CleanupEntities();

            foreach (var block in Block.AllBlocks)
                block?.Remove();
            foreach (var mine in Mine.AllMines)
                mine?.Remove();
            foreach (var shell in Shell.AllShells)
                shell?.Remove();
            foreach (var tank in GameHandler.AllTanks)
                tank?.Remove();

            PlayerTank.TankKills.Clear();

            if (GameHandler.ClearTracks is not null)
                GameHandler.ClearTracks.OnLeftClick?.Invoke(null);
            if (GameHandler.ClearChecks is not null)
                GameHandler.ClearChecks.OnLeftClick?.Invoke(null);

            TankGame.OverheadView = false;
            TankGame.CameraRotationVector.Y = TankGame.DEFAULT_ORTHOGRAPHIC_ANGLE;
            TankGame.AddativeZoom = 1f;
            TankGame.CameraFocusOffset.Y = 0f;

            GameUI.QuitButtonPos.Y -= 50;
            GameUI.OptionsButtonPos.Y += 75;

            LoadTemplateMission();

            TankMusicSystem.StopAll();

            SetPrimaryMenuButtonsVisibility(true);
            SetPlayButtonsVisibility(false);
            SetMPButtonsVisibility(false);

            GameUI.ResumeButton.IsVisible = false;
            GameUI.RestartButton.IsVisible = false;

            GameUI.QuitButtonSize.Y = 50;
            GameUI.OptionsButtonSize.Y = 50;
            GameUI.QuitButton.IsVisible = true;
            GameUI.OptionsButton.IsVisible = true;

            //UIElement.ResizeAndRelocate();

            OnMenuOpen?.Invoke();
        }

        private static bool _firstTime = true;

        private static void LoadTemplateMission(bool autoSetup = true, bool loadForMenu = true)
        {
            if (_firstTime)
            {
                var attempt = 1;

            tryAgain:
                var linkTry = $"https://github.com/RighteousRyan1/tanks_rebirth_motds/blob/master/menu_missions/Menu{attempt}.mission?raw=true";
                var exists = WebUtils.RemoteFileExists(linkTry);

                if (exists)
                {
                    var bytes1 = WebUtils.DownloadWebFile(linkTry, out var name1);

                    var reader1 = new BinaryReader(new MemoryStream(bytes1));

                    _cachedMissions.Add(Mission.Read(reader1));
                    attempt++;
                    goto tryAgain;
                }

                _firstTime = false;
            }

            GameHandler.CleanupScene();

            var rand = GameHandler.GameRand.Next(1, _cachedMissions.Count);

            var mission = _cachedMissions[rand];

            if (autoSetup)
            {
                GameProperties.LoadedCampaign.LoadMission(mission);
                GameProperties.LoadedCampaign.SetupLoadedMission(true);
            }
            if (loadForMenu)
                _curMenuMission = mission;
        }

        private static void HideAll()
        {
            PlayButton.IsVisible = false;
            PlayButton_SinglePlayer.IsVisible = false;
            PlayButton_Multiplayer.IsVisible = false;
            PlayButton_LevelEditor.IsVisible = false;

            GameUI.BackButton.IsVisible = false;
        }
        public static void RemoveAllMenuTanks()
        {
            for (int i = 0; i < tanks.Count; i++)
                tanks[i].Remove();
            tanks.Clear();
        }

        private static readonly string tanksMessage = $"Tanks! Rebirth ALPHA v{TankGame.Instance.GameVersion}\nOriginal game and Assets developed by Nintendo\nProgrammed by RighteousRyan\nArt and Graphics by BigKitty1011\nTANKS to all our contributors!";

        private static int _oldwheel;
        public static void Render()
        {
            if (!_initialized || !_diffButtonsInitialized)
                return;
            if (Active)
            {
                UpdateLogo();
                if (MenuState == State.Cosmetics)
                    RenderCrate();
                else if (MenuState == State.StatsMenu)
                    RenderStats(new Vector2(WindowUtils.WindowWidth * 0.3f, 200), new Vector2(WindowUtils.WindowWidth * 0.7f, 40), Anchor.TopCenter);
                else if (MenuState == State.Difficulties)
                    TankGame.SpriteRenderer.DrawString(TankGame.TextFont, "Ideas are welcome! Let us know in our DISCORD server!", new(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight / 6), Color.White, new Vector2(1f), 0f, GameUtils.GetAnchor(Anchor.Center, TankGame.TextFont.MeasureString("Ideas are welcome! Let us know in our DISCORD server!")));
                else if (MenuState == State.LoadingMods)
                {
                    var alpha = 0.7f;
                    var width = WindowUtils.WindowWidth / 3;
                    TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, new Vector2(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight / 2), null, Color.SkyBlue * alpha, 0f, GameUtils.GetAnchor(Anchor.Center, TankGame.WhitePixel.Size()), new Vector2(width, 200.ToResolutionY()), default, 0f);

                    var barDims = new Vector2(width - 120, 20).ToResolution();

                    TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, new Vector2(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight / 2), null, Color.Goldenrod * alpha, 0f, GameUtils.GetAnchor(Anchor.Center, TankGame.WhitePixel.Size()), 
                        barDims, default, 0f);
                    var ratio = (float)ModLoader.ActionsComplete / ModLoader.ActionsNeeded;
                    if (ModLoader.ActionsNeeded == 0)
                        ratio = 0;
                    TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, new Vector2(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight / 2), null, Color.Yellow * alpha, 0f, GameUtils.GetAnchor(Anchor.Center, TankGame.WhitePixel.Size()), 
                        barDims * new Vector2(ratio, 1f).ToResolution(), default, 0f);

                    var txt = $"{ModLoader.Status} {ModLoader.ModBeingLoaded}...";
                    TankGame.SpriteRenderer.DrawString(TankGame.TextFont, txt, new(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight / 2 - 75.ToResolutionY()), Color.White, Vector2.One.ToResolution(), 0f, GameUtils.GetAnchor(Anchor.Center, TankGame.TextFont.MeasureString(txt)));

                    txt = ModLoader.Error == string.Empty ? $"Loading your mods... {ratio * 100:0}% ({ModLoader.ActionsComplete} / {ModLoader.ActionsNeeded})" :
                    $"Error Loading '{ModLoader.ModBeingLoaded}' ({ModLoader.Error})";
                    TankGame.SpriteRenderer.DrawString(TankGame.TextFont, txt, new(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight / 2 - 150.ToResolutionY()), Color.White, Vector2.One.ToResolution(), 0f, GameUtils.GetAnchor(Anchor.Center, TankGame.TextFont.MeasureString(txt)));
                }
                #region Various things
                if ((NetPlay.CurrentServer is not null && (Server.ConnectedClients is not null || NetPlay.ServerName is not null)) || (Client.IsConnected() && Client.lobbyDataReceived))
                {
                    Vector2 initialPosition = new(WindowUtils.WindowWidth * 0.75f, WindowUtils.WindowHeight * 0.25f);
                    TankGame.SpriteRenderer.DrawString(TankGame.TextFont, $"\"{NetPlay.ServerName}\"", initialPosition - new Vector2(0, 40), Color.White, new Vector2(0.6f));
                    TankGame.SpriteRenderer.DrawString(TankGame.TextFont, $"Connected Players:", initialPosition, Color.White, new Vector2(0.6f));
                    for (int i = 0; i < Server.ConnectedClients.Count(x => x is not null); i++)
                    {
                        var client = Server.ConnectedClients[i];

                        Color textCol = Color.White;
                        if (NetPlay.CurrentClient.Id == i)
                            textCol = Color.Green;

                        TankGame.SpriteRenderer.DrawString(TankGame.TextFont, $"{client.Name}" + $" ({client.Id})", initialPosition + new Vector2(0, 20) * (i + 1), textCol, new Vector2(0.6f));
                    }
                }
                var tanksMessageSize = TankGame.TextFont.MeasureString(tanksMessage);


                TankGame.SpriteRenderer.DrawString(TankGame.TextFont, tanksMessage, new(8, WindowUtils.WindowHeight - 8), Color.White, new(0.6f), 0f, new Vector2(0, tanksMessageSize.Y));

                TankGame.SpriteRenderer.DrawString(TankGame.TextFont, tanksMessage, new(8, WindowUtils.WindowHeight - 8), Color.White, new(0.6f), 0f, new Vector2(0, tanksMessageSize.Y));
                //if (PlayButton.IsVisible)
                //TankGame.SpriteRenderer.DrawString(TankGame.TextFont, keyDisplay, new(12, 12), Color.White, new(0.6f), 0f, Vector2.Zero);
                #endregion

                if (MenuState == State.PrimaryMenu || MenuState == State.PlayList)
                {
                    // draw the logo at it's position
                    TankGame.SpriteRenderer.Draw(LogoTexture, LogoPosition, null, Color.White, LogoRotation, LogoTexture.Size() / 2, LogoScale, default, default);

                    var size = TankGame.TextFont.MeasureString(TankGame.Instance.MOTD);
                    TankGame.SpriteRenderer.DrawString(TankGame.TextFont, TankGame.Instance.MOTD, LogoPosition + LogoTexture.Size() * LogoScale * 0.3f, Color.White, LogoScale * 1.5f, LogoRotation - 0.25f, GameUtils.GetAnchor(Anchor.TopCenter, size));
                }

                if (!campaignNames.Any(x =>
                {
                    if (x is UITextButton btn)
                        return btn.Text == "Vanilla"; // i fucking hate this hardcode. but i'll cry about it later.
                    return false;
                }) && MenuState == State.Campaigns)
                {
                    TankGame.SpriteRenderer.DrawString(TankGame.TextFont, $"You are missing the vanilla campaign!" +
                        $"\nTry downloading the Vanilla campaign by pressing 'Enter'." +
                        $"\nCampaign files belong in '{Path.Combine(TankGame.SaveDirectory, "Campaigns").Replace(Environment.UserName, "%UserName%")}' (press TAB to open on Windows)", new(12, 12), Color.White, new(0.75f), 0f, Vector2.Zero);

                    if (InputUtils.KeyJustPressed(Keys.Tab))
                    {
                        if (Directory.Exists(Path.Combine(TankGame.SaveDirectory, "Campaigns")))
                            Process.Start("explorer.exe", Path.Combine(TankGame.SaveDirectory, "Campaigns"));
                        // do note that this fails on windows lol
                    }
                    if (InputUtils.KeyJustPressed(Keys.Enter))
                    {
                        try {
                            /*var bytes = WebUtils.DownloadWebFile("https://github.com/RighteousRyan1/tanks_rebirth_motds/blob/master/Vanilla.rar?raw=true", out var filename);
                            var path = Path.Combine(TankGame.SaveDirectory, "Campaigns", filename);
                            File.WriteAllBytes(path, bytes);

                            using (var archive = new RarArchive(path))
                                archive.ExtractToDirectory(Path.Combine(TankGame.SaveDirectory, "Campaigns", ""));

                            File.Delete(path);*/
                            // ^ before campaign files.

                            var bytes = WebUtils.DownloadWebFile("https://github.com/RighteousRyan1/tanks_rebirth_motds/blob/master/Vanilla.campaign?raw=true", out var filename);
                            var path = Path.Combine(TankGame.SaveDirectory, "Campaigns", filename);
                            File.WriteAllBytes(path, bytes);


                            SetCampaignDisplay();

                        } catch(Exception e) {
                            Process.Start(new ProcessStartInfo("https://github.com/RighteousRyan1/TanksRebirth/releases/download/1.3.4-alpha/Vanilla.rar")
                            {
                                UseShellExecute = true,
                            });
                            GameHandler.ClientLog.Write($"Error: {e.Message}\n{e.StackTrace}", LogType.Error);
                        }
                    }
                }
                if (MenuState == State.Campaigns)
                {

                    if (_oldwheel != InputUtils.DeltaScrollWheel)
                        MissionCheckpoint += InputUtils.DeltaScrollWheel - _oldwheel;
                    if (MissionCheckpoint < 0)
                        MissionCheckpoint = 0;
                    
                    TankGame.SpriteRenderer.DrawString(TankGame.TextFont, $"You can scroll with your mouse to skip to a certain mission." +
                        $"\nCurrently, you will skip to mission {MissionCheckpoint + 1}." +
                        $"\nYou will be alerted if that mission does not exist.", new(12, 200), Color.White, new(0.75f), 0f, Vector2.Zero);
                }
                else if (MenuState == State.Cosmetics) {
                    TankGame.SpriteRenderer.DrawString(TankGame.TextFontLarge, $"COMING SOON!", new(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight / 6), Color.White, new Vector2(0.75f) * (1920 / 1080 / TankGame.Instance.GraphicsDevice.Viewport.AspectRatio), 0f, TankGame.TextFontLarge.MeasureString($"COMING SOON!") / 2);
                }
            }
            _oldwheel = InputUtils.DeltaScrollWheel;
        }
    }
}
