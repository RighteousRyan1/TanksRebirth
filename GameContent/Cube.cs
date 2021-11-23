using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using WiiPlayTanksRemake;
using WiiPlayTanksRemake.Internals;
using WiiPlayTanksRemake.Internals.Common.Utilities;
using WiiPlayTanksRemake.Internals.Core.Interfaces;

namespace WiiPlayTanksRemake.GameContent
{
    public class Cube : IGameSystem
    {
        internal static List<Cube> cubes = new();

        public Vector3 position;

        public Model model;

        public Matrix World;
        public Matrix View;
        public Matrix Projection;

        public BoundingBox collider;

        public int height;

        public Cube(Vector3 position)
        {
            model = TankGame.CubeModel;
            meshTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/ingame/block.1");
            this.position = position;
            World = Matrix.CreateTranslation(position);
            Projection = TankGame.GameProjection;
            View = TankGame.GameView;

            collider = new BoundingBox(position - new Vector3(3, 40, 3), position + new Vector3(3, 40, 3));
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

                    fx.EnableDefaultLighting();

                    fx.TextureEnabled = true;

                    fx.Texture = meshTexture;
                }

                mesh.Draw();
            }
        }
        public void Update()
        {
            World = Matrix.CreateTranslation(position);
            Projection = TankGame.GameProjection;
            View = TankGame.GameView;
        }
        // going up is negative
        public CubeCollisionDirection GetCollisionDirection(Vector2 collidingPosition)
        {
            var min = collider.Min.FlattenZ();
            var max = collider.Max.FlattenZ();

            if (collidingPosition.X < min.X && collidingPosition.X < max.X
                && collidingPosition.Y < max.Y && collidingPosition.Y > min.Y)
            {
                return CubeCollisionDirection.Left;
            }
            if (collidingPosition.X > min.X && collidingPosition.X > max.X
                && collidingPosition.Y < max.Y && collidingPosition.Y > min.Y)
            {
                return CubeCollisionDirection.Right;
            }
            if (collidingPosition.X > min.X && collidingPosition.X < max.X
                && collidingPosition.Y > max.Y && collidingPosition.Y > min.Y)
            {
                return CubeCollisionDirection.Down;
            }
            if (collidingPosition.X > min.X && collidingPosition.X < max.X
                && collidingPosition.Y < max.Y && collidingPosition.Y < min.Y)
            {
                return CubeCollisionDirection.Up;
            }
            return CubeCollisionDirection.Right;
        }

        public enum CubeCollisionDirection
        {
            Up,
            Down,
            Left,
            Right
        }
    }
}