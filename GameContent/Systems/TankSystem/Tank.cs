using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using TanksRebirth.Enums;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Utilities;
using System.Linq;
using TanksRebirth.Internals.Common.Framework.Audio;
using Microsoft.Xna.Framework.Audio;
using tainicom.Aether.Physics2D.Dynamics;
using TanksRebirth.Internals.Common.Framework;
using TanksRebirth.GameContent.Systems;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TanksRebirth.GameContent.Cosmetics;
using TanksRebirth.Graphics;
using TanksRebirth.GameContent.UI;
using TanksRebirth.GameContent.Properties;
using TanksRebirth.GameContent.ID;
using TanksRebirth.Net;
using TanksRebirth.GameContent.ModSupport;
using TanksRebirth.GameContent.RebirthUtils;

namespace TanksRebirth.GameContent;

public abstract class Tank {
    #region TexPack

    public static Dictionary<string, Texture2D> Assets = new();

    public static string AssetRoot;

    public static void SetAssetNames() {
        Assets.Clear();
        // TankTier.Collection.GetKey(tankToSpawnType)
        foreach (var tier in TankID.Collection.Keys.Where(tier =>
                     TankID.Collection.GetValue(tier) > TankID.Random &&
                     TankID.Collection.GetValue(tier) < TankID.Explosive))
            Assets.Add($"tank_" + tier.ToLower(), null);
        foreach (var type in PlayerID.Collection.Keys)
            Assets.Add($"tank_" + type.ToLower(), null);
    }

    public static void LoadVanillaTextures() {
        Assets.Clear();

        foreach (var tier in TankID.Collection.Keys.Where(tier =>
                     TankID.Collection.GetValue(tier) > TankID.Random &&
                     TankID.Collection.GetValue(tier) < TankID.Explosive))
            if (!Assets.ContainsKey($"tank_" + tier.ToLower()))
                Assets.Add($"tank_" + tier.ToLower(),
                    GameResources.GetGameResource<Texture2D>($"Assets/textures/tank/tank_{tier.ToLower()}"));
        foreach (var type in PlayerID.Collection.Keys)
            if (!Assets.ContainsKey($"tank_" + type.ToLower()))
                Assets.Add($"tank_" + type.ToLower(),
                    GameResources.GetGameResource<Texture2D>($"Assets/textures/tank/tank_{type.ToLower()}"));
        AssetRoot = "Assets/textures/tank";
    }

    public static void LoadTexturePack(string folder) {
        LoadVanillaTextures();
        if (folder.ToLower() == "vanilla") {
            GameHandler.ClientLog.Write($"Loaded vanilla textures for Tank.", LogType.Info);
            return;
        }

        var baseRoot = Path.Combine(TankGame.SaveDirectory, "Resource Packs");
        var rootGameScene = Path.Combine(baseRoot, "Tank");
        var path = Path.Combine(rootGameScene, folder);

        // ensure that these directories exist before dealing with them
        Directory.CreateDirectory(baseRoot);
        Directory.CreateDirectory(rootGameScene);

        if (!Directory.Exists(path)) {
            GameHandler.ClientLog.Write($"Error: Directory '{path}' not found when attempting texture pack load.",
                LogType.Warn);
            return;
        }

        AssetRoot = path;

        foreach (var file in Directory.GetFiles(path)) {
            if (Assets.Any(type => type.Key == Path.GetFileNameWithoutExtension(file))) {
                Assets[Path.GetFileNameWithoutExtension(file)] = Texture2D.FromFile(TankGame.Instance.GraphicsDevice,
                    Path.Combine(path, Path.GetFileName(file)));
                GameHandler.ClientLog.Write(
                    $"Texture pack '{folder}' overrided texture '{Path.GetFileNameWithoutExtension(file)}'",
                    LogType.Info);
            }
        }
    }

    #endregion

    #region Events

    public delegate void DamageDelegate(Tank victim, bool destroy, ITankHurtContext context);

    public static event DamageDelegate? OnDamage;

    public delegate void ApplyDefaultsDelegate(Tank tank, ref TankProperties properties);

    public static event ApplyDefaultsDelegate? PostApplyDefaults;

    public delegate void ShootDelegate(Tank tank, ref Shell shell);

    /// <summary>Does not run for spread-fire.</summary>
    public static event ShootDelegate? OnShoot;

    public delegate void LayMineDelegate(Tank tank, ref Mine mine);

    public static event LayMineDelegate? OnLayMine;

