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
using TanksRebirth.Net;

namespace TanksRebirth.GameContent
{
    public sealed class Mine
    {
        public delegate void ExplodeDelegate(Mine mine);
        public static event ExplodeDelegate OnExplode;
        public delegate void PostUpdateDelegate(Mine mine);
        public static event PostUpdateDelegate OnPostUpdate;
        public delegate void PostRenderDelegate(Mine mine);
        public static event PostRenderDelegate OnPostRender;

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

        public int Id { get; private set; }

        private ModelMesh _mineMesh;
        private ModelMesh _envMesh;

        public Rectangle Hitbox;

        /// <summary>The time left (in ticks) until detonation.</summary>
        public float DetonateTime;
        /// <summary>The time until detonation (in ticks) from when this <see cref="Mine"/> was/is created.</summary>
        public readonly float DetonateTimeMax;

        private bool _tickRed;
        /// <summary>Whether or not this <see cref="Mine"/> is near destructible <see cref="Block"/>s.</summary>
        public bool IsNearDestructibles { get; private set; }

        /// <summary>The radius of this <see cref="Mine"/>'s explosion.</summary>
        public float ExplosionRadius;

        /// <summary>Whether or not this <see cref="Mine"/> has detonated.</summary>
        public bool Detonated { get; set; }

        /// <summary>The amount of time until detonation this <see cref="Mine"/> is set to when an enemy is within <see cref="MineReactRadius"/>.</summary>
        public int MineReactTime = 30;
        /// <summary>The radius of which this mine will shorten its detonation time to <see cref="MineReactTime"/>.<para></para>
        /// Is automatically set to <see cref="ExplosionRadius"/><c> * 0.8f</c></summary>
        public float MineReactRadius;

        /// <summary>
        /// Creates a new <see cref="Mine"/>.
        /// </summary>
        /// <param name="owner">The <see cref="Tank"/> which owns this <see cref="Mine"/>.</param>
        /// <param name="pos">The position of this <see cref="Mine"/> in the game world.</param>
        /// <param name="detonateTime">The time it takes for this <see cref="Mine"/> to detonate.</param>
        /// <param name="radius">The radius of this <see cref="Mine"/>'s explosion.</param>
        public Mine(Tank owner, Vector2 pos, float detonateTime, float radius = 65f)
        {
            Owner = owner;
            ExplosionRadius = radius;

            Model = GameResources.GetGameResource<Model>("Assets/mine");

            DetonateTime = detonateTime;
            DetonateTimeMax = detonateTime;

            Position = pos;

            if (owner != null)
                SoundPlayer.PlaySoundInstance("Assets/sounds/mine_place", SoundContext.Effect, 0.5f, gameplaySound: true);

            _mineMesh = Model.Meshes["polygon1"];
            _envMesh = Model.Meshes["polygon0"];

            _mineTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/mine/mine_env");
            _envTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/mine/mine_shadow");

            MineReactRadius = ExplosionRadius * 0.8f;

            int index = Array.IndexOf(AllMines, AllMines.First(mine => mine is null));

            Id = index;

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

            OnExplode?.Invoke(this);

            Client.SyncMineDetonate(this);

            Remove();
        }

        public void Remove() {
            AllMines[Id] = null; 
        }

        internal void Update() {

            if (!MapRenderer.ShouldRender)
                return;

            World = Matrix.CreateScale(0.7f) * Matrix.CreateTranslation(Position3D);

            Hitbox = new((int)Position.X - 10, (int)Position.Y - 10, 20, 20);

            if (Server.serverNetManager != null || !Client.IsConnected())
            {
                DetonateTime -= TankGame.DeltaTime;

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
                        shell.Destroy(Shell.DestructionContext.WithMine);
                        Detonate();
                    }
                }

                if (Position != _oldPosition) // magicqe number
                    IsNearDestructibles = Block.AllBlocks.Any(b => b != null && Position.Distance(b.Position) <= ExplosionRadius - 6f && b.IsDestructible);
                List<Tank> tanksNear = new();

                // NOTE: this scope may be inconsistent over a server? check soon.
                if (DetonateTime > MineReactTime)
                {
                    foreach (var tank in GameHandler.AllTanks)
                    {
                        if (tank is not null && Vector2.Distance(tank.Position, Position) < MineReactRadius)
                        {
                            tanksNear.Add(tank);
                        }
                    }
                    if (!tanksNear.Any(tnk => tnk == Owner) && tanksNear.Count > 0)
                        DetonateTime = MineReactTime;
                }
            }

            _oldPosition = Position;

            OnPostUpdate?.Invoke(this);
        }

        internal void Render()
        {
            if (!MapRenderer.ShouldRender)
                return;

            View = TankGame.GameView;
            Projection = TankGame.GameProjection;
            DebugUtils.DrawDebugString(TankGame.SpriteRenderer, $"DetonationTime: {DetonateTime}/{DetonateTimeMax}\nNearDestructibles: {IsNearDestructibles}\nId: {Id}", MatrixUtils.ConvertWorldToScreen(Vector3.Zero, World, View, Projection) - new Vector2(0, 20), 1, centered: true);
            for (int i = 0; i < (Lighting.AccurateShadows ? 2 : 1); i++) {
                foreach (ModelMesh mesh in Model.Meshes) {
                    foreach (BasicEffect effect in mesh.Effects) {
                        effect.World = i == 0 ? World : World * Matrix.CreateShadow(Lighting.AccurateLightingDirection, new(Vector3.UnitY, 0)) * Matrix.CreateTranslation(0, 0.2f, 0);
                        effect.View = View;
                        effect.Projection = Projection;

                        effect.TextureEnabled = true;

                        if (mesh == _mineMesh) {
                            if (!_tickRed) {
                                effect.EmissiveColor = new Vector3(1, 1, 0) * GameHandler.GameLight.Brightness;
                            }
                            else {
                                effect.EmissiveColor = new Vector3(1, 0, 0) * GameHandler.GameLight.Brightness;
                            }
                            effect.Texture = _mineTexture;

                            mesh.Draw();
                        }
                        else {
                            if (!Lighting.AccurateShadows) {
                                effect.Texture = _envTexture;
                                mesh.Draw();
                            }
                        }
                        effect.SetDefaultGameLighting_IngameEntities();
                    }
                }
            }
            OnPostRender?.Invoke(this);
        }
    }
}