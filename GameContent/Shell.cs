using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TanksRebirth.Enums;
using TanksRebirth.GameContent.GameMechanics;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Globals.Assets;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.ModSupport;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.Systems.AI;
using TanksRebirth.GameContent.Systems.ParticleSystem;
using TanksRebirth.GameContent.Systems.TankSystem;
using TanksRebirth.GameContent.Systems.TankSystem.AI;
using TanksRebirth.GameContent.UI.MainMenu;
using TanksRebirth.Graphics;
using TanksRebirth.Graphics.Drawing;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Framework.Interfaces;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent;

// TODO: fix some shells instantly being destroyed from outer wall ricochets
public class Shell : IAITankDanger, IHasModContent<ModShell> {
    public const int COLL_RECT_DIM = 3;
    public const int TOO_SHORT_LIFETIME = 5;
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
        public float Cooldown;

        public Vector2 Target;

        public bool HeatSeeks;
    }

    public struct ShellDrawParams {
        public Texture2D? ShellTexture;
        public Model Model;
    }
    public delegate void PostCreateDelegate(Shell shell);
    public static event PostCreateDelegate? PostCreate;

    public delegate void PreCreateDelegate(Shell shell);
    public static event PreCreateDelegate? PreCreate;

    public delegate void RicochetDelegate(Shell shell, Block? block);
    /// <summary>If <see cref="Block"/> is <see langword="null"/>, then it ricocheted off the bounding wall.</summary>
    public static event RicochetDelegate? OnRicochet;

    public delegate void PostUpdateDelegate(Shell shell);
    public static event PostUpdateDelegate? OnPostUpdate;

    public delegate void PostRenderDelegate(Shell shell);
    public static event PostRenderDelegate? OnPostRender;

    public delegate void DestroyDelegate(Shell shell, DestructionContext context);
    public static event DestroyDelegate? OnDestroy;

    // this used to be 1500. why?
    /// <summary>The maximum shells allowed at any given time.</summary>
    public const int MAX_SHELLS = 200;

    public static Shell[] AllShells { get; } = new Shell[MAX_SHELLS];

    /// <summary>The <see cref="Tank"/> which shot this <see cref="Shell"/>.</summary>
    public Tank? Owner;
    public ModShell? ModdedData { get; internal set; }

    public Vector3 Position3D => Position.ExpandZ() + new Vector3(0, Tank.TNK_DMG_COLL_Y, 0);
    public Vector3 Velocity3D => Velocity.ExpandZ();

    /// <summary>Maximum amount of times this <see cref="Shell"/> can bounce off walls.</summary>
    public int Ricochets;
    /// <summary>How many times this <see cref="Shell"/> can hit walls.</summary>
    public int RicochetsRemaining;

    public float Rotation;

    public Vector2 Position { get; set; }
    public Vector2 Velocity;

    public BasicDrawParams DrawParams = new();
    public ShellDrawParams DrawParamsShell;

    public OggAudio? ShootSound;
    public OggAudio? TrailSound;

    /// <summary>Used primarily for collisions with blocks. This may be replaced in the future with 3D calculations.</summary>
    public Rectangle CollHitbox => new((int)(Position.X - COLL_RECT_DIM / 2), (int)(Position.Y - COLL_RECT_DIM / 2), COLL_RECT_DIM, COLL_RECT_DIM);

    public float HitSphereSize = 4.0f;
    public BoundingSphere Hitbox;
    // /// <summary>The hit-circle on the 2D backing map for the game.</summary>
    // public Circle HitCircle => new() { Center = Position, Radius = 5 }; // original is moreso a radius of 7, but 5 is good, since it isnt 480p
    public int Team => Owner?.Team ?? TeamID.NoTeam;
    /// <summary>
    /// Represents the ID of this shell in the array. Useful for local operations relating to collisions and such.
    /// </summary>
    public int Id { get; private set; }
    /// <summary>Represents an ID between (0-63 * client #) used for syncing. Set randomly and is synced on spawn but not manipulated! Useful for state change operations on the bullet, such as death.</summary>
    public byte UID { get; private set; }
    /// <summary>How long this shell has existed in the world.</summary>
    public float LifeTime;
    public ShellProperties Properties = new();
    public int Type { get; set; }

    /// <summary>An identifier of the shell's volley. If shells share the same volley ID, they cannot collide until they separate from spawn.</summary>
    public int VolleyId = -1;
    /// <summary>Updates the UID of the shell. Avoid changing during runtime if unnecessary.</summary>
    public void SetUID(byte newID) => UID = newID;
    /// <summary>aGenerates a random UID for use with shell instance management</summary>
    public static byte GenerateUID(Tank owner) {
        bool repetitionCheck = true;
        int attempts = 0;
        while (repetitionCheck && attempts < 8) {
            attempts += 1;
            repetitionCheck = false;
            byte newID = (byte)(Client.ClientRandom.Next(0, byte.MaxValue / GameHandler.MAX_PLAYERS + 1) + NetPlay.GetMyClientId() * byte.MaxValue / GameHandler.MAX_PLAYERS + 1);        //splits the byte range (0-255) amongst 4 players and allows each player to allocate 0-63 of the addr.
            for (int i = 0; i < owner.OwnedShellCount; i++) {
                if (owner.OwnedShells[i] is null) continue;
                if (owner.OwnedShells[i].UID == newID) {      //if there's a repetition, redo
                    repetitionCheck = true;
                    break;
                }
            }
            if (!repetitionCheck)
                return newID;
        }
        return 255;     //Could not generate.
    }
    public void Swap(int type) {
        Type = type;

        switch (Type) {
            case ShellID.Player:
            case ShellID.Standard:
                DrawParamsShell.ShellTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/bullet");
                break;
            case ShellID.Rocket:
                DrawParamsShell.ShellTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/bullet");
                Properties.Visuals |= VisualFlags.Flaming;
                TrailSound = new OggAudio("Content/Assets/sounds/tnk_shoot_rocket_loop.ogg", 0.3f);
                TrailSound.Instance.IsLooped = true;
                break;
            case ShellID.TrailedRocket:
                DrawParamsShell.ShellTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/bullet");
                Properties.Visuals = VisualFlags.SmokeTrail | VisualFlags.Flaming;
                TrailSound = new OggAudio("Content/Assets/sounds/tnk_shoot_ricochet_rocket_loop.ogg", 0.3f);
                TrailSound.Instance.IsLooped = true;
                break;
            case ShellID.Supressed:
                DrawParamsShell.ShellTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/explosive_bullet");
                break;
            case ShellID.Explosive:
                DrawParamsShell.ShellTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/explosive_bullet");
                Properties.Penetration = -1;
                break;
            default:
                ModdedData?.OnCreate();
                break;
        }
    }

    /// <summary>
    /// Creates a new <see cref="Shell"/>. This is unsafe as if you call this from Tank code, it will cause errors. Use <see cref="Create"/> instead.
    /// </summary>
    Shell(Vector2 position, Vector2 velocity, int type, Tank? owner, int ricochets = 0) {
        Type = type;
        RicochetsRemaining = ricochets;
        Position = position;
        DrawParamsShell.Model = ModelGlobals.Bullet.Asset;
        Owner = owner;
        // if explosive, black
        Velocity = velocity;

        PreCreate?.Invoke(this);

        this.AttachModContent();

        // ths calls OnCreate for ModdedData
        Swap(type);

        CampaignGlobals.OnMissionEnd += StopSounds;
        //TankGame.OnFocusLost += TankGame_OnFocusLost;
        //TankGame.OnFocusRegained += TankGame_OnFocusRegained;

        int index = Array.IndexOf(AllShells, null);
        Id = index;
        AllShells[index] = this;

        if (owner == null) return;

        var idx = Array.IndexOf(Owner.OwnedShells, null);
        if (idx > -1) Owner.OwnedShells[idx] = this;

        SetUID(GenerateUID(owner));
        AITank.Dangers.Add(this);
        PostCreate?.Invoke(this);
    }
    /// <summary>
    /// Creates a <see cref="Shell"/>. This method is thread-agnostic.
    /// </summary>
    /// <param name="position">The position to spawn at.</param>
    /// <param name="velocity">The initial velocity.</param>
    /// <param name="type">The shell kind.</param>
    /// <param name="owner">The tank (if any) that owns this <see cref="Shell"/>.</param>
    /// <param name="ricochets">How many ricochets it will have.</param>
    /// <param name="playSpawnSound">If <see langword="true"/>, <see cref="ShootSound"/> will be assigned to, and played.</param>
    /// <returns>The created <see cref="Shell"/>.</returns>
    public static Shell Create(Vector2 position, Vector2 velocity, int type, Tank? owner, int ricochets = 0, bool playSpawnSound = true) {
        var shell = TankGame.ThreadAgnostic(() => {
            var s = new Shell(position, velocity, type, owner, ricochets);

            if (!playSpawnSound) return s;

            if (s.Type <= ShellID.Explosive) {
                s.ShootSound = s.Type switch {
                    ShellID.Player => new OggAudio("Content/Assets/sounds/tnk_shoot_regular_1.ogg"),
                    ShellID.Standard => new OggAudio("Content/Assets/sounds/tnk_shoot_regular_2.ogg"),
                    ShellID.Rocket => new OggAudio("Content/Assets/sounds/tnk_shoot_rocket.ogg"),
                    ShellID.TrailedRocket => new OggAudio("Content/Assets/sounds/tnk_shoot_ricochet_rocket.ogg"),
                    ShellID.Supressed => new OggAudio("Content/Assets/sounds/tnk_shoot_silencer.ogg"),
                    ShellID.Explosive => new OggAudio("Content/Assets/sounds/tnk_shoot_regular_2.ogg"),
                    _ => throw new NotImplementedException($"Sound for the shell type {s.Type} is not implemented... yet."),
                };
                SoundPlayer.PlaySoundInstance(s.ShootSound, SoundContext.Effect, volume: 1f, pitchOverride: GameUtils.NaturalPitchShift);
            }

            return s;
        });

        return shell;
    }
    void StopSounds(int delay, MissionEndContext context, bool result1up) {
        TrailSound?.Instance?.Stop();
        ShootSound?.Instance?.Stop();
    }
    public void Update() {
        if (!CampaignGlobals.InMission && !MainMenuUI.IsActive) return;

        Hitbox = new(Position3D, HitSphereSize);

        Rotation = Velocity.ToRotation() - MathHelper.PiOver2;
        Position += Velocity * 0.62f * RuntimeData.DeltaTime;
        DrawParams.World = Matrix.CreateFromYawPitchRoll(-Rotation, 0, 0)
                * Matrix.CreateTranslation(Position3D);

        //if (TrailSound != null) {
        //if (CameraGlobals.IsUsingFirstPresonCamera)
        //    SoundUtils.CreateSpatialSound(TrailSound, Position3D, CameraGlobals.RebirthFreecam.Position);
        //}

        if (Position.X is < GameScene.MIN_X or > GameScene.MAX_X) {
            Ricochet(Vector2.UnitX);

            ModdedData?.OnRicochet(null);
            OnRicochet?.Invoke(this, null);
        }

        if (Position.Y is < GameScene.MIN_Z or > GameScene.MAX_Z) {
            Ricochet(Vector2.UnitY);

            OnRicochet?.Invoke(this, null);
            ModdedData?.OnRicochet(null);
        }

        var dummy = Vector2.Zero;

        Collision.HandleCollisionSimple_ForBlocks(CollHitbox, Velocity, ref dummy, out var dir, out var block,
            out bool corner, false, (c) => c.Properties.IsSolid);

        if (corner)
            Destroy(DestructionContext.WithObstacle);
        switch (dir) {
            case CollisionDirection.Up:
            case CollisionDirection.Down:
                Ricochet(Vector2.UnitY);
                block.ModdedData?.OnRicochet(this);
                ModdedData?.OnRicochet(block);
                OnRicochet?.Invoke(this, null);
                break;
            case CollisionDirection.Left:
            case CollisionDirection.Right:
                // TODO: fix this pls
                Ricochet(Vector2.UnitX);
                block.ModdedData?.OnRicochet(this);
                ModdedData?.OnRicochet(block);
                OnRicochet?.Invoke(this, null);
                break;
        }

        LifeTime += RuntimeData.DeltaTime;

        while (LifeTime > Properties.Homing.Cooldown) { // Use loop to reduce nesting smh.
            if (Owner == null)
                break;

            ref var tanksSSpace = ref MemoryMarshal.GetReference((Span<Tank>)GameHandler.AllTanks);

            for (var i = 0; i < GameHandler.AllTanks.Length; i++) {
                var target = Unsafe.Add(ref tanksSSpace, i);

                if (target is null || target.IsDestroyed || target == Owner ||
                    !(Vector2.Distance(Position, target.Position) <= Properties.Homing.Radius)) continue;

                if (target.Team == Owner.Team && target.Team != TeamID.NoTeam) continue;

                if (Properties.Homing.HeatSeeks && target.Velocity != Vector2.Zero)
                    Properties.Homing.Target = target.Position;
                if (!Properties.Homing.HeatSeeks)
                    Properties.Homing.Target = target.Position;
            }

            if (Properties.Homing.Target != Vector2.Zero) {
                bool success = false;
                Tank.CollisionsWorld.RayCast((fixture, point, normal, fraction) => {
                    // pretty self-explanatory
                    if (fixture.Body.Tag is Tank t) {
                        if (!t.IsOnSameTeamAs(Team)) {
                            float distanceToHit = Vector2.Distance(Position / Tank.UNITS_PER_METER, point);
                            if (distanceToHit <= Properties.Homing.Radius / Tank.UNITS_PER_METER) {
                                success = true;
                            }
                            return fraction;
                        }
                        return -1f;
                    }
                    success = false;
                    return 0f;
                }, Position / Tank.UNITS_PER_METER, Properties.Homing.Target / Tank.UNITS_PER_METER);

                if (success) {
                    float dist = Vector2.Distance(Position, Properties.Homing.Target);
                    Velocity += MathUtils.DirectionTo(Position, Properties.Homing.Target) * Properties.Homing.Power / dist;

                    var trueSpeed = Vector2.Normalize(Velocity) * Properties.Homing.Speed;
                    Velocity = trueSpeed;
                }
            }

            break;
        }

        CheckCollisions();

        var bruh = Properties.Visuals.HasFlag(VisualFlags.Flaming) ? (int)Math.Round(6 / Velocity.Length()) : (int)Math.Round(12 / Velocity.Length());
        var num = bruh != 0 ? bruh : 5f;

        if (Properties.Visuals.HasFlag(VisualFlags.SmokePuff))
            RenderSmokeParticle(num);

        if (Properties.Visuals.HasFlag(VisualFlags.SmokeTrail))
            RenderLeaveTrail();

        if (Properties.Visuals.HasFlag(VisualFlags.Flaming))
            RenderFlamingParticle();

        ModdedData?.PostUpdate();
        OnPostUpdate?.Invoke(this);
    }
    #region Particles
    void RenderSmokeParticle(float timer) {

        // TODO: make look accurate
        if (CameraGlobals.IsUsingFirstPersonCamera) timer /= 2;
        if (!(LifeTime % timer <= RuntimeData.DeltaTime)) return;

        Particle p;
        p = GameHandler.Particles.MakeParticle(
            Position3D + new Vector3(0, 0, 5).FlattenZ()
                                        .RotatedBy(Rotation + MathHelper.Pi + Client.ClientRandom.NextFloat(-0.3f, 0.3f))
                                        .ExpandZ(),
        GameResources.GetGameResource<Texture2D>("Assets/textures/misc/tank_smoke"));
        p.Scale = new(0.3f);

        p.Pitch = -CameraGlobals.DEFAULT_ORTHOGRAPHIC_ANGLE;
        p.FaceTowardsMe = CameraGlobals.IsUsingFirstPersonCamera;

        p.HasAdditiveBlending = false;
        p.Color = Properties.SmokeColor;
        p.Alpha = 0.5f;

        p.UniqueBehavior = (particle) => {
            if (particle.Alpha <= 0)
                particle.Destroy();

            if (particle.Alpha > 0)
                particle.Alpha -= (Properties.Visuals.HasFlag(VisualFlags.Flaming) ? 0.03f : 0.02f) * RuntimeData.DeltaTime;

            GeometryUtils.Add(ref particle.Scale, 0.0075f * RuntimeData.DeltaTime);
        };
    }
    void RenderLeaveTrail() {
        // _oldPosition and Position are *not* the same during method call.
        // TODO: make more particles added depending on the positions between 2 distinct frames
        //var numToAdd

        var p = GameHandler.Particles.MakeParticle(
            Position3D + new Vector3(0, 0, 5).FlattenZ().RotatedBy(Rotation + MathHelper.Pi).ExpandZ(),
            GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/smoketrail"));

        // p.Layer = 1f;
        p.Roll = -MathHelper.PiOver2 + (RuntimeData.RunTime % MathHelper.Tau);
        p.Color = Properties.TrailColor;
        p.HasAdditiveBlending = false;
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
    void RenderFlamingParticle() {
        var flame = GameHandler.Particles.MakeParticle(
            Position3D + new Vector3(0, 0, 5).FlattenZ().RotatedBy(Rotation + MathHelper.Pi).ExpandZ(),
            GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/flame"));

        var scaleRand = Client.ClientRandom.NextFloat(0.5f, 0.75f);

        flame.Layer = 1f;
        flame.Scale = new(scaleRand, 0.165f, 0.4f); // x is outward from bullet
        flame.Color = Properties.FlameColor;
        flame.HasAdditiveBlending = false;

        flame.Rotation2D = -MathHelper.PiOver2;

        var rotoff = Client.ClientRandom.NextFloat(-0.25f, 0.25f);
        flame.Position = new Vector3(float.MaxValue);
        flame.Origin2D = new(flame.Texture.Size().X / 2, flame.Texture.Size().Y);

        var initialScale = flame.Scale;

        flame.UniqueBehavior = (p) => {
            const float scalingConstant = 0.06f;

            var off = Position + Vector2.Zero.RotatedBy(Rotation);

            flame.Position = off.ExpandZ() + new Vector3(0, 11, 0);

            flame.Roll = Rotation + MathHelper.PiOver2 + rotoff;
            flame.Pitch = MathHelper.PiOver2;

            //if (TankGame.GameUpdateTime % 2 == 0)
            //p.Roll = Client.ClientRandom.NextFloat(0, MathHelper.TwoPi);


            flame.Scale.X -= scalingConstant * RuntimeData.DeltaTime;

            if (flame.Scale.X <= 0)
                flame.Destroy();
        };
    }
    #endregion
    void TankGame_OnFocusRegained(object? sender, nint e) {
        if (TrailSound is not null && TrailSound.Instance is not null)
            TrailSound.Instance?.Resume();
    }
    void TankGame_OnFocusLost(object? sender, nint e) {
        if (TrailSound is not null && TrailSound.Instance is not null)
            TrailSound.Instance?.Pause();
    }

    /// <summary>
    /// Ricochets this <see cref="Shell"/>.
    /// </summary>
    /// <param name="normal">The normal to reflect off of.</param>
    public void Ricochet(Vector2 normal) {
        const string ricochetSound = "Assets/sounds/bullet_ricochet.ogg";

        // ChatSystem.SendMessage("PENIS BIGGA @ " + DateTime.Now.Nanosecond, ColorUtils.DiscoPartyColor);

        if (RicochetsRemaining <= 0) {
            Destroy(DestructionContext.WithObstacle);
            return;
        }

        if (LifeTime < TOO_SHORT_LIFETIME) {
            Destroy(DestructionContext.WithObstacle);
            return;
        }

        Velocity = Vector2.Reflect(Velocity, normal);


        var sound = SoundPlayer.PlaySoundInstance(ricochetSound, SoundContext.Effect, 0.5f, pitchOverride: GameUtils.NaturalPitchShift);

        //bool fp = CameraGlobals.IsUsingFirstPresonCamera;
        //if (fp)
        //    SoundUtils.CreateSpatialSound(sound, Position3D, CameraGlobals.RebirthFreecam.Position);

        if (Owner is not null) {
            if (Owner.Properties.ShellType == ShellID.TrailedRocket) {
                sound.Instance.Pitch = Client.ClientRandom.NextFloat(0.15f, 0.25f);
                var rocketRSound = SoundPlayer.PlaySoundInstance("Assets/sounds/ricochet_zip.ogg", SoundContext.Effect, 0.05f);
                rocketRSound.Pitch -= 0.65f;
                //if (fp)
                //    SoundUtils.CreateSpatialSound(sound, Position3D, CameraGlobals.RebirthFreecam.Position);
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
            if (tank == null || tank.IsDestroyed) continue;

            if (!tank.Hurtbox.Intersects(Hitbox)) continue;

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

        // prevents collisions between shells spawned in the same volley
        bool hasSibling = false;
        bool stillIntersecting = false;

        for (int i = 0; i < AllShells.Length; i++) {
            var s = AllShells[i];
            if (s == null || s == this)
                continue;

            if (s.VolleyId != VolleyId)
                continue;

            if (s.Hitbox == default)
                continue;

            hasSibling = true;

            if (s.Hitbox.Intersects(Hitbox)) {
                stillIntersecting = true;
                break;
            }
        }

        if (hasSibling && !stillIntersecting)
            VolleyId = -1;

        // regular collision, with respect to collision group
        for (var i = 0; i < AllShells.Length; i++) {
            ref var shell = ref Unsafe.Add(ref bulletSSpace, i);
            if (shell == null || shell == this) continue;
            
            // prevents collisions between shells of the same volley until they separate
            if (shell.VolleyId > -1 && VolleyId > -1
                && shell.VolleyId == VolleyId) continue;

            bool collision = shell.Hitbox.Intersects(Hitbox);
            if (!collision) continue;

            var otherDestructible = shell.Properties.Penetration > -1;
            var thisDestructible = Properties.Penetration > -1;

            if (otherDestructible) shell.Destroy(DestructionContext.WithShell);
            if (thisDestructible) Destroy(DestructionContext.WithShell);

            // if destroy has been called this will be true, so prevent further checking
            if (shell == null) continue;

            // if two indestructible bullets come together, destroy them both. too powerful!
            if (otherDestructible || thisDestructible) continue;

            // bullet is sometimes null here? so null safety is key
            shell.Destroy(DestructionContext.WithShell);
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
        ModdedData?.OnDestroy(context, ref playSound);
        if (context != DestructionContext.WithHostileTank && context != DestructionContext.WithMine &&
            context != DestructionContext.WithExplosion) {
            if (playSound) {
                var sfx = SoundPlayer.PlaySoundInstance("Assets/sounds/bullet_destroy.ogg", SoundContext.Effect, 0.5f, pitchOverride: GameUtils.NaturalPitchShift);

                if (CameraGlobals.IsUsingFirstPersonCamera)
                    sfx.MaxVolume = SoundUtils.GetVolumeFromCameraPosition(Position3D, CameraGlobals.RebirthFreecam.Position);
            }

            GameHandler.Particles.MakeSmallExplosion(Position3D, 8, 10, 1.25f, 10);
        }

        TrailSound?.Instance?.Stop();
        TrailSound?.Dispose();
        TrailSound = null;

        // there's definitely a way to un-hardcode this
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

    public void Render() {
        DrawParams.Projection = CameraGlobals.GameProjection;
        DrawParams.View = CameraGlobals.GameView;

        // TODO: wtf? DoRaycast failing?
        //if (DebugManager.DebuggingEnabled && DebugManager.DebugLevel == 1 && Properties.Homing.Speed > 0)
        //    Collision.DoRaycast(Position, Properties.Homing.Target, (int)Properties.Homing.Radius, true);
        if (DebugManager.DebuggingEnabled) {
            DebugManager.DrawDebugString(TankGame.SpriteRenderer,
                $"RicochetsLeft: {RicochetsRemaining}" +
                $"\nTier: {Type}" +
                $"\nId: {Id}" +
                $"\nSgid: {VolleyId}",
                MatrixUtils.ConvertWorldToScreen(Vector3.Zero, DrawParams.World, DrawParams.View, DrawParams.Projection) - new Vector2(0, 20), 1,
                centered: true);

            DebugManager.DrawBoundingSphere(Hitbox, Color.White, CameraGlobals.GameView, CameraGlobals.GameProjection);
        }
        DrawShellMesh();
        ModdedData?.PostRender();
        OnPostRender?.Invoke(this);
    }

    void DrawShellMesh() {
        void RenderMeshEffects(ModelMesh mesh) {
            for (var j = 0; j < mesh.Effects.Count; j++) {
                var effect = (BasicEffect)mesh.Effects[j];
                effect.World = DrawParams.World;

                effect.View = DrawParams.View;
                effect.Projection = DrawParams.Projection;
                effect.TextureEnabled = true;

                effect.Texture = DrawParamsShell.ShellTexture;

                effect.SetDefaultGameLighting_IngameEntities();
            }
        }

        for (var i = 0; i < DrawParamsShell.Model.Meshes.Count; i++) {
            var mesh = DrawParamsShell.Model.Meshes[i];
            RenderMeshEffects(mesh);
            mesh.Draw();
        }
    }
    /// <summary>Check if this <see cref="Shell"/> is heading towards <paramref name="targetPosition"/>, based on <paramref name="arc"/>.</summary>
    /// <param name="targetPosition">The position to check whether or not this <see cref="Shell"/> is on a collision path with.</param>
    /// <param name="distance">The distance the target must be from this <see cref="Shell"/>.</param>
    /// <param name="arc">The arc length (from the angular rotation) of <see cref="Velocity"/> to <c>arc / 2</c> to check.</param>
    /// <returns></returns>
    public bool IsHeadingTowards(Vector2 targetPosition, float distance, float arc) {
        var rotation = Velocity != Vector2.Zero ? Velocity.ToRotation() : Vector2.UnitX.ToRotation();

        var rotToTarget = MathUtils.DirectionTo(Position, targetPosition).ToRotation();

        var inDistance = GameUtils.TanksDistance(Position, targetPosition) < distance;

        var angleBetween = MathUtils.AbsoluteAngleBetween(rotation, rotToTarget);

        var isInAngle = angleBetween <= arc / 2;

        // check if the direction 
        return isInAngle && inDistance;
    }
}