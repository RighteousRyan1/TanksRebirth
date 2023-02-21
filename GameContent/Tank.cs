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
using TanksRebirth.GameContent.Cosmetics;
using TanksRebirth.Graphics;
using TanksRebirth.GameContent.UI;
using TanksRebirth.GameContent.Properties;
using TanksRebirth.GameContent.Systems.Coordinates;
using TanksRebirth.GameContent.ID;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent;

public struct TankTemplate
{
    /// <summary>If false, the template will contain data for an AI tank.</summary>
    public bool IsPlayer;

    public int AiTier;
    public int PlayerType;

    public Vector2 Position;

    public float Rotation;

    public int Team;

    public Range<int> RandomizeRange;

    public Tank GetTank() => IsPlayer ? GetPlayerTank() : GetAiTank();
    public AITank GetAiTank()
    {
        if (IsPlayer)
            throw new Exception($"{nameof(IsPlayer)} was true. This method cannot execute.");

        var ai = new AITank(AiTier);
        ai.Body.Position = Position / Tank.UNITS_PER_METER;
        ai.Position = Position;
        ai.TankRotation = Rotation;
        ai.TargetTankRotation = Rotation;
        ai.Dead = false;
        ai.TurretRotation = Rotation;
        ai.Team = Team;

        var placement = PlacementSquare.Placements.FindIndex(place => place.Position == ai.Position3D);
        if (placement > -1)
        {
            PlacementSquare.Placements[placement].TankId = ai.WorldId;
            PlacementSquare.Placements[placement].HasBlock = false;
        }

        return ai;
    }
    public PlayerTank GetPlayerTank()
    {
        if (!IsPlayer)
            throw new Exception($"{nameof(IsPlayer)} was false. This method cannot execute.");

        var player = Difficulties.Types["RandomPlayer"] ? new PlayerTank(PlayerType, false, AITank.PickRandomTier()) : new PlayerTank(PlayerType);
        player.Body.Position = Position / Tank.UNITS_PER_METER;
        player.Position = Position;
        player.TankRotation = Rotation;
        player.Dead = false;
        player.Team = Team;

        var placement = PlacementSquare.Placements.FindIndex(place => place.Position == player.Position3D);
        if (placement > -1)
        {
            PlacementSquare.Placements[placement].TankId = player.WorldId;
            PlacementSquare.Placements[placement].HasBlock = false;
        }
        return player;
    }
}
public interface ITankHurtContext
{
    bool IsPlayer { get; set; }

    // PlayerType PlayerType { get; set; } // don't use if IsPlayer is false.
}
public struct TankHurtContext_Other : ITankHurtContext
{
    public enum HurtContext
    {
        FromIngame,
        FromOther
    }
    public HurtContext Context { get; set; }
    public bool IsPlayer { get; set; }
    public int TankId { get; set; }

    public string Reason { get; }

    public TankHurtContext_Other(HurtContext cxt)
    {
        Reason = string.Empty;
        Context = cxt;
        IsPlayer = false;
        TankId = -1;
    }
    public TankHurtContext_Other(string reason)
    {
        Reason = reason;
        Context = HurtContext.FromOther;
        IsPlayer = false;
        TankId = -1;
    }
}
public struct TankHurtContext_Shell : ITankHurtContext
{
    public bool IsPlayer { get; set; }
    public uint Bounces { get; set; }

    public int ShellType { get; set; }

    public Shell Shell { get; set; }

    public TankHurtContext_Shell(bool isPlayer, uint bounces, int type, Shell shell)
    {
        IsPlayer = isPlayer;
        Bounces = bounces;
        ShellType = type;
        Shell = shell;
    }
}
public struct TankHurtContext_Mine : ITankHurtContext
{
    public bool IsPlayer { get; set; }
    public Explosion MineExplosion { get; set; }
    public TankHurtContext_Mine(bool isPlayer, Explosion mineExplosion)
    {
        IsPlayer = isPlayer;
        MineExplosion = mineExplosion;
    }
}
public abstract class Tank {
    #region TexPack
    public static Dictionary<string, Texture2D> Assets = new();

    public static string AssetRoot;