    public delegate void PreUpdateDelegate(Tank tank);

    public static event PreUpdateDelegate? OnPreUpdate;

    public delegate void PostUpdateDelegate(Tank tank);

    public static event PostUpdateDelegate? OnPostUpdate;

    #endregion

    public static bool ShowTeamVisuals = false;

    public static World CollisionsWorld = new(Vector2.Zero);
    public const float UNITS_PER_METER = 20f;

    public const float TNK_WIDTH = 25;
    public const float TNK_HEIGHT = 25;

    #region Fields / Properties

    public Body Body { get; set; } = new();

    /// <summary>This <see cref="Tank"/>'s model.</summary>
    public Model Model { get; set; }

    /// <summary>This <see cref="Tank"/>'s world position. Used to change the actual location of the model relative to the <see cref="View"/> and <see cref="Projection"/>.</summary>
    public Matrix World { get; set; }

    /// <summary>How the <see cref="Model"/> is viewed through the <see cref="Projection"/>.</summary>
    public Matrix View { get; set; }

    /// <summary>The projection from the screen to the <see cref="Model"/>.</summary>
    public Matrix Projection { get; set; }

    public int WorldId { get; set; }

    public TankProperties Properties = new();

    private int _oldShellLimit;
    public Shell[] OwnedShells = Array.Empty<Shell>();

    public Vector2 Position;
    public Vector2 Velocity;

    /// <summary>This <see cref="Tank"/>'s <see cref="TeamID"/>.</summary>
    public int Team { get; set; }

    /// <summary>The rotation of this <see cref="Tank"/>'s barrel. Generally should not be modified in a player context.</summary>>
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

    #region ModelBone & ModelMesh

    internal Matrix[] _boneTransforms;

    internal ModelMesh _cannonMesh;

    #endregion

    public static int[] GetActiveTeams() {
        var teams = new List<int>();
        foreach (var tank in GameHandler.AllTanks) {
            if (tank is not null && !tank.Dead) {
                if (!teams.Contains(tank.Team))
                    teams.Add(tank.Team);
            }
        }

        return [.. teams];
    }

    /// <summary>This <see cref="Tank"/>'s swag apparel as a <see cref="List{T}"/> of <see cref="ICosmetic"/>s.</summary>
    public List<ICosmetic> Cosmetics = new();

    /// <summary>Apply all the default parameters for this <see cref="Tank"/>.</summary>
    public virtual void ApplyDefaults(ref TankProperties properties) {
        PostApplyDefaults?.Invoke(this, ref properties);
        if (this is AITank ai) {
            for (int i = 0; i < ModLoader.ModTanks.Length; i++) {
                if (ai.AiTankType == ModLoader.ModTanks[i].Type) {
                    properties = ModLoader.ModTanks[i].Properties;
                    ai.AiParams = ModLoader.ModTanks[i].AiParameters;
                    ModLoader.ModTanks[i].PostApplyDefaults(ai);
                    return;
                }
            }

        }
        Properties = properties;
    }

    public virtual void Initialize() {
        OwnedShells = new Shell[Properties.ShellLimit];

        _cannonMesh = Model!.Meshes["Cannon"];
        _boneTransforms = new Matrix[Model.Bones.Count];
        if (TankGame.SecretCosmeticSetting) {
            for (int i = 0; i < 1; i++) {
                var recieved = CosmeticChest.Basic.Open();

                if (recieved is Cosmetic3D cosmetic1)
                    Cosmetics.Add(cosmetic1);
                else if (recieved is Cosmetic2D cosmetic2)
                    Add2DCosmetic(cosmetic2);
            }
        }

        if (MapRenderer.Theme == MapTheme.Christmas)
            Cosmetics.Add(CosmeticChest.SantaHat);

        Body = CollisionsWorld.CreateCircle(TNK_WIDTH * 0.4f / UNITS_PER_METER, 1f, Position / UNITS_PER_METER,
            BodyType.Dynamic);

        foreach (var cos in Cosmetics)
            if (cos is Cosmetic2D cos2d)
                Add2DCosmetic(cos2d);

        if (IsIngame) {
            if (Difficulties.Types["BulletHell"])
                Properties.RicochetCount *= 3;
            if (Difficulties.Types["MachineGuns"]) {
                Properties.ShellCooldown = 5;
                Properties.ShellLimit = 50;
                Properties.ShootStun = 0;

                if (this is AITank tank)
                    tank.AiParams.Inaccuracy *= 2;
            }

            if (Difficulties.Types["Shotguns"]) {
                Properties.ShellSpread = 0.3f;
                Properties.ShellShootCount = 3;
                Properties.ShellLimit *= 3;

                if (this is AITank tank)
                    tank.AiParams.Inaccuracy *= 2;
            }
        }

        GameProperties.OnMissionStart += OnMissionStart;
    }

