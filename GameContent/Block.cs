using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using tainicom.Aether.Physics2D.Dynamics;
using TanksRebirth;
using TanksRebirth.GameContent.Systems.Coordinates;
using TanksRebirth.Graphics;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals.Core.Interfaces;

namespace TanksRebirth.GameContent
{
    public struct BlockTemplate
    {
        public sbyte Stack;
        public Block.BlockType Type;
        public Vector2 Position;
        public sbyte TpLink;

        public Block GetBlock()
        {
            Block c = new(Type, Stack, Position);

            c.Position = Position;
            c.Body.Position = Position;
            c.TpLink = TpLink;

            return c;
        }
    }
    /// <summary>A class that is used for obstacles for <see cref="Tank"/>s.</summary>
    public class Block : IGameSystem
    {
        public enum BlockType : byte
        {
            Wood,
            Cork,
            Hole,
            Teleporter
        }

        public sbyte TpLink = -1;
        public int[] _tankCooldowns = new int[GameHandler.AllTanks.Length];

        public BlockType Type { get; set; }

        public static Block[] AllBlocks = new Block[CubeMapPosition.MAP_WIDTH * CubeMapPosition.MAP_HEIGHT * 5];

        public Vector2 Position;
        public Vector3 Position3D => Position.ExpandZ();

        public Model model;
        public Particle shadow;

        public Matrix World;
        public Matrix View;
        public Matrix Projection;

        public Body Body;

        public Rectangle Hitbox;

        public Texture2D meshTexture;

        public sbyte Stack;

        public const sbyte MAX_BLOCK_HEIGHT = 7;

        public const float FULL_BLOCK_SIZE = 21.7f; // 24.5 = 0.7 | 21 = 0.6
        public const float SLAB_SIZE = 11.5142857114f; // 13 = 0.7 | 11.14285714 = 0.6

        // 36, 18 respectively for normal size

        public const float FULL_SIZE = 89.28f; // 100.8 = 0.7 | 86.4 = 0.6

        // 141 for normal

        public int Id;

        private Vector3 _offset;

        public bool IsDestructible { get; set; }
        public bool IsSolid { get; } = true;

        public bool CanStack { get; set; } = true;

        private bool IsAlternateModel => Stack == 3 || Stack == 6;

        public Block(BlockType type, int height, Vector2 position)
        {
            Stack = (sbyte)MathHelper.Clamp(height, 0, 7); // if 0, it will be a hole.
            
            switch (type)
            {
                case BlockType.Wood:
                    meshTexture = MapRenderer.Assets["block.1"];
                    Body = Tank.CollisionsWorld.CreateRectangle(FULL_BLOCK_SIZE, FULL_BLOCK_SIZE, 1f, position, 0f, BodyType.Static);
                    model = IsAlternateModel ? GameResources.GetGameResource<Model>("Assets/toy/cube_stack_alt") : GameResources.GetGameResource<Model>("Assets/toy/cube_stack"); ;
                    break;
                case BlockType.Cork:
                    IsDestructible = true;
                    Body = Tank.CollisionsWorld.CreateRectangle(FULL_BLOCK_SIZE, FULL_BLOCK_SIZE, 1f, position, 0f, BodyType.Static);
                    meshTexture = MapRenderer.Assets["block.2"];
                    model = IsAlternateModel ? GameResources.GetGameResource<Model>("Assets/toy/cube_stack_alt") : GameResources.GetGameResource<Model>("Assets/toy/cube_stack"); ;
                    break;
                case BlockType.Hole:
                    model = GameResources.GetGameResource<Model>("Assets/check");
                    IsSolid = false;
                    Body = Tank.CollisionsWorld.CreateRectangle(FULL_BLOCK_SIZE, FULL_BLOCK_SIZE, 1f, position, 0f, BodyType.Static);
                    meshTexture = MapRenderer.Assets["block_harf.1"];
                    CanStack = false;
                    break;
                case BlockType.Teleporter:
                    model = GameResources.GetGameResource<Model>("Assets/teleporter");
                    IsSolid = false;
                    meshTexture = MapRenderer.Assets["teleporter"];
                    CanStack = false;
                    break;
            }

            Type = type;

            if (Body != null)
                Position = Body.Position;
            else
                Position = position;

            // fix this, but dont worry about it for now
            //shadow = ParticleSystem.MakeParticle(Position3D, GameResources.GetGameResource<Texture2D>($"Assets/toy/cube_shadow_tex"));
            //shadow.roll = -TankGame.DEFAULT_ORTHOGRAPHIC_ANGLE;
            //shadow.Scale = new(1f);
            //shadow.isAddative = false;

            // TODO: Finish collisions

            int index = Array.IndexOf(AllBlocks, AllBlocks.First(cube => cube is null));

            Id = index;

            AllBlocks[index] = this;
        }

        public void Remove()
        {
            if (Body != null)
                Tank.CollisionsWorld.Remove(Body);

            AllBlocks[Id] = null;
        }

