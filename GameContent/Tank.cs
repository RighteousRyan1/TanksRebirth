using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using WiiPlayTanksRemake.Enums;
using WiiPlayTanksRemake.Internals;
using WiiPlayTanksRemake.Internals.Common.Utilities;
using WiiPlayTanksRemake.Internals.Core.Interfaces;
using WiiPlayTanksRemake.Graphics;
using System.Linq;
using WiiPlayTanksRemake.Internals.Common.Framework.Audio;
using Microsoft.Xna.Framework.Audio;
using WiiPlayTanksRemake.GameContent.GameMechanics;
using tainicom.Aether.Physics2D;
using Phys = tainicom.Aether.Physics2D.Collision;
using tainicom.Aether.Physics2D.Dynamics;
using tainicom.Aether.Physics2D.Collision.Shapes;
using tainicom.Aether.Physics2D.Content;
using tainicom.Aether.Physics2D.Fluids;
using tainicom.Aether.Physics2D.Common;
using tainicom.Aether.Physics2D.Controllers;

namespace WiiPlayTanksRemake.GameContent
{
    public abstract class Tank
    {
        public static World CollisionsWorld = new World(Vector2.Zero);
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
        /// <summary>The amount of <see cref="Shell"/>s this <see cref="Tank"/> can own on-scren at any given time.</summary>
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
        }

