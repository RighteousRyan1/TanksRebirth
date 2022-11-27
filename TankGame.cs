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
using TanksRebirth.GameContent.ID;

namespace TanksRebirth
{
    public class TankGame : Game
    {
#pragma warning disable
        private static string GetGPU()
        {
            if (!IsWindows)
                return "Unavailable: Only supported on Windows";
            using var searcher = new ManagementObjectSearcher("select * from Win32_VideoController");

            foreach (ManagementObject obj in searcher.Get())
                return $"{obj["Name"]}"; // - {obj["DriverVersion"]}";
            return "Data not retrieved.";
        }

        public static string GetHardware(string hwclass, string syntax)
        {
            if (!IsWindows)
                return "Unavailable: Only supported on Windows";
            using var searcher = new ManagementObjectSearcher($"SELECT * FROM {hwclass}");

            foreach (var obj in searcher.Get())
                return $"{obj[syntax]}";
            return "Data not retrieved.";
        }
#pragma warning restore

        #region Fields1
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

        public static Camera GameCamera;

        public readonly string SysGPU;
        public readonly string SysCPU;

        public static TimeSpan RenderTime { get; private set; }
        public static TimeSpan LogicTime { get; private set; }

        public static double LogicFPS { get; private set; }
        public static double RenderFPS { get; private set; }

        public static long GCMemory => GC.GetTotalMemory(false);

        public static float DeltaTime => Interp ? (!float.IsInfinity(60 / (float)LogicFPS) ? 60 / (float)LogicFPS : 0) : 1;

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
        public static uint UpdateCount { get; private set; }

        public static float RunTime { get; private set; }

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
        #endregion

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

