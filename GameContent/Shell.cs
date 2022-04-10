using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using TanksRebirth.Enums;
using TanksRebirth.GameContent.GameMechanics;
using TanksRebirth.Graphics;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent
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

            public Vector2 target;

            public bool isHeatSeeking;
        }

        /// <summary>The maximum shells allowed at any given time.</summary>
        private static int maxShells = 1500;
        public static Shell[] AllShells { get; } = new Shell[maxShells];

        /// <summary>The <see cref="Tank"/> which shot this <see cref="Shell"/>.</summary>
        public Tank owner;

        public Vector2 Position2D => Position.FlattenZ();
        public Vector2 Velocity2D => Velocity.FlattenZ();
        /// <summary>How many times this <see cref="Shell"/> can hit walls.</summary>
        public int ricochets;
        public float rotation;

        /// <summary>The homing properties of this <see cref="Shell"/>.</summary>
        public HomingProperties homingProperties = default;

        public Vector3 Position;
        public Vector3 Velocity;

        public Matrix View;
        public Matrix Projection;
        public Matrix World;

        public Model Model;

        private SoundEffectInstance _shootSound;

        /// <summary>The hurtbox on the 2D backing map for the game.</summary>
        public Rectangle hitbox;
        /// <summary>Whether or not this shell should emit flames from behind it.</summary>
        public bool Flaming { get; set; }
        public bool LeavesTrail { get; set; }

        public Texture2D _shellTexture;

        private int worldId;

        /// <summary>How long this shell has existed in the world.</summary>
        public int lifeTime;

        public bool canFriendlyFire = true;

        // private Particle _flame;

        public readonly ShellTier Tier;

        private SoundEffectInstance _loopingSound;

        public bool IsDestructible { get; set; } = true;

        /// <summary>
        /// Creates a new <see cref="Shell"/>.
        /// </summary>
        /// <param name="position">The position of the created <see cref="Shell"/>.</param>
        /// <param name="velocity">The velocity of the created <see cref="Shell"/>.</param>
        /// <param name="ricochets">How many times the newly created <see cref="Shell"/> can ricochet.</param>
        /// <param name="homing">Whether or not the newly created <see cref="Shell"/> homes in on enemies.</param>
        public Shell(Vector3 position, Vector3 velocity, ShellTier tier, Tank owner, int ricochets = 0, HomingProperties homing = default, bool useDarkTexture = false,bool playSpawnSound = true)
        {
            Tier = tier;
            this.ricochets = ricochets;
            Position = position;
            Model = GameResources.GetGameResource<Model>("Assets/bullet");

            if (tier == ShellTier.Supressed || tier == ShellTier.Explosive)
                useDarkTexture = true;

            if (tier == ShellTier.Explosive)
                IsDestructible = false;

            _shellTexture = useDarkTexture ? GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/explosive_bullet") : GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/bullet");

            homingProperties = homing;
            this.owner = owner;

            // if explosive, black

            Velocity = velocity;

            if (Tier == ShellTier.Rocket)
            {
                Flaming = true;
                _loopingSound = SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_rocket_loop"), SoundContext.Effect, 0.3f);
                _loopingSound.IsLooped = true;
            }
            if (Tier == ShellTier.RicochetRocket)
            {
                Flaming = true;
                _loopingSound = SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_ricochet_rocket_loop"), SoundContext.Effect, 0.3f);
                _loopingSound.IsLooped = true;
            }

            if (owner is not null)
            {
                _shootSound = Tier switch
                {
                    ShellTier.Player => SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_regular_1"), SoundContext.Effect, 0.3f),
                    ShellTier.Standard => SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_regular_2"), SoundContext.Effect, 0.3f),
                    ShellTier.Rocket => SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_rocket"), SoundContext.Effect, 0.3f),
                    ShellTier.RicochetRocket => SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_ricochet_rocket"), SoundContext.Effect, 0.3f),
                    ShellTier.Supressed => SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_silencer"), SoundContext.Effect, 0.3f),
                    ShellTier.Explosive => SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_regular_2"), SoundContext.Effect, 0.3f),
                    _ => throw new NotImplementedException()
                };
                _shootSound.Pitch = owner.ShootPitch;
            }

            /*if (Flaming)
            {
                _flame = ParticleSystem.MakeParticle(Position, GameResources.GetGameResource<Texture2D>("Assets/textures/misc/bot_hit_half"));

                _flame.Roll = -MathHelper.PiOver2;
                _flame.Scale = new(0.5f, 0.125f, 0.4f);
                _flame.color = Color.Orange;
                _flame.isAddative = false;
            }*/

            int index = Array.IndexOf(AllShells, AllShells.First(shell => shell is null));

            worldId = index;

            AllShells[index] = this;
        }

        internal void Update()
        {
            rotation = Velocity.ToRotation() - MathHelper.PiOver2;
            Position += Velocity * 0.62f;
            World = Matrix.CreateFromYawPitchRoll(-rotation, 0, 0)
                * Matrix.CreateTranslation(Position);
            Projection = TankGame.GameProjection;
            View = TankGame.GameView;

            hitbox = new((int)(Position2D.X - 3), (int)(Position2D.Y - 3), 5, 5);

            if (!GameHandler.InMission)
                return;

            if (Position2D.X < MapRenderer.MIN_X || Position2D.X > MapRenderer.MAX_X)
                Ricochet(true);
            if (Position2D.Y < MapRenderer.MIN_Y || Position2D.Y > MapRenderer.MAX_Y)
                Ricochet(false);

            var dummy = Vector2.Zero;

            Collision.HandleCollisionSimple_ForBlocks(hitbox, Velocity2D, ref dummy, out var dir, out var block, out bool corner, false, (c) => c.IsSolid);

            if (lifeTime <= 5 && Block.AllBlocks.Any(cu => cu is not null && cu.Hitbox.Intersects(hitbox) && cu.IsSolid))
                Destroy(false);

            if (corner)
                Destroy();
            switch (dir)
            {
                case CollisionDirection.Up:
                case CollisionDirection.Down:
                    Ricochet(false);
                    break;
                case CollisionDirection.Left:
                case CollisionDirection.Right:
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
                        if (target is not null && target != owner && Vector2.Distance(Position2D, target.Position) <= homingProperties.radius)
                        {
                            if (target.Team != owner.Team || target.Team == Team.NoTeam)
                            {
                                if (homingProperties.isHeatSeeking && target.Velocity != Vector2.Zero)
                                    homingProperties.target = target.Position;
                                if (!homingProperties.isHeatSeeking)
                                    homingProperties.target = target.Position;
                            }
                        }
                    }
                    if (homingProperties.target != Vector2.Zero)
                    {
                        bool hits = Collision.DoRaycast(Position2D, homingProperties.target, (int)homingProperties.radius * 2);

                        if (hits)
                        {
                            float dist = Vector2.Distance(Position2D, homingProperties.target);

                            Velocity.X += GameUtils.DirectionOf(Position2D, homingProperties.target).X * homingProperties.power / dist;
                            Velocity.Z += GameUtils.DirectionOf(Position2D, homingProperties.target).Y * homingProperties.power / dist;

                            Vector2 trueSpeed = Vector2.Normalize(Velocity2D) * homingProperties.speed;


                            Velocity = trueSpeed.ExpandZ();
                        }
                    }
                }
            }
            CheckCollisions();

            GameHandler.OnMissionEnd += (delay, fatal) =>
            {
                // _flame?.Destroy();
                _loopingSound?.Stop();
                _shootSound?.Stop();
            };

            int bruh = Flaming ? (int)Math.Round(6 / Velocity2D.Length()) : (int)Math.Round(12 / Velocity2D.Length());
            int nummy = bruh != 0 ? bruh : 5;

            int darkness = 255;

            if (lifeTime % nummy == 0)
            {
                var p = ParticleSystem.MakeParticle(Position + new Vector3(0, 0, 5).FlattenZ().RotatedByRadians(rotation + MathHelper.Pi).ExpandZ(), GameResources.GetGameResource<Texture2D>("Assets/textures/misc/tank_smokes"));
                p.FaceTowardsMe = false;
                p.Scale = new(0.3f);
                // p.color = new Color(50, 50, 50, 150);

                p.Roll = -TankGame.DEFAULT_ORTHOGRAPHIC_ANGLE;

                p.isAddative = false;
                p.color = new Color(darkness, darkness, darkness, darkness);
                p.Opacity = 0.5f;

                p.UniqueBehavior = (p) =>
                {
                    if (p.Opacity <= 0)
                        p.Destroy();

                    if (p.Opacity > 0)
                        p.Opacity -= Flaming ? 0.03f : 0.02f;

                    GeometryUtils.Add(ref p.Scale, 0.0075f);
                };
            }
            if (Flaming && !LeavesTrail)
            {
                // every 5 frames, create a flame particle that is a bit smaller than the shell which expands away from the shell, then after a while, shrinks

                var p = ParticleSystem.MakeParticle(Position + new Vector3(0, 0, 5).FlattenZ().RotatedByRadians(rotation + MathHelper.Pi).ExpandZ(), GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/flame"));

                p.Roll = -MathHelper.PiOver2;
                var scaleRand = GameHandler.GameRand.NextFloat(0.5f, 0.75f);
                p.Scale = new(scaleRand, 0.165f, 0.4f); // x is outward from bullet
                p.color = Color.Orange;
                p.isAddative = false;
                // GameHandler.GameRand.NextFloat(-2f, 2f)
                p.TextureRotation = -MathHelper.PiOver2;

                var rotoff = GameHandler.GameRand.NextFloat(-0.25f, 0.25f);
                p.TextureOrigin = new(p.Texture.Size().X / 2, p.Texture.Size().Y);

                var initialScale = p.Scale;

                p.UniqueBehavior = (par) =>
                {
                    var flat = Position.FlattenZ();

                    var off = flat + new Vector2(0, 0).RotatedByRadians(rotation);

                    p.position = off.ExpandZ() + new Vector3(0, 11, 0);

                    p.Pitch = -rotation - MathHelper.PiOver2 + rotoff;

                        //if (TankGame.GameUpdateTime % 2 == 0)
                        //p.Roll = GameHandler.GameRand.NextFloat(0, MathHelper.TwoPi);

                    var scalingConstant = 0.06f;

                    p.Scale.X -= scalingConstant;

                    if (p.Scale.X <= 0)
                        p.Destroy();
                };
            }

            TankGame.OnFocusLost += TankGame_OnFocusLost;
            TankGame.OnFocusRegained += TankGame_OnFocusRegained;
        }

        private void TankGame_OnFocusRegained(object sender, IntPtr e) 
            => _loopingSound?.Resume();

        private void TankGame_OnFocusLost(object sender, IntPtr e)
            => _loopingSound?.Pause();

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
                Velocity.X = -Velocity.X;
            else
                Velocity.Z = -Velocity.Z;

            var sound = GameResources.GetGameResource<SoundEffect>("Assets/sounds/bullet_ricochet");

            var s = SoundPlayer.PlaySoundInstance(sound, SoundContext.Effect, 0.5f);

            if (owner is not null)
            {
                if (owner.ShellType == ShellTier.RicochetRocket)
                {
                    s.Pitch = GameHandler.GameRand.NextFloat(0.15f, 0.25f);
                    var s2 = SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>("Assets/sounds/ricochet_zip"), SoundContext.Effect, 0.05f);
                    s2.Pitch = -0.65f;
                }
                else
                {
                    s.Pitch = GameHandler.GameRand.NextFloat(-0.05f, 0.05f);
                }
            }
            ParticleSystem.MakeShineSpot(Position, Color.Orange, 0.8f);

            ricochets--;
        }

        public void CheckCollisions()
        {
            foreach (var tank in GameHandler.AllTanks)
            {
                if (tank is not null)
                {
                    if (tank.CollisionBox2D.Intersects(hitbox))
                    {
                        if (!canFriendlyFire)
                        {
                            if (tank.Team == owner.Team && tank != owner && tank.Team != Team.NoTeam)
                                Destroy();
                        }
                        else
                        {
                            Destroy();
                            tank.Damage(owner is AITank ? TankHurtContext.ByAiBullet : TankHurtContext.ByPlayerBullet);
                        }
                    }
                }
            }

            foreach (var bullet in AllShells)
            {
                if (bullet is not null && bullet != this)
                {
                    if (bullet.hitbox.Intersects(hitbox))
                    {
                        if (bullet.IsDestructible)
                            bullet.Destroy();
                        if (IsDestructible)
                            Destroy();

                        if (!bullet.IsDestructible && !IsDestructible)
                        {
                            bullet.Destroy();
                            Destroy();
                        }
                    }
                }
            }
        }
        public void Remove() {
            TankGame.OnFocusLost -= TankGame_OnFocusLost;
            TankGame.OnFocusRegained -= TankGame_OnFocusRegained;
            _loopingSound?.Stop();
            _shootSound?.Stop();
            _loopingSound = null;
            _shootSound = null;
            AllShells[worldId] = null;
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

            _shootSound?.Stop(true);
            // ParticleSystem.MakeSparkEmission(Position, 10);
            ParticleSystem.MakeSmallExplosion(Position, 8, 10, 1.25f, 15);
            if (owner != null)
                owner.OwnedShellCount--;
            //_flame?.Destroy();
            _loopingSound?.Stop();
            _loopingSound?.Dispose();
            _loopingSound = null;

            if (owner is not null)
                if (owner.ShellType == ShellTier.Explosive)
                    new Explosion(Position2D, 7f, owner, 0.25f);

            Remove();
        }

        internal void Render()
        {
            if (DebugUtils.DebugLevel == 1 && homingProperties.speed > 0)
                Collision.DoRaycast(Position2D, homingProperties.target, (int)homingProperties.radius, true);
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
