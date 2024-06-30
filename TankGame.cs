using System;
using System.IO;
using System.Text.Json;
using System.Reflection;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using FontStashSharp;

using Steamworks;

using TanksRebirth.Internals;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals.UI;
using TanksRebirth.Internals.Common.IO;
using TanksRebirth.Internals.Common.Framework.Input;
using TanksRebirth.Internals.Common.Framework.Core;
using TanksRebirth.Internals.Core;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Framework;
using TanksRebirth.Internals.Common.Framework.Graphics;
using TanksRebirth.GameContent;
using TanksRebirth.GameContent.UI;
using TanksRebirth.GameContent.ModSupport;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Properties;
using TanksRebirth.GameContent.Systems.PingSystem;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Graphics;
using TanksRebirth.Graphics.Cameras;
using TanksRebirth.Localization;
using TanksRebirth.Net;
using TanksRebirth.IO;
using TanksRebirth.Achievements;

namespace TanksRebirth;

#pragma warning disable CS8618
public class TankGame : Game {

    #region Fields1
    public static Language GameLanguage = new();

    public static int MainThreadId { get; private set; }

    public static bool IsMainThread => Environment.CurrentManagedThreadId == MainThreadId;

    /// <summary>Currently not functional due to programming problems.</summary>
    public static Camera GameCamera;

    public static OrthographicCamera OrthographicCamera;
    public static SpectatorCamera SpectatorCamera;
    public static PerspectiveCamera PerspectiveCamera;

    /// <summary>The hardware used by the user's computer.</summary>
    public readonly ComputerSpecs CompSpecs;

    public static TimeSpan RenderTime { get; private set; }
    public static TimeSpan LogicTime { get; private set; }

    public static double LogicFPS { get; private set; }
    public static double RenderFPS { get; private set; }