                SysGPU = $"GPU: {GetGPU()}";
                SysCPU = $"CPU: {GetHardware("Win32_Processor", "Name")}";

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
                WriteError(e);
                throw;
            }
        }

        private long _memBytes;

        public static Stopwatch CurrentSessionTimer = new();

        protected override void Initialize()
        {
            try
            {
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
                WriteError(e);
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

                Window.ClientSizeChanged += HandleResizing;

                // I forget why this check is needed...
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
                    GameResources.CopySrcFolderContents("Content/Assets/christmas", ".png");

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

                GameHandler.SetupGraphics();
                GameUI.Initialize();
                MainMenu.InitializeUIGraphics();

                /*TankFootprint.DecalHandler.Effect = new(GraphicsDevice)
                {
                    World = Matrix.CreateRotationX(MathHelper.PiOver2) * Matrix.CreateTranslation(0, 0.05f, 0),
                    View = GameView,
                    Projection = GameProjection,
                };*/

                if (ModLoader.LoadingMods) {
                    MainMenu.MenuState = MainMenu.State.LoadingMods;
                    Task.Run(async () => {
                        while (ModLoader.LoadingMods)
                            await Task.Delay(50).ConfigureAwait(false);
                        MainMenu.MenuState = MainMenu.State.PrimaryMenu;
                    });
                }

                

                GameHandler.ClientLog.Write("Running in directory: " + Directory.GetCurrentDirectory(), LogType.Info);

                GameHandler.ClientLog.Write($"Content loaded in {s.Elapsed}.", LogType.Debug);
                GameHandler.ClientLog.Write($"DebugMode: {Debugger.IsAttached}", LogType.Debug);

                s.Stop();
            }
            catch (Exception e) when (!Debugger.IsAttached)
            {
                WriteError(e);
                throw;
            }
        }

        private void HandleResizing(object sender, EventArgs e)
        {
            // UIElement.ResizeAndRelocate();
        }

        public static void WriteError(Exception e, bool notifyUser = true, bool openFile = true) {
            GameHandler.ClientLog.Write($"Error: {e.Message}\n{e.StackTrace}", LogType.Error);
            if (notifyUser)
                GameHandler.ClientLog.Write($"The error above is important for the developer of this game. If you are able to report it, explain how to reproduce it." +
                    $"\nThis file was opened for your sake of helping the developer out.", LogType.Info);
            if (openFile)
                Process.Start(new ProcessStartInfo(GameHandler.ClientLog.FileName) {
                    UseShellExecute = true,
                    WorkingDirectory = Path.Combine(SaveDirectory, "Logs"),
                });
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

        public static Vector3 ThirdPersonCameraPosition;
        public static Vector2 MouseVelocity => MouseUtils.GetMouseVelocity(WindowUtils.WindowCenter);

        public static bool SecretCosmeticSetting;
        public static bool SpeedrunMode;

        public static bool Interp = true;

        public static bool HoveringAnyTank;

        private static float _spinValue;

        private const float ADD_DEF = 0.8f;
        private static float _zoomAdd = ADD_DEF;

        private const float GRAD_INC_DEF = 0.0075f;
        private static float _gradualIncrease = GRAD_INC_DEF;

        private static float _storedZoom;

        public static void DoZoomStuff() => _zoomAdd = _storedZoom;
        protected override void Update(GameTime gameTime)
        {
            try
            {
                #region Non-Camera
                TargetElapsedTime = TimeSpan.FromMilliseconds(Interp ? 16.67 * (60f / Settings.TargetFPS) : 16.67);


                // hardcode shit for initializing locations. (if needed)
                //if (CurrentSessionTimer.Elapsed < TimeSpan.FromSeconds(5))
                    //UIElement.ResizeAndRelocate();

                if (!float.IsInfinity(DeltaTime))
                    RunTime += DeltaTime;

                if (InputUtils.AreKeysJustPressed(Keys.LeftAlt, Keys.RightAlt))
                    Lighting.AccurateShadows = !Lighting.AccurateShadows;
                if (InputUtils.AreKeysJustPressed(Keys.LeftShift, Keys.RightShift))
                    RenderWireframe = !RenderWireframe;

                if (DebugUtils.DebuggingEnabled && InputUtils.AreKeysJustPressed(Keys.V, Keys.B))
                    ModLoader.LoadMods();

                if (InputUtils.AreKeysJustPressed(Keys.Left, Keys.Right, Keys.Up, Keys.Down))
                {
                    SecretCosmeticSetting = !SecretCosmeticSetting;
                    ChatSystem.SendMessage(SecretCosmeticSetting ? "Activated randomized cosmetics!" : "Deactivated randomized cosmetics.", SecretCosmeticSetting ? Color.Lime : Color.Red);
                }
                if (InputUtils.KeyJustPressed(Keys.F1))
                {
                    SpeedrunMode = !SpeedrunMode;
                    if (SpeedrunMode)
                        GameProperties.OnMissionStart += GameHandler.StartSpeedrun;
                    else
                        GameProperties.OnMissionStart -= GameHandler.StartSpeedrun;
                    ChatSystem.SendMessage(SpeedrunMode ? "Speedrun mode on!" : "Speedrun mode off.", SpeedrunMode ? Color.Lime : Color.Red);
                }
                if (InputUtils.AreKeysJustPressed(Keys.LeftAlt | Keys.RightAlt, Keys.Enter))
                {
                    Graphics.IsFullScreen = !Graphics.IsFullScreen;
                    Graphics.ApplyChanges();
                }

                MouseRenderer.ShouldRender = Difficulties.Types["ThirdPerson"] ? (GameUI.Paused || MainMenu.Active) : true;
                if (UIElement.delay > 0)
                    UIElement.delay--;

                if (NetPlay.CurrentClient is not null)
                    Client.clientNetManager.PollEvents();
                if (NetPlay.CurrentServer is not null)
                    Server.serverNetManager.PollEvents();

                UIElement.UpdateElements();
                GameUI.UpdateButtons();

                DiscordRichPresence.Update();

                if (UpdateCount % 60 == 0 && DebugUtils.DebuggingEnabled) {
                    _memBytes = ProcessMemory;
                }

                LastGameTime = gameTime;

                if (_wasActive && !IsActive)
                    OnFocusLost?.Invoke(this, Window.Handle);
                if (!_wasActive && IsActive)
                    OnFocusRegained?.Invoke(this, Window.Handle);
                if (!MainMenu.Active && DebugUtils.DebuggingEnabled)
                    if (InputUtils.KeyJustPressed(Keys.J))
                        OverheadView = !OverheadView;
                #endregion
                if (!Difficulties.Types["ThirdPerson"])
                {
                    if (transitionTimer > 0) {
                        transitionTimer--;
                        if (OverheadView) {
                            CameraRotationVector.Y = MathUtils.SoftStep(CameraRotationVector.Y, MathHelper.PiOver2, 0.08f * DeltaTime);
                            AddativeZoom = MathUtils.SoftStep(AddativeZoom, 0.6f, 0.08f * DeltaTime);
                            CameraFocusOffset.Y = MathUtils.RoughStep(CameraFocusOffset.Y, 82f, 2f * DeltaTime);
                        }
                        else {
                            CameraRotationVector.Y = MathUtils.SoftStep(CameraRotationVector.Y, DEFAULT_ORTHOGRAPHIC_ANGLE, 0.08f * DeltaTime);
                            if (!LevelEditor.Active)
                                AddativeZoom = MathUtils.SoftStep(AddativeZoom, 1f, 0.08f * DeltaTime);
                            CameraFocusOffset.Y = MathUtils.RoughStep(CameraFocusOffset.Y, 0f, 2f * DeltaTime);
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

                    if (!float.IsInfinity(DeltaTime))
                        _spinValue +=  _gradualIncrease * DeltaTime;

                    if (MainMenu.Active) {
                        if (IntermissionSystem.IsAwaitingNewMission)
                        {
                            _gradualIncrease *= 1.075f;
                            _zoomAdd += _gradualIncrease;
                            _storedZoom = _zoomAdd;
                        }
                        else if (_zoomAdd > ADD_DEF)
                            _zoomAdd -= _gradualIncrease;
                        else
                            _zoomAdd = ADD_DEF;
                    }

                    if (IntermissionSystem.BlackAlpha >= 1f) {
                        _zoomAdd = ADD_DEF;
                        _gradualIncrease = GRAD_INC_DEF;
                    }

                    GameView =
                            Matrix.CreateScale(DEFAULT_ZOOM * AddativeZoom * (MainMenu.Active ? _zoomAdd : 1)) *
                            Matrix.CreateLookAt(new(0f, 0, 350f), Vector3.Zero, Vector3.Up) * // 0, 0, 350
                            Matrix.CreateTranslation(CameraFocusOffset.X, -CameraFocusOffset.Y + 40, 0) *
                            Matrix.CreateRotationY(CameraRotationVector.X + (MainMenu.Active ? _spinValue : 0)) *
                            Matrix.CreateRotationX(CameraRotationVector.Y);
                    GameProjection = Matrix.CreateOrthographic(1920, 1080, -2000, 5000);
                    //Matrix.CreateTranslation(CameraFocusOffset.X, -CameraFocusOffset.Y, 0);
                }
                else
                {
                    if (GameHandler.AllPlayerTanks.Any(x => x is not null && !x.Dead))
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
                        GameProjection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(90), GraphicsDevice.Viewport.AspectRatio, 0.1f, 1000);
                    }
                }

                if (!GameUI.Paused && !MainMenu.Active && DebugUtils.DebuggingEnabled)
                {
                    if (InputUtils.MouseRight)
                        CameraRotationVector += MouseVelocity / 500;

                    if (InputUtils.CurrentKeySnapshot.IsKeyDown(Keys.Add))
                        AddativeZoom += 0.01f;
                    if (InputUtils.CurrentKeySnapshot.IsKeyDown(Keys.Subtract))
                        AddativeZoom -= 0.01f;

                    if (InputUtils.MouseMiddle)
                        CameraFocusOffset += MouseVelocity;
                    MouseUtils.GetMouseVelocity(WindowUtils.WindowCenter);
                }

                FixedUpdate(gameTime);

                //GameView = GameCamera.GetView();
                //GameProjection = GameCamera.GetProjection();

                LogicTime = gameTime.ElapsedGameTime;

                LogicFPS = Math.Round(1f / gameTime.ElapsedGameTime.TotalSeconds);

                _wasActive = IsActive;
            }
            catch (Exception e) when (!Debugger.IsAttached)
            {
                WriteError(e);
                throw;
            }
        }

        public void FixedUpdate(GameTime gameTime)
        {
            // TODO: this
            IsFixedTimeStep = !Settings.Vsync || !Interp;

            UpdateCount++;

            GameShaders.UpdateShaders();

            InputUtils.PollEvents();

            bool shouldUpdate = Client.IsConnected() || (IsActive && !GameUI.Paused && !CampaignCompleteUI.IsViewingResults);

            if (shouldUpdate)
            {
                GameHandler.UpdateAll();

                Tank.CollisionsWorld.Step(1);

                HoveringAnyTank = false;
                if (!MainMenu.Active && OverheadView)
                {
                    foreach (var tnk in GameHandler.AllTanks)
                    {
                        if (tnk is not null && !tnk.Dead)
                        {
                            if (RayUtils.GetMouseToWorldRay().Intersects(tnk.Worldbox).HasValue)
                            {
                                HoveringAnyTank = true;
                                if (InputUtils.KeyJustPressed(Keys.K))
                                {
                                    // var tnk = WPTR.AllAITanks.FirstOrDefault(tank => tank is not null && !tank.Dead && tank.tier == AITank.GetHighestTierActive());

                                    if (Array.IndexOf(GameHandler.AllTanks, tnk) > -1)
                                        tnk?.Destroy(new TankHurtContext_Other()); // hmmm
                                }

                                if (InputUtils.CanDetectClick(rightClick: true))
                                {
                                    tnk.TankRotation -= MathHelper.PiOver2;
                                    tnk.TurretRotation -= MathHelper.PiOver2;
                                    if (tnk is AITank ai)
                                    {
                                        ai.TargetTankRotation += MathHelper.PiOver2;

                                        if (ai.TargetTankRotation >= MathHelper.Tau)
                                            ai.TargetTankRotation -= MathHelper.Tau;
                                        if (ai.TargetTankRotation <= 0)
                                            ai.TargetTankRotation += MathHelper.Tau;
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
        }
        public static Color ClearColor = Color.Black;

        public static bool RenderWireframe = false;

        public static RasterizerState _cachedState;

        public static RasterizerState DefaultRasterizer => RenderWireframe ? new() { FillMode = FillMode.WireFrame } : RasterizerState.CullNone;

        static RenderTarget2D gameTarget;
        public static RenderTarget2D GameTarget => gameTarget;

        public static event Action<GameTime> OnPostDraw;

        protected override void Draw(GameTime gameTime)
        {
            if(gameTarget == null || gameTarget.IsDisposed || gameTarget.Size() != WindowUtils.WindowBounds) {
                gameTarget?.Dispose();
                var presentationParams = GraphicsDevice.PresentationParameters;
                gameTarget = new RenderTarget2D(GraphicsDevice, presentationParams.BackBufferWidth, presentationParams.BackBufferHeight, false, presentationParams.BackBufferFormat, presentationParams.DepthStencilFormat, 0, RenderTargetUsage.PreserveContents);
            }

            GraphicsDevice.SetRenderTarget(gameTarget);
            try
            {
                GraphicsDevice.Clear(ClearColor);

                // TankFootprint.DecalHandler.UpdateRenderTarget();
                SpriteRenderer.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, rasterizerState: DefaultRasterizer);

                GraphicsDevice.DepthStencilState = new DepthStencilState() { };

                GameHandler.RenderAll();

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
                WriteError(e);
                throw;
            }

            GraphicsDevice.SetRenderTarget(null);

            SpriteRenderer.Begin(effect: Difficulties.Types["LanternMode"] ? GameShaders.LanternShader : GameShaders.GaussianBlurShader);
            SpriteRenderer.Draw(gameTarget, Vector2.Zero, Color.White);

            SpriteRenderer.End();

            SpriteRenderer.Begin();
            if (MainMenu.Active)
                MainMenu.Render();
            #region Debug
            if (Debugger.IsAttached)
                SpriteRenderer.DrawString(TextFont, "DEBUGGER ATTACHED", new Vector2(10, 50), Color.Red, new Vector2(0.8f));

            if (DebugUtils.DebuggingEnabled)
                SpriteRenderer.DrawString(TextFont, "Debug Level: " + DebugUtils.CurDebugLabel, new Vector2(10), Color.White, new Vector2(0.6f));
            DebugUtils.DrawDebugString(SpriteRenderer, $"Garbage Collection: {MemoryParser.FromMegabytes(GCMemory)} MB" +
                $"\nProcess Memory: {MemoryParser.FromMegabytes(_memBytes)} MB", new(8, WindowUtils.WindowHeight * 0.15f));
            DebugUtils.DrawDebugString(SpriteRenderer, $"{SysGPU}\n{SysCPU}", new(8, WindowUtils.WindowHeight * 0.2f));

            DebugUtils.DrawDebugString(SpriteRenderer, $"Tank Kill Counts:", new(8, WindowUtils.WindowHeight * 0.05f), 2);

            for (int i = 0; i < PlayerTank.TankKills.Count; i++)
            {
                var tier = PlayerTank.TankKills.ElementAt(i).Key;
                var count = PlayerTank.TankKills.ElementAt(i).Value;

                DebugUtils.DrawDebugString(SpriteRenderer, $"{tier}: {count}", new(8, WindowUtils.WindowHeight * 0.05f + (14f * (i + 1))), 2);
            }

            DebugUtils.DrawDebugString(SpriteRenderer, $"Lives / StartingLives: {PlayerTank.Lives} / {PlayerTank.StartingLives}" +
                $"\nKillCount: {PlayerTank.KillCount}" +
                $"\n\nSaveable Game Data:" +
                $"\nTotal / Bullet / Mine / Bounce Kills: {GameData.TotalKills} / {GameData.BulletKills} / {GameData.MineKills} / {GameData.BounceKills}" +
                $"\nTotal Deaths: {GameData.Deaths}" +
                $"\nTotal Suicides: {GameData.Suicides}" +
                $"\nMissions Completed: {GameData.MissionsCompleted}" +
                $"\nExp Level / DecayMultiplier: {GameData.ExpLevel} / {GameData.UniversalExpMultiplier}", new(8, WindowUtils.WindowHeight * 0.4f), 2);

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

                DebugUtils.DrawDebugString(SpriteRenderer, $"{tier}: {count}", new(WindowUtils.WindowWidth * 0.9f, 8 + (14f * (i + 1))), 2);
            }

            foreach (var body in Tank.CollisionsWorld.BodyList.ToList())
            {
                DebugUtils.DrawDebugString(SpriteRenderer, $"BODY",
                    MatrixUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(body.Position.X * Tank.UNITS_PER_METER, 0, body.Position.Y * Tank.UNITS_PER_METER), TankGame.GameView, TankGame.GameProjection), centered: true);
            }

            for (int i = 0; i < VanillaAchievements.Repository.GetAchievements().Count; i++)
            {
                var achievement = VanillaAchievements.Repository.GetAchievements()[i];
                DebugUtils.DrawDebugString(SpriteRenderer, $"{achievement.Name}: {(achievement.IsComplete ? "Complete" : "Incomplete")}",
                    new Vector2(8, 24 + (i * 20)), level: DebugUtils.Id.AchievementData, centered: false);
            }

            #region TankInfo
            DebugUtils.DrawDebugString(SpriteRenderer, "Spawn Tank With Info:", WindowUtils.WindowTop + new Vector2(0, 8), 3, centered: true);
            DebugUtils.DrawDebugString(SpriteRenderer, $"Tier: {TankID.Collection.GetKey(GameHandler.tankToSpawnType)}", WindowUtils.WindowTop + new Vector2(0, 24), 3, centered: true);
            DebugUtils.DrawDebugString(SpriteRenderer, $"Team: {TeamID.Collection.GetKey(GameHandler.tankToSpawnTeam)}", WindowUtils.WindowTop + new Vector2(0, 40), 3, centered: true);
            DebugUtils.DrawDebugString(SpriteRenderer, $"CubeStack: {GameHandler.blockHeight} | CubeType: {BlockID.Collection.GetKey(GameHandler.blockType)}", WindowUtils.WindowBottom - new Vector2(0, 20), 3, centered: true);

            DebugUtils.DrawDebugString(SpriteRenderer, $"HighestTier: {AITank.GetHighestTierActive()}", new(10, WindowUtils.WindowHeight * 0.26f), 1);
            // DebugUtils.DrawDebugString(TankGame.SpriteRenderer, $"CurSong: {(Music.AllMusic.FirstOrDefault(music => music.Volume == 0.5f) != null ? Music.AllMusic.FirstOrDefault(music => music.Volume == 0.5f).Name : "N/A")}", new(10, WindowUtils.WindowHeight - 100), 1);
            for (int i = 0; i < TankID.Collection.Count; i++)
            {
                DebugUtils.DrawDebugString(SpriteRenderer, $"{TankID.Collection.GetKey(i)}: {AITank.GetTankCountOfType(i)}", new(10, WindowUtils.WindowHeight * 0.3f + (i * 20)), 1);
            }

            GameHandler.tankToSpawnType = MathHelper.Clamp(GameHandler.tankToSpawnType, 2, TankID.Collection.Count - 1);
            GameHandler.tankToSpawnTeam = MathHelper.Clamp(GameHandler.tankToSpawnTeam, 0, TeamID.Collection.Count - 1);
            #endregion

            DebugUtils.DrawDebugString(SpriteRenderer, $"Logic Time: {LogicTime.TotalMilliseconds:0.00}ms" +
                $"\nLogic FPS: {LogicFPS}" +
                $"\n\nRender Time: {RenderTime.TotalMilliseconds:0.00}ms" +
                $"\nRender FPS: {RenderFPS}", new(10, 500));

            DebugUtils.DrawDebugString(SpriteRenderer, $"Current Mission: {GameProperties.LoadedCampaign.CurrentMission.Name}\nCurrent Campaign: {GameProperties.LoadedCampaign.MetaData.Name}", WindowUtils.WindowBottomLeft - new Vector2(-4, 40), 3, centered: false);

            #endregion
            SpriteRenderer.End();

            ChatSystem.DrawMessages();

            SpriteRenderer.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, rasterizerState: DefaultRasterizer);
            if (LevelEditor.Active)
                LevelEditor.Render();
            GameHandler.RenderUI();
            IntermissionSystem.Draw(SpriteRenderer);
            if (CampaignCompleteUI.IsViewingResults)
                CampaignCompleteUI.Render();
            SpriteRenderer.End();

            SpriteRenderer.Begin(blendState: BlendState.AlphaBlend, effect: GameShaders.MouseShader, rasterizerState: DefaultRasterizer);

            MouseRenderer.DrawMouse();

            SpriteRenderer.End();

            OnPostDraw?.Invoke(gameTime);
        }
    }
}
