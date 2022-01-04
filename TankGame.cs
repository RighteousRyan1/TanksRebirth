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

namespace WiiPlayTanksRemake
{
    // TODO: Implement block once all of above things are done
    // TODO: AI in the middle to far future
    // TODO: to some finishing touches to TankMusicSystem

    public class GameConfig
    {
        public float MasterVolume { get; set; } = 1f;
        public float MusicVolume { get; set; } = 0.5f;
        public float SoundVolume { get; set; } = 1f;
        public float AmbientVolume { get; set; } = 1f;

        #region Graphics Settings
        public int TankFootprintLimit { get; set; } = 100000;
        public bool PerPixelLighting { get; set; } = true;
        public bool Vsync { get; set; } = true;
        public bool BorderlessWindow { get; set; } = true;

        public bool MSAA; // not property for a reason for now

        #endregion
    }

    public class Camera
    {
        private Matrix _viewMatrix;
        private Matrix _projectionMatrix;

        private Vector3 _position;

        private bool IsOmnicient { get; set; }

        public float _fov = MathHelper.ToRadians(90);

        public static GraphicsDevice GraphicsDevice { get; set; }

        public Camera() 
        {
            if (GraphicsDevice is null)
                throw new Exception("Please assign a proper graphics device for the camera to use.");

            _viewMatrix = Matrix.CreatePerspectiveFieldOfView(_fov, GraphicsDevice.Viewport.AspectRatio, 1f, 3000f);
            _projectionMatrix = Matrix.CreateOrthographic(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, -2000f, 5000f);
        }

        public Matrix GetView() => _viewMatrix;
        public Matrix GetProjection() => _projectionMatrix;

        public Camera SetToYawPitchRoll(float yaw, float pitch, float roll)
        {
            _viewMatrix *= Matrix.CreateFromYawPitchRoll(yaw, pitch, roll);
            return this;
        }

        public Camera SetOmni(bool omni)
        {
            IsOmnicient = omni;

            return this;
        }

        public Vector3 GetPosition() => _position;

        public Camera SetPosition(Vector3 pos)
        {
            _position = pos;
            return this;
        }

        public Camera SetFov(float degrees)
        {
            _fov = MathHelper.ToRadians(degrees);
            _viewMatrix = Matrix.CreatePerspectiveFieldOfView(_fov, GraphicsDevice.Viewport.AspectRatio, 0.01f, 3000f);
            return this;
        }

        public Camera Crunch()
        {

            return this;
        }

        public Camera Stretch()
        {

            return this;
        }
    }
    public class TankGame : Game
    {

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

        public static TankGame Instance { get; private set; }
        public static string ExePath => Assembly.GetExecutingAssembly().Location.Replace(@$"\WiiPlayTanksRemake.dll", string.Empty);
        public static SpriteBatch spriteBatch;

        public readonly GraphicsDeviceManager graphics;

        private static List<IGameSystem> systems = new();

        public static GameConfig Settings;

        public JsonHandler SettingsHandler;

        public static Texture2D WhitePixel;

        public static readonly string SaveDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "WiiPlayTanksRemake");

        public static Matrix GameView;
        public static Matrix GameProjection;

        public struct Fonts
        {
            public static SpriteFont Default;
        }

        public struct UITextures
        {
            public static Texture2D UIPanelBackground;
        }

        public TankGame() : base()
        {
            graphics = new(this);

            Internals.Core.ResolutionHandler.Initialize(graphics);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Instance = this;
            Window.Title = "Tanks! Remake";
            Window.AllowUserResizing = true;

            IsMouseVisible = false;
        }
        protected override void Initialize()
        {
            Camera.GraphicsDevice = GraphicsDevice;

            GameCamera = new Camera();

            GameCamera.SetToYawPitchRoll(0.75f, 0, 0);

            GameCamera.SetFov(90);

            GameCamera.SetPosition(GameCamera.GetPosition() + new Vector3(0, 100, 0));

            //GameView = GameCamera.GetView();
            //GameProjection = GameCamera.GetProjection();

            // i hate myself impostor syndrom

            systems = ReflectionUtils.GetInheritedTypesOf<IGameSystem>(Assembly.GetExecutingAssembly());

            graphics.PreferredBackBufferWidth = 1920;
            graphics.PreferredBackBufferHeight = 1080;

            if (!File.Exists(SaveDirectory + Path.DirectorySeparatorChar + "settings.json")) {
                Settings = new();
                SettingsHandler = new(Settings, SaveDirectory + Path.DirectorySeparatorChar + "settings.json");
                JsonSerializerOptions opts = new()
                {
                    WriteIndented = true
                };
                SettingsHandler.Serialize(opts, true);
            }
            else {
                SettingsHandler = new(Settings, SaveDirectory + Path.DirectorySeparatorChar + "settings.json");
                Settings = SettingsHandler.DeserializeAndSet<GameConfig>();
            }

            #region Config Initialization

            graphics.SynchronizeWithVerticalRetrace = Settings.Vsync;
            Window.IsBorderless = Settings.BorderlessWindow;
            Lighting.PerPixelLighting = Settings.PerPixelLighting;

            #endregion

            GameView = Matrix.CreateLookAt(new(0f, 0f, 120f), Vector3.Zero, Vector3.Up) * Matrix.CreateRotationX(0.75f) * Matrix.CreateTranslation(0f, 0f, 1000f);
            GameProjection = Matrix.CreateOrthographic(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, -2000f, 5000f);

            graphics.ApplyChanges();

            base.Initialize();
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            WPTR.ClientLog.Dispose();
            SettingsHandler = new(Settings, SaveDirectory + Path.DirectorySeparatorChar + "settings.json");
            JsonSerializerOptions opts = new()
            {
                WriteIndented = true
            };
            SettingsHandler.Serialize(opts, true);
        }

