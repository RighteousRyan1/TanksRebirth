using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TanksRebirth.Enums;
using TanksRebirth.GameContent.GameMechanics;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Properties;
using TanksRebirth.GameContent.Systems.AI;
using TanksRebirth.GameContent.UI;
using TanksRebirth.Graphics;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Framework;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent;

public class Shell : IAITankDanger {
    public delegate void BlockRicochetDelegate(ref Block block, Shell shell);

    /// <summary>Only called when it bounces from block-bounce code.</summary>
    public static event BlockRicochetDelegate? OnRicochetWithBlock;

    public delegate void RicochetDelegate(Shell shell);

    /// <summary>Only called when it bounces from wall-bounce code.</summary>
    public static event RicochetDelegate? OnRicochet;

    public delegate void PostUpdateDelegate(Shell shell);

    public static event PostUpdateDelegate? OnPostUpdate;

    public delegate void PostRenderDelegate(Shell shell);

    public static event PostRenderDelegate? OnPostRender;

    public delegate void DestroyDelegate(Shell shell, DestructionContext context);

    public static event DestroyDelegate? OnDestroy;

    public enum DestructionContext {
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
    public Tank? Owner;

    public Vector3 Position3D => Position.ExpandZ() + new Vector3(0, 11, 0);
    public Vector3 Velocity3D => Velocity.ExpandZ();

    /// <summary>How many times this <see cref="Shell"/> can hit walls.</summary>
    public uint RicochetsRemaining;

    public uint Ricochets;
    public float Rotation;

    /// <summary>The homing properties of this <see cref="Shell"/>.</summary>
    public HomingProperties HomeProperties = default;

    public Vector2 Position { get; set; }
    public Vector2 Velocity;

    public Matrix View;
    public Matrix Projection;
    public Matrix World;

    public Model Model;

    private OggAudio? _shootSound;

    /// <summary>The hurtbox on the 2D backing map for the game.</summary>
    public Rectangle Hitbox => new((int)(Position.X - 2), (int)(Position.Y - 2), 4, 4);

    /// <summary>The hurtcircle on the 2D backing map for the game.</summary>
    public Circle HitCircle => new() { Center = Position, Radius = 4 };

    /// <summary>Whether or not this shell should emit flames from behind it.</summary>
    public bool Flaming { get; set; }

    public Color FlameColor { get; set; } = Color.Orange;
    public bool LeavesTrail { get; set; }
    public Color TrailColor { get; set; } = Color.Gray;
    public bool EmitsSmoke { get; set; } = true;
    public Color SmokeColor { get; set; } = new Color(255, 255, 255, 255);
    public bool IsPlayerSourced { get; set; }

    private Texture2D? _shellTexture;
    public int Id { get; private set; }
    private float _wallRicCooldown;

    /// <summary>How long this shell has existed in the world.</summary>
    public float LifeTime;

    public bool CanFriendlyFire = true;

