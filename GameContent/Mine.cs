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
        private static int maxMines = 500;
        public static Mine[] AllMines { get; } = new Mine[maxMines];

        public Tank owner;

        public Vector2 Position;

        public Matrix View;
        public Matrix Projection;
        public Matrix World;

        public Vector3 Position3D => Position.ExpandZ();

        public Model Model;

        public static Texture2D _mineTexture;
        public static Texture2D _envTexture;

        private int worldId;

        public ModelMesh MineMesh;
        public ModelMesh EnvMesh;

        public Rectangle hitbox;

        public int detonationTime;
        public int detonationTimeMax;

        public bool tickRed;

        /// <summary>The radius of this <see cref="Mine"/>'s explosion.</summary>
        public float ExplosionRadius;

        /// <summary>Whether or not this <see cref="Mine"/> has detonated.</summary>
        public bool Detonated { get; set; }

        public int mineReactTime = 60;

        /// <summary>
        /// Creates a new <see cref="Mine"/>.
        /// </summary>
        /// <param name="owner">The <see cref="Tank"/> which owns this <see cref="Mine"/>.</param>
        /// <param name="pos">The position of this <see cref="Mine"/> in the game world.</param>
        /// <param name="detonateTime">The time it takes for this <see cref="Mine"/> to detonate.</param>
        /// <param name="radius">The radius of this <see cref="Mine"/>'s explosion.</param>
        public Mine(Tank owner, Vector2 pos, int detonateTime, float radius = 65f)
        {
            this.owner = owner;
            ExplosionRadius = radius;

            Model = GameResources.GetGameResource<Model>("Assets/mine");

            detonationTime = detonateTime;
            detonationTimeMax = detonateTime;

            Position = pos;

            MineMesh = Model.Meshes["polygon1"];
            EnvMesh = Model.Meshes["polygon0"];

            _mineTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/mine/mine_env");
            _envTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/mine/mine_shadow");

            int index = Array.IndexOf(AllMines, AllMines.First(mine => mine is null));

            worldId = index;

            AllMines[index] = this;
        }

        /// <summary>Detonates this <see cref="Mine"/>.</summary>
        public void Detonate()
        {
            Detonated = true;

            var expl = new Explosion(Position, ExplosionRadius * 0.101f, owner, 0.3f);

            if (Difficulties.Types["UltraMines"])
                expl.maxScale *= 2f;

            expl.expanseRate = 2f;
            expl.tickAtMax = 15;
            expl.shrinkRate = 0.5f;

            if (owner != null)
                owner.OwnedMineCount--;

            Remove();
        }

        public void Remove() {
            AllMines[worldId] = null; 
        }

        internal void Update()
        {
            World = Matrix.CreateScale(0.7f) * Matrix.CreateTranslation(Position3D);
            View = TankGame.GameView;
            Projection = TankGame.GameProjection;

            hitbox = new((int)Position.X - 10, (int)Position.Y - 10, 20, 20); 

            detonationTime--;

            if (detonationTime < 120)
            {
                if (detonationTime % 2 == 0)
                    tickRed = !tickRed;
            }

            if (detonationTime <= 0)
                Detonate();

            foreach (var shell in Shell.AllShells)
            {
                if (shell is not null && shell.hitbox.Intersects(hitbox))
                {
                    shell.Destroy();
                    Detonate();
                }
            }

            if (detonationTime > mineReactTime && detonationTime < detonationTimeMax / 2)
            {
                foreach (var tank in GameHandler.AllTanks)
                {
                    if (tank is not null && Vector2.Distance(tank.Position, Position) < ExplosionRadius * 9f)
                    {
                        detonationTime = mineReactTime;
                    }
                }
            }
        }

        internal void Render()
        {
            foreach (ModelMesh mesh in Model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = World;
                    effect.View = View;
                    effect.Projection = Projection;

                    effect.TextureEnabled = true;

                    if (mesh == MineMesh)
                    {
                        if (!tickRed)
                        {
                            effect.EmissiveColor = new Vector3(1, 1, 0) * GameHandler.GameLight.Brightness;
                        }
                        else
                        {
                            effect.EmissiveColor = new Vector3(1, 0, 0) * GameHandler.GameLight.Brightness;
                        }
                        effect.Texture = _mineTexture;
                    }
                    else
                    {
                        effect.Texture = _envTexture;
                    }
                    effect.SetDefaultGameLighting_IngameEntities();
                }
                mesh.Draw();
            }
        }
    }
}