    public void Add2DCosmetic(Cosmetic2D cos2d, Func<bool> destroyOn = null) {
        if (!MainMenu.Active && IsIngame) {
            if (Cosmetics.Contains(cos2d))
                return;
            Cosmetics.Add(cos2d);
            var particle = GameHandler.Particles.MakeParticle(Position3D + cos2d.RelativePosition, cos2d.Texture);

            particle.Scale = cos2d.Scale;
            particle.Tag =
                $"cosmetic_2d_{GetHashCode()}"; // store the hash code of this tank, so when we destroy the cosmetic's particle, it destroys all belonging to this tank!
            particle.HasAddativeBlending = false;
            // += vs =  ?
            // TODO: this prolly is the culprit of 2d cosmetics not doin nun
            particle.UniqueBehavior = particle => {
                particle.Position = Position3D + cos2d.RelativePosition;
                particle.Roll = cos2d.Rotation.X;
                particle.Pitch = cos2d.Rotation.Y;
                particle.Yaw = cos2d.Rotation.Z;
                particle.Scale = (Properties.Invisible && GameProperties.InMission) ? Vector3.Zero : cos2d.Scale;

                if (destroyOn == null) return;

                if (destroyOn.Invoke())
                    particle.Destroy();
            };
        }
    }

    void OnMissionStart() {
        const string invisibleTankSound = "Assets/sounds/tnk_invisible.ogg";

        if (Difficulties.Types["FFA"])
            Team = TeamID.NoTeam;
        if (!Properties.Invisible || Dead) return;

        SoundPlayer.PlaySoundInstance(invisibleTankSound, SoundContext.Effect, 0.3f, gameplaySound: true);

        var lightParticle = GameHandler.Particles.MakeParticle(Position3D,
            GameResources.GetGameResource<Texture2D>("Assets/textures/misc/light_particle"));

        // lightParticle.TextureScale = new(5f);
        lightParticle.Alpha = 1;
        lightParticle.IsIn2DSpace = true;
        lightParticle.Color = Color.SkyBlue;

        lightParticle.UniqueBehavior = (lp) => {
            lp.Position = Position3D;
            lp.TextureScale = new(5);

            if (lp.LifeTime > 90)
                lp.Alpha -= 0.01f * TankGame.DeltaTime;

            if (lp.Alpha <= 0)
                lp.Destroy();
            /*if (lp.Scale.X < 5f)
                GeometryUtils.Add(ref lp.Scale, 0.12f * TankGame.DeltaTime);
            if (lp.Alpha < 1f && lp.Scale.X < 5f)
                lp.Alpha += 0.02f * TankGame.DeltaTime;

            if (lp.LifeTime > 90)
                lp.Alpha -= 0.005f * TankGame.DeltaTime;

            if (lp.Scale.X < 0f)
                lp.Destroy();*/
        };

        const int NUM_LOCATIONS = 8;

        for (int i = 0; i < NUM_LOCATIONS; i++) {
            var lp = GameHandler.Particles.MakeParticle(Position3D + new Vector3(0, 5, 0),
                GameResources.GetGameResource<Texture2D>("Assets/textures/misc/tank_smokes"));

            lp.Color = Color.SkyBlue;

            var velocity = Vector2.UnitY.RotatedByRadians(MathHelper.ToRadians(360f / NUM_LOCATIONS * i));

            lp.Scale = new(1f);

            lp.UniqueBehavior = (elp) => {
                elp.Position.X += velocity.X * TankGame.DeltaTime;
                elp.Position.Z += velocity.Y * TankGame.DeltaTime;

                if (elp.LifeTime > 15) {
                    GeometryUtils.Add(ref elp.Scale, -0.03f * TankGame.DeltaTime);
                    elp.Alpha -= 0.03f * TankGame.DeltaTime;
                }

                if (elp.Scale.X <= 0f || elp.Alpha <= 0f)
                    elp.Destroy();
            };
        }
    }

    public bool Flip;

    private float _oldRotation;