    // private Particle _flame;
    public readonly int Type;
    private OggAudio? _loopingSound;
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
    /// <param name="useDarkTexture">Whether or not to use the black texture for this <see cref="Shell"/>.</param>
    /// <param name="playSpawnSound">Play the shooting sound associated with this <see cref="Shell"/>.</param>
    public Shell(Vector2 position, Vector2 velocity, int type, Tank owner, uint ricochets = 0,
        HomingProperties homing = default, bool useDarkTexture = false, bool playSpawnSound = true) {
        Type = type;
        RicochetsRemaining = ricochets;
        Position = position;
        Model = GameResources.GetGameResource<Model>("Assets/bullet");

        AITank.Dangers.Add(this);
        IsPlayerSourced = owner is PlayerTank;

        if (type == ShellID.Supressed || type == ShellID.Explosive)
            useDarkTexture = true;

        if (type == ShellID.Explosive)
            IsDestructible = false;

        _shellTexture = useDarkTexture
            ? GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/explosive_bullet")
            : GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/bullet");

        HomeProperties = homing;
        Owner = owner;

        // if explosive, black

        Velocity = velocity;

        switch (Type) {
            case ShellID.Rocket:
                Flaming = true;
                _loopingSound = SoundPlayer.PlaySoundInstance("Assets/sounds/tnk_shoot_rocket_loop.ogg",
                    SoundContext.Effect, 0.3f, gameplaySound: true);
                _loopingSound.Instance.IsLooped = true;
                break;
            case ShellID.TrailedRocket:
                // MakeTrail();
                EmitsSmoke = false;
                LeavesTrail = true;
                Flaming = true;
                _loopingSound = SoundPlayer.PlaySoundInstance("Assets/sounds/tnk_shoot_ricochet_rocket_loop.ogg",
                    SoundContext.Effect, 0.3f, gameplaySound: true);
                _loopingSound.Instance.IsLooped = true;
                break;
        }

        if (owner is not null) {
            _shootSound = Type switch {
                ShellID.Player => SoundPlayer.PlaySoundInstance("Assets/sounds/tnk_shoot_regular_1.ogg",
                    SoundContext.Effect, 0.3f, gameplaySound: true),
                ShellID.Standard => SoundPlayer.PlaySoundInstance("Assets/sounds/tnk_shoot_regular_2.ogg",
                    SoundContext.Effect, 0.3f, gameplaySound: true),
                ShellID.Rocket => SoundPlayer.PlaySoundInstance("Assets/sounds/tnk_shoot_rocket.ogg",
                    SoundContext.Effect, 0.3f, gameplaySound: true),
                ShellID.TrailedRocket => SoundPlayer.PlaySoundInstance("Assets/sounds/tnk_shoot_ricochet_rocket.ogg",
                    SoundContext.Effect, 0.3f, gameplaySound: true),
                ShellID.Supressed => SoundPlayer.PlaySoundInstance("Assets/sounds/tnk_shoot_silencer.ogg",
                    SoundContext.Effect, 0.3f, gameplaySound: true),
                ShellID.Explosive => SoundPlayer.PlaySoundInstance("Assets/sounds/tnk_shoot_regular_2.ogg",
                    SoundContext.Effect, 0.3f, gameplaySound: true),
                _ => throw new NotImplementedException($"Sound for the shell type {Type} is not implemented, yet."),
            };
            _shootSound.Instance.Pitch = MathHelper.Clamp(owner.Properties.ShootPitch, -1, 1);
        }

        GameProperties.OnMissionEnd += StopSounds;
        TankGame.OnFocusLost += TankGame_OnFocusLost;
        TankGame.OnFocusRegained += TankGame_OnFocusRegained;

        int index = Array.IndexOf(AllShells, null);

        Id = index;

        AllShells[index] = this;

        if (owner == null) return;
        
        var idx = Array.IndexOf(Owner.OwnedShells, null);
        
        if (idx > -1)
            Owner.OwnedShells[idx] = this;
    }

    private void StopSounds(int delay, MissionEndContext context, bool result1up) {
        _loopingSound?.Instance?.Stop();
        _shootSound?.Instance?.Stop();
    }

