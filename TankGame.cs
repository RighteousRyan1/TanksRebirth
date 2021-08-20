using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using WiiPlayTanksRemake.Internals.Common;
using WiiPlayTanksRemake.GameContent;
using WiiPlayTanksRemake.Internals;

namespace WiiPlayTanksRemake
{
    public class TankGame : Game
    {
        public static TankGame Instance { get; private set; }
        public static string ExePath => Assembly.GetExecutingAssembly().Location.Replace(@$"\{nameof(GameContent.WiiPlayTanksRemake)}.dll", string.Empty);
        public static SpriteBatch spriteBatch;

        public readonly GraphicsDeviceManager graphics;

        public static NameWithID[] menuModes =
        {
            new("MainMenu", 0),
            new("IngameMenu", 1),
            new("LevelEditorMenu", 2),
        };

        public struct Fonts
        {
            public static SpriteFont Font;
        }

        public struct Models
        {
            public static Model TankModelEnemy;
            public static Model TankModelPlayer;
        }

        public struct UITextures
        {
            public static Texture2D UIPanelBackground;
            public static Texture2D UIPanelBackgroundCorner;
        }

        public TankGame() : base()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Instance = this;
            Window.Title = "Wii Play Tanks Remake";
            Window.AllowUserResizing = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            GameContent.WiiPlayTanksRemake.Initialize();
            base.Initialize();
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            GameContent.WiiPlayTanksRemake.BaseLogger.Dispose();
        }

        protected override void LoadContent()
        {
            Fonts.Font = Content.Load<SpriteFont>("Assets/DefaultFont");
            spriteBatch = new SpriteBatch(GraphicsDevice);
            Models.TankModelEnemy = Content.Load<Model>("Assets/tnk_tank_e");
            Models.TankModelPlayer = Content.Load<Model>("Assets/tnk_tank_p");
            UITextures.UIPanelBackground = Content.Load<Texture2D>("Assets/UIPanelBackground");
            UITextures.UIPanelBackgroundCorner = Content.Load<Texture2D>("Assets/UIPanelBackgroundCorner");
            // TODO: use this.Content to load your game content here
        }

        protected override void Update(GameTime gameTime)
        {
            Input.HandleInput();
            TextInput.TrackInputKeys();

            GameContent.WiiPlayTanksRemake.Update();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
            GameContent.WiiPlayTanksRemake.Draw();

            // TankModelEnemy.draw
            // TODO: Add your drawing code here

            base.Draw(gameTime);
            spriteBatch.End();
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
