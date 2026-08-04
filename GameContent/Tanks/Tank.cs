using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Utilities;
using System.Linq;
using TanksRebirth.Internals.Common.Framework.Audio;
using tainicom.Aether.Physics2D.Dynamics;
using System.Collections.Generic;
using System.IO;
using TanksRebirth.GameContent.Cosmetics;
using TanksRebirth.Graphics;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.ID;
using TanksRebirth.Net;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.GameContent.UI.MainMenu;
using TanksRebirth.GameContent.Globals.Assets;
using TanksRebirth.Graphics.Drawing;
using TanksRebirth.GameContent.Systems.ParticleSystem;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.Tanks.AI;

namespace TanksRebirth.GameContent.Tanks;
public abstract class Tank(bool ignoresRegister) {
    /// <summary>If true, this tank is not registered with the game entity lists and is managed manually.</summary>
    public bool IgnoreRegister = ignoresRegister;
    public struct TankDrawParams {
        public Texture2D? ShadowTexture;
        public Texture2D? TankTexture;
        /// <summary>If the tank has multiple parts to it (i.e: not a default tank that just uses a different texture), this maps out textures to each mesh.</summary>
        public ModelTextureMap TextureMap;

        public float ShadowAlpha;
        public float TankAlpha;

        // maybe turn into Resource<Model>
        /// <summary>This <see cref="Tank"/>'s model. If this will be any different than the default, set <see cref="UsesCustomModel"/> to <c>true</c>.</summary>
        public Model Model;

        /// <summary>Render-only. Used to make asymmetrical tank textures not visually 180 by rotating the drawn tank model by 180 degrees.</summary>
        public bool GraphicalFlip;

        public const float PLR_AMB_MUL = 2f;
        public const float AI_AMB_MUL = 0.9f;
    }

    public static bool ShowTeamVisuals = false;
    public static World CollisionsWorld = new(Vector2.Zero);
    public const float UNITS_PER_METER = 20f;
    public const float TNK_WIDTH = 25;
    public const float TNK_HEIGHT = 25;
    public const float TNK_DMG_COLL_Y = 11.0f;

    #region TexPack

    public static Dictionary<string, Texture2D?> Assets = [];

    public static string? AssetRoot;

    public static void SetAssetNames() {
        Assets.Clear();
        // TankTier.Collection.GetKey(tankToSpawnType)
        for (int i = TankID.Brown; i < TankID.Collection.Count; i++) {
            var tier = TankID.Collection.GetKey(i)!.ToLower();
            Assets.Add($"tank_" + tier, null);
        }
        for (int i = 0; i < PlayerID.Collection.Count; i++) {
            var tier = PlayerID.Collection.GetKey(i)!.ToLower();
            Assets.Add($"plrtank_" + tier, null);
        }
    }

    public static void LoadVanillaTextures() {
        AssetRoot = "Assets/textures/tank";

        for (int i = TankID.Brown; i < TankID.Collection.Count; i++) {
            var tier = TankID.Collection.GetKey(i)!.ToLower();

            var asset = $"tank_" + tier;
            Assets[asset] = GameResources.GetGameResource<Texture2D>($"{AssetRoot}/{asset}");
        }
        for (int i = 0; i < PlayerID.Collection.Count; i++) {
            var type = PlayerID.Collection.GetKey(i)!.ToLower();

            var asset = $"plrtank_" + type;
            Assets[asset] = GameResources.GetGameResource<Texture2D>($"{AssetRoot}/{asset}");
        }
    }

    public static void LoadTexturePack(string folder) {
        LoadVanillaTextures();
        if (folder.Equals("vanilla", StringComparison.InvariantCultureIgnoreCase)) {
            TankGame.ClientLog.Write($"Loaded vanilla textures for Tank.", LogType.Info);
            return;
        }

        var baseRoot = Path.Combine(TankGame.SaveDirectory, "Resource Packs");
        var rootGameScene = Path.Combine(baseRoot, "Tank");
        var path = Path.Combine(rootGameScene, folder);

        // ensure that these directories exist before dealing with them
        Directory.CreateDirectory(baseRoot);
        Directory.CreateDirectory(rootGameScene);

        if (!Directory.Exists(path)) {
            TankGame.ClientLog.Write($"Error: Directory '{path}' not found when attempting texture pack load.",
                LogType.Warn);
            return;
        }

        AssetRoot = path;

        foreach (var file in Directory.GetFiles(path)) {
            if (Assets.Any(type => type.Key == Path.GetFileNameWithoutExtension(file))) {
                Assets[Path.GetFileNameWithoutExtension(file)] = Texture2D.FromFile(TankGame.Instance.GraphicsDevice,
                    Path.Combine(path, Path.GetFileName(file)));
                TankGame.ClientLog.Write(
                    $"Texture pack '{folder}' overrided texture '{Path.GetFileNameWithoutExtension(file)}'",
                    LogType.Info);
            }
        }
    }

    #endregion

    #region Events

    public delegate void DamageDelegate(Tank victim, bool destroy, ITankHurtContext context);
    public delegate void ApplyDefaultsDelegate(Tank tank, TankProperties properties);
    public delegate void ShootDelegate(Tank tank, Shell shell);
    public delegate void LayMineDelegate(Tank tank, Mine mine);
    public delegate void PreUpdateDelegate(Tank tank);
    public delegate void PostUpdateDelegate(Tank tank);
    public delegate void FireDelegate(Tank tank);

    public static event DamageDelegate? OnDamage;
    public static event ApplyDefaultsDelegate? PostApplyDefaults;
    /// <summary>Ran for every spread-fire shot.</summary>
    public static event ShootDelegate? OnShoot;
    /// <summary>Ran once when a tank shoots.</summary>
    public static event FireDelegate? OnFire;
    public static event LayMineDelegate? OnLayMine;
    public static event PreUpdateDelegate? OnPreUpdate;
    public static event PostUpdateDelegate? OnPostUpdate;

