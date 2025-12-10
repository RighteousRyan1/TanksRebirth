using System;
using System.IO;
using System.Text.Json;
using System.Reflection;
using System.Globalization;
using System.Collections.Generic;
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
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Framework;
using TanksRebirth.GameContent;
using TanksRebirth.GameContent.UI;
using TanksRebirth.GameContent.ModSupport;
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
using TanksRebirth.GameContent.Cosmetics;
using TanksRebirth.GameContent.UI.MainMenu;
using TanksRebirth.GameContent.UI.LevelEditor;
using TanksRebirth.GameContent.Systems.ParticleSystem;
using TanksRebirth.GameContent.Systems.TankSystem;
using System.Collections.Concurrent;
using TanksRebirth.GameContent.Systems.AI;
using TanksRebirth.GameContent.ID;

namespace TanksRebirth;

#pragma warning disable CS8618, CA2211
public class TankGame : Game {
    // ### STRINGS ###
    public string MOTD { get; private set; }
    public static string GameDirectory { get; private set; }
    public static readonly string ExePath = Assembly.GetExecutingAssembly().Location.Replace(@$"\{nameof(TanksRebirth)}.dll", string.Empty);
    public static readonly string SaveDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Tanks Rebirth");

    // ### BOOLEANS ###

    public static bool IsCrashInfoVisible;
    bool _wasActive;
    public static bool MouseUIHover;

    // ### STRUCTURES / CLASSES ###

    /// <summary>The JSON manager/serializer/deserializer for the game's settings.</summary>
    public static JsonHandler<GameConfig> SettingsHandler;
    public static GameConfig Settings;

    Vector2 _mouseOld;

    public static TankGame Instance { get; private set; }
    /// <summary>The user's save data.</summary>
    public static GameData SaveFile { get; private set; } = new();
    /// <summary>The game time for the previous logic loop.</summary>
    public static GameTime LastGameTime { get; private set; }
    /// <summary>The handle of the game's logging file. Used to write information to a file that can be read after the game closes.</summary>
    public static Logger ClientLog { get; private set; }

    public static Language GameLanguage = new();

    /// <summary>How long the process/game has been open.</summary>
    public static Stopwatch CurrentSessionTimer = new();

    /// <summary>Counts the average FPS over the entire lifespan of the process/game.</summary>
    public static readonly FpsTracker FPSTracker = new();
    public readonly GraphicsDeviceManager Graphics;

    public static OrthographicCamera OrthographicCamera;
    public static SpectatorCamera SpectatorCamera;
    public static PerspectiveCamera PerspectiveCamera;
    /// <summary>The index/vertex buffer used to render to a framebuffer.</summary>
    public static SpriteBatch SpriteRenderer;

    public static AutoUpdater AutoUpdater;
    public static AchievementPopupHandler VanillaAchievementPopupHandler;

    public static RenderTarget2D GameFrameBuffer;

    public static RasterizerState _cachedState;

    public static Dictionary<int, RebirthMouse> PlayerMice = [];

    // ### EVENTS ###

    public static event EventHandler<IntPtr> OnFocusLost;
    public static event EventHandler<IntPtr> OnFocusRegained;

    public static event Action<GameTime> PreDrawBackBuffer;

    public delegate void OnResolutionChangedDelegate(int newX, int newY);
    public static event OnResolutionChangedDelegate OnResolutionChanged;

    public static GameConsole IngameConsole;

    /// <summary>A queue of actions to be taken on the main thread.
    /// <br></br>This is particularly useful for performing things on the main thread when you are processing on other threads.</summary>
    public static ConcurrentQueue<Action> MainThreadTasks = [];