        public void Destroy()
        {
            shadow?.Destroy();

            if (IsDestructible)
            {
                const int PARTICLE_COUNT = 12;

                for (int i = 0; i < PARTICLE_COUNT; i++)
                {
                    var tex = GameResources.GetGameResource<Texture2D>(GameHandler.GameRand.Next(0, 2) == 0 ? "Assets/textures/misc/tank_rock" : "Assets/textures/misc/tank_rock_2");

                    var part = ParticleSystem.MakeParticle(Position3D, tex);

                    part.isAddative = false;

                    var vel = new Vector3(GameHandler.GameRand.NextFloat(-3, 3), GameHandler.GameRand.NextFloat(4, 6), GameHandler.GameRand.NextFloat(-3, 3));

                    part.Roll = -TankGame.DEFAULT_ORTHOGRAPHIC_ANGLE;

                    part.Scale = new(0.7f);

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
            }

            AllBlocks[Id] = null;
        }

        public void Render()
        {
            if (Type != BlockType.Teleporter)
            {
                World = Matrix.CreateScale(0.62f) * Matrix.CreateTranslation(Position3D - _offset);
                Projection = TankGame.GameProjection;
                View = TankGame.GameView;
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
            else
            {
                foreach (var mesh in model.Meshes)
                {
                    foreach (BasicEffect effect in mesh.Effects)
                    {
                        effect.View = TankGame.GameView;
                        effect.World = World;
                        effect.Projection = TankGame.GameProjection;

                        effect.TextureEnabled = true;

                        // are the mesh definitions confused?
                        // the .fbx file has them named as they should be
                        if (mesh.Name == "Teleporter_Button")
                        {
                            World = Matrix.CreateRotationX(-MathHelper.PiOver2) * Matrix.CreateScale(10f) * Matrix.CreateTranslation(Position3D);
                            effect.Texture = meshTexture;
                        }
                        else if (mesh.Name == "Teleporter_Shadow")
                        {
                            World = Matrix.CreateRotationX(-MathHelper.PiOver2) * Matrix.CreateScale(10f) * Matrix.CreateTranslation(Position3D);
                            effect.Texture = GameResources.GetGameResource<Texture2D>("Assets/textures/mine/mine_shadow");
                        }
                        else if (mesh.Name == "Teleporter_Ring")
                        {
                            World = Matrix.CreateScale(1f) * Matrix.CreateTranslation(Position3D);
                            effect.Texture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank/wee/tank_rocket");
                        }

                        effect.SetDefaultGameLighting_IngameEntities(8f);

                        effect.DirectionalLight0.Direction *= 0.1f;

                        effect.Alpha = 1f;
                    }

                    mesh.Draw();
                }
            }
        }
        public void Update()
        {
            Hitbox = new((int)(Position.X - FULL_BLOCK_SIZE / 2), (int)(Position.Y - FULL_BLOCK_SIZE / 2), (int)FULL_BLOCK_SIZE, (int)FULL_BLOCK_SIZE);
            _offset = new();

            if (Type == BlockType.Teleporter)
            {
                foreach (var tnk in GameHandler.AllTanks)
                {
                    if (tnk is not null)
                    {
                        _tankCooldowns[tnk.WorldId]--;
                        if (_tankCooldowns[tnk.WorldId] <= 0)
                        {
                            if (Vector2.Distance(tnk.Position, Position) < FULL_BLOCK_SIZE)
                            {
                                var otherTp = AllBlocks.FirstOrDefault(bl => bl != null && bl != this && bl.TpLink == TpLink);

                                if (Array.IndexOf(AllBlocks, otherTp) > -1)
                                {
                                    otherTp._tankCooldowns[tnk.WorldId] = 120;

                                    tnk.Position = otherTp.Position;
                                    tnk.Body.Position = otherTp.Position;
                                }
                            }
                        }
                    }
                }
            }

            if (CanStack)
            {
                var newFullSize = FULL_SIZE;
                switch (Stack)
                {
                    case 1:
                        _offset = new(0, newFullSize - FULL_BLOCK_SIZE, 0);
                        break;
                    case 2:
                        _offset = new(0, newFullSize - (FULL_BLOCK_SIZE + SLAB_SIZE), 0);
                        break;
                    case 3:
                        _offset = new(0, newFullSize - (FULL_BLOCK_SIZE + SLAB_SIZE * 3), 0);
                        break;
                    case 4:
                        _offset = new(0, newFullSize - (FULL_BLOCK_SIZE * 2 + SLAB_SIZE), 0);
                        break;
                    case 5:
                        _offset = new(0, newFullSize - (FULL_BLOCK_SIZE * 2 + SLAB_SIZE * 2), 0);
                        break;
                    case 6:
                        _offset = new(0, newFullSize - (FULL_BLOCK_SIZE * 2 + SLAB_SIZE * 4), 0);
                        break;
                    case 7:
                        _offset = new(0, newFullSize - (FULL_BLOCK_SIZE * 3 + SLAB_SIZE * 2), 0);
                        break;
                }
            }
            else
                _offset.Y -= 0.1f;
        }

        public enum CollisionDirection
        {
            Up,
            Down,
            Left,
            Right
        }
    }
}