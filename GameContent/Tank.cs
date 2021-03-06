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
using TanksRebirth.Graphics;

namespace TanksRebirth.GameContent
{
    public struct TankTemplate
    {
        /// <summary>If false, the template will contain data for an AI tank.</summary>
        public bool IsPlayer;

        public TankTier AiTier;
        public PlayerType PlayerType;

        public Vector2 Position;

        public float Rotation;

        public TankTeam Team;

        public Range<TankTier> RandomizeRange;

        public AITank GetAiTank()
        {
            if (IsPlayer)
                throw new Exception($"{nameof(IsPlayer)} was true. This method cannot execute.");

            var ai = new AITank(AiTier);
            ai.Body.Position = Position;
            ai.Position = Position;
            return ai;
        }
        public PlayerTank GetPlayerTank()
        {
            if (!IsPlayer)
                throw new Exception($"{nameof(IsPlayer)} was false. This method cannot execute.");

            var player = Difficulties.Types["RandomPlayer"] ? new PlayerTank(PlayerType, false, AITank.PickRandomTier()) : new PlayerTank(PlayerType);
            player.Body.Position = Position;
            player.Position = Position;
            return player;
        }
    }
    public interface ITankHurtContext
    {
        bool IsPlayer { get; set; }
        int TankId { get; set; }
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
    public struct TankHurtContext_Bullet : ITankHurtContext
    {
        public bool IsPlayer { get; set; }
        public uint Bounces { get; set; }

        public int TankId { get; set; }

        public ShellType ShellType { get; set; }

        public TankHurtContext_Bullet(bool isPlayer, uint bounces, ShellType type, int tankId)
        {
            IsPlayer = isPlayer;
            Bounces = bounces;
            ShellType = type;
            TankId = tankId;
        }
    }
    public struct TankHurtContext_Mine : ITankHurtContext
    {
        public bool IsPlayer { get; set; }
        public int TankId { get; set; }
        public TankHurtContext_Mine(bool isPlayer, int tankId)
        {
            IsPlayer = isPlayer;
            TankId = tankId;
        }
    }
    public abstract class Tank
    {
        public static Dictionary<string, Texture2D> Assets = new();

        public static string AssetRoot;

        public static List<TankTeam> GetActiveTeams()
        {
            var teams = new List<TankTeam>();
            foreach (var tank in GameHandler.AllTanks)
            {
                if (tank is not null && !tank.Dead)
                {
                    if (!teams.Contains(tank.Team))
                        teams.Add(tank.Team);
                }
            }
            return teams;
        }

        public static void SetAssetNames()
        {
            Assets.Clear();
            foreach (var tier in Enum.GetNames<TankTier>().Where(tier => (int)Enum.Parse<TankTier>(tier) > (int)TankTier.Random && (int)Enum.Parse<TankTier>(tier) < (int)TankTier.Explosive))
            {
                Assets.Add($"tank_" + tier.ToLower(), null);
            }
            foreach (var type in Enum.GetNames<PlayerType>())
            {
                Assets.Add($"tank_" + type.ToLower(), null);
            }
        }
        public static void LoadVanillaTextures()
        {
            Assets.Clear();

            foreach (var tier in Enum.GetNames<TankTier>().Where(tier => (int)Enum.Parse<TankTier>(tier) > (int)TankTier.Random && (int)Enum.Parse<TankTier>(tier) < (int)TankTier.Explosive))
            {
                if (!Assets.ContainsKey($"tank_" + tier.ToLower()))
                {
                    Assets.Add($"tank_" + tier.ToLower(), GameResources.GetGameResource<Texture2D>($"Assets/textures/tank/tank_{tier.ToLower()}"));
                }
            }
            foreach (var type in Enum.GetNames<PlayerType>())
            {
                if (!Assets.ContainsKey($"tank_" + type.ToLower()))
                {
                    Assets.Add($"tank_" + type.ToLower(), GameResources.GetGameResource<Texture2D>($"Assets/textures/tank/tank_{type.ToLower()}"));
                }
            }
            AssetRoot = "Assets/textures/tank";
        }

