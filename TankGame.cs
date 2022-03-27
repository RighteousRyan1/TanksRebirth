using System;
using System.IO;
using System.Text.Json;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using WiiPlayTanksRemake.Internals.Common;
using WiiPlayTanksRemake.Internals.Common.Utilities;
using WiiPlayTanksRemake.GameContent;
using WiiPlayTanksRemake.Internals;
using WiiPlayTanksRemake.Internals.UI;
using WiiPlayTanksRemake.Internals.Core.Interfaces;
using System.Collections.Generic;
using System.Linq;
using WiiPlayTanksRemake.Internals.Common.IO;
using System.Diagnostics;
using WiiPlayTanksRemake.GameContent.UI;
using WiiPlayTanksRemake.Graphics;
using System.Management;
using WiiPlayTanksRemake.Internals.Common.Framework.Input;
using WiiPlayTanksRemake.Internals.Core;
using WiiPlayTanksRemake.Localization;
using FontStashSharp;
using WiiPlayTanksRemake.Internals.Common.Framework.Graphics;
using WiiPlayTanksRemake.GameContent.Systems;
using WiiPlayTanksRemake.Net;

namespace WiiPlayTanksRemake
{
    // TODO: Implement block once all of above things are done
    // TODO: AI in the middle to far future
    // TODO: add some finishing touches to TankMusicSystem

    public class TankGame : Game
    {
        private static string GetGPU()
        {
            if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
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
            if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
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

        public const int LevelEditorVersion = 1;

        public static readonly byte[] LevelFileHeader = { 84, 65, 78, 75 };

        public static Camera GameCamera;

        public static string SysGPU = $"GPU: {GetGPU()}";
        public static string SysCPU = $"CPU: {GetHardware("Win32_Processor", "Name")}";
        public static string SysKeybd = $"Keyboard: {GetHardware("Win32_Keyboard", "Name")}";
        public static string SysMouse = $"Mouse: {GetHardware("Win32_PointingDevice", "Name")}";
        public static string SysText;

        private static Stopwatch RenderStopwatch { get; } = new();
        private static Stopwatch UpdateStopwatch { get; } = new();

        public static TimeSpan RenderTime { get; private set; }
        public static TimeSpan LogicTime { get; private set; }

        public static double LogicFPS { get; private set; }
        public static double RenderFPS { get; private set; }

        public static long TotalMemoryUsed => GC.GetTotalMemory(true);

        public static GameTime LastGameTime { get; private set; }
        public static uint GameUpdateTime { get; private set; }

        public static Model TankModel_Player;
        public static Model TankModel_Enemy;
        public static Model CubeModel;
        public static Model CubeModelAlt;

        public static Texture2D MagicPixel;

        public static TankGame Instance { get; private set; }
        public static string ExePath => Assembly.GetExecutingAssembly().Location.Replace(@$"\WiiPlayTanksRemake.dll", string.Empty);
        public static SpriteBatch spriteBatch;

        public readonly GraphicsDeviceManager graphics;

        private static List<IGameSystem> systems = new();

        public static GameConfig Settings;

        public JsonHandler SettingsHandler;

        public static readonly string SaveDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "WiiPlayTanksRemake");

        public static Matrix GameView;
        public static Matrix GameProjection;

        private FontSystem _fontSystem;

        public static SpriteFontBase TextFont;

        public static event EventHandler<IntPtr> OnFocusLost;
        public static event EventHandler<IntPtr> OnFocusRegained;

        private bool _wasActive;

        public readonly string GameVersion;

        private static Internals.Common.GameUI.UIPanel dummyPannelBecauseCunosCodeIsUtterShitPleaseDoNotHurtMeForLookingAtThisCode;

        public TankGame() : base()
        {
            graphics = new(this) { PreferHalfPixelOffset = true };
            graphics.HardwareModeSwitch = false;


            Content.RootDirectory = "Content";
            Instance = this;
            Window.Title = "Tanks! Remake";
            Window.AllowUserResizing = true;

            IsMouseVisible = false;

            graphics.IsFullScreen = false;

            _fontSystem = new();

            GameVersion = typeof(TankGame).Assembly.GetName().Version.ToString();
        }

