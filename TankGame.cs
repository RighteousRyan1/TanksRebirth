using System;
using System.IO;
using System.Text.Json;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.GameContent;
using TanksRebirth.Internals;
using TanksRebirth.Internals.UI;
using TanksRebirth.Internals.Core.Interfaces;
using System.Collections.Generic;
using System.Linq;
using TanksRebirth.Internals.Common.IO;
using System.Diagnostics;
using TanksRebirth.GameContent.UI;
using TanksRebirth.Graphics;
using System.Management;
using TanksRebirth.Internals.Common.Framework.Input;
using TanksRebirth.Internals.Core;
using TanksRebirth.Localization;
using FontStashSharp;
using TanksRebirth.Internals.Common.Framework.Graphics;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Net;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Audio;
using TanksRebirth.IO;
using TanksRebirth.Enums;
using TanksRebirth.Achievements;
using TanksRebirth.GameContent.Speedrunning;
using TanksRebirth.GameContent.Properties;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.GameContent.ModSupport;
using System.Threading.Tasks;

namespace TanksRebirth
{
    // TODO: Implement block once all of above things are done
    // TODO: AI in the middle to far future
    // TODO: add some finishing touches to TankMusicSystem

    public class TankGame : Game
    {
        private static string GetGPU()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "Unavailable: Only supported on Windows";
            using var searcher = new ManagementObjectSearcher("select * from Win32_VideoController");

            foreach (ManagementObject obj in searcher.Get())
            {
                return $"{obj["Name"]} - {obj["DriverVersion"]}";
            }
            return "Data not retrieved.";
        }

        public static string GetHardware(string hwclass, string syntax)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "Unavailable: Only supported on Windows";
            using var searcher = new ManagementObjectSearcher($"SELECT * FROM {hwclass}");