    /// <summary>Update this <see cref="Tank"/>.</summary>
    public virtual void Update() {
        if (Dead || !MapRenderer.ShouldRenderAll)
            return;

        OnPreUpdate?.Invoke(this);
        if (this is AITank ai) {
            for (int i = 0; i < ModLoader.ModTanks.Length; i++)
                if (ai.AiTankType == ModLoader.ModTanks[i].Type)
                    ModLoader.ModTanks[i].PreUpdate(ai);
        }

        Position = Body.Position * UNITS_PER_METER;

        Body.LinearVelocity = Velocity * 0.55f / UNITS_PER_METER;

        // try to make positive. i hate game
        World = Matrix.CreateScale(Scaling) * Matrix.CreateFromYawPitchRoll(-TankRotation - (Flip ? MathHelper.Pi : 0f),
                                                0, 0)
                                            * Matrix.CreateTranslation(Position3D);

        Worldbox = new(Position3D - new Vector3(7, 0, 7), Position3D + new Vector3(10, 15, 10));

        if (IsTurning) {
            // var real = TankRotation + MathHelper.PiOver2;
            if (TargetTankRotation - TankRotation >= MathHelper.PiOver2) {
                TankRotation += MathHelper.Pi;
                Flip = !Flip;
            }
            else if (TargetTankRotation - TankRotation <= -MathHelper.PiOver2) {
                TankRotation -= MathHelper.Pi;
                Flip = !Flip;
            }
        }

        IsTurning = !(TankRotation > TargetTankRotation - Properties.MaximalTurn - MathHelper.ToRadians(5) &&
                      TankRotation < TargetTankRotation + Properties.MaximalTurn + MathHelper.ToRadians(5));

        if (!MainMenu.Active && (!GameProperties.InMission || IntermissionSystem.IsAwaitingNewMission))
            Velocity = Vector2.Zero;
        if (OwnedMineCount < 0)
            OwnedMineCount = 0;
        if (DecelerationRateDecayTime > 0)
            DecelerationRateDecayTime -= TankGame.DeltaTime;

        if (!Properties.Stationary) {
            float treadPlaceTimer = 0;
            if (Velocity.Length() != 0) {
                treadPlaceTimer = MathF.Round(11 / Velocity.Length());
                // MAYBE: change back to <= delta time if it doesn't work.
                if (TankGame.RunTime % treadPlaceTimer < TankGame.DeltaTime)
                    LayFootprint(Properties.TrackType == TrackID.Thick);
            }
            if (IsTurning && TankRotation != _oldRotation) {
                treadPlaceTimer = Properties.TurningSpeed * 150;
                // MAYBE: change back to <= delta time if it doesn't work.
                if (TankGame.RunTime % treadPlaceTimer < TankGame.DeltaTime)
                    LayFootprint(Properties.TrackType == TrackID.Thick);
            }
            if (!Properties.IsSilent && Velocity.Length() > 0) {
                // why did i clamp? i hate old code
                if (TankGame.RunTime % MathHelper.Clamp(treadPlaceTimer / 2, 4, 6) < TankGame.DeltaTime) {
                    Properties.TreadPitch = MathHelper.Clamp(Properties.TreadPitch, -1f, 1f);
                    var treadPlace = $"Assets/sounds/tnk_tread_place_{GameHandler.GameRand.Next(1, 5)}.ogg";
                    var sfx = SoundPlayer.PlaySoundInstance(treadPlace, SoundContext.Effect, Properties.TreadVolume, 0f,
                        Properties.TreadPitch, gameplaySound: true);
                    sfx.Instance.Pitch = Properties.TreadPitch;
                }
            }
        }

        if (!IsTurning) {
            IsTurning = false;
            Speed += Properties.Acceleration * TankGame.DeltaTime;
            if (Speed > Properties.MaxSpeed)
                Speed = Properties.MaxSpeed;
        }

        if (IsTurning || CurShootStun > 0 || CurMineStun > 0 || Properties.Stationary) {
            Speed -= Properties.Deceleration /* * (DecelerationRateDecayTime > 0 ? 0.25f : 1f)*/ * TankGame.DeltaTime;
            if (Speed < 0)
                Speed = 0;
            IsTurning = true;
        }

        // try to make negative. go poopoo
        _cannonMesh.ParentBone.Transform =
            Matrix.CreateRotationY(TurretRotation + TankRotation + (Flip ? MathHelper.Pi : 0));
        Model!.Root.Transform = World;

        Model.CopyAbsoluteBoneTransformsTo(_boneTransforms);

        if (CurShootStun > 0)
            CurShootStun -= TankGame.DeltaTime;
        if (CurShootCooldown > 0)
            CurShootCooldown -= TankGame.DeltaTime;
        if (CurMineStun > 0)
            CurMineStun -= TankGame.DeltaTime;
        if (CurMineCooldown > 0)
            CurMineCooldown -= TankGame.DeltaTime;

        // fix 2d peeopled
        foreach (var cosmetic in Cosmetics)
            cosmetic?.UniqueBehavior?.Invoke(cosmetic, this);

        _oldRotation = TankRotation;

        OnPostUpdate?.Invoke(this);
    }