        protected override void Initialize()
        {
            DiscordRichPresence.Load();

            systems = ReflectionUtils.GetInheritedTypesOf<IGameSystem>(Assembly.GetExecutingAssembly());

            ResolutionHandler.Initialize(graphics);

            GameCamera = new Camera(GraphicsDevice);

            spriteBatch = new(GraphicsDevice);

            graphics.PreferMultiSampling = true;

            graphics.ApplyChanges();

            base.Initialize();
        }

        public static void Quit()
            => Instance.Exit();

        protected override void OnExiting(object sender, EventArgs args)
        {
            GameHandler.ClientLog.Dispose();
            SettingsHandler = new(Settings, Path.Combine(SaveDirectory, "settings.json"));
            JsonSerializerOptions opts = new()
            {
                WriteIndented = true
            };
            SettingsHandler.Serialize(opts, true);

            DiscordRichPresence.Terminate();
        }
        
        protected override void LoadContent()
        {
            var s = Stopwatch.StartNew();

            CubeModel = GameResources.GetGameResource<Model>("Assets/toy/cube_stack");
            CubeModelAlt = GameResources.GetGameResource<Model>("Assets/toy/cube_stack_alt");

            TankModel_Enemy = GameResources.GetGameResource<Model>("Assets/tank_e");

            TankModel_Player = GameResources.GetGameResource<Model>("Assets/tank_p");

            MagicPixel = GameResources.GetGameResource<Texture2D>("Assets/MagicPixel");

            _fontSystem.AddFont(File.ReadAllBytes(@"Content/Assets/fonts/en_US.ttf"));
            _fontSystem.AddFont(File.ReadAllBytes(@"Content/Assets/fonts/ja_JP.ttf"));
            _fontSystem.AddFont(File.ReadAllBytes(@"Content/Assets/fonts/es_ES.ttf"));
            _fontSystem.AddFont(File.ReadAllBytes(@"Content/Assets/fonts/ru_RU.ttf"));

            TextFont = _fontSystem.GetFont(30);

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
                Settings = SettingsHandler.DeserializeAndSet<GameConfig>();
            }

            #region Config Initialization

            graphics.SynchronizeWithVerticalRetrace = Settings.Vsync;
            Window.IsBorderless = Settings.BorderlessWindow;
            PlayerTank.controlUp.AssignedKey = Settings.UpKeybind;
            PlayerTank.controlDown.AssignedKey = Settings.DownKeybind;
            PlayerTank.controlLeft.AssignedKey = Settings.LeftKeybind;
            PlayerTank.controlRight.AssignedKey = Settings.RightKeybind;
            PlayerTank.controlMine.AssignedKey = Settings.MineKeybind;
            MapRenderer.Theme = Settings.GameTheme;

            graphics.PreferredBackBufferWidth = Settings.ResWidth;
            graphics.PreferredBackBufferHeight = Settings.ResHeight;

            graphics.ApplyChanges();

            Language.LoadLang(ref GameLanguage, Settings.Language);

            #endregion

            GameHandler.SetupGraphics();

            GameUI.Initialize();
            MainMenu.Initialize();

            UIElement.UIPanelBackground = GameResources.GetGameResource<Texture2D>("Assets/UIPanelBackground");

            GameHandler.ClientLog.Write($"Content loaded in {s.Elapsed}.", LogType.Debug);

            DecalSystem.Initialize(spriteBatch, GraphicsDevice);

            dummyPannelBecauseCunosCodeIsUtterShitPleaseDoNotHurtMeForLookingAtThisCode = new() { IsVisible = false };

            s.Stop();
        }

        public const float DEFAULT_ORTHOGRAPHIC_ANGLE = 0.75f;
        internal static Vector2 CameraRotationVector = new(0, DEFAULT_ORTHOGRAPHIC_ANGLE);

        public const float DEFAULT_ZOOM = 3.3f;
        internal static float AddativeZoom = 1f;

        internal static Vector2 CameraFocusOffset;

        internal static bool FirstPerson;

        public static bool OverheadView = false;

        private int transitionTimer;