    #endregion

    int _oldShellLimit;

    public TankExtras Extras = new();

    public bool CamTooClose;
    public Shell? LastShotShell;

    /// <summary>This <see cref="Tank"/>'s swag apparel as a <see cref="List{T}"/> of <see cref="IProp"/>s.</summary>
    public List<IProp> Props = [];
    readonly List<Particle> _propParticles = [];
    // readonly Dictionary<Prop3D, Model> _duplicatedModels = [];
    #region Fields / Properties
    float _oldRotation;
    public Body Physics { get; set; } = new();
    public bool UsesCustomModel { get; set; }
    public int WorldId { get; set; }
    /// <summary>This <see cref="Tank"/>'s <see cref="TeamID"/>.</summary>
    public int Team { get; set; }

    /// <summary>The current speed of this tank.</summary>
    public float Speed { get; set; }
    public float CurShootStun { get; private set; } = 0;
    public float CurShootCooldown { get; private set; } = 0;
    public float CurMineCooldown { get; private set; } = 0;
    public float CurMineStun { get; private set; } = 0;

    public float TimeSinceLastAction = 15000;

    public TankProperties Properties = new();

    public Tank[] TanksSpotted = [];
    public Shell[] OwnedShells = [];

    /// <summary>The *backend* length of the turret. Does not affect anything graphically.</summary>
    public float TurretLength = 20;

    public BasicDrawParams DrawParams = new();
    public TankDrawParams DrawParamsTank;
    /// <summary>The rotation of this <see cref="Tank"/>'s turret. Generally should not be modified in a player context.</summary>
    public float TurretRotation { get; set; }

    /// <summary>The rotation of this <see cref="Tank"/>'s chassis.</summary>
    public float ChassisRotation { get; set; }
    public Vector2 TurretPosition => Position + new Vector2(0, TurretLength).RotatedBy(-TurretRotation);
    public Vector3 TurretPosition3D => new(TurretPosition.X, OffsetY + TNK_DMG_COLL_Y, TurretPosition.Y);
    // would this better to just be set every frame rather than redundantly perform this calculation every time it's accessed?
    public Vector2 Position {
        get => Physics.Position * UNITS_PER_METER;
        set => Physics.Position = value / UNITS_PER_METER;
    }

    public Vector2 Velocity;
    public Vector2 KnockbackVelocity;
    public BoundingBox Worldbox { get; set; }

    public BoundingBox Hurtbox;
    // /// <summary>The 2D circle-represented hitbox of this <see cref="Owner"/>.</summary>
    // public Circle CollCircle => new() { Center = Position, Radius = TNK_WIDTH / 2 };

    // /// <summary>The 2D rectangle-represented hitbox of this <see cref="Owner"/>.</summary>
    //public Rectangle CollRect => new((int)(Position.X - TNK_WIDTH / 2 + 3), (int)(Position.Y - TNK_WIDTH / 2 + 2),
    //    (int)TNK_WIDTH - 8, (int)TNK_HEIGHT - 4);

    /// <summary>How many <see cref="Shell"/>s this <see cref="Tank"/> owns.</summary>
    public int OwnedShellCount => OwnedShells.Count(x => x is not null);

    /// <summary>How many <see cref="Mine"/>s this <see cref="Tank"/> owns.</summary>
    public int OwnedMineCount { get; internal set; }

    /// <summary>Whether or not this <see cref="Tank"/> is currently turning.</summary>
    public bool IsTurning { get; internal set; }

    // this feels really stupid to have as a property of the tank itself... oh well
    /// <summary>Whether or not this <see cref="Tank"/> is being hovered by the pointer.</summary>
    public bool IsHoveredByMouse { get; internal set; }

    /// <summary>The rotation this <see cref="Tank"/>'s chassis will pivot to.</summary>
    public float DesiredChassisRotation;

    /// <summary>Whether or not the tank has been destroyed or not.</summary>
    public bool IsDestroyed { get; set; }

    /// <summary>Positional Y offset. Physics will still occur on Y = 0, but hitbox/hurtboxes and drawing will occur at this Y.</summary>
    public float OffsetY;
    public Vector3 Position3D => Position.ExpandZ() + new Vector3(0, OffsetY, 0);
    public Vector3 Velocity3D => Velocity.ExpandZ();

    #endregion

    internal Matrix[] boneTransforms = [];
    internal ModelMesh? cannonMesh;

