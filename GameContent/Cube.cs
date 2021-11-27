using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        // internal static Cube[] cubes = new Cube[CubeMapPosition.MAP_WIDTH * CubeMapPosition.MAP_HEIGHT];

        public static Cube[,] cubes = new Cube[CubeMapPosition.MAP_WIDTH + 1, CubeMapPosition.MAP_HEIGHT + 1];

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
            meshTexture = type switch
            {
                BlockType.Wood => GameResources.GetGameResource<Texture2D>("Assets/textures/ingame/block.1"),
                BlockType.Cork => GameResources.GetGameResource<Texture2D>("Assets/textures/ingame/block.2"),
                BlockType.Hole => GameResources.GetGameResource<Texture2D>("Assets/textures/ingame/block_harf.2"),
                _ => null
            };

            this.height = MathHelper.Clamp(height, 0, 5); // if 0, it will be a hole.

            model = TankGame.CubeModel;


            Type = type;

            this.position = position;

            collider = new BoundingBox(position - new Vector3(3, 40, 3), position + new Vector3(3, 40, 3));

            /*int index = Array.IndexOf(cubes, cubes.First(tank => tank is null));

            worldId = index;

            cubes[index] = this;*/

            cubes[position.X, position.Y] = this;

            // cubes.Add(this);
        }

        public void Destroy()
        {
            // blah blah particle chunk thingy

            cubes[position.X, position.Y] = null;
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
                    effect.EnableDefaultLighting();

                    effect.TextureEnabled = true;
                    effect.Texture = meshTexture;


                    effect.DirectionalLight0.Enabled = true;
                    effect.DirectionalLight1.Enabled = true;
                    effect.DirectionalLight2.Enabled = false;

                    effect.DirectionalLight0.Direction = new Vector3(0, -0.6f, -0.6f);
                    effect.DirectionalLight1.Direction = new Vector3(0, -0.6f, 0.6f);

                    effect.SpecularColor = new Vector3(0, 0, 0);
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
        public const int MAP_WIDTH = 26;
        public const int MAP_HEIGHT = 20;

        public static implicit operator CubeMapPosition(Vector3 position) => ConvertFromVector3(position);
        public static implicit operator Vector2(CubeMapPosition position) => Convert2D(position);
        public static implicit operator Vector3(CubeMapPosition position) => Convert3D(position);

        public int X;
        public int Y;

        public CubeMapPosition(int x, int y)
        {
            X = x;
            Y = y;
        }
        public CubeMapPosition(int xy)
        {
            X = xy;
            Y = xy;
        }

        public static Vector2 Convert2D(CubeMapPosition pos)
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

        /// <summary>
        /// Literally doesn't work in the slightest. Do NOT USE
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static CubeMapPosition ConvertFromVector3(Vector3 position)
        {
            // var orig = new Vector3(MapRenderer.CUBE_MIN_X + (position.X % Cube.FULLBLOCK_SIZE), 0, MapRenderer.CUBE_MIN_Y + (position.Z % Cube.FULLBLOCK_SIZE));

            //var orig = new Vector3(MapRenderer.CUBE_MIN_X, 0, MapRenderer.CUBE_MIN_Y);

            //var real = new CubeMapPosition((int)(orig.X + (position.X / Cube.FULLBLOCK_SIZE)), (int)(orig.Y + (position.Z / Cube.FULLBLOCK_SIZE)) - 110);

            return new(); // new((int)((position.X + (MapRenderer.MIN_X + MapRenderer.MAX_X / 2)) % Cube.FULLBLOCK_SIZE), (int)((position.Y + (MapRenderer.MIN_Y + MapRenderer.MAX_Y / 2)) % Cube.FULLBLOCK_SIZE));

        }
    }
}