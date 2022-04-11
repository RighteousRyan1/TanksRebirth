using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using TanksRebirth.Enums;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals.Core.Interfaces;
using TanksRebirth.Graphics;
using System.Linq;
using TanksRebirth.Internals.Common.Framework.Audio;
using Microsoft.Xna.Framework.Audio;
using TanksRebirth.GameContent.GameMechanics;
using tainicom.Aether.Physics2D;
using Phys = tainicom.Aether.Physics2D.Collision;
using tainicom.Aether.Physics2D.Dynamics;
using tainicom.Aether.Physics2D.Collision.Shapes;
using tainicom.Aether.Physics2D.Content;
using tainicom.Aether.Physics2D.Fluids;
using tainicom.Aether.Physics2D.Common;
using tainicom.Aether.Physics2D.Controllers;
using TanksRebirth.Internals.Common.Framework;
using TanksRebirth.GameContent.Systems;

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

        public Team Team;

        public Range<TankTier> RandomizeRange;

        public AITank GetAiTank()
        {
            if (IsPlayer)
                throw new Exception($"{nameof(PlayerType)} was true! This method cannot execute.");

            var ai = new AITank(AiTier, default, true, true);
            ai.Body.Position = Position;
            ai.Position = Position;
            return ai;
        }
        public PlayerTank GetPlayerTank()
        {
            if (!IsPlayer)
                throw new Exception($"{nameof(PlayerType)} was true! This method cannot execute.");

            var player = new PlayerTank(PlayerType);
            player.Body.Position = Position;
            player.Position = Position;
            return player;
        }
    }

    public enum TankHurtContext
    {
        ByPlayerBullet,
        ByAiBullet,
        ByPlayerMine,
        ByAiMine,
        ByExplosion,
        Other
    }
    public abstract class Tank
    {
        public static World CollisionsWorld = new(Vector2.Zero);
        public const float TNK_WIDTH = 25;
        public const float TNK_HEIGHT = 25;
        #region Fields / Properties
        public Body Body { get; set; } = new();

        public event EventHandler OnDestroy;

        public int WorldId { get; set; }

        /// <summary>This <see cref="Tank"/>'s model.</summary>
        public Model Model { get; set; }
        /// <summary>This <see cref="Tank"/>'s world position. Used to change the actual location of the model relative to the <see cref="View"/> and <see cref="Projection"/>.</summary>
        public Matrix World { get; set; }
        /// <summary>How the <see cref="Model"/> is viewed through the <see cref="Projection"/>.</summary>
        public Matrix View { get; set; }
        /// <summary>The projection from the screen to the <see cref="Model"/>.</summary>
        public Matrix Projection { get; set; }
        /// <summary>Whether or not the tank has artillery-like function during gameplay.</summary>
        public bool Stationary { get; set; }
        /// <summary>Whether or not the tank has been destroyed or not.</summary>
        public bool Dead { get; set; }
        /// <summary>Whether or not the tank should become invisible at mission start.</summary>
        public bool Invisible { get; set; }
        /// <summary>How fast the tank should accelerate towards its <see cref="MaxSpeed"/>.</summary>
        public float Acceleration { get; set; } = 0.3f;
        /// <summary>How fast the tank should decelerate when not moving.</summary>
        public float Deceleration { get; set; } = 0.6f;
        /// <summary>The current speed of this tank.</summary>
        public float Speed { get; set; } = 1f;
        /// <summary>The maximum speed this tank can achieve.</summary>
        public float MaxSpeed { get; set; } = 1f;
        /// <summary>How fast the bullets this <see cref="Tank"/> shoot are.</summary>
        public float ShellSpeed { get; set; } = 1f;
        /// <summary>The rotation of this <see cref="Tank"/>'s barrel. Generally should not be modified in a player context.</summary>
        public float TurretRotation { get; set; }
        /// <summary>The rotation of this <see cref="Tank"/>.</summary>
        public float TankRotation { get; set; }
        /// <summary>The pitch of the footprint placement sounds.</summary>
        public float TreadPitch { get; set; }
        /// <summary>The pitch of the shoot sound.</summary>
        public float ShootPitch { get; set; }
        /// <summary>The type of bullet this <see cref="Tank"/> shoots.</summary>
        public ShellTier ShellType { get; set; } = ShellTier.Standard;
        /// <summary>The maximum amount of mines this <see cref="Tank"/> can place.</summary>
        public int MineLimit { get; set; }
        /// <summary>The 3D hitbox of this <see cref="Tank"/>.</summary>
        public BoundingBox Worldbox { get; set; }
        /// <summary>The 2D hitbox of this <see cref="Tank"/>.</summary>
        public Rectangle CollisionBox2D => new((int)(Position.X - TNK_WIDTH / 2 + 3), (int)(Position.Y - TNK_WIDTH / 2 + 2), (int)TNK_WIDTH - 8, (int)TNK_HEIGHT - 4);
        /// <summary>How long this <see cref="Tank"/> will be immobile upon firing a bullet.</summary>
        public int ShootStun { get; set; }
        /// <summary>How long this <see cref="Tank"/> will be immobile upon laying a mine.</summary>
        public int MineStun { get; set; }
        /// <summary>How long this <see cref="Tank"/> has to wait until it can fire another bullet..</summary>
        public int ShellCooldown { get; set; }
        /// <summary>How long until this <see cref="Tank"/> can lay another mine</summary>
        public int MineCooldown { get; set; }
        /// <summary>How many times the <see cref="Shell"/> this <see cref="Tank"/> shoots ricochets.</summary>
        public int RicochetCount { get; set; }
        /// <summary>The amount of <see cref="Shell"/>s this <see cref="Tank"/> can own on-screen at any given time.</summary>
        public int ShellLimit { get; set; }
        /// <summary>How fast this <see cref="Tank"/> turns.</summary>
        public float TurningSpeed { get; set; } = 1f;
        /// <summary>The maximum angle this <see cref="Tank"/> can turn (in radians) before it has to start pivoting.</summary>
        public float MaximalTurn { get; set; }
        /// <summary>The <see cref="GameContent.Team"/> this <see cref="Tank"/> is on.</summary>
        public Team Team { get; set; }
        /// <summary>How many <see cref="Shell"/>s this <see cref="Tank"/> owns.</summary>
        public int OwnedShellCount { get; internal set; }
        /// <summary>How many <see cref="Mine"/>s this <see cref="Tank"/> owns.</summary>
        public int OwnedMineCount { get; internal set; }
        /// <summary>Whether or not this <see cref="Tank"/> can lay a <see cref="TankFootprint"/>.</summary>
        public bool CanLayTread { get; set; } = true;
        /// <summary>Whether or not this <see cref="Tank"/> is currently turning.</summary>
        public bool IsTurning { get; internal set; }
        /// <summary>Whether or not this <see cref="Tank"/> is being hovered by the pointer.</summary>
        public bool IsHoveredByMouse { get; internal set; }

        public Vector2 Position;
        public Vector2 Velocity;

        public float TargetTankRotation;

        public Vector3 Position3D => Position.ExpandZ();
        public Vector3 Velocity3D => Velocity.ExpandZ();
        #endregion
        /// <summary>Apply all the default parameters for this <see cref="Tank"/>.</summary>
        public virtual void ApplyDefaults() { }

        public virtual void Initialize()
        {
            if (IsIngame)
            {
                Body = CollisionsWorld.CreateCircle(TNK_WIDTH * 0.4f, 1f, Position, BodyType.Dynamic);
                // Body.LinearDamping = Deceleration * 10;
            }
            
            if (Difficulties.Types["BulletHell"])
                RicochetCount *= 3;
            if (Difficulties.Types["MachineGuns"])
            {
                ShellCooldown = 5;
                ShellLimit = 50;
                ShootStun = 0;

                if (this is AITank tank)
                    tank.AiParams.Inaccuracy *= 2;
            }

            GameHandler.OnMissionStart += OnMissionStart;
        }
        void OnMissionStart()
        {
            if (Invisible && !Dead)
            {
                var invis = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_invisible");
                SoundPlayer.PlaySoundInstance(invis, SoundContext.Effect, 0.3f);

                var lightParticle = ParticleSystem.MakeParticle(Position3D, GameResources.GetGameResource<Texture2D>("Assets/textures/misc/light_particle"));

                lightParticle.Scale = new(0.25f);
                lightParticle.Opacity = 0f;
                lightParticle.is2d = true;

                lightParticle.UniqueBehavior = (lp) =>
                {
                    lp.position = Position3D;
                    if (lp.Scale.X < 5f)
                        GeometryUtils.Add(ref lp.Scale, 0.12f);
                    if (lp.Opacity < 1f && lp.Scale.X < 5f)
                        lp.Opacity += 0.02f;

                    if (lp.lifeTime > 90)
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
                        elp.position.X += velocity.X;
                        elp.position.Z += velocity.Y;

                        if (elp.lifeTime > 15)
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

        /// <summary>Update this <see cref="Tank"/>.</summary>
        public virtual void Update()
        {
            Position = Body.Position;

            Body.LinearVelocity = Velocity * 0.55f;

            World = Matrix.CreateFromYawPitchRoll(-TankRotation, 0, 0)
                * Matrix.CreateTranslation(Position3D);

            if (IsIngame)
            {
                Worldbox = new(Position3D - new Vector3(7, 0, 7), Position3D + new Vector3(10, 15, 10));
                Projection = TankGame.GameProjection;
                View = TankGame.GameView;

                if (!GameHandler.InMission || IntermissionSystem.IsAwaitingNewMission)
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

            if (CurShootStun > 0 || CurMineStun > 0 || Stationary && IsIngame)
            {
                Body.LinearVelocity = Vector2.Zero;
                Velocity = Vector2.Zero;
            }
        }
        /// <summary>Get this <see cref="Tank"/>'s general stats.</summary>
        public string GetGeneralStats()
            => $"Pos2D: {Position} | Vel: {Velocity} | Dead: {Dead}";
        /// <summary>Destroy this <see cref="Tank"/>.</summary>
        public virtual void Damage(TankHurtContext context) {

            if (Dead || Immortal)
                return;
            // ChatSystem.SendMessage(context, Color.White, "<Debug>");
            if (Armor is not null)
            {
                if (Armor.HitPoints > 0)
                {
                    Armor.HitPoints--;
                    var ding = SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/armor_ding_{GameHandler.GameRand.Next(1, 3)}"), SoundContext.Effect);
                    ding.Pitch = GameHandler.GameRand.NextFloat(-0.1f, 0.1f);
                }
                else
                    Destroy(context);
            }
            else
                Destroy(context);
        }
        public virtual void Destroy(TankHurtContext context) {
            OnDestroy?.Invoke(this, new());
            if (CollisionsWorld.BodyList.Contains(Body))
            {
                CollisionsWorld.Remove(Body);
                Body = null;
            }
            var killSound1 = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_destroy");
            SoundPlayer.PlaySoundInstance(killSound1, SoundContext.Effect, 0.2f);
            if (this is AITank)
            {
                var killSound2 = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_destroy_enemy");
                SoundPlayer.PlaySoundInstance(killSound2, SoundContext.Effect, 0.3f);

                new TankDeathMark(TankDeathMark.CheckColor.White)
                {
                    Position = Position3D + new Vector3(0, 0.1f, 0)
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

                new TankDeathMark(c)
                {
                    Position = Position3D + new Vector3(0, 0.1f, 0)
                };
            }

            Armor?.Remove();
            Armor = null;
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

                    part.color = TankDestructionColor;

                    part.UniqueBehavior = (p) =>
                    {
                        part.Pitch += MathF.Sin(part.position.Length() / 10);
                        vel.Y -= 0.2f;
                        part.position += vel;
                        part.Opacity -= 0.025f;

                        if (part.Opacity <= 0f)
                            part.Destroy();
                    };
                }

                var partExpl = ParticleSystem.MakeParticle(Position3D, GameResources.GetGameResource<Texture2D>("Assets/textures/misc/bot_hit"));

                partExpl.color = Color.Yellow * 0.75f;

                partExpl.Scale = new(5f);

                partExpl.is2d = true;

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
            if (!CanLayTread)
                return;
            var fp = new TankFootprint(this, alt)
            {
                Position = Position3D + new Vector3(0, 0.1f, 0),
                rotation = -TankRotation
            };
        }
        /// <summary>Shoot a <see cref="Shell"/> from this <see cref="Tank"/>.</summary>
        public virtual void Shoot() {
            if (!GameHandler.InMission || !HasTurret)
                return;

            if (CurShootCooldown > 0 || OwnedShellCount >= ShellLimit)
                return;

            var shell = new Shell(Position3D, Vector3.Zero, ShellType, this, homing: ShellHoming);
            var new2d = Vector2.UnitY.RotatedByRadians(TurretRotation);

            var newPos = Position + new Vector2(0, 20).RotatedByRadians(-TurretRotation);

            shell.Position = new Vector3(newPos.X, 11, newPos.Y);

            shell.Velocity = new Vector3(-new2d.X, 0, new2d.Y) * ShellSpeed;

            shell.owner = this;
            shell.ricochets = RicochetCount;

            /*var force = (Position - newPos) * Recoil;
            Velocity = force;
            Body.ApplyForce(force);*/

            #region Particles
            var hit = ParticleSystem.MakeParticle(shell.Position, GameResources.GetGameResource<Texture2D>("Assets/textures/misc/bot_hit"));
            var smoke = ParticleSystem.MakeParticle(shell.Position, GameResources.GetGameResource<Texture2D>("Assets/textures/misc/tank_smokes"));

            hit.Roll = -TankGame.DEFAULT_ORTHOGRAPHIC_ANGLE;
            smoke.Roll = -TankGame.DEFAULT_ORTHOGRAPHIC_ANGLE;

            smoke.Scale = new(0.35f);
            hit.Scale = new(0.5f);

            smoke.color = new(84, 22, 0, 255);

            smoke.isAddative = false;

            int achieveable = 80;
            int step = 1;

            hit.UniqueBehavior = (part) =>
            {
                part.color = Color.Orange;

                if (part.lifeTime > 1)
                    part.Opacity -= 0.1f;
                if (part.Opacity <= 0)
                    part.Destroy();
            };
            smoke.UniqueBehavior = (part) =>
            {
                part.color.R = (byte)GameUtils.RoughStep(part.color.R, achieveable, step);
                part.color.G = (byte)GameUtils.RoughStep(part.color.G, achieveable, step);
                part.color.B = (byte)GameUtils.RoughStep(part.color.B, achieveable, step);

                GeometryUtils.Add(ref part.Scale, 0.004f);

                if (part.color.G == achieveable)
                {
                    part.color.B = (byte)achieveable;
                    part.Opacity -= 0.04f;

                    if (part.Opacity <= 0)
                        part.Destroy();
                }
            };
            #endregion

            OwnedShellCount++;

            timeSinceLastAction = 0;

            CurShootStun = ShootStun;
            CurShootCooldown = ShellCooldown;
        }
        /// <summary>Make this <see cref="Tank"/> lay a <see cref="Mine"/>.</summary>
        public virtual void LayMine() {
            if (CurMineCooldown > 0 || OwnedMineCount >= MineLimit)
                return;

            CurMineCooldown = MineCooldown;
            CurMineStun = MineStun;
            var sound = GameResources.GetGameResource<SoundEffect>("Assets/sounds/mine_place");
            SoundPlayer.PlaySoundInstance(sound, SoundContext.Effect, 0.5f);
            OwnedMineCount++;

            timeSinceLastAction = 0;

            var mine = new Mine(this, Position, 600);
        }

        /// <summary>The color of particle <see cref="Tank"/> emits upon destruction.</summary>
        public Color TankDestructionColor { get; set; } = Color.Black;

        public int CurShootStun { get; private set; } = 0;
        public int CurShootCooldown { get; private set; } = 0;
        public int CurMineCooldown { get; private set; } = 0;
        public int CurMineStun { get; private set; } = 0;

        // everything under this comment is added outside of the faithful remake. homing shells, etc

        /// <summary>Whether or not this <see cref="Tank"/> has a turret to fire shells with.</summary>
        public bool HasTurret { get; set; } = true;

        public bool VulnerableToMines { get; set; } = true;

        public bool Immortal { get; set; } = false;

        /// <summary>The homing properties of the shells this <see cref="Tank"/> shoots.</summary>
        public Shell.HomingProperties ShellHoming = new();

        public int timeSinceLastAction = 15000;
        /// <summary>Whether or not this tank is used for ingame purposes or not.</summary>
        public bool IsIngame { get; set; } = true;
        /// <summary>The armor properties this <see cref="Tank"/> has.</summary>
        public Armor Armor { get; set; } = null;
        
        // Get it working before using this.
        /// <summary>How much this <see cref="Tank"/> is launched backward after firing a shell.</summary>
        public float Recoil { get; set; } = 0f;

        public virtual void Remove() 
        {
            if (CollisionsWorld.BodyList.Contains(Body))
                CollisionsWorld.Remove(Body);
            GameHandler.OnMissionStart -= OnMissionStart;
        }
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

            track.position = Position;
            track.Pitch = rotation;
            track.color = Color.White;
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

        public enum CheckColor
        {
            Blue,
            Red,
            White
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
            check.position = Position;
            check.Scale = new(0.6f);
        }
    }

    public enum Team
    {
        NoTeam,
        Red     = 1, 
        Blue    = 2,
        Green   = 3,
        Yellow  = 4,
        Purple  = 5,
        Orange  = 6,
        Cyan    = 7,
        Magenta = 8
    }
}