    /// <summary>Damage this <see cref="Tank"/>. If it has no armor, <see cref="Destroy"/> it.</summary>
    public virtual void Damage(ITankHurtContext context) {
        if (Dead || Properties.Immortal)
            return;

        // if (Server.serverNetManager != null)
        Client.SyncDamage(WorldId);

        void doTextPopup() {
            var part = GameHandler.Particles.MakeParticle(Position3D + new Vector3(0, 15, 0),
                TankGame.GameLanguage.Hit);

            part.IsIn2DSpace = true;
            part.ToScreenSpace = true;

            // Switch on the context the tank got hurt on.
            part.Color = context switch {
                TankHurtContextMine thcm => thcm.MineExplosion.Source switch {
                    PlayerTank pl => PlayerID.PlayerTankColors[pl.PlayerType].ToColor(),
                    AITank ai => AITank.TankDestructionColors[ai.AiTankType],
                    _ => part.Color,
                },

                TankHurtContextShell thcs => thcs.Shell.Owner switch {
                    PlayerTank pl => PlayerID.PlayerTankColors[pl.PlayerType].ToColor(),
                    AITank ai => AITank.TankDestructionColors[ai.AiTankType],
                    _ => part.Color,
                },
                _ => part.Color,
            };
            part.HasAddativeBlending = false;
            part.Origin2D = TankGame.TextFont.MeasureString(TankGame.GameLanguage.Hit) / 2;
            part.Scale = new Vector3(Vector2.One.ToResolution(), 1);
            part.Alpha = 0;

            // TODO: Fix layering bullshit.
            part.Layer = 1;

            var origPos = part.Position;
            var speed = 0.5f;
            float height = 5f;

            part.UniqueBehavior = (a) => {
                var sin = MathF.Sin(TankGame.RunTime * speed) * height;
                part.Position.Y = origPos.Y + sin * TankGame.DeltaTime;

                if (a.LifeTime > 90)
                    GeometryUtils.Add(ref a.Scale, -0.05f * TankGame.DeltaTime);

                height -= 0.02f;

                // ChatSystem.SendMessage(sin.ToString(), Color.White);

                if (a.Scale.X < 0)
                    a.Destroy();
            };
            part.UniqueDraw = particle => {
                SpriteFontUtils.DrawBorderedText(TankGame.SpriteRenderer, TankGame.TextFontLarge, particle.Text,
                    MatrixUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(particle.Position),
                        TankGame.GameView, TankGame.GameProjection),
                    particle.Color, Color.White, new(particle.Scale.X, particle.Scale.Y), 0f, 1f);
            };
        }

        doTextPopup();

        if (Properties.Armor is not null) {
            if (Properties.Armor.HitPoints > 0) {
                Properties.Armor.HitPoints--;
                var ding = SoundPlayer.PlaySoundInstance(
                    $"Assets/sounds/armor_ding_{GameHandler.GameRand.Next(1, 3)}.ogg", SoundContext.Effect,
                    gameplaySound: true);
                ding.Instance.Pitch = GameHandler.GameRand.NextFloat(-0.1f, 0.1f);
                OnDamage?.Invoke(this, Properties.Armor.HitPoints == 0, context);
                if (this is AITank ai) {
                    for (int i = 0; i < ModLoader.ModTanks.Length; i++)
                        if (ai.AiTankType == ModLoader.ModTanks[i].Type) {
                            ModLoader.ModTanks[i].TakeDamage(ai, Properties.Armor.HitPoints == 0, context);
                        }
                }
            }
            else {
                OnDamage?.Invoke(this, true, context);
                if (this is AITank ai) {
                    for (int i = 0; i < ModLoader.ModTanks.Length; i++)
                        if (ai.AiTankType == ModLoader.ModTanks[i].Type) {
                            ModLoader.ModTanks[i].TakeDamage(ai, true, context);
                        }
                }
                Destroy(context);
            }

            return;
        }

