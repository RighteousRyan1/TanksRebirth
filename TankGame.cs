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
using Microsoft.Xna.Framework.Audio;
using Steamworks;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals.UI;
using TanksRebirth.Internals.Common.IO;
using TanksRebirth.Internals.Common.Framework.Input;
using TanksRebirth.Internals.Common.Framework.Core;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Framework;
using TanksRebirth.Internals.Common.Framework.Graphics;
using TanksRebirth.GameContent;
using TanksRebirth.GameContent.UI;
using TanksRebirth.GameContent.ModSupport;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Systems.PingSystem;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Graphics;
using TanksRebirth.Graphics.Cameras;
using TanksRebirth.Localization;
using TanksRebirth.Net;
using TanksRebirth.IO;
using TanksRebirth.Achievements;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.GameContent.Speedrunning;
using tainicom.Aether.Physics2D.Collision;
using TanksRebirth.GameContent.Systems.Coordinates;
using TanksRebirth.Internals.Common.Framework.Animation;

namespace TanksRebirth;

#pragma warning disable CS8618
public class TankGame : Game {
    #region Fields1

    public static Language GameLanguage = new();
    /// <summary>The identifier of the main thread.</summary>
    public static int MainThreadId { get; private set; }
    public static bool IsMainThread => Environment.CurrentManagedThreadId == MainThreadId;

    /// <summary>Currently not functional due to programming problems.</summary>
    public static Camera GameCamera;

    public static OrthographicCamera OrthographicCamera;
    public static SpectatorCamera SpectatorCamera;
    public static PerspectiveCamera PerspectiveCamera;

