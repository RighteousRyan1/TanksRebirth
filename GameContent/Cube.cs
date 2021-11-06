using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using WiiPlayTanksRemake;
using WiiPlayTanksRemake.Internals;
using WiiPlayTanksRemake.Internals.Core.Interfaces;

namespace WiiPlayTanksRemake.GameContent
{
    public class Cube : IGameSystem
    {
        internal static List<Cube> cubes = new();

        public Vector3 position;

        public Model model;

        public Matrix World;

        public Cube(Vector3 position)
        {
            model = TankGame.CubeModel;
            meshTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/wood_default");
            this.position = position;
            cubes.Add(this);
        }

        public Texture2D meshTexture;

        public void Draw()
        {
            foreach (var mesh in model.Meshes)
            {
                foreach (IEffectMatrices effect in mesh.Effects)
                {
                    effect.View = TankGame.GameView;
                    effect.World = World;
                    effect.Projection = TankGame.GameProjection;

                    var fx = effect as BasicEffect;

                    fx.LightingEnabled = true;
                    fx.PreferPerPixelLighting = true;
                    fx.EnableDefaultLighting();

                    fx.TextureEnabled = true;

                    fx.Texture = meshTexture;
                }

                mesh.Draw();
            }
        }
        public void Update()
        {
            World = Matrix.CreateTranslation(position.X, position.Y, position.Z) * Matrix.CreateScale(500f);
        }
    }
}