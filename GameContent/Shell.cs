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
    public class Shell
    {
        public static Shell[] AllShells { get; } = new Shell[500];

        public Tank owner;

        public Vector3 position;
        public Vector3 velocity;
        public int ricochets;
        public float rotation;

        public Vector2 Velocity2D => velocity.FlattenZ();

        public Matrix View;
        public Matrix Projection;
        public Matrix World;

        public Model Model;

        public BoundingBox hurtbox = new();

        public bool Flaming { get; set; }

        public static Texture2D _shellTexture;

        private int worldId;

        public Shell(Vector3 position, Vector3 velocity, int ricochets = 0)
        {
            this.ricochets = ricochets;
            this.position = position;
            Model = GameResources.GetGameResource<Model>("Assets/bullet");
            Projection = TankGame.GameProjection;
            View = TankGame.GameView;
            World = Matrix.CreateTranslation(position);
            _shellTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/bullet");


            this.velocity = velocity;

            int index = Array.IndexOf(AllShells, AllShells.First(bullet => bullet is null));

            worldId = index;

            AllShells[index] = this;
        }

        internal void Update()
        {
            rotation = Velocity2D.ToRotation() - MathHelper.PiOver2;
            position += velocity;
            World = Matrix.CreateFromYawPitchRoll(-rotation, 0, 0)
                * Matrix.CreateTranslation(position);
            Projection = TankGame.GameProjection;
            View = TankGame.GameView;

            hurtbox.Max = position + new Vector3(3, 5, 3);
            hurtbox.Min = position - new Vector3(3, 5, 3);

            if (position.X < -274 || position.X > 274)
                if (ricochets > 0)
                    Ricochet(true);
                else
                    Destroy();
            if (position.Z < -160 || position.Z > 405)
                if (ricochets > 0)
                    Ricochet(false);
                else
                    Destroy();

            KillCollidingTanks();
        }

        /// <summary>
        /// Ricochets this bullet. if <paramref name="horizontal"/>, it will ricochet off of a horizontal axis.
        /// </summary>
        /// <param name="horizontal">Is this ricochet horizontal?</param>
        public void Ricochet(bool horizontal)
        {
            if (horizontal)
                velocity.X = -velocity.X;
            else 
                velocity.Z = -velocity.Z;

            var sound = GameResources.GetGameResource<SoundEffect>("Assets/sounds/bullet_ricochet");

            SoundPlayer.PlaySoundInstance(sound, SoundContext.Sound, 0.5f);

            ricochets--;
        }

        public void KillCollidingTanks()
        {
            /*foreach (var tank in WPTR.AllTanks)
                if (tank.CollisionBox.Intersects(hurtbox))
                    tank.Destroy();*/
            foreach (var tank in WPTR.AllAITanks)
            {
                if (tank.CollisionBox.Intersects(hurtbox))
                {
                    tank.Destroy();
                    Destroy();
                }
            }
            foreach (var tank in WPTR.AllPlayerTanks)
            {
                if (tank.CollisionBox.Intersects(hurtbox))
                {
                    tank.Destroy();
                    Destroy();
                }
            }

            foreach (var bullet in AllShells.Where(b => b is not null && b != this))
            {
                if (bullet.hurtbox.Intersects(hurtbox))
                {
                    bullet.Destroy();
                    Destroy();
                }
            }
        }

        public void Destroy()
        {
            var sfx = SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>("Assets/sounds/bullet_destroy"), SoundContext.Sound, 0.5f);
            sfx.Pitch = -0.2f;
            owner.OwnedBulletCount--;
            AllShells[worldId] = null;
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

                    effect.Texture = _shellTexture;

                    effect.EnableDefaultLighting();
                }
                mesh.Draw();
            }
        }
    }
}
