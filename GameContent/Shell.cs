using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TanksRebirth.Enums;
using TanksRebirth.GameContent.GameMechanics;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.ModSupport;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.Systems.AI;
using TanksRebirth.Graphics;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Framework;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;
using TanksRebirth.GameContent.UI.MainMenu;
using TanksRebirth.GameContent.Globals.Assets;

namespace TanksRebirth.GameContent;

public class Shell : IAITankDanger
{
    public delegate void PostCreateDelegate(Shell shell);

    public static event PostCreateDelegate? PostCreate;

    public delegate void PreCreateDelegate(Shell shell);

    public static event PreCreateDelegate? PreCreate;
    public delegate void BlockRicochetDelegate(Block block, Shell shell);

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
    public struct HomingProperties
    {
        public float Power;
        public float Radius;
        public float Speed;
        public int Cooldown;

        public Vector2 Target;

        public bool HeatSeeks;
    }

    // this used to be 1500. why?
    /// <summary>The maximum shells allowed at any given time.</summary>
    private const int MaxShells = 200;

    public static Shell[] AllShells { get; } = new Shell[MaxShells];

    /// <summary>The <see cref="Tank"/> which shot this <see cref="Shell"/>.</summary>
    public Tank? Owner;
    public ModShell? ModdedData { get; set; }

    public Vector3 Position3D => Position.ExpandZ() + new Vector3(0, 11, 0);
    public Vector3 Velocity3D => Velocity.ExpandZ();

    /// <summary>Maximum amount of times this <see cref="Shell"/> can bounce off walls.</summary>
    public uint Ricochets;
    /// <summary>How many times this <see cref="Shell"/> can hit walls.</summary>
    public uint RicochetsRemaining;

    public float Rotation;

    public Vector2 Position { get; set; }
    public Vector2 Velocity;

    public Matrix View;
    public Matrix Projection;
    public Matrix World;

    public Model Model;

    public OggAudio? ShootSound;
    public OggAudio? TrailSound;

    /// <summary>The hurtbox on the 2D backing map for the game.</summary>
    public Rectangle Hitbox => new((int)(Position.X - 2), (int)(Position.Y - 2), 4, 4);

    /// <summary>The hurtcircle on the 2D backing map for the game.</summary>
    public Circle HitCircle => new() { Center = Position, Radius = 4 };
    public bool IsPlayerSourced { get; set; }