        OnDamage?.Invoke(this, true, context);
        Destroy(context);
    }

    /// <summary>Destroy this <see cref="Tank"/>.</summary>
    public virtual void Destroy(ITankHurtContext context) {
        GameProperties.OnMissionStart -= OnMissionStart;

        // i think this is right? | (Probably, a destroyed tank is a dead tank!)
        Dead = true;

        const string tankDestroySound0 = "Assets/sounds/tnk_destroy.ogg";
        SoundPlayer.PlaySoundInstance(tankDestroySound0, SoundContext.Effect, 0.2f, gameplaySound: true);

        switch (this) {
            case AITank t: {
                const string tankDestroySound1 = "Assets/sounds/tnk_destroy_enemy.ogg";
                SoundPlayer.PlaySoundInstance(tankDestroySound1, SoundContext.Effect, 0.3f, gameplaySound: true);

                var aiDeathMark = new TankDeathMark(TankDeathMark.CheckColor.White) {
                    Position = Position3D + new Vector3(0, 0.1f, 0),
                };

                aiDeathMark.StoredTank = new TankTemplate {
                    AiTier = t.AiTankType,
                    IsPlayer = false,
                    Position = aiDeathMark.Position.FlattenZ(),
                    Rotation = t.TankRotation,
                    Team = t.Team,
                };

                break;
            }
            case PlayerTank p: {
                var c = p.PlayerType switch {
                    PlayerID.Blue => TankDeathMark.CheckColor.Blue,
                    PlayerID.Red => TankDeathMark.CheckColor.Red,
                    PlayerID.GreenPlr => TankDeathMark.CheckColor.Green,
                    PlayerID.YellowPlr => TankDeathMark.CheckColor.Yellow, // TODO: change these colors.
                    _ => throw new Exception($"Player Death Mark for colour {p.PlayerType} is not supported."),
                };

                var playerDeathMark = new TankDeathMark(c) {
                    Position = Position3D + new Vector3(0, 0.1f, 0),
                };

                playerDeathMark.StoredTank = new TankTemplate {
                    IsPlayer = true,
                    Position = playerDeathMark.Position.FlattenZ(),
                    Rotation = p.TankRotation,
                    Team = p.Team,
                    PlayerType = p.PlayerType,
                };
                break;
            }
        }

        Properties.Armor?.Remove();
        Properties.Armor = null;

        void doDestructionFx() {
            for (int i = 0; i < 12; i++) {
                var tex = GameResources.GetGameResource<Texture2D>(GameHandler.GameRand.Next(0, 2) == 0
                    ? "Assets/textures/misc/tank_rock"
                    : "Assets/textures/misc/tank_rock_2");

                var particle = GameHandler.Particles.MakeParticle(Position3D, tex);

                particle.HasAddativeBlending = false;

                var vel = new Vector3(GameHandler.GameRand.NextFloat(-3, 3), GameHandler.GameRand.NextFloat(3, 6),
                    GameHandler.GameRand.NextFloat(-3, 3));

                particle.Roll = -TankGame.DEFAULT_ORTHOGRAPHIC_ANGLE;

                particle.Scale = new(0.55f);

                particle.Color = Properties.DestructionColor;

                particle.UniqueBehavior = particle => { // Hide local var from outer scope with same name.
                    particle.Pitch += MathF.Sin(particle.Position.Length() / 10) * TankGame.DeltaTime;
                    vel.Y -= 0.2f;
                    particle.Position += vel * TankGame.DeltaTime;
                    particle.Alpha -= 0.025f * TankGame.DeltaTime;

                    if (particle.Alpha <= 0f)
                        particle.Destroy();
                };
            }

            var explosionParticle = GameHandler.Particles.MakeParticle(Position3D,
                GameResources.GetGameResource<Texture2D>("Assets/textures/misc/bot_hit"));

            explosionParticle.Color = Color.Yellow * 0.75f;

            explosionParticle.ToScreenSpace = true;

            explosionParticle.TextureScale = new(5f);

            explosionParticle.IsIn2DSpace = true;

            explosionParticle.UniqueBehavior = (p) => {
                GeometryUtils.Add(ref p.Scale, -0.3f * TankGame.DeltaTime);
                p.Alpha -= 0.06f * TankGame.DeltaTime;
                if (p.Scale.X <= 0f)
                    p.Destroy();
            };
            GameHandler.Particles.MakeSmallExplosion(Position3D, 15, 20, 1.3f, 30);
        }

        doDestructionFx();

        Remove(false);
    }

    /// <summary>Lay a <see cref="TankFootprint"/> under this <see cref="Tank"/>.</summary>
    public virtual void LayFootprint(bool alt) {
        if (!Properties.CanLayTread)
            return;
        new TankFootprint(this, -TankRotation, alt) {
            Position = Position3D + new Vector3(0, 0.1f, 0),
        };
    }

    public float DecelerationRateDecayTime;

    /// <summary>Shoot a <see cref="Shell"/> from this <see cref="Tank"/>.</summary>
    public virtual void Shoot(bool fxOnly) {
        if ((!MainMenu.Active && !GameProperties.InMission) || !Properties.HasTurret)
            return;

        if (CurShootCooldown > 0 || OwnedShellCount >= Properties.ShellLimit / Properties.ShellShootCount)
            return;

        bool flip = false;
        float angle = 0f;

        for (int i = 0; i < Properties.ShellShootCount; i++) {
            if (i == 0) {
                var new2d = Vector2.UnitY.RotatedByRadians(TurretRotation);

                var newPos = Position + new Vector2(0, 20).RotatedByRadians(-TurretRotation);

                var defPos = new Vector2(newPos.X, newPos.Y);

                if (!fxOnly) {
                    var shell = new Shell(defPos, new Vector2(-new2d.X, new2d.Y) * Properties.ShellSpeed,
                        Properties.ShellType, this, Properties.RicochetCount, homing: Properties.ShellHoming);

                    OnShoot?.Invoke(this, ref shell);

                    if (this is AITank ai) {
                        for (int j = 0; j < ModLoader.ModTanks.Length; j++)
                            if (ai.AiTankType == ModLoader.ModTanks[j].Type)
                                ModLoader.ModTanks[j].Shoot(ai, ref shell);
                    }

                    if (this is PlayerTank pt) {
                        if (NetPlay.IsClientMatched(pt.PlayerId))
                            Client.SyncShellFire(shell);
                    }
                    else
                        Client.SyncShellFire(shell);
                }

                var force = (Position - defPos) * Properties.Recoil;
                Velocity = force / UNITS_PER_METER;
                DecelerationRateDecayTime = 20 * Properties.Recoil;
                //Body.ApplyForce(force / UNITS_PER_METER);
                DoShootParticles(new Vector3(defPos.X, 11, defPos.Y));
            }
            else {
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
                var new2d = Vector2.UnitY.RotatedByRadians(TurretRotation);

                var newPos = Position + new Vector2(0, 20).RotatedByRadians(-TurretRotation + newAngle);

                shell.Position = new Vector2(newPos.X, newPos.Y);

                shell.Velocity = new Vector2(-new2d.X, new2d.Y).RotatedByRadians(newAngle) *
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

    public void DoShootParticles(Vector3 position) {
        var hit = GameHandler.Particles.MakeParticle(position,
            GameResources.GetGameResource<Texture2D>("Assets/textures/misc/bot_hit"));
        var smoke = GameHandler.Particles.MakeParticle(position,
            GameResources.GetGameResource<Texture2D>("Assets/textures/misc/tank_smokes"));

        hit.Roll = -TankGame.DEFAULT_ORTHOGRAPHIC_ANGLE;
        smoke.Roll = -TankGame.DEFAULT_ORTHOGRAPHIC_ANGLE;

        smoke.Scale = new(0.35f);
        hit.Scale = new(0.5f);

        smoke.Color = new(84, 22, 0, 255);

        smoke.HasAddativeBlending = false;

        int achieveable = 80;
        float step = 1;

        hit.UniqueBehavior = (part) => {
            part.Color = Color.Orange;

            if (part.LifeTime > 1)
                part.Alpha -= 0.1f * TankGame.DeltaTime;
            if (part.Alpha <= 0)
                part.Destroy();
        };
        smoke.UniqueBehavior = (part) => {
            part.Color.R = (byte)MathUtils.RoughStep(part.Color.R, achieveable, step);
            part.Color.G = (byte)MathUtils.RoughStep(part.Color.G, achieveable, step);
            part.Color.B = (byte)MathUtils.RoughStep(part.Color.B, achieveable, step);

            GeometryUtils.Add(ref part.Scale, 0.004f * TankGame.DeltaTime);

            if (part.Color.G == achieveable) {
                part.Color.B = (byte)achieveable;
                part.Alpha -= 0.04f * TankGame.DeltaTime;

                if (part.Alpha <= 0)
                    part.Destroy();
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

        OnLayMine?.Invoke(this, ref mine);
        if (this is AITank ai) {
            for (int i = 0; i < ModLoader.ModTanks.Length; i++)
                if (ai.AiTankType == ModLoader.ModTanks[i].Type)
                    ModLoader.ModTanks[i].LayMine(ai, ref mine);
        }
    }

    public virtual void Render() {
        if (!MapRenderer.ShouldRenderAll)
            return;
        /*foreach (var shell in OwnedShells) {
            SpriteFontUtils.DrawBorderedText(TankGame.SpriteRenderer, TankGame.TextFont, $"Owned by: {(this is PlayerTank ? PlayerID.Collection.GetKey(((PlayerTank)shell.Owner).PlayerType) : TankID.Collection.GetKey(((AITank)shell.Owner).Tier))}",
                MatrixUtils.ConvertWorldToScreen(Vector3.Zero, shell.World, shell.View, shell.Projection) - new Vector2(0, 20), this is PlayerTank ? PlayerID.PlayerTankColors[((PlayerTank)shell.Owner).PlayerType].ToColor() : AITank.TankDestructionColors[((AITank)shell.Owner).Tier], Color.White, Vector2.One.ToResolution(), 0f, 2f);
        }*/

        //if (Dead)
        //return;

        Projection = TankGame.GameProjection;
        View = TankGame.GameView;
        if (!Dead) {
            foreach (var cosmetic in Cosmetics) {
                //if (GameProperties.InMission && Properties.Invisible)
                //break;
                if (cosmetic is not Cosmetic3D cos3d)
                    continue;

                for (int i = 0; i < (Lighting.AccurateShadows ? 2 : 1); i++) {
                    foreach (var mesh in cos3d.Model.Meshes) {
                        if (cos3d.IgnoreMeshesByName.Any(meshName => meshName == mesh.Name))
                            continue;

                        foreach (BasicEffect effect in mesh.Effects) {
                            var rotY = cosmetic.LockOptions switch {
                                CosmeticLockOptions.ToTurret => cosmetic.Rotation.Y + TurretRotation,
                                CosmeticLockOptions.ToTank => cosmetic.Rotation.Y + TankRotation,
                                _ => cosmetic.Rotation.Y,
                            };
                            
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
            //$"Team: {TeamID.Collection.GetKey(Team)}",
            //$"Shell Owned / Max: {OwnedShellCount} / {Properties.ShellLimit}",
            //$"Mine Owned / Max: {OwnedMineCount} / {Properties.MineLimit}",
            //$"Physics.LinearVelocity / Velocity: {Body.LinearVelocity} / {Velocity}",
            $"Tank Rotation/Target: {TankRotation}/{TargetTankRotation}",
            $"WorldID: {WorldId}",
            this is AITank ai
                ? $"Turret Rotation/Target: {TurretRotation}/{ai.TargetTurretRotation}"
                : $"Turret Rotation: {TurretRotation}"
        };

        // TankGame.spriteBatch.Draw(GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel"), CollisionBox2D, Color.White * 0.75f);

        for (int i = 0; i < info.Length; i++)
            DebugManager.DrawDebugString(TankGame.SpriteRenderer, info[i],
                MatrixUtils.ConvertWorldToScreen(Vector3.Up * 20, World, View, Projection) -
                new Vector2(0, ((i + 1) * 20).ToResolutionY() + 8), 1, centered: true, color: Color.Aqua);
    }

    /// <summary>The current speed of this tank.</summary>
    public float Speed { get; set; }

    public float CurShootStun { get; private set; } = 0;
    public float CurShootCooldown { get; private set; } = 0;
    public float CurMineCooldown { get; private set; } = 0;
    public float CurMineStun { get; private set; } = 0;
    public uint timeSinceLastAction = 15000;

    public virtual void Remove(bool nullifyMe) {
        if (CollisionsWorld.BodyList.Contains(Body))
            CollisionsWorld.Remove(Body);
        foreach (var particle in GameHandler.Particles.CurrentParticles)
            if (particle is not null)
                if ((string)particle.Tag == $"cosmetic_2d_{GetHashCode()}") // remove all particles related to this tank
                    particle.Destroy();
        GameProperties.OnMissionStart -= OnMissionStart;

        if (nullifyMe)
            OwnedShells = null;
    }
}