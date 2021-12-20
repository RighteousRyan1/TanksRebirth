using System;
using System.IO;
using System.Text;
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

namespace WiiPlayTanksRemake
{
    // TODO: Implement block once all of above things are done
    // TODO: AI in the middle to far future
    // TODO: to some finishing touches to TankMusicSystem

    public class SettingsData
    {
        public float MusicVolume { get; set; } = 0;
        public float EffectsVolume { get; set; } = 0;
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

        public SettingsData Settings;

        public JsonHandler SettingsHandler;

        public static Texture2D MagicPixel;

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
            Window.Title = "Wii Play Tanks Remake";
            Window.AllowUserResizing = true;

            graphics.SynchronizeWithVerticalRetrace = true;

            IsMouseVisible = false;

            Window.IsBorderless = true;

            // var firstTexture = GetFirstAvailable(ToTexArray(GraphicsDevice.Textures));
        }

        public int GetFirstAvailable<T>(ICollection<T> collection)
        {
            var index = collection.FirstOrDefault(elem => elem is null);

            if (Array.IndexOf(collection.ToArray(), index) <= -1)
                return -1;

            return Array.IndexOf(collection.ToArray(), index);
        }

        public static ICollection<Texture2D> ToTexArray(TextureCollection collection)
        {
            return typeof(TextureCollection).GetField("_textures").GetValue(collection) as Texture2D[];
        }

        protected override void Initialize()
        {
            systems = ReflectionUtils.GetInheritedTypesOf<IGameSystem>();
            // TODO: Add your initialization logic here
            graphics.PreferredBackBufferWidth = 1920;
            graphics.PreferredBackBufferHeight = 1080;

            if (!File.Exists(SaveDirectory + Path.DirectorySeparatorChar + "settings.json")) {
                Settings = new()
                {
                    EffectsVolume = 100,
                    MusicVolume = 100
                };
                SettingsHandler = new(Settings, SaveDirectory + Path.DirectorySeparatorChar + "settings.json");
                System.Text.Json.JsonSerializerOptions opts = new();
                opts.WriteIndented = true;
                SettingsHandler.Serialize(opts, true);
            }
            else {
                SettingsHandler = new(Settings, SaveDirectory + Path.DirectorySeparatorChar + "settings.json");
                Settings = SettingsHandler.DeserializeAndSet<SettingsData>();
            }
            SoundPlayer.MusicVolume = Settings.MusicVolume / 100;
            SoundPlayer.SoundVolume = Settings.EffectsVolume / 100;

            GameView = Matrix.CreateLookAt(new(0f, 0f, 120f), Vector3.Zero, Vector3.Up) * Matrix.CreateRotationX(0.75f) * Matrix.CreateTranslation(0f, 0f, 1000f);
            GameProjection = Matrix.CreateOrthographic(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, -2000f, 5000f);

            //GameProjection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45), GraphicsDevice.Viewport.AspectRatio, 0.1f, 1000f);

            //GameView = Matrix.CreateLookAt(new(0f, 0f, 120f), Vector3.Zero, Vector3.Up);
            // GameProjection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver2, GraphicsDevice.Viewport.AspectRatio, 1, 5000);
            //GameProjection = Matrix.CreateOrthographicOffCenter(0, GameUtils.WindowWidth, GameUtils.WindowHeight, 0, -5000, 5000);//Matrix.CreateOrthographicOffCenter(0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, 0, -1, 5000);

            graphics.ApplyChanges();

            base.Initialize();
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            WPTR.ClientLog.Dispose();
        }