        protected override void Update(GameTime gameTime)
        {
            try
            {
                if (UIElement.delay > 0)
                    UIElement.delay--;
                UpdateStopwatch.Start();

                if (NetPlay.CurrentClient is not null)
                    Client.clientNetManager.PollEvents();
                if (NetPlay.CurrentServer is not null)
                    Server.serverNetManager.PollEvents();
                UIElement.UpdateElements();
                GameUI.UpdateButtons();

                DiscordRichPresence.Update();

                LastGameTime = gameTime;

                if (_wasActive && !IsActive)
                    OnFocusLost?.Invoke(this, Window.Handle);
                if (!_wasActive && IsActive)
                    OnFocusRegained?.Invoke(this, Window.Handle);

                if (Input.KeyJustPressed(Keys.J))
                {
                    transitionTimer = 100;
                    OverheadView = !OverheadView;
                }
                if (!FirstPerson)
                {
                    if (transitionTimer > 0)
                    {
                        transitionTimer--;
                        if (OverheadView)
                        {
                            GameUtils.SoftStep(ref CameraRotationVector.Y, MathHelper.PiOver2, 0.08f);
                            GameUtils.SoftStep(ref AddativeZoom, 0.7f, 0.08f);
                            GameUtils.RoughStep(ref CameraFocusOffset.Y, 82f, 2f);
                        }
                        else
                        {
                            GameUtils.SoftStep(ref CameraRotationVector.Y, DEFAULT_ORTHOGRAPHIC_ANGLE, 0.08f);
                            GameUtils.SoftStep(ref AddativeZoom, 1f, 0.08f);
                            GameUtils.RoughStep(ref CameraFocusOffset.Y, 0f, 2f);
                        }
                    }

                    GameCamera.SetPosition(new Vector3(0, 0, 350));
                    GameCamera.SetLookAt(new Vector3(0, 0, 0));
                    GameCamera.Zoom(DEFAULT_ZOOM * AddativeZoom);

                    GameCamera.RotateX(CameraRotationVector.Y);
                    GameCamera.RotateY(CameraRotationVector.X);

                    GameCamera.SetCameraType(CameraType.Orthographic);

                    GameCamera.Translate(new Vector3(CameraFocusOffset.X, -CameraFocusOffset.Y + 40, 0));

                    GameCamera.SetViewingDistances(-2000f, 5000f);
                }
                else
                {
                    Vector3 pos = Vector3.Zero;
                    var x = GameHandler.AllAITanks.FirstOrDefault(x => x is not null && !x.Dead);

                    if (x is not null && Array.IndexOf(GameHandler.AllAITanks, x) > -1)
                    {
                        pos = x.Position3D;
                        var t = GameUtils.MousePosition.X / GameUtils.WindowWidth;
                        GameCamera.Zoom(DEFAULT_ZOOM * AddativeZoom);
                        GameCamera.SetFov(90);
                        GameCamera.SetPosition(pos);

                        GameCamera.SetLookAt(pos + new Vector3(0, 0, 20).FlattenZ().RotatedByRadians(-x.TurretRotation).ExpandZ());
                        //GameCamera.RotateY(GameUtils.MousePosition.X / 400);
                        //GameCamera.RotateY(DEFAULT_ORTHOGRAPHIC_ANGLE);
                        //GameCamera.RotateX(GameUtils.MousePosition.X / 400);

                        GameCamera.RotateX(CameraRotationVector.Y - MathHelper.PiOver4);
                        GameCamera.RotateY(CameraRotationVector.X);

                        GameCamera.Translate(new Vector3(0, -20, -40));

                        GameCamera.SetViewingDistances(0.1f, 10000f);

                        GameCamera.SetCameraType(CameraType.FieldOfView);
                    }
                }

                if (!GameUI.Paused && !MainMenu.Active && !OverheadView)
                {
                    if (Input.MouseRight)
                    {
                        CameraRotationVector += GameUtils.GetMouseVelocity(GameUtils.WindowCenter) / 500;
                    }

                    if (Input.CurrentKeySnapshot.IsKeyDown(Keys.Add))
                        AddativeZoom += 0.01f;
                    if (Input.CurrentKeySnapshot.IsKeyDown(Keys.Subtract))
                        AddativeZoom -= 0.01f;
                    if (Input.KeyJustPressed(Keys.Q))
                        FirstPerson = !FirstPerson;

                    if (Input.MouseMiddle)
                    {
                        CameraFocusOffset += GameUtils.GetMouseVelocity(GameUtils.WindowCenter);
                    }
                    GameUtils.GetMouseVelocity(GameUtils.WindowCenter);

                    IsFixedTimeStep = !Input.CurrentKeySnapshot.IsKeyDown(Keys.Tab);
                }

                FixedUpdate(gameTime);

                GameView = GameCamera.GetView();
                GameProjection = GameCamera.GetProjection();

                LogicTime = UpdateStopwatch.Elapsed;

                UpdateStopwatch.Stop();

                LogicFPS = Math.Round(1f / gameTime.ElapsedGameTime.TotalSeconds);

                _wasActive = IsActive;
            }
            catch (Exception e)
            {
                GameHandler.ClientLog.Write($"Error: {e.Message}\n{e.StackTrace}", LogType.Error);
                // throw;
            }
        }