            foreach (var obj in searcher.Get())
            {
                return $"{obj[syntax]}";
            }
            return "Data not retrieved.";
        }

        public static Language GameLanguage = new();

        public static class MemoryParser
        {
            public static ulong FromBits(long bytes)
            {
                return (ulong)bytes * 8;
            }
            public static long FromKilobytes(long bytes)
            {
                return bytes / 1000;
            }
            public static long FromMegabytes(long bytes)
            {
                return bytes / 1000 / 1000;
            }
            public static long FromGigabytes(long bytes)
            {
                return bytes / 1000 / 1000 / 1000;
            }
            public static long FromTerabytes(long bytes)
            {
                return bytes / 1000 / 1000 / 1000 / 1000;
            }
        }

        internal static float GetInterpolatedFloat(float value)
            => value * (float)LastGameTime.ElapsedGameTime.TotalSeconds;

        public const int LevelEditorVersion = 2;

        public static readonly byte[] LevelFileHeader = { 84, 65, 78, 75 };

        public static Camera GameCamera;

        public static readonly string SysGPU = $"GPU: {GetGPU()}";
        public static readonly string SysCPU = $"CPU: {GetHardware("Win32_Processor", "Name")}";
        public static readonly string SysKeybd = $"Keyboard: {GetHardware("Win32_Keyboard", "Name")}";
        public static readonly string SysMouse = $"Mouse: {GetHardware("Win32_PointingDevice", "Name")}";

        public static TimeSpan RenderTime { get; private set; }
        public static TimeSpan LogicTime { get; private set; }

        public static double LogicFPS { get; private set; }
        public static double RenderFPS { get; private set; }

        public static long GCMemory => GC.GetTotalMemory(false);
        public static long ProcessMemory
        {
            get
            {
                using Process process = Process.GetCurrentProcess(); 
                return process.PrivateMemorySize64;
            }
            private set { }
        }

        public static GameTime LastGameTime { get; private set; }
        public static uint GameUpdateTime { get; private set; }

        public static Texture2D WhitePixel;

        public static TankGame Instance { get; private set; }
        public static readonly string ExePath = Assembly.GetExecutingAssembly().Location.Replace(@$"\WiiPlayTanksRemake.dll", string.Empty);
        public static SpriteBatch SpriteRenderer;

        public readonly GraphicsDeviceManager Graphics;

        // private static List<IGameSystem> systems = new();

        public static GameConfig Settings;

        public JsonHandler<GameConfig> SettingsHandler;

        public static readonly string SaveDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Tanks Rebirth");
        public static GameData GameData { get; private set; } = new();

        public static Matrix GameView;
        public static Matrix GameProjection;

        private FontSystem _fontSystem;

        public static SpriteFontBase TextFont;
        public static SpriteFontBase TextFontLarge;

        public static event EventHandler<IntPtr> OnFocusLost;
        public static event EventHandler<IntPtr> OnFocusRegained;

        private bool _wasActive;

        public readonly string GameVersion;

        public static OSPlatform OperatingSystem;
        public static bool IsWindows;
        public static bool IsMac;
        public static bool IsLinux;

        public readonly string MOTD;

        public TankGame() : base()
        {
            Directory.CreateDirectory(SaveDirectory);
            Directory.CreateDirectory(Path.Combine(SaveDirectory, "Texture Packs", "Scene"));
            Directory.CreateDirectory(Path.Combine(SaveDirectory, "Texture Packs", "Tank"));
            Directory.CreateDirectory(Path.Combine(SaveDirectory, "Logs"));
            GameHandler.ClientLog = new(Path.Combine(SaveDirectory, "Logs"), "client");
            try {
                try {
                    var bytes = WebUtils.DownloadWebFile("https://raw.githubusercontent.com/RighteousRyan1/tanks_rebirth_motds/master/motd.txt", out var name);
                    MOTD = System.Text.Encoding.Default.GetString(bytes);
                } catch {
                    // in the case that an HTTPRequestException is thrown (no internet access)
                    MOTD = LocalizationRandoms.GetRandomMotd();
                }
                // check if platform is windows, mac, or linux
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    OperatingSystem = OSPlatform.Windows;
                    IsWindows = true;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)){
                    OperatingSystem = OSPlatform.OSX;
                    IsMac = true;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                    OperatingSystem = OSPlatform.Linux;
                    IsLinux = true;
                }

                GameHandler.ClientLog.Write($"Playing on Operating System '{OperatingSystem}'", LogType.Info);

                // IOUtils.SetAssociation(".mission", "MISSION_FILE", "TanksRebirth.exe", "Tanks Rebirth mission file");

                Graphics = new(this) { PreferHalfPixelOffset = true };
                Graphics.HardwareModeSwitch = false;

                Content.RootDirectory = "Content";
                Instance = this;
                Window.Title = "Tanks! Rebirth";
                Window.AllowUserResizing = true;

                IsMouseVisible = false;

                Graphics.IsFullScreen = false;

                _fontSystem = new();

                GameVersion = typeof(TankGame).Assembly.GetName().Version.ToString();

                GameHandler.ClientLog.Write($"Running {typeof(TankGame).Assembly.GetName().Name} on version {GameVersion}'", LogType.Info);
            }
            catch (Exception e) when (!Debugger.IsAttached)
            {
                GameHandler.ClientLog.Write($"Error: {e.Message}\n{e.StackTrace}", LogType.Error);
                throw;
            }
        }

        private long _memBytes;

        public static Stopwatch CurrentSessionTimer = new();

        protected override void Initialize()
        {
            try {
                CurrentSessionTimer.Start();

                GameHandler.MapEvents();
                GameHandler.ClientLog.Write($"Mapped events...", LogType.Info);

                DiscordRichPresence.Load();
                GameHandler.ClientLog.Write($"Loaded Discord Rich Presence...", LogType.Info);

                // systems = ReflectionUtils.GetInheritedTypesOf<IGameSystem>(Assembly.GetExecutingAssembly());

                ResolutionHandler.Initialize(Graphics);

                GameCamera = new Camera(GraphicsDevice);

                SpriteRenderer = new(GraphicsDevice);

                Graphics.PreferMultiSampling = true;

                Graphics.ApplyChanges();

                GameHandler.ClientLog.Write($"Applying changes to graphics device... ({Graphics.PreferredBackBufferWidth}x{Graphics.PreferredBackBufferHeight})", LogType.Info);

                GameData.Setup();
                if (File.Exists(Path.Combine(GameData.Directory, GameData.Name)))
                    GameData.Deserialize();

                GameHandler.ClientLog.Write($"Loaded save data.", LogType.Info);

                VanillaAchievements.InitializeToRepository();

                base.Initialize();
            }
            catch (Exception e) when (!Debugger.IsAttached) {
                GameHandler.ClientLog.Write($"Error: {e.Message}\n{e.StackTrace}", LogType.Error);
                throw;
            }
        }

        public static void Quit()
            => Instance.Exit();

        protected override void OnExiting(object sender, EventArgs args)
        {
            GameHandler.ClientLog.Write($"Handling termination process...", LogType.Info);
            GameData.TimePlayed += CurrentSessionTimer.Elapsed;
            CurrentSessionTimer.Stop();
            GameHandler.ClientLog.Dispose();
            SettingsHandler = new(Settings, Path.Combine(SaveDirectory, "settings.json"));
            JsonSerializerOptions opts = new() { WriteIndented = true };
            SettingsHandler.Serialize(opts, true);
            GameData.Serialize();

            DiscordRichPresence.Terminate();
        }
        protected override void LoadContent()
        {
            try
            {
                var s = Stopwatch.StartNew();

                if (Debugger.IsAttached)
                {
                    GameResources.CopySrcFolderContents("Content/Assets/fonts");
                    GameResources.CopySrcFolderContents("Content/Assets/music");
                    GameResources.CopySrcFolderContents("Content/Assets/music/marble");
                    GameResources.CopySrcFolderContents("Content/Assets/sounds");
                    GameResources.CopySrcFolderContents("Content/Assets/sounds/ambient");
                    GameResources.CopySrcFolderContents("Content/Assets/sounds/crate");
                    GameResources.CopySrcFolderContents("Content/Assets/sounds/menu");
                    GameResources.CopySrcFolderContents("Content/Assets/sounds/thunder");
                    GameResources.CopySrcFolderContents("Content/Assets/sounds/tnk_event");
                    GameResources.CopySrcFolderContents("Content/Assets/sounds/results");
                    GameResources.CopySrcFolderContents("Content/Assets/fanfares");
                    GameResources.CopySrcFolderContents("Content/Assets/mainmenu");
                    GameResources.CopySrcFolderContents("Localization");

                    GameResources.CopySrcFolderContents("Content/Assets/textures/bullet");
                    GameResources.CopySrcFolderContents("Content/Assets/textures/check");
                    GameResources.CopySrcFolderContents("Content/Assets/textures/chest");
                    GameResources.CopySrcFolderContents("Content/Assets/textures/ingame");
                    GameResources.CopySrcFolderContents("Content/Assets/textures/medal");
                    GameResources.CopySrcFolderContents("Content/Assets/textures/mine");
                    GameResources.CopySrcFolderContents("Content/Assets/textures/misc");
                    GameResources.CopySrcFolderContents("Content/Assets/textures/tank");
                    GameResources.CopySrcFolderContents("Content/Assets/textures/tank/wee");
                    GameResources.CopySrcFolderContents("Content/Assets/textures/ui");
                    GameResources.CopySrcFolderContents("Content/Assets/textures/ui/leveledit");

                    GameResources.CopySrcFolderContents("Content/Assets/textures", ".png");
                    GameResources.CopySrcFolderContents("Content/Assets", ".png");
                    GameResources.CopySrcFolderContents("Content/Assets/toy", ".png");
                    GameResources.CopySrcFolderContents("Content/Assets/forest", ".png");
                    GameResources.CopySrcFolderContents("Content/Assets/cosmetics", ".png");

                    GameHandler.ClientLog.Write($"Detected build: Copying source folders to output...", LogType.Info);
                }

                _cachedState = GraphicsDevice.RasterizerState;

                UIElement.UIPanelBackground = GameResources.GetGameResource<Texture2D>("Assets/UIPanelBackground");

                Thunder.SoftRain = new OggAudio("Content/Assets/sounds/ambient/soft_rain");
                Thunder.SoftRain.Instance.Volume = 0;
                Thunder.SoftRain.Instance.IsLooped = true;

                OnFocusLost += TankGame_OnFocusLost;
                OnFocusRegained += TankGame_OnFocusRegained;

                WhitePixel = GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel");

                _fontSystem.AddFont(File.ReadAllBytes(@"Content/Assets/fonts/en_US.ttf"));
                _fontSystem.AddFont(File.ReadAllBytes(@"Content/Assets/fonts/ja_JP.ttf"));
                _fontSystem.AddFont(File.ReadAllBytes(@"Content/Assets/fonts/es_ES.ttf"));
                _fontSystem.AddFont(File.ReadAllBytes(@"Content/Assets/fonts/ru_RU.ttf"));

                GameHandler.ClientLog.Write($"Loaded fonts.", LogType.Info);

                TextFont = _fontSystem.GetFont(30);
                TextFontLarge = _fontSystem.GetFont(120);

                if (!File.Exists(Path.Combine(SaveDirectory, "settings.json")))
                {
                    Settings = new();
                    SettingsHandler = new(Settings, Path.Combine(SaveDirectory, "settings.json"));
                    JsonSerializerOptions opts = new()
                    {
                        WriteIndented = true
                    };
                    SettingsHandler.Serialize(opts, true);
                }
                else
                {
                    SettingsHandler = new(Settings, Path.Combine(SaveDirectory, "settings.json"));
                    Settings = SettingsHandler.Deserialize();
                }

                GameHandler.ClientLog.Write($"Loaded user settings.", LogType.Info);

                #region Config Initialization

                Graphics.SynchronizeWithVerticalRetrace = Settings.Vsync;
                Graphics.IsFullScreen = Settings.FullScreen;
                PlayerTank.controlUp.AssignedKey = Settings.UpKeybind;
                PlayerTank.controlDown.AssignedKey = Settings.DownKeybind;
                PlayerTank.controlLeft.AssignedKey = Settings.LeftKeybind;
                PlayerTank.controlRight.AssignedKey = Settings.RightKeybind;
                PlayerTank.controlMine.AssignedKey = Settings.MineKeybind;
                MapRenderer.Theme = Settings.GameTheme;
                TankFootprint.ShouldTracksFade = Settings.FadeFootprints;

                Graphics.PreferredBackBufferWidth = Settings.ResWidth;
                Graphics.PreferredBackBufferHeight = Settings.ResHeight;

                GameHandler.ClientLog.Write($"Applied user settings.", LogType.Info);

                Tank.SetAssetNames();
                MapRenderer.LoadTexturePack(Settings.MapPack);
                Tank.LoadTexturePack(Settings.TankPack);
                Graphics.ApplyChanges();

                Language.LoadLang(ref GameLanguage, Settings.Language);

                #endregion

                ModLoader.LoadMods();

                MainMenu.InitializeBasics();

                GameUI.Initialize();
                MainMenu.InitializeUIGraphics();

                DecalSystem.Initialize(SpriteRenderer, GraphicsDevice);

                LevelEditor.Initialize();

                if (ModLoader.LoadingMods) {
                    MainMenu.MenuState = MainMenu.State.LoadingMods;
                    Task.Run(async () => {
                        while (ModLoader.LoadingMods)
                            await Task.Delay(50).ConfigureAwait(false);
                        MainMenu.MenuState = MainMenu.State.PrimaryMenu;
                    });
                }

                GameHandler.SetupGraphics();

                GameHandler.ClientLog.Write("Running in directory: " + Directory.GetCurrentDirectory(), LogType.Info);

                GameHandler.ClientLog.Write($"Content loaded in {s.Elapsed}.", LogType.Debug);
                GameHandler.ClientLog.Write($"DebugMode: {Debugger.IsAttached}", LogType.Debug);

                s.Stop();
            }
            catch (Exception e) when (!Debugger.IsAttached)
            {
                GameHandler.ClientLog.Write($"Error: {e.Message}\n{e.StackTrace}", LogType.Error);
                throw;
            }
        }

        private void TankGame_OnFocusRegained(object sender, IntPtr e)
        {
            if (TankMusicSystem.IsLoaded)
            {
                if (Thunder.SoftRain.IsPaused())
                    Thunder.SoftRain.Instance.Resume();
                TankMusicSystem.ResumeAll();
                if (MainMenu.Active)
                    MainMenu.Theme.Resume();
                if (LevelEditor.Active)
                    LevelEditor.Theme.Resume();
            }
        }

        private void TankGame_OnFocusLost(object sender, IntPtr e)
        {
            if (TankMusicSystem.IsLoaded)
            {
                if (Thunder.SoftRain.IsPlaying())
                    Thunder.SoftRain.Instance.Pause();
                TankMusicSystem.PauseAll();
                if (MainMenu.Active)
                    MainMenu.Theme.Pause();
                if (LevelEditor.Active)
                    LevelEditor.Theme.Pause();
            }
        }

        public const float DEFAULT_ORTHOGRAPHIC_ANGLE = 0.75f;
        public static Vector2 CameraRotationVector = new(0, DEFAULT_ORTHOGRAPHIC_ANGLE);

        public const float DEFAULT_ZOOM = 3.3f;
        public static float AddativeZoom = 1f;

        public static Vector2 CameraFocusOffset;
        public static bool ThirdPerson;

        private static bool _oView;
        public static bool OverheadView
        {
            get => _oView; 
            set
            {
                transitionTimer = 100;
                _oView = value;
            }
        }

        private static int transitionTimer;

        private bool _justChanged = true;

        public static Vector3 ThirdPersonCameraPosition;
        public static Vector2 MouseVelocity => GameUtils.GetMouseVelocity(GameUtils.WindowCenter);

        public static bool SecretCosmeticSetting;
        public static bool SpeedrunMode;

        public static bool Interp = true;
        public static float DeltaTime { get; private set; }

        public static bool HoveringAnyTank;

        protected override void Update(GameTime gameTime)
        {
            try
            {
                DeltaTime = Interp ? (float)gameTime.ElapsedGameTime.TotalSeconds : 1; // if interpolation of frames is false, set delta time to 1
                if (Input.AreKeysJustPressed(Keys.LeftAlt, Keys.RightAlt))
                    Lighting.AccurateShadows = !Lighting.AccurateShadows;
                if (Input.AreKeysJustPressed(Keys.LeftShift, Keys.RightShift))
                    RenderWireframe = !RenderWireframe;

                if (DebugUtils.DebuggingEnabled && Input.AreKeysJustPressed(Keys.V, Keys.B))
                    ModLoader.LoadMods();

                if (Input.AreKeysJustPressed(Keys.Left, Keys.Right, Keys.Up, Keys.Down))
                {
                    SecretCosmeticSetting = !SecretCosmeticSetting;
                    ChatSystem.SendMessage(SecretCosmeticSetting ? "Activated randomized cosmetics!" : "Deactivated randomized cosmetics.", SecretCosmeticSetting ? Color.Lime : Color.Red);
                }
                if (Input.KeyJustPressed(Keys.F1))
                {
                    SpeedrunMode = !SpeedrunMode;
                    if (SpeedrunMode)
                        GameProperties.OnMissionStart += GameHandler.StartSpeedrun;
                    else
                        GameProperties.OnMissionStart -= GameHandler.StartSpeedrun;
                    ChatSystem.SendMessage(SpeedrunMode ? "Speedrun mode on!" : "Speedrun mode off.", SpeedrunMode ? Color.Lime : Color.Red);
                }
                if (Input.AreKeysJustPressed(Keys.LeftAlt | Keys.RightAlt, Keys.Enter))
                {
                    Graphics.IsFullScreen = !Graphics.IsFullScreen;
                    Graphics.ApplyChanges();
                }

                MouseRenderer.ShouldRender = ThirdPerson ? (GameUI.Paused || MainMenu.Active) : true;
                if (UIElement.delay > 0)
                    UIElement.delay--;

                if (NetPlay.CurrentClient is not null)
                    Client.clientNetManager.PollEvents();
                if (NetPlay.CurrentServer is not null)
                    Server.serverNetManager.PollEvents();

                UIElement.UpdateElements();
                GameUI.UpdateButtons();

                if (GameUpdateTime % 60 == 0 && DebugUtils.DebuggingEnabled) {
                    DiscordRichPresence.Update();
                    _memBytes = ProcessMemory;
                }

                if (IntermissionSystem.Alpha >= 1)
                    _justChanged = true;

                LastGameTime = gameTime;

                if (_wasActive && !IsActive)
                    OnFocusLost?.Invoke(this, Window.Handle);
                if (!_wasActive && IsActive)
                    OnFocusRegained?.Invoke(this, Window.Handle);
                if (!MainMenu.Active && DebugUtils.DebuggingEnabled)
                    if (Input.KeyJustPressed(Keys.J))
                        OverheadView = !OverheadView;

                if (!ThirdPerson)
                {
                    if (transitionTimer > 0)
                    {
                        transitionTimer--;
                        if (OverheadView)
                        {
                            GameUtils.SoftStep(ref CameraRotationVector.Y, MathHelper.PiOver2, 0.08f);
                            GameUtils.SoftStep(ref AddativeZoom, 0.6f, 0.08f);
                            GameUtils.RoughStep(ref CameraFocusOffset.Y, 82f, 2f);
                        }
                        else
                        {
                            GameUtils.SoftStep(ref CameraRotationVector.Y, DEFAULT_ORTHOGRAPHIC_ANGLE, 0.08f);
                            GameUtils.SoftStep(ref AddativeZoom, 1f, 0.08f);
                            GameUtils.RoughStep(ref CameraFocusOffset.Y, 0f, 2f);
                        }
                    }

                    /*GameCamera.SetPosition(new Vector3(0, 0, 350));
                    GameCamera.SetLookAt(new Vector3(0, 0, 0));
                    GameCamera.Zoom(DEFAULT_ZOOM * AddativeZoom);

                    GameCamera.RotateX(CameraRotationVector.Y);
                    GameCamera.RotateY(CameraRotationVector.X);

                    GameCamera.SetCameraType(CameraType.Orthographic);

                    GameCamera.Translate(new Vector3(CameraFocusOffset.X, -CameraFocusOffset.Y + 40, 0));

                    GameCamera.SetViewingDistances(-2000f, 5000f);*/

                    GameView =
                            Matrix.CreateScale(DEFAULT_ZOOM * AddativeZoom) *
                            Matrix.CreateLookAt(new(0f, 0, 350f), Vector3.Zero, Vector3.Up) * // 0, 0, 350
                            Matrix.CreateTranslation(CameraFocusOffset.X, -CameraFocusOffset.Y + 40, 0) *
                            Matrix.CreateRotationX(CameraRotationVector.Y) *
                            Matrix.CreateRotationY(CameraRotationVector.X);
                    //Matrix.CreateTranslation(CameraFocusOffset.X, -CameraFocusOffset.Y, 0);

                    if (_justChanged)
                    {
                        //if we just changed to third person, we don't want to reset the camera
                        GameProjection = Matrix.CreateOrthographic(GameUtils.WindowWidth, GameUtils.WindowHeight, -2000, 5000);

                        _justChanged = false;
                    }
                }
                else
                {
                    if (GameHandler.AllPlayerTanks.Count(x => x is not null && !x.Dead) > 0)
                    {
                        ThirdPersonCameraPosition = GameHandler.AllPlayerTanks[0].Position.ExpandZ();
                        /*GameCamera.SetPosition(GameHandler.AllPlayerTanks[0].Position.ExpandZ());
                        GameCamera.SetLookAt(GameHandler.AllPlayerTanks[0].Position.ExpandZ());
                        GameCamera.Zoom(DEFAULT_ZOOM * AddativeZoom);

                        GameCamera.RotateX(CameraRotationVector.Y);
                        GameCamera.RotateY(CameraRotationVector.X);

                        GameCamera.SetCameraType(CameraType.Orthographic);

                        GameCamera.Translate(new Vector3(CameraFocusOffset.X, -CameraFocusOffset.Y + 40, 0));

                        GameCamera.SetViewingDistances(-2000f, 5000f);*/

                        GameView = Matrix.CreateLookAt(ThirdPersonCameraPosition,
                            ThirdPersonCameraPosition + new Vector3(0, 0, 20).FlattenZ().RotatedByRadians(-GameHandler.AllPlayerTanks[0].TurretRotation).ExpandZ()
                            , Vector3.Up) * Matrix.CreateScale(AddativeZoom) * Matrix.CreateRotationX(CameraRotationVector.Y - MathHelper.PiOver4) * Matrix.CreateRotationY(CameraRotationVector.X) * Matrix.CreateTranslation(0, -20, -40);
                        if (_justChanged)
                        {
                            GameProjection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(90), GraphicsDevice.Viewport.AspectRatio, 0.1f, 1000);
                            _justChanged = false;
                        }
                    }
                    else
                    {
                        var x = GameHandler.AllAITanks.FirstOrDefault(x => x is not null && !x.Dead);

                        if (x is not null && Array.IndexOf(GameHandler.AllAITanks, x) > -1)
                        {
                            /*pos = x.Position3D;
                            var t = GameUtils.MousePosition.X / GameUtils.WindowWidth;
                            // GameCamera.Zoom(DEFAULT_ZOOM * AddativeZoom);
                            GameCamera.SetPosition(pos - new Vector3(0, 0, 100).FlattenZ().RotatedByRadians(-x.TurretRotation).ExpandZ());

                            GameCamera.SetLookAt(pos + new Vector3(0, 0, 20).FlattenZ().RotatedByRadians(-x.TurretRotation).ExpandZ());                        
                            GameCamera.Zoom(GameUtils.MousePosition.X / GameUtils.WindowWidth * 5);
                            GameCamera.SetFov(90);
                            //GameCamera.SetPosition(pos);
                            //GameCamera.RotateY(GameUtils.MousePosition.X / 400);
                            //GameCamera.RotateY(DEFAULT_ORTHOGRAPHIC_ANGLE);
                            //GameCamera.RotateX(GameUtils.MousePosition.X / 400);

                            GameCamera.RotateX(CameraRotationVector.Y - MathHelper.PiOver4);
                            GameCamera.RotateY(CameraRotationVector.X);

                            GameCamera.Translate(new Vector3(0, -20, -40));

                            GameCamera.SetViewingDistances(0.1f, 10000f);

                            GameCamera.SetCameraType(CameraType.FieldOfView);*/

                            ThirdPersonCameraPosition = x.Position.ExpandZ();

                            GameView = Matrix.CreateLookAt(ThirdPersonCameraPosition,
                                ThirdPersonCameraPosition + new Vector3(0, 0, 20).FlattenZ().RotatedByRadians(-x.TurretRotation).ExpandZ()
                                , Vector3.Up) * Matrix.CreateScale(AddativeZoom) * Matrix.CreateRotationX(CameraRotationVector.Y - MathHelper.PiOver4) * Matrix.CreateRotationY(CameraRotationVector.X) * Matrix.CreateTranslation(0, -20, -40) * Matrix.CreateScale(AddativeZoom);
                            if (_justChanged)
                            {
                                GameProjection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(90), GraphicsDevice.Viewport.AspectRatio, 0.1f, 1000);
                                _justChanged = false;
                            }
                        }
                    }
                }

                if (!GameUI.Paused && !MainMenu.Active && DebugUtils.DebuggingEnabled)
                {
                    if (Input.MouseRight)
                        CameraRotationVector += MouseVelocity / 500;

                    if (Input.CurrentKeySnapshot.IsKeyDown(Keys.Add))
                        AddativeZoom += 0.01f;
                    if (Input.CurrentKeySnapshot.IsKeyDown(Keys.Subtract))
                        AddativeZoom -= 0.01f;

                    ThirdPerson = Difficulties.Types["ThirdPerson"];

                    if (Input.MouseMiddle)
                        CameraFocusOffset += MouseVelocity;
                    GameUtils.GetMouseVelocity(GameUtils.WindowCenter);

                    if (!SpeedrunMode)
                        IsFixedTimeStep = !Input.CurrentKeySnapshot.IsKeyDown(Keys.Tab);
                    else
                        IsFixedTimeStep = true;
                }
                else
                    IsFixedTimeStep = true;

                FixedUpdate(gameTime);

                //GameView = GameCamera.GetView();
                //GameProjection = GameCamera.GetProjection();

                LogicTime = gameTime.ElapsedGameTime;

                LogicFPS = Math.Round(1f / gameTime.ElapsedGameTime.TotalSeconds);

                _wasActive = IsActive;
            }
            catch (Exception e) when (!Debugger.IsAttached)
            {
                GameHandler.ClientLog.Write($"Error: {e.Message}\n{e.StackTrace}", LogType.Error);
                throw;
            }
        }

        public void FixedUpdate(GameTime gameTime)
        {
            /* TODO: this
            if (Input.KeyJustPressed(Keys.Up))
            {
                if (GameHandler.LoadedCampaign != null)
                {
                    using var writer = new BinaryWriter(File.Open(Path.Combine(SaveDirectory, "debug_campaign.camp"), FileMode.OpenOrCreate));

                    // eventual .campaign format
                    for (int i = 0; i < GameHandler.LoadedCampaign.CachedMissions.Length; i++)
                    {
                        var mission = GameHandler.LoadedCampaign.CachedMissions[i];
                    }
                }
            }*/

            GameUpdateTime++;

            GameShaders.UpdateShaders();

            Input.PollEvents();

            bool shouldUpdate = Client.IsConnected() || IsActive && !GameUI.Paused;

            if (shouldUpdate)
            {
                /*foreach (var type in systems)
                    type?.Update();*/

                GameHandler.UpdateAll();

                Tank.CollisionsWorld.Step(1);

                HoveringAnyTank = false;
                if (!MainMenu.Active && OverheadView)
                {
                    foreach (var tnk in GameHandler.AllTanks)
                    {
                        if (tnk is not null && !tnk.Dead)
                        {
                            if (GameUtils.GetMouseToWorldRay().Intersects(tnk.Worldbox).HasValue)
                            {
                                HoveringAnyTank = true;
                                if (Input.KeyJustPressed(Keys.K))
                                {
                                    // var tnk = WPTR.AllAITanks.FirstOrDefault(tank => tank is not null && !tank.Dead && tank.tier == AITank.GetHighestTierActive());

                                    if (Array.IndexOf(GameHandler.AllTanks, tnk) > -1)
                                        tnk?.Destroy(new TankHurtContext_Other()); // hmmm
                                }

                                if (Input.CanDetectClick(rightClick: true))
                                {
                                    tnk.TankRotation -= MathHelper.PiOver2;
                                    tnk.TurretRotation -= MathHelper.PiOver2;
                                    if (tnk is AITank ai)
                                    {
                                        ai.TargetTurretRotation += MathHelper.PiOver2;
                                        ai.TargetTankRotation += MathHelper.PiOver2;

                                        if (ai.TargetTurretRotation >= MathHelper.Tau)
                                            ai.TargetTurretRotation -= MathHelper.Tau;
                                        if (ai.TargetTankRotation >= MathHelper.Tau)
                                            ai.TargetTankRotation -= MathHelper.Tau;
                                    }
                                    if (tnk.TankRotation >= MathHelper.Tau)
                                        tnk.TankRotation -= MathHelper.Tau;

                                    if (tnk.TurretRotation >= MathHelper.Tau)
                                        tnk.TurretRotation -= MathHelper.Tau;
                                }

                                tnk.IsHoveredByMouse = true;
                            }
                            else
                                tnk.IsHoveredByMouse = false;
                        }
                    }
                }
            }

            foreach (var bind in Keybind.AllKeybinds)
                bind?.Update();

            UiScale = GameUtils.MousePosition.X / GameUtils.WindowWidth * 5;
        }

        public static Matrix Matrix2D;

        public static float UiScale = 1f;

        public static Color ClearColor = Color.Black;

        public static bool RenderWireframe = false;

        public static RasterizerState _cachedState;

        public static RasterizerState DefaultRasterizer => RenderWireframe ? new() { FillMode = FillMode.WireFrame } : RasterizerState.CullNone;

        static RenderTarget2D gameTarget;
        public static RenderTarget2D GameTarget => gameTarget;

        public static event Action<GameTime> OnPostDraw;

        protected override void Draw(GameTime gameTime)
        {
            if(gameTarget == null || gameTarget.IsDisposed || gameTarget.Size() != GameUtils.WindowBounds)
            {
                gameTarget?.Dispose();
                var presentationParams = GraphicsDevice.PresentationParameters;
                gameTarget = new RenderTarget2D(GraphicsDevice, presentationParams.BackBufferWidth, presentationParams.BackBufferHeight, false, presentationParams.BackBufferFormat, presentationParams.DepthStencilFormat, presentationParams.MultiSampleCount, RenderTargetUsage.PreserveContents);
            }
            GraphicsDevice.SetRenderTarget(gameTarget);
            try
            {
                // GraphicsDevice.RasterizerState = Default;

                Matrix2D = Matrix.CreateOrthographicOffCenter(0, GameUtils.WindowWidth, GameUtils.WindowHeight, 0, -1, 1)
                    * Matrix.CreateScale(UiScale);

                GraphicsDevice.Clear(ClearColor);

                DecalSystem.UpdateRenderTarget();


                SpriteRenderer.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied/*, transformMatrix: Matrix2D*/, rasterizerState: DefaultRasterizer);



                if (DebugUtils.DebuggingEnabled)
                    SpriteRenderer.DrawString(TextFont, "Debug Level: " + DebugUtils.CurDebugLabel, new Vector2(10), Color.White, new Vector2(0.6f));
                DebugUtils.DrawDebugString(SpriteRenderer, $"Garbage Collection: {MemoryParser.FromMegabytes(GCMemory)} MB" +
                    $"\nProcess Memory: {MemoryParser.FromMegabytes(_memBytes)} MB", new(8, GameUtils.WindowHeight * 0.15f));
                DebugUtils.DrawDebugString(SpriteRenderer, $"{SysGPU}\n{SysCPU}", new(8, GameUtils.WindowHeight * 0.2f));

                DebugUtils.DrawDebugString(SpriteRenderer, $"Tank Kill Counts:", new(8, GameUtils.WindowHeight * 0.05f), 2);

                for (int i = 0; i < PlayerTank.TankKills.Count; i++)
                {
                    var tier = PlayerTank.TankKills.ElementAt(i).Key;
                    var count = PlayerTank.TankKills.ElementAt(i).Value;

                    DebugUtils.DrawDebugString(SpriteRenderer, $"{tier}: {count}", new(8, GameUtils.WindowHeight * 0.05f + (14f*(i + 1))), 2);
                }

                DebugUtils.DrawDebugString(SpriteRenderer, $"Lives / StartingLives: {PlayerTank.Lives} / {PlayerTank.StartingLives}" +
                    $"\nKillCount: {PlayerTank.KillCount}" +
                    $"\n\nSaveable Game Data:" +
                    $"\nTotal / Bullet / Mine / Bounce Kills: {GameData.TotalKills} / {GameData.BulletKills} / {GameData.MineKills} / {GameData.BounceKills}" +
                    $"\nTotal Deaths: {GameData.Deaths}" +
                    $"\nTotal Suicides: {GameData.Suicides}" +
                    $"\nMissions Completed: {GameData.MissionsCompleted}" +
                    $"\nExp Level / DecayMultiplier: {GameData.ExpLevel} / {GameData.UniversalExpMultiplier}", new(8, GameUtils.WindowHeight * 0.4f), 2);

                if (SpeedrunMode)
                {
                    if (GameHandler.CurrentSpeedrun is not null)
                    {
                        int num = 0;

                        if (GameProperties.LoadedCampaign.CurrentMissionId > 2)
                            num = GameProperties.LoadedCampaign.CurrentMissionId - 2;
                        else if (GameProperties.LoadedCampaign.CurrentMissionId == 1)
                            num = GameProperties.LoadedCampaign.CurrentMissionId - 1;

                        var len = GameProperties.LoadedCampaign.CurrentMissionId + 2 > GameProperties.LoadedCampaign.CachedMissions.Length ? GameProperties.LoadedCampaign.CachedMissions.Length - 1 : GameProperties.LoadedCampaign.CurrentMissionId + 2;

                        SpriteRenderer.DrawString(TextFontLarge, $"Time: {GameHandler.CurrentSpeedrun.Timer.Elapsed}", new Vector2(10, 5), Color.White, new Vector2(0.15f), 0f, Vector2.Zero);
                        for (int i = num; i <= len; i++) // current.times.count originally
                        {
                            var time = GameHandler.CurrentSpeedrun.MissionTimes.ElementAt(i);
                            // display mission name and time taken
                            SpriteRenderer.DrawString(TextFontLarge, $"{time.Key}: {time.Value.Item2}", new Vector2(10, 20 + ((i - num) * 15)), Color.White, new Vector2(0.15f), 0f, Vector2.Zero);
                        }
                    }
                }

                for (int i = 0; i < PlayerTank.TankKills.Count; i++)
                {
                    //var tier = GameData.KillCountsTiers[i];
                    //var count = GameData.KillCountsCount[i];
                    var tier = PlayerTank.TankKills.ElementAt(i).Key;
                    var count = PlayerTank.TankKills.ElementAt(i).Value;

                    DebugUtils.DrawDebugString(SpriteRenderer, $"{tier}: {count}", new(GameUtils.WindowWidth * 0.9f, 8 + (14f * (i + 1))), 2);
                }

                GraphicsDevice.DepthStencilState = new DepthStencilState() { };

                GameHandler.RenderAll();

                SpriteRenderer.End();

                base.Draw(gameTime);

                SpriteRenderer.Begin(blendState: BlendState.AlphaBlend, effect: GameShaders.MouseShader, rasterizerState: DefaultRasterizer);

                MouseRenderer.DrawMouse();

                SpriteRenderer.End();

                foreach (var triangle in Triangle2D.triangles)
                    triangle.DrawImmediate();
                foreach (var qu in Quad3D.quads)
                    qu.Render();

                RenderTime = gameTime.ElapsedGameTime;
                RenderFPS = Math.Round(1f / gameTime.ElapsedGameTime.TotalSeconds);
            }
            catch (Exception e) when (!Debugger.IsAttached)
            {
                GameHandler.ClientLog.Write($"Error: {e.Message}\n{e.StackTrace}", LogType.Error);
                throw;
            }

            GraphicsDevice.SetRenderTarget(null);

            SpriteRenderer.Begin();
            SpriteRenderer.Draw(gameTarget, Vector2.Zero, Color.White);
            SpriteRenderer.End();

            OnPostDraw?.Invoke(gameTime);
        }
    }
}
