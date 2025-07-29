using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using TanksRebirth.Enums;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Utilities;
using System.Linq;
using TanksRebirth.Internals.Common.Framework.Audio;
using tainicom.Aether.Physics2D.Dynamics;
using TanksRebirth.GameContent.Systems;
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
using TanksRebirth.GameContent.Systems.ParticleSystem;
using TanksRebirth.GameContent.Systems.AI;
using TanksRebirth.Internals.Common.Framework.Collisions;

namespace TanksRebirth.GameContent.Systems.TankSystem;

public abstract class Tank {
    #region TexPack

    public static Dictionary<string, Texture2D> Assets = [];

    public static string AssetRoot;

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

    public static event DamageDelegate? OnDamage;

    public delegate void ApplyDefaultsDelegate(Tank tank, TankProperties properties);

    public static event ApplyDefaultsDelegate? PostApplyDefaults;

    public delegate void ShootDelegate(Tank tank, Shell shell);

    /// <summary>Does not run for spread-fire.</summary>
    public static event ShootDelegate? OnShoot;

    public delegate void LayMineDelegate(Tank tank, Mine mine);

    public static event LayMineDelegate? OnLayMine;

    public delegate void PreUpdateDelegate(Tank tank);

    public static event PreUpdateDelegate? OnPreUpdate;

    public delegate void PostUpdateDelegate(Tank tank);

    public static event PostUpdateDelegate? OnPostUpdate;

    #endregion

    public Shell? LastShotShell;

    public static bool ShowTeamVisuals = false;

    public static World CollisionsWorld = new(Vector2.Zero);
    public const float UNITS_PER_METER = 20f;

    public const float TNK_WIDTH = 25;
    public const float TNK_HEIGHT = 25;

    /// <summary>This <see cref="Tank"/>'s swag apparel as a <see cref="List{T}"/> of <see cref="IProp"/>s.</summary>
    public List<IProp> Props = [];

    #region Fields / Properties
    private float _oldRotation;
    public Body Physics { get; set; } = new();

    /// <summary>This <see cref="Tank"/>'s model. If this will be any different than the default, set <see cref="UsesCustomModel"/> to <c>true</c>.</summary>
    public Model Model { get; set; }
    public bool UsesCustomModel { get; set; }

    /// <summary>This <see cref="Tank"/>'s world position. Used to change the actual location of the model relative to the <see cref="View"/> and <see cref="Projection"/>.</summary>
    public Matrix World;
    /// <summary>How the <see cref="Model"/> is viewed through the <see cref="Projection"/>.</summary>
    public Matrix View;
    /// <summary>The projection from the screen to the <see cref="Model"/>.</summary>
    public Matrix Projection;

    public int WorldId { get; set; }
    /// <summary>This <see cref="Tank"/>'s <see cref="TeamID"/>.</summary>
    public int Team { get; set; }

    /// <summary>The current speed of this tank.</summary>
    public float Speed { get; set; }
    public float CurShootStun { get; private set; } = 0;
    public float CurShootCooldown { get; private set; } = 0;
    public float CurMineCooldown { get; private set; } = 0;
    public float CurMineStun { get; private set; } = 0;

    private int _oldShellLimit;

    public TankProperties Properties = new();

    public Tank[] TanksSpotted = [];
    public Shell[] OwnedShells = [];

    /// <summary>The *backend* length of the turret. Does not affect anything graphically.</summary>
    public float TurretLength = 20;
    public Vector2 TurretPosition => Position + new Vector2(0, TurretLength).Rotate(-TurretRotation);
    public Vector3 TurretPosition3D => new(TurretPosition.X, 11, TurretPosition.Y);
    public Vector2 Position {
        get => Physics.Position * UNITS_PER_METER;
        set => Physics.Position = value / UNITS_PER_METER;
    }

    public Vector2 Velocity;
    public Vector2 KnockbackVelocity;
    public BoundingBox Worldbox { get; set; }

    /// <summary>The 2D circle-represented hitbox of this <see cref="Tank"/>.</summary>
    public Circle CollisionCircle => new() { Center = Position, Radius = TNK_WIDTH / 2 };

