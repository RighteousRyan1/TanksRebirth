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
        public enum BlockType
        {
            Wood = 1,
            Cork = 2,
            Hole = 3
        }

        public BlockType Type { get; set; }

        internal static List<Cube> cubes = new();

        public CubeMapPosition position;

        public Model model;

        public Matrix World;
        public Matrix View;
        public Matrix Projection;

        public BoundingBox collider;

        public Texture2D meshTexture;

        public int height;

        public const int MAX_CUBE_HEIGHT = 5;

        public const float FULLBLOCK_SIZE = 25.2f;
        public const float SLAB_SIZE = 12.6f;

        // 36, 18 respectively for normal size

        public const float FULL_SIZE = 100.8f;

        // 141 for normal

        public Cube(CubeMapPosition position, BlockType type, int height)
        {
            this.height = MathHelper.Clamp(height, 0, 5); // if 0, it will be a hole.

            model = TankGame.CubeModel;

            meshTexture = type switch
            {
                BlockType.Wood => GameResources.GetGameResource<Texture2D>("Assets/textures/ingame/block.1"),
                BlockType.Cork => GameResources.GetGameResource<Texture2D>("Assets/textures/ingame/block.2"),
                _ => GameResources.GetGameResource<Texture2D>("Assets/textures/ingame/block_harf.2")
            };

            Type = type;

            this.position = position;

            collider = new BoundingBox(position - new Vector3(3, 40, 3), position + new Vector3(3, 40, 3));
            cubes.Add(this);
        }

        public void Draw()
        {
            foreach (var mesh in model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
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
            Vector3 offset = new();

            switch (height)
            {
                case 0:
                    offset = new(0, FULL_SIZE, 0);
                    // this thing is a hole, therefore you're mom; work on later
                    break;
                case 1:
                    offset = new(0, FULL_SIZE - FULLBLOCK_SIZE, 0);
                    break;
                case 2:
                    offset = new(0, FULL_SIZE - (FULLBLOCK_SIZE + SLAB_SIZE), 0);
                    break;
                case 3:
                    offset = new(0, FULL_SIZE - (FULLBLOCK_SIZE * 2 + SLAB_SIZE), 0);
                    break;
                case 4:
                    offset = new(0, FULL_SIZE - (FULLBLOCK_SIZE * 2 + SLAB_SIZE * 2), 0);
                    break;
            }

            World = Matrix.CreateScale(0.72f) * Matrix.CreateTranslation(position - offset);
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

    public struct CubeMapPosition
    {
        public static implicit operator Vector2(CubeMapPosition position) => Convert(position);
        public static implicit operator Vector3(CubeMapPosition position) => Convert3D(position);

        public int X;
        public int Y;

        public CubeMapPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static Vector2 Convert(CubeMapPosition pos)
        {
            // (0, 0) == (MIN_X, MIN_Y)

            var orig = new Vector2(MapRenderer.CUBE_MIN_X, MapRenderer.CUBE_MIN_Y);

            var real = new Vector2(orig.X + (pos.X * Cube.FULLBLOCK_SIZE), orig.Y + (pos.Y * Cube.FULLBLOCK_SIZE) - 110);

            return real;
        }

        public static Vector3 Convert3D(CubeMapPosition pos)
        {
            // (0, 0) == (MIN_X, MIN_Y)

            var orig = new Vector3(MapRenderer.CUBE_MIN_X, 0, MapRenderer.CUBE_MIN_Y);

            var real = new Vector3(orig.X + (pos.X * Cube.FULLBLOCK_SIZE), 0,  orig.Y + (pos.Y * Cube.FULLBLOCK_SIZE) - 110);

            return real;
        }
    }
}