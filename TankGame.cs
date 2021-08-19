using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace WiiPlayTanksRemake
{
    public class TankGame : Game
    {
        public GraphicsDeviceManager graphics;
        public static SpriteBatch SpriteBatch;

        public static Model TankModelEnemy { get; private set; }
        public static Model TankModelPlayer { get; private set; }

        public TankGame()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content/Assets";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        protected override void LoadContent()
        {
            SpriteBatch = new SpriteBatch(GraphicsDevice);
            TankModelEnemy = Content.Load<Model>("tnk_tank_e");
            TankModelPlayer = Content.Load<Model>("tnk_tank_p");
            // TODO: use this.Content to load your game content here
        }

        protected override void Update(GameTime gameTime)
        {

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Orange);

            // TankModelEnemy.draw
            // TODO: Add your drawing code here

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
