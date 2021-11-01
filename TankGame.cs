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

namespace WiiPlayTanksRemake
{

    // TODO: Use orthographic off-center for projections
    // TODO: Make model drawing priority work
    // TODO: Implement block once all of above things are done

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
            public static Texture2D UIPanelBackgroundCorner;
        }

        public TankGame() : base()
        {
            graphics = new(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Instance = this;
            Window.Title = "Wii Play Tanks Remake";
            Window.AllowUserResizing = true;

            Window.IsBorderless = true;
        }

        protected override void Initialize()
        {
            systems = ReflectionUtils.GetInheritedTypesOf<IGameSystem>();
            // TODO: Add your initialization logic here
            graphics.PreferredBackBufferWidth = 1920;
            graphics.PreferredBackBufferHeight = 1080;

            GameView = Matrix.CreateLookAt(new(0f, 0f, 120f), Vector3.Zero, Vector3.Up) * Matrix.CreateRotationX(0.75f);
            GameProjection = Matrix.CreateOrthographic(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, -500f, 5000f);

            //GameView = Matrix.CreateLookAt(new(0f, 0f, 120f), Vector3.Zero, Vector3.Up);
            //GameProjection = Matrix.CreateOrthographicOffCenter(0, GameUtils.WindowWidth, GameUtils.WindowHeight, 0, -5000, 5000);//Matrix.CreateOrthographicOffCenter(0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, 0, -1, 5000);

            graphics.ApplyChanges();
            base.Initialize();
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            WPTR.BaseLogger.Dispose();
        }

        protected override void LoadContent()
        {
            CubeModel = Content.Load<Model>("Assets/cube");

            TankModel_Enemy = Content.Load<Model>("Assets/tank_enemy_noshadow");

            TankModel_Player = Content.Load<Model>("Assets/tank_player_noshadow");

            Fonts.Default = Content.Load<SpriteFont>("Assets/DefaultFont");
            spriteBatch = new SpriteBatch(GraphicsDevice);
            UITextures.UIPanelBackground = Content.Load<Texture2D>("Assets/UIPanelBackground");
            UITextures.UIPanelBackgroundCorner = Content.Load<Texture2D>("Assets/UIPanelBackgroundCorner");

            WPTR.Initialize();
            // TODO: use this.Content to load your game content here
        }
        protected override void Update(GameTime gameTime)
        {
            UpdateGameSystems();
            //GameView = Matrix.CreateLookAt(new(0f, 0f, 120f), Vector3.Zero, Vector3.Up) * Matrix.CreateRotationX(GameUtils.MousePosition.X / GameUtils.WindowWidth * 5);
            Window.IsBorderless = WPTR.WindowBorderless;

            GameUpdateTime++;

            Input.HandleInput();

            WPTR.Update();

            base.Update(gameTime);
        }

        private static void UpdateGameSystems()
        {
            foreach (var type in systems)
                type?.Update();
        }

        public static Matrix GameView;
        public static Matrix GameProjection;

        protected override void Draw(GameTime gameTime)
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);

            var info = $"MouseX/WindowWidth {GameUtils.MousePosition.X / GameUtils.WindowWidth}" +
                $"\nHighestTier: {Tank.GetHighestTierActive()}";

            spriteBatch.End();
            GraphicsDevice.Clear(Color.Black);

            //CubeModel.Draw(View, World, Projection);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);

            spriteBatch.DrawString(Fonts.Default, info, new(10, GameUtils.WindowHeight / 2), Color.White);

            // TankModelEnemy.draw

            graphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            var tList = Tank.AllTanks;

            tList.Sort(new Comparison<Tank>(
                (tank1, tank2) => 
                tank2.position.Z.CompareTo(tank1.position.Z)));

            int i = 0;
            foreach (var tank in tList)
            {
                spriteBatch.DrawString(Fonts.Default, tank.tier.ToString(), new(10, GameUtils.WindowHeight / 4 + (i * 20)), Color.White);
                i++;
                foreach (var mesh in tank.TankModel.Meshes)
                {
                    foreach (IEffectMatrices effect in mesh.Effects)
                    {
                        effect.View = tank.View;
                        effect.World = tank.World;
                        effect.Projection = tank.Projection;

                        if (tank._tankColorMesh != null)
                        {
                            var fx = effect as BasicEffect;

                            fx.LightingEnabled = true;
                            fx.PreferPerPixelLighting = true;
                            fx.EnableDefaultLighting();

                            fx.TextureEnabled = true;

                            fx.Texture = tank._tankColorMesh;
                        }
                    }

                    mesh.Draw();
                }
            }

            /*foreach (ModelMesh mesh in TankModel_Player.Meshes)
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
            }*/

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
