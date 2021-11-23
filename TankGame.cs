using System;
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

namespace WiiPlayTanksRemake
{
    // TODO: Implement block once all of above things are done
    // TODO: AI in the middle to far future
    // TODO: to some finishing touches to TankMusicSystem

    // TODO: working aim for tanks
    // MAJOR TODO: make tanks stop sharing the same goddamn spot, cuno maybe you can try it

    public class TankGame : Game
    {
        public static uint GameUpdateTime { get; private set; }

        public static Model TankModel_Player;
        public static Model TankModel_Enemy;

        public static Model CubeModel;

        public static TankGame Instance { get; private set; }
        public static string ExePath => Assembly.GetExecutingAssembly().Location.Replace(@$"\WiiPlayTanksRemake.dll", string.Empty);
        public static SpriteBatch spriteBatch;

        public readonly GraphicsDeviceManager graphics;

        private static List<IGameSystem> systems = new();

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

            // Window.IsBorderless = true;
        }

        protected override void Initialize()
        {
            systems = ReflectionUtils.GetInheritedTypesOf<IGameSystem>();
            // TODO: Add your initialization logic here
            graphics.PreferredBackBufferWidth = 1920;
            graphics.PreferredBackBufferHeight = 1080;

            GameView = Matrix.CreateLookAt(new(0f, 0f, 120f), Vector3.Zero, Vector3.Up) * Matrix.CreateRotationX(0.75f) * Matrix.CreateTranslation(0f, 0f, 1000f);
            GameProjection = Matrix.CreateOrthographic(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, -2000f, 5000f);

            //GameProjection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45), GraphicsDevice.Viewport.AspectRatio, 0.1f, 1000f);

            //GameView = Matrix.CreateLookAt(new(0f, 0f, 120f), Vector3.Zero, Vector3.Up);
            // GameProjection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver2, GraphicsDevice.Viewport.AspectRatio, 1, 5000);
            //GameProjection = Matrix.CreateOrthographicOffCenter(0, GameUtils.WindowWidth, GameUtils.WindowHeight, 0, -5000, 5000);//Matrix.CreateOrthographicOffCenter(0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, 0, -1, 5000);

            graphics.ApplyChanges();

            base.Initialize();
        }

        public static Vector3 mouse3d;

        protected override void OnExiting(object sender, EventArgs args)
        {
            WPTR.BaseLogger.Dispose();
        }

        protected override void LoadContent()
        {
            CubeModel = GameResources.GetGameResource<Model>("Assets/cube_stack");

            TankModel_Enemy = GameResources.GetGameResource<Model>("Assets/tank_e_fix");

            TankModel_Player = GameResources.GetGameResource<Model>("Assets/tank_p_fix");

            Fonts.Default = GameResources.GetGameResource<SpriteFont>("Assets/DefaultFont");
            spriteBatch = new SpriteBatch(GraphicsDevice);
            UITextures.UIPanelBackground = GameResources.GetGameResource<Texture2D>("Assets/UIPanelBackground");

            graphics.SynchronizeWithVerticalRetrace = true;
            WPTR.Initialize();

            foreach (ModelMesh mesh in TankModel_Player.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.LightingEnabled = true;
                    effect.PreferPerPixelLighting = true;
                    effect.EnableDefaultLighting();
                }
            }
            foreach (ModelMesh mesh in TankModel_Enemy.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.LightingEnabled = true;
                    effect.PreferPerPixelLighting = true;
                    effect.EnableDefaultLighting();
                }
            }
            // TODO: use this.Content to load your game content here
        }

        Vector2 rotVec;

        protected override void Update(GameTime gameTime)
        {
            if (Input.MouseRight)
            {
                rotVec += GameUtils.GetMouseVelocity(GameUtils.WindowCenter) / 500;
            }

            var transform = Vector3.Transform(new(GameUtils.MousePosition, 0), Matrix.Invert(GameView * GameProjection));

            mouse3d = transform;

            System.Diagnostics.Debug.WriteLine(GameUtils.GetMouseVelocity(GameUtils.WindowCenter));

            GameView = Matrix.CreateLookAt(new(0f, 0f, 120f), Vector3.Zero, Vector3.Up) * Matrix.CreateRotationX(0.75f + rotVec.Y) * Matrix.CreateRotationY(rotVec.X);
            // GameProjection = Matrix.CreateOrthographic(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, -2000f, 5000f) * Matrix.CreateScale(zoom);

            FixedUpdate(gameTime);
        }

        private static void UpdateGameSystems()
        {
            foreach (var type in systems)
                type?.Update();
        }

        public static Matrix GameView;
        public static Matrix GameProjection;

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
                    WPTR.AllAITanks.FirstOrDefault(tank => !tank.Dead && tank.tier == AITank.GetHighestTierActive()).Destroy();
                }

                Input.OldKeySnapshot = Input.CurrentKeySnapshot;
                Input.OldMouseSnapshot = Input.CurrentMouseSnapshot;

                base.Update(gameTime);
            }
            //testMatrix =
             //Matrix.CreateFromYawPitchRoll(0, 0, GameUtils.MousePosition.X / GameUtils.WindowWidth)
             //Matrix.CreateTranslation(GameUtils.MousePosition.X, 0, 0);
            // if (Input.CurrentKeySnapshot.IsKeyDown(Keys.Up))
            foreach (var music in Music.AllMusic)
                music?.Update();
        }
        protected override void Draw(GameTime gameTime)
        {
            var info = $"HighestTier: {AITank.GetHighestTierActive()}\nmyTank:{WPTR.myTank}";
            GraphicsDevice.Clear(Color.Black);
            // draw stuff past

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);

            DebugUtils.DrawDebugString(spriteBatch, $"{WPTR.myTank.GetGeneralStats()} : {GameUtils.MousePosition.ToNormalisedCoordinates()}", new(10, GameUtils.WindowHeight * 0.15f));
            DebugUtils.DrawDebugString(spriteBatch, $"{GameUtils.MousePosition}", new(10, GameUtils.WindowHeight * 0.4f));

            DebugUtils.DrawDebugString(spriteBatch, info, new(10, GameUtils.WindowHeight / 2));

            DebugUtils.DrawDebugString(spriteBatch, info, new(10, GameUtils.WindowHeight * 0.2f));

            DebugUtils.DrawDebugString(spriteBatch, $"CurSong: {(Music.AllMusic.FirstOrDefault(music => music.volume == 0.5f) != null ? Music.AllMusic.FirstOrDefault(music => music.volume == 0.5f).Name : "N/A")}", new(10, GameUtils.WindowHeight - 100));

            graphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            WPTR.Draw();

            spriteBatch.End();

            base.Draw(gameTime);
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