        protected override void LoadContent()
        {
            var s = Stopwatch.StartNew();

            CubeModel = GameResources.GetGameResource<Model>("Assets/cube_stack");

            TankModel_Enemy = GameResources.GetGameResource<Model>("Assets/tank_e");

            TankModel_Player = GameResources.GetGameResource<Model>("Assets/tank_p");

            Fonts.Default = GameResources.GetGameResource<SpriteFont>("Assets/DefaultFont");
            spriteBatch = new SpriteBatch(GraphicsDevice);
            UITextures.UIPanelBackground = GameResources.GetGameResource<Texture2D>("Assets/UIPanelBackground");
            WhitePixel = GameResources.GetGameResource<Texture2D>("Assets/MagicPixel");

            graphics.SynchronizeWithVerticalRetrace = true;
            WPTR.Initialize();

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

            WPTR.ClientLog.Write($"Content loaded in {time}.", LogType.Debug);
        }

        Vector2 rotVec;

        float zoom = 1f;

        protected override void Update(GameTime gameTime)
        {
            UpdateStopwatch.Start();

            LastGameTime = gameTime;
            if (Input.MouseRight)
                rotVec += GameUtils.GetMouseVelocity(GameUtils.WindowCenter) / 500;

            if (Input.CurrentKeySnapshot.IsKeyDown(Keys.Up))
                zoom += 0.01f;
            if (Input.CurrentKeySnapshot.IsKeyDown(Keys.Down))
                zoom -= 0.01f;

            GameUtils.GetMouseVelocity(GameUtils.WindowCenter);

            // why do i need to call this????

            GameView = Matrix.CreateScale(zoom) * Matrix.CreateLookAt(new(0f, 0f, 120f), Vector3.Zero, Vector3.Up) * Matrix.CreateRotationX(0.75f + rotVec.Y) * Matrix.CreateRotationY(rotVec.X);
            
            FixedUpdate(gameTime);

            LogicTime = UpdateStopwatch.Elapsed;

            UpdateStopwatch.Stop();

            LogicFPS = Math.Round(1f / gameTime.ElapsedGameTime.TotalSeconds, 4);
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

                WPTR.Update();


                foreach (var tnk in WPTR.AllTanks.Where(tnk => tnk is not null && !tnk.Dead))
                {
                    if (GameUtils.GetMouseToWorldRay().Intersects(tnk.CollisionBox).HasValue)
                    {
                        if (Input.KeyJustPressed(Keys.K))
                        {
                            // var tnk = WPTR.AllAITanks.FirstOrDefault(tank => tank is not null && !tank.Dead && tank.tier == AITank.GetHighestTierActive());

                            if (Array.IndexOf(WPTR.AllAITanks, tnk) > -1)
                                tnk?.Destroy();
                        }

                        tnk.IsHoveredByMouse = true;
                    }
                    else
                        tnk.IsHoveredByMouse = false;
                }
            }
            foreach (var music in Music.AllMusic)
                music?.Update();
        }
        protected override void Draw(GameTime gameTime)
        {
            RenderStopwatch.Start();

            GraphicsDevice.Clear(Color.SkyBlue);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);

            spriteBatch.DrawString(Fonts.Default, "Debug Level: " + DebugUtils.CurDebugLabel, new Vector2(10), Color.White, 0f, default, 0.6f, default, default);
            DebugUtils.DrawDebugString(spriteBatch, $"Memory Used: {MemoryParser.FromMegabytes(TotalMemoryUsed)} MB", new(8, GameUtils.WindowHeight * 0.18f));
            graphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            WPTR.DoRender();

            spriteBatch.End();

            base.Draw(gameTime);

            spriteBatch.Begin(blendState: BlendState.NonPremultiplied, effect: GameShaders.MouseShader);
            MouseRenderer.DrawMouse();
            spriteBatch.End();

            RenderTime = RenderStopwatch.Elapsed;

            RenderStopwatch.Stop();

            RenderFPS = Math.Round(1f / gameTime.ElapsedGameTime.TotalSeconds, 4);
        }
    }
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using var game = new TankGame();
            game.Run();
        }
    }
}