    private Texture2D? _shellTexture;
    public int Id { get; private set; }
    private float _wallRicCooldown;
    /// <summary>How long this shell has existed in the world.</summary>
    public float LifeTime;
    public ShellProperties Properties { get; set; } = new();
    public int Type { get; set; }
    public void ReassignId(int newId) => Id = newId;
    public void SwapTexture(Texture2D texture) => _shellTexture = texture;
    public void Swap(int type) {
        Type = type;

        switch (Type) {
            case ShellID.Player:
            case ShellID.Standard:
                _shellTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/bullet");
                break;
            case ShellID.Rocket:
                _shellTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/bullet");
                Properties.Flaming = true;
                TrailSound = new OggAudio("Content/Assets/sounds/tnk_shoot_rocket_loop.ogg", 0.3f);
                TrailSound.Instance.IsLooped = true;
                break;
            case ShellID.TrailedRocket:
                _shellTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/bullet");
                Properties.EmitsSmoke = false;
                Properties.LeavesTrail = true;
                Properties.Flaming = true;
                TrailSound = new OggAudio("Content/Assets/sounds/tnk_shoot_ricochet_rocket_loop.ogg", 0.3f);
                TrailSound.Instance.IsLooped = true;
                break;
            case ShellID.Supressed:
                _shellTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/explosive_bullet");
                break;
            case ShellID.Explosive:
                _shellTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/explosive_bullet");
                Properties.IsDestructible = false;
                break;
            default:
                for (int i = 0; i < ModLoader.ModShells.Length; i++) {
                    if (Type == ModLoader.ModShells[i].Type) {
                        ModLoader.ModShells[i].OnCreate(this);
                        break;
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Creates a new <see cref="Shell"/>.
    /// </summary>
    /// <param name="position">The position of the created <see cref="Shell"/>.</param>
    /// <param name="velocity">The velocity of the created <see cref="Shell"/>.</param>
    /// <param name="type">The type of <see cref="Shell"/> to be fired.</param>
    /// <param name="owner">Which <see cref="Tank"/> owns this <see cref="Shell"/>.</param>
    /// <param name="ricochets">How many times the newly created <see cref="Shell"/> can ricochet.</param>
    /// <param name="homing">Whether or not the newly created <see cref="Shell"/> homes in on enemies.</param>
    /// <param name="playSpawnSound">Play the shooting sound associated with this <see cref="Shell"/>.</param>
    public Shell(Vector2 position, Vector2 velocity, int type, Tank? owner, uint ricochets = 0,
        HomingProperties homing = default, bool playSpawnSound = true) {
        Type = type;
        RicochetsRemaining = ricochets;
        Position = position;
        Model = ModelResources.Bullet.Asset;

        AITank.Dangers.Add(this);
        IsPlayerSourced = owner is PlayerTank;

        Properties.HomeProperties = homing;
        Owner = owner;

        // if explosive, black

        Velocity = velocity;

        PreCreate?.Invoke(this);

        Swap(type);

        if (playSpawnSound) {
            if (Type <= ShellID.Explosive) {
                ShootSound = Type switch {
                    ShellID.Player => new OggAudio("Content/Assets/sounds/tnk_shoot_regular_1.ogg"),
                    ShellID.Standard => new OggAudio("Content/Assets/sounds/tnk_shoot_regular_2.ogg"),
                    ShellID.Rocket => new OggAudio("Content/Assets/sounds/tnk_shoot_rocket.ogg"),
                    ShellID.TrailedRocket => new OggAudio("Content/Assets/sounds/tnk_shoot_ricochet_rocket.ogg"),
                    ShellID.Supressed => new OggAudio("Content/Assets/sounds/tnk_shoot_silencer.ogg"),
                    ShellID.Explosive => new OggAudio("Content/Assets/sounds/tnk_shoot_regular_2.ogg"),
                    _ => throw new NotImplementedException($"Sound for the shell type {Type} is not implemented, yet."),
                };
            }
        }

        if (owner is not null && playSpawnSound) {
            ShootSound!.Instance.Pitch = MathHelper.Clamp(owner.Properties.ShootPitch, -1, 1);
            SoundPlayer.PlaySoundInstance(ShootSound, SoundContext.Effect, 0.3f);
        }

        CampaignGlobals.OnMissionEnd += StopSounds;
        //TankGame.OnFocusLost += TankGame_OnFocusLost;
        //TankGame.OnFocusRegained += TankGame_OnFocusRegained;

        int index = Array.IndexOf(AllShells, null);

        Id = index;

        AllShells[index] = this;

        if (owner == null) return;

        var idx = Array.IndexOf(Owner.OwnedShells, null);

        if (idx > -1)
            Owner.OwnedShells[idx] = this;

        PostCreate?.Invoke(this);
    }
    private void StopSounds(int delay, MissionEndContext context, bool result1up) {
        TrailSound?.Instance?.Stop();
        ShootSound?.Instance?.Stop();
    }
    internal void Update() {
        if (!GameScene.ShouldRenderAll || (!CampaignGlobals.InMission && !MainMenuUI.Active))
            return;

        Rotation = Velocity.ToRotation() - MathHelper.PiOver2;
        Position += Velocity * 0.62f * RuntimeData.DeltaTime;
        World = Matrix.CreateFromYawPitchRoll(-Rotation, 0, 0)
                * Matrix.CreateTranslation(Position3D);

        if (_wallRicCooldown <= 0) {
            if (Position.X is < GameScene.MIN_X or > GameScene.MAX_X) {
                OnRicochet?.Invoke(this);
                Ricochet(true);

                for (int i = 0; i < ModLoader.ModShells.Length; i++) {
                    if (Type == ModLoader.ModShells[i].Type) {
                        ModLoader.ModShells[i].OnRicochet(this, null);
                        return;
                    }
                }

                _wallRicCooldown = 5;
            }

            if (Position.Y is < GameScene.MIN_Z or > GameScene.MAX_Z) {
                OnRicochet?.Invoke(this);
                Ricochet(false);

                for (int i = 0; i < ModLoader.ModShells.Length; i++) {
                    if (Type == ModLoader.ModShells[i].Type) {
                        ModLoader.ModShells[i].OnRicochet(this, null);
                        return;
                    }
                }

                _wallRicCooldown = 5;
            }
        }
        else
            _wallRicCooldown -= RuntimeData.DeltaTime;

        var dummy = Vector2.Zero;

        Collision.HandleCollisionSimple_ForBlocks(Hitbox, Velocity, ref dummy, out var dir, out var block,
            out bool corner, false, (c) => c.Properties.IsSolid);

        if (LifeTime <= 5 && (dir != CollisionDirection.None || corner))
            Destroy(DestructionContext.WithObstacle);
        if (corner)
            Destroy(DestructionContext.WithObstacle);
        if (_wallRicCooldown <= 0) {
            switch (dir) {
                case CollisionDirection.Up:
                case CollisionDirection.Down:
                    for (int i = 0; i < ModLoader.ModBlocks.Length; i++) {
                        if (Type == ModLoader.ModBlocks[i].Type) {
                            ModLoader.ModBlocks[i].OnRicochet(block, this);
                            return;
                        }
                    }
                    for (int i = 0; i < ModLoader.ModShells.Length; i++) {
                        if (Type == ModLoader.ModShells[i].Type) {
                            ModLoader.ModShells[i].OnRicochet(this, block);
                            return;
                        }
                    }
                    OnRicochetWithBlock?.Invoke(block, this);
                    Ricochet(false);
                    break;
                case CollisionDirection.Left:
                case CollisionDirection.Right:
                    for (int i = 0; i < ModLoader.ModBlocks.Length; i++) {
                        if (Type == ModLoader.ModBlocks[i].Type) {
                            ModLoader.ModBlocks[i].OnRicochet(block, this);
                            return;
                        }
                    }
                    for (int i = 0; i < ModLoader.ModShells.Length; i++) {
                        if (Type == ModLoader.ModShells[i].Type) {
                            ModLoader.ModShells[i].OnRicochet(this, block);
                            return;
                        }
                    }
                    OnRicochetWithBlock?.Invoke(block, this);
                    Ricochet(true);
                    break;
            }
        }

        LifeTime += RuntimeData.DeltaTime;

        while (LifeTime > Properties.HomeProperties.Cooldown) { // Use loop to reduce nesting smh.
            if (Owner == null)
                break;

            ref var tanksSSpace = ref MemoryMarshal.GetReference((Span
                <Tank>)GameHandler.AllTanks);

            for (var i = 0; i < GameHandler.AllTanks.Length; i++) {
                var target = Unsafe.Add(ref tanksSSpace, i);

                if (target is null || target.Dead || target == Owner ||
                    !(Vector2.Distance(Position, target.Position) <= Properties.HomeProperties.Radius)) continue;

                if (target.Team == Owner.Team && target.Team != TeamID.NoTeam) continue;

                if (Properties.HomeProperties.HeatSeeks && target.Velocity != Vector2.Zero)
                    Properties.HomeProperties.Target = target.Position;
                if (!Properties.HomeProperties.HeatSeeks)
                    Properties.HomeProperties.Target = target.Position;
            }

            if (Properties.HomeProperties.Target != Vector2.Zero) {
                bool hits = Collision.DoRaycast(Position, Properties.HomeProperties.Target, (int)Properties.HomeProperties.Radius * 2);

                if (hits) {
                    float dist = Vector2.Distance(Position, Properties.HomeProperties.Target);

                    Velocity.X += MathUtils.DirectionTo(Position, Properties.HomeProperties.Target).X *
                        Properties.HomeProperties.Power / dist;
                    Velocity.Y += MathUtils.DirectionTo(Position, Properties.HomeProperties.Target).Y *
                        Properties.HomeProperties.Power / dist;

                    Vector2 trueSpeed = Vector2.Normalize(Velocity) * Properties.HomeProperties.Speed;


                    Velocity = trueSpeed;
                }
            }

            break;
        }

        CheckCollisions();

        var bruh = Properties.Flaming ? (int)Math.Round(6 / Velocity.Length()) : (int)Math.Round(12 / Velocity.Length());
        var num = bruh != 0 ? bruh : 5f;

        if (Properties.EmitsSmoke)
            RenderSmokeParticle(num);

        if (Properties.LeavesTrail)
            RenderLeaveTrail();

        if (Properties.Flaming)
            RenderFlamingParticle();
        for (int i = 0; i < ModLoader.ModShells.Length; i++) {
            if (Type == ModLoader.ModShells[i].Type) {
                ModLoader.ModShells[i].PostUpdate(this);
                return;
            }
        }
        OnPostUpdate?.Invoke(this);
    }
    private void RenderSmokeParticle(float timer) {
        if (CameraGlobals.IsUsingPOVCamera) timer /= 2;
        if (!(LifeTime % timer <= RuntimeData.DeltaTime)) return;

        Particle p;
        if (CameraGlobals.IsUsingPOVCamera) {
            p = GameHandler.Particles.MakeParticle(Position3D + new Vector3(0, 0, 5).FlattenZ()
                                .Rotate(Rotation + MathHelper.Pi + Client.ClientRandom.NextFloat(-0.3f, 0.3f))
                                .ExpandZ(),
            ModelResources.Smoke.Asset,
            GameResources.GetGameResource<Texture2D>("Assets/textures/smoke/smoke"));
            p.Scale = new(0.7f);
        }
        else {
            p = GameHandler.Particles.MakeParticle(
                Position3D + new Vector3(0, 0, 5).FlattenZ()
                                            .Rotate(Rotation + MathHelper.Pi + Client.ClientRandom.NextFloat(-0.3f, 0.3f))
                                            .ExpandZ(),
            GameResources.GetGameResource<Texture2D>("Assets/textures/misc/tank_smokes"));
            p.Scale = new(0.3f);

            p.Pitch = -CameraGlobals.DEFAULT_ORTHOGRAPHIC_ANGLE;
        }
        p.FaceTowardsMe = false;

        p.HasAddativeBlending = false;
        p.Color = Properties.SmokeColor;
        p.Alpha = 0.5f;

        p.UniqueBehavior = (particle) => {
            if (particle.Alpha <= 0)
                particle.Destroy();

            if (particle.Alpha > 0)
                particle.Alpha -= (Properties.Flaming ? 0.03f : 0.02f) * RuntimeData.DeltaTime;

            GeometryUtils.Add(ref particle.Scale, 0.0075f * RuntimeData.DeltaTime);
        };
    }
    private void RenderLeaveTrail() {
        // _oldPosition and Position are *not* the same during method call.
        // TODO: make more particles added depending on the positions between 2 distinct frames
        //var numToAdd

        var p = GameHandler.Particles.MakeParticle(
            Position3D + new Vector3(0, 0, 5).FlattenZ().Rotate(Rotation + MathHelper.Pi).ExpandZ(),
            GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/smoketrail"));
        p.Roll = -MathHelper.PiOver2 + (RuntimeData.RunTime % MathHelper.Tau);
        p.Color = Properties.TrailColor;
        p.HasAddativeBlending = false;
        p.Scale = new(0.45f, 0.5f, 2f); // x = length, y = height, z = width
                                        // defaults = (x = 0.4, y = 0.25, 0.4)

        p.UniqueBehavior = (a) => {
            var diff = 0.05f * RuntimeData.DeltaTime;
            p.Roll += diff;
            p.Pitch += diff;

            p.Alpha -= 0.02f * RuntimeData.DeltaTime;

            if (p.Alpha <= 0f)
                p.Destroy();
        };
    }
    private void RenderFlamingParticle() {
        if (!(0 <= RuntimeData.DeltaTime)) return;

        var p = GameHandler.Particles.MakeParticle(
            Position3D + new Vector3(0, 0, 5).FlattenZ().Rotate(Rotation + MathHelper.Pi).ExpandZ(),
            GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/flame"));

        p.Roll = -MathHelper.PiOver2;
        var scaleRand = Client.ClientRandom.NextFloat(0.5f, 0.75f);
        p.Scale = new(scaleRand, 0.165f, 0.4f); // x is outward from bullet
        p.Color = Properties.FlameColor;
        p.HasAddativeBlending = false;
        // Client.ClientRandom.NextFloat(-2f, 2f)
        p.Rotation2D = -MathHelper.PiOver2;

        var rotoff = Client.ClientRandom.NextFloat(-0.25f, 0.25f);
        p.Origin2D = new(p.Texture.Size().X / 2, p.Texture.Size().Y);

        var initialScale = p.Scale;

        p.UniqueBehavior = (par) => {
            const float scalingConstant = 0.06f;
            var flat = Position;

            var off = flat + Vector2.Zero.Rotate(Rotation);

            par.Position = off.ExpandZ() + new Vector3(0, 11, 0);

            par.Roll = Rotation + MathHelper.PiOver2 + rotoff;
            par.Pitch = MathHelper.PiOver2;

            //if (TankGame.GameUpdateTime % 2 == 0)
            //p.Roll = Client.ClientRandom.NextFloat(0, MathHelper.TwoPi);


            par.Scale.X -= scalingConstant * RuntimeData.DeltaTime;

            if (par.Scale.X <= 0)
                par.Destroy();
        };
    }
    private void TankGame_OnFocusRegained(object? sender, nint e) {
        if (TrailSound is not null && TrailSound.Instance is not null)
            TrailSound.Instance?.Resume();
    }
    private void TankGame_OnFocusLost(object? sender, nint e) {
        if (TrailSound is not null && TrailSound.Instance is not null)
            TrailSound.Instance?.Pause();
    }

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
                sound.Instance.Pitch = Client.ClientRandom.NextFloat(0.15f, 0.25f);
                var rocketRSound = SoundPlayer.PlaySoundInstance("Assets/sounds/ricochet_zip.ogg", SoundContext.Effect,
                    0.05f,
                    gameplaySound: true);
                rocketRSound.Instance.Pitch = -0.65f;
            }
            else {
                sound.Instance.Pitch = Client.ClientRandom.NextFloat(-0.05f, 0.05f);
            }
        }

        GameHandler.Particles.MakeShineSpot(Position3D, Color.Orange, 0.8f);
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

            if (!Properties.CanFriendlyFire) {
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

            tank.Damage(new TankHurtContextShell(this), true);
        }

        ref var bulletSSpace = ref MemoryMarshal.GetReference((Span<Shell>)AllShells);

        for (var i = 0; i < AllShells.Length; i++) {
            ref var bullet = ref Unsafe.Add(ref bulletSSpace, i);
            if (bullet == null || bullet == this) continue;
            if (!bullet.Hitbox.Intersects(Hitbox)) continue;

            if (bullet.Properties.IsDestructible)
                bullet.Destroy(DestructionContext.WithShell);
            if (Properties.IsDestructible)
                Destroy(DestructionContext.WithShell);

            // if two indestructible bullets come together, destroy them both. too powerful!
            if (bullet is { Properties.IsDestructible: true, } || Properties.IsDestructible) continue;

            // bullet is sometimes null here? so null safety is key
            bullet?.Destroy(DestructionContext.WithShell);
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
        CampaignGlobals.OnMissionEnd -= StopSounds;

        TrailSound?.Instance?.Stop();
        TrailSound = null;
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
        ShootSound?.Instance?.Stop(true);
        // ParticleSystem.MakeSparkEmission(Position, 10);
        for (int i = 0; i < ModLoader.ModShells.Length; i++) {
            if (Type == ModLoader.ModShells[i].Type) {
                ModLoader.ModShells[i].OnDestroy(this, context, ref playSound);
                //return;
            }
        }
        if (context != DestructionContext.WithHostileTank && context != DestructionContext.WithMine &&
            context != DestructionContext.WithExplosion) {
            if (playSound) {
                var sfx = SoundPlayer.PlaySoundInstance("Assets/sounds/bullet_destroy.ogg", SoundContext.Effect, 0.5f,
                    gameplaySound: true);
                sfx.Instance.Pitch = Client.ClientRandom.NextFloat(-0.1f, 0.1f);
            }

            GameHandler.Particles.MakeSmallExplosion(Position3D, 8, 10, 1.25f, 15);
        }

        TrailSound?.Instance?.Stop();
        TrailSound?.Dispose();
        TrailSound = null;

        if (Owner is not null) {
            if (Owner.Properties.ShellType == ShellID.Explosive)
                new Explosion(Position, 7f, Owner, 0.25f);
            if (Owner is PlayerTank)
                // in case the player wants to destroy a mine that may be impeding progress- we don't want to penalize them.
                if (context == DestructionContext.WithHostileTank || context == DestructionContext.WithMine ||
                    context == DestructionContext.WithShell)
                    PlayerTank.PlayerStatistics.ShellHits++;
        }

        if (!wasSentByAnotherClient)
            Client.SyncShellDestroy(this, context);
        OnDestroy?.Invoke(this, context);
        Remove();
    }

    internal void Render() {
        if (!GameScene.ShouldRenderAll)
            return;

        Projection = CameraGlobals.GameProjection;
        View = CameraGlobals.GameView;

        // TODO: wtf? DoRaycast failing?
        if (DebugManager.DebuggingEnabled && DebugManager.DebugLevel == 1 && Properties.HomeProperties.Speed > 0)
            Collision.DoRaycast(Position, Properties.HomeProperties.Target, (int)Properties.HomeProperties.Radius, true);
        if (DebugManager.DebuggingEnabled)
            DebugManager.DrawDebugString(TankGame.SpriteRenderer,
                $"RicochetsLeft: {RicochetsRemaining}\nTier: {Type}\nId: {Id}",
                MatrixUtils.ConvertWorldToScreen(Vector3.Zero, World, View, Projection) - new Vector2(0, 20), 1,
                centered: true);

        for (var i = 0; i < (Lighting.AccurateShadows ? 2 : 1); i++) {
            DrawShellMesh(i);
        }
        for (int i = 0; i < ModLoader.ModShells.Length; i++) {
            if (Type == ModLoader.ModShells[i].Type) {
                ModLoader.ModShells[i].PostRender(this);
                return;
            }
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

        var rotToTarget = MathUtils.DirectionTo(Position, targetPosition).ToRotation();

        var inDistance = GameUtils.Distance_WiiTanksUnits(Position, targetPosition) < distance;

        var angleBetween = MathUtils.AbsoluteAngleBetween(rotation, rotToTarget);

        var isInAngle = angleBetween <= arc / 2;

        // check if the direction 
        return isInAngle && inDistance;
    }
}