    /// <summary>The hardware used by the user's computer.</summary>
    public static ComputerSpecs CompSpecs { get; private set; }
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
    }

    public static Freecam RebirthFreecam;

    public static GameTime LastGameTime { get; private set; }
    public static uint UpdateCount { get; private set; }

    public static float RunTime { get; private set; }

    public static Texture2D WhitePixel;
    public static Texture2D BlackPixel;

    public static TankGame Instance { get; private set; }
    public static readonly string ExePath = Assembly.GetExecutingAssembly().Location.Replace(@$"\{nameof(TanksRebirth)}.dll", string.Empty);
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

    public string MOTD { get; private set; }

    #endregion

    /// <summary>The handle of the game's logging file. Used to write information to a file that can be read after the game closes.</summary>
    public static Logger ClientLog { get; private set; }
    public TankGame() : base() {
        Directory.CreateDirectory(SaveDirectory);
        Directory.CreateDirectory(Path.Combine(SaveDirectory, "Resource Packs", "Scene"));
        Directory.CreateDirectory(Path.Combine(SaveDirectory, "Resource Packs", "Tank"));
        Directory.CreateDirectory(Path.Combine(SaveDirectory, "Resource Packs", "Music"));
        Directory.CreateDirectory(Path.Combine(SaveDirectory, "Logs"));
        Directory.CreateDirectory(Path.Combine(SaveDirectory, "Backup"));
        ClientLog = new(Path.Combine(SaveDirectory, "Logs"), "client");
        Task.Run(() => {
            try {
                ClientLog.Write(
                    "Obtaining message of the day (MOTD) from GitHub...",
                    LogType.Info);
                var bytes = WebUtils.DownloadWebFile(
                    "https://raw.githubusercontent.com/RighteousRyan1/tanks_rebirth_motds/master/motd.txt",
                    out var name);
                MOTD = System.Text.Encoding.Default.GetString(bytes);
            }
            catch {
                // in the case that an HTTPRequestException is thrown (no internet access)
                ClientLog.Write(
                    "Failed to obtain MOTD. Falling back to offline MOTDs.",
                    LogType.Warn);
                MOTD = LocalizationRandoms.GetRandomMotd();
            }
        });

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
        
        ClientLog.Write($"Playing on Operating System '{OperatingSystem}'", LogType.Info);

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

        ClientLog.Write(
            $"Running {typeof(TankGame).Assembly.GetName().Name} on version '{GameVersion}'",
            LogType.Info);
    }

    public static ulong MemoryUsageInBytes;

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
        GameHandler.Initialize();
        GameDir = Directory.GetCurrentDirectory();
        RebirthFreecam = new(GraphicsDevice);
        if (Debugger.IsAttached && SteamAPI.IsSteamRunning()) {
            ClientLog.Write("Initialising SteamWorks API...", LogType.Debug);
            SteamworksUtils.Initialize();
        }
        CurrentSessionTimer.Start();
        PingMenu.Initialize();

        GameHandler.MapEvents();
        ClientLog.Write($"Mapped events...", LogType.Info);

        DiscordRichPresence.Load();
        ClientLog.Write($"Loaded Discord Rich Presence...", LogType.Info);

        // systems = ReflectionUtils.GetInheritedTypesOf<IGameSystem>(Assembly.GetExecutingAssembly());

        GameCamera = new OrthographicCamera(0, WindowUtils.WindowWidth, WindowUtils.WindowHeight, 0f, 0.01f, 2000f);

        SpriteRenderer = new(GraphicsDevice);

        Graphics.PreferMultiSampling = true;

        // Prevent the backbuffer from being wiped when switching render targets... to be reimplemented...
        // Graphics.PreparingDeviceSettings += PreparingDeviceSettingsListener;

        Graphics.ApplyChanges();

        ClientLog.Write($"Applying changes to graphics device... ({Graphics.PreferredBackBufferWidth}x{Graphics.PreferredBackBufferHeight})", LogType.Info);

        GameData.Setup();
        if (File.Exists(Path.Combine(GameData.Directory, GameData.Name)))
            GameData.Deserialize();

        ClientLog.Write($"Loaded save data.", LogType.Info);

        VanillaAchievements.InitializeToRepository();

        IntermissionSystem.InitializeAnmiations();

        VanillaAchievementPopupHandler = new(VanillaAchievements.Repository);

        base.Initialize();
    }

    public static void Quit()
        => Instance.Exit();

    protected override void OnExiting(object sender, EventArgs args) {
        ClientLog.Write($"Handling termination process...", LogType.Info);
        GameData.TimePlayed += CurrentSessionTimer.Elapsed;
        CurrentSessionTimer.Stop();
        ClientLog.Dispose();
        SettingsHandler = new(Settings, Path.Combine(SaveDirectory, "settings.json"));
        JsonSerializerOptions opts = new() { WriteIndented = true };
        SettingsHandler.Serialize(opts, true);
        GameData.Serialize();

        DiscordRichPresence.Terminate();
    }

    void PreloadContent() {

        // TODO: is it emportant that these paths are all hardcoded? i'm doubtful.
        // do more dynamically..?
        string[] textures = [
            /* Miscellaneous */
            "Assets/christmas/snowflake_0",
            "Assets/christmas/snowflake_1",
            "Assets/christmas/snow",
            "Assets/textures/misc/ring",
            "Assets/textures/smoke/smokenade",
            "Assets/textures/smoke/smoke",
            "Assets/textures/misc/tank_rock",
            "Assets/textures/misc/tank_rock_2",
            "Assets/textures/ingame/block_shadow_h",
            "Assets/textures/ingame/block_other_c",
            "Assets/textures/misc/mouse_dot",
            "Assets/textures/misc/cursor_1",
            "Assets/textures/misc/tank_smokes",
            "Assets/textures/misc/tank_smokes",
            "Assets/textures/secret/special",
            "Assets/textures/secret/special2",
            
            
            /* Tanks Textures */
            "Assets/textures/tank/wee/tank_commando",
            "Assets/textures/tank/wee/tank_assassin",
            "Assets/textures/tank/wee/tank_rocket",
            "Assets/textures/tank/wee/tank_electro",
            "Assets/textures/tank/wee/tank_explosive",
            "Assets/textures/tank_shadow",
            "Assets/textures/bullet/bullet",
            "Assets/textures/bullet/flame",
            "Assets/textures/bullet/smoketrail",
            "Assets/textures/bullet/explosive_bullet",
            "Assets/textures/misc/armor",
            
            "Assets/textures/check/check_blue",
            "Assets/textures/check/check_red",
            "Assets/textures/check/check_green",
            "Assets/textures/check/check_yellow",
            "Assets/textures/check/check_white",

            "Assets/textures/tank_footprint",
            "Assets/textures/tank_footprint_alt",

            "Assets/textures/mine/mine_env",
            "Assets/textures/mine/mine_shadow",
            "Assets/textures/mine/explosion",

            /* UI */
            "Assets/textures/ui/bullet_ui",
            "Assets/textures/WhitePixel",
            "Assets/UIPanelBackground",
            "Assets/textures/ui/ping/ping_tex",
            "Assets/textures/ui/chatalert",
            "Assets/textures/ui/chevron_border",
            "Assets/textures/ui/chevron_inside",
            "Assets/textures/misc/light_particle",
            "Assets/textures/misc/tank_smokes",
            "Assets/textures/misc/bot_hit",
            "Assets/textures/ui/tank_background_billboard",
            "Assets/textures/ui/playertank2d",
            "Assets/textures/ui/banner",
            "Assets/textures/ui/grades",
            "Assets/textures/ui/scoreboard",
            "Assets/textures/ui/tank2d",
            "Assets/textures/ui/trophy",
            "Assets/textures/ui/achievement/secret",

        ];
        
        GameResources.MassPreloadAssets<Texture2D, TexturePreloadSettings>(
            textures
        , new TexturePreloadSettings{});

        // Prefix with Content for compatibility reasons with old code.
        // Done mostly dynamcially, because easier than hardcoding for 20 minutes each audio.
        List<string> sounds = [];
        
        // ~~ Vanilla audio ~~
        // sounds.AddRange(Directory.GetFiles("Content/Assets/music"));
        sounds.AddRange(Directory.GetFiles("Content", "*.ogg", SearchOption.AllDirectories));

        // ~~ Sound Effects ~~
        // sounds.AddRange(Directory.GetFiles("Content/Assets/sounds", "*.ogg", SearchOption.AllDirectories));

        GameResources.MassPreloadAssets<SoundEffect, TexturePreloadSettings>(
            sounds.ToArray()
            , default);
    }

    protected override void LoadContent() {
        PreloadContent();
        var s = Stopwatch.StartNew();

        MainThreadId = Environment.CurrentManagedThreadId;

        OrthographicCamera = new(0, 0, 1920, 1080, -2000, 5000);
        SpectatorCamera = new(MathHelper.ToRadians(100), GraphicsDevice.Viewport.AspectRatio, 0.1f, 5000f);
        PerspectiveCamera = new(MathHelper.ToRadians(90), GraphicsDevice.Viewport.AspectRatio, 0.1f, 5000f);

        Task.Run(() => {
            CompSpecs = ComputerSpecs.GetSpecs(out bool error);

            if (error) {
                ClientLog.Write(
                    "Unable to load computer specs: Specified OS Architecture is not Windows.",
                    LogType.Warn);
            }
            else {
                ClientLog.Write($"CPU: {CompSpecs.CPU.Name} (Core Count: {CompSpecs.CPU.CoreCount})", LogType.Info);
                ClientLog.Write($"GPU: {CompSpecs.GPU.Name} (Driver Version: {CompSpecs.GPU.DriverVersion} | VRAM: {MathF.Round(MemoryParser.FromGigabytes(CompSpecs.GPU.VRAM))} GB)", LogType.Info);
                ClientLog.Write($"Physical Memory (RAM): {CompSpecs.RAM.Manufacturer} {MathF.Round(MemoryParser.FromGigabytes(CompSpecs.RAM.TotalPhysical))} GB {CompSpecs.RAM.Type}@{CompSpecs.RAM.ClockSpeed}Mhz", LogType.Info);
            }

            if (!CompSpecs.Equals(default) && !error) {
                var profiler = new SpecAnalysis(CompSpecs.GPU, CompSpecs.CPU, CompSpecs.RAM);

                profiler.Analyze(false, out var ramr, out var gpur, out var cpur);

                ChatSystem.SendMessage(ramr, Color.White);
                ChatSystem.SendMessage(gpur, Color.White);
                ChatSystem.SendMessage(cpur, Color.White);

                ChatSystem.SendMessage(profiler.ToString(), Color.Brown);

                ClientLog.Write("Sucessfully analyzed hardware.", LogType.Info);
            }
            else {
                ClientLog.Write("Failed to analyze hardware.", LogType.Warn);
            }
        });

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
        BlackPixel = new Texture2D(GraphicsDevice, 1, 1);
        BlackPixel.SetData(new Color[] { Color.Black });

        _fontSystem.AddFont(File.ReadAllBytes(@"Content/Assets/fonts/en_US.ttf"));
        _fontSystem.AddFont(File.ReadAllBytes(@"Content/Assets/fonts/ja_JP.ttf"));
        _fontSystem.AddFont(File.ReadAllBytes(@"Content/Assets/fonts/es_ES.ttf"));
        _fontSystem.AddFont(File.ReadAllBytes(@"Content/Assets/fonts/ru_RU.ttf"));

        ClientLog.Write($"Loaded fonts.", LogType.Info);

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

        if (IsSouthernHemi)
            ClientLog.Write("User is in the southern hemisphere.", LogType.Info);
        else
            ClientLog.Write("User is in the northern hemisphere.", LogType.Info);

        ClientLog.Write($"Loaded user settings.", LogType.Info);

        #region Config Initialization

        Graphics.SynchronizeWithVerticalRetrace = Settings.Vsync;
        Graphics.IsFullScreen = Settings.FullScreen;
        PlayerTank.controlUp.ForceReassign(Settings.UpKeybind);
        PlayerTank.controlDown.ForceReassign(Settings.DownKeybind);
        PlayerTank.controlLeft.ForceReassign(Settings.LeftKeybind);
        PlayerTank.controlRight.ForceReassign(Settings.RightKeybind);
        PlayerTank.controlMine.ForceReassign(Settings.MineKeybind);
        GameSceneRenderer.Theme = Settings.GameTheme;

        /*if (!IsSouthernHemi ? LaunchTime.Month != 12 : LaunchTime.Month != 7)
            MapRenderer.Theme = Settings.GameTheme;
        else
            MapRenderer.Theme = MapTheme.Christmas;*/

        TankFootprint.ShouldTracksFade = Settings.FadeFootprints;

        Graphics.PreferredBackBufferWidth = Settings.ResWidth;
        Graphics.PreferredBackBufferHeight = Settings.ResHeight;

        ClientLog.Write($"Applied user settings.", LogType.Info);

        Tank.SetAssetNames();
        TankMusicSystem.SetAssetAssociations();
        GameSceneRenderer.LoadTexturePack(Settings.MapPack);
        TankMusicSystem.LoadSoundPack(Settings.MusicPack);
        Tank.LoadTexturePack(Settings.TankPack);
        Graphics.ApplyChanges();

        Language.LoadLang(Settings.Language, out GameLanguage);
        // Language.GenerateLocalizationTemplate("en_US.loc");

        Achievement.MysteryTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/achievement/secret");
    
        GameResources.EnsurePreloadedAssetsArePreloaded();
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

        ClientLog.Write("Running in directory: " + GameDir, LogType.Info);

        ClientLog.Write($"Content loaded in {s.Elapsed}.", LogType.Debug);
        ClientLog.Write($"DebugMode: {Debugger.IsAttached}", LogType.Debug);

        s.Stop();

        // it isnt really an autoupdater tho.
        Task.Run(() => {
            ClientLog.Write("Checking for update...", LogType.Info);
            AutoUpdater = new("https://github.com/RighteousRyan1/TanksRebirth", GameVersion);

            if (!AutoUpdater.IsOutdated) {
                ClientLog.Write("Game is up to date.", LogType.Info);
                return;
            }

            ClientLog.Write($"Game is out of date (current={GameVersion}, recent={AutoUpdater.GetRecentVersion()}).", LogType.Warn);
            //CommandGlobals.IsUpdatePending = true;
            ChatSystem.SendMessage($"Outdated game version detected (current={GameVersion}, recent={AutoUpdater.GetRecentVersion()}).", Color.Red);
            //ChatSystem.SendMessage("Type /update to update the game and automatically restart.", Color.Red);
            SoundPlayer.SoundError();
        });
        PlaceSecrets();
        SceneManager.GameLight.Apply(false);
    }
    public static void ReportError(Exception e, bool notifyUser = true, bool openFile = true, bool writeToLog = true) {
        if (writeToLog)
            ClientLog.Write($"Error: {e.Message}\n{e.StackTrace}", LogType.ErrorFatal);
        if (notifyUser) {
            var str = $"The error above is important for the developer of this game. If you are able to report it, explain how to reproduce it.";
            if (openFile)
                str += $"\nThis file was opened for your sake of helping the developer out.";
            ClientLog.Write(str, LogType.Info);
        }
        if (openFile)
            Process.Start(new ProcessStartInfo(ClientLog.FileName) {
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
    public static Vector2 OrthoRotationVector = new(0, DEFAULT_ORTHOGRAPHIC_ANGLE);

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

    public static Vector3 POVCameraPosition = new(0, 100, 0);
    public static float POVCameraRotation;
    public static Vector2 MouseVelocity;

    public static bool SecretCosmeticSetting;
    public static bool Interp { get; set; } = true;

    public static bool HoveringAnyTank;

    #endregion

    public static bool IsCrashInfoVisible;
    public static CrashReportInfo CrashInfo;

    public static int SpectatorId;

    public static int SpectateValidTank(int id, bool increase) {
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

            if (LevelEditor.Active && LevelEditor.loadedCampaign != null) {
                Campaign.Save(Path.Combine(SaveDirectory, "Backup", $"backup_{DateTime.Now.StringFormatCustom("_")}"), LevelEditor.loadedCampaign!);
            }

            IsCrashInfoVisible = true;
            CrashInfo = new(e.Message, e.StackTrace ?? "No stack trace available.", e);
        }
    }

    private Vector2 _mOld;
    private void DoUpdate(GameTime gameTime) {
        MouseUtils.MousePosition = new(InputUtils.CurrentMouseSnapshot.X, InputUtils.CurrentMouseSnapshot.Y);
        MouseVelocity = MouseUtils.MousePosition - _mOld;
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

            if (DebugManager.DebuggingEnabled && InputUtils.AreKeysJustPressed(Keys.O, Keys.P))
                ModLoader.LoadMods();
            if (DebugManager.DebuggingEnabled && InputUtils.AreKeysJustPressed(Keys.U, Keys.I))
                ModLoader.UnloadAll();
            if (SteamworksUtils.IsInitialized)
                SteamworksUtils.Update();

            if (InputUtils.AreKeysJustPressed(Keys.Left, Keys.Right, Keys.Up, Keys.Down)) {
                SecretCosmeticSetting = !SecretCosmeticSetting;
                ChatSystem.SendMessage(SecretCosmeticSetting ? "Activated randomized cosmetics!" : "Deactivated randomized cosmetics.", SecretCosmeticSetting ? Color.Lime : Color.Red);
            }
            if (InputUtils.KeyJustPressed(Keys.F1)) {
                Speedrun.SpeedrunMode = !Speedrun.SpeedrunMode;
                if (Speedrun.SpeedrunMode)
                    CampaignGlobals.OnMissionStart += Speedrun.StartSpeedrun;
                else
                    CampaignGlobals.OnMissionStart -= Speedrun.StartSpeedrun;
                ChatSystem.SendMessage(Speedrun.SpeedrunMode ? "Speedrun mode on!" : "Speedrun mode off.", Speedrun.SpeedrunMode ? Color.Lime : Color.Red);
            }
            if (InputUtils.AreKeysJustPressed(Keys.RightAlt, Keys.Enter) || InputUtils.AreKeysJustPressed(Keys.LeftAlt, Keys.Enter)) {
                Graphics.IsFullScreen = !Graphics.IsFullScreen;
                Graphics.ApplyChanges();
            }

            RebirthMouse.ShouldRender = !Difficulties.Types["POV"] || GameUI.Paused || MainMenu.Active || LevelEditor.Active;
            if (UIElement.delay > 0)
                UIElement.delay--;
        }

        if (NetPlay.CurrentClient is not null)
            Client.ClientManager.PollEvents();
        if (NetPlay.CurrentServer is not null)
            Server.NetManager.PollEvents();
        if (!IsCrashInfoVisible) {
            UIElement.UpdateElements();
            GameUI.UpdateButtons();
        }

        DiscordRichPresence.Update();

        if (UpdateCount % 60 == 0 && DebugManager.DebuggingEnabled) {
            MemoryUsageInBytes = (ulong)ProcessMemory;
        }

        LastGameTime = gameTime;

        if (_wasActive && !IsActive)
            OnFocusLost?.Invoke(this, Window.Handle);
        if (!_wasActive && IsActive)
            OnFocusRegained?.Invoke(this, Window.Handle);
        if (!MainMenu.Active && DebugManager.DebuggingEnabled)
            if (InputUtils.KeyJustPressed(Keys.J))
                OverheadView = !OverheadView;

        #endregion

        if (!IsCrashInfoVisible) {
            if (DebugManager.DebugLevel != DebugManager.Id.FreeCamTest && !DebugManager.persistFreecam) {
                if (!MainMenu.Active) {
                    if (!Difficulties.Types["POV"] || LevelEditor.Active) {
                        if (transitionTimer > 0) {
                            transitionTimer--;
                            if (OverheadView) {
                                OrthoRotationVector.Y = MathUtils.SoftStep(OrthoRotationVector.Y, MathHelper.PiOver2, 0.08f * DeltaTime);
                                AddativeZoom = MathUtils.SoftStep(AddativeZoom, 0.6f, 0.08f * DeltaTime);
                                CameraFocusOffset.Y = MathUtils.RoughStep(CameraFocusOffset.Y, 82f, 2f * DeltaTime);
                            }
                            else {
                                OrthoRotationVector.Y = MathUtils.SoftStep(OrthoRotationVector.Y, DEFAULT_ORTHOGRAPHIC_ANGLE, 0.08f * DeltaTime);
                                if (!LevelEditor.Active)
                                    AddativeZoom = MathUtils.SoftStep(AddativeZoom, 1f, 0.08f * DeltaTime);
                                CameraFocusOffset.Y = MathUtils.RoughStep(CameraFocusOffset.Y, 0f, 2f * DeltaTime);
                            }
                        }

                        GameView = Matrix.CreateScale(DEFAULT_ZOOM * AddativeZoom) *
                            // TODO: the Z component is 350 because for some reason values have been offset by that amount. i'll have to dig into my code
                            // to see where tf that happens but alright
                            Matrix.CreateLookAt(new(0f, 0, 350f), Vector3.Zero, Vector3.Up) *
                            Matrix.CreateTranslation(CameraFocusOffset.X, -CameraFocusOffset.Y + 40, 0) *
                            Matrix.CreateRotationY(OrthoRotationVector.X) *
                            Matrix.CreateRotationX(OrthoRotationVector.Y);
                        GameProjection = Matrix.CreateOrthographic(1920, 1080, -2000, 5000);
                    }
                }
                else {
                    // main menu animation semantics
                    RebirthFreecam.Position = MainMenu.CameraPositionAnimator.CurrentPosition3D;
                    RebirthFreecam.HasLookAt = true;
                    RebirthFreecam.LookAt = new Vector3(0, 0, 50);
                    RebirthFreecam.FieldOfView = 100f;
                    RebirthFreecam.NearViewDistance = 0.1f;
                    RebirthFreecam.FarViewDistance = 100000f;
                    GameView = RebirthFreecam.View;
                    GameProjection = RebirthFreecam.Projection;
                    /*if (InputUtils.KeyJustPressed(Keys.G)) {
                        MainMenu.CameraPositionAnimator = Animator.Create()
        .WithFrame(new(position3d: new Vector3(GameSceneRenderer.MAX_X / 2, 50, 0), duration: TimeSpan.FromSeconds(10), easing: EasingFunction.InOutQuad))
        .WithFrame(new(position3d: new Vector3(0, 50, GameSceneRenderer.MAX_Z / 2), duration: TimeSpan.FromSeconds(10), easing: EasingFunction.InOutQuad))
        .WithFrame(new(position3d: new Vector3(GameSceneRenderer.MIN_X / 2, 50, 0), duration: TimeSpan.FromSeconds(10), easing: EasingFunction.InOutQuad))
        .WithFrame(new(position3d: new Vector3(0, 50, GameSceneRenderer.MIN_Z / 2), duration: TimeSpan.FromSeconds(10), easing: EasingFunction.InOutQuad))
        .WithFrame(new(position3d: new Vector3(GameSceneRenderer.MAX_X / 2, 50, 0)));
                        
                        MainMenu.CameraPositionAnimator.Restart();
                        MainMenu.CameraPositionAnimator.Run();
                        MainMenu.CameraPositionAnimator.IsLooped = true;
                    }*/
                    //GameProjection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(90), GraphicsDevice.Viewport.AspectRatio, 0.1f, 100000);
                }
                if (Difficulties.Types["POV"]) {
                    if (GameHandler.AllPlayerTanks[NetPlay.GetMyClientId()] is not null && !GameHandler.AllPlayerTanks[NetPlay.GetMyClientId()].Dead) {
                        SpectatorId = NetPlay.GetMyClientId();
                        POVCameraPosition = GameHandler.AllPlayerTanks[NetPlay.GetMyClientId()].Position.ExpandZ();
                        POVCameraRotation = -GameHandler.AllPlayerTanks[NetPlay.GetMyClientId()].TurretRotation;
                    }
                    else if (GameHandler.AllPlayerTanks[SpectatorId] is not null) {

                        if (InputUtils.KeyJustPressed(Keys.Left))
                            SpectatorId = SpectateValidTank(SpectatorId, false);
                        else if (InputUtils.KeyJustPressed(Keys.Right))
                            SpectatorId = SpectateValidTank(SpectatorId, true);

                        POVCameraPosition = GameHandler.AllPlayerTanks[SpectatorId].Position.ExpandZ();
                        POVCameraRotation = -GameHandler.AllPlayerTanks[SpectatorId].TurretRotation;
                    }


                    // pov...

                    if (IntermissionHandler.ThirdPersonTransitionAnimation != null && PlayerTank.ClientTank is not null) {
                        IntermissionHandler.ThirdPersonTransitionAnimation.KeyFrames[1]
                            = new(position2d: new Vector2(-PlayerTank.ClientTank.TurretRotation), position3d: PlayerTank.ClientTank.Position3D);
                    }
                    // TODO: this shit is ass.
                    var povCameraRotationCurrent = IntermissionHandler.TankFunctionWait > 0 && IntermissionHandler.ThirdPersonTransitionAnimation != null ?
                        IntermissionHandler.ThirdPersonTransitionAnimation.CurrentPosition2D.X : POVCameraRotation;
                    var povCameraPosCurrent = IntermissionHandler.TankFunctionWait > 0 && IntermissionHandler.ThirdPersonTransitionAnimation != null ?
                        IntermissionHandler.ThirdPersonTransitionAnimation.CurrentPosition3D : POVCameraPosition;

                    GameView = Matrix.CreateLookAt(povCameraPosCurrent,
                            POVCameraPosition + new Vector2(0, 20).Rotate(povCameraRotationCurrent).ExpandZ(),
                            Vector3.Up) * Matrix.CreateScale(AddativeZoom) *
                        Matrix.CreateTranslation(0, -20, 0);

                    /*GameView = Matrix.CreateLookAt(POVCameraPosition,
                            POVCameraPosition + new Vector3(0, 0, 20).FlattenZ().RotatedByRadians(POVCameraRotation).ExpandZ(),
                            Vector3.Up) * Matrix.CreateScale(AddativeZoom) *
                        Matrix.CreateRotationX(POVRotationVector.Y - MathHelper.PiOver4) *
                        Matrix.CreateRotationY(POVRotationVector.X) *
                        Matrix.CreateTranslation(0, -20, 0);*/

                    GameProjection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(90), GraphicsDevice.Viewport.AspectRatio, 0.1f, 10000);
                }
            }
            else if (!GameUI.Paused && !MainMenu.Active && DebugManager.DebuggingEnabled) {
                if (DebugManager.DebugLevel == DebugManager.Id.FreeCamTest || DebugManager.persistFreecam) {

                    if (InputUtils.AreKeysJustPressed(Keys.Z, Keys.X)) {
                        DebugManager.persistFreecam = !DebugManager.persistFreecam;
                    }
                    // free camera movement test

                    var moveSpeed = 10f * DeltaTime;

                    var rotationSpeed = 0.01f;

                    RebirthFreecam.NearViewDistance = 0.1f;
                    RebirthFreecam.FarViewDistance = 1000000f;
                    RebirthFreecam.MinPitch = -180;
                    RebirthFreecam.MaxPitch = 180;
                    RebirthFreecam.HasLookAt = false;

                    var isPlayerActive = PlayerTank.ClientTank is not null;

                    var keysprint = LevelEditor.Active || !isPlayerActive ? Keys.LeftShift : Keys.RightShift;

                    if (InputUtils.CurrentKeySnapshot.IsKeyDown(keysprint))
                        moveSpeed *= 2;

                    var keyf = LevelEditor.Active || !isPlayerActive ? Keys.W : Keys.Up;
                    var keyb = LevelEditor.Active || !isPlayerActive ? Keys.S : Keys.Down;
                    var keyl = LevelEditor.Active || !isPlayerActive ? Keys.A : Keys.Left;
                    var keyr = LevelEditor.Active || !isPlayerActive ? Keys.D : Keys.Right;

                    if (InputUtils.MouseRight)
                        RebirthFreecam.Rotation -= new Vector3(0, MouseVelocity.Y * rotationSpeed, MouseVelocity.X * rotationSpeed);
                    if (InputUtils.CurrentKeySnapshot.IsKeyDown(Keys.Add))
                        RebirthFreecam.FieldOfView += 0.5f * DeltaTime;
                    if (InputUtils.CurrentKeySnapshot.IsKeyDown(Keys.Subtract))
                        RebirthFreecam.FieldOfView -= 0.5f * DeltaTime;
                    if (InputUtils.MouseMiddle)
                        RebirthFreecam.FieldOfView = 90;
                    if (InputUtils.CurrentKeySnapshot.IsKeyDown(keyf))
                        RebirthFreecam.Move(RebirthFreecam.World.Forward * moveSpeed);
                    if (InputUtils.CurrentKeySnapshot.IsKeyDown(keyb))
                        RebirthFreecam.Move(RebirthFreecam.World.Backward * moveSpeed);
                    if (InputUtils.CurrentKeySnapshot.IsKeyDown(keyl))
                        RebirthFreecam.Move(RebirthFreecam.World.Left * moveSpeed);
                    if (InputUtils.CurrentKeySnapshot.IsKeyDown(keyr))
                        RebirthFreecam.Move(RebirthFreecam.World.Right * moveSpeed);

                    GameView = RebirthFreecam.View;
                    GameProjection = RebirthFreecam.Projection;
                }
            }
            if (DebugManager.DebuggingEnabled) {
                if (DebugManager.DebugLevel != DebugManager.Id.FreeCamTest) {
                    if (InputUtils.MouseRight)
                        OrthoRotationVector += MouseVelocity / 500f;

                    if (InputUtils.CurrentKeySnapshot.IsKeyDown(Keys.Add))
                        AddativeZoom += 0.01f;
                    if (InputUtils.CurrentKeySnapshot.IsKeyDown(Keys.Subtract))
                        AddativeZoom -= 0.01f;

                    if (InputUtils.MouseMiddle)
                        CameraFocusOffset += MouseVelocity;
                }
            }
        }

        DoUpdate2(gameTime);

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
        //Console.WriteLine($"{MouseUtils.MousePosition} - {_mOld}");
        _mOld = MouseUtils.MousePosition;
    }

    private void DoUpdate2(GameTime gameTime) {
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
                // TODO: why is this here and not LevelEditor
                if (!MainMenu.Active && (OverheadView || LevelEditor.Active)) {
                    foreach (var tnk in GameHandler.AllTanks) {
                        if (tnk == null) continue;

                        if (tnk.Dead)
                            continue;

                        if (RayUtils.GetMouseToWorldRay().Intersects(tnk.Worldbox).HasValue) {
                            HoveringAnyTank = true;
                            if (InputUtils.KeyJustPressed(Keys.K) && Array.IndexOf(GameHandler.AllTanks, tnk) > -1)
                                tnk?.Destroy(new TankHurtContextOther()); // hmmm

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
        foreach (var bind in Keybind.AllKeybinds)
            bind?.Update();
    }

    public static Color ClearColor = Color.Black;

    public static bool RenderWireframe = false;

    public static RasterizerState _cachedState;

    public static RasterizerState DefaultRasterizer => RenderWireframe ? new() { FillMode = FillMode.WireFrame } : RasterizerState.CullNone;

    static RenderTarget2D GameFrameBuffer;
    public static RenderTarget2D GameTarget => GameFrameBuffer;

    public static event Action<GameTime> OnPostDraw;

    public static void SaveRenderTarget(string path = "screenshot.png") {
        using var fs = new FileStream(path, FileMode.OpenOrCreate);
        GameTarget.SaveAsPng(fs, GameTarget.Width, GameTarget.Height);
        ChatSystem.SendMessage("Saved image to " + fs.Name, Color.Lime);
    }

    public static bool SuperSecretDevOption;

    private static DepthStencilState _stencilState = DepthStencilState.Default;

    public static SamplerState WrappingSampler = new() {
        AddressU = TextureAddressMode.Wrap,
        AddressV = TextureAddressMode.Wrap,
    };
    public static SamplerState ClampingSampler = new() {
        AddressU = TextureAddressMode.Clamp,
        AddressV = TextureAddressMode.Clamp,
    };
    // FIXME: this method is a clusterfuck
    protected override void Draw(GameTime gameTime) {
        if (GameFrameBuffer == null || GameFrameBuffer.IsDisposed || GameFrameBuffer.Size() != WindowUtils.WindowBounds) {
            GameFrameBuffer?.Dispose();
            var presentationParams = GraphicsDevice.PresentationParameters;
            GameFrameBuffer = new RenderTarget2D(GraphicsDevice, presentationParams.BackBufferWidth, presentationParams.BackBufferHeight, false, presentationParams.BackBufferFormat, presentationParams.DepthStencilFormat, 0, RenderTargetUsage.PreserveContents);
        }
        // switch to RT, begin SB, do drawing, end SB, SetRenderTarget(null), begin SB again, draw RT, end SB
        GraphicsDevice.SetRenderTarget(GameFrameBuffer);
        GraphicsDevice.Clear(ClearColor);
        // TankFootprint.DecalHandler.UpdateRenderTarget();
        SpriteRenderer.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, rasterizerState: DefaultRasterizer);
        GraphicsDevice.DepthStencilState = _stencilState;
        GraphicsDevice.SamplerStates[0] = WrappingSampler;
        RoomSceneRenderer.Render();
        GraphicsDevice.SamplerStates[0] = ClampingSampler;
        GameHandler.RenderAll();
        SpriteRenderer.End();

        GraphicsDevice.SetRenderTarget(null);
        var shader = Difficulties.Types["LanternMode"] && !MainMenu.Active ? GameShaders.LanternShader : (MainMenu.Active ? GameShaders.GaussianBlurShader : null);
        if (!GameSceneRenderer.ShouldRenderAll) shader = null;
        SpriteRenderer.Begin(effect: shader);
        SpriteRenderer.Draw(GameFrameBuffer, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, Vector2.One, default, 0f);
        SpriteRenderer.End();

        // holy balls this sucks.
        GraphicsDevice.DepthStencilState = _stencilState;
        MainMenu.RenderModels();

        SpriteRenderer.Begin();
        if (MainMenu.Active) MainMenu.Render();
        // i really wish i didn't have to draw this here.
        VanillaAchievementPopupHandler.DrawPopup(SpriteRenderer);
        if (Debugger.IsAttached) SpriteRenderer.DrawString(TextFont, "DEBUGGER ATTACHED", new Vector2(10, 50), Color.Red, new Vector2(0.8f));
        DebugManager.DrawDebug(SpriteRenderer);
        DebugManager.DrawDebugMetrics();
        Speedrun.DrawSpeedrunHUD(SpriteRenderer);
        SpriteRenderer.End();

        ChatSystem.DrawMessages();

        SpriteRenderer.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, rasterizerState: DefaultRasterizer);
        if (LevelEditor.Active) LevelEditor.Render();
        if (CampaignCompleteUI.IsViewingResults) CampaignCompleteUI.Render();
        SpriteRenderer.End();

        // this method begins the spritebatch, since it's supposed to have its own
        IntermissionSystem.Draw(SpriteRenderer);

        SpriteRenderer.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, rasterizerState: DefaultRasterizer);
        GameHandler.RenderUI();
        IntermissionSystem.DrawBlack(SpriteRenderer);
        if (IsCrashInfoVisible) DrawErrorScreen();
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
        SpriteRenderer.DrawString(TextFontLarge,
            "Your game ran into a problem and might need to restart. We're just" +
            "\nshowing you what's wrong, and how it might affect your game.",
            new Vector2(100, 250).ToResolution(),
            Color.White,
            (Vector2.One * 0.4f).ToResolution());
        SpriteRenderer.DrawString(TextFontLarge, CrashInfo.Reason, new Vector2(100, 500).ToResolution(), Color.White, (Vector2.One * 0.3f).ToResolution());
        SpriteRenderer.DrawString(TextFontLarge, CrashInfo.Description, new Vector2(100, 550).ToResolution(), Color.White, (Vector2.One * 0.2f).ToResolution());

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

    private static Particle _secret1_1;
    private static Particle _secret1_2;

    private static Particle _secret2_1;
    private static Particle _secret2_2;

    private static void PlaceSecrets() {
        // magic.
        const float SECRET_BASE_POS_X = GameSceneRenderer.MIN_X - 28.5f;
        const float SECRET_BASE_POS_Y = 22;
        const float SECRET_BASE_POS_Z = 20;
        _secret1_1 = GameHandler.Particles.MakeParticle(new Vector3(100, 0.1f, 0), GameResources.GetGameResource<Texture2D>("Assets/textures/secret/special"));
        _secret1_1.UniqueBehavior = (p) => {
            _secret1_1.Position = new Vector3(SECRET_BASE_POS_X, SECRET_BASE_POS_Y, SECRET_BASE_POS_Z);
            _secret1_1.Roll = MathHelper.Pi;
            _secret1_1.Pitch = MathHelper.PiOver2;
            _secret1_1.Scale = Vector3.One * 0.3f;
            _secret1_1.HasAddativeBlending = false;
        };
        _secret1_2 = GameHandler.Particles.MakeParticle(new Vector3(100, 0.1f, 0), "Litzy <3");
        _secret1_2.UniqueBehavior = (p) => {
            _secret1_2.Position = new Vector3(SECRET_BASE_POS_X, SECRET_BASE_POS_Y + 20, SECRET_BASE_POS_Z - 8);
            _secret1_2.Roll = MathHelper.Pi;
            _secret1_2.Pitch = -MathHelper.PiOver2;
            _secret1_2.Scale = Vector3.One * 0.3f;
            _secret1_2.HasAddativeBlending = false;
        };

        _secret2_1 = GameHandler.Particles.MakeParticle(new Vector3(100, 0.1f, 0), GameResources.GetGameResource<Texture2D>("Assets/textures/secret/special2"));
        _secret2_1.UniqueBehavior = (p) => {
            _secret2_1.Position = new Vector3(SECRET_BASE_POS_X, SECRET_BASE_POS_Y, SECRET_BASE_POS_Z - 40);
            _secret2_1.Roll = MathHelper.Pi;
            _secret2_1.Pitch = MathHelper.PiOver2;
            _secret2_1.Scale = Vector3.One * 0.3f;
            _secret2_1.HasAddativeBlending = false;
        };
        _secret2_2 = GameHandler.Particles.MakeParticle(new Vector3(100, 0.1f, 0), "Ziggy <3");
        _secret2_2.UniqueBehavior = (p) => {
            _secret2_2.Position = new Vector3(SECRET_BASE_POS_X, SECRET_BASE_POS_Y + 20, SECRET_BASE_POS_Z - 8 - 40);
            _secret2_2.Roll = MathHelper.Pi;
            _secret2_2.Pitch = -MathHelper.PiOver2;
            _secret2_2.Scale = Vector3.One * 0.3f;
            _secret2_2.HasAddativeBlending = false;
        };
    }
}