    /// <summary>Total memory used by the Garbage Collector.</summary>
    public static ulong GCMemory => (ulong)GC.GetTotalMemory(false);
    /// <summary>The amount of ticks elapsed in a second of update time.</summary>
    public static float DeltaTime => Interp ? (!float.IsInfinity(60 / (float)LogicFPS) ? 60 / (float)LogicFPS : 0) : 1;
    /// <summary>Currently used physical memory by this application in bytes.</summary>
    public static long ProcessMemory {
        get {
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
    /// <summary>The index/vertex buffer used to render to a framebuffer.</summary>
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

    public readonly System.Version GameVersion;

    public static OSPlatform OperatingSystem;
    public static bool IsWindows => OperatingSystem == OSPlatform.Windows;
    public static bool IsMac => OperatingSystem == OSPlatform.OSX;
    public static bool IsLinux => OperatingSystem == OSPlatform.Linux;

    public readonly string MOTD;
    #endregion

    public TankGame() : base() {
        Directory.CreateDirectory(SaveDirectory);
        Directory.CreateDirectory(Path.Combine(SaveDirectory, "Resource Packs", "Scene"));
        Directory.CreateDirectory(Path.Combine(SaveDirectory, "Resource Packs", "Tank"));
        Directory.CreateDirectory(Path.Combine(SaveDirectory, "Resource Packs", "Music"));
        Directory.CreateDirectory(Path.Combine(SaveDirectory, "Logs"));
        GameHandler.ClientLog = new(Path.Combine(SaveDirectory, "Logs"), "client");
        try {
            var bytes = WebUtils.DownloadWebFile(
                "https://raw.githubusercontent.com/RighteousRyan1/tanks_rebirth_motds/master/motd.txt",
                out var name);
            MOTD = System.Text.Encoding.Default.GetString(bytes);
        }
        catch {
            // in the case that an HTTPRequestException is thrown (no internet access)
            MOTD = LocalizationRandoms.GetRandomMotd();
        }

        // check if platform is windows, mac, or linux
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            OperatingSystem = OSPlatform.Windows;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            OperatingSystem = OSPlatform.OSX;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
            OperatingSystem = OSPlatform.Linux;
        }

        CompSpecs = ComputerSpecs.GetSpecs(out bool error);

        if (error) {
            GameHandler.ClientLog.Write(
                "Unable to load computer specs: Specified OS Architecture is not Windows.", LogType.Warn);
        }
        else {
            GameHandler.ClientLog.Write($"CPU: {CompSpecs.CPU.Name} (Core Count: {CompSpecs.CPU.CoreCount})", LogType.Info);
            GameHandler.ClientLog.Write($"GPU: {CompSpecs.GPU.Name} (Driver Version: {CompSpecs.GPU.DriverVersion} | VRAM: {MathF.Round(MemoryParser.FromGigabytes(CompSpecs.GPU.VRAM))} GB)", LogType.Info);
            GameHandler.ClientLog.Write($"Physical Memory (RAM): {CompSpecs.RAM.Manufacturer} {MathF.Round(MemoryParser.FromGigabytes(CompSpecs.RAM.TotalPhysical))} GB {CompSpecs.RAM.Type}@{CompSpecs.RAM.ClockSpeed}Mhz", LogType.Info);
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

        GameVersion = typeof(TankGame).Assembly.GetName().Version!;

        GameHandler.ClientLog.Write(
            $"Running {typeof(TankGame).Assembly.GetName().Name} on version {GameVersion}'", LogType.Info);
    }

    private ulong _memBytes;

    public static Stopwatch CurrentSessionTimer = new();

    public static DateTime LaunchTime;
    public static bool IsSouthernHemi;

    public static string GameDir { get; private set; }

    public static AutoUpdater AutoUpdater;

    public static AchievementPopupHandler VanillaAchievementPopupHandler;

    private void PreparingDeviceSettingsListener(object sender, PreparingDeviceSettingsEventArgs ev) {
        ev.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
    }

    protected override void Initialize() {
        GameDir = Directory.GetCurrentDirectory();
        if (Debugger.IsAttached && SteamAPI.IsSteamRunning())
            SteamworksUtils.Initialize();
        CurrentSessionTimer.Start();
        PingMenu.Initialize();

        GameHandler.MapEvents();
        GameHandler.ClientLog.Write($"Mapped events...", LogType.Info);

        DiscordRichPresence.Load();
        GameHandler.ClientLog.Write($"Loaded Discord Rich Presence...", LogType.Info);

        // systems = ReflectionUtils.GetInheritedTypesOf<IGameSystem>(Assembly.GetExecutingAssembly());

        ResolutionHandler.Initialize(Graphics);

        GameCamera = new OrthographicCamera(0, WindowUtils.WindowWidth, WindowUtils.WindowHeight, 0f, 0.01f, 2000f);

        SpriteRenderer = new(GraphicsDevice);

        Graphics.PreferMultiSampling = true;

        // Prevent the backbuffer from being wiped when switching render targets... to be reimplemented...
        // Graphics.PreparingDeviceSettings += PreparingDeviceSettingsListener;

        Graphics.ApplyChanges();

        GameHandler.ClientLog.Write($"Applying changes to graphics device... ({Graphics.PreferredBackBufferWidth}x{Graphics.PreferredBackBufferHeight})", LogType.Info);

        GameData.Setup();
        if (File.Exists(Path.Combine(GameData.Directory, GameData.Name)))
            GameData.Deserialize();

        GameHandler.ClientLog.Write($"Loaded save data.", LogType.Info);

        VanillaAchievements.InitializeToRepository();

        VanillaAchievementPopupHandler = new(VanillaAchievements.Repository);

        base.Initialize();
    }

    public static void Quit()
        => Instance.Exit();

    protected override void OnExiting(object sender, EventArgs args) {
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
    protected override void LoadContent() {
        var s = Stopwatch.StartNew();

        MainThreadId = Environment.CurrentManagedThreadId;

        Window.ClientSizeChanged += HandleResizing!; // IDE won't stop talking. so ! is the solution.

        OrthographicCamera = new(0, 0, 1920, 1080, -2000, 5000);
        SpectatorCamera = new(MathHelper.ToRadians(100), GraphicsDevice.Viewport.AspectRatio, 0.1f, 5000f);
        PerspectiveCamera = new(MathHelper.ToRadians(90), GraphicsDevice.Viewport.AspectRatio, 0.1f, 5000f);

        var profiler = new SpecAnalysis(CompSpecs.GPU, CompSpecs.CPU, CompSpecs.RAM);

        profiler.Analyze(false, out var ramr, out var gpur, out var cpur);

        ChatSystem.SendMessage(ramr, Color.White);
        ChatSystem.SendMessage(gpur, Color.White);
        ChatSystem.SendMessage(cpur, Color.White);

        ChatSystem.SendMessage(profiler.ToString(), Color.Brown);

        // I forget why this check is needed...
        ChatSystem.Initialize();

        _cachedState = GraphicsDevice.RasterizerState;

        UIElement.UIPanelBackground = GameResources.GetGameResource<Texture2D>("Assets/UIPanelBackground");

        Thunder.SoftRain = new OggAudio("Content/Assets/sounds/ambient/soft_rain.ogg");
        Thunder.SoftRain.Instance.Volume = 0;
        Thunder.SoftRain.Instance.IsLooped = true;

        OnFocusLost += TankGame_OnFocusLost!;
        OnFocusRegained += TankGame_OnFocusRegained!;

        WhitePixel = GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel");

        _fontSystem.AddFont(File.ReadAllBytes(@"Content/Assets/fonts/en_US.ttf"));
        _fontSystem.AddFont(File.ReadAllBytes(@"Content/Assets/fonts/ja_JP.ttf"));
        _fontSystem.AddFont(File.ReadAllBytes(@"Content/Assets/fonts/es_ES.ttf"));
        _fontSystem.AddFont(File.ReadAllBytes(@"Content/Assets/fonts/ru_RU.ttf"));

        GameHandler.ClientLog.Write($"Loaded fonts.", LogType.Info);

        TextFont = _fontSystem.GetFont(30);
        TextFontLarge = _fontSystem.GetFont(120);

        if (!File.Exists(Path.Combine(SaveDirectory, "settings.json"))) {
            Settings = new();
            SettingsHandler = new(Settings, Path.Combine(SaveDirectory, "settings.json"));
            JsonSerializerOptions opts = new() {
                WriteIndented = true
            };
            SettingsHandler.Serialize(opts, true);
        }
        else {
            SettingsHandler = new(Settings, Path.Combine(SaveDirectory, "settings.json"));
            Settings = SettingsHandler.Deserialize();
        }
        LaunchTime = DateTime.Now;
        IsSouthernHemi = RegionUtils.IsSouthernHemisphere(RegionInfo.CurrentRegion.EnglishName);

        GameHandler.ClientLog.Write($"Loaded user settings.", LogType.Info);

        #region Config Initialization

        Graphics.SynchronizeWithVerticalRetrace = Settings.Vsync;
        Graphics.IsFullScreen = Settings.FullScreen;
        PlayerTank.controlUp.ForceReassign(Settings.UpKeybind);
        PlayerTank.controlDown.ForceReassign(Settings.DownKeybind);
        PlayerTank.controlLeft.ForceReassign(Settings.LeftKeybind);
        PlayerTank.controlRight.ForceReassign(Settings.RightKeybind);
        PlayerTank.controlMine.ForceReassign(Settings.MineKeybind);
        MapRenderer.Theme = Settings.GameTheme;

        /*if (!IsSouthernHemi ? LaunchTime.Month != 12 : LaunchTime.Month != 7)
            MapRenderer.Theme = Settings.GameTheme;
        else
            MapRenderer.Theme = MapTheme.Christmas;*/

        TankFootprint.ShouldTracksFade = Settings.FadeFootprints;

        Graphics.PreferredBackBufferWidth = Settings.ResWidth;
        Graphics.PreferredBackBufferHeight = Settings.ResHeight;

        GameHandler.ClientLog.Write($"Applied user settings.", LogType.Info);

        Tank.SetAssetNames();
        TankMusicSystem.SetAssetAssociations();
        MapRenderer.LoadTexturePack(Settings.MapPack);
        TankMusicSystem.LoadSoundPack(Settings.MusicPack);
        Tank.LoadTexturePack(Settings.TankPack);
        Graphics.ApplyChanges();

        Language.LoadLang(Settings.Language, out GameLanguage);
        // Language.GenerateLocalizationTemplate("en_US.loc");

        Achievement.MysteryTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/achievement/secret");

        GameHandler.SetupGraphics();
        GameUI.Initialize();
        MainMenu.InitializeUIGraphics();
        MainMenu.InitializeBasics();

        // this is achievements stuff
        // TODO: fucking do it mate
        // AchievementsUI.GetVanillaAchievementsToList();
        // AchievementsUI.AchievementsPerRow = 10;
        // AchievementsUI.InitBtns();

        #endregion

        /*TankFootprint.DecalHandler.Effect = new(GraphicsDevice)
        {
            World = Matrix.CreateRotationX(MathHelper.PiOver2) * Matrix.CreateTranslation(0, 0.05f, 0),
            View = GameView,
            Projection = GameProjection,
        };*/

        MainMenu.Open();

        ModLoader.LoadMods();

        if (ModLoader.LoadingMods) {
            MainMenu.MenuState = MainMenu.State.LoadingMods;
            Task.Run(async () => {
                while (ModLoader.LoadingMods)
                    await Task.Delay(50).ConfigureAwait(false);
                MainMenu.MenuState = MainMenu.State.PrimaryMenu;
            });
        }

        GameHandler.ClientLog.Write("Running in directory: " + GameDir, LogType.Info);

        GameHandler.ClientLog.Write($"Content loaded in {s.Elapsed}.", LogType.Debug);
        GameHandler.ClientLog.Write($"DebugMode: {Debugger.IsAttached}", LogType.Debug);

        s.Stop();

        // it isnt really an autoupdater tho.
        AutoUpdater = new("https://github.com/RighteousRyan1/TanksRebirth", GameVersion);

        if (AutoUpdater.IsOutdated) {
            //CommandGlobals.IsUpdatePending = true;
            ChatSystem.SendMessage($"Outdated game version detected (current={GameVersion}, recent={AutoUpdater.GetRecentVersion()}).", Color.Red);
            //ChatSystem.SendMessage("Type /update to update the game and automatically restart.", Color.Red);
            SoundPlayer.SoundError();
        }
        // magic.
        const float SECRET_BASE_POS_X = MapRenderer.MIN_X - 28.5f;
        const float SECRET_BASE_POS_Y = 22;
        const float SECRET_BASE_POS_Z = 20;
        _secret1_1 = GameHandler.ParticleSystem.MakeParticle(new Vector3(100, 0.1f, 0), GameResources.GetGameResource<Texture2D>("Assets/textures/secret/special"));
        _secret1_1.UniqueBehavior = (p) => {
            _secret1_1.Position = new Vector3(SECRET_BASE_POS_X, SECRET_BASE_POS_Y, SECRET_BASE_POS_Z);
            _secret1_1.Roll = MathHelper.Pi;
            _secret1_1.Pitch = MathHelper.PiOver2;
            _secret1_1.Scale = Vector3.One * 0.3f;
            _secret1_1.HasAddativeBlending = false;
        };
        _secret1_2 = GameHandler.ParticleSystem.MakeParticle(new Vector3(100, 0.1f, 0), "Litzy <3");
        _secret1_2.UniqueBehavior = (p) => {
            _secret1_2.Position = new Vector3(SECRET_BASE_POS_X, SECRET_BASE_POS_Y + 20, SECRET_BASE_POS_Z - 8);
            _secret1_2.Roll = MathHelper.Pi;
            _secret1_2.Pitch = -MathHelper.PiOver2;
            _secret1_2.Scale = Vector3.One * 0.3f;
            _secret1_2.HasAddativeBlending = false;
        };

        _secret2_1 = GameHandler.ParticleSystem.MakeParticle(new Vector3(100, 0.1f, 0), GameResources.GetGameResource<Texture2D>("Assets/textures/secret/special2"));
        _secret2_1.UniqueBehavior = (p) => {
            _secret2_1.Position = new Vector3(SECRET_BASE_POS_X, SECRET_BASE_POS_Y, SECRET_BASE_POS_Z - 40);
            _secret2_1.Roll = MathHelper.Pi;
            _secret2_1.Pitch = MathHelper.PiOver2;
            _secret2_1.Scale = Vector3.One * 0.3f;
            _secret2_1.HasAddativeBlending = false;
        };
        _secret2_2 = GameHandler.ParticleSystem.MakeParticle(new Vector3(100, 0.1f, 0), "Ziggy <3");
        _secret2_2.UniqueBehavior = (p) => {
            _secret2_2.Position = new Vector3(SECRET_BASE_POS_X, SECRET_BASE_POS_Y + 20, SECRET_BASE_POS_Z - 8 - 40);
            _secret2_2.Roll = MathHelper.Pi;
            _secret2_2.Pitch = -MathHelper.PiOver2;
            _secret2_2.Scale = Vector3.One * 0.3f;
            _secret2_2.HasAddativeBlending = false;
        };
    }

    private Particle _secret1_1;
    private Particle _secret1_2;

    private Particle _secret2_1;
    private Particle _secret2_2;

    private void HandleResizing(object sender, EventArgs e) {
        // UIElement.ResizeAndRelocate();
    }

    public static void ReportError(Exception e, bool notifyUser = true, bool openFile = true, bool writeToLog = true) {
        if (writeToLog)
            GameHandler.ClientLog.Write($"Error: {e.Message}\n{e.StackTrace}", LogType.ErrorFatal);
        if (notifyUser) {
            var str = $"The error above is important for the developer of this game. If you are able to report it, explain how to reproduce it.";
            if (openFile)
                str += $"\nThis file was opened for your sake of helping the developer out.";
            GameHandler.ClientLog.Write(str, LogType.Info);
        }
        if (openFile)
            Process.Start(new ProcessStartInfo(GameHandler.ClientLog.FileName) {
                UseShellExecute = true,
                WorkingDirectory = Path.Combine(SaveDirectory, "Logs"),
            });
    }

    private void TankGame_OnFocusRegained(object sender, IntPtr e) {
        if (TankMusicSystem.IsLoaded) {
            if (Thunder.SoftRain.IsPaused())
                Thunder.SoftRain.Instance.Resume();
            TankMusicSystem.ResumeAll();
            if (MainMenu.Active)
                MainMenu.Theme.Resume();
            if (LevelEditor.Active)
                LevelEditor.Theme.Resume();
        }
    }
    private void TankGame_OnFocusLost(object sender, IntPtr e) {
        if (TankMusicSystem.IsLoaded) {
            if (Thunder.SoftRain.IsPlaying())
                Thunder.SoftRain.Instance.Pause();
            TankMusicSystem.PauseAll();
            if (MainMenu.Active)
                MainMenu.Theme.Pause();
            if (LevelEditor.Active)
                LevelEditor.Theme.Pause();
        }
    }
    #region Various Fields
    public const float DEFAULT_ORTHOGRAPHIC_ANGLE = 0.75f;
    public static Vector2 CameraRotationVector = new(0, DEFAULT_ORTHOGRAPHIC_ANGLE);

    public const float DEFAULT_ZOOM = 3.3f;
    public static float AddativeZoom = 1f;

    public static Vector2 CameraFocusOffset;

    private static bool _oView;
    public static bool OverheadView {
        get => _oView;
        set {
            transitionTimer = 100;
            _oView = value;
        }
    }

    private static int transitionTimer;

    public static Vector3 ThirdPersonCameraPosition = new(0, 100, 0);
    public static float ThirdPersonCameraRotation;
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
    #endregion

    public static bool IsCrashInfoVisible;
    public static CrashReportInfo CrashInfo;

    public static int SpectatorId;

    public static void DoZoomStuff() => _zoomAdd = _storedZoom;

    public static int SpectateValidTank(int id, bool increase)
    {
        var arr = GameHandler.AllPlayerTanks;

        var newId = id + (increase ? 1 : -1);

        if (newId < 0)
            return arr.Length - 1;
        else if (newId >= arr.Length)
            return 0;

        if (arr[newId] is null || arr[newId].Dead)
            return SpectateValidTank(newId, increase);
        else return newId;
    }
    protected override void Update(GameTime gameTime) {
        try {
            /*if (Debugger.IsAttached) {
                SteamworksUtils.SetSteamStatus("balls", "inspector");
                SteamFriends.GetFriendGamePlayed(SteamFriends.GetFriendByIndex(0, EFriendFlags.k_EFriendFlagAll), out var x);
                
            }*/
            DoUpdate(gameTime);
        }
        catch (Exception e) when (!Debugger.IsAttached) {
            ReportError(e, false, false);

            MainMenu.Theme.Volume = 0f;
            TankMusicSystem.PauseAll();

            SoundPlayer.SoundError();

            IsCrashInfoVisible = true;
            CrashInfo = new(e.Message, e.StackTrace ?? "No stack trace available.", e);
        }
    }

    private void DoUpdate(GameTime gameTime) {
        //SpectatorCamera.FieldOfView = MathHelper.ToRadians(100);
        //SpectatorCamera.AspectRatio = GraphicsDevice.Viewport.AspectRatio;
        //PerspectiveCamera.FieldOfView = MathHelper.ToRadians(90);
        //PerspectiveCamera.AspectRatio = GraphicsDevice.Viewport.AspectRatio;
        //SpectatorCamera.Position = new Vector3(0, 100, 0);
        //SpectatorCamera.Update();

        //OrthographicCamera.Translation = new(CameraFocusOffset.X, -CameraFocusOffset.Y + 40, 0);

        //GameCamera = SpectatorCamera;

        #region Non-Camera
        TargetElapsedTime = TimeSpan.FromMilliseconds(Interp ? 16.67 * (60f / Settings.TargetFPS) : 16.67);

        if (!float.IsInfinity(DeltaTime))
            RunTime += DeltaTime;

        if (!IsCrashInfoVisible) {
            if (InputUtils.AreKeysJustPressed(Keys.LeftAlt, Keys.RightAlt))
                Lighting.AccurateShadows = !Lighting.AccurateShadows;
            if (InputUtils.AreKeysJustPressed(Keys.LeftShift, Keys.RightShift))
                RenderWireframe = !RenderWireframe;

            if (DebugUtils.DebuggingEnabled && InputUtils.AreKeysJustPressed(Keys.O, Keys.P))
                ModLoader.LoadMods();
            if (DebugUtils.DebuggingEnabled && InputUtils.AreKeysJustPressed(Keys.U, Keys.I))
                ModLoader.UnloadAll();
            if (SteamworksUtils.IsInitialized)
                SteamworksUtils.Update();

            if (InputUtils.AreKeysJustPressed(Keys.Left, Keys.Right, Keys.Up, Keys.Down)) {
                SecretCosmeticSetting = !SecretCosmeticSetting;
                ChatSystem.SendMessage(SecretCosmeticSetting ? "Activated randomized cosmetics!" : "Deactivated randomized cosmetics.", SecretCosmeticSetting ? Color.Lime : Color.Red);
            }
            if (InputUtils.KeyJustPressed(Keys.F1)) {
                SpeedrunMode = !SpeedrunMode;
                if (SpeedrunMode)
                    GameProperties.OnMissionStart += GameHandler.StartSpeedrun;
                else
                    GameProperties.OnMissionStart -= GameHandler.StartSpeedrun;
                ChatSystem.SendMessage(SpeedrunMode ? "Speedrun mode on!" : "Speedrun mode off.", SpeedrunMode ? Color.Lime : Color.Red);
            }
            if (InputUtils.AreKeysJustPressed(Keys.RightAlt, Keys.Enter) || InputUtils.AreKeysJustPressed(Keys.LeftAlt, Keys.Enter)) {
                Graphics.IsFullScreen = !Graphics.IsFullScreen;
                Graphics.ApplyChanges();
            }

            RebirthMouse.ShouldRender = !Difficulties.Types["ThirdPerson"] || GameUI.Paused || MainMenu.Active || LevelEditor.Active;
            if (UIElement.delay > 0)
                UIElement.delay--;
        }

        if (NetPlay.CurrentClient is not null)
            Client.clientNetManager.PollEvents();
        if (NetPlay.CurrentServer is not null)
            Server.serverNetManager.PollEvents();
        if (!IsCrashInfoVisible) {
            UIElement.UpdateElements();
            GameUI.UpdateButtons();
        }

        DiscordRichPresence.Update();

        if (UpdateCount % 60 == 0 && DebugUtils.DebuggingEnabled) {
            _memBytes = (ulong)ProcessMemory;
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
        if (!IsCrashInfoVisible) {
            if (!Difficulties.Types["ThirdPerson"] || MainMenu.Active || LevelEditor.Active) {
                if (transitionTimer > 0) {
                    transitionTimer--;
                    if (OverheadView) {
                        //var bounce = Easings.OutBounce(DeltaTime / 2);
                        //CameraRotationVector.Y += bounce;
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

                if (!float.IsInfinity(DeltaTime))
                    _spinValue += _gradualIncrease * DeltaTime;

                if (MainMenu.Active) {
                    if (IntermissionSystem.IsAwaitingNewMission) {
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
                OrthographicCamera.SetLookAt(new(0f, 0, 350f), Vector3.Zero, Vector3.Up);

            }
            else {
                if (GameHandler.AllPlayerTanks[NetPlay.GetMyClientId()] is not null && !GameHandler.AllPlayerTanks[NetPlay.GetMyClientId()].Dead) {
                    SpectatorId = NetPlay.GetMyClientId();
                    ThirdPersonCameraPosition = GameHandler.AllPlayerTanks[NetPlay.GetMyClientId()].Position.ExpandZ();
                    ThirdPersonCameraRotation = -GameHandler.AllPlayerTanks[NetPlay.GetMyClientId()].TurretRotation;
                }
                else if (GameHandler.AllPlayerTanks[SpectatorId] is not null) {

                    if (InputUtils.KeyJustPressed(Keys.Left))
                        SpectatorId = SpectateValidTank(SpectatorId, false);
                    else if (InputUtils.KeyJustPressed(Keys.Right))
                        SpectatorId = SpectateValidTank(SpectatorId, true);

                    ThirdPersonCameraPosition = GameHandler.AllPlayerTanks[SpectatorId].Position.ExpandZ();
                    ThirdPersonCameraRotation = -GameHandler.AllPlayerTanks[SpectatorId].TurretRotation;


                    /*var moveSpeed = 2f * DeltaTime;


                    if (InputUtils.CurrentKeySnapshot.IsKeyDown(Keys.Up))
                        ThirdPersonCameraPosition += GameView.Forward * moveSpeed;
                    if (InputUtils.CurrentKeySnapshot.IsKeyDown(Keys.Down))
                        ThirdPersonCameraPosition += GameView.Backward * moveSpeed;
                    if (InputUtils.CurrentKeySnapshot.IsKeyDown(Keys.Left))
                        ThirdPersonCameraPosition += GameView.Left * moveSpeed;
                    if (InputUtils.CurrentKeySnapshot.IsKeyDown(Keys.Right))
                        ThirdPersonCameraPosition += GameView.Right * moveSpeed;*/

                    /*var rotationSpeed = 0.005f;

                    if (MouseUtils.MousePosition != _mOld)
                    {
                        _rot.X += (MouseUtils.MousePosition - _mOld).X * rotationSpeed;
                        _rot.Y += (MouseUtils.MousePosition - _mOld).Y * rotationSpeed;

                        MathHelper.Clamp(_rot.Y, -MathHelper.PiOver2, MathHelper.PiOver2);
                    }

                    _mOld = MouseUtils.MousePosition;
                    GameView = Matrix.CreateLookAt(ThirdPersonCameraPosition + new Vector3(0, 20, 0),
                        GameView.Forward, Vector3.Up) *
                        Matrix.CreateScale(AddativeZoom) *
                        Matrix.CreateRotationY(_rot.X) *
                        Matrix.CreateRotationX(_rot.Y) *
                        Matrix.CreateRotationZ(_rot.Z);*/
                }

                GameView = Matrix.CreateLookAt(ThirdPersonCameraPosition,
                    ThirdPersonCameraPosition + new Vector3(0, 0, 20).FlattenZ().RotatedByRadians(ThirdPersonCameraRotation).ExpandZ(),
                    Vector3.Up) * Matrix.CreateScale(AddativeZoom) *
                    Matrix.CreateRotationX(CameraRotationVector.Y - MathHelper.PiOver4) *
                    Matrix.CreateRotationY(CameraRotationVector.X) *
                    Matrix.CreateTranslation(0, -20, -40);

                GameProjection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(90), GraphicsDevice.Viewport.AspectRatio, 0.1f, 1000);
            }

            if (!GameUI.Paused && !MainMenu.Active && DebugUtils.DebuggingEnabled) {
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
        }

        DoUpdate2(gameTime);

        DoWorkaroundVolumes();

        // AchievementsUI.UpdateBtns();

        //for (int i = 0; i < AchievementsUI.AchBtns.Count; i++) {
            //var ach = AchievementsUI.AchBtns[i];
            // ach.Position -= new Vector2(2000);
        //}

        //GameView = GameCamera.View;
        //GameProjection = GameCamera.Projection;

        LogicTime = gameTime.ElapsedGameTime;

        LogicFPS = Math.Round(1f / gameTime.ElapsedGameTime.TotalSeconds);

        _wasActive = IsActive;
    }

    private void DoUpdate2(GameTime gameTime)
    {
        // TODO: this
        IsFixedTimeStep = !Settings.Vsync || !Interp;

        UpdateCount++;

        GameShaders.UpdateShaders();

        InputUtils.PollEvents();

        bool shouldUpdate = Client.IsConnected() || (IsActive && !GameUI.Paused && !CampaignCompleteUI.IsViewingResults);
        if (!IsCrashInfoVisible) {
            if (shouldUpdate) {
                if (InputUtils.AreKeysJustPressed(Keys.S, Keys.U, Keys.P, Keys.E, Keys.R)) {
                    if (!SuperSecretDevOption)
                        ChatSystem.SendMessage("You're a devious young one, aren't you?", Color.Orange, "DEBUG", true);
                    else
                        ChatSystem.SendMessage("I guess you aren't a devious one.", Color.Orange, "DEBUG", true);
                    SuperSecretDevOption = !SuperSecretDevOption;
                }

                GameHandler.UpdateAll(gameTime);

                // questionable as to why it causes hella lag on game start
                // TODO: try and find out why this happens lol.
                Tank.CollisionsWorld.Step(float.IsInfinity(DeltaTime) ? 1f : DeltaTime);

                HoveringAnyTank = false;
                if (!MainMenu.Active && (OverheadView || LevelEditor.Active)) {
                    foreach (var tnk in GameHandler.AllTanks) {
                        if (tnk is not null && !tnk.Dead) {
                            if (RayUtils.GetMouseToWorldRay().Intersects(tnk.Worldbox).HasValue) {
                                HoveringAnyTank = true;
                                if (InputUtils.KeyJustPressed(Keys.K)) {
                                    // var tnk = WPTR.AllAITanks.FirstOrDefault(tank => tank is not null && !tank.Dead && tank.tier == AITank.GetHighestTierActive());

                                    if (Array.IndexOf(GameHandler.AllTanks, tnk) > -1)
                                        tnk?.Destroy(new TankHurtContextOther()); // hmmm
                                }

                                if (InputUtils.CanDetectClick(rightClick: true)) {
                                    while (tnk!.TankRotation < 0) {
                                        tnk.TankRotation += MathHelper.Tau;
                                    }

                                    while (tnk.TankRotation > MathHelper.Tau) {
                                        tnk.TankRotation -= MathHelper.Tau;
                                    }

                                    while (tnk.TargetTankRotation < 0) {
                                        tnk.TargetTankRotation += MathHelper.Tau;
                                    }

                                    while (tnk.TargetTankRotation > MathHelper.Tau) {
                                        tnk.TargetTankRotation -= MathHelper.Tau;
                                    }

                                    while (tnk.TurretRotation < 0) {
                                        tnk.TurretRotation += MathHelper.Tau;
                                    }

                                    while (tnk.TurretRotation > MathHelper.Tau) {
                                        tnk.TurretRotation -= MathHelper.Tau;
                                    }


                                    tnk.TankRotation -= MathHelper.PiOver2;
                                    tnk.TurretRotation -= MathHelper.PiOver2;

                                    tnk.TargetTankRotation += MathHelper.PiOver2;

                                    if (tnk.TargetTankRotation >= MathHelper.Tau)
                                        tnk.TargetTankRotation -= MathHelper.Tau;

                                    if (tnk.TankRotation <= -MathHelper.Tau)
                                        tnk.TankRotation += MathHelper.Tau;

                                    if (tnk.TurretRotation <= -MathHelper.Tau)
                                        tnk.TurretRotation += MathHelper.Tau;

                                }

                                tnk.IsHoveredByMouse = true;
                            }
                            else
                                tnk.IsHoveredByMouse = false;
                        }
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

    static byte volmode; // TEMPORARY.

    private static void DoWorkaroundVolumes()
    {
        if (!VolumeUI.BatchVisible)
            return;

        float val = 0f;

        if (InputUtils.KeyJustPressed(Keys.RightShift))
            volmode++;
        if (volmode > 2)
            volmode = 0;

        if (InputUtils.CurrentKeySnapshot.IsKeyDown(Keys.Up))
            val += 0.01f * DeltaTime;
        if (InputUtils.CurrentKeySnapshot.IsKeyDown(Keys.Down))
            val -= 0.01f * DeltaTime;

        if (volmode == 0)
            Settings.MusicVolume += val;
        if (volmode == 1)
            Settings.EffectsVolume += val;
        if (volmode == 2)
            Settings.AmbientVolume += val;

        Settings.MusicVolume = MathHelper.Clamp(Settings.MusicVolume, 0, 1);
        Settings.EffectsVolume = MathHelper.Clamp(Settings.EffectsVolume, 0, 1);
        Settings.AmbientVolume = MathHelper.Clamp(Settings.AmbientVolume, 0, 1);
    }

    public static void SaveRenderTarget(string path = "screenshot.png")
    {
        using var fs = new FileStream(path, FileMode.OpenOrCreate);
        GameTarget.SaveAsPng(fs, GameTarget.Width, GameTarget.Height);
        ChatSystem.SendMessage("Saved image to " + fs.Name, Color.Lime);
    }

    public static bool SuperSecretDevOption;

    private static DepthStencilState _stencilState = new() { };
    protected override void Draw(GameTime gameTime) {
        if (gameTarget == null || gameTarget.IsDisposed || gameTarget.Size() != WindowUtils.WindowBounds) {
            gameTarget?.Dispose();
            var presentationParams = GraphicsDevice.PresentationParameters;
            gameTarget = new RenderTarget2D(GraphicsDevice, presentationParams.BackBufferWidth, presentationParams.BackBufferHeight, false, presentationParams.BackBufferFormat, presentationParams.DepthStencilFormat, 0, RenderTargetUsage.PreserveContents);
        }

        GraphicsDevice.SetRenderTarget(gameTarget);
        GraphicsDevice.Clear(ClearColor);

        // TankFootprint.DecalHandler.UpdateRenderTarget();
        SpriteRenderer.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, rasterizerState: DefaultRasterizer);

        GraphicsDevice.DepthStencilState = _stencilState;

        GameHandler.RenderAll();

        SpriteRenderer.End();

        foreach (var triangle in Triangle2D.triangles)
            triangle.DrawImmediate();
        foreach (var qu in Quad3D.quads)
            qu.Render();

        GraphicsDevice.SetRenderTarget(null);

        var vfx = Difficulties.Types["LanternMode"] ? GameShaders.LanternShader : (MainMenu.Active ? GameShaders.GaussianBlurShader : null);

        if (!MapRenderer.ShouldRenderAll)
            vfx = null;

        SpriteRenderer.Begin(effect: vfx);
        SpriteRenderer.Draw(gameTarget, Vector2.Zero, Color.White);

        SpriteRenderer.End();

        SpriteRenderer.Begin();
        if (MainMenu.Active)
            MainMenu.Render();
        // i really wish i didn't have to draw this here.
        VanillaAchievementPopupHandler.DrawPopup(SpriteRenderer);
        #region Debug
        if (Debugger.IsAttached)
            SpriteRenderer.DrawString(TextFont, "DEBUGGER ATTACHED", new Vector2(10, 50), Color.Red, new Vector2(0.8f));

        if (DebugUtils.DebuggingEnabled) {
            SpriteRenderer.DrawString(TextFont, "Debug Level: " + DebugUtils.CurDebugLabel, new Vector2(10), Color.White, new Vector2(0.6f));
            DebugUtils.DrawDebugString(SpriteRenderer, $"Garbage Collection: {MemoryParser.FromMegabytes(GCMemory):0} MB" +
                $"\nPhysical Memory: {CompSpecs.RAM}" +
                $"\nGPU: {CompSpecs.GPU}" +
                $"\nCPU: {CompSpecs.CPU}" +
                    $"\nProcess Memory: {MemoryParser.FromMegabytes(_memBytes):0} MB / Total Memory: {MemoryParser.FromMegabytes(CompSpecs.RAM.TotalPhysical):0}MB", new(8, WindowUtils.WindowHeight * 0.15f));

            DebugUtils.DrawDebugString(SpriteRenderer, $"Tank Kill Counts:", new(8, WindowUtils.WindowHeight * 0.05f), 2);

            for (int i = 0; i < PlayerTank.TankKills.Count; i++) {
                var tier = PlayerTank.TankKills.ElementAt(i).Key;
                var count = PlayerTank.TankKills.ElementAt(i).Value;

                DebugUtils.DrawDebugString(SpriteRenderer, $"{tier}: {count}", new(8, WindowUtils.WindowHeight * 0.05f + (14f * (i + 1))), 2);
            }
            DebugUtils.DrawDebugString(SpriteRenderer, $"Lives / StartingLives: {PlayerTank.Lives[Client.IsConnected() ? NetPlay.CurrentClient.Id : 0]} / {PlayerTank.StartingLives}" +
                                                       $"\nKillCount: {PlayerTank.KillCount}" +
                                                       $"\n\nSavable Game Data:" +
                                                       $"\nTotal / Bullet / Mine / Bounce Kills: {GameData.TotalKills} / {GameData.BulletKills} / {GameData.MineKills} / {GameData.BounceKills}" +
                                                       $"\nTotal Deaths: {GameData.Deaths}" +
                                                       $"\nTotal Suicides: {GameData.Suicides}" +
                                                       $"\nMissions Completed: {GameData.MissionsCompleted}" +
                                                       $"\nExp Level / DecayMultiplier: {GameData.ExpLevel} / {GameData.UniversalExpMultiplier}", new(8, WindowUtils.WindowHeight * 0.4f), 2);
        }


        if (SpeedrunMode && GameHandler.CurrentSpeedrun is not null) {
            var num = GameProperties.LoadedCampaign.CurrentMissionId switch {
                > 2 => GameProperties.LoadedCampaign.CurrentMissionId - 2,
                1 => GameProperties.LoadedCampaign.CurrentMissionId - 1,
                _ => 0,
            };

            var len = GameProperties.LoadedCampaign.CurrentMissionId + 2 > GameProperties.LoadedCampaign.CachedMissions.Length ? GameProperties.LoadedCampaign.CachedMissions.Length - 1 : GameProperties.LoadedCampaign.CurrentMissionId + 2;

            SpriteRenderer.DrawString(TextFontLarge, $"Time: {GameHandler.CurrentSpeedrun.Timer.Elapsed}", new Vector2(10, 5), Color.White, new Vector2(0.15f), 0f, Vector2.Zero);
            for (int i = num; i <= len; i++) { // current.times.count originally

                var time = GameHandler.CurrentSpeedrun.MissionTimes.ElementAt(i);
                // display mission name and time taken
                SpriteRenderer.DrawString(TextFontLarge, $"{time.Key}: {time.Value.Item2}", new Vector2(10, 20 + ((i - num) * 15)), Color.White, new Vector2(0.15f), 0f, Vector2.Zero);
            }
        }

        if (DebugUtils.DebuggingEnabled) {
            for (int i = 0; i < PlayerTank.TankKills.Count; i++) {
                //var tier = GameData.KillCountsTiers[i];
                //var count = GameData.KillCountsCount[i];
                var tier = PlayerTank.TankKills.ElementAt(i).Key;
                var count = PlayerTank.TankKills.ElementAt(i).Value;

                DebugUtils.DrawDebugString(SpriteRenderer, $"{tier}: {count}", new(WindowUtils.WindowWidth * 0.9f, 8 + (14f * (i + 1))), 2);
            }

            foreach (var body in Tank.CollisionsWorld.BodyList) {
                DebugUtils.DrawDebugString(SpriteRenderer, $"BODY",
                    MatrixUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(body.Position.X * Tank.UNITS_PER_METER, 0, body.Position.Y * Tank.UNITS_PER_METER), TankGame.GameView, TankGame.GameProjection), centered: true);
            }

            for (int i = 0; i < VanillaAchievements.Repository.GetAchievements().Count; i++) {
                var achievement = VanillaAchievements.Repository.GetAchievements()[i];

                DebugUtils.DrawDebugString(SpriteRenderer, $"{achievement.Name}: {(achievement.IsComplete ? "Complete" : "Incomplete")}",
                    new Vector2(8, 24 + (i * 20)), level: DebugUtils.Id.AchievementData, centered: false);
            }
        }


        #region TankInfo

        if (DebugUtils.DebuggingEnabled) {
            DebugUtils.DrawDebugString(SpriteRenderer, "Spawn Tank With Info:", WindowUtils.WindowTop + new Vector2(0, 8), 3, centered: true);
            DebugUtils.DrawDebugString(SpriteRenderer, $"Tier: {TankID.Collection.GetKey(GameHandler.tankToSpawnType)}", WindowUtils.WindowTop + new Vector2(0, 24), 3, centered: true);
            DebugUtils.DrawDebugString(SpriteRenderer, $"Team: {TeamID.Collection.GetKey(GameHandler.tankToSpawnTeam)}", WindowUtils.WindowTop + new Vector2(0, 40), 3, centered: true);
            DebugUtils.DrawDebugString(SpriteRenderer, $"CubeStack: {GameHandler.blockHeight} | CubeType: {BlockID.Collection.GetKey(GameHandler.blockType)}", WindowUtils.WindowBottom - new Vector2(0, 20), 3, centered: true);

            DebugUtils.DrawDebugString(SpriteRenderer, $"HighestTier: {AiHelpers.GetHighestTierActive()}", new(10, WindowUtils.WindowHeight * 0.26f), 1);
            // DebugUtils.DrawDebugString(TankGame.SpriteRenderer, $"CurSong: {(Music.AllMusic.FirstOrDefault(music => music.Volume == 0.5f) != null ? Music.AllMusic.FirstOrDefault(music => music.Volume == 0.5f).Name : "N/A")}", new(10, WindowUtils.WindowHeight - 100), 1);

            for (int i = 0; i < TankID.Collection.Count; i++)
                DebugUtils.DrawDebugString(SpriteRenderer, $"{TankID.Collection.GetKey(i)}: {AiHelpers.GetTankCountOfType(i)}", new(10, WindowUtils.WindowHeight * 0.3f + (i * 20)), 1);
        }

        GameHandler.tankToSpawnType = MathHelper.Clamp(GameHandler.tankToSpawnType, 2, TankID.Collection.Count - 1);
        GameHandler.tankToSpawnTeam = MathHelper.Clamp(GameHandler.tankToSpawnTeam, 0, TeamID.Collection.Count - 1);
        #endregion

        if (DebugUtils.DebuggingEnabled) {
            DebugUtils.DrawDebugString(SpriteRenderer, $"Logic Time: {LogicTime.TotalMilliseconds:0.00}ms" +
                                                       $"\nLogic FPS: {LogicFPS}" +
                                                       $"\n\nRender Time: {RenderTime.TotalMilliseconds:0.00}ms" +
                                                       $"\nRender FPS: {RenderFPS}" +
                                                       $"\nKeys U + I: Unload All Mods" +
                                                       $"\nKeys O + P: Reload All Mods", new(10, 500));

            DebugUtils.DrawDebugString(SpriteRenderer, $"Current Mission: {GameProperties.LoadedCampaign.CurrentMission.Name}\nCurrent Campaign: {GameProperties.LoadedCampaign.MetaData.Name}", WindowUtils.WindowBottomLeft - new Vector2(-4, 40), 3, centered: false);
        }

        #endregion
        SpriteRenderer.End();

        ChatSystem.DrawMessages();

        SpriteRenderer.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, rasterizerState: DefaultRasterizer);
        if (LevelEditor.Active)
            LevelEditor.Render();
        GameHandler.RenderUI();

        // cuno made me do this actually
        if (VolumeUI.BatchVisible) {
            Dictionary<byte, string> display = new() {
                [0] = $"Music: {Math.Round(Settings.MusicVolume * 100, 1)}",
                [1] = $"Effects: {Math.Round(Settings.EffectsVolume * 100, 1)}",
                [2] = $"Ambient: {Math.Round(Settings.AmbientVolume * 100, 1)}",
            };

            SpriteRenderer.DrawString(TextFont, $"Since for some reason these sliders are broken, use these keybinds." +
                $"\nPress [RIGHTSHIFT] to change what volume to change. (Music, Effects, Ambient)" +
                $"\n-- {display[volmode]}% --" +
                $"\nPress UP (arrow) to INCREASE this volume." +
                $"\nPress DOWN (arrow) to DECREASE this volume.", new Vector2(12, 12).ToResolution(), Color.White, new Vector2(0.75f).ToResolution(), 0f, Vector2.Zero);
        }
        IntermissionSystem.Draw(SpriteRenderer);
        if (CampaignCompleteUI.IsViewingResults)
            CampaignCompleteUI.Render();

        if (IsCrashInfoVisible)
            DrawErrorScreen();

        SpriteRenderer.End();

        SpriteRenderer.Begin(blendState: BlendState.AlphaBlend, effect: GameShaders.MouseShader, rasterizerState: DefaultRasterizer);

        RebirthMouse.DrawMouse();

        SpriteRenderer.End();

        OnPostDraw?.Invoke(gameTime);
        RenderTime = gameTime.ElapsedGameTime;
        RenderFPS = Math.Round(1f / gameTime.ElapsedGameTime.TotalSeconds);
    }

    private static void DrawErrorScreen() {

        SpriteRenderer.Draw(WhitePixel, WindowUtils.ScreenRect, Color.Blue);

        SpriteRenderer.DrawString(TextFontLarge, ":(", new Vector2(100, 100).ToResolution(), Color.White, (Vector2.One).ToResolution());
        SpriteRenderer.DrawString(TextFontLarge, "Your game ran into a problem and might need to restart. We're just" +
            "\nshowing you what's wrong, and how it might affect your game.", new Vector2(100, 250).ToResolution(), Color.White, (Vector2.One * 0.4f).ToResolution());
        SpriteRenderer.DrawString(TextFontLarge, CrashInfo.Reason, new Vector2(100, 500).ToResolution(), Color.White, (Vector2.One * 0.5f).ToResolution());
        SpriteRenderer.DrawString(TextFontLarge, CrashInfo.Description, new Vector2(100, 550).ToResolution(), Color.White, (Vector2.One * 0.25f).ToResolution());

        var yMsg = "Press 'Y' to proceed with closing the game.";
        var nMsg = "Press 'N' to attempt to carry on with the game.";
        SpriteRenderer.DrawString(TextFontLarge, yMsg, WindowUtils.WindowBottomLeft + new Vector2(10, -10), Color.White, (Vector2.One * 0.2f).ToResolution(), origin: GameUtils.GetAnchor(Anchor.BottomLeft, TextFontLarge.MeasureString(yMsg)));
        SpriteRenderer.DrawString(TextFontLarge, nMsg, WindowUtils.WindowBottomRight + new Vector2(-10, -10), Color.White, (Vector2.One * 0.2f).ToResolution(), origin: GameUtils.GetAnchor(Anchor.BottomRight, TextFontLarge.MeasureString(nMsg)));
        if (InputUtils.KeyJustPressed(Keys.Y)) {
            ReportError(CrashInfo.Cause, true, true, false);
            Quit();
        }
        if (InputUtils.KeyJustPressed(Keys.N)) {
            IsCrashInfoVisible = false;
            TankMusicSystem.ResumeAll();
        }
        
    }
}