        /// <summary>Update this <see cref="Tank"/>.</summary>
        public virtual void Update()
        {
            if (IsIngame)
            {
                Worldbox = new(Position3D - new Vector3(7, 15, 7), Position3D + new Vector3(10, 15, 10));
                Projection = TankGame.GameProjection;
                View = TankGame.GameView;
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
                Velocity = Vector2.Zero;

            World = Matrix.CreateFromYawPitchRoll(-TankRotation, 0, 0)
                * Matrix.CreateTranslation(Position3D);

            Position = Body.Position;

            Body.AngularDamping = 100;

            Body.LinearVelocity = Velocity * 0.55f;
        }
        /// <summary>Get this <see cref="Tank"/>'s general stats.</summary>
        public string GetGeneralStats()
            => $"Pos2D: {Position} | Vel: {Velocity} | Dead: {Dead}";
        /// <summary>Destroy this <see cref="Tank"/>.</summary>
        public virtual void Destroy() {

            if (CollisionsWorld.BodyList.Contains(Body))
                CollisionsWorld.Remove(Body);
            Dead = true;
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
                var killSound2 = GameResources.GetGameResource<SoundEffect>($"Assets/fanfares/tank_player_death");
                SoundPlayer.PlaySoundInstance(killSound2, SoundContext.Effect, 0.3f);

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

            void doDestructionFx()
            {

                for (int i = 0; i < 12; i++)
                {
                    var tex = GameResources.GetGameResource<Texture2D>(GameHandler.GameRand.Next(0, 2) == 0 ? "Assets/textures/misc/tank_rock" : "Assets/textures/misc/tank_rock_2");

                    var part = ParticleSystem.MakeParticle(Position3D, tex);

                    part.isAddative = false;

                    var vel = new Vector3(GameHandler.GameRand.NextFloat(-3, 3), GameHandler.GameRand.NextFloat(3, 6), GameHandler.GameRand.NextFloat(-3, 3));

                    part.rotationX = -TankGame.DEFAULT_ORTHOGRAPHIC_ANGLE;

                    part.Scale = new(0.55f);

                    part.color = TankDestructionColor;

                    part.UniqueBehavior = (p) =>
                    {
                        part.rotationY += MathF.Sin(part.position.Length() / 10);
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

                const int NUM_LOCATIONS = 8;

                for (int i = 0; i < NUM_LOCATIONS; i++)
                {
                    var tex = GameResources.GetGameResource<Texture2D>("Assets/textures/misc/tank_smokes");

                    var part = ParticleSystem.MakeParticle(Position3D, tex);

                    part.isAddative = false;

                    part.rotationX = -TankGame.DEFAULT_ORTHOGRAPHIC_ANGLE;

                    part.Scale = new(0.8f);

                    var velocity = Vector2.UnitY.RotatedByRadians(MathHelper.ToRadians(360f / NUM_LOCATIONS * i)).ExpandZ() / 2;

                    part.position.Y += 5f;

                    part.color = Color.DarkOrange;//new(152, 96, 26);

                    part.UniqueBehavior = (p) =>
                    {
                        part.position += velocity;
                        GeometryUtils.Add(ref part.Scale, -0.01f);

                        if (part.Scale.X <= 0f)
                            part.Destroy();

                        if (part.lifeTime > 40)
                        {
                            part.Opacity -= 0.02f;
                            part.position.Y += 0.25f;
                        }
                    };
                }
            }
            doDestructionFx();
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

            var hit = ParticleSystem.MakeParticle(shell.Position, GameResources.GetGameResource<Texture2D>("Assets/textures/misc/bot_hit"));
            var smoke = ParticleSystem.MakeParticle(shell.Position, GameResources.GetGameResource<Texture2D>("Assets/textures/misc/tank_smokes"));

            hit.rotationX = -TankGame.DEFAULT_ORTHOGRAPHIC_ANGLE;
            smoke.rotationX = -TankGame.DEFAULT_ORTHOGRAPHIC_ANGLE;

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
        /// <summary>Update this <see cref="Tank"/>'s collision.</summary>

        public Color TankDestructionColor { get; set; }

        public int CurShootStun { get; private set; }
        public int CurShootCooldown { get; private set; }
        public int CurMineCooldown { get; private set; }
        public int CurMineStun { get; private set; }

        // everything under this comment is added outside of the faithful remake. homing shells, etc

        /// <summary>Whether or not this <see cref="Tank"/> has a turret to fire shells with.</summary>

        public bool HasTurret { get; set; } = true;

        /// <summary>The <see cref="Shell.HomingProperties"/>of the bullets this <see cref="Tank"/> shoots.</summary>
        public Shell.HomingProperties ShellHoming = new();

        public int timeSinceLastAction = 15000;

        public bool IsIngame { get; set; } = true;

        public virtual void RemoveSilently() 
        { 
            Dead = true;
            if (CollisionsWorld.BodyList.Contains(Body))
                CollisionsWorld.Remove(Body); 
        }
    }

    /*public class TankFootprint
    {
        public const int MAX_FOOTPRINTS = 100000;

        public static TankFootprint[] footprints = new TankFootprint[TankGame.Settings.TankFootprintLimit];

        public Vector3 Position;
        public float rotation;

        public Matrix World;
        public Matrix View;
        public Matrix Projection;

        public Model Model;

        public Texture2D texture;

        internal static int total_treads_placed;

        private readonly bool alternate;
        
        public long lifeTime;

        public TankFootprint(bool alt = false)
        {
            if (total_treads_placed + 1 > MAX_FOOTPRINTS)
                footprints[Array.IndexOf(footprints, footprints.Min(x => x.lifeTime > 0))] = null; // i think?

            alternate = alt;
            total_treads_placed++;

            Model = GameResources.GetGameResource<Model>("Assets/footprint"); // use this :smiley:

            texture = GameResources.GetGameResource<Texture2D>(alt ? $"Assets/textures/tank_footprint_alt" : $"Assets/textures/tank_footprint");

            footprints[total_treads_placed] = this;

            total_treads_placed++;
        }
        public void Render()
        {
            lifeTime++;
            Matrix scale = alternate ? Matrix.CreateScale(0.5f, 1f, 0.35f) : Matrix.CreateScale(0.5f, 1f, 0.075f);

            World = scale * Matrix.CreateRotationY(rotation) * Matrix.CreateTranslation(Position);
            View = TankGame.GameView;
            Projection = TankGame.GameProjection;

            foreach (ModelMesh mesh in Model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = World;
                    effect.View = View;
                    effect.Projection = Projection;

                    effect.TextureEnabled = true;

                    effect.Texture = texture;

                    effect.SetDefaultGameLighting_IngameEntities();
                }
                mesh.Draw();
            }
        }
    }*/
    public class TankFootprint
    {
        public const int MAX_FOOTPRINTS = 100000;

        public static TankFootprint[] footprints = new TankFootprint[TankGame.Settings.TankFootprintLimit];

        public Vector3 Position;
        public float rotation;

        public Texture2D texture;

        internal static int total_treads_placed;

        private readonly bool alternate;

        public long lifeTime;

        public readonly Particle track;
        public readonly Tank owner;

        public TankFootprint(Tank owner, bool alt = false)
        {
            if (total_treads_placed + 1 > MAX_FOOTPRINTS)
                footprints[Array.IndexOf(footprints, footprints.Min(x => x.lifeTime > 0))] = null; // i think?

            alternate = alt;
            total_treads_placed++;

            texture = GameResources.GetGameResource<Texture2D>(alt ? $"Assets/textures/tank_footprint_alt" : $"Assets/textures/tank_footprint");

            track = ParticleSystem.MakeParticle(Position, texture);

            track.isAddative = false;
            //if (owner.Team != Team.NoTeam)
                //track.color = (Color)typeof(Color).GetProperty(owner.Team.ToString(), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public).GetValue(null);
            track.rotationX = -MathHelper.PiOver2;
            track.Scale = new(0.5f);

            footprints[total_treads_placed] = this;

            total_treads_placed++;
        }

        public void Render()
        {
            lifeTime++;
            // Vector3 scale = alternate ? new(0.5f, 1f, 0.35f) : new(0.5f, 1f, 0.075f);

            track.position = Position;
            track.rotationY = rotation;
            track.color = Color.White;
            // [0.0, 1.1, 1.5, 0.5]
            // [0.0, 0.1, 0.8, 0.0]
            // [0.0, 0.5, 1.2, 1.0]
            // [0.0, 2.0, 0.6, 0.2]
        }
    }
    /*public class TankDeathMark
    {
        public const int MAX_DEATH_MARKS = 1000;

        public static TankDeathMark[] deathMarks = new TankDeathMark[MAX_DEATH_MARKS];

        public Vector3 Position;
        public float rotation;

        internal static int total_death_marks;

        public Matrix World;
        public Matrix View;
        public Matrix Projection;

        public Model Model;

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

            Model = GameResources.GetGameResource<Model>("Assets/check");

            texture = GameResources.GetGameResource<Texture2D>($"Assets/textures/check/check_{color.ToString().ToLower()}");

            deathMarks[total_death_marks] = this;
        }
        public void Render()
        {
            World = Matrix.CreateScale(0.7f) * Matrix.CreateTranslation(Position);
            View = TankGame.GameView;
            Projection = TankGame.GameProjection;

            foreach (ModelMesh mesh in Model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = World;
                    effect.View = View;
                    effect.Projection = Projection;

                    effect.TextureEnabled = true;

                    effect.Alpha = 1f;

                    effect.Texture = texture;

                    effect.SetDefaultGameLighting_IngameEntities();
                }
                mesh.Draw();
            }
        }
    }*/
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
            check.rotationX = -MathHelper.PiOver2;

            deathMarks[total_death_marks] = this;
        }
        public void Render()
        {
            check.position = Position;
            check.Scale = new(0.65f);
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