    public static void SetAssetNames() {
        Assets.Clear();
        // TankTier.Collection.GetKey(tankToSpawnType)
        foreach (var tier in TankID.Collection.Keys.Where(tier => TankID.Collection.GetValue(tier) > TankID.Random && TankID.Collection.GetValue(tier) < TankID.Explosive))
            Assets.Add($"tank_" + tier.ToLower(), null);
        foreach (var type in PlayerID.Collection.Keys)
            Assets.Add($"tank_" + type.ToLower(), null);
    }
    public static void LoadVanillaTextures() {
        Assets.Clear();

        foreach (var tier in TankID.Collection.Keys.Where(tier => TankID.Collection.GetValue(tier) > TankID.Random && TankID.Collection.GetValue(tier) < TankID.Explosive))
            if (!Assets.ContainsKey($"tank_" + tier.ToLower()))
                Assets.Add($"tank_" + tier.ToLower(), GameResources.GetGameResource<Texture2D>($"Assets/textures/tank/tank_{tier.ToLower()}"));
        foreach (var type in PlayerID.Collection.Keys)
            if (!Assets.ContainsKey($"tank_" + type.ToLower()))
                Assets.Add($"tank_" + type.ToLower(), GameResources.GetGameResource<Texture2D>($"Assets/textures/tank/tank_{type.ToLower()}"));
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
            GameHandler.ClientLog.Write($"Error: Directory '{path}' not found when attempting texture pack load.", LogType.Warn);
            return;
        }

        AssetRoot = path;

