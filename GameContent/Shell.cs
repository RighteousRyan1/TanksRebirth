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
            public float Power;
            public float Radius;
            public float Speed;
            public int Cooldown;

            public Vector2 Target;

            public bool HeatSeeks;
        }

        /// <summary>The maximum shells allowed at any given time.</summary>
        private static int MaxShells = 1500;
        public static Shell[] AllShells { get; } = new Shell[MaxShells];

        /// <summary>The <see cref="Tank"/> which shot this <see cref="Shell"/>.</summary>
        public Tank Owner;

        public Vector2 Position2D => Position.FlattenZ();
        public Vector2 Velocity2D => Velocity.FlattenZ();
        /// <summary>How many times this <see cref="Shell"/> can hit walls.</summary>
        public uint RicochetsLeft;
        public float Rotation;

        /// <summary>The homing properties of this <see cref="Shell"/>.</summary>
        public HomingProperties HomeProperties = default;

        public Vector3 Position;
        public Vector3 Velocity;

        public Matrix View;
        public Matrix Projection;
        public Matrix World;

        public Model Model;

        private SoundEffectInstance _shootSound;

        /// <summary>The hurtbox on the 2D backing map for the game.</summary>
        public Rectangle Hitbox;
        /// <summary>Whether or not this shell should emit flames from behind it.</summary>
        public bool Flaming { get; set; }
        public bool LeavesTrail { get; set; }
        public bool EmitsSmoke { get; set; } = true;

        private Texture2D _shellTexture;

        private int _id;

        /// <summary>How long this shell has existed in the world.</summary>
        public int LifeTime;

        public bool CanFriendlyFire = true;

        // private Particle _flame;

        public readonly ShellType Tier;

        private SoundEffectInstance _loopingSound;

        public bool IsDestructible { get; set; } = true;

        /// <summary>
        /// Creates a new <see cref="Shell"/>.
        /// </summary>
        /// <param name="position">The position of the created <see cref="Shell"/>.</param>
        /// <param name="velocity">The velocity of the created <see cref="Shell"/>.</param>
        /// <param name="ricochets">How many times the newly created <see cref="Shell"/> can ricochet.</param>
        /// <param name="homing">Whether or not the newly created <see cref="Shell"/> homes in on enemies.</param>
        public Shell(Vector3 position, Vector3 velocity, ShellType tier, Tank owner, uint ricochets = 0, HomingProperties homing = default, bool useDarkTexture = false, bool playSpawnSound = true)
        {
            Tier = tier;
            RicochetsLeft = ricochets;
            Position = position;
            Model = GameResources.GetGameResource<Model>("Assets/bullet");

            if (tier == ShellType.Supressed || tier == ShellType.Explosive)
                useDarkTexture = true;

            if (tier == ShellType.Explosive)
                IsDestructible = false;

            _shellTexture = useDarkTexture ? GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/explosive_bullet") : GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/bullet");

            HomeProperties = homing;
            this.Owner = owner;

            // if explosive, black

            Velocity = velocity;

            if (Tier == ShellType.Rocket)
            {
                Flaming = true;
                _loopingSound = SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_rocket_loop"), SoundContext.Effect, 0.3f);
                _loopingSound.IsLooped = true;
            }
            if (Tier == ShellType.TrailedRocket)
            {
                // MakeTrail();
                EmitsSmoke = false;
                LeavesTrail = true;
                Flaming = true;
                _loopingSound = SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_ricochet_rocket_loop"), SoundContext.Effect, 0.3f);
                _loopingSound.IsLooped = true;
            }

            if (owner is not null)
            {
                _shootSound = Tier switch
                {
                    ShellType.Player => SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_regular_1"), SoundContext.Effect, 0.3f),
                    ShellType.Standard => SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_regular_2"), SoundContext.Effect, 0.3f),
                    ShellType.Rocket => SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_rocket"), SoundContext.Effect, 0.3f),
                    ShellType.TrailedRocket => SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_ricochet_rocket"), SoundContext.Effect, 0.3f),
                    ShellType.Supressed => SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_silencer"), SoundContext.Effect, 0.3f),
                    ShellType.Explosive => SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_regular_2"), SoundContext.Effect, 0.3f),
                    _ => throw new NotImplementedException()
                };
                _shootSound.Pitch = owner.Properties.ShootPitch;
            }

            int index = Array.IndexOf(AllShells, AllShells.First(shell => shell is null));

            _id = index;

            AllShells[index] = this;
        }

        internal void Update()
        {
            Rotation = Velocity.ToRotation() - MathHelper.PiOver2;
            Position += Velocity * 0.62f;
            World = Matrix.CreateFromYawPitchRoll(-Rotation, 0, 0)
                * Matrix.CreateTranslation(Position);
            Projection = TankGame.GameProjection;
            View = TankGame.GameView;

            Hitbox = new((int)(Position2D.X - 2), (int)(Position2D.Y - 2), 4, 4);

            if (!GameHandler.InMission)
                return;

            if (Position2D.X < MapRenderer.MIN_X || Position2D.X > MapRenderer.MAX_X)
                Ricochet(true);
            if (Position2D.Y < MapRenderer.MIN_Y || Position2D.Y > MapRenderer.MAX_Y)
                Ricochet(false);

            var dummy = Vector2.Zero;

            Collision.HandleCollisionSimple_ForBlocks(Hitbox, Velocity2D, ref dummy, out var dir, out var block, out bool corner, false, (c) => c.IsSolid);

            if (LifeTime <= 5 && Block.AllBlocks.Any(cu => cu is not null && cu.Hitbox.Intersects(Hitbox) && cu.IsSolid))
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
            LifeTime++;

            if (LifeTime > HomeProperties.Cooldown)
            {
                if (Owner != null)
                {
                    foreach (var target in GameHandler.AllTanks)
                    {
                        if (target is not null && target != Owner && Vector2.Distance(Position2D, target.Properties.Position) <= HomeProperties.Radius)
                        {
                            if (target.Properties.Team != Owner.Properties.Team || target.Properties.Team == TankTeam.NoTeam)
                            {
                                if (HomeProperties.HeatSeeks && target.Properties.Velocity != Vector2.Zero)
                                    HomeProperties.Target = target.Properties.Position;
                                if (!HomeProperties.HeatSeeks)
                                    HomeProperties.Target = target.Properties.Position;
                            }
                        }
                    }
                    if (HomeProperties.Target != Vector2.Zero)
                    {
                        bool hits = Collision.DoRaycast(Position2D, HomeProperties.Target, (int)HomeProperties.Radius * 2);

                        if (hits)
                        {
                            float dist = Vector2.Distance(Position2D, HomeProperties.Target);

                            Velocity.X += GameUtils.DirectionOf(Position2D, HomeProperties.Target).X * HomeProperties.Power / dist;
                            Velocity.Z += GameUtils.DirectionOf(Position2D, HomeProperties.Target).Y * HomeProperties.Power / dist;

                            Vector2 trueSpeed = Vector2.Normalize(Velocity2D) * HomeProperties.Speed;


                            Velocity = trueSpeed.ExpandZ();
                        }
                    }
                }
            }
            CheckCollisions();

            GameHandler.OnMissionEnd += (delay, cxt, extralife) =>
            {
                // _flame?.Destroy();
                _loopingSound?.Stop();
                _shootSound?.Stop();
            };

            int bruh = Flaming ? (int)Math.Round(6 / Velocity2D.Length()) : (int)Math.Round(12 / Velocity2D.Length());
            int nummy = bruh != 0 ? bruh : 5;

            int darkness = 255;

            if (EmitsSmoke)
            {
                if (LifeTime % nummy == 0)
                {
                    var p = ParticleSystem.MakeParticle(Position + new Vector3(0, 0, 5).FlattenZ().RotatedByRadians(Rotation + MathHelper.Pi + GameHandler.GameRand.NextFloat(-0.3f, 0.3f)).ExpandZ(), GameResources.GetGameResource<Texture2D>("Assets/textures/misc/tank_smokes"));
                    p.FaceTowardsMe = false;
                    p.Scale = new(0.3f);
                    // p.color = new Color(50, 50, 50, 150);

                    p.Roll = -TankGame.DEFAULT_ORTHOGRAPHIC_ANGLE;

                    p.isAddative = false;
                    p.Color = new Color(darkness, darkness, darkness, darkness);
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
            }

            if (LeavesTrail)
            {
                /*for (int i = 0; i < 4; i++)
                {
                    var p = ParticleSystem.MakeParticle(Position + new Vector3(0, 0, 5).FlattenZ().RotatedByRadians(rotation + MathHelper.Pi).ExpandZ(), GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/smoketrail"));

                    p.Roll = -MathHelper.PiOver2;
                    p.Scale = new(0.4f, 0.165f, 0.4f); // x is outward from bullet
                                                       // p.Scale = new(1f, 1f, 1f);
                    p.color = Color.Black;
                    p.isAddative = false;
                    // GameHandler.GameRand.NextFloat(-2f, 2f)
                    //p.TextureRotation = -MathHelper.PiOver2;
                    p.TextureOrigin = new(p.Texture.Size().X / 2, 0);

                    p.Pitch = -rotation;
                    p.Yaw = -rotation + (MathHelper.PiOver2 * i);// - MathHelper.PiOver2;

                    p.UniqueBehavior = (part) =>
                    {
                        p.Yaw += 0.05f;
                        p.Opacity -= 0.02f;

                        if (p.Opacity <= 0)
                            p.Destroy();
                    };
                }*/
                var p = ParticleSystem.MakeParticle(Position + new Vector3(0, 0, 5).FlattenZ().RotatedByRadians(Rotation + MathHelper.Pi).ExpandZ(), GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/smoketrail"));

                p.Roll = -MathHelper.PiOver2;
                p.Scale = new(0.4f, 0.25f, 0.4f); // x is outward from bullet
                                                   // p.Scale = new(1f, 1f, 1f);
                p.Color = Color.Gray;
                p.isAddative = false;
                // GameHandler.GameRand.NextFloat(-2f, 2f)
                //p.TextureRotation = -MathHelper.PiOver2;
                p.TextureScale = Velocity.Length() / 10 - 0.2f;
                p.TextureOrigin = new(p.Texture.Size().X / 2, 0);

                p.Pitch = -Rotation - MathHelper.PiOver2;

                p.UniqueBehavior = (part) =>
                {
                    p.Opacity -= 0.02f;

                    if (p.Opacity <= 0)
                        p.Destroy();
                };

                var p2 = ParticleSystem.MakeParticle(Position + new Vector3(0, 0, 5).FlattenZ().RotatedByRadians(Rotation + MathHelper.Pi).ExpandZ(), GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/smoketrail"));

                p2.Roll = -MathHelper.PiOver2;
                p2.Scale = new(/*0.4f*/ Velocity.Length() / 10 - 0.2f, 0.25f, 0.4f); // x is outward from bullet
                                                   // p.Scale = new(1f, 1f, 1f);
                p2.Color = Color.Gray;
                p2.isAddative = false;
                // GameHandler.GameRand.NextFloat(-2f, 2f)
                //p.TextureRotation = -MathHelper.PiOver2;
                p.TextureScale = Velocity.Length() / 10 - 0.2f;
                p2.TextureOrigin = new(p.Texture.Size().X / 2, 0);

                p2.Pitch = -Rotation + MathHelper.PiOver2;

                p2.UniqueBehavior = (part) =>
                {
                    p2.Opacity -= 0.02f;

                    if (p2.Opacity <= 0)
                        p2.Destroy();
                };
            }
            if (Flaming)
            {
                // every 5 frames, create a flame particle that is a bit smaller than the shell which expands away from the shell, then after a while, shrinks

                var p = ParticleSystem.MakeParticle(Position + new Vector3(0, 0, 5).FlattenZ().RotatedByRadians(Rotation + MathHelper.Pi).ExpandZ(), GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/flame"));

                p.Roll = -MathHelper.PiOver2;
                var scaleRand = GameHandler.GameRand.NextFloat(0.5f, 0.75f);
                p.Scale = new(scaleRand, 0.165f, 0.4f); // x is outward from bullet
                p.Color = Color.Orange;
                p.isAddative = false;
                // GameHandler.GameRand.NextFloat(-2f, 2f)
                p.TextureRotation = -MathHelper.PiOver2;

                var rotoff = GameHandler.GameRand.NextFloat(-0.25f, 0.25f);
                p.TextureOrigin = new(p.Texture.Size().X / 2, p.Texture.Size().Y);

                var initialScale = p.Scale;

                p.UniqueBehavior = (par) =>
                {
                    var flat = Position.FlattenZ();

                    var off = flat + new Vector2(0, 0).RotatedByRadians(Rotation);

                    p.Position = off.ExpandZ() + new Vector3(0, 11, 0);

                    p.Pitch = -Rotation - MathHelper.PiOver2 + rotoff;

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

        private void MakeTrail()
        {
            var p = ParticleSystem.MakeParticle(Position + new Vector3(0, 0, 5).FlattenZ().RotatedByRadians(Rotation + MathHelper.Pi).ExpandZ(), GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/smoketrail"));

            p.Roll = -MathHelper.PiOver2;
            p.Scale = new(1f, 0.165f, 0.4f); // x is outward from bullet
                                              // p.Scale = new(1f, 1f, 1f);
            p.Color = Color.Black;
            p.isAddative = false;
            // GameHandler.GameRand.NextFloat(-2f, 2f)
            //p.TextureRotation = -MathHelper.PiOver2;
            p.TextureOrigin = new(0, p.Texture.Size().Y / 2);

            p.UniqueBehavior = (part) =>
            {
                p.Pitch = -Rotation - MathHelper.PiOver2;
                var flat = Position.FlattenZ();

                var off = flat + new Vector2(0, 0).RotatedByRadians(Rotation);
                
                p.Position = off.ExpandZ() + new Vector3(0, 11, 0);
                
                if (p.LifeTime < 120)
                    p.Scale.X += 0.01f;
                else
                    p.Scale.X -= 0.01f;

                if (p.Scale.X < 0)
                    p.Destroy();
            };
            var p2 = ParticleSystem.MakeParticle(Position + new Vector3(0, 0, 5).FlattenZ().RotatedByRadians(Rotation + MathHelper.Pi).ExpandZ(), GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/smoketrail"));

            p2.Roll = -MathHelper.PiOver2;
            p2.Scale = new(1f, 0.165f, 0.4f); // x is outward from bullet
                                             // p.Scale = new(1f, 1f, 1f);
            p2.Color = Color.Black;
            p2.isAddative = false;
            // GameHandler.GameRand.NextFloat(-2f, 2f)
            //p.TextureRotation = -MathHelper.PiOver2;
            p2.TextureOrigin = new(0, p.Texture.Size().Y / 2);

            p2.UniqueBehavior = (part) =>
            {
                p2.Pitch = -Rotation + MathHelper.PiOver2;
                var flat = Position.FlattenZ();

                var off = flat + new Vector2(0, 0).RotatedByRadians(Rotation);

                p2.Position = off.ExpandZ() + new Vector3(0, 11, 0);

                if (p2.LifeTime < 120)
                    p2.Scale.X += 0.01f;
                else
                    p2.Scale.X -= 0.01f;

                if (p2.Scale.X < 0)
                    p2.Destroy();
            };
        }

        /// <summary>
        /// Ricochets this <see cref="Shell"/>.
        /// </summary>
        /// <param name="horizontal">Whether or not the ricochet is done off of a horizontal axis.</param>
        public void Ricochet(bool horizontal)
        {
            if (RicochetsLeft <= 0)
            {
                Destroy();
                return;
            }
            
            //if (LeavesTrail)
            //{
                //MakeTrail();
            //}

            if (horizontal)
                Velocity.X = -Velocity.X;
            else
                Velocity.Z = -Velocity.Z;

            var sound = GameResources.GetGameResource<SoundEffect>("Assets/sounds/bullet_ricochet");

            var s = SoundPlayer.PlaySoundInstance(sound, SoundContext.Effect, 0.5f);

            if (Owner is not null)
            {
                if (Owner.Properties.ShellType == ShellType.TrailedRocket)
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

            RicochetsLeft--;
        }

        public void CheckCollisions()
        {
            foreach (var tank in GameHandler.AllTanks)
            {
                if (tank is not null)
                {
                    if (tank.Properties.CollisionBox2D.Intersects(Hitbox))
                    {
                        if (!CanFriendlyFire)
                        {
                            if (tank.Properties.Team == Owner.Properties.Team && tank != Owner && tank.Properties.Team != TankTeam.NoTeam)
                                Destroy();
                        }
                        else
                        {
                            Destroy();
                            tank.Damage(Owner is AITank ? new TankHurtContext_Bullet(false, RicochetsLeft, Tier, Owner is not null ? Owner.WorldId : -1) : new TankHurtContext_Bullet(true, RicochetsLeft, Tier, Owner is not null ? Owner.WorldId : -1));
                        }
                    }
                }
            }

            foreach (var bullet in AllShells)
            {
                if (bullet is not null && bullet != this)
                {
                    if (bullet.Hitbox.Intersects(Hitbox))
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
            // _shootSound?.Stop();
            _loopingSound = null;
            // _shootSound = null;
            AllShells[_id] = null;
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
            if (Owner != null)
                Owner.Properties.OwnedShellCount--;
            //_flame?.Destroy();
            _loopingSound?.Stop();
            _loopingSound?.Dispose();
            _loopingSound = null;

            if (Owner is not null)
                if (Owner.Properties.ShellType == ShellType.Explosive)
                    new Explosion(Position2D, 7f, Owner, 0.25f);

            Remove();
        }

        internal void Render()
        {
            if (DebugUtils.DebugLevel == 1 && HomeProperties.Speed > 0)
                Collision.DoRaycast(Position2D, HomeProperties.Target, (int)HomeProperties.Radius, true);
            DebugUtils.DrawDebugString(TankGame.spriteBatch, $"RicochetsLeft: {RicochetsLeft}\nTier: {Tier}", GeometryUtils.ConvertWorldToScreen(Vector3.Zero, World, View, Projection) - new Vector2(0, 20), 1, centered: true);

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

                        effect.Texture = _shellTexture;

                        effect.SetDefaultGameLighting_IngameEntities();
                    }
                    mesh.Draw();
                }
            }
        }
    }
}