        public static void LoadTexturePack(string folder)
        {
            if (folder.ToLower() == "vanilla")
            {
                LoadVanillaTextures();
                GameHandler.ClientLog.Write($"Loaded vanilla textures for Tank.", LogType.Info);
                return;
            }

            var baseRoot = Path.Combine(TankGame.SaveDirectory, "Texture Packs");
            var rootGameScene = Path.Combine(TankGame.SaveDirectory, "Texture Packs", "Tank");
            var path = Path.Combine(rootGameScene, folder);

            // ensure that these directories exist before dealing with them
            Directory.CreateDirectory(baseRoot);
            Directory.CreateDirectory(rootGameScene);

            if (!Directory.Exists(path))
            {
                GameHandler.ClientLog.Write($"Error: Directory '{path}' not found when attempting texture pack load.", LogType.Warn);
                return;
            }

            AssetRoot = path;

            foreach (var file in Directory.GetFiles(path))
            {
                if (Assets.Any(type => type.Key == Path.GetFileNameWithoutExtension(file)))
                {
                    Assets[Path.GetFileNameWithoutExtension(file)] = Texture2D.FromFile(TankGame.Instance.GraphicsDevice, Path.Combine(path, Path.GetFileName(file)));
                    GameHandler.ClientLog.Write($"Texture pack '{folder}' overrided texture '{Path.GetFileNameWithoutExtension(file)}'", LogType.Info);
                }
            }
        }

        public static World CollisionsWorld = new(Vector2.Zero);
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

        public event EventHandler OnDestroy;

        public int WorldId { get; set; }

        public TankProperties Properties = new();

        public Vector2 Position;
        public Vector2 Velocity;

        /// <summary>This <see cref="Tank"/>'s <see cref="TankTeam"/>.</summary>
        public TankTeam Team { get; set; }
        /// <summary>The rotation of this <see cref="Tank"/>'s barrel. Generally should not be modified in a player context.</summary>>
        public BoundingBox Worldbox { get; set; }
        /// <summary>The 2D circle-represented hitbox of this <see cref="Tank"/>.</summary>
        public Circle CollisionCircle => new() { Center = Position, Radius = TNK_WIDTH / 2 };
        /// <summary>The 2D rectangle-represented hitbox of this <see cref="Tank"/>.</summary>
        public Rectangle CollisionBox => new((int)(Position.X - TNK_WIDTH / 2 + 3), (int)(Position.Y - TNK_WIDTH / 2 + 2), (int)TNK_WIDTH - 8, (int)TNK_HEIGHT - 4);
        /// <summary>How many times the <see cref="Shell"/> this <see cref="Tank"/> shoots can ricochet.</summary>
        public int OwnedShellCount { get; internal set; }
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
        #endregion

        public List<ICosmetic> Cosmetics = new();
        /// <summary>Apply all the default parameters for this <see cref="Tank"/>.</summary>
        public virtual void ApplyDefaults(ref TankProperties properties) { Properties = properties; }

        public virtual void Initialize()
        {
            if (TankGame.SecretCosmeticSetting)
            {
                for (int i = 0; i < 1; i++)
                {
                    var recieved = CosmeticChest.Basic.Open();

                    if (recieved is Cosmetic3D cosmetic1)
                        Cosmetics.Add(cosmetic1);
                    else if (recieved is Cosmetic2D cosmetic2)
                        Add2DCosmetic(cosmetic2);
                }
            }

            if (IsIngame)
            {
                Body = CollisionsWorld.CreateCircle(TNK_WIDTH * 0.4f, 1f, Position, BodyType.Dynamic);
                // Body.LinearDamping = Deceleration * 10;
            }

            foreach (var cos in Cosmetics)
                if (cos is Cosmetic2D cos2d)
                    Add2DCosmetic(cos2d);

            if (IsIngame)
            {
                if (Difficulties.Types["BulletHell"])
                    Properties.RicochetCount *= 3;
                if (Difficulties.Types["MachineGuns"])
                {
                    Properties.ShellCooldown = 5;
                    Properties.ShellLimit = 50;
                    Properties.ShootStun = 0;

                    if (this is AITank tank)
                        tank.AiParams.Inaccuracy *= 2;
                }
                if (Difficulties.Types["Shotguns"])
                {
                    Properties.ShellSpread = 0.3f;
                    Properties.ShellShootCount = 3;
                    Properties.ShellLimit *= 3;

                    if (this is AITank tank)
                        tank.AiParams.Inaccuracy *= 2;
                }
            }

            GameProperties.OnMissionStart += OnMissionStart;
        }