    internal void Update() {
        if (!MapRenderer.ShouldRender || (!GameProperties.InMission && !MainMenu.Active))
            return;

        Rotation = Velocity.ToRotation() - MathHelper.PiOver2;
        Position += Velocity * 0.62f * TankGame.DeltaTime;
        World = Matrix.CreateFromYawPitchRoll(-Rotation, 0, 0)
                * Matrix.CreateTranslation(Position3D);

        if (_wallRicCooldown <= 0) {
            if (Position.X is < MapRenderer.MIN_X or > MapRenderer.MAX_X) {
                OnRicochet?.Invoke(this);
                Ricochet(true);

                _wallRicCooldown = 5;
            }

            if (Position.Y is < MapRenderer.MIN_Y or > MapRenderer.MAX_Y) {
                OnRicochet?.Invoke(this);
                Ricochet(false);

                _wallRicCooldown = 5;
            }
        }
        else
            _wallRicCooldown -= TankGame.DeltaTime;

        var dummy = Vector2.Zero;

        Collision.HandleCollisionSimple_ForBlocks(Hitbox, Velocity, ref dummy, out var dir, out var block,
            out bool corner, false, (c) => c.IsSolid);

        if (LifeTime <= 5 && (dir != CollisionDirection.None || corner))
            Destroy(DestructionContext.WithObstacle);
        if (corner)
            Destroy(DestructionContext.WithObstacle);
        if (_wallRicCooldown <= 0) {
            switch (dir) {
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

        while (LifeTime > HomeProperties.Cooldown) { // Use loop to reduce nesting smh.
            if (Owner == null)
                break;

            ref var tanksSSpace = ref MemoryMarshal.GetReference((Span
                <Tank>)GameHandler.AllTanks);

            for (var i = 0; i < GameHandler.AllTanks.Length; i++) {
                var target = Unsafe.Add(ref tanksSSpace, i);

                if (target is null || target == Owner ||
                    !(Vector2.Distance(Position, target.Position) <= HomeProperties.Radius)) continue;

                if (target.Team == Owner.Team && target.Team != TeamID.NoTeam) continue;

                if (HomeProperties.HeatSeeks && target.Velocity != Vector2.Zero)
                    HomeProperties.Target = target.Position;
                if (!HomeProperties.HeatSeeks)
                    HomeProperties.Target = target.Position;
            }

            if (HomeProperties.Target != Vector2.Zero) {
                bool hits = Collision.DoRaycast(Position, HomeProperties.Target, (int)HomeProperties.Radius * 2);

                if (hits) {
                    float dist = Vector2.Distance(Position, HomeProperties.Target);

                    Velocity.X += MathUtils.DirectionOf(Position, HomeProperties.Target).X *
                        HomeProperties.Power / dist;
                    Velocity.Y += MathUtils.DirectionOf(Position, HomeProperties.Target).Y *
                        HomeProperties.Power / dist;

                    Vector2 trueSpeed = Vector2.Normalize(Velocity) * HomeProperties.Speed;


                    Velocity = trueSpeed;
                }
            }

            break;
        }

        CheckCollisions();

        var bruh = Flaming ? (int)Math.Round(6 / Velocity.Length()) : (int)Math.Round(12 / Velocity.Length());
        var num = bruh != 0 ? bruh : 5f;

        if (EmitsSmoke)
            RenderSmokeParticle(num);

        if (LeavesTrail)
            RenderLeaveTrail(num);

        if (Flaming)
            RenderFlamingParticle();

        OnPostUpdate?.Invoke(this);
    }

    private void RenderSmokeParticle(float timer) {
        if (!(LifeTime % timer <= TankGame.DeltaTime)) return;

        var p = GameHandler.ParticleSystem.MakeParticle(
            Position3D + new Vector3(0, 0, 5).FlattenZ()
                                           .RotatedByRadians(Rotation + MathHelper.Pi +
                                                             GameHandler.GameRand.NextFloat(-0.3f, 0.3f))
                                           .ExpandZ(),
            GameResources.GetGameResource<Texture2D>("Assets/textures/misc/tank_smokes"));
        p.FaceTowardsMe = false;
        p.Scale = new(0.3f);
        // p.color = new Color(50, 50, 50, 150);

        p.Roll = -TankGame.DEFAULT_ORTHOGRAPHIC_ANGLE;
        // 
        p.HasAddativeBlending = false;
        p.Color = SmokeColor;
        p.Alpha = 0.5f;

        p.UniqueBehavior = (particle) => {
            if (particle.Alpha <= 0)
                particle.Destroy();

            if (particle.Alpha > 0)
                particle.Alpha -= (Flaming ? 0.03f : 0.02f) * TankGame.DeltaTime;

            GeometryUtils.Add(ref particle.Scale, 0.0075f * TankGame.DeltaTime);
        };
    }

    private void RenderLeaveTrail(float timer) {
        if (!(LifeTime % (timer / 2) <= TankGame.DeltaTime)) return;

        var p = GameHandler.ParticleSystem.MakeParticle(
            Position3D + new Vector3(0, 0, 5).FlattenZ().RotatedByRadians(Rotation + MathHelper.Pi).ExpandZ(),
            GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/smoketrail"));

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

        p.UniqueBehavior = (part) => {
            p.Alpha -= 0.02f * TankGame.DeltaTime;

            if (p.Alpha <= 0)
                p.Destroy();
        };

        var p2 = GameHandler.ParticleSystem.MakeParticle(
            Position3D + new Vector3(0, 0, 5).FlattenZ().RotatedByRadians(Rotation + MathHelper.Pi).ExpandZ(),
            GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/smoketrail"));

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

        p2.UniqueBehavior = (part) => {
            p2.Alpha -= 0.02f * TankGame.DeltaTime;

            if (p2.Alpha <= 0)
                p2.Destroy();
        };
    }

    private void RenderFlamingParticle() {
        if (!(0 <= TankGame.DeltaTime)) return;

        var p = GameHandler.ParticleSystem.MakeParticle(
            Position3D + new Vector3(0, 0, 5).FlattenZ().RotatedByRadians(Rotation + MathHelper.Pi).ExpandZ(),
            GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/flame"));

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

        p.UniqueBehavior = (par) => {
            const float scalingConstant = 0.06f;
            var flat = Position;

            var off = flat + Vector2.Zero.RotatedByRadians(Rotation);

            par.Position = off.ExpandZ() + new Vector3(0, 11, 0);

            par.Pitch = -Rotation - MathHelper.PiOver2 + rotoff;

            //if (TankGame.GameUpdateTime % 2 == 0)
            //p.Roll = GameHandler.GameRand.NextFloat(0, MathHelper.TwoPi);


            par.Scale.X -= scalingConstant * TankGame.DeltaTime;

            if (par.Scale.X <= 0)
                par.Destroy();
        };
    }

    private void TankGame_OnFocusRegained(object sender, IntPtr e)
        => _loopingSound?.Instance?.Resume();

    private void TankGame_OnFocusLost(object sender, IntPtr e)
        => _loopingSound?.Instance?.Pause();

    /// <summary>
    /// Ricochets this <see cref="Shell"/>.
    /// </summary>
    /// <param name="horizontal">Whether or not the ricochet is done off of a horizontal axis.</param>
    public void Ricochet(bool horizontal) {
        const string ricochetSound = "Assets/sounds/bullet_ricochet.ogg";

        if (RicochetsRemaining <= 0) {
            Destroy(DestructionContext.WithObstacle);
            return;
        }

        if (horizontal)
            Velocity.X = -Velocity.X;
        else
            Velocity.Y = -Velocity.Y;


        var sound = SoundPlayer.PlaySoundInstance(ricochetSound, SoundContext.Effect, 0.5f, gameplaySound: true);

        if (Owner is not null) {
            if (Owner.Properties.ShellType == ShellID.TrailedRocket) {
                sound.Instance.Pitch = GameHandler.GameRand.NextFloat(0.15f, 0.25f);
                var rocketRSound = SoundPlayer.PlaySoundInstance("Assets/sounds/ricochet_zip.ogg", SoundContext.Effect,
                    0.05f,
                    gameplaySound: true);
                rocketRSound.Instance.Pitch = -0.65f;
            }
            else {
                sound.Instance.Pitch = GameHandler.GameRand.NextFloat(-0.05f, 0.05f);
            }
        }

        GameHandler.ParticleSystem.MakeShineSpot(Position3D, Color.Orange, 0.8f);
        Ricochets++;
        RicochetsRemaining--;
    }

    public void CheckCollisions() {
        var cxt = DestructionContext.WithHostileTank;

        ref var tankSSpace = ref MemoryMarshal.GetReference((Span<Tank>)GameHandler.AllTanks);

        for (var i = 0; i < GameHandler.AllTanks.Length; i++) {
            var tank = Unsafe.Add(ref tankSSpace, i);
            if (tank == null || tank.Dead) continue;

            if (!tank.CollisionCircle.Intersects(HitCircle)) continue;

            if (!CanFriendlyFire) {
                if (tank.Team == Owner?.Team && tank != Owner && tank.Team != TeamID.NoTeam)
                    cxt = DestructionContext.WithFriendlyTank;
            }
            else if (Owner != null) {
                if (tank.Team == Owner?.Team && tank != Owner && tank.Team != TeamID.NoTeam)
                    cxt = DestructionContext.WithFriendlyTank;
                else
                    cxt = DestructionContext.WithHostileTank;
            }

            Destroy(cxt);
            tank.Damage(new TankHurtContextShell(Owner is PlayerTank, Ricochets, Type, this));
        }

        ref var bulletSSpace = ref MemoryMarshal.GetReference((Span<Shell>)AllShells);

        for (var i = 0; i < AllShells.Length; i++) {
            ref var bullet = ref Unsafe.Add(ref bulletSSpace, i);
            if (bullet == null || bullet == this) continue;
            if (!bullet.Hitbox.Intersects(Hitbox)) continue;

            if (bullet.IsDestructible)
                bullet.Destroy(DestructionContext.WithShell);
            if (IsDestructible)
                Destroy(DestructionContext.WithShell);

            // if two indestructible bullets come together, destroy them both. too powerful!
            if (bullet is { IsDestructible: true, } || IsDestructible) continue;

            bullet.Destroy(DestructionContext.WithShell);
            Destroy(DestructionContext.WithShell);
        }
    }

    public void Remove() {
        if (Owner?.OwnedShells != null) {
            var idx = Array.IndexOf(Owner.OwnedShells, this);
            if (idx > -1)
                Owner.OwnedShells[idx] = null;
        }

        TankGame.OnFocusLost -= TankGame_OnFocusLost;
        TankGame.OnFocusRegained -= TankGame_OnFocusRegained;
        GameProperties.OnMissionEnd -= StopSounds;

        _loopingSound?.Instance?.Stop();
        _loopingSound = null;
        AITank.Dangers.Remove(this);
        AllShells[Id] = null;
    }

    /// <summary>
    /// Destroys this <see cref="Shell"/>.
    /// </summary>
    /// <param name="context">The context in which this bullet was destroyed.</param>
    /// <param name="playSound">Whether or not to play the bullet destruction sound.</param>
    /// <param name="wasSentByAnotherClient">Whether or not the Destroy was sent by another client.</param>
    public void Destroy(DestructionContext context, bool playSound = true, bool wasSentByAnotherClient = false) {
        _shootSound?.Instance?.Stop(true);
        // ParticleSystem.MakeSparkEmission(Position, 10);
        if (context != DestructionContext.WithHostileTank && context != DestructionContext.WithMine &&
            context != DestructionContext.WithExplosion) {
            if (playSound) {
                var sfx = SoundPlayer.PlaySoundInstance("Assets/sounds/bullet_destroy.ogg", SoundContext.Effect, 0.5f,
                    gameplaySound: true);
                sfx.Instance.Pitch = GameHandler.GameRand.NextFloat(-0.1f, 0.1f);
            }

            GameHandler.ParticleSystem.MakeSmallExplosion(Position3D, 8, 10, 1.25f, 15);
        }

        _loopingSound?.Instance?.Stop();
        _loopingSound?.Dispose();
        _loopingSound = null;

        if (Owner is not null) {
            if (Owner.Properties.ShellType == ShellID.Explosive)
                new Explosion(Position, 7f, Owner, 0.25f);
            if (Owner is PlayerTank)
                // in case the player wants to destroy a mine that may be impeding progress- we don't want to penalize them.
                if (context == DestructionContext.WithHostileTank || context == DestructionContext.WithMine ||
                    context == DestructionContext.WithShell)
                    PlayerTank.PlayerStatistics.ShellHitsThisCampaign++;
        }

        if (!wasSentByAnotherClient)
            Client.SyncShellDestroy(this, context);
        OnDestroy?.Invoke(this, context);
        Remove();
    }

    internal void Render() {
        if (!MapRenderer.ShouldRender)
            return;

        Projection = TankGame.GameProjection;
        View = TankGame.GameView;

        if (DebugUtils.DebuggingEnabled && DebugUtils.DebugLevel == 1 && HomeProperties.Speed > 0)
            Collision.DoRaycast(Position, HomeProperties.Target, (int)HomeProperties.Radius, true);
        if (DebugUtils.DebuggingEnabled)
            DebugUtils.DrawDebugString(TankGame.SpriteRenderer,
                $"RicochetsLeft: {RicochetsRemaining}\nTier: {Type}\nId: {Id}",
                MatrixUtils.ConvertWorldToScreen(Vector3.Zero, World, View, Projection) - new Vector2(0, 20), 1,
                centered: true);

        for (var i = 0; i < (Lighting.AccurateShadows ? 2 : 1); i++) {
            DrawShellMesh(i);
        }

        OnPostRender?.Invoke(this);
    }

    private void DrawShellMesh(int currentIteration) {
        void RenderMeshEffects(int i, ModelMesh mesh) {
            for (var j = 0; j < mesh.Effects.Count; j++) {
                var effect = (BasicEffect)mesh.Effects[j];
                effect.World = i == 0
                    ? World
                    : World * Matrix.CreateShadow(Lighting.AccurateLightingDirection, new(Vector3.UnitY, 0)) *
                      Matrix.CreateTranslation(0, 0.2f, 0);

                effect.View = View;
                effect.Projection = Projection;
                effect.TextureEnabled = true;

                effect.Texture = _shellTexture;

                effect.SetDefaultGameLighting_IngameEntities();
            }
        }
        
        for (var i = 0; i < Model.Meshes.Count; i++) {
            var mesh = Model.Meshes[i];
            RenderMeshEffects(currentIteration, mesh);
            mesh.Draw();
        }
    }
    /// <summary>Check if this <see cref="Shell"/> is heading towards <paramref name="targetPosition"/>, based on <paramref name="arc"/>.</summary>
    /// <param name="targetPosition">The position to check whether or not this <see cref="Shell"/> is on a collision path with.</param>
    /// <param name="distance">The distance the target must be from this <see cref="Shell"/>.</param>
    /// <param name="arc">The arc length (from the angular rotation of <see cref="Velocity"/> to <c>arc / 2</c> to check.</param>
    /// <returns></returns>
    public bool IsHeadingTowards(Vector2 targetPosition, float distance, float arc) {
        var rotation = Velocity != Vector2.Zero ? Velocity.ToRotation() : Vector2.UnitX.ToRotation();

        // check if the direction to the position's rotation is similar on (-arc / 2, arc / 2)
        var targetAngularRotation = (Position - targetPosition).ToRotation();

        // check if the direction 
        return ((targetAngularRotation < rotation + arc / 2) || (targetAngularRotation > rotation - arc / 2)) && GameUtils.Distance_WiiTanksUnits(Position, targetPosition) < distance;
    }
}