        foreach (var file in Directory.GetFiles(path)) {
            if (Assets.Any(type => type.Key == Path.GetFileNameWithoutExtension(file))) {
                Assets[Path.GetFileNameWithoutExtension(file)] = Texture2D.FromFile(TankGame.Instance.GraphicsDevice, Path.Combine(path, Path.GetFileName(file)));
                GameHandler.ClientLog.Write($"Texture pack '{folder}' overrided texture '{Path.GetFileNameWithoutExtension(file)}'", LogType.Info);
            }
        }
    }
    #endregion

    #region Events
    public delegate void DamageDelegate(Tank victim, bool destroy, ITankHurtContext context);
    public static event DamageDelegate OnDamage;
    public delegate void ApplyDefaultsDelegate(Tank tank, ref TankProperties properties);
    public static event ApplyDefaultsDelegate PostApplyDefaults;
    public delegate void ShootDelegate(Tank tank, ref Shell shell);
    /// <summary>Does not run for spread-fire.</summary>
    public static event ShootDelegate OnShoot;
    public delegate void LayMineDelegate(Tank tank, ref Mine mine);
    public static event LayMineDelegate OnLayMine;

    public delegate void PreUpdateDelegate(Tank tank);
    public static event PreUpdateDelegate OnPreUpdate;
    public delegate void PostUpdateDelegate(Tank tank);
    public static event PostUpdateDelegate OnPostUpdate;

    public delegate void InstancedDestroyDelegate();
    public event InstancedDestroyDelegate OnDestroy;
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
    public Shell[] OwnedShells = new Shell[0];

    public Vector2 Position;
    public Vector2 Velocity;

    /// <summary>This <see cref="Tank"/>'s <see cref="TeamID"/>.</summary>
    public int Team { get; set; }
    /// <summary>The rotation of this <see cref="Tank"/>'s barrel. Generally should not be modified in a player context.</summary>>
    public BoundingBox Worldbox { get; set; }
    /// <summary>The 2D circle-represented hitbox of this <see cref="Tank"/>.</summary>
    public Circle CollisionCircle => new() { Center = Position, Radius = TNK_WIDTH / 2 };
    /// <summary>The 2D rectangle-represented hitbox of this <see cref="Tank"/>.</summary>
    public Rectangle CollisionBox => new((int)(Position.X - TNK_WIDTH / 2 + 3), (int)(Position.Y - TNK_WIDTH / 2 + 2), (int)TNK_WIDTH - 8, (int)TNK_HEIGHT - 4);
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
        return teams.ToArray();
    }

    /// <summary>This <see cref="Tank"/>'s swag apparel as a <see cref="List{T}"/> of <see cref="ICosmetic"/>s.</summary>
    public List<ICosmetic> Cosmetics = new();
    /// <summary>Apply all the default parameters for this <see cref="Tank"/>.</summary>
    public virtual void ApplyDefaults(ref TankProperties properties) {
        PostApplyDefaults?.Invoke(this, ref properties);
        Properties = properties;
    }
    public virtual void Initialize() {
        OwnedShells = new Shell[Properties.ShellLimit];

        _cannonMesh = Model.Meshes["Cannon"];
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

        Body = CollisionsWorld.CreateCircle(TNK_WIDTH * 0.4f / UNITS_PER_METER, 1f, Position / UNITS_PER_METER, BodyType.Dynamic);

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
            var particle = GameHandler.ParticleSystem.MakeParticle(Position3D + cos2d.RelativePosition, cos2d.Texture);

            particle.Scale = cos2d.Scale;
            particle.Tag = $"cosmetic_2d_{GetHashCode()}"; // store the hash code of this tank, so when we destroy the cosmetic's particle, it destroys all belonging to this tank!
            particle.HasAddativeBlending = false;
            particle.UniqueBehavior = (z) => {
                particle.Position = Position3D + cos2d.RelativePosition;
                particle.Roll = cos2d.Rotation.X;
                particle.Pitch = cos2d.Rotation.Y;
                particle.Yaw = cos2d.Rotation.Z;
                particle.Scale = (Properties.Invisible && GameProperties.InMission) ? Vector3.Zero : cos2d.Scale;

                if (destroyOn != null)
                    if (destroyOn.Invoke())
                        particle.Destroy();
            };
        }
    }
    void OnMissionStart() {
        if (Difficulties.Types["FFA"])
            Team = TeamID.NoTeam;
        if (Properties.Invisible && !Dead) {
            var invis = "Assets/sounds/tnk_invisible.ogg";
            SoundPlayer.PlaySoundInstance(invis, SoundContext.Effect, 0.3f, gameplaySound: true);

            var lightParticle = GameHandler.ParticleSystem.MakeParticle(Position3D, GameResources.GetGameResource<Texture2D>("Assets/textures/misc/light_particle"));

            lightParticle.Scale = new(0.25f);
            lightParticle.Alpha = 0f;
            lightParticle.IsIn2DSpace = true;

            lightParticle.UniqueBehavior = (lp) => {
                lp.Position = Position3D;
                if (lp.Scale.X < 5f)
                    GeometryUtils.Add(ref lp.Scale, 0.12f * TankGame.DeltaTime);
                if (lp.Alpha < 1f && lp.Scale.X < 5f)
                    lp.Alpha += 0.02f * TankGame.DeltaTime;

                if (lp.LifeTime > 90)
                    lp.Alpha -= 0.005f * TankGame.DeltaTime;

                if (lp.Scale.X < 0f)
                    lp.Destroy();
            };

            const int NUM_LOCATIONS = 8;

            for (int i = 0; i < NUM_LOCATIONS; i++) {
                var lp = GameHandler.ParticleSystem.MakeParticle(Position3D + new Vector3(0, 5, 0), GameResources.GetGameResource<Texture2D>("Assets/textures/misc/tank_smokes"));

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
    }

    public bool Flip;

    private Vector2 _oldPosition;
    /// <summary>Update this <see cref="Tank"/>.</summary>
    public virtual void Update() {
        if (Dead || !MapRenderer.ShouldRender)
            return;

        OnPreUpdate?.Invoke(this);

        Position = Body.Position * UNITS_PER_METER;

        Body.LinearVelocity = Velocity * 0.55f / UNITS_PER_METER * TankGame.DeltaTime;

        // try to make positive. i hate game
        World = Matrix.CreateScale(Scaling) * Matrix.CreateFromYawPitchRoll(-TankRotation - (Flip ? MathHelper.Pi : 0f), 0, 0)
            * Matrix.CreateTranslation(Position3D);

        Worldbox = new(Position3D - new Vector3(7, 0, 7), Position3D + new Vector3(10, 15, 10));

        if (!MainMenu.Active && (!GameProperties.InMission || IntermissionSystem.IsAwaitingNewMission))
            Velocity = Vector2.Zero;
        if (OwnedMineCount < 0)
            OwnedMineCount = 0;
        if (DecelerationRateDecayTime > 0)
            DecelerationRateDecayTime -= TankGame.DeltaTime;

        if (!Properties.Stationary && Velocity.Length() > 0) {
            var rnd = MathF.Round(12 / Velocity.Length());
            float treadPlaceTimer = rnd != 0 ? rnd : 1;
            // MAYBE: change back to <= delta time if it doesn't work.
            if (TankGame.RunTime % treadPlaceTimer < TankGame.DeltaTime)
                LayFootprint(Properties.TrackType == TrackID.Thick);
            if (!Properties.IsSilent) {
                // why did i clamp? i hate old code
                if (TankGame.RunTime % MathHelper.Clamp(treadPlaceTimer / 2, 4, 6) < TankGame.DeltaTime) {
                    var treadPlace = $"Assets/sounds/tnk_tread_place_{GameHandler.GameRand.Next(1, 5)}.ogg";
                    var sfx = SoundPlayer.PlaySoundInstance(treadPlace, SoundContext.Effect, Properties.TreadVolume, 0f, Properties.TreadPitch, gameplaySound: true);
                    sfx.Instance.Pitch = Properties.TreadPitch;
                }
            }
        }

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

        IsTurning = !(TankRotation > TargetTankRotation - Properties.MaximalTurn - MathHelper.ToRadians(5) && TankRotation < TargetTankRotation + Properties.MaximalTurn + MathHelper.ToRadians(5));

        if (!IsTurning) {
            IsTurning = false;
            Speed += Properties.Acceleration * TankGame.DeltaTime;
            if (Speed > Properties.MaxSpeed)
                Speed = Properties.MaxSpeed;
        }
        if (IsTurning || CurShootStun > 0 || CurMineStun > 0 || Properties.Stationary) {
            Speed -= Properties.Deceleration * (DecelerationRateDecayTime > 0 ? 0.25f : 1f) * TankGame.DeltaTime;
            if (Speed < 0)
                Speed = 0;
            IsTurning = true;
        }
        // try to make negative. go poopoo
        _cannonMesh.ParentBone.Transform = Matrix.CreateRotationY(TurretRotation + TankRotation + (Flip ? MathHelper.Pi : 0));
        Model.Root.Transform = World;

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

        _oldPosition = Position;

        OnPostUpdate?.Invoke(this);
    }

    /// <summary>Damage this <see cref="Tank"/>. If it has no armor, <see cref="Destroy"/> it.</summary>
    public virtual void Damage(ITankHurtContext context) {
        if (Dead || Properties.Immortal)
            return;

        // if (Server.serverNetManager != null)
        Client.SyncDamage(WorldId);

        void doTextPopup()
        {
            var part = GameHandler.ParticleSystem.MakeParticle(Position3D + new Vector3(0, 15, 0), TankGame.GameLanguage.Hit);

            part.IsIn2DSpace = true;
            part.ToScreenSpace = true;

            if (context is TankHurtContext_Mine thcm)
            {
                if (thcm.MineExplosion.Source is PlayerTank pl)
                {
                    part.Color = PlayerID.PlayerTankColors[pl.PlayerType].ToColor();
                }
                if (thcm.MineExplosion.Source is AITank ai)
                {
                    part.Color = AITank.TankDestructionColors[ai.AiTankType];
                }
            }
            else if (context is TankHurtContext_Shell thcs)
            {
                if (thcs.Shell.Owner is PlayerTank pl)
                {
                    part.Color = PlayerID.PlayerTankColors[pl.PlayerType].ToColor();
                }
                if (thcs.Shell.Owner is AITank ai)
                {
                    part.Color = AITank.TankDestructionColors[ai.AiTankType];
                }
            }
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
            part.UniqueDraw = (a) =>
            {
                SpriteFontUtils.DrawBorderedText(TankGame.SpriteRenderer, TankGame.TextFontLarge, part.Text,
                MatrixUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(part.Position), TankGame.GameView, TankGame.GameProjection),
                part.Color, Color.White, new(part.Scale.X, part.Scale.Y), 0f, 1f);
            };
        }

        doTextPopup();

        if (Properties.Armor is not null) {
            OnDamage?.Invoke(this, Properties.Armor.HitPoints > 0, context);
            if (Properties.Armor.HitPoints > 0) {
                Properties.Armor.HitPoints--;
                var ding = SoundPlayer.PlaySoundInstance($"Assets/sounds/armor_ding_{GameHandler.GameRand.Next(1, 3)}.ogg", SoundContext.Effect, gameplaySound: true);
                ding.Instance.Pitch = GameHandler.GameRand.NextFloat(-0.1f, 0.1f);
            }
            else {
                OnDamage?.Invoke(this, true, context);
                Destroy(context);
            }
        }
        else {
            OnDamage?.Invoke(this, true, context);
            Destroy(context);
        }
    }
    /// <summary>Destroy this <see cref="Tank"/>.</summary>
    public virtual void Destroy(ITankHurtContext context) {

        GameProperties.OnMissionStart -= OnMissionStart;

        // i think this is right?
        Dead = true;

        OnDestroy?.Invoke();

        var killSound1 = "Assets/sounds/tnk_destroy.ogg";
        SoundPlayer.PlaySoundInstance(killSound1, SoundContext.Effect, 0.2f, gameplaySound: true);
        if (this is AITank t)
        {
            var killSound2 = "Assets/sounds/tnk_destroy_enemy.ogg";
            SoundPlayer.PlaySoundInstance(killSound2, SoundContext.Effect, 0.3f, gameplaySound: true);

            var dm = new TankDeathMark(TankDeathMark.CheckColor.White)
            {
                Position = Position3D + new Vector3(0, 0.1f, 0),
            };

            dm.StoredTank = new()
            {
                AiTier = t.AiTankType,
                IsPlayer = false,
                Position = dm.Position.FlattenZ(),
                Rotation = t.TankRotation,
                Team = t.Team,
            };
        }
        else if (this is PlayerTank p)
        {
            var c = p.PlayerType switch
            {
                PlayerID.Blue => TankDeathMark.CheckColor.Blue,
                PlayerID.Red => TankDeathMark.CheckColor.Red,
                PlayerID.GreenPlr => TankDeathMark.CheckColor.Green,
                PlayerID.YellowPlr => TankDeathMark.CheckColor.Yellow, // TODO: change these colors.
                _ => throw new Exception()
            };

            var dm = new TankDeathMark(c)
            {
                Position = Position3D + new Vector3(0, 0.1f, 0)
            };

            dm.StoredTank = new()
            {
                IsPlayer = true,
                Position = dm.Position.FlattenZ(),
                Rotation = p.TankRotation,
                Team = p.Team,
                PlayerType = p.PlayerType
            };
        }

        Properties.Armor?.Remove();
        Properties.Armor = null;
        void doDestructionFx()
        {
            for (int i = 0; i < 12; i++)
            {
                var tex = GameResources.GetGameResource<Texture2D>(GameHandler.GameRand.Next(0, 2) == 0 ? "Assets/textures/misc/tank_rock" : "Assets/textures/misc/tank_rock_2");

                var part = GameHandler.ParticleSystem.MakeParticle(Position3D, tex);

                part.HasAddativeBlending = false;

                var vel = new Vector3(GameHandler.GameRand.NextFloat(-3, 3), GameHandler.GameRand.NextFloat(3, 6), GameHandler.GameRand.NextFloat(-3, 3));

                part.Roll = -TankGame.DEFAULT_ORTHOGRAPHIC_ANGLE;

                part.Scale = new(0.55f);

                part.Color = Properties.DestructionColor;

                part.UniqueBehavior = (p) =>
                {
                    part.Pitch += MathF.Sin(part.Position.Length() / 10) * TankGame.DeltaTime;
                    vel.Y -= 0.2f;
                    part.Position += vel * TankGame.DeltaTime;
                    part.Alpha -= 0.025f * TankGame.DeltaTime;

                    if (part.Alpha <= 0f)
                        part.Destroy();
                };
            }

            var partExpl = GameHandler.ParticleSystem.MakeParticle(Position3D, GameResources.GetGameResource<Texture2D>("Assets/textures/misc/bot_hit"));

            partExpl.Color = Color.Yellow * 0.75f;

            partExpl.ToScreenSpace = true;

            partExpl.TextureScale = new(5f);

            partExpl.IsIn2DSpace = true;

            partExpl.UniqueBehavior = (p) =>
            {
                GeometryUtils.Add(ref p.Scale, -0.3f * TankGame.DeltaTime);
                p.Alpha -= 0.06f * TankGame.DeltaTime;
                if (p.Scale.X <= 0f)
                    p.Destroy();
            };
            GameHandler.ParticleSystem.MakeSmallExplosion(Position3D, 15, 20, 1.3f, 30);
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

        for (int i = 0; i < Properties.ShellShootCount; i++)
        {
            if (i == 0)
            {
                var new2d = Vector2.UnitY.RotatedByRadians(TurretRotation);

                var newPos = Position + new Vector2(0, 20).RotatedByRadians(-TurretRotation);

                var defPos = new Vector3(newPos.X, 11, newPos.Y);

                if (!fxOnly)
                {
                    var shell = new Shell(defPos, new Vector3(-new2d.X, 0, new2d.Y) * Properties.ShellSpeed, Properties.ShellType, this, Properties.RicochetCount, homing: Properties.ShellHoming);

                    OnShoot?.Invoke(this, ref shell);

                    if (this is PlayerTank pt)
                    {
                        if (NetPlay.IsClientMatched(pt.PlayerId))
                            Client.SyncShellFire(shell);
                    }
                    else
                        Client.SyncShellFire(shell);
                }
                var force = (Position - defPos.FlattenZ()) * Properties.Recoil;
                Velocity = force / UNITS_PER_METER;
                DecelerationRateDecayTime = 20 * Properties.Recoil;
                //Body.ApplyForce(force / UNITS_PER_METER);
                DoShootParticles(defPos);
            }
            else
            {
                // i == 0 : null, 0 rads
                // i == 1 : flipped, -0.15 rads
                // i == 2 : !flipped, 0.15 rads
                // i == 3 : flipped, -0.30 rads
                // i == 4 : !flipped, 0.30 rads
                flip = !flip;
                if ((i - 1) % 2 == 0)
                    angle += Properties.ShellSpread;
                var newAngle = flip ? -angle : angle;

                var shell = new Shell(Position3D, Vector3.Zero, Properties.ShellType, this, homing: Properties.ShellHoming);
                var new2d = Vector2.UnitY.RotatedByRadians(TurretRotation);

                var newPos = Position + new Vector2(0, 20).RotatedByRadians(-TurretRotation + newAngle);

                shell.Position = new Vector3(newPos.X, 11, newPos.Y);

                shell.Velocity = new Vector3(-new2d.X, 0, new2d.Y).FlattenZ().RotatedByRadians(newAngle).ExpandZ() * Properties.ShellSpeed;

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

    public void DoShootParticles(Vector3 position)
    {
        var hit = GameHandler.ParticleSystem.MakeParticle(position, GameResources.GetGameResource<Texture2D>("Assets/textures/misc/bot_hit"));
        var smoke = GameHandler.ParticleSystem.MakeParticle(position, GameResources.GetGameResource<Texture2D>("Assets/textures/misc/tank_smokes"));

        hit.Roll = -TankGame.DEFAULT_ORTHOGRAPHIC_ANGLE;
        smoke.Roll = -TankGame.DEFAULT_ORTHOGRAPHIC_ANGLE;

        smoke.Scale = new(0.35f);
        hit.Scale = new(0.5f);

        smoke.Color = new(84, 22, 0, 255);

        smoke.HasAddativeBlending = false;

        int achieveable = 80;
        float step = 1;

        hit.UniqueBehavior = (part) =>
        {
            part.Color = Color.Orange;

            if (part.LifeTime > 1)
                part.Alpha -= 0.1f * TankGame.DeltaTime;
            if (part.Alpha <= 0)
                part.Destroy();
        };
        smoke.UniqueBehavior = (part) =>
        {
            part.Color.R = (byte)MathUtils.RoughStep(part.Color.R, achieveable, step);
            part.Color.G = (byte)MathUtils.RoughStep(part.Color.G, achieveable, step);
            part.Color.B = (byte)MathUtils.RoughStep(part.Color.B, achieveable, step);

            GeometryUtils.Add(ref part.Scale, 0.004f * TankGame.DeltaTime);

            if (part.Color.G == achieveable)
            {
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

        if (this is PlayerTank pt)
        {
            if (NetPlay.IsClientMatched(pt.PlayerId))
                Client.SyncMinePlace(mine.Position, mine.DetonateTime, WorldId);
        }
        else
            Client.SyncMinePlace(mine.Position, mine.DetonateTime, WorldId);

        OnLayMine?.Invoke(this, ref mine);
    }
    public virtual void Render() {
        if (!MapRenderer.ShouldRender)
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
                if (cosmetic is Cosmetic3D cos3d) {
                    for (int i = 0; i < (Lighting.AccurateShadows ? 2 : 1); i++) {
                        foreach (ModelMesh mesh in cos3d.Model.Meshes) {
                            if (!cos3d.IgnoreMeshesByName.Any(meshname => meshname == mesh.Name)) {
                                foreach (BasicEffect effect in mesh.Effects) {
                                    float rotY;
                                    if (cosmetic.LockOptions == CosmeticLockOptions.ToTurret)
                                        rotY = cosmetic.Rotation.Y + TurretRotation;
                                    else if (cosmetic.LockOptions == CosmeticLockOptions.ToTank)
                                        rotY = cosmetic.Rotation.Y + TankRotation;
                                    else
                                        rotY = cosmetic.Rotation.Y;
                                    effect.World = i == 0 ? Matrix.CreateRotationX(cosmetic.Rotation.X) * Matrix.CreateRotationY(rotY) * Matrix.CreateRotationZ(cosmetic.Rotation.Z) * Matrix.CreateScale(cosmetic.Scale) * Matrix.CreateTranslation(Position3D + cosmetic.RelativePosition)
                                        : Matrix.CreateRotationX(cosmetic.Rotation.X) * Matrix.CreateRotationY(cosmetic.Rotation.Y) * Matrix.CreateRotationZ(cosmetic.Rotation.Z) * Matrix.CreateScale(cosmetic.Scale) * Matrix.CreateTranslation(Position3D + cosmetic.RelativePosition) * Matrix.CreateShadow(Lighting.AccurateLightingDirection, new(Vector3.UnitY, 0)) * Matrix.CreateTranslation(0, 0.2f, 0);
                                    effect.View = View;
                                    effect.Projection = Projection;

                                    effect.TextureEnabled = true;
                                    if (i == 0)
                                        effect.Texture = cos3d.ModelTexture;
                                    else
                                        effect.Texture = GameResources.GetGameResource<Texture2D>("Assets/textures/ingame/block_shadow_h");

                                    effect.SetDefaultGameLighting_IngameEntities();
                                }
                                mesh.Draw();
                            }
                        }
                    }
                }
            }
        }
        var info = new string[] {
            //$"Team: {TeamID.Collection.GetKey(Team)}",
            //$"Shell Owned / Max: {OwnedShellCount} / {Properties.ShellLimit}",
            //$"Mine Owned / Max: {OwnedMineCount} / {Properties.MineLimit}",
            //$"Physics.LinearVelocity / Velocity: {Body.LinearVelocity} / {Velocity}",
            $"Tank Rotation/Target: {TankRotation}/{TargetTankRotation}",
            $"WorldID: {WorldId}",
            this is AITank ai ? $"Turret Rotation/Target: {TurretRotation}/{ai.TargetTurretRotation}" : $"Turret Rotation: {TurretRotation}"
        };

        // TankGame.spriteBatch.Draw(GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel"), CollisionBox2D, Color.White * 0.75f);

        for (int i = 0; i < info.Length; i++)
            DebugUtils.DrawDebugString(TankGame.SpriteRenderer, info[i], MatrixUtils.ConvertWorldToScreen(Vector3.Up * 20, World, View, Projection) - new Vector2(0, ((i + 1) * 20).ToResolutionY() + 8), 1, centered: true, color: Color.Aqua);
    }
    /// <summary>The current speed of this tank.</summary>
    public float Speed { get; set; }
    public float CurShootStun { get; private set; } = 0;
    public float CurShootCooldown { get; private set; } = 0;
    public float CurMineCooldown { get; private set; } = 0;
    public float CurMineStun { get; private set; } = 0;
    public uint timeSinceLastAction = 15000;
    public virtual void Remove(bool nullifyMe) 
    {            
        if (CollisionsWorld.BodyList.Contains(Body))
            CollisionsWorld.Remove(Body);
        foreach (var particle in GameHandler.ParticleSystem.CurrentParticles)
            if (particle is not null)
                if ((string)particle.Tag == $"cosmetic_2d_{GetHashCode()}") // remove all particles related to this tank
                    particle.Destroy();
        GameProperties.OnMissionStart -= OnMissionStart;

        if (nullifyMe)
            OwnedShells = null;
    }
}
public class TankProperties
{
    /// <summary>Whether or not the tank has artillery-like function during gameplay.</summary>
    public bool Stationary { get; set; }
    /// <summary>Whether or not the tank should become invisible at mission start.</summary>
    public bool Invisible { get; set; }
    /// <summary>How fast the tank should accelerate towards its <see cref="MaxSpeed"/>.</summary>
    public float Acceleration { get; set; } = 0.6f;
    /// <summary>How fast the tank should decelerate when not moving.</summary>
    public float Deceleration { get; set; } = 0.3f;
    /// <summary>The maximum speed this tank can achieve.</summary>
    public float MaxSpeed { get; set; }
    /// <summary>How fast the bullets this <see cref="Tank"/> shoot are.</summary>
    public float ShellSpeed { get; set; }
    /// <summary>The volume of the footprint placement sounds.</summary>
    public float TreadVolume { get; set; }
    /// <summary>The pitch of the footprint placement sounds.</summary>
    public float TreadPitch { get; set; }
    /// <summary>The pitch of the shoot sound.</summary>
    public float ShootPitch { get; set; }
    /// <summary>The type of bullet this <see cref="Tank"/> shoots.</summary>
    public int ShellType { get; set; }
    /// <summary>The maximum amount of mines this <see cref="Tank"/> can place.</summary>
    public uint MineLimit { get; set; }
    /// <summary>How long this <see cref="Tank"/> will be immobile upon firing a bullet.</summary>
    public uint ShootStun { get; set; }
    /// <summary>How long this <see cref="Tank"/> will be immobile upon laying a mine.</summary>
    public uint MineStun { get; set; }
    /// <summary>How long this <see cref="Tank"/> has to wait until it can fire another bullet.</summary>
    public uint ShellCooldown { get; set; }
    /// <summary>How long until this <see cref="Tank"/> can lay another mine</summary>
    public uint MineCooldown { get; set; }
    /// <summary>How many times the <see cref="Shell"/> this <see cref="Tank"/> shoots can ricochet.</summary>
    public uint RicochetCount { get; set; }
    /// <summary>How many <see cref="Shell"/>s this <see cref="Tank"/> can own at any given time.</summary>
    public int ShellLimit { get; set; }
    /// <summary>How fast this <see cref="Tank"/> turns.</summary>
    public float TurningSpeed { get; set; }
    /// <summary>The maximum angle this <see cref="Tank"/> can turn (in radians) before it has to start pivoting.</summary>
    public float MaximalTurn { get; set; }
    /// <summary>Whether or not this <see cref="Tank"/> can lay a <see cref="TankFootprint"/>.</summary>
    public bool CanLayTread { get; set; } = true;
    /// <summary>Whether or not this <see cref="Tank"/> makes sounds while moving.</summary>
    public bool IsSilent { get; set; }
    /// <summary>The type of track that is laid.</summary>
    public int TrackType { get; set; }
    /// <summary>If <see cref="ShellShootCount"/> is greater than 1, this is how many radians each shot's offset will be when this <see cref="Tank"/> shoots.
    /// <para></para>
    /// A common formula to calculate values for when the bullets won't instantly collide is:
    /// <para></para>
    /// <c>(ShellShootCount / 12) - 0.05</c>
    /// <para></para>
    /// A table:
    /// <para></para>
    /// 3 = 0.3
    /// <para></para>
    /// 5 = 0.4
    /// <para></para>
    /// 7 = 0.65
    /// <para></para>
    /// 9 = 0.8
    /// </summary>
    public float ShellSpread { get; set; } = 0f;
    /// <summary>How many <see cref="Shell"/>s this <see cref="Tank"/> fires upon shooting in a spread.</summary>
    public int ShellShootCount { get; set; } = 1;

    /// <summary>The color of particle <see cref="Tank"/> emits upon destruction.</summary>
    public Color DestructionColor { get; set; } = Color.Black;
    /// <summary>The armor properties this <see cref="Tank"/> has.</summary>
    public Armor Armor { get; set; } = null;
    // Get it working before using this.
    /// <summary>How much this <see cref="Tank"/> is launched backward after firing a shell.</summary>
    public float Recoil { get; set; } = 0f;
    /// <summary>Whether or not this <see cref="Tank"/> has a turret to fire shells with.</summary>
    public bool HasTurret { get; set; } = true;
    /// <summary>Whether or not this <see cref="Tank"/> is able to be destroyed by <see cref="Mine"/>s.</summary>
    public bool VulnerableToMines { get; set; } = true;
    /// <summary>Whether or not this <see cref="Tank"/> is unable to be destroyed.</summary>
    public bool Immortal { get; set; } = false;
    /// <summary>The homing properties of the shells this <see cref="Tank"/> shoots.</summary>
    public Shell.HomingProperties ShellHoming = new();
}
public class TankFootprint
{
    public static bool ShouldTracksFade;

    public const int MAX_FOOTPRINTS = 100000;

    public static TankFootprint[] footprints = new TankFootprint[TankGame.Settings.TankFootprintLimit];

    public Vector3 Position;
    public float rotation;

    public Texture2D texture;

    internal static int total_treads_placed;

    public readonly bool alternate;

    public long lifeTime;

    public readonly Tank owner;

    public int id = 0;

    public static DecalSystem DecalHandler; // = new(TankGame.SpriteRenderer, TankGame.Instance.GraphicsDevice);

    public TankFootprint(Tank owner, float rotation, bool alt = false)
    {
        this.rotation = rotation;
        alternate = alt;
        this.owner = owner;
        if (total_treads_placed + 1 > MAX_FOOTPRINTS)
            footprints[Array.IndexOf(footprints, footprints.Min(x => x.lifeTime > 0))] = null; // i think?

        alternate = alt;
        id = total_treads_placed;
        Position = owner.Position3D;

        texture = GameResources.GetGameResource<Texture2D>(alt ? $"Assets/textures/tank_footprint_alt" : $"Assets/textures/tank_footprint");

        var track = GameHandler.ParticleSystem.MakeParticle(Position, texture);

        track.HasAddativeBlending = false;

        track.Roll = -MathHelper.PiOver2;
        track.Scale = new(0.5f, 0.55f, 0.5f);
        track.Alpha = 0.7f;
        track.Color = Color.White;
        track.UniqueBehavior = (a) =>
        {
            track.Position = Position;
            track.Pitch = rotation;
            if (ShouldTracksFade)
                track.Alpha -= 0.001f;
            if (track.Alpha <= 0 || _destroy)
            {
                track.Destroy();
                footprints[id] = null;
                total_treads_placed--;
            }

            track.Position = Position;
            track.Pitch = rotation;
        };

        footprints[total_treads_placed] = this;

        total_treads_placed++;

        /*DecalHandler.AddDecal(texture, MatrixUtils.ConvertWorldToScreen(Vector3.Zero,
            Matrix.CreateTranslation(Position), TankGame.GameView,
            TankGame.GameProjection), null, Color.White, rotation, BlendState.Opaque);*/

        // Render();
    }
    public void Update() => lifeTime++;

    private bool _destroy;
    public void Remove() => _destroy = true;
}
public class TankDeathMark
{
    public const int MAX_DEATH_MARKS = 1000;

    public static TankDeathMark[] deathMarks = new TankDeathMark[MAX_DEATH_MARKS];

    public Vector3 Position;
    public float rotation;

    internal static int total_death_marks;

    public Matrix World;
    public Matrix View;
    public Matrix Projection;

    public Particle check;

    public Texture2D texture;

    public TankTemplate StoredTank;

    public enum CheckColor
    {
        Blue,
        Red,
        Green,
        Yellow,
        White
    }
    /// <summary>Resurrects <see cref="StoredTank"/>.</summary>
    public void ResurrectTank()
    {
        StoredTank.GetTank();
    }

    public TankDeathMark(CheckColor color)
    {
        if (total_death_marks + 1 > MAX_DEATH_MARKS)
            return;
        total_death_marks++;

        texture = GameResources.GetGameResource<Texture2D>($"Assets/textures/check/check_{color.ToString().ToLower()}");

        check = GameHandler.ParticleSystem.MakeParticle(Position + new Vector3(0, 0.1f, 0), texture);
        check.HasAddativeBlending = false;
        check.Roll = -MathHelper.PiOver2;
        check.Layer = 0;

        deathMarks[total_death_marks] = this;
    }

    public void Render()
    {
        check.Position = Position;
        check.Scale = new(0.6f);
    }
}