        public void Add2DCosmetic(Cosmetic2D cos2d, Func<bool> destroyOn = null)
        {
            if (!MainMenu.Active && IsIngame)
            {
                if (Cosmetics.Contains(cos2d))
                    return;
                Cosmetics.Add(cos2d);
                var particle = ParticleSystem.MakeParticle(Position3D + cos2d.RelativePosition, cos2d.Texture);

                particle.Scale = cos2d.Scale;
                particle.Tag = $"cosmetic_2d_{GetHashCode()}"; // store the hash code of this tank, so when we destroy the cosmetic's particle, it destroys all belonging to this tank!
                particle.isAddative = false;
                particle.UniqueBehavior = (z) =>
                {
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
        void OnMissionStart()
        {
            if (Difficulties.Types["FFA"])
                Team = TankTeam.NoTeam;
            if (Properties.Invisible && !Dead)
            {
                var invis = "Assets/sounds/tnk_invisible";
                SoundPlayer.PlaySoundInstance(invis, SoundContext.Effect, 0.3f);

                var lightParticle = ParticleSystem.MakeParticle(Position3D, GameResources.GetGameResource<Texture2D>("Assets/textures/misc/light_particle"));

                lightParticle.Scale = new(0.25f);
                lightParticle.Opacity = 0f;
                lightParticle.Is2d = true;

                lightParticle.UniqueBehavior = (lp) =>
                {
                    lp.Position = Position3D;
                    if (lp.Scale.X < 5f)
                        GeometryUtils.Add(ref lp.Scale, 0.12f);
                    if (lp.Opacity < 1f && lp.Scale.X < 5f)
                        lp.Opacity += 0.02f;

                    if (lp.LifeTime > 90)
                        lp.Opacity -= 0.005f;

                    if (lp.Scale.X < 0f)
                        lp.Destroy();
                };

                const int NUM_LOCATIONS = 8;

                for (int i = 0; i < NUM_LOCATIONS; i++)
                {
                    var lp = ParticleSystem.MakeParticle(Position3D + new Vector3(0, 5, 0), GameResources.GetGameResource<Texture2D>("Assets/textures/misc/tank_smokes"));

                    var velocity = Vector2.UnitY.RotatedByRadians(MathHelper.ToRadians(360f / NUM_LOCATIONS * i));

                    lp.Scale = new(1f);

                    lp.UniqueBehavior = (elp) =>
                    {
                        elp.Position.X += velocity.X;
                        elp.Position.Z += velocity.Y;

                        if (elp.LifeTime > 15)
                        {
                            GeometryUtils.Add(ref elp.Scale, -0.03f);
                            elp.Opacity -= 0.03f;
                        }

                        if (elp.Scale.X <= 0f || elp.Opacity <= 0f)
                            elp.Destroy();
                    };
                }
            }
        }

        public bool Flip;
        /// <summary>Update this <see cref="Tank"/>.</summary>
        public virtual void Update()
        {
            Position = Body.Position;

            Body.LinearVelocity = Velocity * 0.55f;

            World = Matrix.CreateFromYawPitchRoll(-TankRotation + (Flip ? MathHelper.Pi : 0f), 0, 0)
                * Matrix.CreateTranslation(Position3D);

            if (IsIngame)
            {
                Worldbox = new(Position3D - new Vector3(7, 0, 7), Position3D + new Vector3(10, 15, 10));
                Projection = TankGame.GameProjection;
                View = TankGame.GameView;

                if (!GameProperties.InMission || IntermissionSystem.IsAwaitingNewMission)
                {
                    Velocity = Vector2.Zero;
                    return;
                }
                if (OwnedShellCount < 0)
                    OwnedShellCount = 0;
                if (OwnedMineCount < 0)
                    OwnedMineCount = 0;
            }

            if (CurShootStun > 0)
                CurShootStun--;
            if (CurShootCooldown > 0)
                CurShootCooldown--;
            if (CurMineStun > 0)
                CurMineStun--;
            if (CurMineCooldown > 0)
                CurMineCooldown--;

            if (CurShootStun > 0 || CurMineStun > 0 || Properties.Stationary && IsIngame)
            {
                Body.LinearVelocity = Vector2.Zero;
                Velocity = Vector2.Zero;
            }
            // fix 2d peeopled
            foreach (var cosmetic in Cosmetics)
                cosmetic?.UniqueBehavior?.Invoke(cosmetic, this); 
        }
        /// <summary>Get this <see cref="Tank"/>'s general stats.</summary>
        public string GetGeneralStats()
            => $"Pos2D: {Position} | Vel: {Velocity} | Dead: {Dead}";
        /// <summary>Damage this <see cref="Tank"/>. If it has no armor, <see cref="Destroy"/> it.</summary>
        public virtual void Damage(ITankHurtContext context) {

            if (Dead || Properties.Immortal)
                return;
            // ChatSystem.SendMessage(context, Color.White, "<Debug>");
            if (Properties.Armor is not null)
            {
                if (Properties.Armor.HitPoints > 0)
                {
                    Properties.Armor.HitPoints--;
                    var ding = SoundPlayer.PlaySoundInstance($"Assets/sounds/armor_ding_{GameHandler.GameRand.Next(1, 3)}", SoundContext.Effect);
                    ding.Instance.Pitch = GameHandler.GameRand.NextFloat(-0.1f, 0.1f);
                }
                else
                    Destroy(context);
            }
            else
                Destroy(context);
        }
        /// <summary>Destroy this <see cref="Tank"/>.</summary>
        public virtual void Destroy(ITankHurtContext context) {

            GameProperties.OnMissionStart -= OnMissionStart;

            OnDestroy?.Invoke(this, new());
            var killSound1 = "Assets/sounds/tnk_destroy";
            SoundPlayer.PlaySoundInstance(killSound1, SoundContext.Effect, 0.2f);
            if (this is AITank t)
            {
                var killSound2 = "Assets/sounds/tnk_destroy_enemy";
                SoundPlayer.PlaySoundInstance(killSound2, SoundContext.Effect, 0.3f);

                var dm = new TankDeathMark(TankDeathMark.CheckColor.White)
                {
                    Position = Position3D + new Vector3(0, 0.1f, 0),
                };

                dm.StoredTank = new()
                {
                    AiTier = t.Tier,
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
                    PlayerType.Blue => TankDeathMark.CheckColor.Blue,
                    PlayerType.Red => TankDeathMark.CheckColor.Red,
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

                    var part = ParticleSystem.MakeParticle(Position3D, tex);

                    part.isAddative = false;

                    var vel = new Vector3(GameHandler.GameRand.NextFloat(-3, 3), GameHandler.GameRand.NextFloat(3, 6), GameHandler.GameRand.NextFloat(-3, 3));

                    part.Roll = -TankGame.DEFAULT_ORTHOGRAPHIC_ANGLE;

                    part.Scale = new(0.55f);

                    part.Color = Properties.DestructionColor;

                    part.UniqueBehavior = (p) =>
                    {
                        part.Pitch += MathF.Sin(part.Position.Length() / 10);
                        vel.Y -= 0.2f;
                        part.Position += vel;
                        part.Opacity -= 0.025f;

                        if (part.Opacity <= 0f)
                            part.Destroy();
                    };
                }

                var partExpl = ParticleSystem.MakeParticle(Position3D, GameResources.GetGameResource<Texture2D>("Assets/textures/misc/bot_hit"));

                partExpl.Color = Color.Yellow * 0.75f;

                partExpl.Scale = new(5f);

                partExpl.Is2d = true;

                partExpl.UniqueBehavior = (p) =>
                {
                    GeometryUtils.Add(ref p.Scale, -0.3f);
                    p.Opacity -= 0.06f;
                    if (p.Scale.X <= 0f)
                        p.Destroy();
                };
                ParticleSystem.MakeSmallExplosion(Position3D, 15, 20, 1.3f, 30);
            }
            doDestructionFx();
            Remove();
        }
        /// <summary>Lay a <see cref="TankFootprint"/> under this <see cref="Tank"/>.</summary>
        public virtual void LayFootprint(bool alt) {
            if (!Properties.CanLayTread)
                return;
            new TankFootprint(this, alt)
            {
                Position = Position3D + new Vector3(0, 0.1f, 0),
                rotation = -TankRotation
            };
        }
        /// <summary>Shoot a <see cref="Shell"/> from this <see cref="Tank"/>.</summary>
        public virtual void Shoot() {
            if (!GameProperties.InMission || !Properties.HasTurret)
                return;

            if (CurShootCooldown > 0 || OwnedShellCount >= Properties.ShellLimit / Properties.ShellShootCount)
                return;

            bool flip = false;
            float angle = 0f;

            for (int i = 0; i < Properties.ShellShootCount; i++)
            {
                if (i == 0)
                {
                    var shell = new Shell(Position3D, Vector3.Zero, Properties.ShellType, this, homing: Properties.ShellHoming);
                    var new2d = Vector2.UnitY.RotatedByRadians(TurretRotation);

                    var newPos = Position + new Vector2(0, 20).RotatedByRadians(-TurretRotation);

                    shell.Position = new Vector3(newPos.X, 11, newPos.Y);

                    shell.Velocity = new Vector3(-new2d.X, 0, new2d.Y) * Properties.ShellSpeed;

                    shell.Owner = this;
                    shell.RicochetsRemaining = Properties.RicochetCount;

                    #region Particles
                    var hit = ParticleSystem.MakeParticle(shell.Position, GameResources.GetGameResource<Texture2D>("Assets/textures/misc/bot_hit"));
                    var smoke = ParticleSystem.MakeParticle(shell.Position, GameResources.GetGameResource<Texture2D>("Assets/textures/misc/tank_smokes"));

                    hit.Roll = -TankGame.DEFAULT_ORTHOGRAPHIC_ANGLE;
                    smoke.Roll = -TankGame.DEFAULT_ORTHOGRAPHIC_ANGLE;

                    smoke.Scale = new(0.35f);
                    hit.Scale = new(0.5f);

                    smoke.Color = new(84, 22, 0, 255);

                    smoke.isAddative = false;

                    int achieveable = 80;
                    int step = 1;

                    hit.UniqueBehavior = (part) =>
                    {
                        part.Color = Color.Orange;

                        if (part.LifeTime > 1)
                            part.Opacity -= 0.1f;
                        if (part.Opacity <= 0)
                            part.Destroy();
                    };
                    smoke.UniqueBehavior = (part) =>
                    {
                        part.Color.R = (byte)GameUtils.RoughStep(part.Color.R, achieveable, step);
                        part.Color.G = (byte)GameUtils.RoughStep(part.Color.G, achieveable, step);
                        part.Color.B = (byte)GameUtils.RoughStep(part.Color.B, achieveable, step);

                        GeometryUtils.Add(ref part.Scale, 0.004f);

                        if (part.Color.G == achieveable)
                        {
                            part.Color.B = (byte)achieveable;
                            part.Opacity -= 0.04f;

                            if (part.Opacity <= 0)
                                part.Destroy();
                        }
                    };
                    #endregion
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

                    shell.Owner = this;
                    shell.RicochetsRemaining = Properties.RicochetCount;
                }
            }

            /*var force = (Position - newPos) * Recoil;
            Velocity = force;
            Body.ApplyForce(force);*/

            OwnedShellCount += Properties.ShellShootCount;

            timeSinceLastAction = 0;

            CurShootStun = Properties.ShootStun;
            CurShootCooldown = Properties.ShellCooldown;
        }
        /// <summary>Make this <see cref="Tank"/> lay a <see cref="Mine"/>.</summary>
        public virtual void LayMine() {
            if (CurMineCooldown > 0 || OwnedMineCount >= Properties.MineLimit)
                return;

            CurMineCooldown = Properties.MineCooldown;
            CurMineStun = Properties.MineStun;
            var sound = "Assets/sounds/mine_place";
            SoundPlayer.PlaySoundInstance(sound, SoundContext.Effect, 0.5f);
            OwnedMineCount++;

            timeSinceLastAction = 0;

            var mine = new Mine(this, Position, 600);
        }
        public virtual void Render() {
            if (IsIngame)
            {
                foreach (var cosmetic in Cosmetics)
                {
                    if (GameProperties.InMission && Properties.Invisible)
                        break;
                    if (cosmetic is Cosmetic3D cos3d)
                    {
                        for (int i = 0; i < (Lighting.AccurateShadows ? 2 : 1); i++)
                        {
                            foreach (ModelMesh mesh in cos3d.Model.Meshes)
                            {
                                if (!cos3d.IgnoreMeshesByName.Any(meshname => meshname == mesh.Name))
                                {
                                    foreach (BasicEffect effect in mesh.Effects)
                                    {
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
            var info = new string[]
            {
                $"Team: {Team}",
                $"OwnedShellCount: {OwnedShellCount}",
                $"OwnedMineCount: {OwnedMineCount}",
                $"Speed / MaxSpeed / Velocity: {Properties.Speed} / {Properties.MaxSpeed} / {Velocity}",
                $"Invisible: {Properties.Invisible}",
                $"ShellCooldown: {Properties.ShellCooldown}",
                $"MineCooldown: {Properties.MineCooldown}"
            };

            // TankGame.spriteBatch.Draw(GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel"), CollisionBox2D, Color.White * 0.75f);

            for (int i = 0; i < info.Length; i++)
                DebugUtils.DrawDebugString(TankGame.SpriteRenderer, info[i], GeometryUtils.ConvertWorldToScreen(Vector3.Zero, World, View, Projection) - new Vector2(0, (info.Length * 20) + (i * 20)), 1, centered: true);

        }

        public uint CurShootStun { get; private set; } = 0;
        public uint CurShootCooldown { get; private set; } = 0;
        public uint CurMineCooldown { get; private set; } = 0;
        public uint CurMineStun { get; private set; } = 0;

        public uint timeSinceLastAction = 15000;

        public virtual void Remove() 
        {            
            if (CollisionsWorld.BodyList.Contains(Body))
                CollisionsWorld.Remove(Body);
            foreach (var particle in ParticleSystem.CurrentParticles)
                if (particle is not null)
                    if ((string)particle.Tag == $"cosmetic_2d_{GetHashCode()}") // remove all particles related to this tank
                        particle.Destroy();
            GameProperties.OnMissionStart -= OnMissionStart;
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
        /// <summary>The current speed of this tank.</summary>
        public float Speed { get; set; }
        /// <summary>The maximum speed this tank can achieve.</summary>
        public float MaxSpeed { get; set; }
        /// <summary>How fast the bullets this <see cref="Tank"/> shoot are.</summary>
        public float ShellSpeed { get; set; }
        /// <summary>The rotation of this <see cref="Tank"/>'s barrel. Generally should not be modified in a player context.</summary>
        /// <summary>The pitch of the footprint placement sounds.</summary>
        public float TreadPitch { get; set; }
        /// <summary>The pitch of the shoot sound.</summary>
        public float ShootPitch { get; set; }
        /// <summary>The type of bullet this <see cref="Tank"/> shoots.</summary>
        public ShellType ShellType { get; set; }
        /// <summary>The maximum amount of mines this <see cref="Tank"/> can place.</summary>
        public uint MineLimit { get; set; }
        /// <summary>How long this <see cref="Tank"/> will be immobile upon firing a bullet.</summary>
        public uint ShootStun { get; set; }
        /// <summary>How long this <see cref="Tank"/> will be immobile upon laying a mine.</summary>
        public uint MineStun { get; set; }
        /// <summary>How long this <see cref="Tank"/> has to wait until it can fire another bullet..</summary>
        public uint ShellCooldown { get; set; }
        /// <summary>How long until this <see cref="Tank"/> can lay another mine</summary>
        public uint MineCooldown { get; set; }
        /// <summary>How many times the <see cref="Shell"/> this <see cref="Tank"/> shoots can ricochet.</summary>
        public uint RicochetCount { get; set; }
        /// <summary>How many bounces the <see cref="Shell"/> this <see cref="Tank"/> shoots has remaining.</summary>
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
        public TrackType Track { get; set; }
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

        public bool VulnerableToMines { get; set; } = true;

        public bool Immortal { get; set; } = false;

        /// <summary>The homing properties of the shells this <see cref="Tank"/> shoots.</summary>
        public Shell.HomingProperties ShellHoming = new();
    }
    public class TankFootprint
    {
        public const int MAX_FOOTPRINTS = 100000;

        public static TankFootprint[] footprints = new TankFootprint[TankGame.Settings.TankFootprintLimit];

        public Vector3 Position;
        public float rotation;

        public Texture2D texture;

        internal static int total_treads_placed;

        public readonly bool alternate;

        public long lifeTime;

        public readonly Particle track;
        public readonly Tank owner;

        public TankFootprint(Tank owner, bool alt = false)
        {
            alternate = alt;
            this.owner = owner;
            if (total_treads_placed + 1 > MAX_FOOTPRINTS)
                footprints[Array.IndexOf(footprints, footprints.Min(x => x.lifeTime > 0))] = null; // i think?

            alternate = alt;
            total_treads_placed++;

            texture = GameResources.GetGameResource<Texture2D>(alt ? $"Assets/textures/tank_footprint_alt" : $"Assets/textures/tank_footprint");

            track = ParticleSystem.MakeParticle(Position, texture);

            track.isAddative = false;

            track.Roll = -MathHelper.PiOver2;
            track.Scale = new(0.5f, 0.55f, 0.5f);
            track.Opacity = 0.7f;

            footprints[total_treads_placed] = this;

            total_treads_placed++;
        }

        public void Render()
        {
            lifeTime++;

            track.Position = Position;
            track.Pitch = rotation;
            track.Color = Color.White;
            // Vector3 scale = alternate ? new(0.5f, 1f, 0.35f) : new(0.5f, 1f, 0.075f);
            // [0.0, 1.1, 1.5, 0.5]
            // [0.0, 0.1, 0.8, 0.0]
            // [0.0, 0.5, 1.2, 1.0]
            // [0.0, 2.0, 0.6, 0.2]
        }
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
            White
        }
        /// <summary>Resurrects <see cref="StoredTank"/>.</summary>
        public void ResurrectTank()
        {
            // FIXME: gonna be real... i have no clue why this makes tank become invincible.
            if (StoredTank.IsPlayer)
            {
                StoredTank.GetPlayerTank();
            }
            else
            {
                StoredTank.GetAiTank();
            }
        }

        public TankDeathMark(CheckColor color)
        {
            if (total_death_marks + 1 > MAX_DEATH_MARKS)
                return;
            total_death_marks++;

            texture = GameResources.GetGameResource<Texture2D>($"Assets/textures/check/check_{color.ToString().ToLower()}");

            check = ParticleSystem.MakeParticle(Position + new Vector3(0, 0.1f, 0), texture);
            check.isAddative = false;
            check.Roll = -MathHelper.PiOver2;

            deathMarks[total_death_marks] = this;
        }

        public void Render()
        {
            check.Position = Position;
            check.Scale = new(0.6f);
        }
    }
}