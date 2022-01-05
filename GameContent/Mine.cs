using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using WiiPlayTanksRemake.Graphics;
using WiiPlayTanksRemake.Internals;
using WiiPlayTanksRemake.Internals.Common.Framework.Audio;
using WiiPlayTanksRemake.Internals.Common.Utilities;

namespace WiiPlayTanksRemake.GameContent
{
    public sealed class Mine
    {
        private static int maxMines = 500;
        public static Mine[] AllMines { get; } = new Mine[maxMines];

        public Tank owner;

        public Vector3 position;

        public Matrix View;
        public Matrix Projection;
        public Matrix World;

        public Vector2 Position2D => position.FlattenZ();

        public Model Model;

        public static Texture2D _mineTexture;
        public static Texture2D _envTexture;

        private int worldId;

        public ModelMesh MineMesh;
        public ModelMesh EnvMesh;

        public BoundingBox hitbox;

        public int detonationTime;
        public int detonationTimeMax;

        public bool tickRed;

        public float explosionRadius;

        public bool tankCameTooClose;

        public bool Detonated { get; set; }

        public Mine(Tank owner, Vector3 pos, int detonateTime, float radius = 80f)
        {
            this.owner = owner;
            explosionRadius = radius;

            Model = GameResources.GetGameResource<Model>("Assets/mine");

            detonationTime = detonateTime;
            detonationTimeMax = detonateTime;

            position = pos;

            MineMesh = Model.Meshes["polygon1"];
            EnvMesh = Model.Meshes["polygon0"];

            _mineTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/mine/mine_env");
            _envTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/mine/mine_shadow");

            int index = Array.IndexOf(AllMines, AllMines.First(bullet => bullet is null));

            worldId = index;

            AllMines[index] = this;
        }

        public void Detonate()
        {
            Detonated = true;
            var destroysound = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_destroy");

            SoundPlayer.PlaySoundInstance(destroysound, SoundContext.Effect, 0.4f);
            
            foreach (var shell in Shell.AllShells.Where(shell => shell is not null && Vector3.Distance(shell.position, position) < explosionRadius))
            {
                shell.Destroy();
            }
            foreach (var tank in WPTR.AllTanks.Where(tank => tank is not null && Vector3.Distance(tank.position, position) < explosionRadius))
            {
                if (!tank.Dead)
                    tank.Destroy();
            }

            foreach (var mine in AllMines.Where(mine => mine is not null && Vector3.Distance(mine.position, position) < explosionRadius && !mine.Detonated))
            {
                mine.Detonate();
            }

            var expl = new MineExplosion(position, explosionRadius * 0.101f, 0.5f);

            expl.expanseRate = 2f;
            expl.tickAtMax = 15;
            expl.shrinkRate = 0.5f;

            if (owner != null)
                owner.OwnedMineCount--;

            AllMines[worldId] = null;
        }

        internal void Update()
        {
            World = Matrix.CreateScale(0.7f) * Matrix.CreateTranslation(position);
            View = TankGame.GameView;
            Projection = TankGame.GameProjection;

            hitbox = new(position - new Vector3(10, 0, 10), position + new Vector3(10, 50, 10)); 

            detonationTime--;

            if (detonationTime < 120)
            {
                if (detonationTime % 2 == 0)
                    tickRed = !tickRed;
            }

            if (detonationTime <= 0)
                Detonate();

            foreach (var shell in Shell.AllShells.Where(shell => shell is not null && shell.hurtbox.Intersects(hitbox)))
            {
                shell.Destroy();
                Detonate();
            }

            if (detonationTime > 40 && !tankCameTooClose && detonationTime < detonationTimeMax / 2)
            {
                foreach (var tank in WPTR.AllTanks.Where(tank => tank is not null && Vector3.Distance(tank.position, position) < explosionRadius))
                {
                    tankCameTooClose = true;
                    detonationTime = 40;
                }
            }
        }

        internal void Draw()
        {
            foreach (var expl in MineExplosion.explosions.Where(expl => expl is not null))
                expl.Render();
            foreach (ModelMesh mesh in Model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = World;
                    effect.View = View;
                    effect.Projection = Projection;

                    effect.SetDefaultGameLighting_IngameEntities();

                    effect.TextureEnabled = true;

                    if (mesh == MineMesh)
                    {
                        if (!tickRed)
                        {
                            effect.EmissiveColor = new(1, 1, 0);
                            effect.SpecularColor = new(1, 1, 0);
                            effect.FogColor = new(1, 1, 0);
                        }
                        else
                        {
                            effect.EmissiveColor = new(1, 0, 0);
                            effect.SpecularColor = new(1, 0, 0);
                            effect.FogColor = new(1, 0, 0);
                        }

                        effect.Texture = _mineTexture;
                    }
                    else
                        effect.Texture = _envTexture;
                }
                mesh.Draw();
            }
        }
    }
    public class MineExplosion
    {
        // model, blah blah blah

        public const int MINE_EXPLOSIONS_MAX = 500;

        public static MineExplosion[] explosions = new MineExplosion[MINE_EXPLOSIONS_MAX];

        public Vector3 position;

        public Matrix View;
        public Matrix Projection;
        public Matrix World;

        public Model Model;

        public static Texture2D mask;

        public float scale;

        public readonly float maxScale;

        public float expanseRate = 1f;
        public float shrinkRate = 1f;

        public int tickAtMax = 40;

        private bool hitMaxAlready;

        private int id;

        public float rotation;

        public float rotationSpeed;

        public MineExplosion(Vector3 pos, float scaleMax, float rotationSpeed = 1f)
        {
            this.rotationSpeed = rotationSpeed;
            position = pos;
            maxScale = scaleMax;
            mask = GameResources.GetGameResource<Texture2D>("Assets/textures/mine/explosion_mask");

            Model = GameResources.GetGameResource<Model>("Assets/mineexplosion");

            int index = Array.IndexOf(explosions, explosions.First(t => t is null));

            id = index;

            explosions[index] = this;
        }

        public void Update()
        {
            if (!hitMaxAlready)
            {
                if (scale < maxScale)
                    scale += expanseRate;

                if (scale > maxScale)
                    scale = maxScale;

                if (scale >= maxScale)
                    hitMaxAlready = true;
            }
            else if (tickAtMax <= 0) 
                scale -= shrinkRate;

            if (hitMaxAlready)
                tickAtMax--;

            if (scale <= 0)
                explosions[id] = null;

            rotation += rotationSpeed;

            World = Matrix.CreateScale(scale) * Matrix.CreateRotationY(rotation) * Matrix.CreateTranslation(position);
            View = TankGame.GameView;
            Projection = TankGame.GameProjection;
        }

        public void Render()
        {
            foreach (ModelMesh mesh in Model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = World;
                    effect.View = View;
                    effect.Projection = Projection;
                    effect.TextureEnabled = true;

                    effect.Texture = mask;

                    effect.SetDefaultGameLighting_IngameEntities();

                    if (tickAtMax <= 0)
                        effect.Alpha -= 0.05f;
                    else
                        effect.Alpha = 1f;
                }
                mesh.Draw();
            }
        }
    }
}