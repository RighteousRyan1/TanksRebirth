using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using tainicom.Aether.Physics2D.Dynamics;
using WiiPlayTanksRemake;
using WiiPlayTanksRemake.GameContent.Systems.Coordinates;
using WiiPlayTanksRemake.Graphics;
using WiiPlayTanksRemake.Internals;
using WiiPlayTanksRemake.Internals.Common.Utilities;
using WiiPlayTanksRemake.Internals.Core.Interfaces;

namespace WiiPlayTanksRemake.GameContent
{
    /// <summary>A class that is used for obstacles for <see cref="Tank"/>s.</summary>
    public class Block : IGameSystem
    {
        public enum BlockType
        {
            Wood = 1,
            Cork = 2,
            Hole = 3
        }

        public BlockType Type { get; set; }

        public static Block[] blocks = new Block[CubeMapPosition.MAP_WIDTH * CubeMapPosition.MAP_HEIGHT * 5];

        public Vector2 Position;
        public Vector3 Position3D => Position.ExpandZ();

        public Model model;
        public Particle shadow;

        public Matrix World;
        public Matrix View;
        public Matrix Projection;

        public Body Body;

        public Rectangle collider2d;

        public Texture2D meshTexture;

        public int height;

        public const int MAX_BLOCK_HEIGHT = 7;

        public const float FULL_BLOCK_SIZE = 21.7f; // 24.5 = 0.7 | 21 = 0.6
        public const float SLAB_SIZE = 11.5142857114f; // 13 = 0.7 | 11.14285714 = 0.6

        // 36, 18 respectively for normal size

        public const float FULL_SIZE = 89.28f; // 100.8 = 0.7 | 86.4 = 0.6

        // 141 for normal

        public int worldId;

        public bool IsDestructible { get; set; }
        public bool IsSolid { get; } = true;

        public bool AffectedByOffset { get; set; } = true;

        private bool IsAlternateModel => height == 3 || height == 6;

        public Block(BlockType type, int height, Vector2 position)
        {
            meshTexture = type switch
            {
                BlockType.Wood => GameResources.GetGameResource<Texture2D>("Assets/textures/ingame/block.1"),
                BlockType.Cork => GameResources.GetGameResource<Texture2D>("Assets/textures/ingame/block.2"),
                BlockType.Hole => GameResources.GetGameResource<Texture2D>("Assets/textures/ingame/block_harf.2"),
                _ => null
            };

            this.height = MathHelper.Clamp(height, 0, 7); // if 0, it will be a hole.

            switch (type)
            {
                case BlockType.Wood:
                    meshTexture = GameResources.GetGameResource<Texture2D>($"{MapRenderer.assetsRoot}block.1");
                    model = IsAlternateModel ? TankGame.CubeModelAlt : TankGame.CubeModel;
                    break;
                case BlockType.Cork:
                    IsDestructible = true;
                    meshTexture = GameResources.GetGameResource<Texture2D>($"{MapRenderer.assetsRoot}block.2");
                    model = IsAlternateModel ? TankGame.CubeModelAlt : TankGame.CubeModel;
                    break;
                case BlockType.Hole:
                    model = GameResources.GetGameResource<Model>("Assets/check");
                    IsSolid = false;
                    meshTexture = GameResources.GetGameResource<Texture2D>($"{MapRenderer.assetsRoot}block_harf.2");
                    AffectedByOffset = false;
                    break;
            }

            Body = Tank.CollisionsWorld.CreateRectangle(FULL_BLOCK_SIZE, FULL_BLOCK_SIZE, 1f, position, 0f, BodyType.Static);

            Type = type;

            Position = Body.Position;

            // fix this, but dont worry about it for now
            //shadow = ParticleSystem.MakeParticle(Position3D, GameResources.GetGameResource<Texture2D>($"Assets/toy/cube_shadow_tex"));
            //shadow.roll = -TankGame.DEFAULT_ORTHOGRAPHIC_ANGLE;
            //shadow.Scale = new(1f);
            //shadow.isAddative = false;

            // TODO: Finish collisions

            int index = Array.IndexOf(blocks, blocks.First(cube => cube is null));

            worldId = index;

            blocks[index] = this;
        }

        public void SilentRemove()
        {
            Tank.CollisionsWorld.Remove(Body);
            // blah blah particle chunk thingy

            blocks[worldId] = null;
        }

        public void Destroy()
        {
            shadow?.Destroy();
            Tank.CollisionsWorld.Remove(Body);
            // blah blah particle chunk thingy

            const int PARTICLE_COUNT = 12;

            for (int i = 0; i < PARTICLE_COUNT; i++)
            {
                var tex = GameResources.GetGameResource<Texture2D>(GameHandler.GameRand.Next(0, 2) == 0 ? "Assets/textures/misc/tank_rock" : "Assets/textures/misc/tank_rock_2");

                var part = ParticleSystem.MakeParticle(Position3D, tex);

                part.isAddative = false;

                var vel = new Vector3(GameHandler.GameRand.NextFloat(-3, 3), GameHandler.GameRand.NextFloat(4, 6), GameHandler.GameRand.NextFloat(-3, 3));

                part.Roll = -TankGame.DEFAULT_ORTHOGRAPHIC_ANGLE;

                part.Scale = new(0.75f);

                part.color = Color.Coral;

                part.UniqueBehavior = (p) =>
                {
                    vel.Y -= 0.2f;
                    part.position += vel;
                    part.Opacity -= 0.025f;

                    if (part.Opacity <= 0f)
                        part.Destroy();
                };
            }

            blocks[worldId] = null;
        }

        public void Render()
        {
            foreach (var mesh in model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.View = TankGame.GameView;
                    effect.World = World;
                    effect.Projection = TankGame.GameProjection;

                    effect.TextureEnabled = true;
                    effect.Texture = meshTexture;

                    effect.SetDefaultGameLighting_IngameEntities(8f);

                    effect.DirectionalLight0.Direction *= 0.1f;

                    effect.Alpha = 1f;
                }

                mesh.Draw();
            }
        }
        public void Update()
        {
            collider2d = new((int)(Position.X - FULL_BLOCK_SIZE / 2), (int)(Position.Y - FULL_BLOCK_SIZE / 2), (int)FULL_BLOCK_SIZE, (int)FULL_BLOCK_SIZE);
            Vector3 offset = new();

            if (AffectedByOffset)
            {
                var newFullSize = FULL_SIZE;
                switch (height)
                {
                    case 1:
                        offset = new(0, newFullSize - FULL_BLOCK_SIZE, 0);
                        break;
                    case 2:
                        offset = new(0, newFullSize - (FULL_BLOCK_SIZE + SLAB_SIZE), 0);
                        break;
                    case 3:
                        offset = new(0, newFullSize - (FULL_BLOCK_SIZE + SLAB_SIZE * 3), 0);
                        break;
                    case 4:
                        offset = new(0, newFullSize - (FULL_BLOCK_SIZE * 2 + SLAB_SIZE), 0);
                        break;
                    case 5:
                        offset = new(0, newFullSize - (FULL_BLOCK_SIZE * 2 + SLAB_SIZE * 2), 0);
                        break;
                    case 6:
                        offset = new(0, newFullSize - (FULL_BLOCK_SIZE * 2 + SLAB_SIZE * 4), 0);
                        break;
                    case 7:
                        offset = new(0, newFullSize - (FULL_BLOCK_SIZE * 3 + SLAB_SIZE * 2), 0);
                        break;
                }
            }
            else
                offset.Y -= 0.05f;

            World = Matrix.CreateScale(0.62f) * Matrix.CreateTranslation(Position3D - offset);
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
}