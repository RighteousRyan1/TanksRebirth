using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using TanksRebirth.Enums;
using TanksRebirth.GameContent.GameMechanics;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Properties;
using TanksRebirth.Graphics;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Framework;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent
{
    public class Shell
    {
        public delegate void BlockRicochetDelegate(ref Block block, Shell shell);
        /// <summary>Only called when it bounces from wall-bounce code.</summary>
        public static event BlockRicochetDelegate OnRicochetWithBlock;
        public delegate void RicochetDelegate(Shell shell);
        /// <summary>Only called when it bounces from wall-bounce code.</summary>
        public static event RicochetDelegate OnRicochet;
        public delegate void PostUpdateDelegate(Shell shell);
        public static event PostUpdateDelegate OnPostUpdate;
        public delegate void PostRenderDelegate(Shell shell);
        public static event PostRenderDelegate OnPostRender;
        public delegate void DestroyDelegate(Shell shell, DestructionContext context);
        public static event DestroyDelegate OnDestroy;

        public enum DestructionContext
        {
            WithObstacle,
            WithMine,
            WithFriendlyTank,
            WithHostileTank,
            WithShell,
            WithExplosion
        }
        /// <summary>A structure that allows you to give a <see cref="Shell"/> homing properties.</summary>
        public struct HomingProperties {
            public float Power;
            public float Radius;
            public float Speed;
            public int Cooldown;

            public Vector2 Target;

            public bool HeatSeeks;
        }

        /// <summary>The maximum shells allowed at any given time.</summary>
        private const int MaxShells = 1500;
        public static Shell[] AllShells { get; } = new Shell[MaxShells];

        /// <summary>The <see cref="Tank"/> which shot this <see cref="Shell"/>.</summary>
        public Tank Owner;

        public Vector2 Position2D => Position.FlattenZ();
        public Vector2 Velocity2D => Velocity.FlattenZ();
        /// <summary>How many times this <see cref="Shell"/> can hit walls.</summary>
        public uint RicochetsRemaining;
        public uint Ricochets;
        public float Rotation;

        /// <summary>The homing properties of this <see cref="Shell"/>.</summary>
        public HomingProperties HomeProperties = default;

        public Vector3 Position;
        public Vector3 Velocity;

        public Matrix View;
        public Matrix Projection;
        public Matrix World;

        public Model Model;

        private OggAudio _shootSound;

        /// <summary>The hurtbox on the 2D backing map for the game.</summary>
        public Rectangle Hitbox => new((int)(Position2D.X - 2), (int)(Position2D.Y - 2), 4, 4);
        /// <summary>The hurtcircle on the 2D backing map for the game.</summary>
        public Circle HitCircle => new() { Center = Position2D, Radius = 4 };
        /// <summary>Whether or not this shell should emit flames from behind it.</summary>
        public bool Flaming { get; set; }
        public Color FlameColor { get; set; } = Color.Orange;
        public bool LeavesTrail { get; set; }
        public Color TrailColor { get; set; } = Color.Gray;
        public bool EmitsSmoke { get; set; } = true;
        public Color SmokeColor { get; set; } = new Color(255, 255, 255, 255);

        private Texture2D _shellTexture;
        public int Id { get; private set; }
        private float _wallRicCooldown;
        /// <summary>How long this shell has existed in the world.</summary>
        public float LifeTime;
        public bool CanFriendlyFire = true;
        // private Particle _flame;
        public readonly int Type;
        private OggAudio _loopingSound;
        public bool IsDestructible { get; set; } = true;
        public void ReassignId(int newId) => Id = newId;

        /// <summary>
        /// Creates a new <see cref="Shell"/>.
        /// </summary>
        /// <param name="position">The position of the created <see cref="Shell"/>.</param>
        /// <param name="velocity">The velocity of the created <see cref="Shell"/>.</param>
        /// <param name="type">The type of <see cref="Shell"/> to be fired.</param>
        /// <param name="owner">Which <see cref="Tank"/> owns this <see cref="Shell"/>.</param>
        /// <param name="ricochets">How many times the newly created <see cref="Shell"/> can ricochet.</param>
        /// <param name="homing">Whether or not the newly created <see cref="Shell"/> homes in on enemies.</param>
        public Shell(Vector3 position, Vector3 velocity, int type, Tank owner, uint ricochets = 0, HomingProperties homing = default, bool useDarkTexture = false, bool playSpawnSound = true)
        {
            Type = type;
            RicochetsRemaining = ricochets;
            Position = position;
            Model = GameResources.GetGameResource<Model>("Assets/bullet");

            if (type == ShellID.Supressed || type == ShellID.Explosive)
                useDarkTexture = true;

            if (type == ShellID.Explosive)
                IsDestructible = false;

            _shellTexture = useDarkTexture ? GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/explosive_bullet") : GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/bullet");

            HomeProperties = homing;
            Owner = owner;

            // if explosive, black

            Velocity = velocity;

            if (Type == ShellID.Rocket)
            {
                Flaming = true;
                _loopingSound = SoundPlayer.PlaySoundInstance("Assets/sounds/tnk_shoot_rocket_loop", SoundContext.Effect, 0.3f, gameplaySound: true);
                _loopingSound.Instance.IsLooped = true;
            }
            if (Type == ShellID.TrailedRocket)
            {
                // MakeTrail();
                EmitsSmoke = false;
                LeavesTrail = true;
                Flaming = true;
                _loopingSound = SoundPlayer.PlaySoundInstance("Assets/sounds/tnk_shoot_ricochet_rocket_loop", SoundContext.Effect, 0.3f, gameplaySound: true);
                _loopingSound.Instance.IsLooped = true;
            }

            if (owner is not null)
            {
                _shootSound = Type switch
                {
                    ShellID.Player => SoundPlayer.PlaySoundInstance("Assets/sounds/tnk_shoot_regular_1", SoundContext.Effect, 0.3f, gameplaySound: true),
                    ShellID.Standard => SoundPlayer.PlaySoundInstance("Assets/sounds/tnk_shoot_regular_2", SoundContext.Effect, 0.3f, gameplaySound: true),
                    ShellID.Rocket => SoundPlayer.PlaySoundInstance("Assets/sounds/tnk_shoot_rocket", SoundContext.Effect, 0.3f, gameplaySound: true),
                    ShellID.TrailedRocket => SoundPlayer.PlaySoundInstance("Assets/sounds/tnk_shoot_ricochet_rocket", SoundContext.Effect, 0.3f, gameplaySound: true),
                    ShellID.Supressed => SoundPlayer.PlaySoundInstance("Assets/sounds/tnk_shoot_silencer", SoundContext.Effect, 0.3f, gameplaySound: true),
                    ShellID.Explosive => SoundPlayer.PlaySoundInstance("Assets/sounds/tnk_shoot_regular_2", SoundContext.Effect, 0.3f, gameplaySound: true),
                    _ => throw new NotImplementedException()
                };
                _shootSound.Instance.Pitch = MathHelper.Clamp(owner.Properties.ShootPitch, -1, 1);
            }

            GameProperties.OnMissionEnd += StopSounds;
            TankGame.OnFocusLost += TankGame_OnFocusLost;
            TankGame.OnFocusRegained += TankGame_OnFocusRegained;

            int index = Array.IndexOf(AllShells, null);

            Id = index;

            AllShells[index] = this;

            if (owner != null)
            {
                var idx = Array.IndexOf(Owner.OwnedShells, null);
                if (idx > -1)
                    Owner.OwnedShells[idx] = this;
            }
        }

        private void StopSounds(int delay, MissionEndContext context, bool result1up)
        {
            _loopingSound?.Instance?.Stop();
            _shootSound?.Instance?.Stop();
        }

        internal void Update()
        {
            if (!MapRenderer.ShouldRender)
                return;

            Rotation = Velocity2D.ToRotation() - MathHelper.PiOver2;
            Position += Velocity * 0.62f * TankGame.DeltaTime;
            World = Matrix.CreateFromYawPitchRoll(-Rotation, 0, 0)
                * Matrix.CreateTranslation(Position);

            if (_wallRicCooldown <= 0) {
                if (Position2D.X < MapRenderer.MIN_X || Position2D.X > MapRenderer.MAX_X) {
                    OnRicochet?.Invoke(this);
                    Ricochet(true);

                    _wallRicCooldown = 5;
                }
                if (Position2D.Y < MapRenderer.MIN_Y || Position2D.Y > MapRenderer.MAX_Y) {
                    OnRicochet?.Invoke(this);
                    Ricochet(false);

                    _wallRicCooldown = 5;
                }
            }
            else
                _wallRicCooldown -= TankGame.DeltaTime;

            var dummy = Vector2.Zero;

            Collision.HandleCollisionSimple_ForBlocks(Hitbox, Velocity2D, ref dummy, out var dir, out var block, out bool corner, false, (c) => c.IsSolid);

            if (LifeTime <= 5 && (dir != CollisionDirection.None || corner))
                Destroy(DestructionContext.WithObstacle);
            if (corner)
                Destroy(DestructionContext.WithObstacle);
            if (_wallRicCooldown <= 0)
            {
                switch (dir)
                {
                    case CollisionDirection.Up:
                    case CollisionDirection.Down:
                        OnRicochetWithBlock?.Invoke(ref block, this);
                        Ricochet(false);
                        break;
                    case CollisionDirection.Left:
                    case CollisionDirection.Right:
                        OnRicochetWithBlock?.Invoke(ref block, this);
                        Ricochet(true);
                        break;
                }
            }
            LifeTime += TankGame.DeltaTime;

            if (LifeTime > HomeProperties.Cooldown)
            {
                if (Owner != null)
                {
                    foreach (var target in GameHandler.AllTanks)
                    {
                        if (target is not null && target != Owner && Vector2.Distance(Position2D, target.Position) <= HomeProperties.Radius)
                        {
                            if (target.Team != Owner.Team || target.Team == TeamID.NoTeam)
                            {
                                if (HomeProperties.HeatSeeks && target.Velocity != Vector2.Zero)
                                    HomeProperties.Target = target.Position;
                                if (!HomeProperties.HeatSeeks)
                                    HomeProperties.Target = target.Position;
                            }
                        }
                    }
                    if (HomeProperties.Target != Vector2.Zero)
                    {
                        bool hits = Collision.DoRaycast(Position2D, HomeProperties.Target, (int)HomeProperties.Radius * 2);

                        if (hits)
                        {
                            float dist = Vector2.Distance(Position2D, HomeProperties.Target);

                            Velocity.X += MathUtils.DirectionOf(Position2D, HomeProperties.Target).X * HomeProperties.Power / dist;
                            Velocity.Z += MathUtils.DirectionOf(Position2D, HomeProperties.Target).Y * HomeProperties.Power / dist;

                            Vector2 trueSpeed = Vector2.Normalize(Velocity2D) * HomeProperties.Speed;


                            Velocity = trueSpeed.ExpandZ();
                        }
                    }
                }
            }
            CheckCollisions();

            float bruh = Flaming ? (int)Math.Round(6 / Velocity2D.Length()) : (int)Math.Round(12 / Velocity2D.Length());
            float nummy = bruh != 0 ? bruh : 5;

            if (EmitsSmoke)
            {
                if (LifeTime % nummy <= TankGame.DeltaTime)
                {
                    var p = GameHandler.ParticleSystem.MakeParticle(Position + new Vector3(0, 0, 5).FlattenZ().RotatedByRadians(Rotation + MathHelper.Pi + GameHandler.GameRand.NextFloat(-0.3f, 0.3f)).ExpandZ(), GameResources.GetGameResource<Texture2D>("Assets/textures/misc/tank_smokes"));
                    p.FaceTowardsMe = false;
                    p.Scale = new(0.3f);
                    // p.color = new Color(50, 50, 50, 150);

                    p.Roll = -TankGame.DEFAULT_ORTHOGRAPHIC_ANGLE;
                    // 
                    p.HasAddativeBlending = false;
                    p.Color = SmokeColor;
                    p.Alpha = 0.5f;

                    p.UniqueBehavior = (p) =>
                    {
                        if (p.Alpha <= 0)
                            p.Destroy();

                        if (p.Alpha > 0)
                            p.Alpha -= (Flaming ? 0.03f : 0.02f) * TankGame.DeltaTime;

                        GeometryUtils.Add(ref p.Scale, 0.0075f * TankGame.DeltaTime);
                    };
                }
            }
            if (LeavesTrail)
            {
                if (LifeTime % (nummy / 2) <= TankGame.DeltaTime)
                {
                    var p = GameHandler.ParticleSystem.MakeParticle(Position + new Vector3(0, 0, 5).FlattenZ().RotatedByRadians(Rotation + MathHelper.Pi).ExpandZ(), GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/smoketrail"));

                    p.Roll = -MathHelper.PiOver2;
                    p.Scale = new(0.4f, 0.25f, 0.4f); // x is outward from bullet
                                                      // p.Scale = new(1f, 1f, 1f);
                    p.Color = TrailColor;
                    p.HasAddativeBlending = false;
                    // GameHandler.GameRand.NextFloat(-2f, 2f)
                    //p.TextureRotation = -MathHelper.PiOver2;
                    p.TextureScale = new(Velocity.Length() / 10 - 0.2f);
                    p.Origin2D = new(p.Texture.Size().X / 2, 0);

                    p.Pitch = -Rotation - MathHelper.PiOver2;

                    p.UniqueBehavior = (part) =>
                    {
                        p.Alpha -= 0.02f * TankGame.DeltaTime;

                        if (p.Alpha <= 0)
                            p.Destroy();
                    };

                    var p2 = GameHandler.ParticleSystem.MakeParticle(Position + new Vector3(0, 0, 5).FlattenZ().RotatedByRadians(Rotation + MathHelper.Pi).ExpandZ(), GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/smoketrail"));

                    p2.Roll = -MathHelper.PiOver2;
                    p2.Scale = new(0.4f /*Velocity.Length() / 10 - 0.2f*/, 0.25f, 0.4f); // x is outward from bullet
                                                                                         // p.Scale = new(1f, 1f, 1f);
                    p2.Color = TrailColor;
                    p2.HasAddativeBlending = false;
                    // GameHandler.GameRand.NextFloat(-2f, 2f)
                    //p.TextureRotation = -MathHelper.PiOver2;
                    p.TextureScale = new(Velocity.Length() / 10 - 0.2f);
                    p2.Origin2D = new(p.Texture.Size().X / 2, 0);

                    p2.Pitch = -Rotation + MathHelper.PiOver2;

                    p2.UniqueBehavior = (part) =>
                    {
                        p2.Alpha -= 0.02f * TankGame.DeltaTime;

                        if (p2.Alpha <= 0)
                            p2.Destroy();
                    };
                }
            }
            if (Flaming)
            {
                if (LifeTime % 1 <= TankGame.DeltaTime)
                {
                    var p = GameHandler.ParticleSystem.MakeParticle(Position + new Vector3(0, 0, 5).FlattenZ().RotatedByRadians(Rotation + MathHelper.Pi).ExpandZ(), GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/flame"));

                    p.Roll = -MathHelper.PiOver2;
                    var scaleRand = GameHandler.GameRand.NextFloat(0.5f, 0.75f);
                    p.Scale = new(scaleRand, 0.165f, 0.4f); // x is outward from bullet
                    p.Color = FlameColor;
                    p.HasAddativeBlending = false;
                    // GameHandler.GameRand.NextFloat(-2f, 2f)
                    p.Rotation2D = -MathHelper.PiOver2;

                    var rotoff = GameHandler.GameRand.NextFloat(-0.25f, 0.25f);
                    p.Origin2D = new(p.Texture.Size().X / 2, p.Texture.Size().Y);

                    var initialScale = p.Scale;

                    p.UniqueBehavior = (par) =>
                    {
                        var flat = Position.FlattenZ();

                        var off = flat + Vector2.Zero.RotatedByRadians(Rotation);

                        p.Position = off.ExpandZ() + new Vector3(0, 11, 0);

                        p.Pitch = -Rotation - MathHelper.PiOver2 + rotoff;

                        //if (TankGame.GameUpdateTime % 2 == 0)
                        //p.Roll = GameHandler.GameRand.NextFloat(0, MathHelper.TwoPi);

                        var scalingConstant = 0.06f;

                        p.Scale.X -= scalingConstant * TankGame.DeltaTime;

                        if (p.Scale.X <= 0)
                            p.Destroy();
                    };
                }
            }
            OnPostUpdate?.Invoke(this);
        }

        private void TankGame_OnFocusRegained(object sender, IntPtr e) 
            => _loopingSound?.Instance?.Resume();

        private void TankGame_OnFocusLost(object sender, IntPtr e)
            => _loopingSound?.Instance?.Pause();

        /// <summary>
        /// Ricochets this <see cref="Shell"/>.
        /// </summary>
        /// <param name="horizontal">Whether or not the ricochet is done off of a horizontal axis.</param>
        public void Ricochet(bool horizontal)
        {
            if (RicochetsRemaining <= 0) {
                Destroy(DestructionContext.WithObstacle);
                return;
            }

            if (horizontal)
                Velocity.X = -Velocity.X;
            else
                Velocity.Z = -Velocity.Z;

            var sound = "Assets/sounds/bullet_ricochet";

            var s = SoundPlayer.PlaySoundInstance(sound, SoundContext.Effect, 0.5f, gameplaySound: true);

            if (Owner is not null)
            {
                if (Owner.Properties.ShellType == ShellID.TrailedRocket)
                {
                    s.Instance.Pitch = GameHandler.GameRand.NextFloat(0.15f, 0.25f);
                    var s2 = SoundPlayer.PlaySoundInstance("Assets/sounds/ricochet_zip", SoundContext.Effect, 0.05f, gameplaySound: true);
                    s2.Instance.Pitch = -0.65f;
                }
                else
                {
                    s.Instance.Pitch = GameHandler.GameRand.NextFloat(-0.05f, 0.05f);
                }
            }
            GameHandler.ParticleSystem.MakeShineSpot(Position, Color.Orange, 0.8f);
            Ricochets++;
            RicochetsRemaining--;
        }
        public void CheckCollisions()
        {
            var cxt = DestructionContext.WithHostileTank;
            foreach (var tank in GameHandler.AllTanks)
            {
                if (tank is not null && !tank.Dead)
                {
                    if (tank.CollisionCircle.Intersects(HitCircle))
                    {
                        if (!CanFriendlyFire)
                        {
                            if (tank.Team == Owner.Team && tank != Owner && tank.Team != TeamID.NoTeam)
                                cxt = DestructionContext.WithFriendlyTank;
                        }
                        else if (Owner != null)
                        {
                            if (tank == Owner)
                                cxt = DestructionContext.WithFriendlyTank;
                            if (tank.Team == Owner.Team && tank != Owner && tank.Team != TeamID.NoTeam)
                                cxt = DestructionContext.WithFriendlyTank;
                            else
                                cxt = DestructionContext.WithHostileTank;
                        }
                        Destroy(cxt);
                        tank.Damage(new TankHurtContext_Shell(Owner is PlayerTank, Ricochets, Type, this));
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
                            bullet.Destroy(DestructionContext.WithShell);
                        if (IsDestructible)
                            Destroy(DestructionContext.WithShell);

                        // if two indestructible bullets come together, destroy them both. too powerful!
                        if (!bullet.IsDestructible && !IsDestructible)
                        {
                            bullet.Destroy(DestructionContext.WithShell);
                            Destroy(DestructionContext.WithShell);
                        }
                    }
                }
            }
        }
        public void Remove() {
            if (Owner != null)
                if (Owner.OwnedShells != null) {
                    var idx = Array.IndexOf(Owner.OwnedShells, this);
                    if (idx > -1)
                        Owner.OwnedShells[idx] = null;
                }

            TankGame.OnFocusLost -= TankGame_OnFocusLost;
            TankGame.OnFocusRegained -= TankGame_OnFocusRegained;
            GameProperties.OnMissionEnd -= StopSounds;
            _loopingSound?.Instance?.Stop();
            // _shootSound?.Stop();
            _loopingSound = null;
            // _shootSound = null;
            AllShells[Id] = null;
        }
        /// <summary>
        /// Destroys this <see cref="Shell"/>.
        /// </summary>
        /// <param name="playSound">Whether or not to play the bullet destruction sound.</param>
        public void Destroy(DestructionContext context, bool playSound = true, bool wasSentByAnotherClient = false)
        {
            _shootSound?.Instance?.Stop(true);
            // ParticleSystem.MakeSparkEmission(Position, 10);
            if (context != DestructionContext.WithHostileTank && context != DestructionContext.WithMine && context != DestructionContext.WithExplosion) {
                if (playSound) {
                    var sfx = SoundPlayer.PlaySoundInstance("Assets/sounds/bullet_destroy", SoundContext.Effect, 0.5f, gameplaySound: true);
                    sfx.Instance.Pitch = GameHandler.GameRand.NextFloat(-0.1f, 0.1f);
                }
                GameHandler.ParticleSystem.MakeSmallExplosion(Position, 8, 10, 1.25f, 15);
            }
            //_flame?.Destroy();
            _loopingSound?.Instance?.Stop();
            _loopingSound?.Dispose();
            _loopingSound = null;

            if (Owner is not null)
            {
                if (Owner.Properties.ShellType == ShellID.Explosive)
                    new Explosion(Position2D, 7f, Owner, 0.25f);
                if (Owner is PlayerTank)
                    // in case the player wants to destroy a mine that may be impeding progress- we don't want to penalize them.
                    if (context == DestructionContext.WithHostileTank || context == DestructionContext.WithMine || context == DestructionContext.WithShell)
                        PlayerTank.PlayerStatistics.ShellHitsThisCampaign++;
            }
            if (!wasSentByAnotherClient)
                Client.SyncShellDestroy(this, context);
            OnDestroy?.Invoke(this, context);
            Remove();
        }

        internal void Render()
        {
            if (!MapRenderer.ShouldRender)
                return;

            Projection = TankGame.GameProjection;
            View = TankGame.GameView;

            if (DebugUtils.DebugLevel == 1 && HomeProperties.Speed > 0)
                Collision.DoRaycast(Position2D, HomeProperties.Target, (int)HomeProperties.Radius, true);
            DebugUtils.DrawDebugString(TankGame.SpriteRenderer, $"RicochetsLeft: {RicochetsRemaining}\nTier: {Type}\nId: {Id}", MatrixUtils.ConvertWorldToScreen(Vector3.Zero, World, View, Projection) - new Vector2(0, 20), 1, centered: true);

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
            OnPostRender?.Invoke(this);
        }
    }
}
