using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using WiiPlayTanksRemake.GameContent.GameMechanics;
using WiiPlayTanksRemake.Graphics;
using WiiPlayTanksRemake.Internals;
using WiiPlayTanksRemake.Internals.Common.Framework.Audio;
using WiiPlayTanksRemake.Internals.Common.Utilities;

namespace WiiPlayTanksRemake.GameContent
{
    public class Shell
    {
        /// <summary>A structure that allows you to give a <see cref="Shell"/> homing properties.</summary>
        public struct HomingProperties {
            public float power;
            public float radius;
            public float speed;
            public int cooldown;
        }

        /// <summary>The maximum shells allowed at any given time.</summary>
        private static int maxShells = 1500;
        public static Shell[] AllShells { get; } = new Shell[maxShells];

        /// <summary>The tank who shot this <see cref="Shell"/>.</summary>
        public Tank owner;

        public Vector3 position;
        public Vector3 velocity;
        /// <summary>How many times this <see cref="Shell"/> can hit walls.</summary>
        public int ricochets;
        public float rotation;

        /// <summary>The homing properties of this shell.</summary>
        public HomingProperties homingProperties = default;

        public Vector2 Position2D => position.FlattenZ();
        public Vector2 Velocity2D => velocity.FlattenZ();

        public Matrix View;
        public Matrix Projection;
        public Matrix World;

        public Model Model;

        /// <summary>The <see cref="BoundingBox"/> of this <see cref="Shell"/> determining the size of it's hurtbox/hitbox.</summary>
        public BoundingBox hurtbox = new();
        /// <summary>The hurtbox on the 2D backing map for the game.</summary>
        public Rectangle hurtbox2d;
        /// <summary>Whether or not this shell should emit flames from behind it.</summary>
        public bool Flaming { get; set; }

        public static Texture2D _shellTexture;

        private int worldId;

        /// <summary>How long this shell has existed in the world.</summary>
        public int lifeTime;

        internal bool INTERNAL_ignoreCollisions;

        internal bool INTERNAL_doRender = true;

        public Shell(Vector3 position, Vector3 velocity, int ricochets = 0, HomingProperties homing = default)
        {
            this.ricochets = ricochets;
            this.position = position;
            Model = GameResources.GetGameResource<Model>("Assets/bullet");
            Projection = TankGame.GameProjection;
            View = TankGame.GameView;
            World = Matrix.CreateTranslation(position);
            _shellTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/bullet");

            homingProperties = homing;

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

            hurtbox2d = new((int)(position.X - 3), (int)(position.Z - 3), 6, 6);

            if (!WPTR.InMission)
                return;

            if (!INTERNAL_ignoreCollisions)
            {
                if (position.X < MapRenderer.MIN_X || position.X > MapRenderer.MAX_X)
                    if (ricochets > 0)
                        Ricochet(true);
                    else
                        Destroy();
                if (position.Z < MapRenderer.MIN_Y || position.Z > MapRenderer.MAX_Y)
                    if (ricochets > 0)
                        Ricochet(false);
                    else
                        Destroy();

                var dummyVel = Velocity2D;

                Collision.HandleCollisionSimple_ForBlocks(hurtbox2d, ref dummyVel, ref position, out var dir, false);

                if (lifeTime <= 5 && Cube.cubes.Any(cu => cu is not null && cu.collider.Intersects(hurtbox)))
                    Destroy(false);

                switch (dir)
                {
                    case Collision.CollisionDirection.Up:
                        if (ricochets > 0)
                            Ricochet(false);
                        else
                            Destroy();
                        break;
                    case Collision.CollisionDirection.Down:
                        if (ricochets > 0)
                            Ricochet(false);
                        else
                            Destroy();
                        break;
                    case Collision.CollisionDirection.Left:
                        if (ricochets > 0)
                            Ricochet(true);
                        else
                            Destroy();
                        break;
                    case Collision.CollisionDirection.Right:
                        if (ricochets > 0)
                            Ricochet(true);
                        else
                            Destroy();
                        break;
                }
            }
            lifeTime++;

            if (lifeTime > homingProperties.cooldown)
            {
                if (owner != null)
                {
                    foreach (var target in WPTR.AllTanks.Where(tank => tank is not null && tank.Team != owner.Team && Vector3.Distance(position, tank.position) <= homingProperties.radius))
                    {
                        float dist = Vector3.Distance(position, target.position);

                        velocity.X += (target.position.X - position.X) * homingProperties.power / dist;
                        velocity.Z += (target.position.Z - position.Z) * homingProperties.power / dist;

                        Vector3 trueSpeed = Vector3.Normalize(velocity) * homingProperties.speed;


                        velocity = trueSpeed;
                    }
                }
            }
            if (!INTERNAL_ignoreCollisions)
                CheckCollisions();
        }

        /// <summary>
        /// Ricochets this <see cref="Shell"/>. If <paramref name="horizontal"/> is true, it will ricochet off of a horizontal axis.
        /// </summary>
        /// <param name="horizontal">Is this ricochet horizontal?</param>
        public void Ricochet(bool horizontal)
        {
            if (horizontal)
                velocity.X = -velocity.X;
            else 
                velocity.Z = -velocity.Z;

            var sound = GameResources.GetGameResource<SoundEffect>("Assets/sounds/bullet_ricochet");

            SoundPlayer.PlaySoundInstance(sound, SoundContext.Effect, 0.5f);

            ricochets--;
        }

        public void CheckCollisions()
        {
            foreach (var tank in WPTR.AllAITanks.Where(tnk => tnk is not null))
            {
                if (tank.CollisionBox.Intersects(hurtbox))
                {
                    if (owner != null)
                    {
                        if (tank.Team == owner.Team && tank != owner && tank.Team != Team.NoTeam)
                            Destroy();
                        else
                        {
                            Destroy();
                            tank.Destroy();
                        }
                    }
                    else
                    {
                        Destroy();
                        tank.Destroy();
                    }
                }
            }
            foreach (var tank in WPTR.AllPlayerTanks.Where(tnk => tnk is not null))
            {
                if (tank.CollisionBox.Intersects(hurtbox))
                {
                    if (tank.Team == owner.Team && tank != owner)
                        Destroy();
                    else
                    {
                        Destroy();
                        tank.Destroy();
                    }
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

        /// <summary>
        /// Destroys this <see cref="Shell"/>.
        /// </summary>
        /// <param name="playSound">Whether or not to play the sound of the bullet being destroyed</param>
        public void Destroy(bool playSound = true)
        {
            if (!INTERNAL_ignoreCollisions)
            {
                if (playSound)
                {
                    var sfx = SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>("Assets/sounds/bullet_destroy"), SoundContext.Effect, 0.5f);
                    sfx.Pitch = -0.2f;
                }
                if (owner != null)
                    owner.OwnedShellCount--;
                AllShells[worldId] = null;
            }
        }


        internal void Render()
        {
            if (INTERNAL_doRender)
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

                        effect.SetDefaultGameLighting_IngameEntities();
                    }
                    mesh.Draw();
                }
            }
        }
    }
}