        protected override void LoadContent()
        {
            var s = Stopwatch.StartNew();

            CubeModel = GameResources.GetGameResource<Model>("Assets/cube_stack");

            TankModel_Enemy = GameResources.GetGameResource<Model>("Assets/tank_e_fix");

            TankModel_Player = GameResources.GetGameResource<Model>("Assets/tank_p_fix");

            Fonts.Default = GameResources.GetGameResource<SpriteFont>("Assets/DefaultFont");
            spriteBatch = new SpriteBatch(GraphicsDevice);
            UITextures.UIPanelBackground = GameResources.GetGameResource<Texture2D>("Assets/UIPanelBackground");
            MagicPixel = GameResources.GetGameResource<Texture2D>("Assets/MagicPixel");

            graphics.SynchronizeWithVerticalRetrace = true;
            WPTR.Initialize();

            foreach (ModelMesh mesh in TankModel_Player.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.LightingEnabled = true;
                    effect.PreferPerPixelLighting = true;
                    effect.EnableDefaultLighting();

                    effect.DirectionalLight0.Enabled = true;
                    effect.DirectionalLight1.Enabled = false;
                    effect.DirectionalLight2.Enabled = false;

                    if (mesh.Name == "polygon0.001")
                        effect.DirectionalLight0.Enabled = false;
                    else
                    {
                        effect.DirectionalLight1.Enabled = true;
                        //effect.DirectionalLight0.Direction = Vector3.Down;
                        effect.DirectionalLight0.Direction = new Vector3(0, -0.6f, -0.6f);
                        effect.DirectionalLight1.Direction = new Vector3(0, -0.6f, 0.6f);
                        // effect.DirectionalLight0.Direction = new(0, -1, -1);
                    }
                    effect.SpecularColor = new Vector3(0, 0, 0);
                }
            }
            foreach (ModelMesh mesh in TankModel_Enemy.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.LightingEnabled = true;
                    effect.PreferPerPixelLighting = true;
                    effect.EnableDefaultLighting();

                    effect.DirectionalLight0.Enabled = true;
                    effect.DirectionalLight1.Enabled = false;
                    effect.DirectionalLight2.Enabled = false;

                    if (mesh.Name == "polygon1")
                        effect.DirectionalLight0.Enabled = false;
                    else
                    {
                        effect.DirectionalLight1.Enabled = true;
                        //effect.DirectionalLight0.Direction = Vector3.Down;
                        effect.DirectionalLight0.Direction = new Vector3(0, -0.6f, -0.6f);
                        effect.DirectionalLight1.Direction = new Vector3(0, -0.6f, 0.6f);
                        // effect.DirectionalLight0.Direction = new(0, -1, -1);
                    }
                    effect.SpecularColor = new Vector3(0, 0, 0);
                }
            }


            var time = s.Elapsed;

            s.Stop();

            WPTR.ClientLog.Write($"Content loaded in {time}.", Logger.LogType.Debug);
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

            LogicFPS = Math.Round(1 / gameTime.ElapsedGameTime.TotalSeconds, 4);
        }

        private static void UpdateGameSystems()
        {
            foreach (var type in systems)
                type?.Update();
        }

        public void FixedUpdate(GameTime gameTime)
        {
            // ... still working this one out.
            if (IsActive)
            {
                //GameView = Matrix.CreateLookAt(new(0f, 0f, 120f), Vector3.Zero, Vector3.Up) * Matrix.CreateRotationX(GameUtils.MousePosition.X / GameUtils.WindowWidth * 5);
                Window.IsBorderless = WPTR.WindowBorderless;

                GameUpdateTime++;

                Input.HandleInput();

                UpdateGameSystems();

                WPTR.Update();

                if (Input.KeyJustPressed(Keys.K))
                {
                    var tnk = WPTR.AllAITanks.FirstOrDefault(tank => tank is not null && !tank.Dead && tank.tier == AITank.GetHighestTierActive());

                    if (Array.IndexOf(WPTR.AllAITanks, tnk) > -1)
                        tnk?.Destroy();
                }

                Input.OldKeySnapshot = Input.CurrentKeySnapshot;
                Input.OldMouseSnapshot = Input.CurrentMouseSnapshot;
                Input.OldGamePadSnapshot = Input.CurrentGamePadSnapshot;

                base.Update(gameTime);
            }
            foreach (var music in Music.AllMusic)
                music?.Update();
        }
        protected override void Draw(GameTime gameTime)
        {
            RenderStopwatch.Start();

            GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);

            spriteBatch.DrawString(Fonts.Default, "Debug Level: " + DebugUtils.CurDebugLabel, new Vector2(10), Color.White, 0f, default, 0.6f, default, default);
            DebugUtils.DrawDebugString(spriteBatch, "Memory Used: " + MemoryParser.FromMegabytes(TotalMemoryUsed) + " MB", new(8, GameUtils.WindowHeight * 0.18f));
            graphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            WPTR.DoRender();

            spriteBatch.End();

            base.Draw(gameTime);

            spriteBatch.Begin(blendState: BlendState.NonPremultiplied, effect: GameShaders.MouseShader);
            MouseRenderer.DrawMouse();
            spriteBatch.End();

            RenderTime = RenderStopwatch.Elapsed;

            RenderStopwatch.Stop();

            RenderFPS = Math.Round(1 / gameTime.ElapsedGameTime.TotalSeconds, 4);
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