        public void FixedUpdate(GameTime gameTime)
        {
            GameUpdateTime++;

            GameShaders.UpdateShaders();

            Input.HandleInput();

            if (IsActive && !GameUI.Paused)
            {
                foreach (var type in systems)
                    type?.Update();

                GameHandler.UpdateAll();

                Tank.CollisionsWorld.Step(1);

                if (!MainMenu.Active && OverheadView)
                {
                    foreach (var tnk in GameHandler.AllTanks)
                    {
                        if (tnk is not null && !tnk.Dead)
                        {
                            if (GameUtils.GetMouseToWorldRay().Intersects(tnk.Worldbox).HasValue)
                            {
                                if (Input.KeyJustPressed(Keys.K))
                                {
                                    // var tnk = WPTR.AllAITanks.FirstOrDefault(tank => tank is not null && !tank.Dead && tank.tier == AITank.GetHighestTierActive());

                                    if (Array.IndexOf(GameHandler.AllTanks, tnk) > -1)
                                        tnk?.Destroy();
                                }

                                if (Input.CanDetectClick(rightClick: true))
                                {
                                    tnk.TankRotation += MathHelper.PiOver2;
                                    tnk.TurretRotation += MathHelper.PiOver2;
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

            foreach (var music in Music.AllMusic)
                music?.Update();

            UiScale = GameUtils.MousePosition.X / GameUtils.WindowWidth * 5;
        }

        public static Matrix Matrix2D;

        public static float UiScale = 1f;

        protected override void Draw(GameTime gameTime)
        {
            RenderStopwatch.Start();

            Matrix2D = Matrix.CreateOrthographicOffCenter(0, GameUtils.WindowWidth, GameUtils.WindowHeight, 0, -1, 1)
                * Matrix.CreateScale(UiScale);

            GraphicsDevice.Clear(Color.Transparent);

            DecalSystem.UpdateRenderTarget();


            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied/*, transformMatrix: Matrix2D*/);

            if (DebugUtils.DebuggingEnabled)
                spriteBatch.DrawString(TextFont, "Debug Level: " + DebugUtils.CurDebugLabel, new Vector2(10), Color.White, new Vector2(0.6f));
            DebugUtils.DrawDebugString(spriteBatch, $"Memory Used: {MemoryParser.FromMegabytes(TotalMemoryUsed)} MB", new(8, GameUtils.WindowHeight * 0.18f));
            DebugUtils.DrawDebugString(spriteBatch, $"{SysGPU}\n{SysCPU}", new(8, GameUtils.WindowHeight * 0.2f));

            GraphicsDevice.DepthStencilState = new DepthStencilState() { };

            GameHandler.RenderAll();

            spriteBatch.End();

            base.Draw(gameTime);

            spriteBatch.Begin(blendState: BlendState.AlphaBlend, effect: GameShaders.MouseShader);

            MouseRenderer.DrawMouse();

            spriteBatch.End();

            foreach (var triangle in Triangle2D.triangles)
                triangle.DrawImmediate();
            foreach (var qu in Quad.quads)
                qu.Render();

            RenderTime = RenderStopwatch.Elapsed;

            RenderStopwatch.Stop();
            RenderFPS = Math.Round(1f / gameTime.ElapsedGameTime.TotalSeconds);
        }
    }
}
