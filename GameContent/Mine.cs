using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Graphics;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent
{
    public sealed class Mine
    {
        public const int MAX_MINES = 500;
        public static Mine[] AllMines { get; } = new Mine[MAX_MINES];

        public Tank Owner;

        public Vector2 Position;
        private Vector2 _oldPosition;

        public Matrix View;
        public Matrix Projection;
        public Matrix World;

        public Vector3 Position3D => Position.ExpandZ();

        public Model Model;

        private static Texture2D _mineTexture;
        private static Texture2D _envTexture;

        private int _worldId;

        public ModelMesh MineMesh;
        public ModelMesh EnvMesh;

        public Rectangle Hitbox;

        public int DetonateTime;
        public readonly int DetonateTimeMax;

        private bool _tickRed;

        public bool IsNearDestructibles { get; private set; }

        /// <summary>The radius of this <see cref="Mine"/>'s explosion.</summary>
        public float ExplosionRadius;

        /// <summary>Whether or not this <see cref="Mine"/> has detonated.</summary>
        public bool Detonated { get; set; }

        public int MineReactTime = 30;

        /// <summary>
        /// Creates a new <see cref="Mine"/>.
        /// </summary>
        /// <param name="owner">The <see cref="Tank"/> which owns this <see cref="Mine"/>.</param>
        /// <param name="pos">The position of this <see cref="Mine"/> in the game world.</param>
        /// <param name="detonateTime">The time it takes for this <see cref="Mine"/> to detonate.</param>
        /// <param name="radius">The radius of this <see cref="Mine"/>'s explosion.</param>
        public Mine(Tank owner, Vector2 pos, int detonateTime, float radius = 65f)
        {
            this.Owner = owner;
            ExplosionRadius = radius;

            Model = GameResources.GetGameResource<Model>("Assets/mine");

            DetonateTime = detonateTime;
            DetonateTimeMax = detonateTime;

            Position = pos;

            MineMesh = Model.Meshes["polygon1"];
            EnvMesh = Model.Meshes["polygon0"];

            _mineTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/mine/mine_env");
            _envTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/mine/mine_shadow");

            int index = Array.IndexOf(AllMines, AllMines.First(mine => mine is null));

            _worldId = index;

            AllMines[index] = this;
        }

        /// <summary>Detonates this <see cref="Mine"/>.</summary>
        public void Detonate()
        {
            Detonated = true;

            var expl = new Explosion(Position, ExplosionRadius * 0.101f, Owner, 0.3f);

            if (Difficulties.Types["UltraMines"])
                expl.MaxScale *= 2f;

            expl.ExpanseRate = 2f;
            expl.ShrinkDelay = 15;
            expl.ShrinkRate = 0.5f;

            if (Owner != null)
                Owner.OwnedMineCount--;

            Remove();
        }

        public void Remove() {
            AllMines[_worldId] = null; 
        }

        internal void Update()
        {
            World = Matrix.CreateScale(0.7f) * Matrix.CreateTranslation(Position3D);
            View = TankGame.GameView;
            Projection = TankGame.GameProjection;

            Hitbox = new((int)Position.X - 10, (int)Position.Y - 10, 20, 20); 

            DetonateTime--;

            if (DetonateTime < 120)
            {
                if (DetonateTime % 2 == 0)
                    _tickRed = !_tickRed;
            }

            if (DetonateTime <= 0)
                Detonate();

            foreach (var shell in Shell.AllShells)
            {
                if (shell is not null && shell.Hitbox.Intersects(Hitbox))
                {
                    shell.Destroy();
                    Detonate();
                }
            }

            if (Position != _oldPosition) // magicqe number
                IsNearDestructibles = Block.AllBlocks.Any(b => b != null && Position.Distance(b.Position) <= ExplosionRadius - 6f && b.IsDestructible);
            List<Tank> tanksNear = new();

            if (DetonateTime > MineReactTime)
            {
                foreach (var tank in GameHandler.AllTanks)
                {
                    if (tank is not null && Vector2.Distance(tank.Position, Position) < ExplosionRadius * 0.8f)
                    {
                        tanksNear.Add(tank);
                    }
                }
                if (!tanksNear.Any(tnk => tnk == Owner) && tanksNear.Count > 0)
                    DetonateTime = MineReactTime;
            }

            _oldPosition = Position;
        }

        internal void Render()
        {
            DebugUtils.DrawDebugString(TankGame.SpriteRenderer, $"Det: {DetonateTime}/{DetonateTimeMax}\nNearDestructibles: {IsNearDestructibles}", GeometryUtils.ConvertWorldToScreen(Vector3.Zero, World, View, Projection) - new Vector2(0, 20), 1, centered: true);
            for (int i = 0; i < (Lighting.AccurateShadows ? 2 : 1); i++)
            {
                foreach (ModelMesh mesh in Model.Meshes)
                {
                    foreach (BasicEffect effect in mesh.Effects)
                    {
                        effect.World = i == 0 ? World : World * Matrix.CreateShadow(Lighting.AccurateLightingDirection, new(Vector3.UnitY, 0)) * Matrix.CreateTranslation(0, 0.2f, 0);
                        effect.View = View;
                        effect.Projection = Projection;

                        effect.TextureEnabled = true;

                        if (mesh == MineMesh)
                        {
                            if (!_tickRed)
                            {
                                effect.EmissiveColor = new Vector3(1, 1, 0) * GameHandler.GameLight.Brightness;
                            }
                            else
                            {
                                effect.EmissiveColor = new Vector3(1, 0, 0) * GameHandler.GameLight.Brightness;
                            }
                            effect.Texture = _mineTexture;

                            mesh.Draw();
                        }
                        else
                        {
                            if (!Lighting.AccurateShadows)
                            {
                                effect.Texture = _envTexture;
                                mesh.Draw();
                            }
                        }
                        effect.SetDefaultGameLighting_IngameEntities();
                    }
                }
            }
        }
    }
}