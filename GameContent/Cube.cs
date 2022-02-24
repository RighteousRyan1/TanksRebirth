using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WiiPlayTanksRemake;
using WiiPlayTanksRemake.Graphics;
using WiiPlayTanksRemake.Internals;
using WiiPlayTanksRemake.Internals.Common.Utilities;
using WiiPlayTanksRemake.Internals.Core.Interfaces;

namespace WiiPlayTanksRemake.GameContent
{
    /// <summary>A class that is used for obstacles for <see cref="Tank"/>s.</summary>
    public class Cube : IGameSystem
    {
        public enum BlockType
        {
            Wood = 1,
            Cork = 2,
            Hole = 3
        }

        public BlockType Type { get; set; }

        public static Cube[] cubes = new Cube[CubeMapPosition.MAP_WIDTH * CubeMapPosition.MAP_HEIGHT];

        // public static Cube[,] cubes = new Cube[CubeMapPosition.MAP_WIDTH + 1, CubeMapPosition.MAP_HEIGHT + 1];

        public Vector3 position;

        public Model model;

        public Matrix World;
        public Matrix View;
        public Matrix Projection;

        public BoundingBox collider;

        public Rectangle collider2d;

        public Texture2D meshTexture;

        public int height;

        public const int MAX_CUBE_HEIGHT = 7;

        public const float FULLBLOCK_SIZE = 25.2f;
        public const float SLAB_SIZE = 12.6f;

        // 36, 18 respectively for normal size

        public const float FULL_SIZE = 100.8f;

        // 141 for normal

        public int worldId;

        public Cube(BlockType type, int height)
        {
            meshTexture = type switch
            {
                BlockType.Wood => GameResources.GetGameResource<Texture2D>("Assets/textures/ingame/block.1"),
                BlockType.Cork => GameResources.GetGameResource<Texture2D>("Assets/textures/ingame/block.2"),
                BlockType.Hole => GameResources.GetGameResource<Texture2D>("Assets/textures/ingame/block_harf.2"),
                _ => null
            };
            this.height = MathHelper.Clamp(height, 0, 7); // if 0, it will be a hole.

            model = TankGame.CubeModel;

            Type = type;

            position = new(-1000, 0, 0);

            // TODO: Finish collisions

            int index = Array.IndexOf(cubes, cubes.First(cube => cube is null));

            worldId = index;

            cubes[index] = this;

            // cubes[position.X, position.Y] = this;

            // cubes.Add(this);
        }

        public void Destroy()
        {
            // blah blah particle chunk thingy

            cubes[worldId] = null;

            // cubes[position.X, position.Y] = null;
        }

        public void Render()
        {
            var whitePixel = GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel");

            //var deconstruct = GeometryUtils.ConvertWorldToScreen(Vector3.Zero, World, View, Projection);
            //var rect = new Rectangle((int)deconstruct.X - collider2d.Width / 2, (int)deconstruct.Y - collider2d.Height / 2, collider2d.Width, collider2d.Height);
            //TankGame.spriteBatch.Draw(whitePixel, rect, null, Color.White, 0f, /*rect.Size.ToVector2() / 2*/ Vector2.Zero, default, 1f);

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

                    effect.SetDefaultGameLighting_IngameEntities();

                    effect.DirectionalLight0.Direction *= 0.1f;
                }

                mesh.Draw();
            }
            // TankGame.spriteBatch.Draw(GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel"), collider2d, Color.White * 0.75f);
        }
        public void Update()
        {
            collider2d = new((int)(position.X - FULLBLOCK_SIZE / 2), (int)(position.Z - FULLBLOCK_SIZE / 2), (int)FULLBLOCK_SIZE, (int)FULLBLOCK_SIZE);
            collider = new BoundingBox(position - new Vector3(FULLBLOCK_SIZE / 2 + 4, FULL_SIZE, FULLBLOCK_SIZE / 2 + 4), position + new Vector3(FULLBLOCK_SIZE / 2 + 4, FULL_SIZE, FULLBLOCK_SIZE / 2 + 4));
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
        public const int MAP_WIDTH = 27;
        public const int MAP_HEIGHT = 19;

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
            var origPos = new Vector3(MapRenderer.CUBE_MIN_X % Cube.FULLBLOCK_SIZE, 0, MapRenderer.CUBE_MIN_Y % Cube.FULLBLOCK_SIZE);

            var invarX = (int)MathF.Round(origPos.X + GameUtils.GetWorldPosition(GameUtils.MousePosition).X % Cube.FULLBLOCK_SIZE, 1);
            var invarY = (int)MathF.Round(origPos.Z + GameUtils.GetWorldPosition(GameUtils.MousePosition).Z % Cube.FULLBLOCK_SIZE, 1);
            var invar = new CubeMapPosition(invarX, invarY);

            Systems.ChatSystem.SendMessage(invar, Color.White);

            return invar;

        }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder()
                .Append("{ ")
                .Append($"X: {X} | Y: {Y}")
                .Append(" }");

            return sb.ToString();
        }
    }
}