    /// <summary>The 2D rectangle-represented hitbox of this <see cref="Tank"/>.</summary>
    public Rectangle CollisionBox => new((int)(Position.X - TNK_WIDTH / 2 + 3), (int)(Position.Y - TNK_WIDTH / 2 + 2),
        (int)TNK_WIDTH - 8, (int)TNK_HEIGHT - 4);

    /// <summary>How many <see cref="Shell"/>s this <see cref="Tank"/> owns.</summary>
    public int OwnedShellCount => OwnedShells.Count(x => x is not null);

    /// <summary>How many <see cref="Mine"/>s this <see cref="Tank"/> owns.</summary>
    public int OwnedMineCount { get; internal set; }

    /// <summary>Whether or not this <see cref="Tank"/> is currently turning.</summary>
    public bool IsTurning { get; internal set; }

    /// <summary>Whether or not this <see cref="Tank"/> is being hovered by the pointer.</summary>
    public bool IsHoveredByMouse { get; internal set; }
    /// <summary>The rotation of this <see cref="Tank"/>'s turret
    /// . Generally should not be modified in a player context.</summary>>
    public float TurretRotation { get; set; }

    /// <summary>The rotation of this <see cref="Tank"/>.</summary>
    public float TankRotation { get; set; }

    /// <summary>The rotation this <see cref="Tank"/> will pivot to.</summary>
    public float TargetTankRotation;

    /// <summary>Whether or not the tank has been destroyed or not.</summary>
    public bool Dead { get; set; }

    /// <summary>Whether or not this tank is used for ingame purposes or not.</summary>
    public bool IsIngame { get; set; } = true;

    public Vector3 Position3D => Position.ExpandZ();
    public Vector3 Velocity3D => Velocity.ExpandZ();
    public Vector3 Scaling = Vector3.One;

    #endregion

    #region Model Stuff

    internal Matrix[] _boneTransforms;

    internal ModelMesh _cannonMesh;

    public bool Flip;

