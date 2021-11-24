using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using WiiPlayTanksRemake.Internals;
using WiiPlayTanksRemake.Internals.Common.Utilities;

namespace WiiPlayTanksRemake.GameContent
{
    public sealed class Mine
    {
        private static int maxMines = 200;
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

        public bool tickRed;

        public float explosionRadius;

        public Mine(Tank owner, Vector3 pos, int detonateTime, float radius = 80f)
        {
            this.owner = owner;
            explosionRadius = radius;

            Model = GameResources.GetGameResource<Model>("Assets/mine");

            detonationTime = detonateTime;

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
            var destroysound = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_destroy");

            SoundPlayer.PlaySoundInstance(destroysound, SoundContext.Sound, 0.4f);
            
            foreach (var shell in Shell.AllShells.Where(shell => shell is not null && Vector3.Distance(shell.position, position) < explosionRadius))
            {
                shell.Destroy();
            }
            foreach (var tank in WPTR.AllTanks.Where(tank => tank is not null && Vector3.Distance(tank.position, position) < explosionRadius))
            {
                tank.Destroy();
            }

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
                if (detonationTime % 10 == 0)
                    tickRed = !tickRed;
            }

            if (detonationTime <= 0)
                Detonate();

            foreach (var shell in Shell.AllShells.Where(shell => shell is not null && shell.hurtbox.Intersects(hitbox)))
            {
                shell.Destroy();
                Detonate();
            }
        }

        internal void Draw()
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
                        effect.EmissiveColor = new(1, 1, 0);
                        effect.DiffuseColor = new(1, 1, 0);
                        effect.SpecularColor = new(1, 1, 0);
                        effect.FogColor = new(1, 1, 0);

                        effect.Texture = _mineTexture;
                    }
                    else
                        effect.Texture = _envTexture;

                    effect.EnableDefaultLighting();
                }
                mesh.Draw();
            }
        }

        private class MineExplosion
        {
            // model, blah blah blah

            public Vector3 position;

            public Matrix View;
            public Matrix Projection;
            public Matrix World;

            public Model Model;

            public MineExplosion()
            {

            }
        }
    }
}