    public static int[] GetActiveTeams(Func<Tank, bool>? predicate) {
        var teams = new List<int>();

        if (predicate != null) {
            var tanks = GameHandler.AllTanks.Where(predicate.Invoke).ToArray();
            foreach (var tank in tanks) {
                if (tank is null || tank.IsDestroyed) continue;
                if (teams.Contains(tank.Team)) continue;
                teams.Add(tank.Team);
            }
            return [.. teams];
        }
        // if no predicate is given, just get all teams
        foreach (var tank in GameHandler.AllTanks) {
            if (tank is null || tank.IsDestroyed) continue;
            if (teams.Contains(tank.Team)) continue;
            teams.Add(tank.Team);
        }

        return [.. teams];
    }
    /// <summary>Initializes the physics body for this tank. Only use if you know what you're doing with it.</summary>
    public void GeneratePhysics() {
        //Scaling = new Vector3(1, 1, 3);
        //Body = CollisionsWorld.CreateEllipse(TNK_WIDTH * 0.4f / UNITS_PER_METER * Scaling.X, TNK_WIDTH * 0.4f / UNITS_PER_METER * Scaling.Z, 8, 1f, 
        //    Position / UNITS_PER_METER, bodyType: BodyType.Dynamic);
        Physics = CollisionsWorld.CreateCircle(TNK_WIDTH * 0.4f / UNITS_PER_METER * DrawParams.Scaling.X, 1f, Position / UNITS_PER_METER,
            BodyType.Dynamic);
        Physics.Tag = this;
    }
    /// <summary>Initializes bone transforms and mesh assignments. You will want to call this method if you're modifying a tank model, and the new model
    /// contains a different number of bones than the original one.</summary>
    public void InitModelSemantics() {
        // for some reason Model is null when returning from campaign completion with certain mods.
        if (DrawParamsTank.Model is null) {
            DrawParamsTank.Model = this is PlayerTank ? ModelGlobals.TankPlayer.Asset : ModelGlobals.TankEnemy.Asset;
            TankGame.ClientLog.Write("Unexpected pitfall in initializing tank model semantics. Assigning defaults.", LogType.Warn);
        }

        cannonMesh = DrawParamsTank.Model.Meshes["Cannon"];
        boneTransforms = new Matrix[DrawParamsTank.Model.Bones.Count];
    }
    void AddProp2D(Prop2D prop, Func<bool>? destroyOn = null) {
        if (Props.Contains(prop)) return;
        Props.Add(prop);
        var particle = GameHandler.Particles.MakeParticle(Position3D + prop.RelativePosition, prop.Texture);

        particle.Scale = prop.Scale;
        particle.HasAdditiveBlending = false;
        particle.Tag = _propParticles.Count; // tag is essentially an index
        // += vs =  ?
        // TODO: this prolly is the culprit of 2d cosmetics not doin nun
        particle.UniqueBehavior = particle => {
            particle.Position = Position3D + prop.RelativePosition;
            particle.Roll = prop.Rotation.X;
            particle.Pitch = prop.Rotation.Y;
            particle.Yaw = prop.Rotation.Z;
            particle.Scale = Properties.Invisible && CampaignGlobals.InMission ? Vector3.Zero : prop.Scale;

            if (destroyOn == null) return;

            if (destroyOn.Invoke())
                particle.Destroy();
        };

        _propParticles.Add(particle);
    }
    public void AddCosmetic(IProp prop) {
        if (prop is Prop3D p3d)
            Props.Add((Prop3D)p3d.Clone());
        else if (prop is Prop2D p2d)
            AddProp2D(p2d);
    }
    /*public void RemoveCosmetic(IProp prop) {
        if (prop is Prop3D p3d) {
            Props.Remove(p3d);
        }
        else if (prop is Prop2D p2d) {
            _propParticles.Remove(;
        }
    }*/
    public void RemoveCosmetics() {
        foreach (var item in _propParticles) {
            item.Destroy();
        }
        _propParticles.Clear();
        Props.Clear();
    }
    void OnMissionStart() {
        DoInvisibilityGFXandSFX();
    }
    public void DoInvisibilityGFXandSFX() {
        const string invisibleTankSound = "Assets/sounds/tnk_invisible.ogg";

        if (Modifiers.Map[Modifiers.FFA])
            Team = TeamID.NoTeam;
        if (!Properties.Invisible || IsDestroyed) return;

        SoundPlayer.PlaySoundInstance(invisibleTankSound, SoundContext.Effect, 0.3f);

        var lp1 = GameHandler.Particles.MakeParticle(Position3D,
            GameResources.GetGameResource<Texture2D>("Assets/textures/misc/light_particle"));

        var color = new Color(0, 200, 255, 181);

        lp1.Alpha = 1f;
        lp1.IsIn2DSpace = true;
        lp1.Color = color;

        lp1.UniqueBehavior = (lp) => {
            lp1.Position = Position3D;
            lp1.TextureScale = new(5);

            if (lp1.LifeTime > 75) {
                lp1.Alpha -= 0.02f * RuntimeData.DeltaTime;
                lp1.Scale -= new Vector3(0.02f);
            }

            if (lp1.Alpha <= 0)
                lp1.Destroy();
        };

        var lp2 = GameHandler.Particles.MakeParticle(Position3D,
            GameResources.GetGameResource<Texture2D>("Assets/textures/misc/light_particle"));

        lp2.Alpha = 1f;
        lp2.IsIn2DSpace = true;
        lp2.Color = color;
        lp2.Alpha = 0.4f;

        lp2.UniqueBehavior = (lp) => {
            lp2.Position = Position3D;
            lp2.TextureScale = new(15);

            if (lp2.LifeTime > 40) {
                lp2.Alpha -= 0.025f * RuntimeData.DeltaTime;
                lp1.Scale -= new Vector3(0.02f);
            }

            if (lp.Alpha <= 0)
                lp2.Destroy();
        };

        const int NUM_LOCATIONS = 8;

        for (int i = 0; i < NUM_LOCATIONS; i++) {
            var lpSmoke = GameHandler.Particles.MakeParticle(Position3D + new Vector3(0, 5, 0),
                GameResources.GetGameResource<Texture2D>("Assets/textures/misc/tank_smokes"));

            lpSmoke.Color = color;

            var velocity = Vector2.UnitY.RotatedBy(MathHelper.ToRadians(360f / NUM_LOCATIONS * i));

            lpSmoke.Pitch = -CameraGlobals.DEFAULT_ORTHOGRAPHIC_ANGLE;

            lpSmoke.FaceTowardsMe = CameraGlobals.IsUsingFirstPersonCamera;

            lpSmoke.Scale = new(0.75f);

            lpSmoke.UniqueBehavior = (elp) => {
                elp.Position.X += velocity.X * RuntimeData.DeltaTime;
                elp.Position.Z += velocity.Y * RuntimeData.DeltaTime;

                if (elp.LifeTime > 15) {
                    GeometryUtils.Add(ref elp.Scale, -0.03f * RuntimeData.DeltaTime);
                    elp.Alpha -= 0.03f * RuntimeData.DeltaTime;
                }

                if (elp.Scale.X <= 0f || elp.Alpha <= 0f)
                    elp.Destroy();
            };
        }
    }
    /// <summary>Apply all the default parameters for this <see cref="Tank"/>.</summary>
    public virtual void ApplyDefaults(ref TankProperties properties) {
        PostApplyDefaults?.Invoke(this, properties);
        Properties = properties;
    }
    public virtual void Initialize() {
        InitModelSemantics();
        DrawParams.Scaling = Vector3.One;
        if (DebugManager.SecretCosmeticSetting) {
            for (int i = 0; i < 1; i++) {
                var recieved = VanillaCosmetics.LootPool.Roll(out _);

                AddCosmetic(recieved);
            }
        }

        // deep clone the props...

        DrawParamsTank.ShadowAlpha = 0.5f;
        DrawParamsTank.TankAlpha = 1f;

        GeneratePhysics();

        // here be JOLLY
        if (GameScene.Theme == MapTheme.Christmas)
            Props.Add(VanillaCosmetics.SantaHat);

        foreach (var cos in Props.ToList())
            AddCosmetic(cos);
            //if (cos is Prop2D cos2d)
            //    AddProp2D(cos2d);

        if (Modifiers.Map[Modifiers.TRIPLE_BOUNCE])
            Properties.RicochetCount *= 3;
        if (Modifiers.Map[Modifiers.MACHINE_GUNS]) {
            Properties.ShellCooldown = 5;
            Properties.ShellLimit = 50;
            Properties.ShootStun = 0;

            // just slap ts in AITank???
            if (this is AITank tank)
                tank.Parameters.DetectionForgivenessHostile *= 2;
        }

        if (Modifiers.Map[Modifiers.SHOTGUNS]) {
            Properties.ShellSpread = 0.15f;
            Properties.ShellShootCount = 3;
            Properties.ShellLimit *= 3;
            Properties.Recoil = 2f;

            if (this is AITank tank)
                tank.Parameters.DetectionForgivenessHostile *= 2;
        }

        _oldShellLimit = Properties.ShellLimit;
        OwnedShells = new Shell[Properties.ShellLimit];
        CampaignGlobals.OnMissionStart += OnMissionStart;
    }
    void DecrementTimers() {
        if (CurShootStun > 0)
            CurShootStun -= RuntimeData.DeltaTime;
        if (CurShootCooldown > 0)
            CurShootCooldown -= RuntimeData.DeltaTime;
        if (CurMineStun > 0)
            CurMineStun -= RuntimeData.DeltaTime;
        if (CurMineCooldown > 0)
            CurMineCooldown -= RuntimeData.DeltaTime;
    }
    /// <summary>Update this <see cref="Tank"/>.</summary>
    public virtual void Update() {
        OnPreUpdate?.Invoke(this);

        DecrementTimers();

        // old boundingsphere impl
        // Hurtbox = new(Position3D + new Vector3(0, TNK_DMG_COLL_Y, 0), TNK_WIDTH * 0.4f);

        // used to be 0.7
        // * 0.65 because we already divide by 2 in our calculations
        // but maybe it needs a little shrink...?
        float hurtBoxSize = TNK_WIDTH * 0.65f;

        // could opt for not offsetting the hitbox Y
        var center = Position3D + new Vector3(0, 5, 0);
        Hurtbox = new(center - new Vector3(hurtBoxSize / 2, hurtBoxSize / 2, hurtBoxSize / 2),
                center + new Vector3(hurtBoxSize / 2, hurtBoxSize / 2, hurtBoxSize / 2));

        if (IsDestroyed) return;

        KnockbackVelocity.X = MathUtils.RoughStep(KnockbackVelocity.X, 0, 0.1f * RuntimeData.DeltaTime);
        KnockbackVelocity.Y = MathUtils.RoughStep(KnockbackVelocity.Y, 0, 0.1f * RuntimeData.DeltaTime);

        // magical multiplication number to maintain values like 1.8 max speed with the original game
        Physics.LinearVelocity = (Velocity * 0.55f + KnockbackVelocity) / UNITS_PER_METER;

        // try to make positive. i hate game
        DrawParams.World = Matrix.CreateScale(DrawParams.Scaling)
            * Matrix.CreateFromYawPitchRoll(-ChassisRotation - (DrawParamsTank.GraphicalFlip ? MathHelper.Pi : 0f), 0, 0)
            * Matrix.CreateTranslation(Position3D);

        Worldbox = new(Position3D - new Vector3(7, 0, 7), Position3D + new Vector3(10, 15, 10));

        // * 2 since it's in both directions
        IsTurning = !(ChassisRotation > DesiredChassisRotation - Properties.MaximalTurn * 2 && ChassisRotation < DesiredChassisRotation + Properties.MaximalTurn * 2);

        if (IsTurning) {
            if (DesiredChassisRotation - ChassisRotation >= MathHelper.PiOver2) {
                ChassisRotation += MathHelper.Pi;
                DrawParamsTank.GraphicalFlip = !DrawParamsTank.GraphicalFlip;
            }
            else if (DesiredChassisRotation - ChassisRotation <= -MathHelper.PiOver2) {
                ChassisRotation -= MathHelper.Pi;
                DrawParamsTank.GraphicalFlip = !DrawParamsTank.GraphicalFlip;
            }
            // used to be 1f - DeltaTime
            Speed *= (float)Math.Pow(Properties.Deceleration, RuntimeData.DeltaTime);
        }
        else {
            Speed = Math.Min(Properties.MaxSpeed, Speed + Properties.Acceleration * RuntimeData.DeltaTime);
        }

        // bigkitty told me that stuns instantly apply zero-velocity
        if (CurShootStun > 0 || CurMineStun > 0 || Properties.Stationary || !CampaignGlobals.InMission && !MainMenuUI.IsActive) {
            Velocity = Vector2.Zero;
            Speed = 0f;
        }

        // try to make negative. go poopoo
        SetBoneTransforms();

        static bool IsPeriodicTick(float period) =>
            period > 0f && RuntimeData.RunTime % period < RuntimeData.DeltaTime;

        if (!Properties.Stationary) {
            float speed = Velocity.Length();
            bool isMoving = speed != 0f;
            bool isRotating = IsTurning && ChassisRotation != _oldRotation;

            float moveTreadTimer = 0f;
            float turnTreadTimer = 0f;

            if (isMoving) {
                moveTreadTimer = MathF.Round(11 / speed) * DrawParams.Scaling.X;
                if (IsPeriodicTick(moveTreadTimer))
                    LayFootprint(Properties.TrackType == TrackID.Thick);
            }

            if (isRotating) {
                turnTreadTimer = Properties.TurningSpeed * 150 * DrawParams.Scaling.X;
                if (IsPeriodicTick(turnTreadTimer))
                    LayFootprint(Properties.TrackType == TrackID.Thick);
            }

            if (!Properties.IsSilent && speed > 0.01f) {
                // NOTE: previously this used whichever of the two timers was computed
                // last (turn overwrote move), which was likely accidental. Pick the
                // one that's actually intended to drive tread audio timing:
                float baseTimer = isRotating ? turnTreadTimer : moveTreadTimer;

                // for some slight randomness (so the noises dont all overlap)
                baseTimer %= (WorldId % 10) + 1;

                if (IsPeriodicTick(MathHelper.Clamp(baseTimer / 2, 4, 6))) {
                    // shouldnt be necessary anymore given oggaudio update
                    // Properties.TreadPitch = MathHelper.Clamp(Properties.TreadPitch, -1f, 1f);
                    var treadPlace = $"Assets/sounds/tnk_tread_place_{Client.ClientRandom.Next(1, 5)}.ogg";
                    var sfx = SoundPlayer.PlaySoundInstance(treadPlace, SoundContext.Effect, volume: Properties.TreadVolume, pitchOverride: Properties.TreadPitch);
                    sfx.Instance.Pitch = Properties.TreadPitch;
                }
            }
        }

        var camDist = Vector3.Distance(CameraGlobals.RebirthFreecam.Position, Position3D + new Vector3(0, CameraGlobals.POV_CAM_OFFSET_Y, 0));
        CamTooClose = CameraGlobals.IsUsingFirstPersonCamera && camDist < 10;

        // fix 2d peeopled
        if (!CamTooClose) {
            foreach (var cosmetic in Props) {
                cosmetic?.UniqueBehavior?.Invoke(cosmetic, this);
            }
        }

        _oldRotation = ChassisRotation;
        TimeSinceLastAction += RuntimeData.DeltaTime;

        OnPostUpdate?.Invoke(this);
    }
    /// <summary>Damage this <see cref="Tank"/>. If it has no armor, destroy it.</summary>
    public virtual void Damage(ITankHurtContext context, bool netSend, Color? colorOverride = null) {
        if (IsDestroyed) return;
        if (context is TankHurtContextShell && Properties.Resistance.HasFlag(ResistanceFlags.Shells)) return;
        if (context is TankHurtContextExplosion && Properties.Resistance.HasFlag(ResistanceFlags.Explosions)) return;

        var popupColor = context.Source is not null ? context.Source switch {
            PlayerTank pl => PlayerID.PlayerTankColors[pl.PlayerType],
            AITank ai => AITank.TankDestructionColors[ai.AiTankType],
            _ => Color.White
        } : colorOverride is null ? Color.White : colorOverride.Value;

        if (netSend)
            Client.SyncDamage(WorldId, popupColor);

        DoDamageTextPopup(popupColor);

        bool willDestroy = true;

        // this method returns 0 if Armor is null
        var hp = Extras.SafeGetArmorHitPoints();
        if (hp > 0) {
            Extras.Armor!.HitPoints--;
            var ding = SoundPlayer.PlaySoundInstance(
                $"Assets/sounds/armor_ding_{Client.ClientRandom.Next(1, 3)}.ogg", SoundContext.Effect);

            ding.Instance.Pitch = Client.ClientRandom.NextFloat(-0.1f, 0.1f);
            //if (CameraGlobals.IsUsingFirstPresonCamera)
            //    SoundUtils.CreateSpatialSound(ding, Position3D, CameraGlobals.RebirthFreecam.Position, 1.25f);

            willDestroy = false;
        }

        if (this is AITank aiTank)
            aiTank.ModdedData?.TakeDamage(willDestroy, context);

        if (willDestroy) {
            Destroy(context, netSend);
            return;
        }
        OnDamage?.Invoke(this, hp == 0, context);
    }
    public void DoDamageTextPopup(Color color) {
        var part = GameHandler.Particles.MakeParticle(Position3D + new Vector3(0, 15, 0),
            TankGame.GameLanguage.Gameplay.Hit);

        part.IsIn2DSpace = true;
        part.ToScreenSpace = true;

        part.Color = color;

        part.HasAdditiveBlending = false;
        part.Origin2D = FontGlobals.RebirthFont.MeasureString(TankGame.GameLanguage.Gameplay.Hit) / 2;
        part.Scale = new Vector3(Vector2.One.ToResolution(), 1);
        part.Alpha = 0;

        // TODO: Fix layering bullshit.
        part.Layer = 1;

        var origPos = part.Position;
        var speed = 0.5f;
        float height = 5f;

        part.UniqueBehavior = (a) => {
            var sin = MathF.Sin(RuntimeData.RunTime * speed) * height;
            part.Position.Y = origPos.Y + sin * RuntimeData.DeltaTime;

            if (a.LifeTime > 90)
                GeometryUtils.Add(ref a.Scale, -0.05f * RuntimeData.DeltaTime);

            height -= 0.02f;

            // ChatSystem.SendMessage(sin.ToString(), Color.White);

            if (a.Scale.X < 0)
                a.Destroy();
        };
        part.UniqueDraw = particle => {
            DrawUtils.DrawStringWithBorder(TankGame.SpriteRenderer, FontGlobals.RebirthFontLarge, particle.Text,
                MatrixUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(particle.Position),
                    CameraGlobals.GameView, CameraGlobals.GameProjection),
                particle.Color, Color.White, new(particle.Scale.X, particle.Scale.Y), 0f, Anchor.Center);
        };
    }
    /// <summary>Destroy this <see cref="Tank"/>.</summary>
    public virtual void Destroy(ITankHurtContext context, bool netSend) {
        CampaignGlobals.OnMissionStart -= OnMissionStart;

        // i think this is right? | (Probably, a destroyed tank is a dead tank!)
        IsDestroyed = true;

        const string tankDestroySound0 = "Assets/sounds/tnk_destroy.ogg";
        var destroy = SoundPlayer.PlaySoundInstance(tankDestroySound0, SoundContext.Effect, 0.2f);

        //if (CameraGlobals.IsUsingFirstPresonCamera)
        //    SoundUtils.CreateSpatialSound(destroy, Position3D, CameraGlobals.RebirthFreecam.Position, 1.25f);

        Extras.Armor?.Remove();

        DoDestructionEffects();

        // if Damage ends up calling Destroy, Damage itself will not invoke OnDamage, but Destroy will.
        OnDamage?.Invoke(this, true, context);
        Remove(false);
    }
    public void DoDestructionEffects() {
        for (int i = 0; i < 12; i++) {
            var tex = GameResources.GetGameResource<Texture2D>(Client.ClientRandom.Next(0, 2) == 0
                ? "Assets/textures/misc/tank_rock"
                : "Assets/textures/misc/tank_rock_2");

            var rock = GameHandler.Particles.MakeParticle(Position3D, tex);

            rock.HasAdditiveBlending = false;

            var vel = new Vector3(Client.ClientRandom.NextFloat(-3, 3), Client.ClientRandom.NextFloat(3, 6),
                Client.ClientRandom.NextFloat(-3, 3));

            rock.Roll = -CameraGlobals.DEFAULT_ORTHOGRAPHIC_ANGLE;
            rock.Scale = new(0.55f);
            rock.FaceTowardsMe = CameraGlobals.IsUsingFirstPersonCamera;
            rock.Color = Properties.DestructionColor;

            rock.UniqueBehavior = particle => { // Hide local var from outer scope with same name.
                rock.Pitch += MathF.Sin(rock.Position.Length() / 10) * RuntimeData.DeltaTime;
                vel.Y -= 0.2f * RuntimeData.DeltaTime;
                rock.Position += vel * RuntimeData.DeltaTime;
                rock.Alpha -= 0.025f * RuntimeData.DeltaTime;

                if (rock.Alpha <= 0f)
                    rock.Destroy();
            };
        }
        var expl = GameHandler.Particles.MakeParticle(Position3D,
                GameResources.GetGameResource<Texture2D>("Assets/textures/misc/bot_hit"));

        expl.Color = Color.Yellow * 0.75f;
        expl.ToScreenSpace = true;
        expl.Scale = new(50f);
        expl.TextureScale = new(4f);
        expl.HasAdditiveBlending = true;
        expl.IsIn2DSpace = true;

        expl.UniqueBehavior = (p) => {
            GeometryUtils.Add(ref p.Scale, -0.3f * RuntimeData.DeltaTime);
            p.Alpha -= 0.06f * RuntimeData.DeltaTime;
            if (p.Scale.X <= 0f)
                p.Destroy();
        };
        GameHandler.Particles.MakeSmallExplosion(Position3D, 15, 20, 1.3f, 15);
    }
    /// <summary>Lay a <see cref="TankFootprint"/> under this <see cref="Tank"/>.</summary>
    public virtual void LayFootprint(bool alt) {
        if (!Properties.CanLayTread)
            return;

        // will be TankRotation, Position, Scaling.FlattenZ()
        var fp = TankFootprint.Place(this, -ChassisRotation, alt);
        fp.Position += new Vector3(0, 0.15f, 0);
    }

    /// <summary>Shoot a <see cref="Shell"/> from this <see cref="Tank"/>.</summary>
    public virtual void Shoot(bool fxOnly = false, bool netSend = true) {
        if (!MainMenuUI.IsActive && !CampaignGlobals.InMission || !Properties.HasTurret) return;

        if (CurShootCooldown > 0) return;

        bool notEnoughShots = Properties.ShellLimit - OwnedShellCount < Properties.ShellShootCount;
        if (notEnoughShots) return;

        TankGame.MainThreadTasks.Enqueue(DoShootParticles);

        var force = (Position - TurretPosition) * Properties.Recoil;
        KnockbackVelocity = force / UNITS_PER_METER;

        if (!fxOnly) {
            foreach (var shell in ShootSpread()) {
                OnShoot?.Invoke(this, shell);

                // shitcode...
                if (this is AITank ai)
                    ai.ModdedData?.Shoot(shell);

                // this might be shitcode...
                if (netSend) {
                    if (this is PlayerTank pt) {
                        if (NetPlay.IsClientMatched(pt.PlayerId))
                            Client.SyncShellFire(shell);
                    }
                    else
                        Client.SyncShellFire(shell);
                }
            }
        }

        TimeSinceLastAction = 0;
        CurShootStun = Properties.ShootStun;
        CurShootCooldown = Properties.ShellCooldown;

        if (_oldShellLimit != Properties.ShellLimit)
            Array.Resize(ref OwnedShells, Properties.ShellLimit);

        OnFire?.Invoke(this);

        _oldShellLimit = Properties.ShellLimit;
    }
    IEnumerable<Shell> ShootSpread() {
        bool flip = false;
        float angle = 0f;

        var rotatedPos = Vector2.UnitY.RotatedBy(TurretRotation);

        var volley = (int)RuntimeData.UpdateCount % 10000;
        for (int i = 0; i < Properties.ShellShootCount; i++) {
            // i == 0 : null, 0 rads
            // i == 1 : flipped, -0.15 rads
            // i == 2 : !flipped, 0.15 rads
            // i == 3 : flipped, -0.30 rads
            // i == 4 : !flipped, 0.30 rads
            flip = !flip;
            if ((i - 1) % 2 == 0)
                angle += Properties.ShellSpread;

            var newAngle = flip ? -angle : angle;

            var shell = Shell.Create(Position, Vector2.Zero, Properties.ShellType, this);
            // this could be magical and lead to *super specific* edge cases but otherwise this is a decent way to put it
            shell.VolleyId = volley;
            shell.Properties.Homing = Properties.ShellHoming;

            var newPos = Position + new Vector2(0, 20).RotatedBy(-TurretRotation + newAngle);
            shell.Position = new Vector2(newPos.X, newPos.Y);
            shell.Velocity = new Vector2(-rotatedPos.X, rotatedPos.Y).RotatedBy(newAngle) * Properties.ShellSpeed;
            shell.RicochetsRemaining = Properties.RicochetCount;

            yield return shell;
        }
    }
    public void DoShootParticles() {
        var hit = GameHandler.Particles.MakeParticle(TurretPosition3D,
            GameResources.GetGameResource<Texture2D>("Assets/textures/misc/bot_hit"));

        var billboard = CameraGlobals.IsUsingFirstPersonCamera;

        hit.Pitch = -CameraGlobals.DEFAULT_ORTHOGRAPHIC_ANGLE;
        hit.Scale = new(0.5f);
        hit.FaceTowardsMe = billboard;
        hit.UniqueBehavior = (part) => {
            part.Color = Color.Orange;

            if (part.LifeTime > 1)
                part.Alpha -= 0.1f * RuntimeData.DeltaTime;
            if (part.Alpha <= 0)
                part.Destroy();
        };
        var smoke = GameHandler.Particles.MakeParticle(TurretPosition3D, GameResources.GetGameResource<Texture2D>("Assets/textures/misc/tank_smokes"));
        smoke.Pitch = -CameraGlobals.DEFAULT_ORTHOGRAPHIC_ANGLE;
        smoke.Scale = new(0.35f);
        var smokeInitColor = new Color(251, 122, 74, 255);
        smoke.HasAdditiveBlending = false;
        smoke.FaceTowardsMe = billboard;

        var fullLerpTime = 20f;

        smoke.UniqueBehavior = (p) => {
            var time = MathF.Min(smoke.LifeTime, fullLerpTime);
            smoke.Color = Color.Lerp(smokeInitColor, new Color(80, 80, 80), time / fullLerpTime);

            smoke.Scale += new Vector3(0.004f) * RuntimeData.DeltaTime;

            if (time == fullLerpTime) {
                smoke.Alpha -= 0.04f * RuntimeData.DeltaTime;

                if (smoke.Alpha <= 0)
                    smoke.Destroy();
            }
        };

        var ring = GameHandler.Particles.MakeParticle(TurretPosition3D, GameResources.GetGameResource<Texture2D>("Assets/textures/misc/ring"));
        ring.Pitch = -CameraGlobals.DEFAULT_ORTHOGRAPHIC_ANGLE;
        ring.Scale = new(0.4f);
        ring.HasAdditiveBlending = true;
        var ringInitColor = Color.Red; // new(251, 122, 74, 255);
        ring.FaceTowardsMe = billboard;

        var fullLerpTimeRing = 10f;

        ring.UniqueBehavior = (p) => {
            var time = MathF.Min(ring.LifeTime, fullLerpTimeRing);
            ring.Color = Color.Lerp(smokeInitColor, new Color(80, 80, 80), time / fullLerpTimeRing);

            ring.Scale += new Vector3(0.008f) * RuntimeData.DeltaTime;

            if (time == fullLerpTimeRing) {
                ring.Alpha -= 0.1f * RuntimeData.DeltaTime;

                if (ring.Alpha <= 0)
                    ring.Destroy();
            }
        };
    }

    /// <summary>Make this <see cref="Tank"/> lay a <see cref="Mine"/>.</summary>
    public virtual void LayMine() {
        if (CurMineCooldown > 0 || OwnedMineCount >= Properties.MineLimit)
            return;

        CurMineCooldown = Properties.MineCooldown;
        CurMineStun = Properties.MineStun;
        OwnedMineCount++;

        TimeSinceLastAction = 0;

        var mine = Mine.Create(this, Position, 600);

        // horrendous code
        if (this is PlayerTank pt) {
            if (NetPlay.IsClientMatched(pt.PlayerId))
                Client.SyncMinePlace(mine.Position, mine.DetonateTime, WorldId);
        }
        else
            Client.SyncMinePlace(mine.Position, mine.DetonateTime, WorldId);

        OnLayMine?.Invoke(this, mine);

        if (this is AITank ai)
            ai.ModdedData?.LayMine(mine);
    }

    public virtual void Render() {
        if (IsDestroyed) return;

        DrawParams.Projection = CameraGlobals.GameProjection;
        DrawParams.View = CameraGlobals.GameView;

        if (!CampaignGlobals.InMission || !Properties.Invisible && CampaignGlobals.InMission) {
            DrawProps();
        }

        if (!DebugManager.DebuggingEnabled) return;

        var info = new string[] {
            $"Tank Rotation/Target: {ChassisRotation}/{DesiredChassisRotation}",
            $"WorldID: {WorldId}",
            this is AITank ai
                ? $"Turret Rotation/Target: {TurretRotation}/{ai.TargetTurretRotation}"
                : $"Turret Rotation: {TurretRotation}",
            $"OwnedShells/ShellsLeft: {OwnedShellCount}/{Properties.ShellLimit - OwnedShellCount}",
            $"{TanksSpotted.Length} tank(s) spotted"
        };

        if (DebugManager.DebugLevel == DebugManager.Id.EntityData)
            DebugManager.DrawBoundingBox(Hurtbox, Color.White, CameraGlobals.GameView, CameraGlobals.GameProjection);

        // TankGame.spriteBatch.Draw(GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel"), CollisionBox2D, Color.White * 0.75f);

        if (DebugManager.DebugLevel != DebugManager.Id.EntityData) return;
        for (int i = 0; i < info.Length; i++) {
            var pos = MatrixUtils.ConvertWorldToScreen(Vector3.Up * 20, DrawParams.World, DrawParams.View, DrawParams.Projection) -
                new Vector2(0, i * 20);
            DrawUtils.DrawStringWithBorder(TankGame.SpriteRenderer, FontGlobals.RebirthFont, info[i], pos, 
                Color.Aqua, Color.Black, new Vector2(0.5f).ToResolution(), 0f, Anchor.TopCenter, 0.6f);
        }
    }
    void DrawProps() {
        if (CamTooClose) {
            // prevent particle drawing...?
            foreach (var particle in _propParticles) {
                particle.Position = new Vector3(0, 100000, 0);
            }
            return;
        }

        foreach (var cosmetic in Props) {
            if (cosmetic is not Prop3D cos3d)
                continue;

            foreach (var mesh in cos3d.PropModel.Asset.Meshes) {
                if (cos3d.IgnoreMeshesByName.Any(meshName => meshName == mesh.Name))
                    continue;

                foreach (BasicEffect effect in mesh.Effects) {
                    float rotY = TurretRotation;
                    if (cosmetic.LockOptions == PropLockOptions.ToTurret)
                        rotY = cosmetic.Rotation.Y + TurretRotation;
                    else if (cosmetic.LockOptions == PropLockOptions.ToTank)
                        rotY = cosmetic.Rotation.Y + -ChassisRotation;
                    else if (cosmetic.LockOptions == PropLockOptions.AroundTurret)
                        cosmetic.RelativePosition = cosmetic.RelativePosition.RotateXZ(-rotY);

                    var baseMatrix = Matrix.CreateRotationX(cosmetic.Rotation.X) * Matrix.CreateRotationY(rotY) * Matrix.CreateRotationZ(cosmetic.Rotation.Z) * Matrix.CreateScale(cosmetic.Scale) * Matrix.CreateTranslation(Position3D + cosmetic.RelativePosition);
                    effect.World = baseMatrix;
                    effect.View = DrawParams.View;
                    effect.Projection = DrawParams.Projection;

                    // hover highlight
                    if (IsHoveredByMouse)
                        effect.EmissiveColor = Color.White.ToVector3();
                    else
                        effect.EmissiveColor = Color.Black.ToVector3();

                    if (ShowTeamVisuals) {
                        if (Team != TeamID.NoTeam) {
                            var ex = new Color[1024];

                            Array.Fill(ex, TeamID.TeamColors[Team]);

                            effect.Texture?.SetData(0, new Rectangle(0, 0, 32, 9), ex, 0, 288);
                            effect.Texture?.SetData(0, new Rectangle(0, 23, 32, 9), ex, 0, 288);
                        }
                    }

                    effect.TextureEnabled = true;
                    effect.Texture = cos3d.ModelTexture;
                    effect.SetDefaultGameLighting_IngameEntities(DrawParams.LightPower, DrawParams.AmbientPower, DrawParams.UsePhong, DrawParams.LightDirection);
                }

                mesh.Draw();
            }
        }
    }
    /// <summary>Checks if this <see cref="Tank"/> is on the same team as the passed-in <see cref="TeamID"/> and is not on <see cref="TeamID.NoTeam"/>.</summary>
    public bool IsOnSameTeamAs(int otherTeam) => Team == otherTeam && Team != TeamID.NoTeam && otherTeam != TeamID.NoTeam;
    public virtual void Remove(bool nullifyMe) {
        if (CollisionsWorld.BodyList.Contains(Physics))
            CollisionsWorld.Remove(Physics);

        RemoveCosmetics();
        CampaignGlobals.OnMissionStart -= OnMissionStart;
    }
    public void SetBoneTransforms() {
        // commented = old
        cannonMesh!.ParentBone.Transform = // Matrix.CreateRotationY(TurretRotation + ChassisRotation + (DrawParamsTank.GraphicalFlip ? MathHelper.Pi : 0));
            Matrix.CreateRotationY(TurretRotation + ChassisRotation + (DrawParamsTank.GraphicalFlip ? MathHelper.Pi : 0));
        DrawParamsTank.Model!.Root.Transform = DrawParams.World;

        DrawParamsTank.Model.CopyAbsoluteBoneTransformsTo(boneTransforms);
    }
}