    #endregion
    public static int[] GetActiveTeams(Func<Tank, bool>? predicate) {
        var teams = new List<int>();

        if (predicate != null) {
            var tanks = GameHandler.AllTanks.Where(predicate.Invoke).ToArray();
            foreach (var tank in tanks) {
                if (tank is null || tank.Dead) continue;
                if (teams.Contains(tank.Team)) continue;
                teams.Add(tank.Team);
            }
            return [.. teams];
        }
        // if no predicate is given, just get all teams
        foreach (var tank in GameHandler.AllTanks) {
            if (tank is null || tank.Dead) continue;
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
        Physics = CollisionsWorld.CreateCircle(TNK_WIDTH * 0.4f / UNITS_PER_METER, 1f, Position / UNITS_PER_METER,
            BodyType.Dynamic);
        Physics.Tag = this;
    }
    /// <summary>Initializes bone transforms and mesh assignments. You will want to call this method if you're modifying a tank model, and the new model
    /// contains a different number of bones than the original one.</summary>
    public void InitModelSemantics() {
        // for some reason Model is null when returning from campaign completion with certain mods.
        if (Model is null) {
            Model = this is PlayerTank ? ModelGlobals.TankPlayer.Asset : ModelGlobals.TankEnemy.Asset;
            TankGame.ClientLog.Write("Unexpected pitfall in initializing tank model semantics. Assigning defaults.", LogType.Warn);
        }

        _cannonMesh = Model.Meshes["Cannon"];
        _boneTransforms = new Matrix[Model.Bones.Count];
    }
    public void AddProp2D(Prop2D prop, Func<bool>? destroyOn = null) {
        if (Props.Contains(prop))
            return;
        Props.Add(prop);
        var particle = GameHandler.Particles.MakeParticle(Position3D + prop.RelativePosition, prop.Texture);

        particle.Scale = prop.Scale;
        particle.Tag = $"cosmetic_2d_{GetHashCode()}"; // store the hash code of this tank, so when we destroy the cosmetic's particle, it destroys all belonging to this tank!
        particle.HasAdditiveBlending = false;
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
    }
    void OnMissionStart() {
        DoInvisibilityGFXandSFX();
    }
    public void DoInvisibilityGFXandSFX() {
        const string invisibleTankSound = "Assets/sounds/tnk_invisible.ogg";

        if (Difficulties.Types["FFA"])
            Team = TeamID.NoTeam;
        if (!Properties.Invisible || Dead) return;

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

            var velocity = Vector2.UnitY.Rotate(MathHelper.ToRadians(360f / NUM_LOCATIONS * i));

            lpSmoke.Pitch = -CameraGlobals.DEFAULT_ORTHOGRAPHIC_ANGLE;

            lpSmoke.FaceTowardsMe = CameraGlobals.IsUsingFirstPresonCamera;

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
        if (DebugManager.SecretCosmeticSetting) {
            for (int i = 0; i < 1; i++) {
                var recieved = CosmeticChest.Basic.Open();

                if (recieved is Prop3D cosmetic1)
                    Props.Add(cosmetic1);
                else if (recieved is Prop2D cosmetic2)
                    AddProp2D(cosmetic2);
            }
        }

        GeneratePhysics();

        if (GameScene.Theme == MapTheme.Christmas)
            Props.Add(CosmeticChest.SantaHat);

        foreach (var cos in Props)
            if (cos is Prop2D cos2d)
                AddProp2D(cos2d);

        if (Difficulties.Types["BulletHell"])
            Properties.RicochetCount *= 3;
        if (Difficulties.Types["MachineGuns"]) {
            Properties.ShellCooldown = 5;
            Properties.ShellLimit = 50;
            Properties.ShootStun = 0;

            if (this is AITank tank)
                tank.Parameters.DetectionForgivenessHostile *= 2;
        }

        if (Difficulties.Types["Shotguns"]) {
            Properties.ShellSpread = 0.3f;
            Properties.ShellShootCount = 3;
            Properties.ShellLimit *= 3;

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
        PreUpdate();

        DecrementTimers();

        if (Dead) return;

        KnockbackVelocity.X = MathUtils.RoughStep(KnockbackVelocity.X, 0, 0.1f * RuntimeData.DeltaTime);
        KnockbackVelocity.Y = MathUtils.RoughStep(KnockbackVelocity.Y, 0, 0.1f * RuntimeData.DeltaTime);

        // magical multiplication number to maintain values like 1.8 max speed with the original game
        Physics.LinearVelocity = (Velocity * 0.55f + KnockbackVelocity) / UNITS_PER_METER;

        // try to make positive. i hate game
        World = Matrix.CreateScale(Scaling)
            * Matrix.CreateFromYawPitchRoll(-TankRotation - (Flip ? MathHelper.Pi : 0f), 0, 0)
            * Matrix.CreateTranslation(Position3D);

        Worldbox = new(Position3D - new Vector3(7, 0, 7), Position3D + new Vector3(10, 15, 10));

        if (IsTurning) {
            if (TargetTankRotation - TankRotation >= MathHelper.PiOver2) {
                TankRotation += MathHelper.Pi;
                Flip = !Flip;
            }
            else if (TargetTankRotation - TankRotation <= -MathHelper.PiOver2) {
                TankRotation -= MathHelper.Pi;
                Flip = !Flip;
            }
        }

        // * 2 since it's in both directions
        IsTurning = !(TankRotation > TargetTankRotation - Properties.MaximalTurn * 2 && TankRotation < TargetTankRotation + Properties.MaximalTurn * 2);

        // ensure no movements happen when not desired
        if (!MainMenuUI.IsActive && (!CampaignGlobals.InMission || IntermissionSystem.IsAwaitingNewMission))
            Velocity = Vector2.Zero;

        if (!IsTurning) {
            Speed += Properties.Acceleration * RuntimeData.DeltaTime;

            if (Speed > Properties.MaxSpeed)
                Speed = Properties.MaxSpeed;
        }
        else
            Speed *= Properties.Deceleration * (1f - RuntimeData.DeltaTime);

        // bigkitty told me that stuns instantly apply zero-velocity
        if (CurShootStun > 0 || CurMineStun > 0 || Properties.Stationary || (!CampaignGlobals.InMission && !MainMenuUI.IsActive)) {
            Velocity = Vector2.Zero;
            Speed = 0f;
        }

        // try to make negative. go poopoo
        _cannonMesh.ParentBone.Transform = Matrix.CreateRotationY(TurretRotation + TankRotation + (Flip ? MathHelper.Pi : 0));
        Model!.Root.Transform = World;

        Model.CopyAbsoluteBoneTransformsTo(_boneTransforms);

        if (!Properties.Stationary) {
            float treadPlaceTimer = 0;
            if (Velocity.Length() != 0) {
                treadPlaceTimer = MathF.Round(11 / Velocity.Length()) * Scaling.X;
                // MAYBE: change back to <= delta time if it doesn't work.
                if (RuntimeData.RunTime % treadPlaceTimer < RuntimeData.DeltaTime)
                    LayFootprint(Properties.TrackType == TrackID.Thick);
            }
            if (IsTurning && TankRotation != _oldRotation) {
                treadPlaceTimer = Properties.TurningSpeed * 150 * Scaling.X;
                // MAYBE: change back to <= delta time if it doesn't work.
                if (RuntimeData.RunTime % treadPlaceTimer < RuntimeData.DeltaTime)
                    LayFootprint(Properties.TrackType == TrackID.Thick);
            }
            if (!Properties.IsSilent && Velocity.Length() > 0.01) {
                // why did i clamp? i hate old code
                if (RuntimeData.RunTime % MathHelper.Clamp(treadPlaceTimer / 2, 4, 6) < RuntimeData.DeltaTime) {
                    // can use reflection to go over or under the pitch limits, remember that
                    Properties.TreadPitch = MathHelper.Clamp(Properties.TreadPitch, -1f, 1f);
                    var treadPlace = $"Assets/sounds/tnk_tread_place_{Client.ClientRandom.Next(1, 5)}.ogg";
                    var sfx = SoundPlayer.PlaySoundInstance(treadPlace, SoundContext.Effect, volume: Properties.TreadVolume, pitchOverride: Properties.TreadPitch);
                    sfx.Instance.Pitch = Properties.TreadPitch;

                    //if (CameraGlobals.IsUsingFirstPresonCamera)
                    //    SoundUtils.CreateSpatialSound(sfx, Position3D, CameraGlobals.RebirthFreecam.Position);
                }
            }
        }

        // fix 2d peeopled
        foreach (var cosmetic in Props)
            cosmetic?.UniqueBehavior?.Invoke(cosmetic, this);

        _oldRotation = TankRotation;
        timeSinceLastAction++;

        OnPostUpdate?.Invoke(this);
        PostUpdate();
    }
    public virtual void PreUpdate() { }
    public virtual void PostUpdate() { }
    /// <summary>Damage this <see cref="Tank"/>. If it has no armor, destroy it.</summary>
    public virtual void Damage(ITankHurtContext context, bool netSend, Color? colorOverride = null) {
        if (Dead || Properties.Immortal)
            return;

        Color popupColor;

        popupColor = context.Source is not null ? context.Source switch {
            PlayerTank pl => PlayerID.PlayerTankColors[pl.PlayerType],
            AITank ai => AITank.TankDestructionColors[ai.AiTankType],
            _ => Color.White
        } : colorOverride is null ? Color.White : colorOverride.Value;

        if (netSend)
            Client.SyncDamage(WorldId, popupColor);

        DoDamageTextPopup(popupColor);

        bool willDestroy = true;

        // this method returns 0 if Armor is null
        if (Properties.SafeGetArmorHitPoints() > 0) {
            Properties.Armor!.HitPoints--;
            var ding = SoundPlayer.PlaySoundInstance(
                $"Assets/sounds/armor_ding_{Client.ClientRandom.Next(1, 3)}.ogg", SoundContext.Effect);

            ding.Instance.Pitch = Client.ClientRandom.NextFloat(-0.1f, 0.1f);
            //if (CameraGlobals.IsUsingFirstPresonCamera)
            //    SoundUtils.CreateSpatialSound(ding, Position3D, CameraGlobals.RebirthFreecam.Position, 1.25f);
            OnDamage?.Invoke(this, Properties.Armor.HitPoints == 0, context);

            willDestroy = false;
        }

        if (this is AITank aiTank)
            aiTank.ModdedData?.TakeDamage(willDestroy, context);

        OnDamage?.Invoke(this, willDestroy, context);

        if (willDestroy)
            Destroy(context, netSend);
    }
    public void DoDamageTextPopup(Color color) {
        var part = GameHandler.Particles.MakeParticle(Position3D + new Vector3(0, 15, 0),
            TankGame.GameLanguage.Hit);

        part.IsIn2DSpace = true;
        part.ToScreenSpace = true;

        part.Color = color;

        part.HasAdditiveBlending = false;
        part.Origin2D = FontGlobals.RebirthFont.MeasureString(TankGame.GameLanguage.Hit) / 2;
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
            DrawUtils.DrawTextWithBorder(TankGame.SpriteRenderer, FontGlobals.RebirthFontLarge, particle.Text,
                MatrixUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(particle.Position),
                    CameraGlobals.GameView, CameraGlobals.GameProjection),
                particle.Color, Color.White, new(particle.Scale.X, particle.Scale.Y), 0f, Anchor.Center);
        };
    }
    /// <summary>Destroy this <see cref="Tank"/>.</summary>
    public virtual void Destroy(ITankHurtContext context, bool netSend) {
        CampaignGlobals.OnMissionStart -= OnMissionStart;

        // i think this is right? | (Probably, a destroyed tank is a dead tank!)
        Dead = true;

        const string tankDestroySound0 = "Assets/sounds/tnk_destroy.ogg";
        var destroy = SoundPlayer.PlaySoundInstance(tankDestroySound0, SoundContext.Effect, 0.2f);

        //if (CameraGlobals.IsUsingFirstPresonCamera)
        //    SoundUtils.CreateSpatialSound(destroy, Position3D, CameraGlobals.RebirthFreecam.Position, 1.25f);

        Properties.Armor?.Remove();

        DoDestructionEffects();

        Remove(false);
    }
    public void DoDestructionEffects() {
        for (int i = 0; i < 12; i++) {
            var tex = GameResources.GetGameResource<Texture2D>(Client.ClientRandom.Next(0, 2) == 0
                ? "Assets/textures/misc/tank_rock"
                : "Assets/textures/misc/tank_rock_2");

            var particle = GameHandler.Particles.MakeParticle(Position3D, tex);

            particle.HasAdditiveBlending = false;

            var vel = new Vector3(Client.ClientRandom.NextFloat(-3, 3), Client.ClientRandom.NextFloat(3, 6),
                Client.ClientRandom.NextFloat(-3, 3));

            particle.Roll = -CameraGlobals.DEFAULT_ORTHOGRAPHIC_ANGLE;

            particle.Scale = new(0.55f);

            particle.FaceTowardsMe = CameraGlobals.IsUsingFirstPresonCamera;

            particle.Color = Properties.DestructionColor;

            particle.UniqueBehavior = particle => { // Hide local var from outer scope with same name.
                particle.Pitch += MathF.Sin(particle.Position.Length() / 10) * RuntimeData.DeltaTime;
                vel.Y -= 0.2f;
                particle.Position += vel * RuntimeData.DeltaTime;
                particle.Alpha -= 0.025f * RuntimeData.DeltaTime;

                if (particle.Alpha <= 0f)
                    particle.Destroy();
            };
        }
        var explosionParticle = GameHandler.Particles.MakeParticle(Position3D,
                GameResources.GetGameResource<Texture2D>("Assets/textures/misc/bot_hit"));

        explosionParticle.Color = Color.Yellow * 0.75f;

        explosionParticle.ToScreenSpace = true;

        explosionParticle.Scale = new(50f);
        explosionParticle.TextureScale = new(4f);

        explosionParticle.HasAdditiveBlending = true;

        explosionParticle.IsIn2DSpace = true;

        explosionParticle.UniqueBehavior = (p) => {
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
        var fp = TankFootprint.Place(this, -TankRotation, alt);
        fp.Position += new Vector3(0, 0.15f, 0);
    }

    /// <summary>Shoot a <see cref="Shell"/> from this <see cref="Tank"/>.</summary>
    public virtual void Shoot(bool fxOnly, bool netSend = true) {
        if (!MainMenuUI.IsActive && !CampaignGlobals.InMission || !Properties.HasTurret)
            return;

        if (CurShootCooldown > 0 || OwnedShellCount >= Properties.ShellLimit / Properties.ShellShootCount)
            return;

        bool flip = false;
        float angle = 0f;

        var rotatedPos = Vector2.UnitY.Rotate(TurretRotation);

        if (!fxOnly) {
            var shell = new Shell(TurretPosition, new Vector2(-rotatedPos.X, rotatedPos.Y) * Properties.ShellSpeed,
                Properties.ShellType, this, Properties.RicochetCount, homing: Properties.ShellHoming);

            LastShotShell = shell;

            OnShoot?.Invoke(this, shell);

            if (this is AITank ai)
                ai.ModdedData?.Shoot(shell);

            // only send this code once for the shooting player
            if (netSend) {
                if (this is PlayerTank pt) {
                    if (NetPlay.IsClientMatched(pt.PlayerId))
                        Client.SyncShellFire(shell);
                }
                else
                    Client.SyncShellFire(shell);
            }
        }

        DoShootParticles();

        var force = (Position - TurretPosition) * Properties.Recoil;
        KnockbackVelocity = force / UNITS_PER_METER;

        if (!fxOnly) {
            for (int i = 1; i < Properties.ShellShootCount; i++) {
                // i == 0 : null, 0 rads
                // i == 1 : flipped, -0.15 rads
                // i == 2 : !flipped, 0.15 rads
                // i == 3 : flipped, -0.30 rads
                // i == 4 : !flipped, 0.30 rads
                flip = !flip;
                if ((i - 1) % 2 == 0)
                    angle += Properties.ShellSpread;
                var newAngle = flip ? -angle : angle;

                var shell = new Shell(Position, Vector2.Zero, Properties.ShellType, this,
                    homing: Properties.ShellHoming);
                rotatedPos = Vector2.UnitY.Rotate(TurretRotation);

                var newPos = Position + new Vector2(0, 20).Rotate(-TurretRotation + newAngle);

                shell.Position = new Vector2(newPos.X, newPos.Y);

                shell.Velocity = new Vector2(-rotatedPos.X, rotatedPos.Y).Rotate(newAngle) *
                                 Properties.ShellSpeed;

                shell.RicochetsRemaining = Properties.RicochetCount;
            }
        }

        timeSinceLastAction = 0;

        CurShootStun = Properties.ShootStun;
        CurShootCooldown = Properties.ShellCooldown;

        if (_oldShellLimit != Properties.ShellLimit)
            Array.Resize(ref OwnedShells, Properties.ShellLimit);

        _oldShellLimit = Properties.ShellLimit;
    }
    public void DoShootParticles() {
        var hit = GameHandler.Particles.MakeParticle(TurretPosition3D,
            GameResources.GetGameResource<Texture2D>("Assets/textures/misc/bot_hit"));

        var billboard = CameraGlobals.IsUsingFirstPresonCamera;

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

        timeSinceLastAction = 0;

        var mine = new Mine(this, Position, 600);

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
        if (!GameScene.ShouldRenderAll) return;
        if (Dead) return;

        Projection = CameraGlobals.GameProjection;
        View = CameraGlobals.GameView;

        if (!CampaignGlobals.InMission || !Properties.Invisible && CampaignGlobals.InMission) {
            foreach (var cosmetic in Props) {
                //if (GameProperties.InMission && Properties.Invisible)
                //break;
                if (cosmetic is not Prop3D cos3d)
                    continue;

                for (int i = 0; i < (Lighting.AccurateShadows ? 2 : 1); i++) {
                    foreach (var mesh in cos3d.PropModel.Meshes) {
                        if (cos3d.IgnoreMeshesByName.Any(meshName => meshName == mesh.Name))
                            continue;

                        foreach (BasicEffect effect in mesh.Effects) {
                            float rotY = TurretRotation;
                            if (cosmetic.LockOptions == PropLockOptions.ToTurret)
                                rotY = cosmetic.Rotation.Y + TurretRotation;
                            else if (cosmetic.LockOptions == PropLockOptions.ToTank)
                                rotY = cosmetic.Rotation.Y + TankRotation;
                            else if (cosmetic.LockOptions == PropLockOptions.ToTurretCentered)
                                cosmetic.RelativePosition = cosmetic.RelativePosition.RotateXZ(-rotY);


                            effect.World = i == 0 ? Matrix.CreateRotationX(cosmetic.Rotation.X) * Matrix.CreateRotationY(rotY) * Matrix.CreateRotationZ(cosmetic.Rotation.Z) * Matrix.CreateScale(cosmetic.Scale) * Matrix.CreateTranslation(Position3D + cosmetic.RelativePosition)
                                : Matrix.CreateRotationX(cosmetic.Rotation.X) * Matrix.CreateRotationY(cosmetic.Rotation.Y) * Matrix.CreateRotationZ(cosmetic.Rotation.Z) * Matrix.CreateScale(cosmetic.Scale) * Matrix.CreateTranslation(Position3D + cosmetic.RelativePosition) * Matrix.CreateShadow(Lighting.AccurateLightingDirection, new(Vector3.UnitY, 0)) * Matrix.CreateTranslation(0, 0.2f, 0);
                            effect.View = View;
                            effect.Projection = Projection;

                            effect.TextureEnabled = true;
                            effect.Texture = i == 0 ? cos3d.ModelTexture : GameResources.GetGameResource<Texture2D>("Assets/textures/ingame/block_shadow_h");
                            effect.SetDefaultGameLighting_IngameEntities();
                        }

                        mesh.Draw();
                    }
                }
            }
        }

        if (!DebugManager.DebuggingEnabled) return;

        var info = new string[] {
            $"Tank Rotation/Target: {TankRotation}/{TargetTankRotation}",
            $"WorldID: {WorldId}",
            this is AITank ai
                ? $"Turret Rotation/Target: {TurretRotation}/{ai.TargetTurretRotation}"
                : $"Turret Rotation: {TurretRotation}",
            $"OwnedShells/ShellsLeft: {OwnedShellCount}/{Properties.ShellLimit - OwnedShellCount}",
            $"{TanksSpotted.Length} tank(s) spotted"
        };

        // TankGame.spriteBatch.Draw(GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel"), CollisionBox2D, Color.White * 0.75f);

        if (DebugManager.DebugLevel != DebugManager.Id.EntityData) return;
        for (int i = 0; i < info.Length; i++) {
            var pos = MatrixUtils.ConvertWorldToScreen(Vector3.Up * 20, World, View, Projection) -
                new Vector2(0, i * 20);
            DrawUtils.DrawTextWithBorder(TankGame.SpriteRenderer, FontGlobals.RebirthFont, info[i], pos, 
                Color.Aqua, Color.Black, new Vector2(0.5f).ToResolution(), 0f, Anchor.TopCenter, 0.6f);
        }
    }
    public uint timeSinceLastAction = 15000;

    public virtual void Remove(bool nullifyMe) {
        if (CollisionsWorld.BodyList.Contains(Physics))
            CollisionsWorld.Remove(Physics);
        foreach (var particle in GameHandler.Particles.CurrentParticles) {
            if (particle is not null && particle.Tag is string tag) {
                if (tag == $"cosmetic_2d_{GetHashCode()}") // remove all particles related to this tank
                    particle.Destroy();
            }
        }
        CampaignGlobals.OnMissionStart -= OnMissionStart;
    }
}