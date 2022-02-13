using System;
using System.IO;
using System.Text.Json;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using WiiPlayTanksRemake.Internals.Common;
using WiiPlayTanksRemake.Internals.Common.Utilities;
using WiiPlayTanksRemake.GameContent;
using WiiPlayTanksRemake.Internals;
using WiiPlayTanksRemake.Internals.UI;
using WiiPlayTanksRemake.Internals.Common.GameInput;
using WiiPlayTanksRemake.Internals.Core.Interfaces;
using System.Collections.Generic;
using System.Linq;
using WiiPlayTanksRemake.Internals.Common.IO;
using System.Diagnostics;
using WiiPlayTanksRemake.GameContent.UI;
using WiiPlayTanksRemake.Internals.Common.Framework.Audio;
using WiiPlayTanksRemake.Graphics;
using System.Management;
using WiiPlayTanksRemake.Internals.Common.Framework.Input;
using WiiPlayTanksRemake.Internals.Core;
using WiiPlayTanksRemake.Localization;
using FontStashSharp.SharpFont;
using FontStashSharp;
using WiiPlayTanksRemake.Internals.Common.GameUI;

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

        public Camera GameCamera;

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

        public TankGame() : base()
        {
            graphics = new(this);

            Content.RootDirectory = "Content";
            Instance = this;
            Window.Title = "Tanks! Remake";
            Window.AllowUserResizing = true;

            IsMouseVisible = false;

            graphics.IsFullScreen = false;

            _fontSystem = new();
        }

        protected override void Initialize()
        {
            DiscordRichPresence.Load();

            //GameView = GameCamera.GetView();
            //GameProjection = GameCamera.GetProjection();

            // i hate myself impostor syndrom

            systems = ReflectionUtils.GetInheritedTypesOf<IGameSystem>(Assembly.GetExecutingAssembly());

            ResolutionHandler.Initialize(graphics);

            Camera.GraphicsDevice = GraphicsDevice;

            GameCamera = new Camera();
            GameCamera.SetToYawPitchRoll(0.75f, 0, 0);
            GameCamera.SetFov(90);
            GameCamera.SetPosition(GameCamera.GetPosition() + new Vector3(0, 100, 0));

            spriteBatch = new(GraphicsDevice);

            GameView = Matrix.CreateLookAt(new(0f, 0f, 120f), Vector3.Zero, Vector3.Up) * Matrix.CreateRotationX(0.75f) * Matrix.CreateTranslation(0f, 0f, 1000f);
            CalculateProjection();

            graphics.ApplyChanges();

            // TODO: make this load current language

            base.Initialize();
        }

        public void CalculateProjection()
        {
            GameProjection = Matrix.CreateOrthographic(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, -2000f, 5000f);
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

            CubeModel = GameResources.GetGameResource<Model>("Assets/cube_stack");

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

            graphics.PreferredBackBufferWidth = Settings.ResWidth;
            graphics.PreferredBackBufferHeight = Settings.ResHeight;

            Language.LoadLang(ref GameLanguage, Settings.Language);

            #endregion

            UIElement.UIPanelBackground = GameResources.GetGameResource<Texture2D>("Assets/UIPanelBackground");

            GameHandler.Initialize();

            foreach (ModelMesh mesh in TankModel_Player.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.SetDefaultGameLighting_IngameEntities();
                }
            }
            foreach (ModelMesh mesh in TankModel_Enemy.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.SetDefaultGameLighting_IngameEntities();
                }
            }

            var time = s.Elapsed;

            s.Stop();

            GameHandler.ClientLog.Write($"Content loaded in {time}.", LogType.Debug);
        }

        Vector2 rotVec;

        float zoom = 1f;

        Vector2 off;

        protected override void Update(GameTime gameTime)
        {
            try
            {
                UpdateStopwatch.Start();

                DiscordRichPresence.Update();

                LastGameTime = gameTime;

                if (!IngameUI.Paused)
                {
                    if (Input.MouseRight)
                        rotVec += GameUtils.GetMouseVelocity(GameUtils.WindowCenter) / 500;

                    if (Input.CurrentKeySnapshot.IsKeyDown(Keys.Up))
                        zoom += 0.01f;
                    if (Input.CurrentKeySnapshot.IsKeyDown(Keys.Down))
                        zoom -= 0.01f;

                    if (Input.MouseMiddle)
                    {
                        off += GameUtils.GetMouseVelocity(GameUtils.WindowCenter);
                    }

                    GameUtils.GetMouseVelocity(GameUtils.WindowCenter);

                    // why do i need to call this????

                    IsFixedTimeStep = true;

                    if (Input.CurrentKeySnapshot.IsKeyDown(Keys.Tab))
                        IsFixedTimeStep = false;

                    GameView = Matrix.CreateScale(zoom) * Matrix.CreateLookAt(new(0f, 0f, 120f), Vector3.Zero, Vector3.Up) * Matrix.CreateRotationX(0.75f + rotVec.Y) * Matrix.CreateRotationY(rotVec.X) * Matrix.CreateTranslation(off.X, -off.Y, 0);
                }

                FixedUpdate(gameTime);

                LogicTime = UpdateStopwatch.Elapsed;

                UpdateStopwatch.Stop();

                LogicFPS = Math.Round(1f / gameTime.ElapsedGameTime.TotalSeconds);
            }
            catch (Exception e)
            {
                GameHandler.ClientLog.Write($"{e.Message}\nError:{e.StackTrace}", LogType.Error);
            }
        }

        private static void UpdateGameSystems()
        {
            foreach (var type in systems)
                type?.Update();
        }

        public void FixedUpdate(GameTime gameTime)
        {
            GameUpdateTime++;

            GameShaders.UpdateShaders();

            Input.HandleInput();

            IngameUI.UpdateButtons();

            if (IsActive && !IngameUI.Paused)
            {
                //GameView = Matrix.CreateLookAt(new(0f, 0f, 120f), Vector3.Zero, Vector3.Up) * Matrix.CreateRotationX(GameUtils.MousePosition.X / GameUtils.WindowWidth * 5);

                UpdateGameSystems();

                GameHandler.Update();


                foreach (var tnk in GameHandler.AllTanks)
                {
                    if (tnk is not null && !tnk.Dead)
                    {
                        if (GameUtils.GetMouseToWorldRay().Intersects(tnk.CollisionBox).HasValue)
                        {
                            if (Input.KeyJustPressed(Keys.K))
                            {
                                // var tnk = WPTR.AllAITanks.FirstOrDefault(tank => tank is not null && !tank.Dead && tank.tier == AITank.GetHighestTierActive());

                                if (Array.IndexOf(GameHandler.AllAITanks, tnk) > -1)
                                    tnk?.Destroy();
                            }

                            tnk.IsHoveredByMouse = true;
                        }
                        else
                            tnk.IsHoveredByMouse = false;
                    }
                }
            }

            foreach (var bind in Keybind.AllKeybinds)
                bind?.Update();

            foreach (var music in Music.AllMusic)
                music?.Update();
        }

        protected override void Draw(GameTime gameTime)
        {
            RenderStopwatch.Start();

            GraphicsDevice.Clear(Color.SkyBlue);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);

            spriteBatch.DrawString(TextFont, "Debug Level: " + DebugUtils.CurDebugLabel, new Vector2(10), Color.White, new Vector2(0.6f));
            DebugUtils.DrawDebugString(spriteBatch, $"Memory Used: {MemoryParser.FromMegabytes(TotalMemoryUsed)} MB", new(8, GameUtils.WindowHeight * 0.18f));
            DebugUtils.DrawDebugString(spriteBatch, $"{SysGPU}\n{SysCPU}", new(8, GameUtils.WindowHeight * 0.2f));

            graphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            GameHandler.DoRender();

            spriteBatch.End();

            base.Draw(gameTime);

            spriteBatch.Begin(blendState: BlendState.NonPremultiplied, effect: GameShaders.MouseShader);

            MouseRenderer.DrawMouse();

            spriteBatch.End();

            RenderTime = RenderStopwatch.Elapsed;

            RenderStopwatch.Stop();
            RenderFPS = Math.Round(1f / gameTime.ElapsedGameTime.TotalSeconds);
        }
    }
}