    public TankGame() : base() {
        // prepare IO
        Directory.CreateDirectory(SaveDirectory);
        Directory.CreateDirectory(Path.Combine(SaveDirectory, "Resource Packs", "Scene"));
        Directory.CreateDirectory(Path.Combine(SaveDirectory, "Resource Packs", "Tank"));
        Directory.CreateDirectory(Path.Combine(SaveDirectory, "Resource Packs", "Music"));
        Directory.CreateDirectory(Path.Combine(SaveDirectory, "Logs"));
        Directory.CreateDirectory(Path.Combine(SaveDirectory, "Backup"));
        ClientLog = new(Path.Combine(SaveDirectory, "Logs"), "tanks_rebirth_client");
        IngameConsole = new GameConsole(this);
        ClientLog.OnLogWrite += WriteToIngameConsole;

        // logging speaks for itself
        Task.Run(() => {
            try {
                ClientLog.Write(
                    "Obtaining message of the day (MOTD) from GitHub...",
                    LogType.Info);
                var bytes = WebUtils.DownloadWebFile(
                    "https://raw.githubusercontent.com/RighteousRyan1/tanks_rebirth_motds/master/motd.txt",
                    out var name, out var status);
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
            RuntimeData.OS = OSPlatform.Windows;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            RuntimeData.OS = OSPlatform.OSX;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
            RuntimeData.OS = OSPlatform.Linux;
        }
        
        ClientLog.Write($"Playing on Operating System '{RuntimeData.OS}'", LogType.Info);

        // IOUtils.SetAssociation(".mission", "MISSION_FILE", "TanksRebirth.exe", "Tanks Rebirth mission file");

        Graphics = new(this) {
            PreferHalfPixelOffset = true,
            HardwareModeSwitch = false,
            IsFullScreen = false,
        };

        Content.RootDirectory = "Content";
        Instance = this;
        Window.AllowUserResizing = true;

        IsMouseVisible = false;

        FontGlobals.RebirthFontSystem = new();

        RuntimeData.GameVersion = typeof(TankGame).Assembly.GetName().Version!;
        RuntimeData.ShortVersion = StringUtils.RemoveTrailingZeros(RuntimeData.GameVersion);

        ClientLog.Write(
            $"Running {typeof(TankGame).Assembly.GetName().Name} on version '{RuntimeData.GameVersion}'",
            LogType.Info);
    }

    private void WriteToIngameConsole(string data, LogType logType) {
        var color = logType switch {
            LogType.Info => Color.Orange,
            LogType.Warn => Color.IndianRed,
            LogType.ErrorFatal => Color.Red,
            LogType.ErrorSilent => Color.OrangeRed,
            LogType.Debug => Color.MediumPurple,
            _ => Color.Green
        };
        IngameConsole?.Log($"[{logType}] " + data, color);
    }

    protected override void Initialize() {
        try {
            if (File.Exists(Path.Combine(SaveFile.Directory, SaveFile.Name)))
                SaveFile.Deserialize();

            ClientLog.Write("Save file loaded.", LogType.Info);

            GameHandler.Initialize();
            GameDirectory = Directory.GetCurrentDirectory();
            CameraGlobals.Initialize(GraphicsDevice);
            if (Debugger.IsAttached && SteamAPI.IsSteamRunning()) {
                ClientLog.Write("Initialising SteamWorks API...", LogType.Debug);
                SteamworksUtils.Initialize();
            }
            Window.Title = "Tanks! Rebirth";

            CurrentSessionTimer.Start();
            PingMenu.Initialize();

            GameHandler.MapEvents();
            ClientLog.Write($"Mapped events...", LogType.Info);

            DiscordRichPresence.Load();
            ClientLog.Write($"Loaded Discord Rich Presence...", LogType.Info);

            // systems = ReflectionUtils.GetInheritedTypesOf<IGameSystem>(Assembly.GetExecutingAssembly());

            SpriteRenderer = new(GraphicsDevice);

            Graphics.PreferMultiSampling = true;
            Graphics.ApplyChanges();

            ClientLog.Write($"Applying changes to graphics device... ({Graphics.PreferredBackBufferWidth}x{Graphics.PreferredBackBufferHeight})", LogType.Info);
            ClientLog.Write($"Loaded save data.", LogType.Info);

            VanillaAchievements.InitializeToRepository();
            ClientLog.Write("Loaded achievements.", LogType.Info);
            IntermissionSystem.InitializeAllStartupLogic();
            TextureGlobals.Populate();

            VanillaAchievementPopupHandler = new(VanillaAchievements.Repository);

            AIManager.AIThread1.Start();
            AIManager.AIThread2.Start();
            AIManager.AIThread3.Start();
            ClientLog.Write("Tank AI Threads started.", LogType.Info);

            // add the main player in when loading
            PlayerMice.Add(PlayerID.Blue, new RebirthMouse(PlayerID.PlayerTankColors[PlayerID.Blue], PlayerID.PlayerTankColorsBright[PlayerID.Blue], PlayerID.Blue));

            Client.OnClientStart += UpdateMainClientMouse;

            InputUtils.OnGamePadConnected += InputUtils_OnGamePadConnected;
            InputUtils.OnGamePadDisconnected += InputUtils_OnGamePadDisconnected;

            base.Initialize();
        }
        catch (Exception e) when (!Debugger.IsAttached) {
            ReportError(e);
        }
    }

    private void InputUtils_OnGamePadDisconnected(int player) {
        ClientLog.Write($"Gamepad disconnected from player {player}.", LogType.Info);
        PlayerMice.Remove(player + 1);
    }

    private void InputUtils_OnGamePadConnected(int player) {
        ClientLog.Write($"Gamepad connected, controlling player {player}.", LogType.Info);
        PlayerMice.Add(player + 1, new RebirthMouse(PlayerID.PlayerTankColors[player + 1], PlayerID.PlayerTankColorsBright[player + 1], player + 1));
        PlayerMice[player + 1].Position = MouseUtils.MousePosition + Vector2.UnitX * 100 * (player + 1);
    }

    private void UpdateMainClientMouse(Client client) {
        // update only the current client's mouse.
        // other mice won't exist in online multiplayer contexts, but will exist in local multiplayer.
        PlayerMice[0].MouseColor = PlayerID.PlayerTankColorsBright[client.Id];
    }

    protected override void OnExiting(object sender, ExitingEventArgs args) {
        ClientLog.Write($"Handling termination process...", LogType.Info);

        // update game-related numbers
        SaveFile.TimePlayed += CurrentSessionTimer.Elapsed;

        // save everything related to game-data
        SettingsHandler = new(Settings, Path.Combine(SaveDirectory, "settings.json"));
        JsonSerializerOptions opts = new() { WriteIndented = true };
        SettingsHandler.Serialize(opts, true);

        SaveFile.ExpLevel = GameHandler.ExpBar.Level + GameHandler.ExpBar.Value;
        SaveFile.Serialize();

        WiimoteSystem.TryDisconnect();

        DiscordRichPresence.Terminate();

        CurrentSessionTimer.Stop();

        AIManager.RunThreads = false;
        AIManager.AIThread1.Join();

        // write end-life metrics

        ClientLog.Write($"Average overall FPS: {FPSTracker.AverageFPS}", LogType.Info);
        ClientLog.Write($"Session time: {CurrentSessionTimer.Elapsed.StringFormat()}", LogType.Info);

        ClientLog.Dispose();
    }
    private static void PreloadContent() {
        // TODO: is it emportant that these paths are all hardcoded? i'm doubtful.
        // do more dynamically..?

        string[] textures = [
            // Misc
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

            "Assets/textures/secret/ziggy",
            "Assets/textures/secret/bk_cypher",
            
            
            // Tank Textures
            "Assets/textures/tank_shadow",
            "Assets/textures/bullet/bullet",
            "Assets/textures/bullet/flame",
            "Assets/textures/bullet/smoketrail",
            "Assets/textures/bullet/explosive_bullet",
            "Assets/textures/misc/armor",

            "Assets/textures/deathmark/deathmark_blue",
            "Assets/textures/deathmark/deathmark_red",
            "Assets/textures/deathmark/deathmark_green",
            "Assets/textures/deathmark/deathmark_yellow",
            "Assets/textures/deathmark/deathmark_white",

            "Assets/textures/tank_footprint",
            "Assets/textures/tank_footprint_alt",

            "Assets/textures/mine/mine_env",
            "Assets/textures/mine/mine_shadow",
            "Assets/textures/mine/explosion",

            // UI
            "Assets/textures/ui/bullet_ui",
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
            "Assets/textures/ui/tnk_bonus_star",
            "Assets/textures/ui/tnk_bonus_base",
            "Assets/textures/ui/banner",
            "Assets/textures/ui/grades",
            "Assets/textures/ui/scoreboard_inner",
            "Assets/textures/ui/scoreboard_outer",
            "Assets/textures/ui/tank2d",
            "Assets/textures/ui/trophy",
            "Assets/textures/ui/achievement/secret",
        ];

        /*var parent = Directory.GetFiles("Content\\Assets");
        var subDirs = Directory.GetDirectories("Content\\Assets", "*", SearchOption.AllDirectories);

        for (int i = 0; i < subDirs.Length; i++) {
            var subDir = subDirs[i];

            var files = Directory.GetFiles(subDir).Where(x => x.EndsWith(".png")).ToArray();

            for (int j = 0; j < files.Length; j++) {
                var file = files[j];

                textures.Add(Path.GetFileNameWithoutExtension(file));
            }
        }*/
        
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
        try {
            PreloadContent();
            var s = Stopwatch.StartNew();

            RuntimeData.MainThreadId = Environment.CurrentManagedThreadId;

            OrthographicCamera = new(0, 0, 1920, 1080, -2000, 5000);
            SpectatorCamera = new(MathHelper.ToRadians(100), GraphicsDevice.Viewport.AspectRatio, 0.1f, 5000f);
            PerspectiveCamera = new(MathHelper.ToRadians(90), GraphicsDevice.Viewport.AspectRatio, 0.1f, 5000f);

            Task.Run(() => {
                RuntimeData.CompSpecs = ComputerSpecs.GetSpecs(out bool error);

                if (error) {
                    ClientLog.Write(
                        "Unable to load computer specs: Error.",
                        LogType.Warn);
                }
                else {
                    ClientLog.Write($"CPU: {RuntimeData.CompSpecs.CPU}", LogType.Info);
                    ClientLog.Write($"GPU: {RuntimeData.CompSpecs.GPU}", LogType.Info);
                    ClientLog.Write($"Physical Memory (RAM): {RuntimeData.CompSpecs.RAM}", LogType.Info);
                }

                if (!RuntimeData.CompSpecs.Equals(default) && !error) {
                    var profiler = new SpecAnalysis(RuntimeData.CompSpecs.GPU, RuntimeData.CompSpecs.CPU, RuntimeData.CompSpecs.RAM);

                    profiler.Analyze(false, out var ramr, out var gpur, out var cpur);

                    //ChatSystem.SendMessage(ramr, Color.White);
                    //ChatSystem.SendMessage(gpur, Color.White);
                    //ChatSystem.SendMessage(cpur, Color.White);

                    // ChatSystem.SendMessage(profiler.ToString(), Color.Brown);

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

            TextureGlobals.CreateDynamicTexturesAsync(GraphicsDevice);

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
       
            // english is loaded so fallback characters work.
            FontGlobals.LoadFont(LangCode.English);
            FontGlobals.LoadFont(Settings.Language);

            FontGlobals.RebirthFont = FontGlobals.RebirthFontSystem.GetFont(35);
            FontGlobals.RebirthFontLarge = FontGlobals.RebirthFontSystem.GetFont(120);
            ClientLog.Write($"Loaded fonts.", LogType.Info);

            RuntimeData.LaunchTime = DateTime.Now;
            RuntimeData.IsSouthernHemi = RegionUtils.IsSouthernHemisphere(RegionInfo.CurrentRegion.EnglishName);

            if (RuntimeData.IsSouthernHemi)
                ClientLog.Write("User is in the southern hemisphere.", LogType.Info);
            else
                ClientLog.Write("User is in the northern hemisphere.", LogType.Info);

            ClientLog.Write($"Loaded user settings.", LogType.Info);

            #region Config Initialization

            Graphics.SynchronizeWithVerticalRetrace = Settings.Vsync;
            WindowUtils.ChangeWindowKind(Settings.WindowKind);
            PlayerTank.MoveUp.ForceReassign(Settings.UpKeybind);
            PlayerTank.MoveDown.ForceReassign(Settings.DownKeybind);
            PlayerTank.MoveLeft.ForceReassign(Settings.LeftKeybind);
            PlayerTank.MoveRight.ForceReassign(Settings.RightKeybind);
            PlayerTank.PlaceMine.ForceReassign(Settings.MineKeybind);
            GameScene.Theme = Settings.GameTheme;

            /*if (!IsSouthernHemi ? LaunchTime.Month != 12 : LaunchTime.Month != 7)
                MapRenderer.Theme = Settings.GameTheme;
            else
                MapRenderer.Theme = MapTheme.Christmas;*/

            // GameScene.Theme = MapTheme.Christmas;

            TankFootprint.ShouldTracksFade = Settings.FadeFootprints;

            Graphics.PreferredBackBufferWidth = Settings.ResWidth;
            Graphics.PreferredBackBufferHeight = Settings.ResHeight;

            ClientLog.Write($"Applied user settings.", LogType.Info);

            Tank.SetAssetNames();
            TankMusicSystem.SetAssetAssociations();
            GameScene.LoadTexturePack(Settings.MapPack);
            TankMusicSystem.LoadSoundPack(Settings.MusicPack);
            Tank.LoadTexturePack(Settings.TankPack);
            Graphics.ApplyChanges();

            Language.LoadLang(Settings.Language, out GameLanguage);
            // Language.GenerateLocalizationTemplate("en_US.loc");

            Achievement.MysteryTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/achievement/secret");

            GameResources.EnsurePreloadedAssetsArePreloaded();
            GameHandler.SetupGraphics();
            GameUI.Initialize();
            MainMenuUI.InitializeUI();
            MainMenuUI.InitializeBasics();

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
            MainMenuUI.MenuState = MainMenuUI.UIState.PrimaryMenu;

            MainMenuUI.Open();
            ModLoader.LoadMods();

            if (ModLoader.IsLoadingMods) {
                MainMenuUI.MenuState = MainMenuUI.UIState.LoadingMods;
                Task.Run(async () => {
                    while (ModLoader.IsLoadingMods)
                        await Task.Delay(RuntimeData.LogicTime).ConfigureAwait(false);
                    MainMenuUI.MenuState = MainMenuUI.UIState.PrimaryMenu;
                });
            }

            ClientLog.Write("Running in directory: " + GameDirectory, LogType.Info);

            ClientLog.Write($"Content loaded in {s.Elapsed}.", LogType.Debug);
            ClientLog.Write($"DebugMode: {Debugger.IsAttached}", LogType.Debug);

            if (SteamworksUtils.IsInitialized) {
                ClientLog.Write($"Steam Active, username: {SteamworksUtils.MyUsername} with {SteamworksUtils.FriendsCount} friends.", LogType.Info);
            }

            s.Stop();

            // it isnt really an autoupdater tho.
            Task.Run(() => {
                AutoUpdater = new("https://github.com/RighteousRyan1/TanksRebirth", RuntimeData.GameVersion);

                if (!AutoUpdater.IsOutdated) {
                    ClientLog.Write("Game is up to date.", LogType.Info);
                    return;
                }

                ClientLog.Write($"Game is out of date (current={RuntimeData.GameVersion}, recent={AutoUpdater.GetRecentVersion()}).", LogType.Warn);
                //CommandGlobals.IsUpdatePending = true;
                ChatSystem.SendMessage($"Outdated game version detected (current={RuntimeData.GameVersion}, recent={AutoUpdater.GetRecentVersion()}).", Color.Red);
                //ChatSystem.SendMessage("Type /update to update the game and automatically restart.", Color.Red);
                SoundPlayer.SoundError();
            });
            PlaceSecrets();

            SceneManager.GameLight.Apply(false);
        }
        catch (Exception e) when (!Debugger.IsAttached) {
            ReportError(e);
        }
    }
    // FIXME: this method is a clusterfuck
    protected override void Update(GameTime gameTime) {
        try {
            MouseUIHover = false;

            if (GameUI.Paused)
                MouseUIHover = true;
            IngameConsole.Update(gameTime);
            /*if (Debugger.IsAttached) {
                SteamworksUtils.SetSteamStatus("balls", "inspector");
                SteamFriends.GetFriendGamePlayed(SteamFriends.GetFriendByIndex(0, EFriendFlags.k_EFriendFlagAll), out var x);

            }*/
            /*if (InputUtils.KeyJustPressed(Keys.F11)) {
                for (int i = 0; i < TankID.Collection.Count; i++) {
                    var parameters = AIManager.GetAIParameters(i);
                    var properties = AIManager.GetAITankProperties(i);

                    var json = JsonSerializer.Serialize(
                        new {
                            Parameters = parameters,
                            Properties = properties
                        },
                        new JsonSerializerOptions { WriteIndented = true, IncludeFields = true });

                    Directory.CreateDirectory("ai_params");
                    File.WriteAllText("ai_params/tank_" + TankID.Collection.GetKey(i) + ".json", json);
                }
            }*/

            if (InputUtils.AreKeysJustPressed(Keys.M, Keys.O, Keys.T, Keys.E)) {
                if (WiimoteSystem.IsConnected) {
                    bool disconnected = WiimoteSystem.TryDisconnect();

                    if (disconnected)
                        ChatSystem.SendMessage("Wiimote disconnected.", Color.Lime);
                    else
                        ChatSystem.SendMessage("Wiimote cannot disconnect.", Color.Red);
                }
                else {
                    bool connected = WiimoteSystem.TryConnect();

                    if (connected)
                        ChatSystem.SendMessage("Wiimote connected.", Color.Lime);
                    else
                        ChatSystem.SendMessage("Wiimote cannot connect.", Color.Red);
                }
            }
            HandleLogic(gameTime);

            if (MainThreadTasks.TryDequeue(out var action))
                action.Invoke();

        } catch (Exception e) when (!Debugger.IsAttached) {
            ReportError(e, false, false);

            MainMenuUI.Theme.Volume = 0f;
            TankMusicSystem.PauseAll();

            SoundPlayer.SoundError();

            if (LevelEditorUI.IsActive && LevelEditorUI.loadedCampaign != null) {
                Campaign.Save(Path.Combine(SaveDirectory, "Backup", $"backup_{DateTime.Now.StringFormatCustom("_")}"), LevelEditorUI.loadedCampaign!);
            }

            IsCrashInfoVisible = true;
            RuntimeData.CrashInfo = new(e.Message, e.StackTrace ?? "No stack trace available.", e);
        }
    }
    private void HandleLogic(GameTime gameTime) {
        MouseUtils.MousePosition = new(InputUtils.KeyboardMouse.CurrentMouse.X, InputUtils.KeyboardMouse.CurrentMouse.Y);
        MouseUtils.MouseVelocity = MouseUtils.MousePosition - _mouseOld;

        PlayerMice[0].Position = MouseUtils.MousePosition;

        /*if ()
        // since "connected inputs" also tracks the keyboard.
        for (int i = 1; i < PlayerMice.Count; i++) {
            var gpCur = InputUtils.GamePads[i - 1];
            var lStick = gpCur.Current.ThumbSticks.Left;

            PlayerTank.AimTargets[i] += new Vector2(lStick.X, -lStick.Y) * 10;
            ChatSystem.SendMessage(lStick);

            PlayerMice[i].Position = PlayerTank.AimTargets[i];
        }*/

        #region Non-Camera

        TargetElapsedTime = TimeSpan.FromMilliseconds(RuntimeData.Interp ? 16.67 * (60f / Settings.TargetFPS) : 16.67);

        if (!float.IsInfinity(RuntimeData.DeltaTime))
            RuntimeData.RunTime += RuntimeData.DeltaTime;

        if (!IsCrashInfoVisible) {
            if (SteamworksUtils.IsInitialized)
                SteamworksUtils.Update();

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

            foreach (var elem in PlayerMice) {
                elem.Value.ShouldRender = !Modifiers.Map[Modifiers.POV] || GameUI.Paused || MainMenuUI.IsActive || LevelEditorUI.IsActive;
            }

            UIElement.UpdateElements();
            GameUI.UpdateButtons();

            // i have NO clue what this does i forgot
            if (UIElement.delay > 0)
                UIElement.delay--;
        }

        NetPlay.PollEvents();

        if (RuntimeData.RunTime % 60 < RuntimeData.DeltaTime)
            Task.Run(DiscordRichPresence.Update);

        LastGameTime = gameTime;

        if (_wasActive && !IsActive)
            OnFocusLost?.Invoke(this, Window.Handle);
        if (!_wasActive && IsActive)
            OnFocusRegained?.Invoke(this, Window.Handle);

        #endregion

        // TODO: this is quite hellcode. reorganize.
        if (!IsCrashInfoVisible) {
            CameraGlobals.UpdateCamera();
        }

        if (CampaignCompleteUI.IsViewingResults)
            CampaignCompleteUI.Update();

        SubHandleLogic(gameTime);

        // AchievementsUI.UpdateBtns();

        //for (int i = 0; i < AchievementsUI.AchBtns.Count; i++) {
        //var ach = AchievementsUI.AchBtns[i];
        // ach.Position -= new Vector2(2000);
        //}

        //GameView = GameCamera.View;
        //GameProjection = GameCamera.Projection;

        RuntimeData.LogicTime = gameTime.ElapsedGameTime;

        RuntimeData.LogicFPS = Math.Round(1f / gameTime.ElapsedGameTime.TotalSeconds);

        RuntimeData.DeltaTime = RuntimeData.Interp ? (!float.IsInfinity(60 / (float)RuntimeData.LogicFPS) ? 60 / (float)RuntimeData.LogicFPS : 0) : 1;

        _wasActive = IsActive;
        //Console.WriteLine($"{MouseUtils.MousePosition} - {_mOld}");
        _mouseOld = MouseUtils.MousePosition;
    }

    // wtf is wrong with me btw this code is ass
    private void SubHandleLogic(GameTime gameTime) {
        // TODO: this
        IsFixedTimeStep = !Settings.Vsync || !RuntimeData.Interp;

        RuntimeData.UpdateCount++;

        GameShaders.UpdateShaders();

        InputUtils.PollGamepad();
        InputUtils.PollKBM();
        InputUtils.Watch();

        bool shouldUpdate = Client.IsConnected() || (IsActive && !GameUI.Paused && !CampaignCompleteUI.IsViewingResults);
        if (!IsCrashInfoVisible) {
            if (shouldUpdate) {
                GameHandler.GameLoopLogic(gameTime);

                // questionable as to why it causes hella lag on game start
                // TODO: try and find out why this happens lol.
                if (float.IsFinite(RuntimeData.DeltaTime))
                    Tank.CollisionsWorld.Step(RuntimeData.DeltaTime);
            }
        }
        foreach (var bind in Keybind.AllKeybinds)
            bind?.Update();
    }
    public static void SaveRenderTarget(string path = "screenshot.png") {
        using var fs = new FileStream(path, FileMode.OpenOrCreate);
        GameFrameBuffer.SaveAsPng(fs, GameFrameBuffer.Width, GameFrameBuffer.Height);
        ChatSystem.SendMessage("Saved image to " + fs.Name, Color.Lime);
    }
    public static void ReportError(Exception? e, bool notifyUser = true, bool openFile = true, bool writeToLog = true) {
        if (writeToLog && e != null)
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
    public static void Quit() => Instance.Exit();
    public void PrepareGameBuffers(SpriteBatch spriteBatch) {
        // idea: have 3d resolution parameter to save performance (which also scales this down
        if (GameFrameBuffer == null || GameFrameBuffer.IsDisposed || GameFrameBuffer.Size() != WindowUtils.WindowBounds) {
            GameFrameBuffer?.Dispose();
            var presentationParams = GraphicsDevice.PresentationParameters;
            GameFrameBuffer = new RenderTarget2D(GraphicsDevice, presentationParams.BackBufferWidth, presentationParams.BackBufferHeight, false, presentationParams.BackBufferFormat, presentationParams.DepthStencilFormat, 0, RenderTargetUsage.PreserveContents);

            OnResolutionChanged?.Invoke(WindowUtils.WindowWidth, WindowUtils.WindowHeight);
        }

        // TankFootprint.DecalHandler.UpdateRenderTarget();
        GraphicsDevice.SetRenderTarget(GameFrameBuffer);
        GraphicsDevice.Clear(RenderGlobals.BackBufferColor);

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, rasterizerState: RenderGlobals.DefaultRasterizer);

        // TankFootprint.PrepareRT(GraphicsDevice);

        GraphicsDevice.DepthStencilState = RenderGlobals.DefaultStencilState;

        // so the meshes that need UV wrapping will work
        GraphicsDevice.SamplerStates[0] = RenderGlobals.WrappingSampler;
        RoomScene.Render();
        CosmeticsUI.RenderCrates();
        GraphicsDevice.SamplerStates[0] = RenderGlobals.ClampingSampler;
        GameHandler.RenderAll();

        spriteBatch.End();
        // stop drawing the regular game scene
        GraphicsDevice.SetRenderTarget(null);
    }
    protected override void Draw(GameTime gameTime) {
        PrepareAllRTs();

        DrawGameElements();

        DrawNonInteractiveUI();

        DrawInteractiveUI(gameTime);

        if (IngameConsole.IsOpen)
            IngameConsole.Draw(SpriteRenderer, FontGlobals.RebirthFont);

        DrawCursors();

        PreDrawBackBuffer?.Invoke(gameTime);

        RuntimeData.RenderTime = gameTime.ElapsedGameTime;
        RuntimeData.RenderFPS = Math.Round(1f / gameTime.ElapsedGameTime.TotalSeconds);

        // we only want to track frames where the game is active because if they aren't tabbed into the game, it locks to ~42fps
        if (IsActive)
            FPSTracker.Update(gameTime.ElapsedGameTime.TotalSeconds);
    }
    private static void DrawErrorScreen() {
        SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White], WindowUtils.ScreenRect, Color.Blue);

        SpriteRenderer.DrawString(FontGlobals.RebirthFontLarge, ":(", new Vector2(100, 100).ToResolution(), Color.White, (Vector2.One).ToResolution());
        SpriteRenderer.DrawString(FontGlobals.RebirthFontLarge,
            "Your game ran into a problem and might need to restart. We're just" +
            "\nshowing you what's wrong, and how it might affect your game.",
            new Vector2(100, 250).ToResolution(),
            Color.White,
            (Vector2.One * 0.4f).ToResolution());
        SpriteRenderer.DrawString(FontGlobals.RebirthFontLarge, RuntimeData.CrashInfo.Reason, new Vector2(100, 500).ToResolution(), Color.White, (Vector2.One * 0.3f).ToResolution());
        SpriteRenderer.DrawString(FontGlobals.RebirthFontLarge, RuntimeData.CrashInfo.Description, new Vector2(100, 550).ToResolution(), Color.White, (Vector2.One * 0.2f).ToResolution());

        var yMsg = "Press 'Y' to proceed with closing the game.";
        var nMsg = "Press 'N' to attempt to carry on with the game.";
        SpriteRenderer.DrawString(FontGlobals.RebirthFontLarge, yMsg, WindowUtils.WindowBottomLeft + new Vector2(10, -10), Color.White, (Vector2.One * 0.2f).ToResolution(), origin: GameUtils.GetAnchor(Anchor.BottomLeft, FontGlobals.RebirthFontLarge.MeasureString(yMsg)));
        SpriteRenderer.DrawString(FontGlobals.RebirthFontLarge, nMsg, WindowUtils.WindowBottomRight + new Vector2(-10, -10), Color.White, (Vector2.One * 0.2f).ToResolution(), origin: GameUtils.GetAnchor(Anchor.BottomRight, FontGlobals.RebirthFontLarge.MeasureString(nMsg)));
        if (InputUtils.KeyJustPressed(Keys.Y)) {
            ReportError(RuntimeData.CrashInfo.Cause, true, true, false);
            Quit();
        }
        if (InputUtils.KeyJustPressed(Keys.N)) {
            IsCrashInfoVisible = false;
            TankMusicSystem.ResumeAll();
        }
    }
    public static void DrawInteractiveUI(GameTime gameTime) {
        SpriteRenderer.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, rasterizerState: RenderGlobals.DefaultRasterizer);

        // if (LevelEditorUI.Active) LevelEditorUI.Render();
        if (CampaignCompleteUI.IsViewingResults) CampaignCompleteUI.Render();

        // draw black before intermission graphics because we don't want to reveal the mission as it's loading
        IntermissionSystem.DrawBlack(SpriteRenderer);

        SpriteRenderer.End();

        // this method begins the spritebatch, since it's supposed to have its own
        IntermissionSystem.Draw(SpriteRenderer);

        // hardcode hell. but whatever
        if (!LevelEditorUI.IsActive && MainMenuUI.MenuState != MainMenuUI.UIState.ModsMenu) {
            ChatSystem.DrawMessages();
            ChatSystem.Update(gameTime);
        }

        SpriteRenderer.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, rasterizerState: RenderGlobals.DefaultRasterizer);

        GameHandler.RenderUI();

        if (IsCrashInfoVisible) DrawErrorScreen();

        SpriteRenderer.End();
    }
    public void DrawNonInteractiveUI() {
        // holy balls this sucks.
        GraphicsDevice.DepthStencilState = RenderGlobals.DefaultStencilState;
        MainMenuUI.RenderModels();

        SpriteRenderer.Begin();

        MainMenuUI.Render(SpriteRenderer);
        // i really wish i didn't have to draw this here.
        VanillaAchievementPopupHandler.DrawPopup(SpriteRenderer);

        if (Debugger.IsAttached) {
            var font = FontGlobals.RebirthFont;
            var msg = "DEBUGGER ATTACHED";
            var measure = font.MeasureString(msg);
            SpriteRenderer.DrawString(font, msg, WindowUtils.WindowBottom, Color.Red, new Vector2(0.8f), origin: new Vector2(measure.X / 2, measure.Y));
        }

        DebugManager.DrawDebug(SpriteRenderer);
        DebugManager.DrawDebugMetrics();
        Speedrun.DrawSpeedrunHUD(SpriteRenderer);

        var shouldSeeInfo = !MainMenuUI.IsActive && !LevelEditorUI.IsActive && !CampaignCompleteUI.IsViewingResults;
        if (shouldSeeInfo) {
            GameSceneUI.DrawAll();
        }

        SpriteRenderer.End();
    }
    public void PrepareAllRTs() {
        // switch to RT, begin SB, do drawing, end SB, SetRenderTarget(null), begin SB again, draw RT, end SB
        PrepareGameBuffers(SpriteRenderer);
        MainMenuUI.PrepareTextBuffers(GraphicsDevice, SpriteRenderer);

        IntermissionSystem.PrepareBuffers(GraphicsDevice, SpriteRenderer);
    }

    public static void DrawGameElements() {
        var shader = Modifiers.Map[Modifiers.LANTERN] && !MainMenuUI.IsActive ? GameShaders.LanternShader : (MainMenuUI.IsActive ? GameShaders.GaussianBlurShader : null);
        if (!GameScene.ShouldRenderAll) shader = null;

        SpriteRenderer.Begin(effect: shader);
        SpriteRenderer.Draw(GameFrameBuffer, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, Vector2.One, default, 0f);
        SpriteRenderer.End();
    } 
    public static void DrawCursors() {
        foreach (var elem in PlayerMice) {
            elem.Value.Draw();
        }
    }

    static Particle _ziggy;
    static Particle _ziggyText;
    static Particle _bkCypher;
    static void PlaceSecrets() {
        // magic.
        const float SECRET_BASE_POS_X = GameScene.MIN_X - 28.5f;
        const float SECRET_BASE_POS_Y = 22;
        const float SECRET_BASE_POS_Z = -130;

        _ziggy = GameHandler.Particles.MakeParticle(new Vector3(100, 0.1f, 0), GameResources.GetGameResource<Texture2D>("Assets/textures/secret/ziggy"));
        _ziggy.Position = new Vector3(SECRET_BASE_POS_X, SECRET_BASE_POS_Y, SECRET_BASE_POS_Z);
        _ziggy.Pitch = MathHelper.Pi;
        _ziggy.Yaw = -MathHelper.PiOver2;
        _ziggy.Scale = Vector3.One * 0.3f;
        _ziggy.HasAdditiveBlending = false;
        _ziggy.UniqueBehavior = (a) => {
            _ziggy.Roll = RuntimeData.RunTime / 60 % 2 < 1 ? -MathHelper.PiOver4 / 4 : MathHelper.PiOver4 / 4;
        };

        _ziggyText = GameHandler.Particles.MakeParticle(new Vector3(100, 0.1f, 0), "Ziggy <3");
        _ziggyText.Position = new Vector3(SECRET_BASE_POS_X, SECRET_BASE_POS_Y + 20, SECRET_BASE_POS_Z - 8);
        _ziggyText.Yaw = MathHelper.PiOver2;
        _ziggyText.Roll = MathHelper.Pi;
        _ziggyText.Scale = Vector3.One * 0.3f;
        _ziggyText.HasAdditiveBlending = false;

        _bkCypher = GameHandler.Particles.MakeParticle(new Vector3(1000, 0.1f, 0), GameResources.GetGameResource<Texture2D>("Assets/textures/secret/bk_cypher"));
        _bkCypher.Yaw = MathHelper.PiOver2 + MathHelper.PiOver4;
        _bkCypher.Roll = MathHelper.Pi;
        _bkCypher.Scale = Vector3.One * 0.65f;
        _bkCypher.HasAdditiveBlending = false;
        _bkCypher.Position = new Vector3(1500, 1350, 149.5f);
    }
    private void TankGame_OnFocusRegained(object sender, IntPtr e) {
        if (TankMusicSystem.IsLoaded) {
            Thunder.ResumeGlobalSounds();
            TankMusicSystem.ResumeAll();
            if (MainMenuUI.IsActive)
                MainMenuUI.Theme.Resume();
            if (LevelEditorUI.IsActive)
                LevelEditorUI.Theme.Resume();
        }
    }
    private void TankGame_OnFocusLost(object sender, IntPtr e) {
        if (TankMusicSystem.IsLoaded) {
            Thunder.PauseGlobalSounds();
            TankMusicSystem.PauseAll();
            if (MainMenuUI.IsActive)
                MainMenuUI.Theme.Pause();
            if (LevelEditorUI.IsActive)
                LevelEditorUI.Theme.Pause();
        }
    }
}
