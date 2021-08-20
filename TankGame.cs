using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using WiiPlayTanksRemake.Internals.Common;
using WiiPlayTanksRemake.Internals.Common.Utilities;
using WiiPlayTanksRemake.GameContent;

namespace WiiPlayTanksRemake
{
    public class TankGame : Game
    {
        public static TankGame Instance { get; private set; }
        public static string ExePath => Assembly.GetExecutingAssembly().Location.Replace(@$"\{nameof(GameContent.WiiPlayTanksRemake)}.dll", string.Empty);
        public static SpriteBatch spriteBatch;

        public GraphicsDeviceManager graphics;

        public Tank tank;

        public static DynamicSoundEffectInstance DynamicSoundEffectTest;

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
            Content.RootDirectory = "Content/Assets";
            IsMouseVisible = true;
            Instance = this;
            Window.Title = "Wii Play Tanks Remake";
            Window.AllowUserResizing = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            GameContent.WiiPlayTanksRemake.BaseLogger.Dispose();
        }

        protected override void LoadContent()
        {
            Fonts.Font = Content.Load<SpriteFont>("Go");
            spriteBatch = new SpriteBatch(GraphicsDevice);
            Models.TankModelEnemy = Content.Load<Model>("tnk_tank_e");
            Models.TankModelPlayer = Content.Load<Model>("tnk_tank_p");
            UITextures.UIPanelBackground = Content.Load<Texture2D>("UIPanelBackground");
            UITextures.UIPanelBackgroundCorner = Content.Load<Texture2D>("UIPanelBackgroundCorner");
            GameContent.WiiPlayTanksRemake.par = new Internals.UI.UIParent();
            GameContent.WiiPlayTanksRemake.text = new Internals.Common.GameUI.UIText("Test", Fonts.Font, Color.White);
            GameContent.WiiPlayTanksRemake.text.InteractionBox = new Rectangle(100, 100, 100, 100);
            GameContent.WiiPlayTanksRemake.par.AppendElement(GameContent.WiiPlayTanksRemake.text);
            // TODO: use this.Content to load your game content here
        }

        protected override void Update(GameTime gameTime)
        {
            Input.HandleInput();

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
