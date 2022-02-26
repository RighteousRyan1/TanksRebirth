using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using WiiPlayTanksRemake.Enums;
using WiiPlayTanksRemake.Internals;
using WiiPlayTanksRemake.Internals.Common.Utilities;
using WiiPlayTanksRemake.Internals.Core.Interfaces;
using WiiPlayTanksRemake.Graphics;
using System.Linq;

namespace WiiPlayTanksRemake.GameContent
{
    public abstract class Tank
    {

        public const int TNK_WIDTH = 25;
        public const int TNK_HEIGHT = 25;

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
        /// <summary>The hitbox of this <see cref="Tank"/>.</summary>
        public BoundingBox CollisionBox { get; set; }
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

        public Vector2 Position2D => position.FlattenZ();
        public Vector2 Velocity2D => velocity.FlattenZ();

        public Vector3 position, velocity;
        /// <summary>Apply all the default parameters for this <see cref="Tank"/>.</summary>
        public virtual void ApplyDefaults() { }

        public Rectangle CollisionBox2D => new((int)(Position2D.X - TNK_WIDTH / 2 + 3), (int)(Position2D.Y - TNK_WIDTH / 2 + 2), TNK_WIDTH - 8, TNK_HEIGHT - 4);

        public string GetGeneralStats()
            => $"Pos2D: {Position2D} | Vel: {Velocity2D} | Dead: {Dead}";
        /// <summary>Destroy this <see cref="Tank"/>.</summary>
        public virtual void Destroy() { }
        /// <summary>Lay a <see cref="TankFootprint"/> under this <see cref="Tank"/>.</summary>
        public virtual void LayFootprint(bool alt) { }
        /// <summary>Shoot a <see cref="Shell"/> from this <see cref="Tank"/>.</summary>
        public virtual void Shoot() { }
        /// <summary>Make this <see cref="Tank"/> lay a <see cref="Mine"/>.</summary>
        public virtual void LayMine() { }

        // everything under this comment is added outside of the faithful remake. homing shells, etc

        /// <summary>Whether or not this <see cref="Tank"/> has a turret to fire shells with.</summary>

        public bool HasTurret { get; set; } = true;

        /// <summary>The <see cref="Shell.HomingProperties"/>of the bullets this <see cref="Tank"/> shoots.</summary>
        public Shell.HomingProperties ShellHoming = new();

        public int timeSinceLastAction = 15000;

        public virtual void RemoveSilently() { }
    }

    public class TankFootprint
    {
        public const int MAX_FOOTPRINTS = 100000;

        public static TankFootprint[] footprints = new TankFootprint[TankGame.Settings.TankFootprintLimit];

        public Vector3 location;
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

            World = scale * Matrix.CreateRotationY(rotation) * Matrix.CreateTranslation(location);
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
    }

    public class TankDeathMark
    {
        public const int MAX_DEATH_MARKS = 1000;

        public static TankDeathMark[] deathMarks = new TankDeathMark[MAX_DEATH_MARKS];

        public Vector3 location;
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
            World = Matrix.CreateScale(0.9f) * Matrix.CreateTranslation(location);
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