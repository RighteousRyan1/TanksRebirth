using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using tainicom.Aether.Physics2D.Dynamics;
using TanksRebirth;
using TanksRebirth.GameContent.ID;
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
        public int Type;
        public Vector2 Position;
        public sbyte TpLink;

        public Block GetBlock()
        {
            Block c = new(Type, Stack, Position);

            c.Position = Position;
            if (c.Body != null)
                c.Body.Position = Position;
            c.TpLink = TpLink;

            for (int i = 0; i < PlacementSquare.Placements.Count; i++) {
                if (c.Position3D == PlacementSquare.Placements[i].Position)
                    PlacementSquare.Placements[i].BlockId = c.Id;
            }

            return c;
        }
    }
    /// <summary>A class that is used for obstacles for <see cref="Tank"/>s.</summary>
    public class Block : IGameSystem
    {

        public delegate void DestroyDelegate(Block block);
        public static event DestroyDelegate OnDestroy;

        public delegate void UpdateDelegate(Block block);
        public static event UpdateDelegate OnPostUpdate;

        public delegate void PostRenderDelegate(Block block);
        public static event PostRenderDelegate OnPostRender;

        public delegate void InitializeDelegate(Block block);
        public static event InitializeDelegate OnInitialize;

        public sbyte TpLink = -1;
        public int[] _tankCooldowns = new int[GameHandler.AllTanks.Length];

        public int Type { get; set; }

        public static Block[] AllBlocks = new Block[CubeMapPosition.MAP_WIDTH * CubeMapPosition.MAP_HEIGHT * 5];

        public Vector2 Position;
        public Vector3 Position3D => Position.ExpandZ();

        public Model Model;

        public Matrix World;
        public Matrix View;
        public Matrix Projection;

        public Body Body;

        public Rectangle Hitbox;

        public Texture2D _texture;

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
        public bool IsSolid { get; set; } = true;
        public bool IsCollidable { get; set; } = true;
        public bool AllowShotPathBounce { get; set; } = true;
        /// <summary>Set to negative to increase the amount. Set to 0 to make nothing happen.</summary>
        public int PathBounceCount { get; set; } = 1;

        public bool CanStack { get; set; } = true;

        public bool IsAlternateModel => Stack == 3 || Stack == 6;

        public void SwapTexture(Texture2D texture) => _texture = texture;

        public Block(int type, int height, Vector2 position)
        {
            Stack = (sbyte)MathHelper.Clamp(height, 0, 7);
            Type = type;

            switch (type) {
                case BlockID.Wood:
                    _texture = MapRenderer.Assets["block.1"];
                    IsSolid = true;
                    Model = IsAlternateModel ? GameResources.GetGameResource<Model>("Assets/toy/cube_stack_alt") : GameResources.GetGameResource<Model>("Assets/toy/cube_stack"); ;
                    break;
                case BlockID.Cork:
                    IsDestructible = true;
                    _texture = MapRenderer.Assets["block.2"];
                    IsSolid = true;
                    Model = IsAlternateModel ? GameResources.GetGameResource<Model>("Assets/toy/cube_stack_alt") : GameResources.GetGameResource<Model>("Assets/toy/cube_stack"); ;
                    break;
                case BlockID.Hole:
                    Model = GameResources.GetGameResource<Model>("Assets/check");
                    IsSolid = false;
                    _texture = MapRenderer.Assets["block_harf.1"];
                    CanStack = false;
                    break;
                case BlockID.Teleporter:
                    Model = GameResources.GetGameResource<Model>("Assets/teleporter");
                    IsSolid = false;
                    IsCollidable = false;
                    _texture = MapRenderer.Assets["teleporter"];
                    CanStack = false;
                    break;
            }

            if (Body != null)
                Position = Body.Position;
            else
                Position = position;

            OnInitialize?.Invoke(this);

            if (IsCollidable)
                Body = Tank.CollisionsWorld.CreateRectangle(FULL_BLOCK_SIZE, FULL_BLOCK_SIZE, 1f, Position, 0f, BodyType.Static);

            Id = Array.FindIndex(AllBlocks, block => block is null);

            AllBlocks[Id] = this;

            if (CanStack)
            {
                // fix this, but dont worry about it for now
                var p = ParticleSystem.MakeParticle(Position3D, GameResources.GetGameResource<Texture2D>($"Assets/toy/cube_shadow_tex"));
                p.Tag = "block_shadow_" + Id;
                bool moveL = true;
                bool moveD = true;
                p.UniqueBehavior = (a) =>
                {
                    p.Roll = MathHelper.PiOver2;
                    p.Scale = new(1f);
                    p.isAddative = false;
                    p.TextureCrop = new(0, 0, 32, 32);
                    p.Alpha = 1f;

                    moveL = p.TextureCrop.Value.X < 32;
                    moveD = p.TextureCrop.Value.Y < 32;

                    float coordOff = 16 * p.Scale.X;

                    p.Position = new Vector3(Position3D.X + (moveL ? coordOff : -coordOff), 0.15f, Position3D.Z + (moveD ? coordOff : -coordOff));
                };
                // TODO: Finish collisions
            }
            UpdateOffset();
        }
        public void Remove()
        {
            var index = Array.FindIndex(ParticleSystem.CurrentParticles, a => {
                if (a == null)
                    return false;
                return (string)a.Tag == "block_shadow_" + Id;
                });
            if (index > -1)
                ParticleSystem.CurrentParticles[index].Destroy();

            if (Body != null && Tank.CollisionsWorld.BodyList.Contains(Body))
                Tank.CollisionsWorld.Remove(Body);
            AllBlocks[Id] = null;
        }
        public void Destroy()
        {
            if (IsDestructible)
            {
                const int PARTICLE_COUNT = 12;

                for (int i = 0; i < PARTICLE_COUNT; i++)
                {
                    var tex = GameResources.GetGameResource<Texture2D>(GameHandler.GameRand.Next(0, 2) == 0 ? "Assets/textures/misc/tank_rock" : "Assets/textures/misc/tank_rock_2");

                    var part = ParticleSystem.MakeParticle(Position3D, tex);
                    // var part = ParticleSystem.MakeParticle(Position3D, "wtf");

                    part.isAddative = false;

                    var vel = new Vector3(GameHandler.GameRand.NextFloat(-3, 3), GameHandler.GameRand.NextFloat(4, 6), GameHandler.GameRand.NextFloat(-3, 3));

                    part.Roll = -TankGame.DEFAULT_ORTHOGRAPHIC_ANGLE;

                    part.Scale = new(0.7f);

                    part.Color = Color.Coral;

                    part.UniqueBehavior = (p) =>
                    {
                        vel.Y -= 0.2f;
                        part.Position += vel;
                        part.Alpha -= 0.025f;

                        if (part.Alpha <= 0f)
                            part.Destroy();
                    };
                }
            }

            OnDestroy?.Invoke(this);
            Remove();
        }
        public void Render()
        {
            if (Type != BlockID.Teleporter)
            {
                World = Matrix.CreateScale(0.62f) * Matrix.CreateTranslation(Position3D - _offset);
                Projection = TankGame.GameProjection;
                View = TankGame.GameView;

                for (int i = 0; i < /*(Lighting.AccurateShadows ? 2 : 1)*/ 1; i++) // shadows later if i can fix it
                {
                    foreach (var mesh in Model.Meshes)
                    {
                        foreach (BasicEffect effect in mesh.Effects)
                        {
                            effect.View = TankGame.GameView;
                            effect.World = i == 0 ? World : World * Matrix.CreateShadow(Lighting.AccurateLightingDirection, new(Vector3.UnitY, 0)) * Matrix.CreateTranslation(0, 0.2f, 0) * Matrix.CreateScale(1, 1, (float)Stack / 7f);
                            effect.Projection = TankGame.GameProjection;

                            effect.TextureEnabled = true;
                            effect.Texture = _texture;

                            effect.SetDefaultGameLighting_IngameEntities(10f);

                            effect.DirectionalLight0.Direction *= 0.1f;

                            effect.Alpha = 1f;
                        }

                        mesh.Draw();
                    }
                }
            }
            else
            {
                foreach (var mesh in Model.Meshes)
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
                            effect.Texture = _texture;
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
            OnPostRender?.Invoke(this);
        }
        public void Update()
        {

            Hitbox = new((int)(Position.X - FULL_BLOCK_SIZE / 2 + 1), (int)(Position.Y - FULL_BLOCK_SIZE / 2), (int)FULL_BLOCK_SIZE - 1, (int)FULL_BLOCK_SIZE);
            _offset = new();

            if (Type == BlockID.Teleporter)
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

            UpdateOffset();

            OnPostUpdate?.Invoke(this);
        }
        private void UpdateOffset()
        {
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
    }
}