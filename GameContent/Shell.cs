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

        /// <summary>The <see cref="Tank"/> which shot this <see cref="Shell"/>.</summary>
        public Tank owner;

        public Vector3 position;
        public Vector3 velocity;
        /// <summary>How many times this <see cref="Shell"/> can hit walls.</summary>
        public int ricochets;
        public float rotation;

        /// <summary>The homing properties of this <see cref="Shell"/>.</summary>
        public HomingProperties homingProperties = default;

        public Vector2 Position2D => position.FlattenZ();
        public Vector2 Velocity2D => velocity.FlattenZ();

        public Matrix View;
        public Matrix Projection;
        public Matrix World;

        public Model Model;

        /// <summary>The <see cref="BoundingBox"/> of this <see cref="Shell"/> determining the size of its hurtbox/hitbox.</summary>
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

        /// <summary>
        /// Creates a new <see cref="Shell"/>.
        /// </summary>
        /// <param name="position">The position of the created <see cref="Shell"/>.</param>
        /// <param name="velocity">The velocity of the created <see cref="Shell"/>.</param>
        /// <param name="ricochets">How many times the newly created <see cref="Shell"/> can ricochet.</param>
        /// <param name="homing">Whether or not the newly created <see cref="Shell"/> homes in on enemies.</param>
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

            int index = Array.IndexOf(AllShells, AllShells.First(shell => shell is null));

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

            if (!GameHandler.InMission)
                return;

            if (!INTERNAL_ignoreCollisions)
            {
                if (position.X < MapRenderer.MIN_X || position.X > MapRenderer.MAX_X)
                    Ricochet(true);
                if (position.Z < MapRenderer.MIN_Y || position.Z > MapRenderer.MAX_Y)
                    Ricochet(false);

                var dummyVel = Velocity2D;

                Collision.HandleCollisionSimple_ForBlocks(hurtbox2d, ref dummyVel, ref position, out var dir, false);

                if (lifeTime <= 5 && Cube.cubes.Any(cu => cu is not null && cu.collider.Intersects(hurtbox)))
                    Destroy(false);

                switch (dir)
                {
                    case Collision.CollisionDirection.Up:
                        Ricochet(false);
                        break;
                    case Collision.CollisionDirection.Down:
                        Ricochet(false);
                        break;
                    case Collision.CollisionDirection.Left:
                        Ricochet(true);
                        break;
                    case Collision.CollisionDirection.Right:
                        Ricochet(true);
                        break;
                }
            }
            lifeTime++;

            if (lifeTime > homingProperties.cooldown)
            {
                if (owner != null)
                {
                    foreach (var target in GameHandler.AllTanks)
                    {
                        if (target is not null && target.Team != owner.Team && Vector3.Distance(position, target.position) <= homingProperties.radius)
                        {
                            float dist = Vector3.Distance(position, target.position);

                            velocity.X += (target.position.X - position.X) * homingProperties.power / dist;
                            velocity.Z += (target.position.Z - position.Z) * homingProperties.power / dist;

                            Vector3 trueSpeed = Vector3.Normalize(velocity) * homingProperties.speed;


                            velocity = trueSpeed;
                        }
                    }
                }
            }
            if (!INTERNAL_ignoreCollisions)
                CheckCollisions();

            if (lifeTime % 15 == 0 && !INTERNAL_ignoreCollisions)
            {
                var p = ParticleSystem.MakeParticle(position, GameResources.GetGameResource<Texture2D>("Assets/textures/misc/tank_smoke"));
                p.FaceTowardsMe = false;
                p.Scale = 50f;

                p.UniqueBehavior = (p) =>
                {
                    if (p.Opacity <= 0)
                        p.Destroy();

                    if (p.Opacity > 0)  
                        p.Opacity -= 0.01f;
                    p.position.Y += 0.1f;
                };
            }
        }

        /// <summary>
        /// Ricochets this <see cref="Shell"/>.
        /// </summary>
        /// <param name="horizontal">Whether or not the ricochet is done off of a horizontal axis.</param>
        public void Ricochet(bool horizontal)
        {
            if (ricochets <= 0)
            {
                Destroy();
                return;
            }

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
            foreach (var tank in GameHandler.AllAITanks)
            {
                if (tank is not null)
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
            }
            foreach (var tank in GameHandler.AllPlayerTanks)
            {
                if (tank is not null)
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
            }

            foreach (var bullet in AllShells)
            {
                if (bullet is not null && bullet != this)
                {
                    if (bullet.hurtbox.Intersects(hurtbox))
                    {
                        bullet.Destroy();
                        Destroy();
                    }
                }
            }
        }

        /// <summary>
        /// Destroys this <see cref="Shell"/>.
        /// </summary>
        /// <param name="playSound">Whether or not to play the bullet destruction sound.</param>
        public void Destroy(bool playSound = true)
        {
            if (!INTERNAL_ignoreCollisions)
            {
                if (playSound)
                {
                    var sfx = SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>("Assets/sounds/bullet_destroy"), SoundContext.Effect, 0.5f);
                    sfx.Pitch = new Random().NextFloat(-0.1f, 0.1f);
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
