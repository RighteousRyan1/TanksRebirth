using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using WiiPlayTanksRemake.Enums;
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
        public struct HomingProperties
        {
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

        public bool canFriendlyFire = true;

        private Particle _flame;

        public readonly ShellTier Tier;

        /// <summary>
        /// Creates a new <see cref="Shell"/>.
        /// </summary>
        /// <param name="position">The position of the created <see cref="Shell"/>.</param>
        /// <param name="velocity">The velocity of the created <see cref="Shell"/>.</param>
        /// <param name="ricochets">How many times the newly created <see cref="Shell"/> can ricochet.</param>
        /// <param name="homing">Whether or not the newly created <see cref="Shell"/> homes in on enemies.</param>
        public Shell(Vector3 position, Vector3 velocity, ShellTier tier, Tank owner, int ricochets = 0, HomingProperties homing = default, bool playSpawnSound = true)
        {
            Tier = tier;
            this.ricochets = ricochets;
            this.position = position;
            Model = GameResources.GetGameResource<Model>("Assets/bullet");
            Projection = TankGame.GameProjection;
            View = TankGame.GameView;
            World = Matrix.CreateTranslation(position);
            _shellTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/bullet");
            homingProperties = homing;
            this.owner = owner;

            // if explosive, black

            this.velocity = velocity;

            if (Tier == ShellTier.Rocket || Tier == ShellTier.RicochetRocket)
                Flaming = true;

            SoundEffectInstance sfx = Tier switch
            {
                ShellTier.Player => SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_regular_1"), SoundContext.Effect, 0.3f),
                ShellTier.Standard => SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_regular_2"), SoundContext.Effect, 0.3f),
                ShellTier.Rocket => SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_rocket"), SoundContext.Effect, 0.3f),
                ShellTier.RicochetRocket => SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_ricochet_rocket"), SoundContext.Effect, 0.3f),
                ShellTier.Supressed => SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_silencer"), SoundContext.Effect, 0.3f),
                ShellTier.Explosive => SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_regular_2"), SoundContext.Effect, 0.3f),
                _ => throw new NotImplementedException()
            };
            sfx.Pitch = owner.ShootPitch;

            if (Flaming)
            {
                _flame = ParticleSystem.MakeParticle(position, GameResources.GetGameResource<Texture2D>("Assets/textures/misc/bot_hit_half"));

                _flame.rotationX = -MathHelper.PiOver2;
                _flame.Scale = new(0.5f, 0.125f, 0.4f);
                _flame.color = Color.Orange;
                _flame.isAddative = false;
            }

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

            if (_flame is not null)
            {
                _flame.UniqueBehavior = (p) =>
                {
                    var flat = position.FlattenZ();

                    var off = flat + new Vector2(0, -12).RotatedByRadians(rotation);

                    p.position = off.Expand_Z() + new Vector3(0, 11, 0);

                    p.rotationY = -rotation - MathHelper.PiOver2;

                    if (TankGame.GameUpdateTime % 2 == 0)
                        p.rotationX = GameHandler.GameRand.NextFloat(0, MathHelper.TwoPi);
                };
            }


            hurtbox.Max = position + new Vector3(3, 5, 3);
            hurtbox.Min = position - new Vector3(3, 5, 3);

            hurtbox2d = new((int)(position.X - 3), (int)(position.Z - 3), 6, 6);

            if (!GameHandler.InMission)
                return;

            if (position.X < MapRenderer.MIN_X || position.X > MapRenderer.MAX_X)
                Ricochet(true);
            if (position.Z < MapRenderer.MIN_Y || position.Z > MapRenderer.MAX_Y)
                Ricochet(false);

            var dummyVel = Velocity2D;

            Collision.HandleCollisionSimple_ForBlocks(hurtbox2d, ref dummyVel, ref position, out var dir, false, (c) => c.IsSolid);

            if (lifeTime <= 5 && Block.blocks.Any(cu => cu is not null && cu.collider2d.Intersects(hurtbox2d) && cu.IsSolid))
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
            CheckCollisions();

            int bruh = (int)Math.Round(10 / velocity.Length());
            int nummy = bruh != 0 ? bruh : 5;

            if (lifeTime % nummy == 0)
            {
                var p = ParticleSystem.MakeParticle(position + new Vector3(0, 0, 5).FlattenZ().RotatedByRadians(rotation + MathHelper.Pi).Expand_Z(), GameResources.GetGameResource<Texture2D>("Assets/textures/misc/tank_smokes"));
                p.FaceTowardsMe = false;
                p.Scale = new(0.4f);
                p.color = new Color(50, 50, 50, 150);

                p.rotationX = -TankGame.DEFAULT_ORTHOGRAPHIC_ANGLE;

                p.UniqueBehavior = (p) =>
                {
                    if (p.Opacity <= 0)
                        p.Destroy();

                    if (p.Opacity > 0)
                        p.Opacity -= 0.02f;

                    GeometryUtils.Add(ref p.Scale, 0.005f);

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

            var p = ParticleSystem.MakeParticle(position, GameResources.GetGameResource<Texture2D>("Assets/textures/misc/light_star"));
            p.Scale = new(0.8f);
            p.color = Color.Orange;
            p.UniqueBehavior = (part) =>
            {
                GeometryUtils.Add(ref p.Scale, -0.0175f);

                p.Opacity -= 0.025f;

                if (p.Opacity <= 0f || p.Scale.X <= 0f)
                    p.Destroy();
            };

            ricochets--;
        }

        public void CheckCollisions()
        {
            foreach (var tank in GameHandler.AllTanks)
            {
                if (tank is not null)
                {
                    if (tank.CollisionBox2D.Intersects(hurtbox2d))
                    {
                        if (!canFriendlyFire)
                        {
                            if (tank.Team == owner.Team && tank != owner && tank.Team != Team.NoTeam)
                                Destroy();
                        }
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
            if (playSound)
            {
                var sfx = SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>("Assets/sounds/bullet_destroy"), SoundContext.Effect, 0.5f);
                sfx.Pitch = GameHandler.GameRand.NextFloat(-0.1f, 0.1f);
            }
            if (owner != null)
                owner.OwnedShellCount--;
            _flame?.Destroy();
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

                    effect.SetDefaultGameLighting_IngameEntities();
                }
                mesh.Draw();
            }
        }
    }
}
